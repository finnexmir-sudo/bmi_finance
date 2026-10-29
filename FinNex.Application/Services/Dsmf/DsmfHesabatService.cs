using FinNex.Application.DTOs.Dsmf;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.Dsmf;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FinNex.Application.Services.Dsmf;

public class DsmfHesabatService : IDsmfHesabatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMaasHesablamaService _maasHesablama;
    private readonly IIsciHYSDondurmaService _hysDondurma;

    private static readonly string[] AyAdlari =
    {
        "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
        "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr"
    };

    public DsmfHesabatService(
        IUnitOfWork unitOfWork,
        IMaasHesablamaService maasHesablama,
        IIsciHYSDondurmaService hysDondurma)
    {
        _unitOfWork = unitOfWork;
        _maasHesablama = maasHesablama;
        _hysDondurma = hysDondurma;
    }

    public async Task<DsmfHissesi3Dto> Hissesi3HazirlaAsync(int il, int rub)
    {
        rub = Math.Clamp(rub, 1, 4);
        var ay1 = (rub - 1) * 3 + 1;
        var ay2 = ay1 + 1;
        var ay3 = ay1 + 2;

        var dto = new DsmfHissesi3Dto
        {
            Il = il,
            Rub = rub,
            Ay1 = ay1,
            Ay2 = ay2,
            Ay3 = ay3,
            Ay1Adi = AyAdlari[ay1],
            Ay2Adi = AyAdlari[ay2],
            Ay3Adi = AyAdlari[ay3],
            AvtomatikAskarlanmayanSebebler = DsmfIslemeyenGunSebebi.AvtomatikAskarlanmayanKodlar
                .Select(k => DsmfIslemeyenGunSebebi.Metinler[k]).ToList(),
        };

        var rubBaslangic = new DateTime(il, ay1, 1);
        var rubBitis = new DateTime(il, ay3, 1).AddMonths(1).AddDays(-1);

        // Rübdə İSTƏNİLƏN gün aktiv olmuş işçilər — "Aktiv işçi" kanonik filtri
        // (IsciStatus.Aktiv) BURAYA YARAMIR, çünki rüb ərzində işdən çıxmış/işə
        // qəbul olmuş işçi də lazımdır. Tarix-kəsişməsi şərti işlədilir.
        var iscilər = await _unitOfWork.Repository<Isci>()
            .Query()
            .AsNoTracking()
            .Where(x => x.IsheQebulTarixi <= rubBitis
                     && (x.IsdenAyrilmaTarixi == null || x.IsdenAyrilmaTarixi >= rubBaslangic))
            .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
            .ToListAsync();

        var ayLar = new[] { ay1, ay2, ay3 };
        var setirler = new List<DsmfHissesi3SetirDto>();

        foreach (var isci in iscilər)
        {
            // sebebKodu -> [ay1Gun, ay2Gun, ay3Gun]
            var sebebGunleri = new Dictionary<int, int[]>();
            void Elave(int sebebKodu, int ayIndex, int gun)
            {
                if (gun <= 0) return;
                if (!sebebGunleri.TryGetValue(sebebKodu, out var arr))
                {
                    arr = new int[3];
                    sebebGunleri[sebebKodu] = arr;
                }
                arr[ayIndex] += gun;
            }

            for (int i = 0; i < 3; i++)
            {
                var ay = ayLar[i];
                var ayBaslangic = new DateTime(il, ay, 1);
                var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

                var aktivBaslangic = isci.IsheQebulTarixi < ayBaslangic ? ayBaslangic : isci.IsheQebulTarixi;
                var aktivBitis = (isci.IsdenAyrilmaTarixi.HasValue && isci.IsdenAyrilmaTarixi.Value < ayBitis)
                    ? isci.IsdenAyrilmaTarixi.Value
                    : ayBitis;

                if (aktivBaslangic > aktivBitis)
                {
                    // Bu ay üzrə işəgötürənlə heç bir müqavilə kəsişməsi yoxdur —
                    // kod 9: tam ayın həqiqi iş günü.
                    var tamAyGun = await HaqiqiIsGunuSayAsync(
                        new[] { (ayBaslangic, ayBitis) }, il, ay);
                    Elave(9, i, tamAyGun);
                    continue;
                }

                // Kod 8 — ayın ortasında işə qəbul (1-ci gün deyil).
                if (isci.IsheQebulTarixi > ayBaslangic && isci.IsheQebulTarixi <= ayBitis)
                {
                    var bitenGun = isci.IsheQebulTarixi.AddDays(-1);
                    if (bitenGun >= ayBaslangic)
                    {
                        var gun = await HaqiqiIsGunuSayAsync(new[] { (ayBaslangic, bitenGun) }, il, ay);
                        Elave(8, i, gun);
                    }
                }

                // Kod 6 — ay ərzində əmək müqaviləsinə xitam (son gün deyil).
                if (isci.IsdenAyrilmaTarixi.HasValue
                    && isci.IsdenAyrilmaTarixi.Value >= ayBaslangic
                    && isci.IsdenAyrilmaTarixi.Value < ayBitis)
                {
                    var baslayanGun = isci.IsdenAyrilmaTarixi.Value.AddDays(1);
                    var gun = await HaqiqiIsGunuSayAsync(new[] { (baslayanGun, ayBitis) }, il, ay);
                    Elave(6, i, gun);
                }

                // Kod 1 — ödənişli əsas məzuniyyət (Illik).
                Elave(1, i, await NovUzreHaqiqiIsGunuAsync(isci.Id, MezuniyyetNovu.Illik, il, ay));

                // Kod 3 — ödənişsiz məzuniyyət (Ə.M. 129). Mövcud, sınanmış servis metodu.
                Elave(3, i, await _maasHesablama.OzHesabinaIsGunuSayAsync(isci.Id, il, ay));

                // Kod 5 — xəstələnmə. Mövcud, sınanmış servis metodu (26.10.2026-da
                // düzəldilmiş "həqiqi iş günü" versiyası).
                Elave(5, i, await _maasHesablama.XestelikHaqiqiIsGunuSayAsync(isci.Id, il, ay));

                // Kod 11 — dövlət/ictimai vəzifə.
                Elave(11, i, await NovUzreHaqiqiIsGunuAsync(isci.Id, MezuniyyetNovu.DovletVezifelerininIcrasi, il, ay));
            }

            if (sebebGunleri.Count == 0) continue;

            var adSoyadAta = $"{isci.Soyad} {isci.Ad} {isci.AtaAdi}".Trim();
            foreach (var (sebebKodu, gunler) in sebebGunleri.OrderBy(x => x.Key))
            {
                setirler.Add(new DsmfHissesi3SetirDto
                {
                    IsciId = isci.Id,
                    AdSoyadAta = adSoyadAta,
                    Fin = isci.FIN,
                    SebebKodu = sebebKodu,
                    SebebMetni = DsmfIslemeyenGunSebebi.Metinler[sebebKodu],
                    Ay1Gun = gunler[0],
                    Ay2Gun = gunler[1],
                    Ay3Gun = gunler[2],
                });
            }
        }

        for (int i = 0; i < setirler.Count; i++)
            setirler[i].SiraNo = i + 1;

        dto.Setirler = setirler;
        return dto;
    }

    // ── Mezuniyyet.Nov üzrə REAL (həftəsonu/bayram çıxılmış) iş günü — ayrıca,
    // MaasHesablamaService-dən MÜSTƏQİL kopyadır. Payroll-un öz "əsl hesablama"
    // metodlarına toxunmamaq üçün (bax CLAUDE.md "İşlək funksiyanı silmək/dəyişmək
    // yalnız açıq icazə ilə") bu hesabat üçün ayrıca, oxu-yalnız bir nüsxə yazılıb —
    // MezuniyyetGunleriniSayGenisAsync işlədilmədi, çünki o, Illik+Ezamiyyət-i
    // BİRLİKDƏ qaytarır (Ezamiyyət DSMF-nin 12 səbəbindən biri DEYİL — işçi
    // ezamiyyətdə işləyir, "işləmədiyi gün" sayılmır).
    private async Task<int> NovUzreHaqiqiIsGunuAsync(int isciId, MezuniyyetNovu nov, int il, int ay)
    {
        var ayBaslangic = new DateTime(il, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        var qeydler = await _unitOfWork.Repository<Mezuniyyet>()
            .Query()
            .AsNoTracking()
            .Where(x => x.IsciId == isciId
                     && x.Status == MezuniyyetStatus.Tesdiqlenib
                     && x.Nov == nov
                     && !x.JetonIleOdendi
                     && (x.IsGunlerininSayiManual ?? x.IsGunlerininSayi) > 0
                     && x.BaslamaTarixi <= ayBitis
                     && x.BitmeTarixi >= ayBaslangic)
            .Select(x => new { x.BaslamaTarixi, x.BitmeTarixi })
            .ToListAsync();

        if (qeydler.Count == 0) return 0;

        return await HaqiqiIsGunuSayAsync(
            qeydler.Select(x => (x.BaslamaTarixi, x.BitmeTarixi)), il, ay);
    }

    // Verilmiş tarix aralıqlarının bir aya düşən REAL iş günü sayı (BayramGunu +
    // həftəsonu qaydaları) — HashSet ilə üst-üstə düşən günlər ikiqat sayılmır.
    private async Task<int> HaqiqiIsGunuSayAsync(
        IEnumerable<(DateTime Baslama, DateTime Bitme)> araliqlar, int il, int ay)
    {
        var ayBaslangic = new DateTime(il, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
            .HamisiniGetirAsync(x => x.Tarix >= ayBaslangic && x.Tarix <= ayBitis);
        var ozelTipDict = ozelGunler
            .GroupBy(x => x.Tarix.Date)
            .ToDictionary(g => g.Key, g => g.First().Tip);

        var gunler = new HashSet<DateTime>();
        foreach (var (baslamaX, bitmeX) in araliqlar)
        {
            var baslama = baslamaX < ayBaslangic ? ayBaslangic : baslamaX;
            var bitme = bitmeX > ayBitis ? ayBitis : bitmeX;
            for (var t = baslama.Date; t <= bitme.Date; t = t.AddDays(1))
            {
                bool realIsGunu = ozelTipDict.TryGetValue(t.Date, out var tip)
                    ? tip == GunTipi.IsGunu
                    : t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;
                if (realIsGunu) gunler.Add(t.Date);
            }
        }
        return gunler.Count;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Əlavə №4 Bölmə 2/3/4/5 — "güzəşt/azadolma" cədvəlləri (29.10.2026). Hissə
    // 3-dən FƏRQLİ struktur: işçi-sətri YOX, qanun-maddəsi-sətridir. İstifadəçi
    // ilə təsdiqlənmiş əhatə (yalnız bu bəndlər, qalanı bu banka aid deyil —
    // bax DsmfIstisnaGostericileri.cs):
    //   Bölmə 2: 4.2.6/102.1.8 (HYS, yalnız işçi) + 4.2.5/102.1.6 (200 AZN) +
    //            4.5.3/102.4.5 (Qaçqın) + 4.4/102.3 (Əlillər) +
    //            4.2.7.7/102.1.14.16 (əsgər pulu)
    //   Bölmə 3: 5.2.9 (HYS, yalnız işçi — 29.10.2026-da düzəldildi) + 5.2.12 (xəstəlik)
    //   Bölmə 4: 6.3 (xəstəlik + manual gəlir IssizliyeCelb=false)
    //   Bölmə 5: 7.2 (xəstəlik + manual gəlir ItsseCelb=false)
    // ═══════════════════════════════════════════════════════════════════════
    // Mənbə şablon: UNIFIED_APPENDIX_4_DISCOUNTS.xlsx. 32 mümkün VM maddəsindən
    // yalnız 5-i sistemdə qarşılığı olan (BMI Finance üçün) maddələrdir, qalanı
    // (neft-qaz, texnologiya parkı, Ələt AİZ, diplomatik xidmət və s.) bu banka
    // aid deyil.
    public async Task<DsmfIstisnaBolmesiDto> Bolme2HazirlaAsync(int il, int rub)
    {
        var dto = BolmeBos(2, DsmfIstisnaGostericileri.Bolme2Basliq, il, rub);
        var ayLar = new[] { dto.Ay1, dto.Ay2, dto.Ay3 };

        var hysSetir = new DsmfIstisnaSetirDto { SiraNo = 1, Gostərici = DsmfIstisnaGostericileri.Madde1018 };
        var standartSetir = new DsmfIstisnaSetirDto { SiraNo = 2, Gostərici = DsmfIstisnaGostericileri.Madde1016 };
        var qacqinSetir = new DsmfIstisnaSetirDto { SiraNo = 3, Gostərici = DsmfIstisnaGostericileri.Madde10245 };
        var elillikSetir = new DsmfIstisnaSetirDto { SiraNo = 4, Gostərici = DsmfIstisnaGostericileri.Madde1023 };
        var esgerSetir = new DsmfIstisnaSetirDto { SiraNo = 5, Gostərici = DsmfIstisnaGostericileri.Madde10211416 };

        for (int i = 0; i < 3; i++)
        {
            var (hysIscilər, hysCemi) = await HysIsciPayiAsync(il, ayLar[i]);
            SetirAyaYaz(hysSetir, i, hysIscilər.Count, hysCemi);

            var (standartIscilər, standartCemi) = await StandartGuzestAsync(il, ayLar[i]);
            SetirAyaYaz(standartSetir, i, standartIscilər.Count, standartCemi);

            var (qacqinIscilər, qacqinCemi) = await GuzestNovuUzreAsync(il, ayLar[i], GuzestNovu.MecburiKockun);
            SetirAyaYaz(qacqinSetir, i, qacqinIscilər.Count, qacqinCemi);

            var (elillikIscilər, elillikCemi) = await GuzestNovuUzreAsync(il, ayLar[i], GuzestNovu.Elillik);
            SetirAyaYaz(elillikSetir, i, elillikIscilər.Count, elillikCemi);

            var (esgerIscilər, esgerCemi) = await ManualGelirVergiIstisnasiAsync(il, ayLar[i]);
            SetirAyaYaz(esgerSetir, i, esgerIscilər.Count, esgerCemi);
        }

        dto.Setirler = new List<DsmfIstisnaSetirDto> { hysSetir, standartSetir, qacqinSetir, elillikSetir, esgerSetir };
        return dto;
    }

    // HYS — YALNIZ işçi payı (IsciHYS.Mebleg), işəgötürən payı DAXİL DEYİL
    // (istifadəçi qərarı, 29.10.2026: "sirket yox" — bu qayda Bölmə 2-nin
    // VM 102.1.8 sətrinə VƏ Bölmə 3-ün 5.2.9-una EYNİ dərəcədə aiddir, ikisi
    // də bu metodu çağırır — əvvəlki "5.2.9 = işçi+işəgötürən" versiyası
    // səhv idi, düzəldildi). Dondurulmuş aylar (IIsciHYSDondurmaService) TƏK
    // mənbədən xaric edilir — bax CLAUDE.md "HYS — Bir Aylıq Dondurma".
    private async Task<(HashSet<int> Iscilər, decimal Cemi)> HysIsciPayiAsync(int il, int ay)
    {
        var ayBaslangic = new DateTime(il, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        var qeydler = await _unitOfWork.Repository<IsciHYS>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib
                     && x.BaslamaTarixi <= ayBitis
                     && (x.BitmeTarixi == null || x.BitmeTarixi >= ayBaslangic))
            .ToListAsync();

        var dondurulmus = await _hysDondurma.DondurulmusIdlerAsync(il, ay);
        if (dondurulmus.Count > 0)
            qeydler = qeydler.Where(x => !dondurulmus.Contains(x.Id)).ToList();

        if (qeydler.Count == 0) return (new HashSet<int>(), 0m);

        var isciMebleqleri = qeydler.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Sum(x => x.Mebleg));
        return (isciMebleqleri.Keys.ToHashSet(), isciMebleqleri.Values.Sum());
    }

    // Standart 200 AZN gəlir vergisi güzəşti (VM 102.1.6, brüt ≤ 2500 — bax
    // MaasHesablamaService.cs "9.2 Standart 200 AZN güzəşti" bölməsi). Düstur
    // deep-dependency-lidir (brutMaasGuzestYoxlama elaveCemi/mezuniyyetAvansBrutu/
    // VM9821-dən asılıdır) — TƏKRAR YAZILMADI (dublikat risk), əvəzinə artıq
    // HESABLANMIŞ VƏ PERSİST EDİLMİŞ "Vergi Guzesti" izahat addımından (Maas.
    // HesablamaIzahi JSON) oxunur — tək mənbə, canlı payroll ilə HƏMİŞƏ
    // tutuşur.
    private async Task<(HashSet<int> Iscilər, decimal Cemi)> StandartGuzestAsync(int il, int ay)
    {
        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay
                     && (x.Status == MaasStatus.Tesdiqlendi || x.Status == MaasStatus.Odenildi)
                     && x.HesablamaIzahi != null)
            .Select(x => new { x.IsciId, x.HesablamaIzahi })
            .ToListAsync();

        var iscilər = new HashSet<int>();
        decimal cemi = 0m;
        foreach (var m in maaslar)
        {
            var standart = StandartGuzestdenCixar(m.HesablamaIzahi!);
            if (standart <= 0) continue;
            iscilər.Add(m.IsciId);
            cemi += standart;
        }
        return (iscilər, cemi);
    }

    // Əlillik/Qaçqın (MecburiKockun) güzəştinin HƏQİQƏTƏN tətbiq olunan payı.
    // İşçidə eyni anda BİRDƏN ÇOX növdə aktiv güzəşt varsa (nadir), vergi
    // hesablamasında yalnız ƏN BÖYÜYÜ tətbiq olunur (Madde 102 — toplanmır).
    // Bu metod HƏR növü ayrıca sorğulayır, amma yalnız HƏQİQƏTƏN qalib gələn
    // (Maas.HesablamaIzahi-dəki "İşçi güzəştləri" addımının Mebleg-i bu
    // Guzest-in Mebleg-inə DƏQİQ bərabərdirsə) sətirləri sayır — əks halda
    // güzəşt qeydiyyatda var, amma HEÇ VAXT tətbiq olunmayıb (başqa, daha
    // böyük güzəşt qalib gəlib) deməkdir, DSMF-ə yazılmamalıdır.
    private async Task<(HashSet<int> Iscilər, decimal Cemi)> GuzestNovuUzreAsync(int il, int ay, GuzestNovu novu)
    {
        var ayBaslangic = new DateTime(il, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        var namizedler = await _unitOfWork.Repository<IsciGuzest>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib
                     && x.BaslamaTarixi <= ayBitis
                     && (x.BitmeTarixi == null || x.BitmeTarixi >= ayBaslangic))
            .Include(x => x.Guzest)
            .Where(x => x.Guzest != null && !x.Guzest.Silinib && x.Guzest.Aktivdir && x.Guzest.Novu == novu)
            .Select(x => new { x.IsciId, x.Guzest!.Mebleg })
            .ToListAsync();

        if (namizedler.Count == 0) return (new HashSet<int>(), 0m);

        var isciMax = namizedler.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Max(x => x.Mebleg));

        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay
                     && isciMax.Keys.Contains(x.IsciId)
                     && (x.Status == MaasStatus.Tesdiqlendi || x.Status == MaasStatus.Odenildi)
                     && x.HesablamaIzahi != null)
            .Select(x => new { x.IsciId, x.HesablamaIzahi })
            .ToListAsync();

        var iscilər = new HashSet<int>();
        decimal cemi = 0m;
        foreach (var m in maaslar)
        {
            var tetbiqOlunan = MaksIsciGuzestindenCixar(m.HesablamaIzahi!);
            if (tetbiqOlunan > 0 && tetbiqOlunan == isciMax[m.IsciId])
            {
                iscilər.Add(m.IsciId);
                cemi += tetbiqOlunan;
            }
        }
        return (iscilər, cemi);
    }

    // Manual gəlir növlərindən (məs. "Hərbi Xidmət") QİSMƏN gəlir vergisindən
    // azad olanlar — ManualGelirIstisnasiAsync-dən FƏRQLİ: orada İşsizlik/İTSS
    // bazasından TAM azadlıq (bayraqla), burada gəlir vergisi bazasından YALNIZ
    // MaasNovu.GelirVergisiAzadMebleg qədər (Min(ödəniş, azad məbləğ)) azadlıq.
    private async Task<(HashSet<int> Iscilər, decimal Cemi)> ManualGelirVergiIstisnasiAsync(int il, int ay)
    {
        var novlər = await _unitOfWork.Repository<MaasNovu>()
            .Query()
            .AsNoTracking()
            .Where(x => x.Aktivdir && !x.Silinib && x.ManualGelir
                     && x.VergiyeCelb && x.GelirVergisiAzadMebleg > 0)
            .Select(x => new { x.Id, x.GelirVergisiAzadMebleg })
            .ToListAsync();

        if (novlər.Count == 0) return (new HashSet<int>(), 0m);
        var azadMap = novlər.ToDictionary(x => x.Id, x => x.GelirVergisiAzadMebleg);

        var detallar = await _unitOfWork.Repository<MaasDetay>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && azadMap.Keys.Contains(x.MaasNovuId)
                     && x.Maas.Il == il && x.Maas.Ay == ay && !x.Maas.Silinib
                     && (x.Maas.Status == MaasStatus.Tesdiqlendi || x.Maas.Status == MaasStatus.Odenildi))
            .Select(x => new { x.Maas.IsciId, x.MaasNovuId, x.Mebleg })
            .ToListAsync();

        if (detallar.Count == 0) return (new HashSet<int>(), 0m);

        var iscilər = new HashSet<int>();
        decimal cemi = 0m;
        foreach (var d in detallar)
        {
            var azad = Math.Min(d.Mebleg, azadMap[d.MaasNovuId]);
            if (azad <= 0) continue;
            iscilər.Add(d.IsciId);
            cemi += azad;
        }
        return (iscilər, cemi);
    }

    // ── Maas.HesablamaIzahi (JSON) oxuyucuları — Bölmə 2-nin HYS-dən başqa HƏR
    // sətri bu ikisindən biri ilə işləyir. Parse xətası (köhnə/boş/korrupt JSON)
    // SƏSSİZ 0 qaytarır — DSMF sətri əskik qalar, amma səhv rəqəm YAZILMAZ.
    private static List<HesablamaIzahiDto>? IzahatlarıOxu(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<HesablamaIzahiDto>>(json); }
        catch (JsonException) { return null; }
    }

    private static decimal MaksIsciGuzestindenCixar(string? hesablamaIzahiJson)
        => IzahatlarıOxu(hesablamaIzahiJson)?.FirstOrDefault(x => x.Addim == "İşçi güzəştləri")?.Mebleg ?? 0m;

    private static decimal StandartGuzestdenCixar(string? hesablamaIzahiJson)
    {
        var izahatlar = IzahatlarıOxu(hesablamaIzahiJson);
        var vergiGuzesti = izahatlar?.FirstOrDefault(x => x.Addim == "Vergi Guzesti");
        if (vergiGuzesti == null) return 0m;
        return Math.Max(0, vergiGuzesti.Mebleg - MaksIsciGuzestindenCixar(hesablamaIzahiJson));
    }

    public async Task<DsmfIstisnaBolmesiDto> Bolme3HazirlaAsync(int il, int rub)
    {
        var dto = BolmeBos(3, DsmfIstisnaGostericileri.Bolme3Basliq, il, rub);
        var ayLar = new[] { dto.Ay1, dto.Ay2, dto.Ay3 };

        var hysSetir = new DsmfIstisnaSetirDto { SiraNo = 1, Gostərici = DsmfIstisnaGostericileri.Madde529 };
        var xstSetir = new DsmfIstisnaSetirDto { SiraNo = 2, Gostərici = DsmfIstisnaGostericileri.Madde5212 };

        for (int i = 0; i < 3; i++)
        {
            // 29.10.2026, istifadəçi düzəlişi: "sirket yox" — 5.2.9 YALNIZ
            // işçi payını (IsciHYS.Mebleg) göstərir, işəgötürən 15%-i artıq
            // DAXİL DEYİL (əvvəlki versiya birləşdirirdi — düzəldildi).
            // Bölmə 2-nin VM 102.1.8 sətri ilə EYNİ metodu (HysIsciPayiAsync)
            // çağırır — iki ayrı qanun maddəsi, amma eyni "yalnız işçi" qaydası.
            var (hysIscilər, hysCemi) = await HysIsciPayiAsync(il, ayLar[i]);
            var (xstIscilər, xstCemi) = await XestelikSirketOdenisiAsync(il, ayLar[i]);
            SetirAyaYaz(hysSetir, i, hysIscilər.Count, hysCemi);
            SetirAyaYaz(xstSetir, i, xstIscilər.Count, xstCemi);
        }

        dto.Setirler = new List<DsmfIstisnaSetirDto> { hysSetir, xstSetir };
        return dto;
    }

    public async Task<DsmfIstisnaBolmesiDto> Bolme4HazirlaAsync(int il, int rub)
        => await BirSetirlikBolmeHazirlaAsync(4, DsmfIstisnaGostericileri.Bolme4Basliq,
            DsmfIstisnaGostericileri.Madde63, il, rub, issizlikFlag: true);

    public async Task<DsmfIstisnaBolmesiDto> Bolme5HazirlaAsync(int il, int rub)
        => await BirSetirlikBolmeHazirlaAsync(5, DsmfIstisnaGostericileri.Bolme5Basliq,
            DsmfIstisnaGostericileri.Madde72, il, rub, issizlikFlag: false);

    // Bölmə 4 və 5 EYNİ naxışdadır (xəstəlik + manual-gəlir-istisnası, TƏK sətir) —
    // yalnız hansı MaasNovu bayrağına (İşsizlik/İTSS) baxıldığı fərqlənir.
    private async Task<DsmfIstisnaBolmesiDto> BirSetirlikBolmeHazirlaAsync(
        int bolmeNo, string basliq, string maddeMetni, int il, int rub, bool issizlikFlag)
    {
        var dto = BolmeBos(bolmeNo, basliq, il, rub);
        var ayLar = new[] { dto.Ay1, dto.Ay2, dto.Ay3 };
        var setir = new DsmfIstisnaSetirDto { SiraNo = 1, Gostərici = maddeMetni };

        for (int i = 0; i < 3; i++)
        {
            var (xstIscilər, xstCemi) = await XestelikSirketOdenisiAsync(il, ayLar[i]);
            var (manualIscilər, manualCemi) = await ManualGelirIstisnasiAsync(il, ayLar[i], issizlikFlag);

            var birlesmisIscilər = new HashSet<int>(xstIscilər);
            birlesmisIscilər.UnionWith(manualIscilər);
            SetirAyaYaz(setir, i, birlesmisIscilər.Count, xstCemi + manualCemi);
        }

        dto.Setirler = new List<DsmfIstisnaSetirDto> { setir };
        return dto;
    }

    private static DsmfIstisnaBolmesiDto BolmeBos(int bolmeNo, string basliq, int il, int rub)
    {
        rub = Math.Clamp(rub, 1, 4);
        var ay1 = (rub - 1) * 3 + 1;
        return new DsmfIstisnaBolmesiDto
        {
            BolmeNo = bolmeNo,
            Basliq = basliq,
            Il = il,
            Rub = rub,
            Ay1 = ay1,
            Ay2 = ay1 + 1,
            Ay3 = ay1 + 2,
            Ay1Adi = AyAdlari[ay1],
            Ay2Adi = AyAdlari[ay1 + 1],
            Ay3Adi = AyAdlari[ay1 + 2],
        };
    }

    private static void SetirAyaYaz(DsmfIstisnaSetirDto s, int ayIndex, int isciSayi, decimal mebleg)
    {
        switch (ayIndex)
        {
            case 0: s.Ay1IsciSayi = isciSayi; s.Ay1Mebleg = mebleg; break;
            case 1: s.Ay2IsciSayi = isciSayi; s.Ay2Mebleg = mebleg; break;
            case 2: s.Ay3IsciSayi = isciSayi; s.Ay3Mebleg = mebleg; break;
        }
    }

    // Xəstəlik vərəqəsi üzrə ŞİRKƏT ödənişi (MaasHesablamaService.FerdiHesabla-nın
    // DSMF/İşsizlik/İTSS bazasından çıxdığı ƏN TƏK dəyər — bax o faylın 1147-1149-cu
    // sətirləri) — yalnız real Maas-a bağlanmış (MaasId != null) və TƏSDİQLƏNMİŞ/
    // ÖDƏNİLMİŞ statusda olan qeydlər sayılır, Layihə (hələ hesablanmamış) YOX.
    private async Task<(HashSet<int> Iscilər, decimal Cemi)> XestelikSirketOdenisiAsync(int il, int ay)
    {
        var qeydler = await _unitOfWork.Repository<XestelikOdenis>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay && x.SirketOdenis > 0
                     && x.MaasId != null
                     && (x.Maas!.Status == MaasStatus.Tesdiqlendi || x.Maas!.Status == MaasStatus.Odenildi))
            .Select(x => new { x.IsciId, x.SirketOdenis })
            .ToListAsync();

        if (qeydler.Count == 0) return (new HashSet<int>(), 0m);
        return (qeydler.Select(x => x.IsciId).ToHashSet(), qeydler.Sum(x => x.SirketOdenis));
    }

    // Konfiqurasiyalı manual gəlir növlərindən (HR → Əmək haqqı parametrləri →
    // "Konfiqurasiyalı Gəlir Növləri", məs. "Hərbi Xidmət") İşsizlik/İTSS
    // bazasından AZAD olanlar — ADLA YOX, BAYRAQLA tutulur (`issizlikFlag` true
    // olanda IssizliyeCelb=false, false olanda ItsseCelb=false). Gələcəkdə
    // yaradılan HƏR yeni manual gəlir növü, eyni bayraqla işarələnərsə, kodda
    // dəyişiklik lazım olmadan avtomatik düşür. Yalnız real Maas-a bağlanmış,
    // TƏSDİQLƏNMİŞ/ÖDƏNİLMİŞ sətirlər sayılır (Layihə YOX) —
    // XestelikSirketOdenisiAsync ilə EYNİ status qaydası.
    private async Task<(HashSet<int> Iscilər, decimal Cemi)> ManualGelirIstisnasiAsync(
        int il, int ay, bool issizlikFlag)
    {
        var novIdler = await _unitOfWork.Repository<MaasNovu>()
            .Query()
            .AsNoTracking()
            .Where(x => x.Aktivdir && !x.Silinib && x.ManualGelir
                     && (issizlikFlag ? !x.IssizliyeCelb : !x.ItsseCelb))
            .Select(x => x.Id)
            .ToListAsync();

        if (novIdler.Count == 0) return (new HashSet<int>(), 0m);

        var detallar = await _unitOfWork.Repository<MaasDetay>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && novIdler.Contains(x.MaasNovuId)
                     && x.Maas.Il == il && x.Maas.Ay == ay && !x.Maas.Silinib
                     && (x.Maas.Status == MaasStatus.Tesdiqlendi || x.Maas.Status == MaasStatus.Odenildi))
            .Select(x => new { x.Maas.IsciId, x.Mebleg })
            .ToListAsync();

        if (detallar.Count == 0) return (new HashSet<int>(), 0m);
        return (detallar.Select(x => x.IsciId).ToHashSet(), detallar.Sum(x => x.Mebleg));
    }
}
