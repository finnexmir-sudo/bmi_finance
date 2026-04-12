using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinNex.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Deaktivdir — "Mail-dən yenilə" düyməsi ilə əvəz olunub.
    /// Gələcəkdə avtomatik yoxlama lazım olsa aktivləşdirilə bilər.
    /// </summary>
    public class KreditMailBackgroundService : BackgroundService
    {
        private readonly ILogger<KreditMailBackgroundService> _logger;

        public KreditMailBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<KreditMailBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KreditMailBackgroundService deaktivdir.");
            return Task.CompletedTask;
        }
    }
}
