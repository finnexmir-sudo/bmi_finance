// Infrastructure/BackgroundJobs/PlanUzreBaglamaBackgroundService.cs
using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinNex.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// GÜN BAĞLAMA — cihaza vurmadan gedənlərin icazə/ezamiyyət vaxtlarını bağlayır.
    ///
    /// İşçi icazə/ezamiyyətlə gedəndə cihaza baxmasa, faktiki çıxış/qayıdış boş
    /// qalır və HR hər dəfə əl ilə düzəltməli olurdu. Bu servis hər gün 23:00-dan
    /// sonra KEÇMİŞ günlər üçün (son 7 gün):
    ///   1) əvvəl XAM cihaz oxumalarından (CihazOxuma) bərpa etməyə çalışır
    ///      (pəncərə daxilində ilk oxuma = çıxış, son oxuma = qayıdış);
    ///   2) cihazda heç nə yoxdursa, MÜRACİƏTDƏKİ PLAN vaxtlarını yazır və
    ///      qeydi PlanUzre bayrağı ilə işarələyir (audit üçün cihaz datasından
    ///      fərqlənsin).
    ///
    /// Yalnız KEÇMİŞ günlərə toxunur — cari günün icazəsi axşama qədər canlı
    /// ADMS hook-u ilə bağlana bilər, ona qarışmır. Əl ilə yazılmış (HR düzəlişi)
    /// dəyərlərin üstünə YAZMIR (yalnız boş sahələri doldurur).
    /// </summary>
    public class PlanUzreBaglamaBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PlanUzreBaglamaBackgroundService> _logger;

        private static readonly TimeSpan _interval = TimeSpan.FromHours(1);
        private const int IsBitmeSaati = 23;
        private const int BackfillGunSayi = 7;

        public PlanUzreBaglamaBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<PlanUzreBaglamaBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PlanUzreBaglamaBackgroundService başladı.");
            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.Now.Hour >= IsBitmeSaati)
                        await IcraEtAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PlanUzreBaglamaBackgroundService xətası");
                }

                try { await Task.Delay(_interval, stoppingToken); }
                catch (TaskCanceledException) { return; }
            }
        }

        private async Task IcraEtAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var bugun = DateTime.Today;
            var minTarix = bugun.AddDays(-BackfillGunSayi);

            // ── 0) CixisGiris qeydi ÜMUMİYYƏTLƏ olmayan təsdiqlənmiş keçmiş icazələr ──
            // (məs. köhnə jeton icazələri bu qeyd olmadan yaranıb) — əvvəl qeyd yaradılır
            // ki, aşağıdakı bağlama onları da tutsun.
            var qeydsizler = await db.Icazeler
                .Where(x => !x.Silinib
                         && x.Status == IcazeStatus.Tesdiqlenib
                         && x.CixisGiris == null
                         && x.IcazeTarixi >= minTarix
                         && x.IcazeTarixi < bugun)
                .Select(x => new { x.Id, x.Birdefelik })
                .ToListAsync(ct);
            if (qeydsizler.Count > 0)
            {
                foreach (var q in qeydsizler)
                    db.IcazeCixisGirisler.Add(new IcazeCixisGiris
                    {
                        IcazeId = q.Id,
                        Birdefelik = q.Birdefelik,
                        Status = IcazeCixisGirisStatus.Gozlenir
                    });
                await db.SaveChangesAsync(ct);
            }

            // ── 1) İCAZƏLƏR — bağlanmamış keçmiş qeydlər ─────────────────────
            var icazeler = await db.IcazeCixisGirisler
                .Include(x => x.Icaze)
                .Where(x => !x.Silinib
                         && x.Status != IcazeCixisGirisStatus.LegvEdildi
                         && x.QayidisVaxt == null
                         && !x.Icaze.Silinib
                         && x.Icaze.Status == IcazeStatus.Tesdiqlenib
                         && x.Icaze.IcazeTarixi >= minTarix
                         && x.Icaze.IcazeTarixi < bugun)
                .ToListAsync(ct);

            // ── 2) EZAMİYYƏTLƏR — saatlı, bağlanmamış keçmişlər ──────────────
            var ezamlar = await db.EzamiyyetMuracietler
                .Where(x => !x.Silinib
                         && x.Status == EzamiyyetStatus.Tesdiqlendi
                         && x.BaslamaSaati != null
                         && x.CihazQayidisVaxti == null
                         && x.BitmeTarixi >= minTarix
                         && x.BitmeTarixi < bugun)
                .ToListAsync(ct);

            if (icazeler.Count == 0 && ezamlar.Count == 0) return;

            // Xam cihaz oxumaları — hamısı üçün bir sorğu
            var isciIds = icazeler.Select(x => x.Icaze.IsciId)
                .Concat(ezamlar.Select(x => x.IsciId)).Distinct().ToList();
            var oxumalar = await db.CihazOxumalar.AsNoTracking()
                .Where(o => !o.Silinib && isciIds.Contains(o.IsciId)
                         && o.Vaxt >= minTarix && o.Vaxt < bugun.AddDays(1))
                .Select(o => new { o.IsciId, o.Vaxt })
                .ToListAsync(ct);
            var punchXerite = oxumalar
                .GroupBy(o => (o.IsciId, o.Vaxt.Date))
                .ToDictionary(g => g.Key, g => g.Select(p => p.Vaxt).OrderBy(v => v).ToList());

            int icazeSay = 0, ezamSay = 0;

            foreach (var cg in icazeler)
            {
                var gun = cg.Icaze.IcazeTarixi.Date;
                var alt = cg.Icaze.BaslamaSaati - TimeSpan.FromMinutes(30);
                var ust = cg.Icaze.BitisSaati + TimeSpan.FromMinutes(30);
                var pencere = punchXerite.TryGetValue((cg.Icaze.IsciId, gun), out var pl)
                    ? pl.Where(v => v.TimeOfDay >= alt && v.TimeOfDay <= ust).ToList()
                    : new List<DateTime>();

                bool planIsledi = false;
                if (cg.CixisVaxt == null)
                {
                    if (pencere.Count > 0) cg.CixisVaxt = pencere[0];
                    else { cg.CixisVaxt = gun + cg.Icaze.BaslamaSaati; planIsledi = true; }
                }
                if (cg.QayidisVaxt == null && !cg.Birdefelik)
                {
                    var sonPunch = pencere.LastOrDefault(v => v > cg.CixisVaxt.Value.AddMinutes(5));
                    if (sonPunch != default) cg.QayidisVaxt = sonPunch;
                    else { cg.QayidisVaxt = gun + cg.Icaze.BitisSaati; planIsledi = true; }
                }

                cg.Status = IcazeCixisGirisStatus.Tamamlandi;
                if (planIsledi) cg.PlanUzreAvtomatik = true;
                cg.YenilenmeTarixi = DateTime.Now;
                icazeSay++;
            }

            foreach (var ez in ezamlar)
            {
                var gun = ez.BaslamaTarixi.Date;
                var alt = (ez.BaslamaSaati ?? TimeSpan.Zero) - TimeSpan.FromMinutes(15);
                var ust = (ez.BitisSaati ?? new TimeSpan(23, 59, 59)) + TimeSpan.FromMinutes(15);
                var pencere = punchXerite.TryGetValue((ez.IsciId, gun), out var pl)
                    ? pl.Where(v => v.TimeOfDay >= alt && v.TimeOfDay <= ust).ToList()
                    : new List<DateTime>();

                bool planIsledi = false;
                if (ez.CihazCixisVaxti == null)
                {
                    if (pencere.Count > 0) ez.CihazCixisVaxti = pencere[0];
                    else { ez.CihazCixisVaxti = gun + (ez.BaslamaSaati ?? TimeSpan.Zero); planIsledi = true; }
                }
                if (ez.CihazQayidisVaxti == null)
                {
                    var sonPunch = pencere.LastOrDefault(v => v > ez.CihazCixisVaxti.Value.AddMinutes(5));
                    if (sonPunch != default) ez.CihazQayidisVaxti = sonPunch;
                    else if (ez.BitisSaati.HasValue)
                    { ez.CihazQayidisVaxti = ez.BitmeTarixi.Date + ez.BitisSaati.Value; planIsledi = true; }
                }

                if (planIsledi) ez.CihazVaxtPlanUzre = true;
                ez.YenilenmeTarixi = DateTime.Now;
                ezamSay++;
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Plan üzrə bağlama: {Icaze} icazə, {Ezam} ezamiyyət qeydi bağlandı.", icazeSay, ezamSay);
        }
    }
}
