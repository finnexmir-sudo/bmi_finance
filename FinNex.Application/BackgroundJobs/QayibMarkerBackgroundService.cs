// Infrastructure/BackgroundJobs/QayibMarkerBackgroundService.cs
using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinNex.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Hər gün saat 23:00-dan sonra gün ərzində iş yerinə gəlməyən aktiv işçilər
    /// üçün avtomatik Davamiyyət qeydi yaradır (Status = Qayib). Son 7 günü
    /// nəzərdən keçirir ki, server downtime halında da qeydlər tutulsun.
    ///
    /// Qeyd: Məzuniyyət/Xəstəlik/Ezamiyyət təsdiqləndikdə MezuniyyetService
    /// avtomatik olaraq Qayib → Icazeli/Xestelik/Ezamiyyət çevirir, ona görə
    /// əvvəlcə Qayib yazmaq təhlükəsizdir.
    /// </summary>
    public class QayibMarkerBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QayibMarkerBackgroundService> _logger;

        // Hər saat bir dəfə yoxlayır, amma yalnız 23:00-dan sonra işə düşür.
        private static readonly TimeSpan _interval = TimeSpan.FromHours(1);
        private const int IsBitmeSaati = 23;
        private const int BackfillGunSayi = 7;

        public QayibMarkerBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<QayibMarkerBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("QayibMarkerBackgroundService başladı.");

            // İlk yoxlama 1 dəqiqədən sonra — startup zamanı app tam qalxsın.
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.Now.Hour >= IsBitmeSaati)
                    {
                        await IcraEtAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "QayibMarkerBackgroundService xətası");
                }

                try { await Task.Delay(_interval, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }
        }

        private async Task IcraEtAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var bugun = DateTime.Today;
            var baslanic = bugun.AddDays(-BackfillGunSayi);

            // Backfill dövründəki aktiv işçilər (hal-hazırda aktiv olanlar — köhnə
            // günlərdə də işdə olublarsa onlara Qayib yazılacaq, yoxsa yox).
            var aktivIsciler = await db.Set<Isci>()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv
                         && x.IsheQebulTarixi.Date <= bugun)
                .Select(x => new { x.Id, x.IsheQebulTarixi, x.IsdenAyrilmaTarixi })
                .ToListAsync(ct);

            if (aktivIsciler.Count == 0) return;

            // Backfill dövründə mövcud davamiyyət qeydləri (işçi+tarix cütləri)
            var movcudQeydler = await db.Set<Davamiyyet>()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Tarix.Date >= baslanic && x.Tarix.Date <= bugun)
                .Select(x => new { x.IsciId, Tarix = x.Tarix.Date })
                .ToListAsync(ct);

            var movcudSet = new HashSet<string>(
                movcudQeydler.Select(x => $"{x.IsciId}|{x.Tarix:yyyy-MM-dd}"));

            // Backfill dövründə təsdiqlənmiş saatlıq icazələr — bu günlər Icazeli yazılacaq
            var icazeQeydler = await db.Set<Icaze>()
                .AsNoTracking()
                .Where(x => !x.Silinib &&
                             x.Status == IcazeStatus.Tesdiqlenib &&
                             x.IcazeTarixi.Date >= baslanic &&
                             x.IcazeTarixi.Date <= bugun)
                .Select(x => new { x.IsciId, Tarix = x.IcazeTarixi.Date })
                .ToListAsync(ct);

            var icazeSet = new HashSet<string>(
                icazeQeydler.Select(x => $"{x.IsciId}|{x.Tarix:yyyy-MM-dd}"));

            var indi = DateTime.Now;
            var yeniQeydler = new List<Davamiyyet>();

            // Son 7 gün (daxil olmaqla bu gün — amma bu gün üçün yalnız 23:00-dan sonra)
            for (var gun = baslanic; gun <= bugun; gun = gun.AddDays(1))
            {
                // Həftəsonu — istər iş günü olmasın deyə yoxla (sadəcə Şən/Baz keç)
                // Qeyd: BayramGunleri cədvəlinə görə dəqiq yoxlama lazımdırsa sonra
                //       genişləndirilə bilər. Hazırda bu sadə qayda kifayətdir.
                if (gun.DayOfWeek == DayOfWeek.Saturday || gun.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                foreach (var isci in aktivIsciler)
                {
                    // İşçi bu tarixdə işdə deyildisə (işə qəbuldan əvvəl / ayrılandan sonra)
                    if (isci.IsheQebulTarixi.Date > gun) continue;
                    if (isci.IsdenAyrilmaTarixi.HasValue && isci.IsdenAyrilmaTarixi.Value.Date < gun) continue;

                    var key = $"{isci.Id}|{gun:yyyy-MM-dd}";
                    if (movcudSet.Contains(key)) continue;

                    // Həmin gün üçün təsdiqlənmiş icazə varsa Icazeli yaz, Qayib yox
                    var status = icazeSet.Contains(key)
                        ? DavamiyyetStatus.Icazeli
                        : DavamiyyetStatus.Qayib;

                    yeniQeydler.Add(new Davamiyyet
                    {
                        IsciId = isci.Id,
                        Tarix = gun,
                        GirisVaxti = null,
                        CixisVaxti = null,
                        Status = status,
                        YaradilmaTarixi = indi,
                        Silinib = false
                    });
                }
            }

            if (yeniQeydler.Count == 0) return;

            await db.Set<Davamiyyet>().AddRangeAsync(yeniQeydler, ct);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("QayibMarker: {Count} Qayib qeydi yaradıldı", yeniQeydler.Count);
        }
    }
}
