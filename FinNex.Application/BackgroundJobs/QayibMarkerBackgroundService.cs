// Infrastructure/BackgroundJobs/QayibMarkerBackgroundService.cs
using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.Communication;
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

            // Backfill dövründəki bayram/iş günü overrideları
            var xususiGunler = await db.Set<BayramGunu>()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Tarix.Date >= baslanic && x.Tarix.Date <= bugun)
                .Select(x => new { Tarix = x.Tarix.Date, x.Tip })
                .ToListAsync(ct);

            var xususiDict = xususiGunler
                .GroupBy(x => x.Tarix)
                .ToDictionary(g => g.Key, g => g.First().Tip);

            // Backfill dövründə mövcud davamiyyət qeydləri (işçi+tarix cütləri).
            //
            // SİLİNMİŞLƏR DƏ GƏTİRİLİR (14.08.2026) — və bu, kritikdir:
            // `Davamiyyet`-də (IsciId, Tarix) üzrə UNİKAL indeks var və o, `Silinib`-i
            // FİLTRLƏMİR (AppDbContext:760). Yalnız aktiv qeydlərə baxsaydıq,
            // yumşaq silinmiş günə YENİ sətir INSERT etməyə çalışardıq və unikal
            // indeks pozulardı. AddRange + tək SaveChanges olduğu üçün bu, həmin
            // gedişdə BÜTÜN Qayıb qeydlərini uçurardı — üstəlik səssizcə, çünki
            // istisna yuxarıda tutulub yalnız loga yazılır.
            //
            // Belə silinmiş sətirlər real yaranır: məzuniyyət ləğv olunanda onun
            // davamiyyət izləri yumşaq silinir (HrLegvEtAsync / AdminLegvEtAsync).
            // Admin ləğvi məhz KEÇMİŞ günlərə işlədiyi üçün nəticəsi demək olar ki,
            // həmişə bu 7 günlük pəncərəyə düşür.
            //
            // Silinmiş sətri təkrar INSERT etmək əvəzinə DİRİLDİRİK: gün faktiki
            // olaraq qeydsizdir, ona Qayıb yazılmalıdır. (Qayıb özü maaşa təsir
            // etmir — kəsinti üçün `MaasdanKes` ayrıca işarələnməlidir.)
            var pencereQeydleri = await db.Set<Davamiyyet>()
                .AsNoTracking()
                .Where(x => x.Tarix.Date >= baslanic && x.Tarix.Date <= bugun)
                .Select(x => new { x.Id, x.IsciId, Tarix = x.Tarix.Date, x.Silinib })
                .ToListAsync(ct);

            // Aktiv qeyd var → toxunma
            var movcudSet = new HashSet<string>(
                pencereQeydleri.Where(x => !x.Silinib)
                               .Select(x => $"{x.IsciId}|{x.Tarix:yyyy-MM-dd}"));

            // Yalnız silinmiş qeyd var → yenisini INSERT etmə, mövcudu dirilt
            var silinmisDict = pencereQeydleri
                .Where(x => x.Silinib)
                .Select(x => new { Key = $"{x.IsciId}|{x.Tarix:yyyy-MM-dd}", x.Id })
                .Where(x => !movcudSet.Contains(x.Key))
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.First().Id);

            var dirildilecekler = new List<(int Id, DavamiyyetStatus Status)>();

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

            // Backfill dövründəki offline görüş iştirakçıları — Qayib yerinə Icazeli yazılacaq
            var gorushIshtirakcilar = await db.GorushIshtirakcilar
                .AsNoTracking()
                .Where(x => !x.Silinib
                         && x.Gorush.Nov == GorushNovu.Offline
                         && x.Gorush.Status != GorushStatus.LegvEdildi
                         && x.Gorush.Tarix.Date >= baslanic
                         && x.Gorush.Tarix.Date <= bugun
                         && x.Status != IshtirakciStatus.Redd
                         && x.Status != IshtirakciStatus.IshtiraketmeyecekBildirib)
                .Select(x => new { x.IsciId, Tarix = x.Gorush.Tarix.Date })
                .ToListAsync(ct);

            var gorushSet = new HashSet<string>(
                gorushIshtirakcilar.Select(x => $"{x.IsciId}|{x.Tarix:yyyy-MM-dd}"));

            // Backfill dövrünü örtən təsdiqlənmiş EZAMİYYƏTLƏR — bu günlər Qayib YOX,
            // Ezamiyyet yazılacaq (tam günlük ezamiyyətdə cihaz qeydi olmaması normaldır;
            // əvvəllər yalnız icazə/görüş istisna idi və ezamiyyətli işçi Qayib düşürdü).
            var ezamiyyetSet = new HashSet<string>();
            var ezamiyyetler = await db.Set<EzamiyyetMuraciet>()
                .AsNoTracking()
                .Where(x => !x.Silinib &&
                             x.Status == EzamiyyetStatus.Tesdiqlendi &&
                             x.BaslamaTarixi.Date <= bugun &&
                             x.BitmeTarixi.Date >= baslanic)
                .Select(x => new { x.IsciId, Bas = x.BaslamaTarixi.Date, Bit = x.BitmeTarixi.Date })
                .ToListAsync(ct);
            foreach (var e in ezamiyyetler)
            {
                var d1 = e.Bas < baslanic ? baslanic : e.Bas;
                var d2 = e.Bit > bugun ? bugun : e.Bit;
                for (var d = d1; d <= d2; d = d.AddDays(1))
                    ezamiyyetSet.Add($"{e.IsciId}|{d:yyyy-MM-dd}");
            }

            var indi = DateTime.Now;
            var yeniQeydler = new List<Davamiyyet>();

            // Son 7 gün (daxil olmaqla bu gün — amma bu gün üçün yalnız 23:00-dan sonra)
            for (var gun = baslanic; gun <= bugun; gun = gun.AddDays(1))
            {
                // İş günü yoxlaması: BayramGunu cədvəlinə bax, sonra həftəsonu qaydasına
                if (xususiDict.TryGetValue(gun.Date, out var gunTipi))
                {
                    // Bayram olaraq işarələnib → iş günü deyil, Qayib yazma
                    if (gunTipi == GunTipi.Bayram) continue;
                    // GunTipi.IsGunu → həftəsonu olsa belə iş günüdür, davam et
                }
                else
                {
                    // Cədvəldə yoxdur — standart qayda: Şənbə/Bazar iş günü deyil
                    if (gun.DayOfWeek == DayOfWeek.Saturday || gun.DayOfWeek == DayOfWeek.Sunday)
                        continue;
                }

                foreach (var isci in aktivIsciler)
                {
                    // İşçi bu tarixdə işdə deyildisə (işə qəbuldan əvvəl / ayrılandan sonra)
                    if (isci.IsheQebulTarixi.Date > gun) continue;
                    if (isci.IsdenAyrilmaTarixi.HasValue && isci.IsdenAyrilmaTarixi.Value.Date < gun) continue;

                    var key = $"{isci.Id}|{gun:yyyy-MM-dd}";
                    if (movcudSet.Contains(key)) continue;

                    // Həmin gün ezamiyyət → Ezamiyyet; icazə/offline görüş → Icazeli; qalanı Qayib
                    var status = ezamiyyetSet.Contains(key)
                        ? DavamiyyetStatus.Ezamiyyet
                        : (icazeSet.Contains(key) || gorushSet.Contains(key))
                            ? DavamiyyetStatus.Icazeli
                            : DavamiyyetStatus.Qayib;

                    // Yumşaq silinmiş sətir varsa YENİ yazmırıq — unikal indeks
                    // (IsciId, Tarix) Silinib-i filtrləmir, INSERT pozulardı.
                    if (silinmisDict.TryGetValue(key, out var silinmisId))
                    {
                        dirildilecekler.Add((silinmisId, status));
                        continue;
                    }

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

            // Silinmiş sətirləri dirilt (yeni INSERT əvəzinə) — indekslə toqquşmasın
            if (dirildilecekler.Count > 0)
            {
                var idler = dirildilecekler.Select(x => x.Id).ToList();
                var statusDict = dirildilecekler.ToDictionary(x => x.Id, x => x.Status);

                var qeydler = await db.Set<Davamiyyet>()
                    .Where(x => idler.Contains(x.Id))
                    .ToListAsync(ct);

                foreach (var q in qeydler)
                {
                    q.Silinib = false;
                    q.SilinmeTarixi = null;
                    q.Status = statusDict[q.Id];
                    q.GirisVaxti = null;
                    q.CixisVaxti = null;
                    q.YenilenmeTarixi = indi;
                }
            }

            if (yeniQeydler.Count > 0)
                await db.Set<Davamiyyet>().AddRangeAsync(yeniQeydler, ct);

            if (yeniQeydler.Count > 0 || dirildilecekler.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "QayibMarker: {Yeni} yeni, {Dirilen} dirildilmiş davamiyyət qeydi",
                    yeniQeydler.Count, dirildilecekler.Count);
            }

            // Dünən görüşdən qayıtmayan iştirakçıların CihazQayidisVaxti-ni iş günü sonuna qoy
            var oncekiGun = bugun.AddDays(-1);
            var aciqCixislar = await db.GorushIshtirakcilar
                .Include(x => x.Gorush)
                .Where(x => !x.Silinib
                         && x.CihazCixisVaxti != null
                         && x.CihazQayidisVaxti == null
                         && x.Gorush.Tarix.Date == oncekiGun)
                .ToListAsync(ct);

            if (aciqCixislar.Count > 0)
            {
                var isParam = await db.Set<IsParametri>()
                    .AsNoTracking()
                    .Where(x => !x.Silinib)
                    .FirstOrDefaultAsync(ct);
                var gunSonu = isParam?.StandartCixisVaxti ?? new TimeSpan(17, 45, 0);

                foreach (var gi in aciqCixislar)
                    gi.CihazQayidisVaxti = gi.Gorush.Tarix.Date.Add(gunSonu);

                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "GörüşMarker: {Count} açıq çıxış iş günü sonuna qədər kapatıldı",
                    aciqCixislar.Count);
            }

            // Bitmiş, amma cihazdan qayıdışı qeyd olunmamış ezamiyyətlər — işçinin
            // yazdığı SON günün (BitmeTarixi) iş günü sonuna qədər kapatılır.
            //  - Yalnız bitmiş səfərlər (BitmeTarixi < bugün) — davam edən səfər
            //    vaxtından əvvəl kapatılmır.
            //  - Son 90 gün backfill — xidmət bir neçə gecə işləməsə belə qalmış
            //    (qayıtmayıb) qeydlər növbəti işləmədə avtomatik bağlanır.
            var aciqEzamCixislar = await db.Set<EzamiyyetMuraciet>()
                .Where(x => !x.Silinib
                         && x.Status         == EzamiyyetStatus.Tesdiqlendi
                         && x.CihazCixisVaxti  != null
                         && x.CihazQayidisVaxti == null
                         && x.BitmeTarixi.Date < bugun
                         && x.BitmeTarixi.Date >= bugun.AddDays(-90))
                .ToListAsync(ct);

            if (aciqEzamCixislar.Count > 0)
            {
                var ezIsParam = await db.Set<IsParametri>()
                    .AsNoTracking()
                    .Where(x => !x.Silinib)
                    .FirstOrDefaultAsync(ct);
                var ezGunSonu = ezIsParam?.StandartCixisVaxti ?? new TimeSpan(17, 45, 0);

                foreach (var ez in aciqEzamCixislar)
                    ez.CihazQayidisVaxti = ez.BitmeTarixi.Date.Add(ezGunSonu);

                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "EzamiyyetMarker: {Count} açıq ezamiyyət çıxışı son günün iş günü sonuna kapatıldı",
                    aciqEzamCixislar.Count);
            }
        }
    }
}
