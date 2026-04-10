using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinNex.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Hər gün gecə saat 02:00-da 30 gündən köhnə chat mesajlarını silir.
    /// </summary>
    public class ChatCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatCleanupBackgroundService> _logger;
        private const int KohneMesajGunLimiti = 30;

        public ChatCleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ChatCleanupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var next = now.Date.AddDays(1).AddHours(2);
                var delay = next - now;
                if (delay.TotalMilliseconds < 0) delay = TimeSpan.FromHours(1);

                await Task.Delay(delay, stoppingToken);

                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Chat cleanup xətası");
                }
            }
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var limit = DateTime.Now.AddDays(-KohneMesajGunLimiti);

            var kohneMesajlar = await db.ChatMesajlar
                .Where(x => x.GonderilmeTarixi < limit)
                .ToListAsync(ct);

            if (!kohneMesajlar.Any())
            {
                _logger.LogInformation("Chat cleanup: silinəcək mesaj yoxdur");
                return;
            }

            db.ChatMesajlar.RemoveRange(kohneMesajlar);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Chat cleanup: {Count} köhnə mesaj silindi", kohneMesajlar.Count);
        }
    }
}
