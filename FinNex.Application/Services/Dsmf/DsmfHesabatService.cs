using FinNex.Application.DTOs.Dsmf;
using FinNex.Application.Interfaces.Dsmf;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Dsmf;

public class DsmfHesabatService : IDsmfHesabatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMaasHesablamaService _maasHesablama;

    private static readonly string[] AyAdlari =
    {
        "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
        "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr"
    };

    public DsmfHesabatService(IUnitOfWork unitOfWork, IMaasHesablamaService maasHesablama)
    {
        _unitOfWork = unitOfWork;
        _maasHesablama = maasHesablama;
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
}
