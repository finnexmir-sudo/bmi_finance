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
        private readonly Microsoft.Extensions.Configuration.IConfiguration? _config;

        public MaasHesablamaService(
            IUnitOfWork unitOfWork,
            IIsciAyliqQazancService ayliqQazancService,
            IXestelikService xestelikService,
            Microsoft.Extensions.Configuration.IConfiguration? config = null)
        {
            _unitOfWork = unitOfWork;
            _ayliqQazancService = ayliqQazancService;
            _xestelikService = xestelikService;
            _config = config;
        }

        // ─────────────────────────────────────────────────────────
        // MƏZUNİYYƏT — YENİ QAYDA KƏSİM TARİXİ (ƏM md.140 tam-dövr MAX)
        // Başlama tarixi bu tarixdən (daxil) sonrakı məzuniyyətlərə yeni qayda
        // tətbiq olunur; köhnələr KÖHNƏ düsturla qalır (keçmiş dəyişmir).
        // Konfiq: appsettings → Mezuniyyet:YeniQaydaBaslama (yyyy-MM-dd).
        // ─────────────────────────────────────────────────────────
        private static readonly DateTime YeniQaydaDefolt = new DateTime(2026, 8, 1);

        private DateTime YeniQaydaBaslamaTarixi()
        {
            var s = _config?["Mezuniyyet:YeniQaydaBaslama"];
            if (!string.IsNullOrWhiteSpace(s) &&
                DateTime.TryParseExact(s.Trim(),
                    new[] { "yyyy-MM-dd", "dd.MM.yyyy" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var d))
                return d.Date;
            return YeniQaydaDefolt;
        }

        private bool YeniQaydaTetbiqOlunur(DateTime mezuniyyetBaslama)
            => mezuniyyetBaslama.Date >= YeniQaydaBaslamaTarixi();

        // ─────────────────────────────────────────────────────────
        // QABAQCADAN ÖDƏNİLƏN MƏZUNİYYƏT — VERGİ BAZASININ AYLARA BÖLÜNMƏSİ
        // (27.08.2026, mühasibin uçot modeli — istifadəçi qərarı)
        //
        // KÖHNƏ DAVRANIŞ: məzuniyyətin BÜTÜN brütü ÖDƏNİLMƏ ayının vergi bazasına
        //   düşürdü (Rüfət C.: avqustda 761,90 + 365,37 = 1 127,27).
        // YENİ DAVRANIŞ: hər ay yalnız ÖZ payını götürür
        //   (avqust 761,90 + 38,10 = 800,00; sentyabr 472,73 + 327,27 = 800,00).
        //
        // ⚠️ İŞÇİYƏ ÖDƏNİLƏN MƏBLƏĞ DƏYİŞMİR. Düstur (bax addım 11):
        //     net = brutMaas − [vergilər(brutMaas + pay) + avans − (pay − payın neti)]
        //   İki model eyni nəticəni verir, çünki «payın neti» elə vergili və
        //   vergisiz bazanın fərqi kimi təyin olunub. Yoxlanıb (Rüfət C., 2026):
        //     köhnə  avqust 761,90 − 151,74 − 200 + 56,64 = 466,80
        //     yeni   avqust 761,90 − 101,00 − 200 +  5,90 = 466,80
        //     köhnə  sentyabr 472,73 − 50,26            = 422,47
        //     yeni   sentyabr 472,73 − 101,00 + 50,74   = 422,47
        //   Dəyişən YALNIZ bəyan olunan vergi bazası və tutulma məbləğləridir.
        //
        // KEÇMİŞ AYLAR TOXUNULMUR: kəsim tarixindən ƏVVƏLKİ maaş ayları köhnə
        // modellə hesablanır — onlar artıq bəyan olunub.
        // Konfiq: appsettings → Mezuniyyet:AvansAylaraBolunmeBaslama (yyyy-MM-dd).
        // ─────────────────────────────────────────────────────────
        private static readonly DateTime AvansBolgusuDefolt = new DateTime(2026, 8, 1);

        private DateTime AvansBolgusuBaslamaTarixi()
        {
            var s = _config?["Mezuniyyet:AvansAylaraBolunmeBaslama"];
            if (!string.IsNullOrWhiteSpace(s) &&
                DateTime.TryParseExact(s.Trim(),
                    new[] { "yyyy-MM-dd", "dd.MM.yyyy" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var d))
                return d.Date;
            return AvansBolgusuDefolt;
        }

        /// <summary>Maaş ayı (il, ay) kəsim tarixindən sonradırsa aylara bölünmüş model tətbiq olunur.</summary>
        private bool AvansAylaraBolunurmu(int il, int ay)
        {
            var k = AvansBolgusuBaslamaTarixi();
            return new DateTime(il, ay, 1) >= new DateTime(k.Year, k.Month, 1);
        }

        /// <summary>
        /// Qabaqcadan ödənilmiş məzuniyyətin AY-AY paylarını qaytarır (brüt / vergi / net).
        ///
        /// TƏK MƏNBƏDİR — həm maaş hesablaması (vergi bazası), həm də Mühasib
        /// Detail səhifəsi bunu çağırır. İki nüsxə saxlansaydı biri mütləq
        /// köhnə qalardı (CLAUDE.md qaydası).
        ///
        /// Payın brütü = slice.EH («cari maaş hesabı»): işlənmiş maaş + EH = tam ay.
        /// Neti MARJİNALDIR — tax(işlənmiş + EH) − tax(işlənmiş); ayın güzəştlərini
        /// işlənmiş hissə udur, ona görə düz faiz (15,5%) YAZILMIR, real vergi
        /// funksiyası çağırılır (aşağı maaşda güzəşt sərhədi tələsi var).
        ///
        /// <paramref name="hedefNet"/> verilibsə (faktiki ödənilmiş NET) netlərin
        /// cəmi ona qəpiyinə bağlanır — qalıq SON aya yazılır. Verilməsə paylar
        /// olduğu kimi qalır. Bank köçürməsi ilə 1–2 qəpik fərq qalmasın deyə
        /// maaş tərəfi HƏMİŞƏ faktiki ödənilmiş neti ötürməlidir.
        /// </summary>
        public async Task<List<MezuniyyetAvansAyPayiDto>> MezuniyyetAvansAyPaylariAsync(
            int isciId, DateTime baslama, DateTime bitme, decimal? hedefNet = null)
        {
            var paylar = new List<MezuniyyetAvansAyPayiDto>();

            var hesab = await MezuniyyetOdenisiDetalliHesablaAsync(isciId, baslama, bitme);
            if (hesab.AySliceleri.Count == 0 || hesab.CemiOdenis <= 0) return paylar;

            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId);
            decimal cariMaas = maliye?.CariMaas ?? 0m;

            // ⚠️ EH payları ÖDƏNİLƏN CƏMƏ normallaşdırılır, xam götürülmür.
            // ÜSUL B qalib gələndə ΣEH = CemiOdenis olur və pay elə EH-ə bərabər
            // çıxır (Rüfət C.: 38,10 / 327,27). ÜSUL A qalibdirsə CemiOdenis daha
            // böyükdür — normallaşdırmasaq payların cəmi ödənilən brütdən AZ olar
            // və həm vergi bazası, həm əvəzləşmə səssizcə əskik qalar.
            decimal ehCemi = hesab.AySliceleri.Sum(x => x.EH);
            if (ehCemi <= 0) return paylar;

            foreach (var s in hesab.AySliceleri)
            {
                decimal brut = Math.Round(hesab.CemiOdenis * s.EH / ehCemi, 2);

                // İşlənmiş günlərin maaşı — həmin ayın vergi güzəştini bu hissə udur
                int ig = Math.Max(0, s.AyIsGun - s.HaqiqiIsGun);
                decimal im = (cariMaas > 0 && s.AyIsGun > 0)
                    ? Math.Round(cariMaas / s.AyIsGun * ig, 2) : 0m;

                var ayIlk = new DateTime(s.Il, s.Ay, 1);
                var itax = await TutulmalariHesablaAsync(im, ayIlk, isciId);
                var etax = await TutulmalariHesablaAsync(im + brut, ayIlk, isciId);

                decimal net = Math.Round(etax.Net - itax.Net, 2);
                paylar.Add(new MezuniyyetAvansAyPayiDto
                {
                    Il    = s.Il,
                    Ay    = s.Ay,
                    Brut  = brut,
                    Net   = net,
                    Vergi = Math.Round(brut - net, 2)
                });
            }

            // Yuvarlaqlaşma qalığı SON aya — netlərin cəmi ödənilən NET-ə bərabər olsun
            if (hedefNet.HasValue && hedefNet.Value > 0 && paylar.Count > 0)
            {
                decimal evvelkiler = paylar.Take(paylar.Count - 1).Sum(x => x.Net);
                var son = paylar[^1];
                son.Net   = Math.Round(hedefNet.Value - evvelkiler, 2);
                son.Vergi = Math.Round(son.Brut - son.Net, 2);
            }

            return paylar;
        }

        // ─────────────────────────────────────────────────────────
        // TOPLU HESABLAMA
        // ─────────────────────────────────────────────────────────

        public async Task<Result<TopluHesablamaNeticesiDto>> TopluHesablaAsync(TopluHesablaInputDto input, bool saxla = true)
        {
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib && (
                    x.Status == IsciStatus.Aktiv ||
                    (x.Status == IsciStatus.IshtenCixib &&
                     x.IsdenAyrilmaTarixi.HasValue &&
                     x.IsdenAyrilmaTarixi.Value.Year == input.Il &&
                     x.IsdenAyrilmaTarixi.Value.Month == input.Ay)))
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
                        !x.Silinib &&
                        x.Status != MaasStatus.LegvEdildi);

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
                    IH07Meblegi = elave?.IH07Meblegi ?? 0,
                    VM9821Meblegi = elave?.VM9821Meblegi ?? 0,
                    ElaveGelirler = elave?.ElaveGelirler ?? new()
                };

                var r = await FerdiHesablaAsync(ferdiInput, saxla);

                if (r.Success)
                {
                    var d = r.Data!;
                    netice.UgurluSayi++;
                    netice.UmumiNetMebleg += d.NetMaas;

                    // Provodka önizləməsi üçün tam server-nəticəsi (Detallar + BankHesabNo daxil).
                    netice.FerdiNeticeler.Add(d);

                    // Önizləmə üçün: hər işçinin server-tərəfi yekun rəqəmləri.
                    // (saxla=true olsa da doldurulur — zərərsizdir, yalnız oxunur.)
                    netice.Ferdiler.Add(new MaasOnizlemeFerdiDto
                    {
                        IsciId       = d.IsciId,
                        BrutMaas     = d.BrutMaas,
                        NetMaas      = d.NetMaas,
                        GelirVergisi = d.GelirVergisi,
                        DsmfIsci     = d.DsmfIsci,
                        IssizlikIsci = d.IssizlikIsci,
                        Itss         = d.Itss,
                        HysIsci      = d.HysIsci,
                        UmumiTutulma = d.UmumiTutulma
                    });
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

        public async Task<Result<MaasHesablaNeticesiDto>> FerdiHesablaAsync(FerdiHesablaInputDto input, bool saxla = true)
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

            // 2. Artiq hesablanibmi? LegvEdildi qeydlər hard-delete olunduğu üçün
            //    DB-də qalmır — !x.Silinib şərti kifayətdir.
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

            // 3.1 Aylıq əlavə qeydi — Bonus və Overtime.
            //     HR ayrıca "Aylıq Əlavə" səhifəsində daxil edir; maaş hesablama
            //     bu məbləğləri həmin qeyddən oxuyur (tək mənbə).
            decimal overtimeMebleg = 0;
            var elaveQeyd = await _unitOfWork.Repository<AyliqElaveQeydi>()
                .GetirAsync(x => x.IsciId == input.IsciId
                              && x.Il == input.Il
                              && x.Ay == input.Ay
                              && !x.Silinib);
            if (elaveQeyd != null)
            {
                input.BonusMeblegi = elaveQeyd.Bonus;
                overtimeMebleg = elaveQeyd.Overtime;
            }

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

            // 5b. Ödənişsiz (öz hesabına) məzuniyyət kəsintisi — Ə.M. 129.
            //     Həmin real iş günlərinin baza haqqı çıxılır; əvəzinə HEÇ BİR ödəniş
            //     (məzuniyyət/xəstəlik haqqı) əlavə olunmur. İşçi cədvəldə qalır,
            //     tam ay ödənişsizdirsə maaş 0-a enir.
            int odenissizGun = await OzHesabinaIsGunuSayAsync(input.IsciId, input.Il, input.Ay);
            decimal odenissizKesinti = 0;
            if (odenissizGun > 0 && ayIsGunu > 0)
            {
                odenissizKesinti = Math.Round(esasMaas / ayIsGunu * odenissizGun, 2);
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Ödənişsiz Məzuniyyət Kəsintisi",
                    Izah = $"{esasMaas:N2} / {ayIsGunu} iş günü × {odenissizGun} ödənişsiz gün (Ə.M. 129 — məzuniyyət haqqı yoxdur)",
                    Mebleg = odenissizKesinti,
                    Tip = "kesinti"
                });
            }

            // 5a. MƏZUNIYYƏT KOMPENSASİYASI (varsa) — istifadə edilməmiş əmək məzuniyyəti
            //     günlərinə görə kompensasiya /HR/Kompensasiya səhifəsində hesablanıb
            //     Layihe/Tesdiqlenib statusunda saxlanılır. Maaş hesablananda gəlir kimi
            //     BRUT-a əlavə olunur (vergi tutulur, NET-də əks olunur); maaş saxlandıqdan
            //     sonra status MaasaDaxilEdildi olur (aşağıda 15b).
            //     DİQQƏT: dövrəvi DI-dan qaçmaq üçün IKompensasiyaService inject EDİLMİR —
            //     cədvəl birbaşa repozitoriyadan oxunur.
            decimal mezKompensasiyaMebleg = 0;
            MezuniyyetKompensasiyasi? aktivKompensasiya = null;
            try
            {
                aktivKompensasiya = await _unitOfWork.Repository<MezuniyyetKompensasiyasi>()
                    .Query()
                    .FirstOrDefaultAsync(x => x.IsciId == input.IsciId
                                           && x.HesablananIl == input.Il
                                           && x.HesablananAy == input.Ay
                                           && !x.Silinib
                                           && (x.Status == KompensasiyaStatus.Layihe
                                            || x.Status == KompensasiyaStatus.Tesdiqlenib));
            }
            catch { /* kompensasiya cədvəli xətası maaş hesablamasını dayandırmasın */ }

            if (aktivKompensasiya != null)
            {
                mezKompensasiyaMebleg = aktivKompensasiya.CemiMebleg;
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "İstifadə edilməmiş Məz. Kompensasiyası",
                    Izah = $"{aktivKompensasiya.CemiKompensasiyaGun:N2} gün × {aktivKompensasiya.GunlukRate:N4} ₼/gün — /HR/Kompensasiya/Detal/{aktivKompensasiya.Id}",
                    Mebleg = mezKompensasiyaMebleg,
                    Tip = "gelir"
                });
            }

            // 5.1 İşdən çıxma kəsintisi — həmin ayda işdən çıxıbsa yalnız işlənmiş günlər ödənir
            decimal cixisKesintisi = 0;
            if (isci.Status == IsciStatus.IshtenCixib &&
                isci.IsdenAyrilmaTarixi.HasValue &&
                isci.IsdenAyrilmaTarixi.Value.Year == input.Il &&
                isci.IsdenAyrilmaTarixi.Value.Month == input.Ay &&
                ayIsGunu > 0)
            {
                var ayBash = new DateTime(input.Il, input.Ay, 1);
                int islenmisDays = await IsGunleriniTariheGoereSayAsync(ayBash, isci.IsdenAyrilmaTarixi.Value.Date);
                int cixisGun = Math.Max(0, ayIsGunu - islenmisDays);
                if (cixisGun > 0)
                {
                    cixisKesintisi = Math.Round(esasMaas / ayIsGunu * cixisGun, 2);
                    izahatlar.Add(new HesablamaIzahiDto
                    {
                        Addim = "Çıxış Kəsintisi",
                        Izah = $"İşdən çıxma: {isci.IsdenAyrilmaTarixi.Value:dd.MM.yyyy}. " +
                               $"İşlənmiş: {islenmisDays} iş günü / {ayIsGunu} iş günü. " +
                               $"Kəsilən: {cixisGun} gün × {esasMaas:N2}/{ayIsGunu} = {cixisKesintisi:N2} AZN",
                        Mebleg = cixisKesintisi,
                        Tip = "kesinti"
                    });
                }
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
            decimal mezuniyyetAvansBrutu = 0;     // birləşdirilmiş vergi bazası və 200 AZN güzəşt yoxlaması üçün
            decimal mezuniyyetAvansNetPaid = 0;   // artıq ödənilmiş NET (umumiTutulma-dan çıxılır)
            decimal qabaqcadanTarixcePayi = 0;    // addım 16: bu AYA düşən qabaqcadan məz. brüt payı (tarixçə üçün)

            if (mezGun > 0)
            {
                // AySonu tipli qeydlər üçün məzuniyyət ödənişi həmin ayın maaşına daxil edilir.
                // aySonuIGS    — bütün məzuniyyət günləri (PR #352, həftəsonu daxil): ödəniş üçün
                // aySonuHIGS   — yalnız real iş günü (həftəsonu/bayram çıxılır): kəsinti üçün
                var (aySonuGS, aySonuIGS, aySonuHIGS, aySonuQeydler) = await MezuniyyetAyGunleriFiltreliSayAsync(
                    input.IsciId, input.Il, input.Ay, MezuniyyetOdenisTipi.AySonuOdenis);

                if (aySonuIGS > 0 || aySonuGS > 0)
                {
                    // Dispatcher: köhnə qayda qeydləri üçün davranış dəyişməz (V2),
                    // yeni qayda (kəsim tarixindən sonra başlayan) qeydlər üçün
                    // tam-dövr MAX-ın bu aya düşən payı (ƏM md.140).
                    mezOdenis = await AySonuMezOdenisiniHesablaAsync(
                        input.IsciId, input.Il, input.Ay, aySonuGS, aySonuIGS, aySonuQeydler);
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

                // TARİXÇƏ PAYI (addım 16 üçün): qabaqcadan ödənilən məzuniyyətin brütü
                // ÖDƏNİLMƏ ayına yox, MƏZUNİYYƏT günlərinin düşdüyü ay(lar)a aiddir —
                // "qabaqcadan" elə deməkdir ki, pul adətən məzuniyyətdən ƏVVƏLKİ ayda
                // ödənilir. Bu ayın qazanc qeydinə yalnız BU AYA düşən pay yazılır
                // (AySonu rejimi ilə simmetrik: orada mezOdenis onsuz da öz ayındadır).
                // advanceQeydler yalnız bu ayla kəsişən qeydlərdir → başqa ay pay almır.
                foreach (var advMez in advanceQeydler)
                {
                    if (advMez.OdenisStatus != MezuniyyetOdenisStatus.Odenilib &&
                        advMez.OdenisStatus != MezuniyyetOdenisStatus.PlanliOdenis) continue;
                    try
                    {
                        var mhesPay = await MezuniyyetOdenisiDetalliHesablaAsync(
                            advMez.IsciId, advMez.BaslamaTarixi, advMez.BitmeTarixi);
                        var slPay = mhesPay.AySliceleri
                            .FirstOrDefault(s => s.Il == input.Il && s.Ay == input.Ay);
                        if (slPay == null || mhesPay.CemiOdenis <= 0) continue;

                        // BÖLGÜ ÜSULU — vergi bazası ilə EYNİ olmalıdır (27.08.2026):
                        //  · YENİ  → slPay.EH («cari maaş hesabı», iş günü bölgüsü).
                        //            İşlənmiş maaş + EH = işçinin tam aylıq qazancı
                        //            (Rüfət C.: avqust 761,90 + 38,10 = 800,00;
                        //             sentyabr 472,73 + 327,27 = 800,00).
                        //            Mühasib Exceli də məhz bunu yazır — «Cəmi
                        //            hesablanmış aylıq ödənişlər» sütunu 800,00
                        //            (2026 Əmək haqqı.xls, 08-2026, sətir 22).
                        //  · KÖHNƏ → slPay.Secilen (təqvim bölgüsü): 792,35 / 807,65.
                        //            Cəmi eynidir, aylıq bölgü fərqlidir.
                        //
                        // ⚠️ BU CƏDVƏL MƏZUNİYYƏT ORTALAMASININ YEGANƏ MƏNBƏYİDİR —
                        // bölgü dəyişdiyi üçün gələcək məzuniyyət hesablamaları da
                        // dəyişir. Kəsim tarixindən ƏVVƏLKİ aylar toxunulmur; keçmiş
                        // qeydləri düzəltmək lazım olsa ayrıca SQL tələb olunur.
                        // Faktiki ödənilmiş brüt saxlanılıbsa ona, yoxdursa (köhnə
                        // qeydlərdə brüt NULL) canlı hesabın cəminə miqyaslanır.
                        decimal hedefBrut = (advMez.OdenenMeblegBrut.HasValue && advMez.OdenenMeblegBrut.Value > 0)
                            ? advMez.OdenenMeblegBrut.Value
                            : mhesPay.CemiOdenis;

                        decimal pay;
                        if (AvansAylaraBolunurmu(input.Il, input.Ay))
                        {
                            // ⚠️ NORMALLAŞDIRICI ΣEH-dir, CemiOdenis DEYİL.
                            // ÜSUL B qalib gələndə ΣEH = CemiOdenis olur və pay elə
                            // EH-ə bərabər çıxır. ÜSUL A qalibdirsə CemiOdenis daha
                            // böyükdür (ΣEH ≠ CemiOdenis) — CemiOdenis-ə bölsək
                            // payların cəmi ödənilmiş brütdən AZ olar və qazanc
                            // tarixçəsi səssizcə əskik yazılar.
                            decimal ehCemi = mhesPay.AySliceleri.Sum(x => x.EH);
                            if (ehCemi <= 0) continue;
                            pay = Math.Round(hedefBrut * slPay.EH / ehCemi, 2);
                        }
                        else
                        {
                            // KÖHNƏ: təqvim bölgüsü. ΣSecilen = CemiOdenis olduğu
                            // üçün normallaşdırıcı burada CemiOdenis-dir.
                            pay = Math.Round(hedefBrut * slPay.Secilen / mhesPay.CemiOdenis, 2);
                        }
                        qabaqcadanTarixcePayi += pay;
                    }
                    catch { /* pay hesablanmasa qazanc qeydi brutMaas ilə qalır — maaşı pozma */ }
                }

                // Qabaqcadan ödənilmiş avansın brütü — vergi bazası və güzəşt yoxlaması üçün.
                //
                // İKİ MODEL VAR (27.08.2026-dan) — aşağıdakı `if` onları ayırır:
                //  · YENİ  — hər ay yalnız ÖZ payını götürür (mühasibin uçot modeli);
                //  · KÖHNƏ — bütün brüt yalnız ÖDƏNİLMƏ ayına düşür. Bu, çoxaylı
                //    məzuniyyətdə brütün hər ay TƏKRAR əlavə olunmasının qarşısını
                //    alırdı (əks halda standart 200 AZN güzəşti yanlış itirdi).
                //    Yeni modeldə həmin risk yoxdur: hər ay tam brütü yox, öz payını
                //    alır və payların cəmi brütü dəqiq bir dəfə verir.
                foreach (var advanceMez in advanceQeydler)
                {
                    if (advanceMez.OdenisStatus == MezuniyyetOdenisStatus.Odenilib)
                    {
                        // ── YENİ MODEL (27.08.2026-dan): vergi bazasına bu ayın PAYI ──
                        // Mühasib uçotu məzuniyyəti aylara bölür. İşçiyə ödənilən
                        // məbləğ DƏYİŞMİR (bax AvansAylaraBolunurmu şərhi) — yalnız
                        // bəyan olunan baza və tutulmalar dəyişir.
                        // Köhnə model aşağıda olduğu kimi qalır; geri qayıtmaq üçün
                        // appsettings → Mezuniyyet:AvansAylaraBolunmeBaslama = "2099-01-01".
                        if (AvansAylaraBolunurmu(input.Il, input.Ay))
                        {
                            var aypaylar = await MezuniyyetAvansAyPaylariAsync(
                                advanceMez.IsciId, advanceMez.BaslamaTarixi, advanceMez.BitmeTarixi,
                                advanceMez.OdenenMebleg);
                            var buAy = aypaylar.FirstOrDefault(x => x.Il == input.Il && x.Ay == input.Ay);
                            if (buAy != null && buAy.Brut > 0)
                            {
                                mezuniyyetAvansBrutu  += buAy.Brut;
                                mezuniyyetAvansNetPaid += buAy.Net;
                                izahatlar.Add(new HesablamaIzahiDto
                                {
                                    Addim = "Mezuniyyet (qabaqcadan — bu ayın payı)",
                                    Izah = $"{advanceMez.BaslamaTarixi:dd.MM.yyyy}–{advanceMez.BitmeTarixi:dd.MM.yyyy} " +
                                           $"məzuniyyətinin bu aya düşən payı: brüt {buAy.Brut:N2}, " +
                                           $"vergi {buAy.Vergi:N2}, net {buAy.Net:N2} (qabaqcadan verilib). " +
                                           "Vergi bazası aylara bölünür — ödənilmə ayına toplu salınmır.",
                                    Mebleg = buAy.Brut,
                                    Tip = "melumati"
                                });
                            }
                            continue;
                        }

                        // ── KÖHNƏ MODEL (kəsim tarixindən əvvəlki aylar) ──
                        // Bütün brüt yalnız ÖDƏNİLMƏ ayının bazasına düşür.
                        bool odenilmeBuAyda = advanceMez.OdenilmeTarixi.HasValue
                            && advanceMez.OdenilmeTarixi.Value.Year == input.Il
                            && advanceMez.OdenilmeTarixi.Value.Month == input.Ay;

                        if (!odenilmeBuAyda)
                        {
                            izahatlar.Add(new HesablamaIzahiDto
                            {
                                Addim = "Mezuniyyet (qabaqcadan — başqa ayda ödənilib)",
                                Izah = $"{advanceMez.BaslamaTarixi:dd.MM.yyyy}–{advanceMez.BitmeTarixi:dd.MM.yyyy} " +
                                       $"məzuniyyəti {advanceMez.OdenilmeTarixi:dd.MM.yyyy} tarixində ödənilib. " +
                                       "Vergi bazasına yalnız ödəniş ayında daxil edilir; bu ayda yalnız iş günü kəsintisi tətbiq olunur.",
                                Mebleg = 0,
                                Tip = "melumati"
                            });
                            continue;
                        }

                        // Brüt sahəsi varsa istifadə et, yoxdursa canlı hesabla
                        if (advanceMez.OdenenMeblegBrut.HasValue && advanceMez.OdenenMeblegBrut > 0)
                            mezuniyyetAvansBrutu += advanceMez.OdenenMeblegBrut.Value;
                        else
                        {
                            var mhes = await MezuniyyetOdenisiDetalliHesablaAsync(
                                advanceMez.IsciId, advanceMez.BaslamaTarixi, advanceMez.BitmeTarixi);
                            mezuniyyetAvansBrutu += mhes.CemiOdenis;
                        }
                        if (advanceMez.OdenenMebleg.HasValue && advanceMez.OdenenMebleg > 0)
                            mezuniyyetAvansNetPaid += advanceMez.OdenenMebleg.Value;
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

            // 6.6. Əvvəlki ay post-maaş korreksiyası
            // Əvvəlki ayın maaşı hesablandıqdan SONRA daxil edilmiş xəstəlik/məzuniyyət
            // qeydlərini cari ayın maaşında düzəldirik:
            //   a) Artıq ödənilmiş günlər — əvvəlki ay gündəlik dərəcəsi ilə kəsinti
            //   b) Həmin günlər üçün xəstəlik/məzuniyyət haqqı gəlir kimi əlavə olunur
            decimal korreksiyaKesinti = 0;
            decimal korreksiyaGelir = 0;
            string? korreksiyaAciq = null;
            List<XestelikOdenis>? korreksiyaXstOdenisler = null;

            try
            {
                var prevIl = input.Ay == 1 ? input.Il - 1 : input.Il;
                var prevAy = input.Ay == 1 ? 12 : input.Ay - 1;
                var prevAyBaslama = new DateTime(prevIl, prevAy, 1);
                var prevAyBitis = prevAyBaslama.AddMonths(1).AddDays(-1);

                var prevMaas = await _unitOfWork.Repository<Maas>()
                    .Query()
                    .Where(x => x.IsciId == input.IsciId && x.Il == prevIl && x.Ay == prevAy
                        && !x.Silinib && x.Status != MaasStatus.LegvEdildi)
                    .Include(x => x.Detallar).ThenInclude(d => d.MaasNovu)
                    .FirstOrDefaultAsync();

                if (prevMaas != null)
                {
                    int prevAyIsGunu = await AyinIsGunleriniHesablaAsync(prevIl, prevAy);
                    decimal prevEsasMaas = prevMaas.Detallar
                        .FirstOrDefault(d => !d.Silinib && d.MaasNovu?.Ad == "Əsas Əməkhaqqı")?.Mebleg ?? 0;

                    if (prevEsasMaas > 0 && prevAyIsGunu > 0)
                    {
                        decimal prevGunluk = prevEsasMaas / prevAyIsGunu;
                        var korrHisseler = new List<string>();

                        // Xəstəlik: əvvəlki aya aid, heç bir maaşa bağlanmamış XestelikOdenis qeydlər
                        korreksiyaXstOdenisler = await _unitOfWork.Repository<XestelikOdenis>()
                            .Query()
                            .Where(x => x.IsciId == input.IsciId
                                && x.Il == prevIl && x.Ay == prevAy
                                && !x.Silinib && x.MaasId == null && x.SirketGunSayi > 0)
                            .ToListAsync();

                        if (korreksiyaXstOdenisler.Any())
                        {
                            int xstGun = korreksiyaXstOdenisler.Sum(x => x.SirketGunSayi);
                            decimal xstOdenis = korreksiyaXstOdenisler.Sum(x => x.SirketOdenis);
                            decimal xstKesinti = Math.Round(prevGunluk * xstGun, 2);
                            korreksiyaKesinti += xstKesinti;
                            korreksiyaGelir += xstOdenis;
                            korrHisseler.Add(
                                $"{prevAy:D2}/{prevIl} xəstəlik: {xstGun} gün artıq ödəniş " +
                                $"kəsintisi {xstKesinti:N2} ₼ + xəstəlik haqqı {xstOdenis:N2} ₼");
                        }

                        // Məzuniyyət: əvvəlki ayın maaşı hesablandıqdan SONRA yaradılan AySonuOdenis məzuniyyətlər
                        // (məzuniyyət günü əvvəlki ayda olsa da, qeyd sonradan əlavə edildiyi üçün
                        //  o ayın maaşına düşməyib — indi korreksiya etmək lazımdır)
                        var postMezler = await _unitOfWork.Repository<Mezuniyyet>()
                            .Query()
                            .Where(x => x.IsciId == input.IsciId && !x.Silinib
                                && x.Status == MezuniyyetStatus.Tesdiqlenib
                                && x.OdenisTipi == MezuniyyetOdenisTipi.AySonuOdenis
                                // Korreksiya qeydləri mövcud Illik məzuniyyətin üzərindədir;
                                // ayrıca ödəniş tələb etmir, post-korreksiyaya daxil edilməməlidir.
                                && x.Nov != MezuniyyetNovu.DovletVezifelerininIcrasi
                                && x.Nov != MezuniyyetNovu.OzHesabina   // ödənişsiz — məzuniyyət haqqı yoxdur
                                && (x.IsGunlerininSayiManual ?? x.IsGunlerininSayi) > 0
                                && x.YaradilmaTarixi > prevMaas.HesablanmaTarixi
                                && x.BaslamaTarixi <= prevAyBitis
                                && x.BitmeTarixi >= prevAyBaslama)
                            .ToListAsync();

                        foreach (var mez in postMezler)
                        {
                            // Yalnız əvvəlki aya düşən hissəni götür
                            var mezPrevBaslama = mez.BaslamaTarixi < prevAyBaslama ? prevAyBaslama : mez.BaslamaTarixi;
                            var mezPrevBitis = mez.BitmeTarixi < prevAyBitis ? mez.BitmeTarixi : prevAyBitis;
                            int mezIsGun = await IsGunleriniTariheGoereSayAsync(mezPrevBaslama, mezPrevBitis);
                            if (mezIsGun <= 0) continue;

                            decimal mezKes = Math.Round(prevGunluk * mezIsGun, 2);
                            korreksiyaKesinti += mezKes;

                            // YENİ qaydada MAX tam dövr üzrə hesablanır — kəsilmiş dövrlə çağırmaq
                            // yanlış olar; tam dövr hesablanıb əvvəlki ayın PAYI götürülür.
                            decimal mezOd;
                            if (YeniQaydaTetbiqOlunur(mez.BaslamaTarixi))
                            {
                                var mezHesabTam = await MezuniyyetOdenisiDetalliHesablaAsync(
                                    mez.IsciId, mez.BaslamaTarixi, mez.BitmeTarixi);
                                mezOd = mezHesabTam.AySliceleri
                                    .FirstOrDefault(s => s.Il == prevIl && s.Ay == prevAy)?.Secilen ?? 0m;
                            }
                            else
                            {
                                var mezHesab = await MezuniyyetOdenisiDetalliHesablaAsync(
                                    mez.IsciId, mezPrevBaslama, mezPrevBitis);
                                mezOd = mezHesab.CemiOdenis;
                            }
                            korreksiyaGelir += mezOd;

                            korrHisseler.Add(
                                $"{prevAy:D2}/{prevIl} məzuniyyət " +
                                $"({mezPrevBaslama:dd.MM.yyyy}–{mezPrevBitis:dd.MM.yyyy}): " +
                                $"{mezIsGun} gün kəsinti {mezKes:N2} ₼ + məzuniyyət haqqı {mezOd:N2} ₼");
                        }

                        if (korrHisseler.Any())
                        {
                            korreksiyaAciq = string.Join("; ", korrHisseler);
                            izahatlar.Add(new HesablamaIzahiDto
                            {
                                Addim = "Əvvəlki Ay Korreksiyası",
                                Izah = korreksiyaAciq,
                                Mebleg = Math.Abs(korreksiyaGelir - korreksiyaKesinti),
                                Tip = korreksiyaGelir >= korreksiyaKesinti ? "gelir" : "kesinti"
                            });
                        }
                    }
                }
            }
            catch { /* koreksiya xətası əsas hesablamanı pozmasın */ }

            // 7. Bonus
            if (input.BonusMeblegi > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Bonus / Mukafat",
                    Izah = input.BonusAciqlama ?? "El ile daxil edilib",
                    Mebleg = input.BonusMeblegi,
                    Tip = "gelir"
                });

            // 7.0 Overtime — aylıq əlavə qeydindən (bonus kimi gəlir, vergiyə cəlb olunur)
            if (overtimeMebleg > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "Overtime",
                    Izah = "Aylıq əlavə qeydindən",
                    Mebleg = overtimeMebleg,
                    Tip = "gelir"
                });

            // 7.1 IH-07 əlavə təminat (18.02.2016 tarixli əmr) — vergiyə cəlb olunur, brüt-ə əlavə edilir
            if (input.IH07Meblegi > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "IH-07 Əlavə Təminat",
                    Izah = "18.02.2016 tarixli IH-07 saylı əmrlə əlavə təminat",
                    Mebleg = input.IH07Meblegi,
                    Tip = "gelir"
                });

            // 7.2 VM 98.2.1 — vergiyə cəlb olunan gəlirlər
            if (input.VM9821Meblegi > 0)
                izahatlar.Add(new HesablamaIzahiDto
                {
                    Addim = "VM 98.2.1 Gəlirləri",
                    Izah = "VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan hesabi gəlir — " +
                           "4 vergi/ayırma bazasına ƏLAVƏ OLUNUR, brüt/net-ə DAXİL DEYİL (nağd ödənmir; tutulmalar artır)",
                    Mebleg = input.VM9821Meblegi,
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

            // 8.7. Konfiqurasiyalı manual gəlir növləri — işçi üzrə daxil edilən məbləğlər.
            //      Hər növün vergi rejimi öz bayraqlarından gəlir. elave=0 olduqda hesablama
            //      əvvəlki kimi qalır (reqressiya təhlükəsiz).
            var elaveGirisler = (input.ElaveGelirler ?? new List<ElaveGelirGirisi>())
                .Where(e => e.NovId > 0 && e.Mebleg != 0).ToList();
            decimal elaveCemi = 0m, elaveVergiTaxable = 0m, elaveDsmf = 0m,
                    elaveIssizlik = 0m, elaveItss = 0m, elaveGuzest = 0m;
            // Qərar 137: birdəfəlik ödənişlər məzuniyyət üçün 12-aylıq orta qazanca DAXİL DEYİL.
            // Növdə MezuniyyetOrtalamasinaDaxil=false işarələnmiş məbləğlər burada yığılır və
            // ayın qazanc qeydinə (IsciAyliqQazanc) yazılarkən brütdan çıxılır (aşağıda, addım 16).
            decimal mezOrtalamaXaric = 0m;
            var elaveDetallar = new List<(string Ad, decimal Mebleg)>();
            if (elaveGirisler.Count > 0)
            {
                var elaveNovIdler = elaveGirisler.Select(e => e.NovId).Distinct().ToList();
                var elaveNovler = await _unitOfWork.Repository<MaasNovu>()
                    .HamisiniGetirAsync(x => x.Aktivdir && !x.Silinib && x.ManualGelir && elaveNovIdler.Contains(x.Id));
                foreach (var g in elaveGirisler)
                {
                    var nov = elaveNovler.FirstOrDefault(n => n.Id == g.NovId);
                    if (nov == null) continue;          // növ tapılmadı/deaktiv — keç
                    decimal m = g.Mebleg;
                    elaveCemi += m;
                    if (nov.VergiyeCelb)        elaveVergiTaxable += Math.Max(0m, m - nov.GelirVergisiAzadMebleg);
                    if (nov.DsmfyeCelb)         elaveDsmf         += m;
                    if (nov.IssizliyeCelb)      elaveIssizlik     += m;
                    if (nov.ItsseCelb)          elaveItss         += m;
                    if (nov.GuzestHeddineDaxil) elaveGuzest       += m;
                    if (!nov.MezuniyyetOrtalamasinaDaxil) mezOrtalamaXaric += m;
                    elaveDetallar.Add((nov.Ad, m));
                }
            }

            // 9. BRUT = əsas maaş ± düzəlişlər + işəgötürən HYS payı + konfiqurasiyalı gəlirlər
            decimal esasBrut = esasMaas
                - cixisKesintisi
                - mezKesinti
                + mezOdenis
                + xestelikSirketOdenis
                - xestelikKesinti
                - qayibKesinti
                - odenissizKesinti
                + input.BonusMeblegi
                + overtimeMebleg
                + input.IH07Meblegi
                // VM 98.2.1 gəlirləri BRÜT-ə DAXİL EDİLMİR (gəlir kimi maaşı ARTIRMIR).
                // Bu, VM 98.2.1-ə əsasən vergiyə cəlb olunan hesabi (imputed) gəlirdir:
                // yalnız 4 vergi/ayırma bazasına və güzəşt həddinə əlavə olunur (aşağıda),
                // beləliklə tutulmalar artır, amma işçiyə əlavə pul (brüt/net) ödənmir.
                - input.CerimeMeblegi
                + korreksiyaGelir
                + mezKompensasiyaMebleg
                - korreksiyaKesinti
                + elaveCemi;

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
            // esasBrut konfiqurasiyalı gəlirlərin TAM məbləğini ehtiva edir; hər baza üçün
            // həmin gəlirlərin yalnız o haqqa cəlb olunan hissəsi qalmalıdır → tam elaveCemi
            // çıxılır, müvafiq hissə geri əlavə olunur. elave=0-da bazalar əvvəlki kimi qalır.
            // İşsizlik və İTSS ayrı bazalara bölünür (konfiqurasiyalı gəlir biri üçün cəlb,
            // digəri üçün azad ola bilər); elave=0-da hər ikisi köhnə itssBazasi-na bərabərdir.
            // VM 98.2.1 gəlirləri (input.VM9821Meblegi) BÜTÜN 4 bazaya əlavə olunur — brüt-ə YOX.
            // Bu bazalar həm İŞÇİ, həm İŞƏGÖTÜRƏN paylarını qidalandırır (aşağıda dsmfIsegoturen
            // və s. eyni dəyişənləri istifadə edir) → VM 98.2.1 hər iki tərəfə simmetrik təsir edir.
            decimal vergiBazasi    = Math.Max(0, esasBrut + mezuniyyetAvansBrutu - hysMebleg - elaveCemi + elaveVergiTaxable + input.VM9821Meblegi);
            decimal dsmfBazasi     = Math.Max(0, esasBrut + mezuniyyetAvansBrutu - hysMebleg - xestelikSirketOdenis - elaveCemi + elaveDsmf + input.VM9821Meblegi);
            decimal issizlikBazasi = Math.Max(0, esasBrut + mezuniyyetAvansBrutu + hysIsegoturen - xestelikSirketOdenis - elaveCemi + elaveIssizlik + input.VM9821Meblegi);
            decimal itssBazasi     = Math.Max(0, esasBrut + mezuniyyetAvansBrutu + hysIsegoturen - xestelikSirketOdenis - elaveCemi + elaveItss + input.VM9821Meblegi);

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
            // Standart güzəşt: GROSS (maaş + işv.HYS + məzuniyyət avansı brütü) ≤ 2500 olmalıdır.
            // Məzuniyyət avansı ayrıca ödənilsə də, həmin ayın ümumi gəliri sayılır → threshold-a daxildir.
            // brutMaas elaveCemi-ni tam ehtiva edir; 2500 güzəşt həddinə yalnız
            // GuzestHeddineDaxil=true olan konfiqurasiyalı gəlirlər sayılmalıdır.
            // VM 98.2.1 gəlirləri vergiyə cəlb olunan gəlirdir → 200 AZN güzəşt həddinə (2500)
            // sayılır (brüt-ə daxil olmasa da), əks halda güzəşt yanlış tətbiq oluna bilər.
            decimal brutMaasGuzestYoxlama = brutMaas + mezuniyyetAvansBrutu - (elaveCemi - elaveGuzest) + input.VM9821Meblegi;
            decimal standartGuzest = brutMaasGuzestYoxlama <= firstBracketMax ? p.VergiGuzestiMeblegi : 0m;

            decimal vergilenecek = Math.Max(0, vergiBazasi - standartGuzest - maxIsciGuzesti);

            var vergiIzahHisseleri = new List<string> { $"Brüt: {brutMaas:N2}" };
            if (mezuniyyetAvansBrutu > 0)
                vergiIzahHisseleri.Add($"+ Məzuniyyət avansı brütü: {mezuniyyetAvansBrutu:N2} → Cəm: {brutMaasGuzestYoxlama:N2} (güzəşt yoxlaması üçün)");
            if (hysMebleg > 0)
                vergiIzahHisseleri.Add($"− HYS: {hysMebleg:N2} → Vergi bazası: {vergiBazasi:N2}");
            if (standartGuzest > 0)
                vergiIzahHisseleri.Add($"− Standart güzəşt: {standartGuzest:N2} (cəm brüt ≤ {firstBracketMax:N0})");
            else
                vergiIzahHisseleri.Add(
                    $"Cəm brüt {brutMaasGuzestYoxlama:N2} > {firstBracketMax:N0} — standart {p.VergiGuzestiMeblegi:N2} ₼ güzəşti tətbiq olunmur");
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
            decimal issizlikIsci   = Math.Round(issizlikBazasi * (p.IssizlikSigortasiFaizi / 100m), 2);
            decimal itss           = HesablaTutulma(itssBazasi,   MaasParametrNovu.IcbariTibbiSigortaFaizi, p.IcbariTibbiSigortaFaizi, out var itssIzah);

            string dsmfAzadIzahi()
            {
                var hisseler = new List<string>();
                if (hysMebleg > 0) hisseler.Add("HYS çıxılıb");
                if (xestelikSirketOdenis > 0) hisseler.Add("xəstəlik çıxılıb");
                return hisseler.Count > 0 ? $" ({string.Join(", ", hisseler)})" : "";
            }

            izahatlar.Add(new HesablamaIzahiDto { Addim = "Gelir Vergisi",              Izah = $"{gvIzah} (guzest: {p.VergiGuzestiMeblegi:N2} AZN)",                                Mebleg = gelirVergisi, Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "DSMF (Isci)",                Izah = $"{dsmfIzah}{dsmfAzadIzahi()}",                                                   Mebleg = dsmfIsci,     Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "Issizlik Sigortas (Isci)",   Izah = $"{itssBazasi:N2} x {p.IssizlikSigortasiFaizi}%{(xestelikSirketOdenis > 0 ? " (xəstəlik çıxılıb)" : "")}", Mebleg = issizlikIsci, Tip = "vergi" });
            izahatlar.Add(new HesablamaIzahiDto { Addim = "ITSS (Isci)",                Izah = $"{itssIzah}{(xestelikSirketOdenis > 0 ? " (xəstəlik çıxılıb)" : "")}",            Mebleg = itss,         Tip = "vergi" });

            // 11. NET maas — HYS də NET-dən tutulur (çünki işçinin öz payıdır)
            // HYS: brüt-ə hysIsegoturen daxildir, deməli NET-dən çıxılmalıdır.
            //
            // Məzuniyyət avansı (QabaqcadanOdenis) varsa: vergi cəmi (cTaxes) birləşmiş
            // brüt üzərindən hesablanır (bax 9.0.1 bazaları). Avansın vergi payı artıq
            // həmin ödəniş zamanı tutulub (mavBrut − mavNetPaid), ona görə cəmi
            // tutulmadan çıxılır — beləliklə yalnız maaş hissəsinə düşən vergi qalır.
            decimal mavImpliedTax = mezuniyyetAvansBrutu > 0 && mezuniyyetAvansNetPaid > 0
                ? Math.Max(0, mezuniyyetAvansBrutu - mezuniyyetAvansNetPaid)
                : 0m;
            decimal umumiTutulma = gelirVergisi + dsmfIsci + issizlikIsci + itss + hysMebleg + hysIsegoturen + avansMebleg - mavImpliedTax;
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
            decimal issizlikIsegoturen = Math.Round(issizlikBazasi * (p.IssizlikIsegotürenFaizi / 100m), 2);
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
            decimal umumiDavamKesinti = qayibKesinti + mezKesinti + xestelikKesinti + odenissizKesinti;
            var davamHisseleri = new List<string>();
            if (qayibGun > 0) davamHisseleri.Add($"{qayibGun} qayıb");
            if (mezGun > 0) davamHisseleri.Add($"{mezGun} məz.");
            if (xestelikUmumiGun > 0) davamHisseleri.Add($"{xestelikUmumiGun} xəstəlik");
            if (odenissizGun > 0) davamHisseleri.Add($"{odenissizGun} ödənişsiz");
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
                DetayEkle("Overtime",                          MaasDetayTipi.Gelir,           overtimeMebleg,     "Aylıq əlavə qeydindən"),
                DetayEkle("IH-07 Əlavə Təminat",               MaasDetayTipi.Gelir,           input.IH07Meblegi,        "18.02.2016 tarixli IH-07 saylı əmrlə əlavə təminat"),
                DetayEkle("VM 98.2.1 Gəlirləri",              MaasDetayTipi.Gelir,           input.VM9821Meblegi,      "VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan gəlirlər"),
                DetayEkle("Məzuniyyət Kompensasiyası",         MaasDetayTipi.Gelir,           mezKompensasiyaMebleg, aktivKompensasiya != null ? $"{aktivKompensasiya.CemiKompensasiyaGun:N2} gün × {aktivKompensasiya.GunlukRate:N4} ₼/gün (istifadə edilməmiş məzuniyyət)" : null),
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
                // Əvvəlki ay korreksiyası
                DetayEkle("Əvvəlki Ay Artıq Ödəniş Kəsintisi", MaasDetayTipi.Tutulma,         korreksiyaKesinti, korreksiyaAciq),
                DetayEkle("Əvvəlki Ay Kompensasiyası",          MaasDetayTipi.Gelir,           korreksiyaGelir,   korreksiyaAciq),
            };

            // Konfiqurasiyalı manual gəlirləri də detal kimi əlavə et (breakdown + provodka üçün).
            foreach (var (elaveAd, elaveMbl) in elaveDetallar)
            {
                var elaveRes = DetayEkle(elaveAd, MaasDetayTipi.Gelir, elaveMbl);
                if (!elaveRes.Success)
                    return Result<MaasHesablaNeticesiDto>.Fail(elaveRes.Message);
            }

            var ilkXeta = xetalar.FirstOrDefault(x => !x.Success);
            if (ilkXeta != null)
                return Result<MaasHesablaNeticesiDto>.Fail(ilkXeta.Message);

            // Addım-addım izahları JSON kimi saxla — Detal səhifəsində
            // mühasib yekun rəqəmin hardan gəldiyini oxuya bilsin deyə.
            maas.HesablamaIzahi = JsonSerializer.Serialize(izahatlar);

            // ─── DRY-RUN qapısı ──────────────────────────────────────────────
            // Bütün hesablama yuxarıda BİTDİ (brutMaas, netMaas, vergilər, izahlar).
            // Aşağıdakı blok YALNIZ persistensiyadır (Maas yaz, XestelikOdenis bağla,
            // kompensasiya işarələ, qazanc tarixçəsi). saxla=false (önizləmə) olduqda
            // bu blok atlanır → eyni rəqəmlər qaytarılır, amma heç nə bazaya yazılmır.
            // Beləliklə: önizlədə görünən rəqəm = save zamanı yazılan rəqəm.
            if (saxla)
            {
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

                // Əvvəlki ay korreksiyasına daxil edilmiş XestelikOdenis qeydlərini də bağla
                if (korreksiyaXstOdenisler?.Any() == true)
                {
                    foreach (var od in korreksiyaXstOdenisler)
                        od.MaasId = maas.Id;
                }

                await _unitOfWork.YaddaSaxlaAsync();
            }
            catch { }

            // 15b. Məzuniyyət kompensasiyasını bu Maas-a bağla — status MaasaDaxilEdildi,
            //      MaasId set olunur (IsareLamasiniYadda məntiqi, dövrəvi DI olmadan).
            if (aktivKompensasiya != null)
            {
                try
                {
                    var komp = await _unitOfWork.Repository<MezuniyyetKompensasiyasi>()
                        .Query().FirstOrDefaultAsync(x => x.Id == aktivKompensasiya.Id);
                    if (komp != null)
                    {
                        komp.Status = KompensasiyaStatus.MaasaDaxilEdildi;
                        komp.MaasId = maas.Id;
                        komp.YenilenmeTarixi = DateTime.Now;
                        await _unitOfWork.Repository<MezuniyyetKompensasiyasi>().YenileAsync(komp);
                        await _unitOfWork.YaddaSaxlaAsync();
                    }
                }
                catch { /* işarələmə xətası maaşı pozmasın */ }
            }

            // 16. Aylıq qazanc tarixçəsinə avtomatik əlavə (sliding window 12 ay)
            // Məzuniyyət bazası = BrutMaaş PLUS bu aya düşən qabaqcadan məzuniyyət brüt
            // payı MINUS orta qazanca daxil olmayan birdəfəlik ödənişlər (Qərar 137).
            // QabaqcadanOdenis məzuniyyətin pulu ayrıca ödənildiyi üçün brutMaas-a düşmür,
            // amma ayın REAL qazancının hissəsidir — daxil edilməsə, məzuniyyətli ay
            // tarixçədə süni aşağı görünür və gələcək məzuniyyət ortalamasını salır
            // (real hadisə: İyul 2026 — 1.321,30 yazılmışdı, düzü 2.798,55).
            // DİQQƏT: pay ÖDƏNİLMƏ ayına yox, MƏZUNİYYƏT günlərinin ayına yazılır
            // (qabaqcadanTarixcePayi — yalnız bu ayla kəsişən qeydlərin bu ay payı);
            // vergi bazası isə qanuna uyğun ödənilmə ayında qalır (dəyişməyib).
            // XƏSTƏLİK şirkət ödənişi brutMaas-a daxildir, amma məzuniyyət ortalamasına
            // DAXİL DEYİL (IsciAyliqQazanc.Qazanc sənədi: "xəstəlik ödənişi artıq
            // çıxılmış olmalıdır") — çıxılır (real hadisə: 2026-04, xəstəlik pulu 93,21
            // qazanca düşmüşdü). IH-07, VM 98.2.1 əlavə təminatlardır — çıxılmır.
            try
            {
                decimal qazanc = brutMaas + qabaqcadanTarixcePayi
                                 - mezOrtalamaXaric - xestelikSirketOdenis;
                if (qazanc < 0) qazanc = 0;

                // Xəstəlik ödənişinin yeni DSMF-əsaslı düsturu üçün DSMF məbləğləri də saxlanılır.
                await _ayliqQazancService.AutoInsertFromMaasAsync(
                    input.IsciId, input.Il, input.Ay, qazanc,
                    dsmfIsci: dsmfIsci,
                    dsmfIsegoturen: dsmfIsegoturen);
            }
            catch { /* avtomatik sync xətası əsas əməliyyatı pozmasın */ }
            } // if (saxla) — DRY-RUN qapısının sonu

            // 16. Netice DTO  (saxla=false olduqda MaasId = 0 → yazılmadığını bildirir)
            var teyinat = isci.IsciTeyinatlari.FirstOrDefault();

            // Provodka (əməliyyat yazılışı) önizləməsi üçün detal parçalanması:
            // MaasNovu id → ad. maas.Detallar bütün id-ləri novler-dən götürüb.
            var novAdMap = novler.ToDictionary(n => n.Id, n => n.Ad);
            var detalSetirleri = maas.Detallar
                .Select(d => new MaasDetaySetiriDto
                {
                    Ad = novAdMap.TryGetValue(d.MaasNovuId, out var ad) ? ad : "",
                    Mebleg = d.Mebleg
                })
                .ToList();

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
                OvertimeMeblegi = overtimeMebleg,
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
                Izahatlar = izahatlar,
                BankHesabNo = isci.Maliye?.BankHesabNo,
                Detallar = detalSetirleri
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
                    x.MaasdanKes &&
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

        // Ödənişsiz (öz hesabına) məzuniyyətin bu aydakı REAL İŞ GÜNLƏRİ sayı — Ə.M. 129.
        // Yalnız baza maaş KƏSİNTİSİ üçün (məzuniyyət haqqı ÖDƏNMİR). Həftəsonu və
        // hesablanmayan bayramlar çıxılır — məzuniyyət kəsintisi (HaqiqiIsGunu) ilə eyni məntiq.
        // Üst-üstə düşən qeydlərdə gün ikiqat sayılmasın deyə HashSet istifadə olunur.
        public async Task<int> OzHesabinaIsGunuSayAsync(int isciId, int il, int ay)
        {
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

            var qeydler = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib
                         && x.Status == MezuniyyetStatus.Tesdiqlenib
                         && x.Nov == MezuniyyetNovu.OzHesabina
                         && (x.IsGunlerininSayiManual ?? x.IsGunlerininSayi) > 0
                         && x.BaslamaTarixi <= ayBitis
                         && x.BitmeTarixi >= ayBaslangic)
                .ToListAsync();

            if (!qeydler.Any()) return 0;

            var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= ayBaslangic && x.Tarix <= ayBitis && !x.Silinib);
            var ozelTipDict = ozelGunler
                .GroupBy(x => x.Tarix.Date)
                .ToDictionary(g => g.Key, g => g.First().Tip);

            var isGunleri = new HashSet<DateTime>();
            foreach (var m in qeydler)
            {
                var baslama = m.BaslamaTarixi < ayBaslangic ? ayBaslangic : m.BaslamaTarixi;
                var bitis = m.BitmeTarixi > ayBitis ? ayBitis : m.BitmeTarixi;
                for (var t = baslama.Date; t <= bitis.Date; t = t.AddDays(1))
                {
                    bool realIsGunu;
                    if (ozelTipDict.TryGetValue(t.Date, out var tip))
                        realIsGunu = tip == GunTipi.IsGunu;
                    else
                        realIsGunu = t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;
                    if (realIsGunu) isGunleri.Add(t.Date);
                }
            }
            return isGunleri.Count;
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
                    // DovletVezifelerininIcrasi qeydləri həmişə mövcud Illik məzuniyyətin
                    // üzərindəki korreksiya kimi yaranır — həmin günlər artıq Illik qeyd
                    // tərəfindən sayılır, ikiqat saymamaq üçün burada çıxarılır.
                    x.Nov != MezuniyyetNovu.DovletVezifelerininIcrasi &&
                    // Öz hesabına (ödənişsiz) məzuniyyət — məzuniyyət haqqı ÖDƏNMİR;
                    // baza kəsintisi ayrıca (OzHesabinaIsGunuSayAsync) aparılır.
                    x.Nov != MezuniyyetNovu.OzHesabina &&
                    !x.JetonIleOdendi &&
                    (x.IsGunlerininSayiManual ?? x.IsGunlerininSayi) > 0 &&
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

        // ─────────────────────────────────────────────────────────
        // AY SONU MƏZUNİYYƏT ÖDƏNİŞİ — KÖHNƏ/YENİ QAYDA DISPATCHER-İ
        //
        // Bütün qeydlər köhnə qaydadadırsa → köhnə yol OLDUĞU KİMİ (aqreqat
        // saylarla V2 — yuvarlaqlama daxil bugünkü davranış dəyişmir).
        // Yeni qayda qeydləri üçün: tam-dövr MAX (DetalliHesabla) hesablanır və
        // BU AYA düşən təqvim-mütənasib pay (slice.Secilen) götürülür — ƏM md.140.
        // Qarışıq halda köhnə qeydlərin günləri ayrıca sayılıb köhnə düsturla gedir.
        // ─────────────────────────────────────────────────────────
        private async Task<decimal> AySonuMezOdenisiniHesablaAsync(
            int isciId, int il, int ay, int cemGS, int cemIGS, List<Mezuniyyet> qeydler)
        {
            var hamisi = qeydler ?? new List<Mezuniyyet>();
            var yeniler = hamisi.Where(q => YeniQaydaTetbiqOlunur(q.BaslamaTarixi)).ToList();

            if (yeniler.Count == 0)
                return await MezuniyyetOdenisiniHesablaV2Async(isciId, il, ay, cemGS, cemIGS);

            decimal cem = 0m;
            foreach (var q in yeniler)
            {
                var hesab = await MezuniyyetOdenisiDetalliHesablaAsync(
                    isciId, q.BaslamaTarixi, q.BitmeTarixi);
                cem += hesab.AySliceleri
                    .FirstOrDefault(s => s.Il == il && s.Ay == ay)?.Secilen ?? 0m;
            }

            var kohneler = hamisi.Where(q => !YeniQaydaTetbiqOlunur(q.BaslamaTarixi)).ToList();
            if (kohneler.Count > 0)
            {
                var (kGS, kIGS) = await QeydSubsetAyGunSayAsync(kohneler, il, ay);
                if (kGS > 0 || kIGS > 0)
                    cem += await MezuniyyetOdenisiniHesablaV2Async(isciId, il, ay, kGS, kIGS);
            }
            return Math.Round(cem, 2);
        }

        // Verilən məzuniyyət qeydləri ALT-ÇOXLUĞUNUN bu aya düşən günlərini sayır.
        // FiltreliSay ilə eyni qayda: GS — bütün günlər; IGS — hesablanmayan bayramlar
        // çıxılmaqla (həftəsonu daxil). Üst-üstə düşmə HashSet ilə ikiqat sayılmır.
        private async Task<(int GS, int IGS)> QeydSubsetAyGunSayAsync(
            List<Mezuniyyet> qeydler, int il, int ay)
        {
            if (qeydler == null || qeydler.Count == 0) return (0, 0);

            var ayBas = new DateTime(il, ay, 1);
            var ayBit = ayBas.AddMonths(1).AddDays(-1);

            var ozel = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= ayBas && x.Tarix <= ayBit && !x.Silinib);
            var skip = ozel
                .Where(x => x.Tip == GunTipi.Bayram && !x.MezuniyyetdeHesablanir)
                .Select(x => x.Tarix.Date)
                .ToHashSet();

            var gunler = new HashSet<DateTime>();
            foreach (var m in qeydler)
            {
                var b = m.BaslamaTarixi < ayBas ? ayBas : m.BaslamaTarixi;
                var e = m.BitmeTarixi > ayBit ? ayBit : m.BitmeTarixi;
                for (var t = b.Date; t <= e.Date; t = t.AddDays(1)) gunler.Add(t);
            }

            return (gunler.Count, gunler.Count(t => !skip.Contains(t)));
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
            decimal S = await Son12AyDuzelmisCeminiHesablaAsync(isciId, cariMaas, il, ay);

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
        private async Task<decimal> Son12AyDuzelmisCeminiHesablaAsync(int isciId, decimal cariMaas, int il, int ay)
        {
            // Seçilmiş aydan əvvəlki 12 ay götürülür — cari tarix yox, seçilmiş ay əsas götürülür
            int refKey = il * 12 + ay;
            var son12 = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib && (x.Il * 12 + x.Ay) < refKey)
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

            // HYS bazaları: vergi+DSMF = brut − HYS, İTSS/İşsizlik = brut + işəgötürən HYS
            // (FerdiHesablaAsync ilə eyni məntiq — hysIsv işçi gəlirinə daxildir)
            decimal vergiDsmfBazasi = Math.Max(0, brut - hysMebleg);
            decimal itssBazasi = brut + hysIsv;

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

            // Son 12 ayın qazancları — məzuniyyət başlama ayından əvvəlki 12 ay
            int baslamaRefKey = baslama.Year * 12 + baslama.Month;
            var son12Qazanc = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib && (x.Il * 12 + x.Ay) < baslamaRefKey)
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

            // ── QAYDA SEÇİMİ: kəsim tarixindən sonra başlayan məzuniyyətlərə YENİ qayda
            // (ƏM md.140 — iki üsulun TAM CƏMLƏRİ müqayisə olunur), köhnələrə KÖHNƏ düstur.
            bool yeniQayda = YeniQaydaTetbiqOlunur(baslama);
            result.YeniQayda = yeniQayda;

            if (yeniQayda)
                result.IzahatAddimlari.Add(
                    $"YENİ QAYDA (başlama ≥ {YeniQaydaBaslamaTarixi():dd.MM.yyyy}): iki üsul TAM DÖVR üzrə " +
                    "ayrıca hesablanır — ÜSUL A: gündəlik orta (düzəlmiş cəm ÷ 12 ÷ 30.4) × məzuniyyətin " +
                    "TƏQVİM günü; ÜSUL B: hər ay üçün (cari maaş ÷ ayın iş günü) × həmin aya düşən FAKTİKİ " +
                    "İŞ günü, cəmlənir. Sonda BİR DƏFƏ böyük olan ödənilir: MAX(A, B).");
            else
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
            decimal bCemiYeni = 0;   // YENİ qayda: Üsul B-nin (cari maaş, İŞ günü) aylıq cəmi

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

                decimal gunlukMezPul = (S > 0) ? S / 12m / 30.4m : 0m;
                decimal gunlukMaas = (cariMaas > 0 && ayIsGun > 0) ? cariMaas / ayIsGun : 0m;

                decimal MH, EH, secilen;
                string qalib;
                if (yeniQayda)
                {
                    // YENİ QAYDA — bu mərhələdə yalnız komponentlər yığılır, MAX SONDA:
                    //   MH (Üsul A payı, informativ) = gündəlik orta × TƏQVİM günü (igs)
                    //   EH (Üsul B komponenti)       = gündəlik maaş × FAKTİKİ İŞ günü (haqiqiIs)
                    // secilen hələ 0-dır — dövr üzrə MAX tapılandan sonra ayın
                    // təqvim-mütənasib payı ilə doldurulur (aşağıda).
                    MH = (igs > 0) ? Math.Round(gunlukMezPul * igs, 2) : 0m;
                    EH = (haqiqiIs > 0) ? Math.Round(gunlukMaas * haqiqiIs, 2) : 0m;
                    bCemiYeni += EH;
                    secilen = 0m;
                    qalib = "";
                }
                else
                {
                    // KÖHNƏ QAYDA — DƏYİŞMƏZ: gündəlik dərəcələrin böyüyü İGS-ə vurulur.
                    // Bu, ödənişlə kəsintini eyni bazada saxlayır (hər ikisi İGS əsaslı).
                    decimal gunlukDerece = Math.Max(gunlukMezPul, gunlukMaas);
                    MH = (igs > 0) ? Math.Round(gunlukMezPul * igs, 2) : 0m;
                    EH = (igs > 0) ? Math.Round(gunlukMaas * igs, 2) : 0m;
                    secilen = (igs > 0) ? Math.Round(gunlukDerece * igs, 2) : 0m;
                    qalib = gunlukMezPul >= gunlukMaas ? "MH" : "ƏH";
                }

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
                if (yeniQayda)
                {
                    result.IzahatAddimlari.Add(
                        $"    Bu ayın məzuniyyəti: {gs} təqvim günü ({igs} ödənilən), {haqiqiIs} faktiki iş günü " +
                        $"(ayın ümumi iş günü sayı: {ayIsGun})");
                    result.IzahatAddimlari.Add(
                        $"    ÜSUL A payı (informativ): {gunlukMezPul:N4} ₼/gün × {igs} təqvim günü = {MH:N2} ₼");
                    result.IzahatAddimlari.Add(
                        $"    ÜSUL B komponenti: {cariMaas:N2} ÷ {ayIsGun} = {gunlukMaas:N4} ₼/gün " +
                        $"× {haqiqiIs} İŞ günü = {EH:N2} ₼");
                }
                else
                {
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
                }

                umumiGS += gs;
                umumiIGS += igs;
                cemi += secilen;

                cursorAy = cursorAy.AddMonths(1);
            }

            // ── YENİ QAYDA YEKUNU: tam-dövr MAX + ayların təqvim-mütənasib payı ──
            if (yeniQayda)
            {
                decimal gunlukOrta = (S > 0) ? S / 12m / 30.4m : 0m;
                decimal aCemi = (umumiIGS > 0) ? Math.Round(gunlukOrta * umumiIGS, 2) : 0m;
                decimal bCemi = Math.Round(bCemiYeni, 2);

                cemi = Math.Max(aCemi, bCemi);
                string qalibUsul = aCemi >= bCemi ? "A" : "B";

                result.ACemi = aCemi;
                result.BCemi = bCemi;
                result.QalibUsul = qalibUsul;

                // Aya bölgü (ƏM md.140): ödəniş hər ayın ödənilən TƏQVİM gününə mütənasib
                // bölünür və həmin ayın gəliri sayılır. Yuvarlaqlama itkisi olmasın deyə
                // son ayın payı = cəmi − əvvəlki payların cəmi.
                decimal bolunmus = 0m;
                for (int i = 0; i < result.AySliceleri.Count; i++)
                {
                    var sl = result.AySliceleri[i];
                    decimal pay;
                    if (umumiIGS <= 0)
                        pay = 0m;
                    else if (i == result.AySliceleri.Count - 1)
                        pay = Math.Round(cemi - bolunmus, 2);
                    else
                        pay = Math.Round(cemi * sl.IsGun / umumiIGS, 2);
                    bolunmus += pay;
                    sl.Secilen = pay;
                    sl.Qalib = qalibUsul == "A" ? "MH" : "ƏH";
                }

                result.IzahatAddimlari.Add(
                    $"ÜSUL A (orta əməkhaqqı): {gunlukOrta:N4} ₼/gün × {umumiIGS} təqvim günü = {aCemi:N2} ₼");
                result.IzahatAddimlari.Add(
                    $"ÜSUL B (cari maaş, iş günü üzrə aylıq cəm): {bCemi:N2} ₼");
                result.IzahatAddimlari.Add(
                    $"MÜQAYİSƏ (ƏM md.140, işçinin xeyrinə): MAX({aCemi:N2}; {bCemi:N2}) = {cemi:N2} ₼ " +
                    $"— ÜSUL {qalibUsul} ({(qalibUsul == "A" ? "orta əməkhaqqı" : "cari maaş")}) tətbiq olunur");
                foreach (var sl in result.AySliceleri)
                    result.IzahatAddimlari.Add(
                        $"    {sl.AyAdi}: ayın gəliri kimi {sl.Secilen:N2} ₼ " +
                        $"({sl.IsGun} təqvim günü / {umumiIGS}) — icbari ödənişlər bu ay üzrə tutulur");
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
            var (aySonuGS, aySonuIGS, aySonuHIGS, aySonuQeydler) = await MezuniyyetAyGunleriFiltreliSayAsync(
                isciId, il, ay, MezuniyyetOdenisTipi.AySonuOdenis);
            decimal aySonuOdenisi = (aySonuGS > 0 || aySonuIGS > 0)
                ? await AySonuMezOdenisiniHesablaAsync(isciId, il, ay, aySonuGS, aySonuIGS, aySonuQeydler)
                : 0;

            if (ayIsGunu > 0 && aySonuHIGS > 0)
                kesinti += Math.Round(maliye.CariMaas / ayIsGunu * aySonuHIGS, 2);

            return (toplamIsGun, kesinti, aySonuOdenisi);
        }

        public Task<int> AyIsGunSayiniHesablaAsync(int il, int ay) =>
            AyinIsGunleriniHesablaAsync(il, ay);

        // İşdən çıxma (çıxış) kəsintisi PREVIEW — toplu hesablama ekranında GROSS-u
        // real hesablama (FerdiHesablaAsync 5.1) ilə eyni göstərmək üçün.
        // İşçi həmin ay işdən çıxıbsa, ayrılma tarixindən SONRAKI iş günləri kəsilir
        // (ayrılma günü daxildir — işlənmiş sayılır). Kəsinti olmasa 0 qaytarır.
        public async Task<decimal> CixisKesintisiPreviewAsync(int isciId, int il, int ay)
        {
            var isci = await _unitOfWork.Repository<Isci>()
                .GetirAsync(x => x.Id == isciId && !x.Silinib);
            if (isci == null
                || isci.Status != IsciStatus.IshtenCixib
                || !isci.IsdenAyrilmaTarixi.HasValue
                || isci.IsdenAyrilmaTarixi.Value.Year != il
                || isci.IsdenAyrilmaTarixi.Value.Month != ay)
                return 0m;

            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId && !x.Silinib);
            decimal esasMaas = maliye?.CariMaas ?? 0m;
            if (esasMaas <= 0) return 0m;

            int ayIsGunu = await AyinIsGunleriniHesablaAsync(il, ay);
            if (ayIsGunu <= 0) return 0m;

            var ayBash = new DateTime(il, ay, 1);
            int islenmis = await IsGunleriniTariheGoereSayAsync(ayBash, isci.IsdenAyrilmaTarixi.Value.Date);
            int cixisGun = Math.Max(0, ayIsGunu - islenmis);
            if (cixisGun <= 0) return 0m;

            return Math.Round(esasMaas / ayIsGunu * cixisGun, 2);
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

        // ─────────────────────────────────────────────────────────
        // ƏVVƏLKİ AY KORREKSİYASI PREVIEW
        // Toplu hesablama ekranında mühasib öncədən görsün deyə —
        // FerdiHesablaAsync-dəki addım 6.6 ilə eyni məntiqdə hesablanır.
        // ─────────────────────────────────────────────────────────
        public async Task<(decimal Kesinti, decimal Gelir, string? Aciq)>
            EvvelkiAyKorreksiyasiPreviewAsync(int isciId, int il, int ay)
        {
            decimal kesinti = 0;
            decimal gelir = 0;
            var hisseler = new List<string>();

            try
            {
                var prevIl = ay == 1 ? il - 1 : il;
                var prevAy = ay == 1 ? 12 : ay - 1;
                var prevAyBaslama = new DateTime(prevIl, prevAy, 1);
                var prevAyBitis = prevAyBaslama.AddMonths(1).AddDays(-1);

                var prevMaas = await _unitOfWork.Repository<Maas>()
                    .Query()
                    .Where(x => x.IsciId == isciId && x.Il == prevIl && x.Ay == prevAy
                        && !x.Silinib && x.Status != MaasStatus.LegvEdildi)
                    .Include(x => x.Detallar).ThenInclude(d => d.MaasNovu)
                    .FirstOrDefaultAsync();

                if (prevMaas == null) return (0, 0, null);

                int prevAyIsGunu = await AyinIsGunleriniHesablaAsync(prevIl, prevAy);
                decimal prevEsasMaas = prevMaas.Detallar
                    .FirstOrDefault(d => !d.Silinib && d.MaasNovu?.Ad == "Əsas Əməkhaqqı")?.Mebleg ?? 0;

                if (prevEsasMaas <= 0 || prevAyIsGunu <= 0) return (0, 0, null);

                decimal prevGunluk = prevEsasMaas / prevAyIsGunu;

                // Xəstəlik
                var xstOdenisler = await _unitOfWork.Repository<XestelikOdenis>()
                    .Query()
                    .Where(x => x.IsciId == isciId && x.Il == prevIl && x.Ay == prevAy
                        && !x.Silinib && x.MaasId == null && x.SirketGunSayi > 0)
                    .ToListAsync();

                if (xstOdenisler.Any())
                {
                    int xstGun = xstOdenisler.Sum(x => x.SirketGunSayi);
                    decimal xstOd = xstOdenisler.Sum(x => x.SirketOdenis);
                    decimal xstKes = Math.Round(prevGunluk * xstGun, 2);
                    kesinti += xstKes;
                    gelir += xstOd;
                    hisseler.Add(
                        $"{prevAy:D2}/{prevIl} xəstəlik: {xstGun} gün kəsinti " +
                        $"{xstKes:N2} ₼ + ödəniş {xstOd:N2} ₼");
                }

                // Məzuniyyət
                var postMezler = await _unitOfWork.Repository<Mezuniyyet>()
                    .Query()
                    .Where(x => x.IsciId == isciId && !x.Silinib
                        && x.Status == MezuniyyetStatus.Tesdiqlenib
                        && x.OdenisTipi == MezuniyyetOdenisTipi.AySonuOdenis
                        && x.Nov != MezuniyyetNovu.DovletVezifelerininIcrasi
                        && x.Nov != MezuniyyetNovu.OzHesabina   // ödənişsiz — məzuniyyət haqqı yoxdur
                        && (x.IsGunlerininSayiManual ?? x.IsGunlerininSayi) > 0
                        && x.YaradilmaTarixi > prevMaas.HesablanmaTarixi
                        && x.BaslamaTarixi <= prevAyBitis
                        && x.BitmeTarixi >= prevAyBaslama)
                    .ToListAsync();

                foreach (var mez in postMezler)
                {
                    var mezBas = mez.BaslamaTarixi < prevAyBaslama ? prevAyBaslama : mez.BaslamaTarixi;
                    var mezBit = mez.BitmeTarixi < prevAyBitis ? mez.BitmeTarixi : prevAyBitis;
                    int mezGun = await IsGunleriniTariheGoereSayAsync(mezBas, mezBit);
                    if (mezGun <= 0) continue;

                    decimal mezKes = Math.Round(prevGunluk * mezGun, 2);
                    var mezH = await MezuniyyetOdenisiDetalliHesablaAsync(mez.IsciId, mezBas, mezBit);
                    decimal mezOd = mezH.CemiOdenis;

                    kesinti += mezKes;
                    gelir += mezOd;
                    hisseler.Add(
                        $"{prevAy:D2}/{prevIl} məzuniyyət ({mezBas:dd.MM}–{mezBit:dd.MM}): " +
                        $"{mezGun} gün kəsinti {mezKes:N2} ₼ + məzuniyyət haqqı {mezOd:N2} ₼");
                }
            }
            catch { return (0, 0, null); }

            return (kesinti, gelir, hisseler.Any() ? string.Join("; ", hisseler) : null);
        }

        // ─────────────────────────────────────────────────────────
        // VERİLMİŞ TARİX ARALIĞINDA İŞ GÜNLƏRİNİ SAY
        // Koreksiya hesablaması üçün: iki tarix arası iş günlərini sayır
        // ─────────────────────────────────────────────────────────
        private async Task<int> IsGunleriniTariheGoereSayAsync(DateTime from, DateTime to)
        {
            var frm = from.Date;
            var end = to.Date;
            if (frm > end) return 0;

            var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= frm && x.Tarix <= end && !x.Silinib);
            var ozelDict = ozelGunler
                .GroupBy(x => x.Tarix.Date)
                .ToDictionary(g => g.Key, g => g.First().Tip);

            int sayi = 0;
            for (var d = frm; d <= end; d = d.AddDays(1))
            {
                if (ozelDict.TryGetValue(d, out var tip))
                {
                    if (tip == GunTipi.IsGunu) sayi++;
                }
                else
                {
                    if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                        sayi++;
                }
            }
            return sayi;
        }
    }
}
