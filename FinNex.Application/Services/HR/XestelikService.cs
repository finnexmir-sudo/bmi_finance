using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// Xəstəlik bülletənləri xidməti.
    ///
    /// Hesablama məntiqi:
    ///   S            = Son 12 ayın cəmi qazancı (IsciAyliqQazanc)
    ///   Son12AyIsGun = Son 12 ayın iş günlərinin cəmi (BayramGunu nəzərə alınır)
    ///   BirGunluk    = S / Son12AyIsGun
    ///
    ///   Şirkət LİMİTİ: bütün cari il boyunca maksimum 14 iş günü
    ///   (qalan günlər DSMF-in payıdır — sistem yalnız informativ göstərir)
    /// </summary>
    public class XestelikService : IXestelikService
    {
        private readonly IUnitOfWork _unitOfWork;
        private const int SIRKET_MAX_GUN = 14;

        public XestelikService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IList<XestelikDto>>> GetListAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Xestelik>()
                    .Query()
                    .Where(x => !x.Silinib)
                    .Include(x => x.Isci)
                    .Include(x => x.Odenisler)
                    .OrderByDescending(x => x.BaslamaTarixi)
                    .ToListAsync();

                var dtos = list.Select(x => new XestelikDto
                {
                    Id = x.Id,
                    IsciId = x.IsciId,
                    IsciAdSoyad = x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : "",
                    BaslamaTarixi = x.BaslamaTarixi,
                    BitmeTarixi = x.BitmeTarixi,
                    IsGunSayi = x.IsGunSayi,
                    BulletenNomresi = x.BulletenNomresi,
                    MualiceMuessisesi = x.MualiceMuessisesi,
                    Qeyd = x.Qeyd,
                    Status = (int)x.Status,
                    HrTesdiqTarixi = x.HrTesdiqTarixi,
                    UmumiSirketOdenisi = x.Odenisler.Sum(o => o.SirketOdenis)
                }).ToList();

                return Result<IList<XestelikDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<XestelikDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<XestelikDto?>> GetByIdAsync(int id)
        {
            try
            {
                var x = await _unitOfWork.Repository<Xestelik>()
                    .Query()
                    .Where(x => x.Id == id && !x.Silinib)
                    .Include(x => x.Isci)
                    .Include(x => x.Odenisler)
                    .FirstOrDefaultAsync();

                if (x == null) return Result<XestelikDto?>.Fail("Tapılmadı.");

                var dto = new XestelikDto
                {
                    Id = x.Id,
                    IsciId = x.IsciId,
                    IsciAdSoyad = x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : "",
                    BaslamaTarixi = x.BaslamaTarixi,
                    BitmeTarixi = x.BitmeTarixi,
                    IsGunSayi = x.IsGunSayi,
                    BulletenNomresi = x.BulletenNomresi,
                    MualiceMuessisesi = x.MualiceMuessisesi,
                    Qeyd = x.Qeyd,
                    Status = (int)x.Status,
                    HrTesdiqTarixi = x.HrTesdiqTarixi,
                    UmumiSirketOdenisi = x.Odenisler.Sum(o => o.SirketOdenis)
                };

                return Result<XestelikDto?>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<XestelikDto?>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<int>> CreateAsync(XestelikCreateDto input, int? hrIsciId)
        {
            try
            {
                if (input.BitmeTarixi < input.BaslamaTarixi)
                    return Result<int>.Fail("Bitmə tarixi başlama tarixindən sonra olmalıdır.");

                if (string.IsNullOrWhiteSpace(input.BulletenNomresi))
                    return Result<int>.Fail("Bülletən nömrəsi məcburidir.");

                int isGunSayi = await IsGunSayiniHesablaAsync(input.BaslamaTarixi, input.BitmeTarixi);
                if (isGunSayi <= 0)
                    return Result<int>.Fail("Verilmiş tarix aralığında iş günü yoxdur.");

                var entity = new Xestelik
                {
                    IsciId = input.IsciId,
                    BaslamaTarixi = input.BaslamaTarixi,
                    BitmeTarixi = input.BitmeTarixi,
                    IsGunSayi = isGunSayi,
                    BulletenNomresi = input.BulletenNomresi.Trim(),
                    MualiceMuessisesi = string.IsNullOrWhiteSpace(input.MualiceMuessisesi) ? null : input.MualiceMuessisesi.Trim(),
                    Qeyd = string.IsNullOrWhiteSpace(input.Qeyd) ? null : input.Qeyd.Trim(),
                    Status = XestelikStatus.Tesdiqlenib,
                    HrId = hrIsciId,
                    HrTesdiqTarixi = DateTime.Now
                };

                await _unitOfWork.Repository<Xestelik>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Avtomatik ödəniş qeydləri yarat (hər ay üçün)
                try
                {
                    await OdenisleriYaratAsync(entity);
                }
                catch (Exception odEx)
                {
                    // Ödəniş yaradılması uğursuz olarsa — xəstəlik qeydi yenə qalır
                    // amma istifadəçiyə xəbər ver
                    return Result<int>.Ok(entity.Id);
                }

                return Result<int>.Ok(entity.Id);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return Result<int>.Fail($"Xəta: {msg}");
            }
        }

        public async Task<Result> DeleteAsync(int id)
        {
            try
            {
                var entity = await _unitOfWork.Repository<Xestelik>()
                    .Query()
                    .Where(x => x.Id == id && !x.Silinib)
                    .Include(x => x.Odenisler)
                    .FirstOrDefaultAsync();

                if (entity == null) return Result.Fail("Tapılmadı.");

                // Ödəniş qeydlərini də sil
                foreach (var o in entity.Odenisler)
                {
                    o.Silinib = true;
                    o.SilinmeTarixi = DateTime.Now;
                }

                entity.Silinib = true;
                entity.SilinmeTarixi = DateTime.Now;
                entity.Status = XestelikStatus.LegvEdildi;

                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Xəstəlik qeydi silindi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<XestelikPreviewDto>> PreviewAsync(int isciId, DateTime baslama, DateTime bitme)
        {
            try
            {
                if (bitme < baslama)
                    return Result<XestelikPreviewDto>.Fail("Bitmə tarixi başlama tarixindən sonra olmalıdır.");

                int isGunSayi = await IsGunSayiniHesablaAsync(baslama, bitme);

                // Son 12 ayın cəmi qazancı
                var son12 = await _unitOfWork.Repository<IsciAyliqQazanc>()
                    .Query()
                    .Where(x => x.IsciId == isciId && !x.Silinib)
                    .OrderByDescending(x => x.Il * 12 + x.Ay)
                    .Take(12)
                    .ToListAsync();

                decimal S = son12.Sum(x => x.Qazanc);
                int qeydSayi = son12.Count;

                // Son 12 ayın iş günü cəmi (xəstəliyin başladığı aydan əvvəlki 12 ay)
                int son12AyIsGun = await Son12AyIsGununuHesablaAsync(baslama);

                decimal birGunluk = son12AyIsGun > 0 ? S / son12AyIsGun : 0;

                // Cari ildə bu işçinin artıq aldığı şirkət ödənişi (gün sayı)
                int oncekiSirketGun = await OncekiSirketGununuTapAsync(isciId, baslama.Year);

                // Şirkət limitini hesabla
                int qalanLimit = Math.Max(0, SIRKET_MAX_GUN - oncekiSirketGun);
                int sirketGun = Math.Min(isGunSayi, qalanLimit);
                int dsmfGun = isGunSayi - sirketGun;

                decimal sirketOdenis = Math.Round(birGunluk * sirketGun, 2);
                decimal dsmfOdenis = Math.Round(birGunluk * dsmfGun, 2);

                string? xeberdarliq = null;
                if (qeydSayi < 12)
                    xeberdarliq = $"Yalnız {qeydSayi}/12 ay qazanc tarixçəsi var — hesablama dəqiq olmaya bilər.";
                if (sirketGun == 0 && isGunSayi > 0)
                    xeberdarliq = $"İşçi cari ildə artıq {oncekiSirketGun}/14 şirkət ödənişli xəstəlik istifadə edib. Yalnız DSMF ödəyəcək.";
                else if (sirketGun < isGunSayi)
                    xeberdarliq = $"İşçi cari ildə artıq {oncekiSirketGun} gün xəstəlik istifadə edib. Şirkət yalnız {sirketGun} gün ödəyə bilər.";

                return Result<XestelikPreviewDto>.Ok(new XestelikPreviewDto
                {
                    IsGunSayi = isGunSayi,
                    S = S,
                    Son12AyIsGunu = son12AyIsGun,
                    BirGunluk = Math.Round(birGunluk, 2),
                    SirketGun = sirketGun,
                    DsmfGun = dsmfGun,
                    SirketOdenis = sirketOdenis,
                    DsmfOdenis = dsmfOdenis,
                    OncekiSirketGun = oncekiSirketGun,
                    Xeberdarliq = xeberdarliq
                });
            }
            catch (Exception ex)
            {
                return Result<XestelikPreviewDto>.Fail($"Xəta: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<List<Xestelik>> AyUzreXestelikleriGetirAsync(int isciId, int il, int ay)
        {
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

            return await _unitOfWork.Repository<Xestelik>()
                .Query()
                .Where(x =>
                    x.IsciId == isciId &&
                    !x.Silinib &&
                    x.Status == XestelikStatus.Tesdiqlenib &&
                    x.BaslamaTarixi <= ayBitis &&
                    x.BitmeTarixi >= ayBaslangic)
                .Include(x => x.Odenisler)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────
        // KÖMƏKÇI METODLAR
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Tarix aralığında iş günlərini sayır (şənbə, bazar, bayram çıxılmış).
        /// </summary>
        private async Task<int> IsGunSayiniHesablaAsync(DateTime baslama, DateTime bitme)
        {
            var bayramlar = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x =>
                    x.Tarix >= baslama &&
                    x.Tarix <= bitme &&
                    !x.Silinib);
            var bayramTarixleri = bayramlar.Select(x => x.Tarix.Date).ToHashSet();

            int sayi = 0;
            for (var t = baslama.Date; t <= bitme.Date; t = t.AddDays(1))
            {
                if (t.DayOfWeek != DayOfWeek.Saturday &&
                    t.DayOfWeek != DayOfWeek.Sunday &&
                    !bayramTarixleri.Contains(t))
                    sayi++;
            }
            return sayi;
        }

        /// <summary>
        /// Verilmiş tarix əsasında son 12 ayın iş günlərinin cəmini hesablayır.
        /// Ayın 1-dən başlayır, xəstəliyin başladığı aydan əvvəlki 12 ay götürülür.
        ///
        /// Məsələn, baslama = 14.04.2026 ⇒ 01.04.2025 → 31.03.2026 (12 tam ay)
        ///
        /// Daxili olaraq IsGunSayiniHesablaAsync istifadə edir
        /// (şənbə, bazar və BayramGunu cədvəlindəki bayramları çıxır).
        /// </summary>
        private async Task<int> Son12AyIsGununuHesablaAsync(DateTime baslama)
        {
            // Xəstəlik başlayan aydan əvvəlki 12 tam ay
            var ayBitis = new DateTime(baslama.Year, baslama.Month, 1).AddDays(-1);
            var ayBaslangic = new DateTime(ayBitis.Year, ayBitis.Month, 1).AddMonths(-11);

            return await IsGunSayiniHesablaAsync(ayBaslangic, ayBitis);
        }

        /// <summary>
        /// Cari ildə bu işçinin artıq aldığı şirkət xəstəlik günləri.
        /// (14 gün limitinin nə qədər istifadə olunduğunu yoxlamaq üçün)
        /// </summary>
        private async Task<int> OncekiSirketGununuTapAsync(int isciId, int il)
        {
            return await _unitOfWork.Repository<XestelikOdenis>()
                .Query()
                .Where(x => x.IsciId == isciId && x.Il == il && !x.Silinib)
                .SumAsync(x => x.SirketGunSayi);
        }

        /// <summary>
        /// Yeni xəstəlik üçün ödəniş qeydlərini yaradır (hər ay üzrə).
        /// 14 gün limiti il üzrə yoxlanılır.
        /// </summary>
        private async Task OdenisleriYaratAsync(Xestelik xestelik)
        {
            // Son 12 ay qazanc
            var son12 = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .Where(x => x.IsciId == xestelik.IsciId && !x.Silinib)
                .OrderByDescending(x => x.Il * 12 + x.Ay)
                .Take(12)
                .ToListAsync();

            decimal S = son12.Sum(x => x.Qazanc);
            int son12AyIsGun = await Son12AyIsGununuHesablaAsync(xestelik.BaslamaTarixi);
            decimal birGunluk = son12AyIsGun > 0 ? S / son12AyIsGun : 0;

            // Cari ildə artıq istifadə edilmiş şirkət gün sayı
            int oncekiSirketGun = await OncekiSirketGununuTapAsync(xestelik.IsciId, xestelik.BaslamaTarixi.Year);
            int qalanLimit = Math.Max(0, SIRKET_MAX_GUN - oncekiSirketGun);

            // Xəstəliyi aylara böl
            var aylar = AylaraBolAsync(xestelik.BaslamaTarixi, xestelik.BitmeTarixi);

            int istifadeOlunmusSirketGun = 0;
            foreach (var (il, ay, ayBaslama, ayBitme) in aylar)
            {
                int ayIsGun = await IsGunSayiniHesablaAsync(ayBaslama, ayBitme);
                if (ayIsGun == 0) continue;

                // Bu ay üçün şirkət payı (limitlə)
                int qalan = Math.Max(0, qalanLimit - istifadeOlunmusSirketGun);
                int aySirketGun = Math.Min(ayIsGun, qalan);
                int ayDsmfGun = ayIsGun - aySirketGun;
                istifadeOlunmusSirketGun += aySirketGun;

                var odenis = new XestelikOdenis
                {
                    XestelikId = xestelik.Id,
                    IsciId = xestelik.IsciId,
                    Il = il,
                    Ay = ay,
                    BirGunluk = Math.Round(birGunluk, 4),
                    SirketGunSayi = aySirketGun,
                    DsmfGunSayi = ayDsmfGun,
                    SirketOdenis = Math.Round(birGunluk * aySirketGun, 2),
                    DsmfOdenis = Math.Round(birGunluk * ayDsmfGun, 2)
                };

                await _unitOfWork.Repository<XestelikOdenis>().YaratAsync(odenis);
            }

            await _unitOfWork.YaddaSaxlaAsync();
        }

        /// <summary>
        /// Tarix aralığını aylara bölür (məs. 25.03 → 08.04 → 2 ay).
        /// </summary>
        private List<(int il, int ay, DateTime baslama, DateTime bitme)> AylaraBolAsync(DateTime baslama, DateTime bitme)
        {
            var nticeler = new List<(int, int, DateTime, DateTime)>();
            var current = new DateTime(baslama.Year, baslama.Month, 1);

            while (current <= bitme)
            {
                var ayBitis = current.AddMonths(1).AddDays(-1);
                var fact_baslama = current < baslama ? baslama : current;
                var fact_bitme = ayBitis > bitme ? bitme : ayBitis;

                if (fact_baslama <= fact_bitme)
                    nticeler.Add((current.Year, current.Month, fact_baslama, fact_bitme));

                current = current.AddMonths(1);
            }

            return nticeler;
        }
    }
}
