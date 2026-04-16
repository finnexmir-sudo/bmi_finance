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
                    CerimeAciqlama = elave?.CerimeAciqlama
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
            //    Kəsinti həmişə bütün məzuniyyət günlərinə görə tətbiq olunur.
            //    Ödəniş yalnız AySonuOdenis tipli qeydlər üçün əlavə olunur
            //    (QabaqcadanOdenis qeydləri Mühasib tərəfindən ayrıca ödənilir).
            var (mezTeqvimGun, mezGun) = await MezuniyyetGunleriniSayGenisAsync(input.IsciId, input.Il, input.Ay);

            decimal mezOdenis = 0;
            decimal mezKesinti = 0;

            if (mezGun > 0)
            {
                // Esas maasdan kesinti: maas / ayIsGunu x mezGun (yalnız iş günləri)
                mezKesinti = Math.Round(esasMaas / ayIsGunu * mezGun, 2);
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Mezuniyyet Kesintisi",
                    Izah = $"{esasMaas:N2} / {ayIsGunu} is gunu x {mezGun} mez. is gunu",
                    Mebleg = mezKesinti,
                    Tip = "kesinti"
                });

                // AySonu tipli qeydlər üçün məzuniyyət ödənişi həmin ayın maaşına daxil edilir
                var (aySonuGS, aySonuIGS, _) = await MezuniyyetAyGunleriFiltreliSayAsync(
                    input.IsciId, input.Il, input.Ay, MezuniyyetOdenisTipi.AySonuOdenis);

                if (aySonuIGS > 0 || aySonuGS > 0)
                {
                    mezOdenis = await MezuniyyetOdenisiniHesablaV2Async(
                        input.IsciId, input.Il, input.Ay, aySonuGS, aySonuIGS);
                    izahatlar.Add(new HesablamaIzahiDto
                    {
                        Addim = "Mezuniyyet Odenisi",
                        Izah = $"2026 qaydası: MAX(S/12/30.4×{aySonuGS}, Maas/{ayIsGunu}×{aySonuIGS})",
                        Mebleg = mezOdenis,
                        Tip = "gelir"
                    });
                }

                // Qabaqcadan ödənilən məzuniyyətlər üçün informativ sətir
                var (_, _, advanceQeydler) = await MezuniyyetAyGunleriFiltreliSayAsync(
                    input.IsciId, input.Il, input.Ay, MezuniyyetOdenisTipi.QabaqcadanOdenis);

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
            decimal xestelikSirketOdenis = 0;
            decimal xestelikDsmfOdenis = 0;
            int xestelikSirketGun = 0;
            int xestelikDsmfGun = 0;
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

            // 8.5. HYS (Həyat Yığım Sığortası) — işçiyə təyin olunmuş aktiv HYS-ı tap
            var hysAyBitis = new DateTime(input.Il, input.Ay, 1).AddMonths(1).AddDays(-1);
            var isciHysList = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    x.IsciId == input.IsciId &&
                    x.BaslamaTarixi <= hysAyBitis &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= hesabTarixi))
                .ToListAsync();

            // Aktiv dövrdə yalnız bir HYS olmalıdır, amma əgər çoxdursa birincini götür
            decimal hysMebleg = isciHysList.FirstOrDefault()?.Mebleg ?? 0;

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

                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "HYS (İşçi payı)",
                    Izah = $"Aylıq HYS: {hysMebleg:N2} ₼ — vergi+DSMF bazasından çıxılır, İTSS/İşsizlik bazasında tam qalır",
                    Mebleg = hysMebleg,
                    Tip = "kesinti"
                });
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "HYS (İşəgötürən payı)",
                    Izah = $"{hysMebleg:N2} × {hysIsegoturenFaizi:G29}% = {hysIsegoturen:N2} — işçinin gəlirinə əlavə olunur",
                    Mebleg = hysIsegoturen,
                    Tip = "gelir"
                });
            }

            // 9. BRUT = əsas maaş ± düzəlişlər + işəgötürən HYS payı (işçinin ümumi gəliri)
            decimal esasBrut = esasMaas
                - mezKesinti
                + mezOdenis
                + xestelikSirketOdenis
                - qayibKesinti
                + input.BonusMeblegi
                - input.CerimeMeblegi;

            if (esasBrut < 0) esasBrut = 0;
            decimal brutMaas = esasBrut + hysIsegoturen;

            izahatlar.Add(new HesablamaIzahiDto
            {
                Addim = "Gross Məbləğ",
                Izah = esasBrut == brutMaas
                    ? $"Esas ({esasMaas:N2}) - MezKes ({mezKesinti:N2}) + MezOd ({mezOdenis:N2}) - Qayıb ({qayibKesinti:N2}) + Bonus ({input.BonusMeblegi:N2}) - Cerime ({input.CerimeMeblegi:N2})"
                    : $"Esas ({esasMaas:N2}) ± düzəlişlər ({esasBrut:N2}) + İşəgötürən HYS ({hysIsegoturen:N2}) = {brutMaas:N2}",
                Mebleg = brutMaas,
                Tip = "melumati"
            });

            // 9.0.1 Vergi bazaları — əsas brüt (işəgötürən HYS daxil deyil) üzrə hesablanır
            decimal vergiDsmfBazasi = Math.Max(0, esasBrut - hysMebleg);
            decimal itssBazasi = esasBrut; // İTSS/İşsizlik əsas brüt üzrə (HYS çıxılmır)

            if (hysMebleg > 0)
            {
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Vergi+DSMF bazası (HYS çıxılıb)",
                    Izah = $"Əsas brüt ({esasBrut:N2}) − HYS ({hysMebleg:N2}) = {vergiDsmfBazasi:N2}",
                    Mebleg = vergiDsmfBazasi,
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

            decimal vergilenecek = Math.Max(0, vergiDsmfBazasi - standartGuzest - maxIsciGuzesti);

            var vergiIzahHisseleri = new List<string> { $"Brüt: {brutMaas:N2}" };
            if (hysMebleg > 0)
                vergiIzahHisseleri.Add($"− HYS: {hysMebleg:N2} → Vergi bazası: {vergiDsmfBazasi:N2}");
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

            // HYS: Gəlir vergisi və DSMF — vergiDsmfBazası üzrə hesablanır (HYS çıxılıb)
            //      İTSS və İşsizlik — itssBazası (= tam brüt) üzrə hesablanır
            decimal gelirVergisi   = HesablaTutulma(vergilenecek,    MaasParametrNovu.GelirVergisiFaizi,         p.GelirVergisiFaizi,         out var gvIzah);
            decimal dsmfIsci       = HesablaTutulma(vergiDsmfBazasi, MaasParametrNovu.DsmfFaizi,                 p.DsmfFaizi,                 out var dsmfIzah);
            decimal issizlikIsci   = Math.Round(itssBazasi * (p.IssizlikSigortasiFaizi / 100m), 2); // flat — tam brüt
            decimal itss           = HesablaTutulma(itssBazasi,      MaasParametrNovu.IcbariTibbiSigortaFaizi,   p.IcbariTibbiSigortaFaizi,   out var itssIzah);

            izahatlar.Add(new HesablamaIzahiDto { Addim = "Gelir Vergisi",              Izah = $"{gvIzah} (guzest: {p.VergiGuzestiMeblegi} AZN)", Mebleg = gelirVergisi, Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "DSMF (Isci)",                Izah = $"{dsmfIzah}{(hysMebleg > 0 ? " (HYS çıxılıb)" : "")}",  Mebleg = dsmfIsci,     Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "Issizlik Sigortas (Isci)",   Izah = $"{itssBazasi:N2} x {p.IssizlikSigortasiFaizi}% (tam brüt)", Mebleg = issizlikIsci, Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "ITSS (Isci)",                Izah = $"{itssIzah}{(hysMebleg > 0 ? " (tam brüt)" : "")}",        Mebleg = itss,         Tip = "vergi" });

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
            // HYS: DSMF işəgötürən — vergiDsmfBazası (HYS çıxılıb)
            //       İTSS/İşsizlik işəgötürən — itssBazası (tam brüt)
            decimal dsmfIsegoturen     = HesablaTutulma(vergiDsmfBazasi, MaasParametrNovu.DsmfIsegoturenFaizi,             p.DsmfIsegotürenFaizi,     out var dsmfIsvIzah);
            decimal issizlikIsegoturen = Math.Round(itssBazasi * (p.IssizlikIsegotürenFaizi / 100m), 2); // flat — tam brüt
            decimal itssIsegoturen     = HesablaTutulma(itssBazasi, MaasParametrNovu.IcbariTibbiSigortaIsegoturenFaizi, p.IcbariTibbiSigortaFaizi, out var itssIsvIzah);

            // HYS işəgötürən payı artıq 8.6-da hesablanıb (hysIsegoturen)

            izahatlar.Add(new HesablamaIzahiDto { Addim = "DSMF (Isegoturen)",              Izah = $"{dsmfIsvIzah}{(hysMebleg > 0 ? " (HYS çıxılıb)" : "")} -- isciden tutulmur", Mebleg = dsmfIsegoturen,     Tip = "sirket" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "Issizlik Sigortas (Isegoturen)", Izah = $"{itssBazasi:N2} x {p.IssizlikIsegotürenFaizi}% (tam brüt) -- isciden tutulmur", Mebleg = issizlikIsegoturen, Tip = "sirket" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "ITSS (Isegoturen)",              Izah = $"{itssIsvIzah}{(hysMebleg > 0 ? " (tam brüt)" : "")} -- isciden tutulmur",     Mebleg = itssIsegoturen,     Tip = "sirket" });
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
            // Seed-də "Məzuniyyət Kəsintisi" yoxdur — məzuniyyət günləri də
            // "Davamiyyət Kəsintisi" altında birləşdirilir
            decimal umumiDavamKesinti = qayibKesinti + mezKesinti;
            string? davamAciq =
                (qayibGun > 0 && mezGun > 0) ? $"{qayibGun} qayıb + {mezGun} məz. gün / {ayIsGunu} iş günü"
                : qayibGun > 0 ? $"{qayibGun} qayıb gün / {ayIsGunu} iş günü"
                : mezGun > 0 ? $"{mezGun} məz. gün / {ayIsGunu} iş günü"
                : null;

            var xetalar = new[]
            {
                // Gəlirlər
                DetayEkle("Əsas Əməkhaqqı",                    MaasDetayTipi.Gelir,           esasMaas),
                DetayEkle("Məzuniyyət Ödənişi",                MaasDetayTipi.Gelir,           mezOdenis,          mezGun > 0 ? $"{mezGun} gün" : null),
                DetayEkle("Xəstəlik Ödənişi",                  MaasDetayTipi.Gelir,           xestelikSirketOdenis, xestelikSirketGun > 0 ? $"{xestelikSirketGun} iş günü (şirkət payı)" : null),
                DetayEkle("Bonus/Mükafat",                     MaasDetayTipi.Gelir,           input.BonusMeblegi, input.BonusAciqlama),
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
            var (tg, ig, _) = await MezuniyyetAyGunleriFiltreliSayAsync(isciId, il, ay, null);
            return (tg, ig);
        }

        /// <summary>
        /// Verilmiş ay üçün məzuniyyət günlərini sayır. Əgər <paramref name="odenisTipi"/>
        /// verilibsə, yalnız həmin ödəniş tipinə sahib qeydlər sayılır. Həmçinin
        /// tapılmış məzuniyyət qeydlərinin siyahısını qaytarır (info üçün).
        /// </summary>
        private async Task<(int TeqvimGun, int IsGun, List<Mezuniyyet> Qeydler)>
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

            if (!mezuniyyetler.Any()) return (0, 0, mezuniyyetler);

            var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x =>
                    x.Tarix >= ayBaslangic &&
                    x.Tarix <= ayBitis &&
                    !x.Silinib);
            var ozelDict = ozelGunler
                .GroupBy(x => x.Tarix.Date)
                .ToDictionary(g => g.Key, g => g.First().Tip);

            int teqvimGun = 0;
            int isGun = 0;
            foreach (var m in mezuniyyetler)
            {
                var baslama = m.BaslamaTarixi < ayBaslangic ? ayBaslangic : m.BaslamaTarixi;
                var bitis = m.BitmeTarixi > ayBitis ? ayBitis : m.BitmeTarixi;

                for (var t = baslama; t <= bitis; t = t.AddDays(1))
                {
                    teqvimGun++;
                    bool isIsGunu;
                    if (ozelDict.TryGetValue(t.Date, out var tip))
                        isIsGunu = tip == GunTipi.IsGunu;
                    else
                        isIsGunu = t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;

                    if (isIsGunu) isGun++;
                }
            }

            return (teqvimGun, isGun, mezuniyyetler);
        }

        // ─────────────────────────────────────────────────────────
        // MEZUNIYYET ODENISI -- 2026 QAYDASI (Yeni formula)
        //
        // S    = Son 12 ayın cəmi qazancı (IsciAyliqQazanc cədvəlindən)
        // MH   = S / 12 / 30.4 × GS    (təqvim günü əsaslı)
        // ƏH   = CariMaas / AyİşGün × İGS  (iş günü əsaslı)
        // Ödəniş = MAX(MH, ƏH)
        // ─────────────────────────────────────────────────────────
        public async Task<decimal> MezuniyyetOdenisiniHesablaAsync(
            int isciId, int il, int ay, int isGunSayi)
        {
            // Geri uyğunluq üçün — yalnız İGS verilibsə, GS-i tap
            var (teqvimGun, _) = await MezuniyyetGunleriniSayGenisAsync(isciId, il, ay);
            return await MezuniyyetOdenisiniHesablaV2Async(isciId, il, ay, teqvimGun, isGunSayi);
        }

        // Əsas hesablama — həm GS, həm İGS qəbul edir (artıq ARTIM ƏMSALLIDIR)
        public async Task<decimal> MezuniyyetOdenisiniHesablaV2Async(
            int isciId, int il, int ay, int teqvimGun, int isGun)
        {
            if (teqvimGun <= 0 && isGun <= 0) return 0;

            // 1. Cari maaş
            decimal cariMaas = (await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId))?.CariMaas ?? 0;

            // 2. Son 12 ay qazancları + artım əmsalı (K) ilə düzəlmiş cəm
            decimal S = await Son12AyDuzelmisCeminiHesablaAsync(isciId, cariMaas);

            // 3. Cari ayın iş gün sayı
            int ayIsGun = await AyinIsGunleriniHesablaAsync(il, ay);

            // 4. MH = S_düzəlmiş / 12 / 30.4 × GS
            decimal MH = 0;
            if (S > 0 && teqvimGun > 0)
            {
                MH = Math.Round(S / 12m / 30.4m * teqvimGun, 2);
            }

            // 5. ƏH = CariMaas / AyİşGün × İGS  (cari maaş əsaslı iş günü)
            decimal EH = 0;
            if (cariMaas > 0 && ayIsGun > 0 && isGun > 0)
            {
                EH = Math.Round(cariMaas / ayIsGun * isGun, 2);
            }

            // 6. MAX(MH, ƏH)
            return Math.Max(MH, EH);
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

            // HYS — işçinin aktiv HYS-ını tap (vergi+DSMF bazasından çıxılır)
            decimal hysMebleg = 0;
            decimal hysIsv = 0;
            if (isciId.HasValue)
            {
                var ayBaslangicHys = new DateTime(tarix.Year, tarix.Month, 1);
                var ayBitisHys = ayBaslangicHys.AddMonths(1).AddDays(-1);
                var isciHys = await _unitOfWork.Repository<IsciHYS>()
                    .Query()
                    .Where(x =>
                        !x.Silinib &&
                        x.IsciId == isciId.Value &&
                        x.BaslamaTarixi <= ayBitisHys &&
                        (x.BitmeTarixi == null || x.BitmeTarixi >= ayBaslangicHys))
                    .FirstOrDefaultAsync();
                hysMebleg = isciHys?.Mebleg ?? 0;
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

                decimal emsal = (statMaasOAyda > 0 && cariMaas > 0)
                    ? Math.Round(cariMaas / statMaasOAyda, 4)
                    : 1m;
                // Yalnız artım — azalma halda əmsal 1.0 qalır
                if (emsal < 1m) emsal = 1m;

                decimal duzelmis = Math.Round(q.Qazanc * emsal, 2);
                sDuzelmis += duzelmis;

                result.QazancEmsallari.Add(new QazancEmsalSliceDto
                {
                    Il = q.Il,
                    Ay = q.Ay,
                    AyAdi = $"{azAyAdlari[q.Ay]} {q.Il}",
                    StatMaas = statMaasOAyda,
                    Qazanc = q.Qazanc,
                    Emsal = emsal,
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
                "Hesablama məntiqi: hər ay üçün iki rəqəm tapılır — TARİXİ ORTA " +
                "(düzəlmiş cəm ÷ 12 ÷ 30.4 × təqvim günü) və CARİ MAAŞ HESABI " +
                "(cari maaş ÷ ay iş günü × məzuniyyət iş günü). Hansı böyükdürsə, " +
                "məzuniyyət ödənişi olaraq götürülür.");

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

                // Bu ayın BayramGunu qeydlərini tap (override dəstəklənir)
                var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
                    .HamisiniGetirAsync(x =>
                        x.Tarix >= ayBaslangic &&
                        x.Tarix <= ayBitis &&
                        !x.Silinib);
                var ozelDict = ozelGunler
                    .GroupBy(x => x.Tarix.Date)
                    .ToDictionary(g => g.Key, g => g.First().Tip);

                int gs = 0;  // teqvim gün
                int igs = 0; // iş gün (şənbə/bazar + bayram çıxıldı)
                for (var t = sliceBaslama.Date; t <= sliceBitis.Date; t = t.AddDays(1))
                {
                    gs++;
                    bool isIsGunu;
                    if (ozelDict.TryGetValue(t, out var tip))
                        isIsGunu = tip == GunTipi.IsGunu;
                    else
                        isIsGunu = t.DayOfWeek != DayOfWeek.Saturday &&
                                   t.DayOfWeek != DayOfWeek.Sunday;
                    if (isIsGunu) igs++;
                }

                int ayIsGun = await AyinIsGunleriniHesablaAsync(il, ay);

                decimal MH = 0;
                if (S > 0 && gs > 0)
                    MH = Math.Round(S / 12m / 30.4m * gs, 2);

                decimal EH = 0;
                if (cariMaas > 0 && ayIsGun > 0 && igs > 0)
                    EH = Math.Round(cariMaas / ayIsGun * igs, 2);

                decimal secilen = Math.Max(MH, EH);
                string qalib = MH >= EH ? "MH" : "ƏH";

                var slice = new MezuniyyetOdenisAySliceDto
                {
                    Il = il,
                    Ay = ay,
                    AyAdi = $"{azAyAdlari[ay]} {il}",
                    TeqvimGun = gs,
                    IsGun = igs,
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
                    $"    Tarixi orta: {S:N2} ÷ 12 ÷ 30.4 × {gs} = {MH:N2} ₼");
                result.IzahatAddimlari.Add(
                    $"    Cari maaş hesabı: {cariMaas:N2} ÷ {ayIsGun} × {igs} = {EH:N2} ₼");
                result.IzahatAddimlari.Add(
                    $"    Məzuniyyət ödənişi (böyüyü götürülür): {secilen:N2} ₼ " +
                    $"({(qalib == "MH" ? "tarixi orta üstündür" : "cari maaş hesabı üstündür")})");

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
