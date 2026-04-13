using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
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

        public MaasHesablamaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            // 6. Mezuniyyet gunleri ve odenisi
            int mezGun = await MezuniyyetGunleriniSayAsync(input.IsciId, input.Il, input.Ay);

            decimal mezOdenis = 0;
            decimal mezKesinti = 0;

            if (mezGun > 0)
            {
                // Esas maasdan kesinti: maas / ayIsGunu x mezGun
                mezKesinti = Math.Round(esasMaas / ayIsGunu * mezGun, 2);
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Mezuniyyet Kesintisi",
                    Izah = $"{esasMaas:N2} / {ayIsGunu} is gunu x {mezGun} mezuniyyet gunu",
                    Mebleg = mezKesinti,
                    Tip = "kesinti"
                });

                // 2026 mezuniyyet odenisi
                mezOdenis = await MezuniyyetOdenisiniHesablaAsync(
                    input.IsciId, input.Il, input.Ay, mezGun);
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Mezuniyyet Odenisi",
                    Izah = $"2026 qaydasi: Max(son12AyOrta, cariMaas) / 30 x {mezGun} gun",
                    Mebleg = mezOdenis,
                    Tip = "gelir"
                });
            }

            // 6. Bonus
            if (input.BonusMeblegi > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Bonus / Mukafat",
                    Izah = input.BonusAciqlama ?? "El ile daxil edilib",
                    Mebleg = input.BonusMeblegi,
                    Tip = "gelir"
                });

            // 7. Cerime
            if (input.CerimeMeblegi > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Gecikdirme Cerimesi",
                    Izah = input.CerimeAciqlama ?? "El ile daxil edilib",
                    Mebleg = input.CerimeMeblegi,
                    Tip = "kesinti"
                });

            // 8. BRUT
            decimal brutMaas = esasMaas
                - mezKesinti
                + mezOdenis
                - qayibKesinti
                + input.BonusMeblegi
                - input.CerimeMeblegi;

            if (brutMaas < 0) brutMaas = 0;

            izahatlar.Add(new HesablamaIzahiDto
            {
                Addim = "Brut Mebleg",
                Izah = $"Esas ({esasMaas:N2}) - MezKes ({mezKesinti:N2}) + MezOd ({mezOdenis:N2}) - Qayıb ({qayibKesinti:N2}) + Bonus ({input.BonusMeblegi:N2}) - Cerime ({input.CerimeMeblegi:N2})",
                Mebleg = brutMaas,
                Tip = "melumati"
            });

            // 9. Vergi guzesti
            decimal vergilenecek = Math.Max(0, brutMaas - p.VergiGuzestiMeblegi);
            izahatlar.Add(new HesablamaIzahiDto
            {
                Addim = "Vergi Guzesti",
                Izah = $"Parametrden: {p.VergiGuzestiMeblegi:N2} AZN -- vergilerdirilecek: {vergilenecek:N2} AZN",
                Mebleg = p.VergiGuzestiMeblegi,
                Tip = "melumati"
            });

            // 10. Tutulmalar
            decimal gelirVergisi = Math.Round(vergilenecek * (p.GelirVergisiFaizi / 100m), 2);
            decimal dsmfIsci = Math.Round(brutMaas * (p.DsmfFaizi / 100m), 2);
            decimal issizlikIsci = Math.Round(brutMaas * (p.IssizlikSigortasiFaizi / 100m), 2);
            decimal itss = Math.Round(brutMaas * (p.IcbariTibbiSigortaFaizi / 100m), 2);

            izahatlar.Add(new HesablamaIzahiDto { Addim = "Gelir Vergisi", Izah = $"{vergilenecek:N2} x {p.GelirVergisiFaizi}% (guzest: {p.VergiGuzestiMeblegi} AZN)", Mebleg = gelirVergisi, Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "DSMF (Isci)", Izah = $"{brutMaas:N2} x {p.DsmfFaizi}%", Mebleg = dsmfIsci, Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "Issizlik Sigortas (Isci)", Izah = $"{brutMaas:N2} x {p.IssizlikSigortasiFaizi}%", Mebleg = issizlikIsci, Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "ITSS", Izah = $"{brutMaas:N2} x {p.IcbariTibbiSigortaFaizi}%", Mebleg = itss, Tip = "vergi" });

            // 11. NET maas
            decimal umumiTutulma = gelirVergisi + dsmfIsci + issizlikIsci + itss;
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

            // 12. Sirket xercleri -- indi parametrden oxunur, hardcode deyil
            decimal dsmfIsegoturen = Math.Round(brutMaas * (p.DsmfIsegotürenFaizi / 100m), 2);
            decimal issizlikIsegoturen = Math.Round(brutMaas * (p.IssizlikIsegotürenFaizi / 100m), 2);

            izahatlar.Add(new HesablamaIzahiDto { Addim = "DSMF (Isegoturen)", Izah = $"{brutMaas:N2} x {p.DsmfIsegotürenFaizi}% -- isciden tutulmur", Mebleg = dsmfIsegoturen, Tip = "sirket" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "Issizlik Sigortas (Isegoturen)", Izah = $"{brutMaas:N2} x {p.IssizlikIsegotürenFaizi}% -- isciden tutulmur", Mebleg = issizlikIsegoturen, Tip = "sirket" });

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

            var xetalar = new[]
            {
                // Gelirlər
                DetayEkle("Esas Emekhaqqı",          MaasDetayTipi.Gelir,           esasMaas),
                DetayEkle("Mezuniyyet Odenisi",       MaasDetayTipi.Gelir,           mezOdenis,          mezGun > 0 ? $"{mezGun} gun" : null),
                DetayEkle("Bonus/Mukafat",            MaasDetayTipi.Gelir,           input.BonusMeblegi, input.BonusAciqlama),
                // Kesintiler
                DetayEkle("Davamiyyət Kəsintisi",     MaasDetayTipi.Tutulma,         qayibKesinti,       qayibGun > 0 ? $"{qayibGun} qayıb gün / {ayIsGunu} iş günü" : null),
                DetayEkle("Mezuniyyet Kesintisi",     MaasDetayTipi.Tutulma,         mezKesinti,         mezGun > 0 ? $"{mezGun} gun / {ayIsGunu} is gunu" : null),
                DetayEkle("Gecikdirme Cerimesi",      MaasDetayTipi.Tutulma,         input.CerimeMeblegi, input.CerimeAciqlama),
                // Vergiler
                DetayEkle("Gelir Vergisi",            MaasDetayTipi.Tutulma,         gelirVergisi,       $"{p.GelirVergisiFaizi}% (guzest: {p.VergiGuzestiMeblegi} AZN)"),
                DetayEkle("DSMF (Isci)",              MaasDetayTipi.Tutulma,         dsmfIsci,           $"{p.DsmfFaizi}%"),
                DetayEkle("Issizlik Sigortas (Isci)", MaasDetayTipi.Tutulma,         issizlikIsci,       $"{p.IssizlikSigortasiFaizi}%"),
                DetayEkle("ITSS",                     MaasDetayTipi.Tutulma,         itss,               $"{p.IcbariTibbiSigortaFaizi}%"),
                // Sirket xercleri
                DetayEkle("DSMF (Isegoturen)",        MaasDetayTipi.IsegoturenXerci, dsmfIsegoturen,     $"{p.DsmfIsegotürenFaizi}%"),
                DetayEkle("Issizlik (Isegoturen)",    MaasDetayTipi.IsegoturenXerci, issizlikIsegoturen, $"{p.IssizlikIsegotürenFaizi}%"),
            };

            var ilkXeta = xetalar.FirstOrDefault(x => !x.Success);
            if (ilkXeta != null)
                return Result<MaasHesablaNeticesiDto>.Fail(ilkXeta.Message);

            await _unitOfWork.Repository<Maas>().YaratAsync(maas);
            await _unitOfWork.YaddaSaxlaAsync();

            // 15. Netice DTO
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
                NetMaas = netMaas,
                DsmfIsegoturen = dsmfIsegoturen,
                IssizlikIsegoturen = issizlikIsegoturen,
                UmumiSirketXerci = brutMaas + dsmfIsegoturen + issizlikIsegoturen,
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
        // MEZUNIYYET GUNLERINI SAY
        // ─────────────────────────────────────────────────────────
        public async Task<int> MezuniyyetGunleriniSayAsync(int isciId, int il, int ay)
        {
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

            var mezuniyyetler = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x =>
                    x.IsciId == isciId &&
                    !x.Silinib &&
                    x.Status == MezuniyyetStatus.Tesdiqlenib &&
                    x.BaslamaTarixi <= ayBitis &&
                    x.BitmeTarixi >= ayBaslangic)
                .ToListAsync();

            if (!mezuniyyetler.Any()) return 0;

            int gunSayi = 0;
            foreach (var m in mezuniyyetler)
            {
                var baslama = m.BaslamaTarixi < ayBaslangic ? ayBaslangic : m.BaslamaTarixi;
                var bitis = m.BitmeTarixi > ayBitis ? ayBitis : m.BitmeTarixi;

                for (var t = baslama; t <= bitis; t = t.AddDays(1))
                {
                    if (t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday)
                        gunSayi++;
                }
            }

            return gunSayi;
        }

        // ─────────────────────────────────────────────────────────
        // MEZUNIYYET ODENISI -- 2026 QAYDASI
        //
        // Kohne kod: NetMebleg istifade edirdi + 29.3-e bolurdu
        // Yeni 2026:
        //   1. Son 12 ayin yalniz islenmis (BrutMebleg > 0) aylari
        //   2. avgDaily = totalBrut / islenmis_ay_sayi / 30
        //   3. minDaily = cariMaas / 30
        //   4. Netice = Max(avgDaily, minDaily) x gunSayi
        // ─────────────────────────────────────────────────────────
        public async Task<decimal> MezuniyyetOdenisiniHesablaAsync(
            int isciId, int il, int ay, int gunSayi)
        {
            if (gunSayi <= 0) return 0;

            // Son 12 ayin BRUT maaslari (LegvEdilmis ve silinmisler xaric)
            var son12Ay = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x =>
                    x.IsciId == isciId &&
                    !x.Silinib &&
                    x.Status != MaasStatus.LegvEdildi &&
                    (x.Il * 12 + x.Ay) < (il * 12 + ay))
                .OrderByDescending(x => x.Il * 12 + x.Ay)
                .Take(12)
                .ToListAsync();

            decimal cariMaas = (await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId))?.CariMaas ?? 0;

            decimal avgDaily;

            if (son12Ay.Any())
            {
                // 2026 qaydasi: yalniz islenmis aylar (unpaid leave aylari avtomatik cixilir)
                var islenmisAylar = son12Ay.Where(x => x.BrutMebleg > 0).ToList();

                if (islenmisAylar.Any())
                {
                    decimal totalBrut = islenmisAylar.Sum(x => x.BrutMebleg);
                    decimal avgMonthly = totalBrut / islenmisAylar.Count;
                    avgDaily = avgMonthly / 30m;
                }
                else
                {
                    avgDaily = cariMaas / 30m; // Hec bir islenmis ay yoxdursa
                }
            }
            else
            {
                avgDaily = cariMaas / 30m; // Yeni isci -- tarixce yoxdur
            }

            // 2026 minimumu: cari maasdan asagi ola bilmez
            decimal minDaily = cariMaas / 30m;
            decimal hesabGunluk = Math.Max(avgDaily, minDaily);

            return Math.Round(hesabGunluk * gunSayi, 2);
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
            };
        }

        // ─────────────────────────────────────────────────────────
        // AYIN IS GUNLERINI HESABLA -- bayram gunleri DB-den oxunur
        // ─────────────────────────────────────────────────────────
        private async Task<int> AyinIsGunleriniHesablaAsync(int il, int ay)
        {
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

            var bayramlar = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x =>
                    x.Tarix >= ayBaslangic &&
                    x.Tarix <= ayBitis &&
                    !x.Silinib);

            var bayramTarixleri = bayramlar.Select(x => x.Tarix.Date).ToHashSet();

            int sayi = 0;
            for (var t = ayBaslangic; t <= ayBitis; t = t.AddDays(1))
            {
                if (t.DayOfWeek != DayOfWeek.Saturday &&
                    t.DayOfWeek != DayOfWeek.Sunday &&
                    !bayramTarixleri.Contains(t.Date))
                    sayi++;
            }

            return sayi > 0 ? sayi : 22; // fallback: 22 is gunu
        }
    }
}
