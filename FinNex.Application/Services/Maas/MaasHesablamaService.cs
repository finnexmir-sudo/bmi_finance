using System.Text.Json;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// Maas hesablamasinin tam mantigi.
    /// Azerbaycan Emek Mecellesi + 2026 vergi qanunvericiliyine uygundu.
    ///
    /// HESABLAMA SIRASI:
    ///  1. Esas maas (IsciMaliye.CariMaas)
    ///  2. Mezuniyyet gunlerini tap (Tesdiqlenib statuslu)
    ///  3. Mezuniyyet odenisini hesabla -- 2026 qaydasi
    ///  4. Mezuniyyet ucun esas maas kesintisi (maas / is gunu x mez.gun)
    ///  5. Bonus elave et (konullu)
    ///  6. Cerime cix (konullu)
    ///  7. BRUT = Esas - Kesinti + MezOdenis + Bonus - Cerime
    ///  8. Vergi guzesti tetbiq et (parametrden, standart 200 AZN)
    ///  9. Tutulmalari hesabla (gelir vergisi, DSMF, issizlik, ITSS)
    /// 10. NET = BRUT - Tutulmalar
    /// 11. Sirket xerclerini hesabla (DSMF 22%, issizlik 0.5%) -- melumati, tutulmur
    ///
    /// 2026 MEZUNIYYET QAYDASI:
    ///   Son 12 ayin yalniz islenmis (BrutMebleg > 0) aylari goturulur.
    ///   avgDaily = totalBrut / islenmis_ay_sayi / 30
    ///   minDaily = cariMaas / 30
    ///   Netice   = Max(avgDaily, minDaily) x gunSayi
    /// </summary>
    public class MaasHesablamaService : IMaasHesablamaService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIsciAyliqQazancService _ayliqQazancService;
        private readonly IXestelikService _xestelikService;

        public MaasHesablamaService(
            IUnitOfWork unitOfWork,
            IIsciAyliqQazancService ayliqQazancService,
            IXestelikService xestelikService)
        {
            _unitOfWork = unitOfWork;
            _ayliqQazancService = ayliqQazancService;
            _xestelikService = xestelikService;
        }

        // ─────────────────────────────────────────────────────────
        // TOPLU HESABLAMA
        // ─────────────────────────────────────────────────────────

        public async Task<Result<TopluHesablamaNeticesiDto>> TopluHesablaAsync(TopluHesablaInputDto input)
        {
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .Include(x => x.Maliye)
                .ToListAsync();

            if (!isciler.Any())
                return Result<TopluHesablamaNeticesiDto>.Fail("Aktiv isci tapilmadi.");

            var netice = new TopluHesablamaNeticesiDto { Il = input.Il, Ay = input.Ay };

            foreach (var isci in isciler)
            {
                var movcud = await _unitOfWork.Repository<Maas>()
                    .MovcuddurmuAsync(x =>
                        x.IsciId == isci.Id &&
                        x.Il == input.Il &&
                        x.Ay == input.Ay &&
                        !x.Silinib);

                if (movcud) { netice.AtlananSayi++; continue; }

                var elave = input.FerdiElaveler.FirstOrDefault(x => x.IsciId == isci.Id);

                var ferdiInput = new FerdiHesablaInputDto
                {
                    IsciId = isci.Id,
                    Il = input.Il,
                    Ay = input.Ay,
                    BonusMeblegi = elave?.BonusMeblegi ?? 0,
                    BonusAciqlama = elave?.BonusAciqlama,
                    CerimeMeblegi = elave?.CerimeMeblegi ?? 0,
                    CerimeAciqlama = elave?.CerimeAciqlama,
                    FerqliGelirMeblegi = elave?.FerqliGelirMeblegi ?? 0,
                    FerqliGelirAciqlama = elave?.FerqliGelirAciqlama
                };

                var r = await FerdiHesablaAsync(ferdiInput);

                if (r.Success)
                {
                    netice.UgurluSayi++;
                    netice.UmumiNetMebleg += r.Data!.NetMaas;
                }
                else
                {
                    netice.XetaliSayi++;
                    netice.Xetalar.Add($"{isci.Ad} {isci.Soyad}: {r.Message}");
                }
            }

            return Result<TopluHesablamaNeticesiDto>.Ok(netice);
        }

        // ─────────────────────────────────────────────────────────
        // FERDI HESABLAMA -- esas engine
        // ─────────────────────────────────────────────────────────

        public async Task<Result<MaasHesablaNeticesiDto>> FerdiHesablaAsync(FerdiHesablaInputDto input)
        {
            var izahatlar = new List<HesablamaIzahiDto>();

            // 1. Isci melumatlarini getir
            var isci = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Id == input.IsciId && !x.Silinib)
                .Include(x => x.Maliye)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Vezife)
                .FirstOrDefaultAsync();

            if (isci == null)
                return Result<MaasHesablaNeticesiDto>.Fail("Isci tapilmadi.");

            if (isci.Maliye == null)
                return Result<MaasHesablaNeticesiDto>.Fail(
                    $"{isci.Ad} {isci.Soyad} ucun maliyye melumatları tapilmadi. Evvelce maas melumatlarini doldurun.");

            // 2. Artiq hesablanibmi?
            var movcud = await _unitOfWork.Repository<Maas>()
                .MovcuddurmuAsync(x =>
                    x.IsciId == input.IsciId &&
                    x.Il == input.Il &&
                    x.Ay == input.Ay &&
                    !x.Silinib);

            if (movcud)
                return Result<MaasHesablaNeticesiDto>.Fail(
                    $"{input.Il}/{input.Ay:D2} ayi ucun artiq hesablama movcuddur.");

            // 3. Vergi parametrlerini getir (hamisi DB-den, hec ne hardcode deyil)
            var hesabTarixi = new DateTime(input.Il, input.Ay, 1);
            var p = await VergiParametrleriniGetirAsync(hesabTarixi);

            // 4. Esas maas
            decimal esasMaas = isci.Maliye.CariMaas;
            izahatlar.Add(new HesablamaIzahiDto
            {
                Addim = "Esas Emekhaqqı",
                Izah = "Iscinin muqavile uzre stat maasi",
                Mebleg = esasMaas,
                Tip = "gelir"
            });

            // 5. Qayıb (icazəsiz) günləri hesabla və kəsintini tətbiq et
            int ayIsGunu = await AyinIsGunleriniHesablaAsync(input.Il, input.Ay);
            int qayibGun = await QayibGunleriniSayAsync(input.IsciId, input.Il, input.Ay);

            decimal qayibKesinti = 0;
            if (qayibGun > 0)
            {
                qayibKesinti = Math.Round(esasMaas / ayIsGunu * qayibGun, 2);
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Qayıb Gün Kesintisi",
                    Izah = $"{esasMaas:N2} / {ayIsGunu} iş günü x {qayibGun} qayıb gün",
                    Mebleg = qayibKesinti,
                    Tip = "kesinti"
                });
            }

            // 6. Mezuniyyet gunleri ve odenisi (2026 qaydası: GS + İGS)
            //    QAYDA (BMI Finance biznes qərarı):
            //      - AySonuOdenis qeydləri → bu ayın maaşına ödəniş əlavə olunur,
            //        həm də həmin günlər üçün əsas maaşdan iş günü mütənasib
            //        KƏSİNTİ tutulur. İşçi (işlədiyi günlərə görə maaş) +
            //        məzuniyyət haqqı alır — ikiqat ödəmənin qarşısı alınır.
            //      - QabaqcadanOdenis qeydləri → Mühasib ayrıca ödəyib göndərib;
            //        bu ay üçün əsas maaşdan iş günü mütənasib KƏSİNTİ tutulur
            //        (ikiqat ödəməyə yol verməmək üçün) və ödəniş əlavə olunmur.
            var (mezTeqvimGun, mezGun) = await MezuniyyetGunleriniSayGenisAsync(input.IsciId, input.Il, input.Ay);

            decimal mezOdenis = 0;
            decimal mezKesinti = 0;

            if (mezGun > 0)
            {
                // AySonu tipli qeydlər üçün məzuniyyət ödənişi həmin ayın maaşına daxil edilir.
                // aySonuIGS    — bütün məzuniyyət günləri (PR #352, həftəsonu daxil): ödəniş üçün
                // aySonuHIGS   — yalnız real iş günü (həftəsonu/bayram çıxılır): kəsinti üçün
                var (aySonuGS, aySonuIGS, aySonuHIGS, _) = await MezuniyyetAyGunleriFiltreliSayAsync(
                    input.IsciId, input.Il, input.Ay, MezuniyyetOdenisTipi.AySonuOdenis);

                if (aySonuIGS > 0 || aySonuGS > 0)
                {
                    mezOdenis = await MezuniyyetOdenisiniHesablaV2Async(
                        input.IsciId, input.Il, input.Ay, aySonuGS, aySonuIGS);
                    izahatlar.Add(new HesablamaIzahiDto
                    {
                        Addim = "Mezuniyyet Odenisi",
                        Izah = $"MAX(Maas/{ayIsGunu}, S/12/30.4) × {aySonuIGS} məzuniyyət günü — " +
                               "böyük gündəlik dərəcə bütün məzuniyyət günlərinə (həftəsonu daxil) vurulur",
                        Mebleg = mezOdenis,
                        Tip = "gelir"
                    });

                    if (aySonuHIGS > 0)
                    {
                        var aySonuKesinti = Math.Round(esasMaas / ayIsGunu * aySonuHIGS, 2);
                        mezKesinti += aySonuKesinti;
                        izahatlar.Add(new HesablamaIzahiDto
                        {
                            Addim = "Mezuniyyet Kesintisi (ay sonu ödəniş)",
                            Izah = $"{esasMaas:N2} / {ayIsGunu} iş günü × {aySonuHIGS} faktiki iş günü " +
                                   "(işçinin məzuniyyətdə işləməyəcəyi günlər; həftəsonu skip).",
                            Mebleg = aySonuKesinti,
                            Tip = "kesinti"
                        });
                    }
                }

                // Qabaqcadan ödənilən məzuniyyətlər — ödəniş ayrıca edildiyi üçün
                // yalnız həmin günlərə görə əsas maaşdan proporsional kəsinti tutulur (faktiki iş günü).
                var (_, _, advanceHIGS, advanceQeydler) = await MezuniyyetAyGunleriFiltreliSayAsync(
                    input.IsciId, input.Il, input.Ay, MezuniyyetOdenisTipi.QabaqcadanOdenis);

                if (advanceHIGS > 0)
                {
                    mezKesinti += Math.Round(esasMaas / ayIsGunu * advanceHIGS, 2);
                    izahatlar.Add(new HesablamaIzahiDto
                    {
                        Addim = "Mezuniyyet Kesintisi (qabaqcadan ödənilən günlər)",
                        Izah = $"{esasMaas:N2} / {ayIsGunu} iş günü × {advanceHIGS} faktiki iş günü " +
                               "(qabaqcadan ödənilmiş; ödəniş ayrıca edildiyi üçün əsas maaşdan çıxılır).",
                        Mebleg = mezKesinti,
                        Tip = "kesinti"
                    });
                }

                foreach (var advanceMez in advanceQeydler)
                {
                    if (advanceMez.OdenisStatus == MezuniyyetOdenisStatus.Odenilib)
                    {
                        izahatlar.Add(new HesablamaIzahiDto
                        {
                            Addim = "Mezuniyyet (qabaqcadan ödənildi)",
                            Izah = $"{advanceMez.BaslamaTarixi:dd.MM.yyyy}–{advanceMez.BitmeTarixi:dd.MM.yyyy} " +
                                   $"dövrü üçün ödəniş {advanceMez.OdenilmeTarixi:dd.MM.yyyy} tarixində ayrıca edilib. " +
                                   $"Bu ayın hesablamasına yalnız iş günü kəsintisi daxildir.",
                            Mebleg = advanceMez.OdenenMebleg ?? 0,
                            Tip = "melumati"
                        });
                    }
                    else
                    {
                        izahatlar.Add(new HesablamaIzahiDto
                        {
                            Addim = "Mezuniyyet (qabaqcadan — ödəniş gözləyir)",
                            Izah = $"{advanceMez.BaslamaTarixi:dd.MM.yyyy}–{advanceMez.BitmeTarixi:dd.MM.yyyy} " +
                                   "məzuniyyəti qabaqcadan ödənişə təyin olunub, lakin Mühasib hələ təsdiq etməyib. " +
                                   "Bu ayın maaşına ödəniş əlavə edilməyib.",
                            Mebleg = 0,
                            Tip = "melumati"
                        });
                    }
                }
            }

            // 6.5. Xəstəlik ödənişi (avtomatik — XestelikOdenis cədvəlindən)
            // HR əvvəlcədən xəstəlik bülletənini yaradıbsa, sistem həmin ay üçün
            // şirkət payını brüt-ə əlavə edir.
            // QAYDA: İşçi xəstə olduğu günlər FAKTİKİ işlənmir → əsas maaşdan proporsional
            // kəsinti edilir (qayıb və məzuniyyət kimi), yerinə xəstəlik ödənişi gəlir kimi
            // əlavə olunur. Əks halda həmin günlər iki dəfə ödənmiş olur.
            decimal xestelikSirketOdenis = 0;
            decimal xestelikDsmfOdenis = 0;
            int xestelikSirketGun = 0;
            int xestelikDsmfGun = 0;
            decimal xestelikKesinti = 0;
            try
            {
                var xestelikler = await _xestelikService.AyUzreXestelikleriGetirAsync(input.IsciId, input.Il, input.Ay);
                foreach (var xst in xestelikler)
                {
                    var ayOdenisleri = xst.Odenisler
                        .Where(o => o.Il == input.Il && o.Ay == input.Ay && !o.Silinib);
                    foreach (var od in ayOdenisleri)
                    {
                        xestelikSirketOdenis += od.SirketOdenis;
                        xestelikDsmfOdenis += od.DsmfOdenis;
                        xestelikSirketGun += od.SirketGunSayi;
                        xestelikDsmfGun += od.DsmfGunSayi;
                    }
                }

                int xestelikGun = xestelikSirketGun + xestelikDsmfGun;
                if (xestelikGun > 0)
                {
                    xestelikKesinti = Math.Round(esasMaas / ayIsGunu * xestelikGun, 2);
                    izahatlar.Add(new HesablamaIzahiDto
                    {
                        Addim = "Xəstəlik Gün Kəsintisi",
                        Izah = $"{esasMaas:N2} / {ayIsGunu} iş günü × {xestelikGun} xəstəlik günü " +
                               $"(şirkət: {xestelikSirketGun}, DSMF: {xestelikDsmfGun}) = {xestelikKesinti:N2} ₼. " +
                               "Əsas maaşdan çıxılır — həmin günlər üçün ayrıca Xəstəlik Ödənişi əlavə olunur.",
                        Mebleg = xestelikKesinti,
                        Tip = "kesinti"
                    });
                }

                if (xestelikSirketOdenis > 0 || xestelikDsmfOdenis > 0)
                {
                    izahatlar.Add(new HesablamaIzahiDto
                    {
                        Addim = "Xəstəlik Ödənişi (Şirkət)",
                        Izah = $"{xestelikSirketGun} iş günü × bir günlük (max 14 gün/il)",
                        Mebleg = xestelikSirketOdenis,
                        Tip = "gelir"
                    });
                    if (xestelikDsmfOdenis > 0)
                    {
                        izahatlar.Add(new HesablamaIzahiDto
                        {
                            Addim = "Xəstəlik (DSMF — informativ)",
                            Izah = $"{xestelikDsmfGun} gün, sistemxarici ödənilir",
                            Mebleg = xestelikDsmfOdenis,
                            Tip = "melumati"
                        });
                    }
                }
            }
            catch { /* xəstəlik xidməti xətası əsas hesablamanı pozmasın */ }

            // 7. Bonus
            if (input.BonusMeblegi > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Bonus / Mukafat",
                    Izah = input.BonusAciqlama ?? "El ile daxil edilib",
                    Mebleg = input.BonusMeblegi,
                    Tip = "gelir"
                });

            // 7.1 Fərqli gəlir — bəzi işçilərdə bonusdan ayrı gəlir kimi ödənilir;
            // bonus kimi vergiyə cəlb olunur və brüt-ə daxil edilir, ayrıca xəttdə uçota düşür.
            if (input.FerqliGelirMeblegi > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Fərqli Gəlir",
                    Izah = input.FerqliGelirAciqlama ?? "El ile daxil edilib",
                    Mebleg = input.FerqliGelirMeblegi,
                    Tip = "gelir"
                });

            // 8. Cerime
            if (input.CerimeMeblegi > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Gecikdirme Cerimesi",
                    Izah = input.CerimeAciqlama ?? "El ile daxil edilib",
                    Mebleg = input.CerimeMeblegi,
                    Tip = "kesinti"
                });

            // 8.1. Avans — tesdiqlenmiş avans mebleğini NET-den cix
            decimal avansMebleg = 0;
            var avanslar = await _unitOfWork.Repository<Avans>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    x.IsciId == input.IsciId &&
                    x.Il == input.Il &&
                    x.Ay == input.Ay &&
                    (x.Status == AvansStatus.Tesdiqlenib || x.Status == AvansStatus.Odenilib))
                .ToListAsync();
            avansMebleg = avanslar.Sum(x => x.Mebleg);

            if (avansMebleg > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Avans Kəsintisi",
                    Izah = $"Bu ay üçün təsdiqlənmiş avans — maaşdan çıxılır",
                    Mebleg = avansMebleg,
                    Tip = "kesinti"
                });

            // 8.5. HYS (Həyat Yığım Sığortası) — işçiyə təyin olunmuş bütün aktiv HYS-lər
            // İşçi bir neçə şirkətdə HYS aça bilər; hamısının cəmi maaş bazasından çıxılır.
            var hysAyBitis = new DateTime(input.Il, input.Ay, 1).AddMonths(1).AddDays(-1);
            var isciHysList = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    x.IsciId == input.IsciId &&
                    x.BaslamaTarixi <= hysAyBitis &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= hesabTarixi))
                .ToListAsync();

            // Bütün aktiv HYS qeydlərinin cəm məbləği — vergi+DSMF bazasından çıxılır.
            decimal hysMebleg = isciHysList.Sum(x => x.Mebleg);

            // 8.6. İşəgötürən HYS payını əvvəlcədən hesabla (GROSS-a daxildir)
            decimal hysIsegoturenFaizi = 0;
            decimal hysIsegoturen = 0;
            if (hysMebleg > 0)
            {
                hysIsegoturenFaizi = (await _unitOfWork.Repository<MaasParametri>()
                    .HamisiniGetirAsync(x =>
                        x.Aktivdir && !x.Silinib &&
                        x.Nov == MaasParametrNovu.HysIsegoturenFaizi &&
                        x.BaslamaTarixi <= hesabTarixi &&
                        (x.BitmeTarixi == null || x.BitmeTarixi >= hesabTarixi)))
                    .OrderByDescending(x => x.BaslamaTarixi)
                    .FirstOrDefault()?.Deyer ?? 15m;
                hysIsegoturen = Math.Round(hysMebleg * (hysIsegoturenFaizi / 100m), 2);

                // Çoxlu HYS varsa hər birini izahatda göstər
                string hysBolmesi;
                if (isciHysList.Count == 1)
                {
                    var tek = isciHysList[0];
                    hysBolmesi = string.IsNullOrWhiteSpace(tek.Sirket)
                        ? $"Aylıq HYS: {hysMebleg:N2} ₼"
                        : $"Aylıq HYS ({tek.Sirket}): {hysMebleg:N2} ₼";
                }
                else
                {
                    var detallar = isciHysList.Select(x =>
                    {
                        var ad = string.IsNullOrWhiteSpace(x.Sirket) ? "(şirkət göstərilməyib)" : x.Sirket;
                        return $"{ad}: {x.Mebleg:N2}";
                    });
                    hysBolmesi = $"Aylıq HYS ({isciHysList.Count} şirkət) — cəm {hysMebleg:N2} ₼ [{string.Join("; ", detallar)}]";
                }

                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "HYS (İşçi payı)",
                    Izah = $"{hysBolmesi} — vergi+DSMF bazasından çıxılır, İTSS/İşsizlik bazasında tam qalır",
                    Mebleg = hysMebleg,
                    Tip = "kesinti"
                });
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "HYS (İşəgötürən payı)",
                    Izah = $"{hysMebleg:N2} × {hysIsegoturenFaizi:G29}% = {hysIsegoturen:N2} — işçinin gəlirinə əlavə olunur (İTSS/İşsizlik bazasına daxildir)",
                    Mebleg = hysIsegoturen,
                    Tip = "gelir"
                });
            }

            // 9. BRUT = əsas maaş ± düzəlişlər + işəgötürən HYS payı (işçinin ümumi gəliri)
            decimal esasBrut = esasMaas
                - mezKesinti
                + mezOdenis
                + xestelikSirketOdenis
                - xestelikKesinti
                - qayibKesinti
                + input.BonusMeblegi
                + input.FerqliGelirMeblegi
                - input.CerimeMeblegi;

            if (esasBrut < 0) esasBrut = 0;
            decimal brutMaas = esasBrut + hysIsegoturen;

            izahatlar.Add(new HesablamaIzahiDto
            {
                Addim = "Gross Məbləğ",
                Izah = esasBrut == brutMaas
                    ? $"Esas ({esasMaas:N2}) - MezKes ({mezKesinti:N2}) + MezOd ({mezOdenis:N2}) " +
                      $"- XstKes ({xestelikKesinti:N2}) + XstOd ({xestelikSirketOdenis:N2}) " +
                      $"- Qayıb ({qayibKesinti:N2}) + Bonus ({input.BonusMeblegi:N2}) - Cerime ({input.CerimeMeblegi:N2})"
                    : $"Esas ({esasMaas:N2}) ± düzəlişlər ({esasBrut:N2}) + İşəgötürən HYS ({hysIsegoturen:N2}) = {brutMaas:N2}",
                Mebleg = brutMaas,
                Tip = "melumati"
            });

            // 9.0.1 Vergi bazaları — əsas brüt (işəgötürən HYS daxil deyil) üzrə hesablanır
            //
            // QAYDA: Xəstəlik vərəqəsi üzrə şirkət ödənişi YALNIZ gəlir vergisinə cəlb olunur,
            // DSMF / İşsizlik / İTSS-dən azaddır (AR Vergi Məcəlləsi və sosial sığorta qanunları).
            //
            // HYS:
            //   - İşçi HYS payı (maaşdan tutulan): Gəlir vergisi və DSMF-dən AZAD,
            //     İTSS və İşsizlikdən cəlb olunur.
            //   - İşəgötürən HYS payı: Gəlir vergisi və DSMF-dən AZAD,
            //     İTSS və İşsizlikdən cəlb OLUNUR (işçinin qazancına əlavə kimi sayılır).
            //
            //   - vergiBazasi   = esasBrut − HYS                (gəlir vergisi üçün; xəstəlik daxildir)
            //   - dsmfBazasi    = esasBrut − HYS − Xəstəlik     (DSMF işçi/işəgötürən)
            //   - itssBazasi    = esasBrut + HYSişv − Xəstəlik  (İTSS+İşsizlik; işəgötürən HYS DAXİL)
            decimal vergiBazasi = Math.Max(0, esasBrut - hysMebleg);
            decimal dsmfBazasi  = Math.Max(0, vergiBazasi - xestelikSirketOdenis);
            decimal itssBazasi  = Math.Max(0, esasBrut + hysIsegoturen - xestelikSirketOdenis);

            if (hysMebleg > 0)
            {
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Vergi bazası (HYS çıxılıb)",
                    Izah = $"Əsas brüt ({esasBrut:N2}) − HYS ({hysMebleg:N2}) = {vergiBazasi:N2}",
                    Mebleg = vergiBazasi,
                    Tip = "melumati"
                });
            }

            if (xestelikSirketOdenis > 0)
            {
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "DSMF/İşsizlik/İTSS bazası (xəstəlik çıxılıb)",
                    Izah = $"Xəstəlik vərəqəsi şirkət ödənişi ({xestelikSirketOdenis:N2}) " +
                           "yalnız gəlir vergisinə cəlb olunur — DSMF, İşsizlik, İTSS bazalarından çıxılır. " +
                           $"DSMF bazası: {dsmfBazasi:N2}, İTSS/İşsizlik bazası: {itssBazasi:N2}",
                    Mebleg = xestelikSirketOdenis,
                    Tip = "melumati"
                });
            }

            // 9. Vergi pillələri — əvvəlcə gətiririk ki, standart 200 AZN güzəştin
            //    tətbiq edilib-edilməyəcəyini (yalnız birinci pillə daxilindədirsə)
            //    təyin etmək üçün birinci pillənin YuxariHedd-i məlum olsun.
            var pilleler = await _unitOfWork.Repository<VergiPille>()
                .HamisiniGetirAsync(x =>
                    x.Aktivdir && !x.Silinib &&
                    x.BaslamaTarixi <= hesabTarixi &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= hesabTarixi));

            var gvPilleleri = pilleler
                .Where(x => x.Nov == MaasParametrNovu.GelirVergisiFaizi)
                .OrderBy(x => x.AsagiHedd)
                .ToList();
            decimal firstBracketMax = gvPilleleri.FirstOrDefault()?.YuxariHedd ?? 2500m;

            // 9.1 İşçiyə aid aktiv güzəştləri gətir (hesab tarixində qüvvədə olanlar)
            //     Bir neçə güzəşt varsa, ən böyüyü götürülür (toplanmır — Madde 102).
            var ayBitisTarixi = new DateTime(input.Il, input.Ay, 1).AddMonths(1).AddDays(-1);
            var isciGuzestleri = await _unitOfWork.Repository<IsciGuzest>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    x.IsciId == input.IsciId &&
                    x.BaslamaTarixi <= ayBitisTarixi &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= hesabTarixi))
                .Include(x => x.Guzest)
                .Where(x => x.Guzest != null && !x.Guzest.Silinib && x.Guzest.Aktivdir)
                .ToListAsync();

            decimal maxIsciGuzesti = 0;
            string? maxIsciGuzestAd = null;
            foreach (var ig in isciGuzestleri)
            {
                if (ig.Guzest.Mebleg > maxIsciGuzesti)
                {
                    maxIsciGuzesti = ig.Guzest.Mebleg;
                    maxIsciGuzestAd = ig.Guzest.Ad;
                }
            }

            if (isciGuzestleri.Any())
            {
                var siyahi = string.Join(", ",
                    isciGuzestleri.Select(x => $"{x.Guzest.Ad} — {x.Guzest.Mebleg:N2} ₼"));
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "İşçi güzəştləri",
                    Izah = $"Aktiv güzəştlər: {siyahi}. Seçilən (ən böyük): " +
                           (maxIsciGuzestAd ?? "—"),
                    Mebleg = maxIsciGuzesti,
                    Tip = "melumati"
                });
            }

            // 9.2 Standart 200 AZN güzəşti yalnız vergi bazası birinci pillə içindədirsə tətbiq olunur.
            //     HYS çıxıldıqdan sonrakı baza istifadə olunur.
            // Standart güzəşt: GROSS (maaş + işəgötürən HYS) ≤ 2500 olmalıdır.
            // Məs: maaş 2400 + işv.HYS 150 = GROSS 2550 > 2500 → güzəşt yoxdur
            decimal standartGuzest = brutMaas <= firstBracketMax ? p.VergiGuzestiMeblegi : 0m;

            decimal vergilenecek = Math.Max(0, vergiBazasi - standartGuzest - maxIsciGuzesti);

            var vergiIzahHisseleri = new List<string> { $"Brüt: {brutMaas:N2}" };
            if (hysMebleg > 0)
                vergiIzahHisseleri.Add($"− HYS: {hysMebleg:N2} → Vergi bazası: {vergiBazasi:N2}");
            if (standartGuzest > 0)
                vergiIzahHisseleri.Add($"− Standart güzəşt: {standartGuzest:N2} (baza ≤ {firstBracketMax:N0})");
            else
                vergiIzahHisseleri.Add(
                    $"Baza > {firstBracketMax:N0} — standart {p.VergiGuzestiMeblegi:N2} ₼ güzəşti tətbiq olunmur");
            if (maxIsciGuzesti > 0)
                vergiIzahHisseleri.Add($"− İşçi güzəşti: {maxIsciGuzesti:N2} ({maxIsciGuzestAd})");
            vergiIzahHisseleri.Add($"= Vergilənəcək: {vergilenecek:N2}");

            izahatlar.Add(new HesablamaIzahiDto
            {
                Addim = "Vergi Guzesti",
                Izah = string.Join("  |  ", vergiIzahHisseleri),
                Mebleg = standartGuzest + maxIsciGuzesti,
                Tip = "melumati"
            });

            decimal HesablaTutulma(decimal mebleg, MaasParametrNovu nov, decimal flatFaiz, out string izah)
            {
                var novPilleler = pilleler.Where(x => x.Nov == nov).OrderBy(x => x.AsagiHedd).ToList();
                if (novPilleler.Any())
                {
                    var pille = PilleniTap(mebleg, novPilleler);
                    if (pille == null)
                    {
                        izah = $"{mebleg:N2} üçün pillə tapılmadı";
                        return 0;
                    }
                    var netice = Math.Round(
                        pille.SabitMebleg + (mebleg - pille.AsagiHedd) * (pille.Faiz / 100m),
                        2);
                    izah = $"{mebleg:N2} → pillə [{pille.AsagiHedd:N0}..{(pille.YuxariHedd?.ToString("N0") ?? "∞")}]: {pille.SabitMebleg:N2} + ({mebleg:N2} − {pille.AsagiHedd:N2}) × {pille.Faiz}%";
                    return netice;
                }
                // Fallback: köhnə flat faiz
                izah = $"{mebleg:N2} × {flatFaiz}% (flat)";
                return Math.Round(mebleg * (flatFaiz / 100m), 2);
            }

            // Bazalar:
            //   Gəlir vergisi  — vergiBazasi (esasBrut − HYS); xəstəlik DAXİL
            //   DSMF (işçi)    — dsmfBazasi (esasBrut − HYS − Xəstəlik)
            //   İşsizlik+İTSS  — itssBazasi (esasBrut − Xəstəlik)
            decimal gelirVergisi   = HesablaTutulma(vergilenecek, MaasParametrNovu.GelirVergisiFaizi,       p.GelirVergisiFaizi,       out var gvIzah);
            decimal dsmfIsci       = HesablaTutulma(dsmfBazasi,   MaasParametrNovu.DsmfFaizi,               p.DsmfFaizi,               out var dsmfIzah);
            decimal issizlikIsci   = Math.Round(itssBazasi * (p.IssizlikSigortasiFaizi / 100m), 2);
            decimal itss           = HesablaTutulma(itssBazasi,   MaasParametrNovu.IcbariTibbiSigortaFaizi, p.IcbariTibbiSigortaFaizi, out var itssIzah);

            string dsmfAzadIzahi()
            {
                var hisseler = new List<string>();
                if (hysMebleg > 0) hisseler.Add("HYS çıxılıb");
                if (xestelikSirketOdenis > 0) hisseler.Add("xəstəlik çıxılıb");
                return hisseler.Count > 0 ? $" ({string.Join(", ", hisseler)})" : "";
            }

            izahatlar.Add(new HesablamaIzahiDto { Addim = "Gelir Vergisi",              Izah = $"{gvIzah} (guzest: {p.VergiGuzestiMeblegi} AZN)",                                Mebleg = gelirVergisi, Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "DSMF (Isci)",                Izah = $"{dsmfIzah}{dsmfAzadIzahi()}",                                                   Mebleg = dsmfIsci,     Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "Issizlik Sigortas (Isci)",   Izah = $"{itssBazasi:N2} x {p.IssizlikSigortasiFaizi}%{(xestelikSirketOdenis > 0 ? " (xəstəlik çıxılıb)" : "")}", Mebleg = issizlikIsci, Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "ITSS (Isci)",                Izah = $"{itssIzah}{(xestelikSirketOdenis > 0 ? " (xəstəlik çıxılıb)" : "")}",            Mebleg = itss,         Tip = "vergi" });

            // 11. NET maas — HYS də NET-dən tutulur (çünki işçinin öz payıdır)
            // HYS: brüt-ə hysIsegoturen daxildir, deməli NET-dən çıxılmalıdır
            decimal umumiTutulma = gelirVergisi + dsmfIsci + issizlikIsci + itss + hysMebleg + hysIsegoturen + avansMebleg;
            decimal netMaas = brutMaas - umumiTutulma;

            // Minimum əmək haqqı yoxlaması
            if (netMaas < p.MinimumEmekHaqqi && qayibGun == 0 && mezGun == 0)
            {
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "XƏBƏRDARLIQ",
                    Izah = $"Net maaş ({netMaas:N2}) minimum əmək haqqından ({p.MinimumEmekHaqqi:N2}) aşağıdır!",
                    Mebleg = p.MinimumEmekHaqqi,
                    Tip = "melumati"
                });
            }

            izahatlar.Add(new HesablamaIzahiDto
            {
                Addim = "NET Maas",
                Izah = $"Brut ({brutMaas:N2}) - Tutulmalar ({umumiTutulma:N2})",
                Mebleg = netMaas,
                Tip = "melumati"
            });

            // 12. Sirket xercleri — pilləli (2026 qaydaları)
            // İşəgötürən payları işçi paylarıyla eyni bazalardan hesablanır:
            //   DSMF işəgötürən           — dsmfBazasi (HYS + xəstəlik çıxılıb)
            //   İşsizlik+İTSS işəgötürən  — itssBazasi (xəstəlik çıxılıb)
            decimal dsmfIsegoturen     = HesablaTutulma(dsmfBazasi, MaasParametrNovu.DsmfIsegoturenFaizi,             p.DsmfIsegotürenFaizi,     out var dsmfIsvIzah);
            decimal issizlikIsegoturen = Math.Round(itssBazasi * (p.IssizlikIsegotürenFaizi / 100m), 2);
            decimal itssIsegoturen     = HesablaTutulma(itssBazasi, MaasParametrNovu.IcbariTibbiSigortaIsegoturenFaizi, p.IcbariTibbiSigortaFaizi, out var itssIsvIzah);

            // HYS işəgötürən payı artıq 8.6-da hesablanıb (hysIsegoturen)

            izahatlar.Add(new HesablamaIzahiDto { Addim = "DSMF (Isegoturen)",              Izah = $"{dsmfIsvIzah}{dsmfAzadIzahi()} -- isciden tutulmur",                                                                 Mebleg = dsmfIsegoturen,     Tip = "sirket" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "Issizlik Sigortas (Isegoturen)", Izah = $"{itssBazasi:N2} x {p.IssizlikIsegotürenFaizi}%{(xestelikSirketOdenis > 0 ? " (xəstəlik çıxılıb)" : "")} -- isciden tutulmur", Mebleg = issizlikIsegoturen, Tip = "sirket" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "ITSS (Isegoturen)",              Izah = $"{itssIsvIzah}{(xestelikSirketOdenis > 0 ? " (xəstəlik çıxılıb)" : "")} -- isciden tutulmur",                              Mebleg = itssIsegoturen,     Tip = "sirket" });
            if (hysIsegoturen > 0)
            {
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "HYS (İşəgötürən)",
                    Izah = $"{hysMebleg:N2} × {hysIsegoturenFaizi:G29}% = {hysIsegoturen:N2} -- işəgötürən payı",
                    Mebleg = hysIsegoturen,
                    Tip = "sirket"
                });
            }

            // 13. MaasNovu kitabcasindan id-leri tap
            var novler = await _unitOfWork.Repository<MaasNovu>()
                .HamisiniGetirAsync(x => x.Aktivdir && !x.Silinib);

            // 14. Maas master yarat
            var maas = new Maas
            {
                IsciId = input.IsciId,
                Il = input.Il,
                Ay = input.Ay,
                HesablanmaTarixi = DateTime.UtcNow,
                BrutMebleg = brutMaas,
                NetMebleg = netMaas,
                Status = MaasStatus.Layihe,
                Detallar = new List<MaasDetay>()
            };

            // MaasNovu tapilmadisa xeta qaytir -- sessiz atlama yoxdur (bank ucun kritikdir)
            Result DetayEkle(string ad, MaasDetayTipi tip, decimal mebleg, string? aciq = null)
            {
                if (mebleg == 0) return Result.Ok();
                var nov = novler.FirstOrDefault(x => x.Ad == ad && x.Tip == tip);
                if (nov == null)
                    return Result.Fail($"MaasNovu tapilmadi: [{tip}] \"{ad}\". Seed data-ni yoxlayin.");
                maas.Detallar.Add(new MaasDetay { MaasNovuId = nov.Id, Mebleg = mebleg, Aciqlama = aciq });
                return Result.Ok();
            }

            // NOT: Adlar DB-dəki MaasNovleri cədvəlindəki Ad ilə dəqiq üst-üstə düşməlidir
            // (seed: migration 20260331052001_dbBaza.cs line 1608+)
            // Seed-də "Məzuniyyət Kəsintisi" və "Xəstəlik Kəsintisi" yoxdur —
            // məzuniyyət və xəstəlik günləri də "Davamiyyət Kəsintisi" altında birləşdirilir
            int xestelikUmumiGun = xestelikSirketGun + xestelikDsmfGun;
            decimal umumiDavamKesinti = qayibKesinti + mezKesinti + xestelikKesinti;
            var davamHisseleri = new List<string>();
            if (qayibGun > 0) davamHisseleri.Add($"{qayibGun} qayıb");
            if (mezGun > 0) davamHisseleri.Add($"{mezGun} məz.");
            if (xestelikUmumiGun > 0) davamHisseleri.Add($"{xestelikUmumiGun} xəstəlik");
            string? davamAciq = davamHisseleri.Count > 0
                ? $"{string.Join(" + ", davamHisseleri)} gün / {ayIsGunu} iş günü"
                : null;

            var xetalar = new[]
            {
                // Gəlirlər
                DetayEkle("Əsas Əməkhaqqı",                    MaasDetayTipi.Gelir,           esasMaas),
                DetayEkle("Məzuniyyət Ödənişi",                MaasDetayTipi.Gelir,           mezOdenis,          mezGun > 0 ? $"{mezGun} gün" : null),
                DetayEkle("Xəstəlik Ödənişi",                  MaasDetayTipi.Gelir,           xestelikSirketOdenis, xestelikSirketGun > 0 ? $"{xestelikSirketGun} iş günü (şirkət payı)" : null),
                DetayEkle("Bonus/Mükafat",                     MaasDetayTipi.Gelir,           input.BonusMeblegi, input.BonusAciqlama),
                DetayEkle("Fərqli Gəlir",                      MaasDetayTipi.Gelir,           input.FerqliGelirMeblegi, input.FerqliGelirAciqlama),
                // Kəsintilər
                DetayEkle("Davamiyyət Kəsintisi",              MaasDetayTipi.Tutulma,         umumiDavamKesinti,  davamAciq),
                DetayEkle("Gecikdirmə Cəriməsi",               MaasDetayTipi.Tutulma,         input.CerimeMeblegi, input.CerimeAciqlama),
                // Vergilər
                DetayEkle("Gəlir Vergisi",                     MaasDetayTipi.Tutulma,         gelirVergisi,       gvIzah),
                DetayEkle("DSMF (İşçi)",                       MaasDetayTipi.Tutulma,         dsmfIsci,           dsmfIzah),
                DetayEkle("İşsizlik Sığortası (İşçi)",         MaasDetayTipi.Tutulma,         issizlikIsci,       $"{itssBazasi:N2} × {p.IssizlikSigortasiFaizi}%"),
                DetayEkle("İTSS",                              MaasDetayTipi.Tutulma,         itss,               itssIzah),
                // HYS (işçi payı — brüt-dən tutulur)
                DetayEkle("HYS (İşçi)",                        MaasDetayTipi.Tutulma,         hysMebleg,          hysMebleg > 0 ? $"Aylıq HYS payı" : null),
                // Avans kəsintisi
                DetayEkle("Avans Kəsintisi",                   MaasDetayTipi.Tutulma,         avansMebleg,        avansMebleg > 0 ? "Təsdiqlənmiş avans" : null),
                // Şirkət xərcləri
                DetayEkle("DSMF (İşəgötürən)",                 MaasDetayTipi.IsegoturenXerci, dsmfIsegoturen,     dsmfIsvIzah),
                DetayEkle("İşsizlik Sığortası (İşəgötürən)",   MaasDetayTipi.IsegoturenXerci, issizlikIsegoturen, $"{itssBazasi:N2} × {p.IssizlikIsegotürenFaizi}%"),
                DetayEkle("İTSS (İşəgötürən)",                 MaasDetayTipi.IsegoturenXerci, itssIsegoturen,     itssIsvIzah),
                // HYS (işəgötürən payı — 15%)
                DetayEkle("HYS (İşəgötürən)",                  MaasDetayTipi.IsegoturenXerci, hysIsegoturen,      hysIsegoturen > 0 ? $"{hysMebleg:N2} × {hysIsegoturenFaizi:G29}%" : null),
            };

            var ilkXeta = xetalar.FirstOrDefault(x => !x.Success);
            if (ilkXeta != null)
                return Result<MaasHesablaNeticesiDto>.Fail(ilkXeta.Message);

            // Addım-addım izahları JSON kimi saxla — Detal səhifəsində
            // mühasib yekun rəqəmin hardan gəldiyini oxuya bilsin deyə.
            maas.HesablamaIzahi = JsonSerializer.Serialize(izahatlar);

            await _unitOfWork.Repository<Maas>().YaratAsync(maas);
            await _unitOfWork.YaddaSaxlaAsync();

            // 15. XestelikOdenis qeydlərini bu Maas-a bağla
            try
            {
                var xestelikler = await _xestelikService.AyUzreXestelikleriGetirAsync(input.IsciId, input.Il, input.Ay);
                foreach (var xst in xestelikler)
                {
                    var ayOdenisleri = xst.Odenisler
                        .Where(o => o.Il == input.Il && o.Ay == input.Ay && !o.Silinib && o.MaasId == null);
                    foreach (var od in ayOdenisleri)
                    {
                        od.MaasId = maas.Id;
                    }
                }
                await _unitOfWork.YaddaSaxlaAsync();
            }
            catch { }

            // 16. Aylıq qazanc tarixçəsinə avtomatik əlavə (sliding window 12 ay)
            // Brüt - məzuniyyət ödənişi - xəstəlik şirkət payı (məzuniyyət üçün
            // hesablamada bu hissələr çıxılır — qeyri-müəyyən gücləndirməyə yol verməmək üçün)
            try
            {
                // Bazaya düşən gəlir = brüt - məzuniyyət - xəstəlik + işəgötürən HYS payı
                // brutMaas artıq hysIsegoturen daxildir, ayrıca əlavə etmək lazım deyil
                decimal qazanc = brutMaas - mezOdenis - xestelikSirketOdenis;
                if (qazanc < 0) qazanc = 0;
                await _ayliqQazancService.AutoInsertFromMaasAsync(input.IsciId, input.Il, input.Ay, qazanc);
            }
            catch { /* avtomatik sync xətası əsas əməliyyatı pozmasın */ }

            // 16. Netice DTO
            var teyinat = isci.IsciTeyinatlari.FirstOrDefault();
            return Result<MaasHesablaNeticesiDto>.Ok(new MaasHesablaNeticesiDto
            {
                MaasId = maas.Id,
                IsciId = input.IsciId,
                IsciAdSoyad = $"{isci.Ad} {isci.Soyad}",
                DepartamentAd = teyinat?.Departament?.Ad ?? "—",
                VezifeAd = teyinat?.Vezife?.Ad ?? "—",
                Il = input.Il,
                Ay = input.Ay,
                EsasMaas = esasMaas,
                BonusMeblegi = input.BonusMeblegi,
                QayibGunSayi = qayibGun,
                QayibKesintisi = qayibKesinti,
                MezuniyyetGunSayi = mezGun,
                MezuniyyetOdenisi = mezOdenis,
                MezuniyyetEsasMaasKesintisi = mezKesinti,
                CerimeMeblegi = input.CerimeMeblegi,
                BrutMaas = brutMaas,
                VergiGuzesti = p.VergiGuzestiMeblegi,
                GelirVergisi = gelirVergisi,
                DsmfIsci = dsmfIsci,
                IssizlikIsci = issizlikIsci,
                Itss = itss,
                HysIsci = hysMebleg,
                NetMaas = netMaas,
                DsmfIsegoturen = dsmfIsegoturen,
                IssizlikIsegoturen = issizlikIsegoturen,
                ItssIsegoturen = itssIsegoturen,
                HysIsegoturen = hysIsegoturen,
                UmumiSirketXerci = brutMaas + dsmfIsegoturen + issizlikIsegoturen + itssIsegoturen + hysIsegoturen,
                Izahatlar = izahatlar
            });
        }

        // ─────────────────────────────────────────────────────────
        // QAYIB GUNLERINI SAY (icazesiz ishe gelmemeler)
        // ─────────────────────────────────────────────────────────
        public async Task<int> QayibGunleriniSayAsync(int isciId, int il, int ay)
        {
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

            var qayibSayi = await _unitOfWork.Repository<Davamiyyet>()
                .Query()
                .Where(x =>
                    x.IsciId == isciId &&
                    !x.Silinib &&
                    x.Status == DavamiyyetStatus.Qayib &&
                    x.Tarix >= ayBaslangic &&
                    x.Tarix <= ayBitis)
                .CountAsync();

            return qayibSayi;
        }

        // ─────────────────────────────────────────────────────────
        // MEZUNIYYET GUNLERINI SAY (köhnə — yalnız iş gün — backward compat)
        // ─────────────────────────────────────────────────────────
        public async Task<int> MezuniyyetGunleriniSayAsync(int isciId, int il, int ay)
        {
            var (_, isGun) = await MezuniyyetGunleriniSayGenisAsync(isciId, il, ay);
            return isGun;
        }

        // ─────────────────────────────────────────────────────────
        // MEZUNIYYET GUNLERINI SAY — GENİŞ
        // Həm təqvim günlərini (GS), həm iş günlərini (İGS) qaytarır
        // İş günü = şənbə/bazar VƏ bayram günləri çıxılmış
        // ─────────────────────────────────────────────────────────
        public async Task<(int TeqvimGun, int IsGun)> MezuniyyetGunleriniSayGenisAsync(int isciId, int il, int ay)
        {
            var (tg, ig, _, _) = await MezuniyyetAyGunleriFiltreliSayAsync(isciId, il, ay, null);
            return (tg, ig);
        }

        /// <summary>
        /// Verilmiş ay üçün məzuniyyət günlərini sayır. Əgər <paramref name="odenisTipi"/>
        /// verilibsə, yalnız həmin ödəniş tipinə sahib qeydlər sayılır.
        ///
        /// İki fərqli sayğac qaytarılır:
        ///   <c>IsGun</c>            — bütün məzuniyyət günləri (PR #352: həftəsonu daxil,
        ///                              yalnız hesablanmayan bayram skip). Ödəniş və balans üçün.
        ///   <c>HaqiqiIsGunu</c>     — yalnız real iş günləri (cari iş təqviminə görə —
        ///                              həftəsonu/bayram çıxılır). Maaş kəsintisi üçün:
        ///                              işçi yalnız faktiki işləyəcəyi günləri itirir.
        /// </summary>
        private async Task<(int TeqvimGun, int IsGun, int HaqiqiIsGunu, List<Mezuniyyet> Qeydler)>
            MezuniyyetAyGunleriFiltreliSayAsync(int isciId, int il, int ay, MezuniyyetOdenisTipi? odenisTipi)
        {
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

            var query = _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x =>
                    x.IsciId == isciId &&
                    !x.Silinib &&
                    x.Status == MezuniyyetStatus.Tesdiqlenib &&
                    x.BaslamaTarixi <= ayBitis &&
                    x.BitmeTarixi >= ayBaslangic);

            if (odenisTipi.HasValue)
                query = query.Where(x => x.OdenisTipi == odenisTipi.Value);

            var mezuniyyetler = await query.ToListAsync();

            if (!mezuniyyetler.Any()) return (0, 0, 0, mezuniyyetler);

            // BayramGunu cədvəlində ay üçün bütün xüsusi günlər (Bayram + IsGunu override).
            var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x =>
                    x.Tarix >= ayBaslangic &&
                    x.Tarix <= ayBitis &&
                    !x.Silinib);

            // Məzuniyyət sayğacı (PR #352): yalnız hesablanmayan bayram skip,
            // həftəsonu sayılır → balans üçün də, ödəniş üçün də.
            var skipBayramSet = ozelGunler
                .Where(x => x.Tip == GunTipi.Bayram && !x.MezuniyyetdeHesablanir)
                .Select(x => x.Tarix.Date)
                .ToHashSet();

            // Real iş günü təqvimi (kəsinti üçün): həftəsonu/Bayram çıxılır,
            // BayramGunu.Tip == IsGunu olanda günü iş günü kimi sayılır.
            var ozelTipDict = ozelGunler
                .GroupBy(x => x.Tarix.Date)
                .ToDictionary(g => g.Key, g => g.First().Tip);

            int teqvimGun = 0;
            int isGun = 0;
            int haqiqiIsGunu = 0;
            foreach (var m in mezuniyyetler)
            {
                var baslama = m.BaslamaTarixi < ayBaslangic ? ayBaslangic : m.BaslamaTarixi;
                var bitis = m.BitmeTarixi > ayBitis ? ayBitis : m.BitmeTarixi;

                for (var t = baslama; t <= bitis; t = t.AddDays(1))
                {
                    teqvimGun++;
                    if (!skipBayramSet.Contains(t.Date)) isGun++;

                    // Real iş günü: həftəsonu və bayram skip; BayramGunu.IsGunu override
                    bool realIsGunu;
                    if (ozelTipDict.TryGetValue(t.Date, out var tip))
                        realIsGunu = tip == GunTipi.IsGunu;
                    else
                        realIsGunu = t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;
                    if (realIsGunu) haqiqiIsGunu++;
                }
            }

            return (teqvimGun, isGun, haqiqiIsGunu, mezuniyyetler);
        }

        // ─────────────────────────────────────────────────────────
        // MEZUNIYYET ODENISI -- per-gün dərəcəsi əsasında
        //
        // Per-gün dərəcəsi əsasında müqayisə (BMI Finance qaydası):
        //   gunlukMaas    = CariMaas / AyİşGün         (maaş / iş gününə)
        //   gunlukMezPul  = S / 12 / 30.4              (12 aylıq orta gündəlik)
        //   gunlukDerece  = MAX(gunlukMaas, gunlukMezPul)
        //   Ödəniş        = gunlukDerece × İGS         (məzuniyyətin iş günü sayı)
        //
        // Niyə iş günü? Əsas maaşdan kəsinti də iş günü əsasında tutulur
        // (esasMaas / AyİşGün × İGS); ödənişin də eyni bazada olması iki
        // hesablama arasında uyğunsuzluğu (həftəsonu padding-i) aradan qaldırır.
        // ─────────────────────────────────────────────────────────
        public async Task<decimal> MezuniyyetOdenisiniHesablaAsync(
            int isciId, int il, int ay, int isGunSayi)
        {
            var (teqvimGun, _) = await MezuniyyetGunleriniSayGenisAsync(isciId, il, ay);
            return await MezuniyyetOdenisiniHesablaV2Async(isciId, il, ay, teqvimGun, isGunSayi);
        }

        // Əsas hesablama — yalnız İGS əhəmiyyətlidir (teqvimGun geri uyğunluq üçün saxlanır)
        public async Task<decimal> MezuniyyetOdenisiniHesablaV2Async(
            int isciId, int il, int ay, int teqvimGun, int isGun)
        {
            if (isGun <= 0) return 0;

            // 1. Cari maaş
            decimal cariMaas = (await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId))?.CariMaas ?? 0;

            // 2. Son 12 ay qazancları + artım əmsalı (K) ilə düzəlmiş cəm
            decimal S = await Son12AyDuzelmisCeminiHesablaAsync(isciId, cariMaas);

            // 3. Cari ayın iş gün sayı
            int ayIsGun = await AyinIsGunleriniHesablaAsync(il, ay);

            // 4. Gündəlik dərəcələr — hansısa biri 0 ola bilər (məs. tarixçə yoxdur)
            decimal gunlukMaas = (cariMaas > 0 && ayIsGun > 0) ? cariMaas / ayIsGun : 0m;
            decimal gunlukMezPul = (S > 0) ? S / 12m / 30.4m : 0m;

            decimal gunlukDerece = Math.Max(gunlukMaas, gunlukMezPul);
            if (gunlukDerece <= 0) return 0;

            return Math.Round(gunlukDerece * isGun, 2);
        }

        /// <summary>
        /// Son 12 ayın DÜZƏLMİŞ cəmi qazancı (artım əmsallı).
        /// K_i = MAX(1.0, CariStatMaas / StatMaas_i) — yalnız maaş artımı
        /// köhnə ayları qaldırır; azalma halda əmsal 1.0 qalır.
        /// </summary>
        private async Task<decimal> Son12AyDuzelmisCeminiHesablaAsync(int isciId, decimal cariMaas)
        {
            var son12 = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib)
                .OrderByDescending(x => x.Il * 12 + x.Ay)
                .Take(12)
                .ToListAsync();

            decimal cemi = 0;
            foreach (var q in son12)
            {
                var ayBitis = new DateTime(q.Il, q.Ay, 1).AddMonths(1).AddDays(-1);
                decimal statMaas = await StatMaasiTarixeGoreTapAsync(isciId, ayBitis);
                if (statMaas <= 0) statMaas = cariMaas;

                decimal emsal = (statMaas > 0 && cariMaas > 0)
                    ? cariMaas / statMaas
                    : 1m;
                if (emsal < 1m) emsal = 1m;

                cemi += Math.Round(q.Qazanc * emsal, 2);
            }
            return cemi;
        }

        // ─────────────────────────────────────────────────────────
        // PİLLƏ TAP — məbləğin hansı pilləyə düşdüyünü təyin edir
        // ─────────────────────────────────────────────────────────
        private static VergiPille? PilleniTap(decimal mebleg, List<VergiPille> pilleler)
        {
            if (mebleg < 0 || !pilleler.Any()) return null;

            // Pillələr AsagiHedd üzrə sıralanmış olmalıdır
            foreach (var pille in pilleler)
            {
                if (mebleg >= pille.AsagiHedd &&
                    (pille.YuxariHedd == null || mebleg < pille.YuxariHedd))
                {
                    return pille;
                }
            }

            // Heç bir pilləyə uyğun gəlmədisə — məbləğ ən sonuncudan da böyükdür
            return pilleler.LastOrDefault();
        }

        // ─────────────────────────────────────────────────────────
        // VERGI PARAMETRLERINI GETIR -- hamisi DB-den, hec ne hardcode deyil
        // ─────────────────────────────────────────────────────────
        private async Task<VergiParametrlerDto> VergiParametrleriniGetirAsync(DateTime tarix)
        {
            var parametrler = await _unitOfWork.Repository<MaasParametri>()
                .HamisiniGetirAsync(x =>
                    x.Aktivdir && !x.Silinib &&
                    x.BaslamaTarixi <= tarix &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= tarix));

            // Eyni növdən çoxlu aktiv sətr olarsa (köhnə datada mümkündür),
            // həmişə ən son BaslamaTarixi olan sətri götür
            decimal Get(MaasParametrNovu nov, decimal defolt) =>
                parametrler
                    .Where(x => x.Nov == nov)
                    .OrderByDescending(x => x.BaslamaTarixi)
                    .FirstOrDefault()?.Deyer ?? defolt;

            return new VergiParametrlerDto
            {
                GelirVergisiFaizi = Get(MaasParametrNovu.GelirVergisiFaizi, 14m),
                DsmfFaizi = Get(MaasParametrNovu.DsmfFaizi, 3m),
                IssizlikSigortasiFaizi = Get(MaasParametrNovu.IssizlikSigortasiFaizi, 0.5m),
                IcbariTibbiSigortaFaizi = Get(MaasParametrNovu.IcbariTibbiSigortaFaizi, 2m),
                VergiGuzestiMeblegi = Get(MaasParametrNovu.VergiGuzestiMeblegi, 200m),
                MinimumEmekHaqqi = Get(MaasParametrNovu.MinimumEmekHaqqi, 345m),
                // Sirket paylari indi parametrden oxunur (evvel hardcode idi)
                DsmfIsegotürenFaizi = Get(MaasParametrNovu.DsmfIsegoturenFaizi, 22m),
                IssizlikIsegotürenFaizi = Get(MaasParametrNovu.IssizlikIsegoturenFaizi, 0.5m),
                HysIsegoturenFaizi = Get(MaasParametrNovu.HysIsegoturenFaizi, 15m),
            };
        }

        // ─────────────────────────────────────────────────────────
        // VERİLMİŞ TARİXDƏ STAT MAAŞINI TAP — IsciMaasTarixcesi-dən
        //
        // Məzuniyyət pulunun artım əmsallı (K) hesablanması üçün hər ayın
        // sonunda işçinin həmin andaki ştat maaşı lazımdır.
        //
        // Məntiq:
        //  1. IsciMaasTarixcesi-dən `DeyismeTarixi <= tarix` olan ən son qeydi
        //     götür — onun `YeniMaas` həmin tarixdə qüvvədə olan maaşdır.
        //  2. Əgər tarixdən əvvəl heç bir qeyd yoxdursa, amma tarixdən sonra
        //     qeyd var — o qeydin `KohneMaas` həmin tarixdə qüvvədə olan
        //     maaşdır (hələ dəyişməmiş dövr).
        //  3. Heç bir qeyd yoxdursa (maaş heç vaxt dəyişməyib) — cari maaşı
        //     (IsciMaliye.CariMaas) qaytar (K = 1.0 verəcək).
        // ─────────────────────────────────────────────────────────
        public async Task<decimal> StatMaasiTarixeGoreTapAsync(int isciId, DateTime tarix)
        {
            var repo = _unitOfWork.Repository<IsciMaasTarixcesi>();

            // Tarixdən əvvəl (və ya bərabər) son dəyişiklik
            var evvelki = (await repo
                .HamisiniGetirAsync(
                    x => x.IsciId == isciId &&
                         !x.Silinib &&
                         x.DeyismeTarixi <= tarix,
                    izlemeden: true))
                .OrderByDescending(x => x.DeyismeTarixi)
                .FirstOrDefault();

            if (evvelki != null) return evvelki.YeniMaas;

            // Tarixdən sonra ilk dəyişiklik — o tarixdə KohneMaas qüvvədə idi
            var sonraki = (await repo
                .HamisiniGetirAsync(
                    x => x.IsciId == isciId &&
                         !x.Silinib &&
                         x.DeyismeTarixi > tarix,
                    izlemeden: true))
                .OrderBy(x => x.DeyismeTarixi)
                .FirstOrDefault();

            if (sonraki != null) return sonraki.KohneMaas;

            // Heç bir tarixçə qeydi yoxdur — maaş sabit qalıb
            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId);
            return maliye?.CariMaas ?? 0m;
        }

        // ─────────────────────────────────────────────────────────
        // TUTULMALARI HESABLA (ümumi istifadə üçün — eyni məntiq FerdiHesablaAsync-də)
        // Məzuniyyət Preview və Detail səhifələrində NET məbləği çıxartmaq üçün.
        // ─────────────────────────────────────────────────────────
        public async Task<MezuniyyetTutulmaDto> TutulmalariHesablaAsync(
            decimal brut, DateTime tarix, int? isciId = null)
        {
            if (brut < 0) brut = 0;
            var p = await VergiParametrleriniGetirAsync(tarix);

            var pilleler = await _unitOfWork.Repository<VergiPille>()
                .HamisiniGetirAsync(x =>
                    x.Aktivdir && !x.Silinib &&
                    x.BaslamaTarixi <= tarix &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= tarix));

            decimal Pilleli(decimal mebleg, MaasParametrNovu nov, decimal flatFaiz)
            {
                var novPilleler = pilleler.Where(x => x.Nov == nov).OrderBy(x => x.AsagiHedd).ToList();
                if (novPilleler.Any())
                {
                    var pille = PilleniTap(mebleg, novPilleler);
                    if (pille == null) return 0;
                    return Math.Round(
                        pille.SabitMebleg + (mebleg - pille.AsagiHedd) * (pille.Faiz / 100m), 2);
                }
                return Math.Round(mebleg * (flatFaiz / 100m), 2);
            }

            // Birinci pillə üst həddi — standart 200 AZN güzəşti yalnız brüt bu
            // sərhəddən yuxarı deyilsə tətbiq olunur (FerdiHesablaAsync ilə eyni məntiq).
            var gvPilleleri = pilleler
                .Where(x => x.Nov == MaasParametrNovu.GelirVergisiFaizi)
                .OrderBy(x => x.AsagiHedd)
                .ToList();
            decimal firstBracketMax = gvPilleleri.FirstOrDefault()?.YuxariHedd ?? 2500m;

            // İşçi güzəşti — isciId verilibsə, aktiv olan ən böyüyünü tap.
            //
            // Vacib: filter ay sonu / ay əvvəli üzrə qurulur ki, ay ortasında təyin
            // olunmuş güzəşt (məsələn 15.04 başlanır) həmin ay üçün qüvvədə sayılsın.
            //   - Başlama tarixi ≤ AY SONU   (ay daxilində başlamış güzəşt sayılır)
            //   - Bitmə tarixi   ≥ AY ƏVVƏLİ (ay daxilində bitmiş güzəşt də sayılır)
            decimal maxIsciGuzesti = 0m;
            string? isciGuzestAd = null;
            if (isciId.HasValue)
            {
                var ayBaslangic = new DateTime(tarix.Year, tarix.Month, 1);
                var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

                var isciGuzestleri = await _unitOfWork.Repository<IsciGuzest>()
                    .Query()
                    .Where(x =>
                        !x.Silinib &&
                        x.IsciId == isciId.Value &&
                        x.BaslamaTarixi <= ayBitis &&
                        (x.BitmeTarixi == null || x.BitmeTarixi >= ayBaslangic))
                    .Include(x => x.Guzest)
                    .Where(x => x.Guzest != null && !x.Guzest.Silinib && x.Guzest.Aktivdir)
                    .ToListAsync();
                foreach (var ig in isciGuzestleri)
                {
                    if (ig.Guzest.Mebleg > maxIsciGuzesti)
                    {
                        maxIsciGuzesti = ig.Guzest.Mebleg;
                        isciGuzestAd = ig.Guzest.Ad;
                    }
                }
            }

            // HYS — bütün aktiv qeydlərin cəmi (işçi bir neçə şirkətdə HYS aça bilər)
            decimal hysMebleg = 0;
            decimal hysIsv = 0;
            if (isciId.HasValue)
            {
                var ayBaslangicHys = new DateTime(tarix.Year, tarix.Month, 1);
                var ayBitisHys = ayBaslangicHys.AddMonths(1).AddDays(-1);
                hysMebleg = await _unitOfWork.Repository<IsciHYS>()
                    .Query()
                    .Where(x =>
                        !x.Silinib &&
                        x.IsciId == isciId.Value &&
                        x.BaslamaTarixi <= ayBitisHys &&
                        (x.BitmeTarixi == null || x.BitmeTarixi >= ayBaslangicHys))
                    .SumAsync(x => (decimal?)x.Mebleg) ?? 0m;
                if (hysMebleg > 0)
                {
                    var hysIsvParam = await _unitOfWork.Repository<MaasParametri>()
                        .Query()
                        .Where(x => x.Aktivdir && !x.Silinib && x.Nov == MaasParametrNovu.HysIsegoturenFaizi)
                        .OrderByDescending(x => x.BaslamaTarixi)
                        .FirstOrDefaultAsync();
                    hysIsv = Math.Round(hysMebleg * ((hysIsvParam?.Deyer ?? 15m) / 100m), 2);
                }
            }

            // HYS bazaları: vergi+DSMF = brut − HYS, İTSS/İşsizlik = brut (tam)
            decimal vergiDsmfBazasi = Math.Max(0, brut - hysMebleg);
            decimal itssBazasi = brut;

            // GROSS = brut + işəgötürən HYS (2500 güzəşt yoxlaması üçün)
            decimal grossForCheck = brut + hysIsv;

            decimal standartGuzest = grossForCheck <= firstBracketMax ? p.VergiGuzestiMeblegi : 0m;
            decimal vergilenecek = Math.Max(0, vergiDsmfBazasi - standartGuzest - maxIsciGuzesti);

            decimal gelirVergisi = Pilleli(vergilenecek, MaasParametrNovu.GelirVergisiFaizi, p.GelirVergisiFaizi);
            decimal dsmfIsci     = Pilleli(vergiDsmfBazasi, MaasParametrNovu.DsmfFaizi,        p.DsmfFaizi);
            decimal itss         = Pilleli(itssBazasi,      MaasParametrNovu.IcbariTibbiSigortaFaizi, p.IcbariTibbiSigortaFaizi);
            decimal issizlikIsci = Math.Round(itssBazasi * (p.IssizlikSigortasiFaizi / 100m), 2);
            decimal umumi        = gelirVergisi + dsmfIsci + issizlikIsci + itss;
            decimal net          = Math.Max(0, brut - umumi);

            return new MezuniyyetTutulmaDto
            {
                Brut = brut,
                VergiGuzesti = standartGuzest + maxIsciGuzesti,
                StandartGuzest = standartGuzest,
                IsciGuzesti = maxIsciGuzesti,
                IsciGuzestiAd = isciGuzestAd,
                Vergilenecek = vergilenecek,
                GelirVergisi = gelirVergisi,
                DsmfIsci = dsmfIsci,
                IssizlikIsci = issizlikIsci,
                Itss = itss,
                UmumiTutulma = umumi,
                Net = net
            };
        }

        // ─────────────────────────────────────────────────────────
        // ═════════════════════════════════════════════════════════
        // MEZUNIYYET ODENISININ TAM, ADDIM-ADDIM HESABLAMASI
        //
        // HR təsdiq anında (QabaqcadanOdenis seçilibsə) və Muhasibin Detail
        // səhifəsində bu metod çağırılır. Verilən tarix aralığını ay-ay bölür,
        // hər ay üçün MH/ƏH hesablayır və cəmləyir. Hər addım Muhasib üçün
        // insan-oxunaqlı mətn olaraq `IzahatAddimlari` siyahısında qayıdır.
        // ═════════════════════════════════════════════════════════
        public async Task<MezuniyyetOdenisHesablamaDto> MezuniyyetOdenisiDetalliHesablaAsync(
            int isciId, DateTime baslama, DateTime bitme)
        {
            var result = new MezuniyyetOdenisHesablamaDto
            {
                IsciId = isciId,
                BaslamaTarixi = baslama.Date,
                BitmeTarixi = bitme.Date
            };

            // İşçi adı (informativ)
            var isci = await _unitOfWork.Repository<Isci>()
                .GetirAsync(x => x.Id == isciId);
            result.IsciAdSoyad = isci?.TamAd ?? $"İşçi #{isciId}";

            // Cari maaş
            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId);
            decimal cariMaas = maliye?.CariMaas ?? 0;
            result.CariMaas = cariMaas;

            // Son 12 ayın qazancları (ən yeni → ən köhnəyə doğru)
            var son12Qazanc = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib)
                .OrderByDescending(x => x.Il * 12 + x.Ay)
                .Take(12)
                .ToListAsync();

            decimal Sxam = son12Qazanc.Sum(x => x.Qazanc);
            result.Son12AyCemi = Sxam;
            result.Son12AyQeydSayi = son12Qazanc.Count;

            // Ay adları (aşağıda bir neçə yerdə lazım olacaq)
            var azAyAdlari = new[]
            {
                "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
                "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr"
            };

            // ──────────────────────────────────────────────────────
            // ARTIM ƏMSALI (K) — hər ay üçün o aydaki ştat maaşına görə
            //
            //   K_i = CariStatMaas / StatMaas_i    (amma >= 1.0 — azalmalar köhnə qazancı
            //                                       aşağı salmır, yalnız artım tətbiq olunur)
            //   Qazanc_düzəlmiş_i = Qazanc_i × K_i
            //   S_düzəlmiş = Σ Qazanc_düzəlmiş_i
            // ──────────────────────────────────────────────────────
            decimal sDuzelmis = 0;
            foreach (var q in son12Qazanc.OrderBy(x => x.Il).ThenBy(x => x.Ay))
            {
                var ayBitis = new DateTime(q.Il, q.Ay, 1).AddMonths(1).AddDays(-1);
                decimal statMaasOAyda = await StatMaasiTarixeGoreTapAsync(isciId, ayBitis);
                if (statMaasOAyda <= 0) statMaasOAyda = cariMaas; // fallback

                decimal emsalRaw = (statMaasOAyda > 0 && cariMaas > 0)
                    ? cariMaas / statMaasOAyda
                    : 1m;
                // Yalnız artım — azalma halda əmsal 1.0 qalır
                if (emsalRaw < 1m) emsalRaw = 1m;

                // Hesablamada TAM (yuvarlaqlanmamış) əmsal istifadə olunur,
                // yalnız son nəticə 2 rəqəmə yuvarlaqlanır. Ekrana əmsal 4
                // rəqəmlə göstərilir.
                decimal duzelmis = Math.Round(q.Qazanc * emsalRaw, 2);
                decimal emsalDisplay = Math.Round(emsalRaw, 4);
                sDuzelmis += duzelmis;

                result.QazancEmsallari.Add(new QazancEmsalSliceDto
                {
                    Il = q.Il,
                    Ay = q.Ay,
                    AyAdi = $"{azAyAdlari[q.Ay]} {q.Il}",
                    StatMaas = statMaasOAyda,
                    Qazanc = q.Qazanc,
                    Emsal = emsalDisplay,
                    DuzelmisQazanc = duzelmis
                });
            }
            result.Son12AyDuzelmisCemi = sDuzelmis;

            // Tarixçə boşluq xəbərdarlığı
            int noTarixceCount = await _unitOfWork.Repository<IsciMaasTarixcesi>()
                .Query()
                .CountAsync(x => x.IsciId == isciId && !x.Silinib);
            if (noTarixceCount == 0 && son12Qazanc.Count > 0)
            {
                result.TarixceXeberdarliqlari.Add(
                    "İşçi üçün IsciMaasTarixcesi-də heç bir qeyd yoxdur — bütün aylar üçün " +
                    "əmsal 1.0 tətbiq olundu (maaş sabit qəbul edildi).");
            }
            else if (son12Qazanc.Count < 12)
            {
                result.TarixceXeberdarliqlari.Add(
                    $"Yalnız {son12Qazanc.Count}/12 ay qazanc qeydi mövcuddur — MH dəqiq olmaya bilər.");
            }

            // MH formulu artıq DÜZƏLMİŞ S istifadə edir
            decimal S = sDuzelmis;

            result.IzahatAddimlari.Add(
                $"İşçi: {result.IsciAdSoyad}");
            result.IzahatAddimlari.Add(
                $"Məzuniyyət dövrü: {baslama:dd.MM.yyyy} – {bitme:dd.MM.yyyy}");
            result.IzahatAddimlari.Add(
                $"Cari ştat maaşı: {cariMaas:N2} ₼");
            result.IzahatAddimlari.Add(
                $"Son 12 ayın faktiki cəmi qazancı: {Sxam:N2} ₼ ({son12Qazanc.Count} qeyd üzrə)");
            result.IzahatAddimlari.Add(
                "Hər ay üçün ARTIM ƏMSALI tətbiq olundu — köhnə ayların qazancı " +
                "bu günkü cari ştat maaşı səviyyəsinə qaldırıldı (yalnız maaş artıbsa). " +
                "Azalma halda əmsal 1.0 saxlanır, qazanc dəyişmir.");
            result.IzahatAddimlari.Add(
                $"Son 12 ayın artım əmsallı düzəlmiş cəmi qazancı: {sDuzelmis:N2} ₼");
            result.IzahatAddimlari.Add(
                "Hesablama məntiqi: hər ay üçün iki GÜNDƏLİK dərəcə tapılır — " +
                "TARİXİ ORTA (düzəlmiş cəm ÷ 12 ÷ 30.4) və CARİ MAAŞ HESABI " +
                "(cari maaş ÷ ay iş günü). Hansı gündəlik dərəcə böyükdürsə, " +
                "o, məzuniyyətin iş günü sayına vurulur.");

            // Məzuniyyət periodunu aylar üzrə böl
            var cursorAy = new DateTime(baslama.Year, baslama.Month, 1);
            var sonAy = new DateTime(bitme.Year, bitme.Month, 1);

            int umumiGS = 0, umumiIGS = 0;
            decimal cemi = 0;

            while (cursorAy <= sonAy)
            {
                int il = cursorAy.Year;
                int ay = cursorAy.Month;
                var ayBaslangic = new DateTime(il, ay, 1);
                var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

                // Bu ay üçün məzuniyyət sliceini hesabla
                var sliceBaslama = baslama > ayBaslangic ? baslama : ayBaslangic;
                var sliceBitis = bitme < ayBitis ? bitme : ayBitis;

                // İki sayğac:
                //   igs       — bütün məzuniyyət günü (PR #352, həftəsonu daxil) — ödəniş və balans üçün
                //   haqiqiIs  — yalnız real iş günü (həftəsonu/bayram çıxılır)   — maaş kəsintisi üçün
                var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
                    .HamisiniGetirAsync(x =>
                        x.Tarix >= ayBaslangic &&
                        x.Tarix <= ayBitis &&
                        !x.Silinib);
                var skipBayramSet = ozelGunler
                    .Where(x => x.Tip == GunTipi.Bayram && !x.MezuniyyetdeHesablanir)
                    .Select(x => x.Tarix.Date)
                    .ToHashSet();
                var ozelTipDict = ozelGunler
                    .GroupBy(x => x.Tarix.Date)
                    .ToDictionary(g => g.Key, g => g.First().Tip);

                int gs = 0;
                int igs = 0;
                int haqiqiIs = 0;
                for (var t = sliceBaslama.Date; t <= sliceBitis.Date; t = t.AddDays(1))
                {
                    gs++;
                    if (!skipBayramSet.Contains(t)) igs++;

                    bool realIsGunu;
                    if (ozelTipDict.TryGetValue(t, out var tip))
                        realIsGunu = tip == GunTipi.IsGunu;
                    else
                        realIsGunu = t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;
                    if (realIsGunu) haqiqiIs++;
                }

                int ayIsGun = await AyinIsGunleriniHesablaAsync(il, ay);

                // Gündəlik dərəcələr — yalnız böyüyü iş günü sayına vurulur.
                // Bu, ödənişlə kəsintini eyni bazada saxlayır (hər ikisi İGS əsaslı).
                decimal gunlukMezPul = (S > 0) ? S / 12m / 30.4m : 0m;
                decimal gunlukMaas = (cariMaas > 0 && ayIsGun > 0) ? cariMaas / ayIsGun : 0m;
                decimal gunlukDerece = Math.Max(gunlukMezPul, gunlukMaas);

                decimal MH = (igs > 0) ? Math.Round(gunlukMezPul * igs, 2) : 0m;
                decimal EH = (igs > 0) ? Math.Round(gunlukMaas * igs, 2) : 0m;
                decimal secilen = (igs > 0) ? Math.Round(gunlukDerece * igs, 2) : 0m;
                string qalib = gunlukMezPul >= gunlukMaas ? "MH" : "ƏH";

                var slice = new MezuniyyetOdenisAySliceDto
                {
                    Il = il,
                    Ay = ay,
                    AyAdi = $"{azAyAdlari[ay]} {il}",
                    TeqvimGun = gs,
                    IsGun = igs,
                    HaqiqiIsGun = haqiqiIs,
                    AyIsGun = ayIsGun,
                    MH = MH,
                    EH = EH,
                    Secilen = secilen,
                    Qalib = qalib
                };
                result.AySliceleri.Add(slice);

                result.IzahatAddimlari.Add(
                    $"── {slice.AyAdi} ──");
                result.IzahatAddimlari.Add(
                    $"    Bu ayın məzuniyyəti: {gs} təqvim günü, {igs} iş günü " +
                    $"(ayın ümumi iş günü sayı: {ayIsGun})");
                result.IzahatAddimlari.Add(
                    $"    Gündəlik məzuniyyət pulu: {S:N2} ÷ 12 ÷ 30.4 = {gunlukMezPul:N4} ₼/gün " +
                    $"→ × {igs} iş günü = {MH:N2} ₼");
                result.IzahatAddimlari.Add(
                    $"    Gündəlik maaş: {cariMaas:N2} ÷ {ayIsGun} = {gunlukMaas:N4} ₼/gün " +
                    $"→ × {igs} iş günü = {EH:N2} ₼");
                result.IzahatAddimlari.Add(
                    $"    Məzuniyyət ödənişi (böyük gündəlik götürülür): {secilen:N2} ₼ " +
                    $"({(qalib == "MH" ? "gündəlik məzuniyyət pulu üstündür" : "gündəlik maaş üstündür")})");

                umumiGS += gs;
                umumiIGS += igs;
                cemi += secilen;

                cursorAy = cursorAy.AddMonths(1);
            }

            result.UmumiTeqvimGun = umumiGS;
            result.UmumiIsGun = umumiIGS;
            result.CemiOdenis = cemi;

            result.IzahatAddimlari.Add(
                $"═══ CƏMİ ÖDƏNİŞ: {cemi:N2} ₼  ({umumiGS} təqvim günü, {umumiIGS} iş günü) ═══");

            return result;
        }

        // ─────────────────────────────────────────────────────────
        // MEZUNIYYET PREVIEW -- toplu hesablama ekranında GROSS/NET-i
        // serverlə eyni rəqəmlərlə göstərmək üçün. FerdiHesablaAsync-dəki
        // məzuniyyət bloku ilə eyni formuldur.
        //  - AySonuOdenis günləri → ödəniş əlavə olunur, həm də həmin
        //    günlərə görə əsas maaşdan iş günü mütənasib KƏSİNTİ tutulur.
        //  - QabaqcadanOdenis günləri → kəsinti tətbiq olunur, ödəniş YOX.
        // ─────────────────────────────────────────────────────────
        public async Task<(int IsGun, decimal Kesinti, decimal AySonuOdenisi)>
            MezuniyyetPreviewAsync(int isciId, int il, int ay)
        {
            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId && !x.Silinib);
            if (maliye == null) return (0, 0, 0);

            var (_, toplamIsGun, _, _) = await MezuniyyetAyGunleriFiltreliSayAsync(isciId, il, ay, null);
            if (toplamIsGun == 0) return (0, 0, 0);

            int ayIsGunu = await AyinIsGunleriniHesablaAsync(il, ay);

            // Qabaqcadan ödənilmiş günlər — kəsinti yalnız faktiki iş günləri üçün tutulur
            var (_, _, advanceHIGS, _) = await MezuniyyetAyGunleriFiltreliSayAsync(
                isciId, il, ay, MezuniyyetOdenisTipi.QabaqcadanOdenis);
            decimal kesinti = (ayIsGunu > 0 && advanceHIGS > 0)
                ? Math.Round(maliye.CariMaas / ayIsGunu * advanceHIGS, 2)
                : 0;

            // AySonu günləri:
            //   ödəniş — bütün məzuniyyət günü (PR #352, həftəsonu daxil) × per-day dərəcə
            //   kəsinti — yalnız faktiki iş günü (işçi yalnız faktiki itirdiyi günü itirir)
            var (aySonuGS, aySonuIGS, aySonuHIGS, _) = await MezuniyyetAyGunleriFiltreliSayAsync(
                isciId, il, ay, MezuniyyetOdenisTipi.AySonuOdenis);
            decimal aySonuOdenisi = (aySonuGS > 0 || aySonuIGS > 0)
                ? await MezuniyyetOdenisiniHesablaV2Async(isciId, il, ay, aySonuGS, aySonuIGS)
                : 0;

            if (ayIsGunu > 0 && aySonuHIGS > 0)
                kesinti += Math.Round(maliye.CariMaas / ayIsGunu * aySonuHIGS, 2);

            return (toplamIsGun, kesinti, aySonuOdenisi);
        }

        public Task<int> AyIsGunSayiniHesablaAsync(int il, int ay) =>
            AyinIsGunleriniHesablaAsync(il, ay);

        // ─────────────────────────────────────────────────────────
        // AYIN IS GUNLERINI HESABLA -- BayramGunu cədvəlindən oxunur
        // Tip = Bayram → həmin gün istirahət
        // Tip = IsGunu → həmin gün iş (şənbə/bazar olsa belə)
        // ─────────────────────────────────────────────────────────
        private async Task<int> AyinIsGunleriniHesablaAsync(int il, int ay)
        {
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

            var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x =>
                    x.Tarix >= ayBaslangic &&
                    x.Tarix <= ayBitis &&
                    !x.Silinib);

            var ozelDict = ozelGunler
                .GroupBy(x => x.Tarix.Date)
                .ToDictionary(g => g.Key, g => g.First().Tip);

            int sayi = 0;
            for (var t = ayBaslangic; t <= ayBitis; t = t.AddDays(1))
            {
                if (ozelDict.TryGetValue(t.Date, out var tip))
                {
                    if (tip == GunTipi.IsGunu) sayi++;
                }
                else
                {
                    if (t.DayOfWeek != DayOfWeek.Saturday &&
                        t.DayOfWeek != DayOfWeek.Sunday)
                        sayi++;
                }
            }

            return sayi > 0 ? sayi : 22; // fallback: 22 is gunu
        }
    }
}
