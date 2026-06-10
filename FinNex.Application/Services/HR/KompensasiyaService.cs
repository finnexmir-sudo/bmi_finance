using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Kompensasiya;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// İstifadə edilməmiş əmək məzuniyyəti günlərinə görə kompensasiya
    /// hesablama servisi. Adi məzuniyyət ödənişi düsturu ilə eyni
    /// (MAX(S/12/30.4, CariMaas/AyIsGun)) gündəlik dərəcəni istifadə edir.
    /// Fərq yalnız günlərin sayında: keçmiş illərin qalığı + cari il prorate.
    /// </summary>
    public class KompensasiyaService : IKompensasiyaService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMaasHesablamaService _hesablamaService;

        public KompensasiyaService(IUnitOfWork uow, IMaasHesablamaService hesablamaService)
        {
            _uow = uow;
            _hesablamaService = hesablamaService;
        }

        // ─── HESABLA (preview) ───────────────────────────────────
        public async Task<Result<KompensasiyaHesablamaNeticesiDto>> HesablaAsync(
            int isciId, DateTime ayrilmaTarixi)
        {
            try
            {
                var isci = await _uow.Repository<Isci>()
                    .Query()
                    .Where(x => x.Id == isciId && !x.Silinib)
                    .Include(x => x.Maliye)
                    .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Departament)
                    .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Vezife)
                    .FirstOrDefaultAsync();

                if (isci == null)
                    return Result<KompensasiyaHesablamaNeticesiDto>.Fail("İşçi tapılmadı.");

                var result = new KompensasiyaHesablamaNeticesiDto
                {
                    IsciId = isciId,
                    IsciAdSoyad = isci.TamAd,
                    IseQebulTarixi = isci.IsheQebulTarixi,
                    AyrilmaTarixi = ayrilmaTarixi.Date,
                    HesablananIl = ayrilmaTarixi.Year,
                    HesablananAy = ayrilmaTarixi.Month,
                    CariMaas = isci.Maliye?.CariMaas ?? 0,
                    CariIl = ayrilmaTarixi.Year
                };

                var aktivTeyinat = isci.IsciTeyinatlari.FirstOrDefault(t => t.Aktivdir);
                result.DepartamentAd = aktivTeyinat?.Departament?.Ad;
                result.VezifeAd = aktivTeyinat?.Vezife?.Ad;

                // ─── 1. MEZUNIYYET BALANSI (anniversary illər üzrə) ──
                // MezuniyyetBalans.Il anniversary ilini bildirir, takvim deyil.
                // Misal: işə qəbul 01.02.2010 → "Il=2025" anniversary ili
                // 01.02.2025–31.01.2026-ya uyğundur.
                var balanslar = await _uow.Repository<MezuniyyetBalans>()
                    .Query()
                    .Where(x => x.IsciId == isciId
                             && x.Nov == MezuniyyetNovu.Illik
                             && !x.Silinib)
                    .OrderBy(x => x.Il)
                    .ToListAsync();

                // ─── 2. CARI ANNIVERSARY İLİNİ TAP ──────────────────
                // İşçinin işə qəbul gün/ay-ı hər il anniversary tarixini formalaşdırır.
                // Cari anniversary = ən son keçən anniversary tarixinin baş düşdüyü il.
                //
                // Misal: işə qəbul 01.02.2010, ayrılma 24.05.2026
                //   → bu il anniversary 01.02.2026, keçib → cariAnniversaryIli = 2026
                //
                // Misal: işə qəbul 01.06.2010, ayrılma 24.05.2026
                //   → bu il anniversary 01.06.2026, hələ gəlməyib
                //   → son anniversary 01.06.2025 → cariAnniversaryIli = 2025
                var anniversaryBuIl = new DateTime(
                    ayrilmaTarixi.Year, isci.IsheQebulTarixi.Month, isci.IsheQebulTarixi.Day);
                int cariAnniversaryIli;
                DateTime anniversaryBaslangic;
                if (anniversaryBuIl <= ayrilmaTarixi.Date)
                {
                    cariAnniversaryIli = ayrilmaTarixi.Year;
                    anniversaryBaslangic = anniversaryBuIl;
                }
                else
                {
                    cariAnniversaryIli = ayrilmaTarixi.Year - 1;
                    anniversaryBaslangic = new DateTime(
                        cariAnniversaryIli, isci.IsheQebulTarixi.Month, isci.IsheQebulTarixi.Day);
                }

                // Yeni işçi (heç bir anniversary keçməyib) — prorate işə qəbul tarixindən
                if (isci.IsheQebulTarixi.Date > anniversaryBaslangic)
                {
                    anniversaryBaslangic = isci.IsheQebulTarixi.Date;
                    result.SonuncuMezuniyyetIsheQebulTarixindenGoturuldu = true;
                }

                result.CariIl = cariAnniversaryIli;
                result.AnniversaryBaslangic = anniversaryBaslangic;

                // ─── 3. KEÇMİŞ İLLƏR QALIĞI ─────────────────────────
                // Anniversary ili < cariAnniversaryIli olan bütün balans qalığı
                decimal kecmisQalig = 0;
                foreach (var b in balanslar.Where(x => x.Il < cariAnniversaryIli))
                {
                    decimal qalig = b.ToplamGun - b.IstifadeOlunanGun;
                    if (qalig > 0) kecmisQalig += qalig;
                    result.KecmisIller.Add(new KompensasiyaIlDto
                    {
                        Il = b.Il,
                        ToplamGun = b.ToplamGun,
                        IstifadeOlunanGun = b.IstifadeOlunanGun,
                        QaligGun = qalig
                    });
                }
                result.KecmisQaligGun = kecmisQalig;

                // Cari anniversary ili balansı
                var cariIlBalans = balanslar.FirstOrDefault(x => x.Il == cariAnniversaryIli);
                decimal illikGunHuququ = cariIlBalans?.ToplamGun ?? 21;
                result.CariIlToplamGun = illikGunHuququ;
                result.CariIlIstifadeOlunan = cariIlBalans?.IstifadeOlunanGun ?? 0;

                // ─── 4. SONUNCU MEZUNIYYET (informativ — istifadə olunmur) ───
                var sonMezuniyyet = await _uow.Repository<Mezuniyyet>()
                    .Query()
                    .Where(x => x.IsciId == isciId
                             && !x.Silinib
                             && x.Status == MezuniyyetStatus.Tesdiqlenib
                             && x.BitmeTarixi <= ayrilmaTarixi)
                    .OrderByDescending(x => x.BitmeTarixi)
                    .FirstOrDefaultAsync();

                if (sonMezuniyyet != null)
                {
                    result.SonuncuMezuniyyetBitmeTarixi = sonMezuniyyet.BitmeTarixi.Date;
                    result.SonuncuMezuniyyetId = sonMezuniyyet.Id;
                }

                // ─── 5. CARI ANNIVERSARY PRORATE ────────────────────
                // Cari anniversary başlanğıcından ayrılma tarixinə qədər keçən
                // günə görə cari il günü hüququ prorate olunur.
                // Cari ildə artıq istifadə edilən günlər çıxılır
                // (köhnə illərdən qalanlar onsuzda kecmisQalig-a daxil deyil).
                int kecenGun = (ayrilmaTarixi.Date - anniversaryBaslangic).Days;
                if (kecenGun < 0) kecenGun = 0;
                result.KecenGunSayi = kecenGun;

                decimal cariIlProrate = Math.Round(
                    (decimal)kecenGun / 365m * illikGunHuququ - result.CariIlIstifadeOlunan,
                    2);
                if (cariIlProrate < 0) cariIlProrate = 0;
                result.CariIlProrateGun = cariIlProrate;

                result.CemiKompensasiyaGun = result.KecmisQaligGun + cariIlProrate;

                // ─── 4. GUNLUK RATE (məzuniyyət düsturu ilə eyni) ───
                //   S = son 12 ay düzəlmiş qazanc cəmi (ayrılma ayından əvvəlki 12 ay)
                //   gunlukMezPul = S / 12 / 30.4
                //   gunlukMaas   = CariMaas / ayIsGun (ayrılma ayının iş günü sayı)
                //   gunlukRate   = MAX(gunlukMezPul, gunlukMaas)
                int baslamaRefKey = ayrilmaTarixi.Year * 12 + ayrilmaTarixi.Month;
                var son12Qazanc = await _uow.Repository<IsciAyliqQazanc>()
                    .Query()
                    .Where(x => x.IsciId == isciId
                             && !x.Silinib
                             && (x.Il * 12 + x.Ay) < baslamaRefKey)
                    .OrderByDescending(x => x.Il * 12 + x.Ay)
                    .Take(12)
                    .ToListAsync();

                result.Son12AyCemQazanc = son12Qazanc.Sum(x => x.Qazanc);
                result.Son12AyQeydSayi = son12Qazanc.Count;

                // Artım əmsalı (K_i) — köhnə qazancları cari maaş səviyyəsinə qaldır
                decimal sDuzelmis = 0;
                foreach (var q in son12Qazanc.OrderBy(x => x.Il).ThenBy(x => x.Ay))
                {
                    var ayBitis = new DateTime(q.Il, q.Ay, 1).AddMonths(1).AddDays(-1);
                    decimal statMaas = await _hesablamaService
                        .StatMaasiTarixeGoreTapAsync(isciId, ayBitis);
                    if (statMaas <= 0) statMaas = result.CariMaas;

                    decimal emsal = (statMaas > 0 && result.CariMaas > 0)
                        ? result.CariMaas / statMaas : 1m;
                    if (emsal < 1m) emsal = 1m;

                    sDuzelmis += Math.Round(q.Qazanc * emsal, 2);
                }
                result.Son12AyDuzelmisQazanc = sDuzelmis;

                int ayIsGun = await _hesablamaService.AyIsGunSayiniHesablaAsync(
                    ayrilmaTarixi.Year, ayrilmaTarixi.Month);

                decimal gunlukMezPul = sDuzelmis > 0
                    ? Math.Round(sDuzelmis / 12m / 30.4m, 4)
                    : 0m;
                decimal gunlukMaas = (result.CariMaas > 0 && ayIsGun > 0)
                    ? Math.Round(result.CariMaas / ayIsGun, 4)
                    : 0m;
                decimal gunlukRate = Math.Max(gunlukMezPul, gunlukMaas);
                string qalib = gunlukMezPul >= gunlukMaas ? "MH" : "ƏH";

                result.GunlukMezPul = gunlukMezPul;
                result.GunlukMaas = gunlukMaas;
                result.GunlukRate = gunlukRate;
                result.Qalib = qalib;

                // ─── 5. CEMI MEBLEG ─────────────────────────────────
                result.CemiMebleg = Math.Round(result.CemiKompensasiyaGun * gunlukRate, 2);

                // ─── 6. IZAHAT ─────────────────────────────────────
                result.Izahatlar.Add($"İşçi: {isci.TamAd}");
                result.Izahatlar.Add($"İşə qəbul: {isci.IsheQebulTarixi:dd.MM.yyyy}, Ayrılma: {ayrilmaTarixi:dd.MM.yyyy}");
                result.Izahatlar.Add(
                    $"Cari məzuniyyət ili başlanğıcı (anniversary): " +
                    $"{anniversaryBaslangic:dd.MM.yyyy} — bu tarixdən ayrılmaya qədər " +
                    $"{kecenGun} gün keçib");
                result.Izahatlar.Add($"Keçmiş anniversary illərdən qalıq cəmi: {kecmisQalig:N2} gün");

                if (sonMezuniyyet != null)
                {
                    result.Izahatlar.Add(
                        $"(İnformativ) Sonuncu məzuniyyət: {sonMezuniyyet.BaslamaTarixi:dd.MM.yyyy} – " +
                        $"{sonMezuniyyet.BitmeTarixi:dd.MM.yyyy} — köhnə illərdən qalan günlərdən artıq azalıb, " +
                        $"prorate-ə təsir etmir.");
                }

                result.Izahatlar.Add(
                    $"Cari il prorate: ({kecenGun} / 365) × {illikGunHuququ:N0} " +
                    $"− {result.CariIlIstifadeOlunan:N0} (cari ildə istifadə) = {cariIlProrate:N2} gün");
                result.Izahatlar.Add(
                    $"CƏMİ KOMPENSASİYA GÜNLƏRİ: {kecmisQalig:N2} + {cariIlProrate:N2} = " +
                    $"{result.CemiKompensasiyaGun:N2} gün");

                result.Izahatlar.Add(
                    $"Son 12 ayın faktiki qazancı: {result.Son12AyCemQazanc:N2} ₼ " +
                    $"({result.Son12AyQeydSayi} qeyd)");
                result.Izahatlar.Add(
                    $"Artım əmsalı tətbiq edilmiş cəm (S): {sDuzelmis:N2} ₼");
                result.Izahatlar.Add(
                    $"Gündəlik məzuniyyət pulu (MH): {sDuzelmis:N2} ÷ 12 ÷ 30.4 = {gunlukMezPul:N4} ₼");
                result.Izahatlar.Add(
                    $"Gündəlik maaş (ƏH): {result.CariMaas:N2} ÷ {ayIsGun} = {gunlukMaas:N4} ₼");
                result.Izahatlar.Add(
                    $"Seçilən gündəlik dərəcə: MAX(MH, ƏH) = {gunlukRate:N4} ₼ ({qalib} qalib)");
                result.Izahatlar.Add(
                    $"YEKUN MƏBLƏĞ: {result.CemiKompensasiyaGun:N2} gün × {gunlukRate:N4} ₼ = " +
                    $"{result.CemiMebleg:N2} ₼");

                // Xəbərdarlıqlar
                if (son12Qazanc.Count < 12)
                {
                    result.Xeberdarliqlar.Add(
                        $"Yalnız {son12Qazanc.Count}/12 ay üçün qazanc qeydi tapıldı — " +
                        $"hesablama dəqiq olmaya bilər.");
                }
                if (result.CemiKompensasiyaGun == 0)
                {
                    result.Xeberdarliqlar.Add(
                        "Kompensasiya günü 0-dır — bu işçi üçün kompensasiya hesablanmır.");
                }
                if (kecenGun > 366)
                {
                    result.Xeberdarliqlar.Add(
                        $"Sonuncu məzuniyyətdən bəri {kecenGun} gün keçib — " +
                        $"bu, normal il limitindən artıqdır. Mənbə məlumatları yoxlayın.");
                }

                return Result<KompensasiyaHesablamaNeticesiDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return Result<KompensasiyaHesablamaNeticesiDto>.Fail(
                    $"Kompensasiya hesablanarkən xəta: {ex.Message}");
            }
        }

        // ─── YARAT (DB-yə yaz) ───────────────────────────────────
        public async Task<Result<int>> YaratAsync(KompensasiyaYaratDto dto, int hesablayanIsciId)
        {
            try
            {
                // Eyni il/ay üçün aktiv qeyd varsa qadağa
                var movcud = await _uow.Repository<MezuniyyetKompensasiyasi>()
                    .Query()
                    .AnyAsync(x => x.IsciId == dto.IsciId
                                && x.HesablananIl == dto.HesablananIl
                                && x.HesablananAy == dto.HesablananAy
                                && x.Status != KompensasiyaStatus.LegvEdildi
                                && !x.Silinib);
                if (movcud)
                    return Result<int>.Fail(
                        "Bu işçi üçün seçilmiş ay-da artıq aktiv kompensasiya qeydi var. " +
                        "Əvvəlcə onu ləğv edin.");

                // Hesabla
                var calc = await HesablaAsync(dto.IsciId, dto.AyrilmaTarixi);
                if (!calc.Success || calc.Data == null)
                    return Result<int>.Fail(calc.Message ?? "Hesablama uğursuz.");

                var c = calc.Data;

                // Manual override (qismi kompensasiya) — HR yalnız müəyyən gün sayını
                // kompensasiya etmək istəsə (məs. işçi aktiv qalır, qalan günlərin bir
                // hissəsini pulla alır). Boş buraxılsa avtomatik (tam) hesablanır.
                // Gündəlik rate eyni qalır, yalnız gün sayı dəyişir.
                decimal cemiGun = c.CemiKompensasiyaGun;
                decimal cemiMebleg = c.CemiMebleg;
                string? autoOverrideQeyd = null;
                if (dto.ManualGunSayi.HasValue && dto.ManualGunSayi.Value > 0)
                {
                    if (dto.ManualGunSayi.Value > c.CemiKompensasiyaGun)
                        return Result<int>.Fail(
                            $"Qismi gün ({dto.ManualGunSayi.Value:N2}) mövcud kompensasiya " +
                            $"günündən ({c.CemiKompensasiyaGun:N2}) çox ola bilməz.");

                    cemiGun = dto.ManualGunSayi.Value;
                    cemiMebleg = Math.Round(cemiGun * c.GunlukRate, 2);
                    autoOverrideQeyd =
                        $"Manual override: {cemiGun:N2} gün × {c.GunlukRate:N4} ₼/gün = {cemiMebleg:N2} ₼ " +
                        $"(avtomatik hesablanan {c.CemiKompensasiyaGun:N2} gün override olundu).";
                }

                var entity = new MezuniyyetKompensasiyasi
                {
                    IsciId = dto.IsciId,
                    AyrilmaTarixi = dto.AyrilmaTarixi.Date,
                    SonuncuMezuniyyetBitmeTarixi = c.SonuncuMezuniyyetBitmeTarixi,
                    SonuncuMezuniyyetId = c.SonuncuMezuniyyetId,
                    KecenGunSayi = c.KecenGunSayi,
                    KecmisQaligGun = c.KecmisQaligGun,
                    CariIlProrateGun = c.CariIlProrateGun,
                    CemiKompensasiyaGun = cemiGun,
                    Son12AyDuzelmisQazanc = c.Son12AyDuzelmisQazanc,
                    GunlukMezPul = c.GunlukMezPul,
                    GunlukMaas = c.GunlukMaas,
                    GunlukRate = c.GunlukRate,
                    CemiMebleg = cemiMebleg,
                    HesablananIl = dto.HesablananIl,
                    HesablananAy = dto.HesablananAy,
                    Status = KompensasiyaStatus.Layihe,
                    Qeyd = string.IsNullOrWhiteSpace(autoOverrideQeyd)
                        ? dto.Qeyd?.Trim()
                        : (string.IsNullOrWhiteSpace(dto.Qeyd)
                            ? autoOverrideQeyd
                            : dto.Qeyd.Trim() + Environment.NewLine + autoOverrideQeyd),
                    HesablayanIsciId = hesablayanIsciId,
                    YaradanIcraciId = hesablayanIsciId,
                    YaradilmaTarixi = DateTime.Now
                };

                await _uow.Repository<MezuniyyetKompensasiyasi>().YaratAsync(entity);

                // Yaradılan kimi kompensasiya günləri məzuniyyət balansından çıxılır
                // (FIFO — ən köhnə il əvvəl). Beləcə işçi həm günü saxlayıb, həm
                // pulunu ala bilməz. Balans tam ədəddir, ona görə yuvarlaqlaşdırılır.
                int cixilacaqGun = (int)Math.Round(cemiGun, MidpointRounding.AwayFromZero);
                await BalansdanCixAsync(dto.IsciId, cixilacaqGun);
                entity.BalansdanCixilib = cixilacaqGun > 0;

                await _uow.YaddaSaxlaAsync();   // entity + balans birlikdə commit olunur

                return Result<int>.Ok(entity.Id,
                    "Kompensasiya hesablandı, yadda saxlandı və balansdan çıxıldı.");
            }
            catch (Exception ex)
            {
                return Result<int>.Fail($"Yadda saxlama zamanı xəta: {ex.Message}");
            }
        }

        // ─── SIYAHI ─────────────────────────────────────────────
        public async Task<Result<IList<KompensasiyaListDto>>> HamisiniGetirAsync()
        {
            try
            {
                var list = await _uow.Repository<MezuniyyetKompensasiyasi>()
                    .Query()
                    .Where(x => !x.Silinib)
                    .Include(x => x.Isci)
                    .OrderByDescending(x => x.YaradilmaTarixi)
                    .Select(x => new KompensasiyaListDto
                    {
                        Id = x.Id,
                        IsciId = x.IsciId,
                        IsciAdSoyad = x.Isci.Ad + " " + x.Isci.Soyad,
                        AyrilmaTarixi = x.AyrilmaTarixi,
                        HesablananIl = x.HesablananIl,
                        HesablananAy = x.HesablananAy,
                        CemiKompensasiyaGun = x.CemiKompensasiyaGun,
                        CemiMebleg = x.CemiMebleg,
                        Status = x.Status,
                        YaradilmaTarixi = x.YaradilmaTarixi,
                        MaasId = x.MaasId
                    })
                    .ToListAsync();
                return Result<IList<KompensasiyaListDto>>.Ok(list);
            }
            catch (Exception ex)
            {
                return Result<IList<KompensasiyaListDto>>.Fail($"Siyahı yüklənmədi: {ex.Message}");
            }
        }

        // ─── DETAL ──────────────────────────────────────────────
        public async Task<Result<KompensasiyaDetalDto?>> IdIleGetirAsync(int id)
        {
            try
            {
                var e = await _uow.Repository<MezuniyyetKompensasiyasi>()
                    .Query()
                    .Where(x => x.Id == id && !x.Silinib)
                    .Include(x => x.Isci).ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Departament)
                    .Include(x => x.Isci).ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Vezife)
                    .FirstOrDefaultAsync();
                if (e == null)
                    return Result<KompensasiyaDetalDto?>.Fail("Qeyd tapılmadı.");

                // Detal preview-i təkrar hesablayırıq ki, breakdown göstərə bilək
                var calc = await HesablaAsync(e.IsciId, e.AyrilmaTarixi);
                var dto = new KompensasiyaDetalDto
                {
                    Id = e.Id,
                    Status = e.Status,
                    Qeyd = e.Qeyd,
                    MaasId = e.MaasId,
                    YaradilmaTarixi = e.YaradilmaTarixi,
                    IsciId = e.IsciId,
                    IsciAdSoyad = calc.Data?.IsciAdSoyad ?? e.Isci.Ad + " " + e.Isci.Soyad,
                    DepartamentAd = calc.Data?.DepartamentAd,
                    VezifeAd = calc.Data?.VezifeAd,
                    IseQebulTarixi = e.Isci.IsheQebulTarixi,
                    AyrilmaTarixi = e.AyrilmaTarixi,
                    SonuncuMezuniyyetBitmeTarixi = e.SonuncuMezuniyyetBitmeTarixi,
                    SonuncuMezuniyyetId = e.SonuncuMezuniyyetId,
                    KecenGunSayi = e.KecenGunSayi,
                    KecmisQaligGun = e.KecmisQaligGun,
                    CariIlProrateGun = e.CariIlProrateGun,
                    CemiKompensasiyaGun = e.CemiKompensasiyaGun,
                    Son12AyDuzelmisQazanc = e.Son12AyDuzelmisQazanc,
                    GunlukMezPul = e.GunlukMezPul,
                    GunlukMaas = e.GunlukMaas,
                    GunlukRate = e.GunlukRate,
                    Qalib = calc.Data?.Qalib ?? "MH",
                    CemiMebleg = e.CemiMebleg,
                    HesablananIl = e.HesablananIl,
                    HesablananAy = e.HesablananAy,
                    CariIl = calc.Data?.CariIl ?? 0,
                    AnniversaryBaslangic = calc.Data?.AnniversaryBaslangic ?? default,
                    CariIlToplamGun = calc.Data?.CariIlToplamGun ?? 0,
                    CariIlIstifadeOlunan = calc.Data?.CariIlIstifadeOlunan ?? 0,
                    SonuncuMezuniyyetIsheQebulTarixindenGoturuldu = calc.Data?.SonuncuMezuniyyetIsheQebulTarixindenGoturuldu ?? false,
                    Izahatlar = calc.Data?.Izahatlar ?? new List<string>(),
                    Xeberdarliqlar = calc.Data?.Xeberdarliqlar ?? new List<string>(),
                    KecmisIller = calc.Data?.KecmisIller ?? new List<KompensasiyaIlDto>()
                };
                return Result<KompensasiyaDetalDto?>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<KompensasiyaDetalDto?>.Fail($"Detal yüklənmədi: {ex.Message}");
            }
        }

        // ─── LEGV ───────────────────────────────────────────────
        public async Task<Result> LegvEtAsync(int id)
        {
            try
            {
                var e = await _uow.Repository<MezuniyyetKompensasiyasi>()
                    .Query().FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
                if (e == null) return Result.Fail("Qeyd tapılmadı.");
                if (e.Status == KompensasiyaStatus.MaasaDaxilEdildi)
                    return Result.Fail(
                        "Bu kompensasiya artıq maaşa daxil edilib — ləğv etmək olmaz.");

                e.Status = KompensasiyaStatus.LegvEdildi;
                e.YenilenmeTarixi = DateTime.Now;
                await _uow.Repository<MezuniyyetKompensasiyasi>().YenileAsync(e);

                // Ləğv — yalnız yaradılanda balansdan çıxılmış qeydlər üçün günləri
                // geri qaytar (ən yeni il əvvəl — çıxmanın əksinə). Köhnə qeydlər
                // (BalansdanCixilib=false) fantom qaytarma verməsin.
                string mesaj = "Kompensasiya ləğv edildi.";
                if (e.BalansdanCixilib)
                {
                    int qaytarilacaqGun = (int)Math.Round(e.CemiKompensasiyaGun, MidpointRounding.AwayFromZero);
                    await BalansaGeriQaytarAsync(e.IsciId, qaytarilacaqGun);
                    e.BalansdanCixilib = false;
                    mesaj = "Kompensasiya ləğv edildi, günlər balansa qaytarıldı.";
                }

                await _uow.YaddaSaxlaAsync();
                return Result.Ok(mesaj);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Ləğv zamanı xəta: {ex.Message}");
            }
        }

        // ─── MAAŞ ENGINE ÜÇÜN ─────────────────────────────────────
        public async Task<MezuniyyetKompensasiyasi?> GetAktivKompensasiyaAsync(int isciId, int il, int ay)
        {
            return await _uow.Repository<MezuniyyetKompensasiyasi>()
                .Query()
                .FirstOrDefaultAsync(x => x.IsciId == isciId
                                       && x.HesablananIl == il
                                       && x.HesablananAy == ay
                                       && !x.Silinib
                                       && (x.Status == KompensasiyaStatus.Layihe
                                        || x.Status == KompensasiyaStatus.Tesdiqlenib));
        }

        public async Task IsareLamasiniYadda(int kompensasiyaId, int maasId)
        {
            var e = await _uow.Repository<MezuniyyetKompensasiyasi>()
                .Query().FirstOrDefaultAsync(x => x.Id == kompensasiyaId);
            if (e == null) return;
            e.Status = KompensasiyaStatus.MaasaDaxilEdildi;
            e.MaasId = maasId;
            e.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<MezuniyyetKompensasiyasi>().YenileAsync(e);
            await _uow.YaddaSaxlaAsync();
        }

        // ─── BALANS: ÇIX (FIFO — ən köhnə il əvvəl) ──────────────
        // İllik məzuniyyət balansından `gun` qədər çıxır (IstifadeOlunanGun artırır).
        // MezuniyyetService-dəki məzuniyyət düşmə məntiqi ilə eyni qaydadır.
        // Commit etmir — çağıran YaddaSaxlaAsync edir.
        private async Task BalansdanCixAsync(int isciId, int gun)
        {
            if (gun <= 0) return;
            var repo = _uow.Repository<MezuniyyetBalans>();
            var balanslar = await repo.Query()
                .Where(b => !b.Silinib && b.IsciId == isciId
                         && b.Nov == MezuniyyetNovu.Illik
                         && (b.ToplamGun - b.IstifadeOlunanGun) > 0)
                .OrderBy(b => b.Il)
                .ToListAsync();

            int qalan = gun;
            foreach (var b in balanslar)
            {
                if (qalan <= 0) break;
                int ilinQaligi = b.ToplamGun - b.IstifadeOlunanGun;
                int kesilecek = Math.Min(ilinQaligi, qalan);
                b.IstifadeOlunanGun += kesilecek;
                b.YenilenmeTarixi = DateTime.Now;
                await repo.YenileAsync(b);
                qalan -= kesilecek;
            }
            // qalan > 0 qalsa (balans çatmasa) — borc yazılmır; kompensasiya
            // onsuz da mövcud qalıqdan çox ola bilməz (YaratAsync-də validasiya var).
        }

        // ─── BALANS: GERİ QAYTAR (ən yeni il əvvəl — çıxmanın əksinə) ──
        // Ləğv zamanı çıxılan günləri geri verir (IstifadeOlunanGun azaldır).
        private async Task BalansaGeriQaytarAsync(int isciId, int gun)
        {
            if (gun <= 0) return;
            var repo = _uow.Repository<MezuniyyetBalans>();
            var balanslar = await repo.Query()
                .Where(b => !b.Silinib && b.IsciId == isciId
                         && b.Nov == MezuniyyetNovu.Illik
                         && b.IstifadeOlunanGun > 0)
                .OrderByDescending(b => b.Il)
                .ToListAsync();

            int qalan = gun;
            foreach (var b in balanslar)
            {
                if (qalan <= 0) break;
                int qaytarila = Math.Min(b.IstifadeOlunanGun, qalan);
                b.IstifadeOlunanGun -= qaytarila;
                b.YenilenmeTarixi = DateTime.Now;
                await repo.YenileAsync(b);
                qalan -= qaytarila;
            }
        }
    }
}
