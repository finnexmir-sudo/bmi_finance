using FinNex.Application.Interfaces.Communication;
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
    /// PlanliOdenis statuslu məzuniyyət ödənişlərini həmin planlı tarix (və ya
    /// daha erkən) çatanda avtomatik Odenilib statusuna keçirir və işçiyə
    /// faktiki ödəniş bildirişi göndərir. Hər 1 saatda bir işə düşür.
    /// </summary>
    public class MezuniyyetOdenisSchedulerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MezuniyyetOdenisSchedulerService> _logger;
        private static readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public MezuniyyetOdenisSchedulerService(
            IServiceScopeFactory scopeFactory,
            ILogger<MezuniyyetOdenisSchedulerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MezuniyyetOdenisSchedulerService başladı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await IcraEtAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MezuniyyetOdenisSchedulerService xətası");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException) { break; }
            }
        }

        private async Task IcraEtAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bildirisService = scope.ServiceProvider.GetRequiredService<IBildirisService>();

            var indi = DateTime.Now;

            // PlanliOdenis statuslu və PlanliOdenisTarixi çatmış və ya keçmiş müraciətlər
            var hedefler = await db.Set<Mezuniyyet>()
                .Where(x => !x.Silinib
                         && x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                         && x.OdenisStatus == MezuniyyetOdenisStatus.PlanliOdenis
                         && x.PlanliOdenisTarixi != null
                         && x.PlanliOdenisTarixi <= indi)
                .ToListAsync(ct);

            if (hedefler.Count == 0) return;

            foreach (var m in hedefler)
            {
                m.OdenisStatus = MezuniyyetOdenisStatus.Odenilib;
                m.OdenilmeTarixi = indi;
                m.YenilenmeTarixi = indi;
            }

            await db.SaveChangesAsync(ct);

            foreach (var m in hedefler)
            {
                try
                {
                    await bildirisService.YaratAsync(
                        isciId: m.IsciId,
                        nov: BildirisNovu.MezuniyyetOdenisIcraEdildi,
                        bashliq: "Məzuniyyət ödənişi icra edildi",
                        metn: $"{m.BaslamaTarixi:dd.MM.yyyy}–{m.BitmeTarixi:dd.MM.yyyy} məzuniyyət " +
                              $"ödənişiniz ({m.OdenenMebleg:N2} ₼) kartınıza köçürüldü.",
                        redirectUrl: $"/User/Mezuniyyet/Detail/{m.Id}",
                        mezuniyyetId: m.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bildiriş xətası: MezuniyyetId={MezId}", m.Id);
                }
            }

            _logger.LogInformation("MezuniyyetOdenisScheduler: {Count} ödəniş icra edildi", hedefler.Count);
        }
    }
}
