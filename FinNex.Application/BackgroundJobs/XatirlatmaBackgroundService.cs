// Infrastructure/BackgroundJobs/XatirlatmaBackgroundService.cs
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinNex.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Hər 1 saatda bir işə düşür:
    ///  1) Tapşırıq son tarixi sabah olan işçilərə xatırlatma yaradır
    ///  2) Görüş sabah olan iştirakçılara xatırlatma yaradır
    ///  3) Vaxtı keçmiş xatırlatmaları "göndərildi" kimi işarələyir
    ///     (real push/email sistemi bağlansa buradan genişləndirilir)
    /// </summary>
    public class XatirlatmaBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<XatirlatmaBackgroundService> _logger;
        private static readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public XatirlatmaBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<XatirlatmaBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("XatirlatmaBackgroundService başladı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await IsleAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "XatirlatmaBackgroundService xətası.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task IsleAsync()
        {
            using var scope = _scopeFactory.CreateScope();

            var xatirlatmaService = scope.ServiceProvider
                .GetRequiredService<IXatirlatmaService>();
            var tapshiriqService = scope.ServiceProvider
                .GetRequiredService<ITapshiriqService>();
            var gorushService = scope.ServiceProvider
                .GetRequiredService<IGorushService>();

            var sabahinTarixi = DateTime.Today.AddDays(1);

            // ── 1) Tapşırıq xatırlatmaları ───────────────────
            var tapshiriqlar = await tapshiriqService.GetYaxinlasanlarAsync(sabahinTarixi);
            if (tapshiriqlar.Success && tapshiriqlar.Data != null)
            {
                foreach (var t in tapshiriqlar.Data)
                {
                    await xatirlatmaService.SistemXatirlatmasiYaratAsync(new XatirlatmaSistemCreateDto
                    {
                        IsciId = t.TeyinOlunanIsciId,
                        Bashliq = $"Tapşırıq sabah bitir: {t.Bashliq}",
                        Qeyd = $"Son tarix: {t.SonTarix:dd.MM.yyyy}",
                        XatirlatmaTarixi = DateTime.Now,
                        EntityTipi = XatirlatmaEntityTipi.Tapshiriq,
                        EntityId = t.Id
                    });
                }
            }

            // ── 2) Görüş xatırlatmaları ───────────────────────
            var gorushler = await gorushService.GetYaxinlasanlarAsync(sabahinTarixi);
            if (gorushler.Success && gorushler.Data != null)
            {
                foreach (var g in gorushler.Data)
                {
                    foreach (var ishtirakci in g.Ishtirakcılar
                        .Where(i => i.Status != IshtirakciStatus.Redd))
                    {
                        await xatirlatmaService.SistemXatirlatmasiYaratAsync(new XatirlatmaSistemCreateDto
                        {
                            IsciId = ishtirakci.IsciId,
                            Bashliq = $"Görüş sabah: {g.Bashliq}",
                            Qeyd = $"{g.Tarix:dd.MM.yyyy} — {g.BaslamaSaati:hh\\:mm}",
                            XatirlatmaTarixi = DateTime.Now,
                            EntityTipi = XatirlatmaEntityTipi.Gorush,
                            EntityId = g.Id
                        });
                    }
                }
            }

            // ── 3) Göndərilməmiş keçmiş xatırlatmaları işarələ
            // (Burada real email/push inteqrasiyası əlavə etmək olar)
            var gonderilemeyenler = await xatirlatmaService.GetGonderilemeyenlerAsync();
            if (gonderilemeyenler.Success && gonderilemeyenler.Data != null)
            {
                foreach (var x in gonderilemeyenler.Data)
                {
                    // TODO: EmailService.GonderAsync(x) — lazım olsa bağla
                    await xatirlatmaService.GonderildiIsareEtAsync(x.Id);
                }
            }

            _logger.LogInformation(
                "Xatırlatma dövrü tamamlandı. Tapşırıq: {t}, Görüş: {g}",
                tapshiriqlar.Data?.Count ?? 0,
                gorushler.Data?.Count ?? 0);
        }
    }
}