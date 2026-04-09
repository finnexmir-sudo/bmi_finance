using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebPush;

namespace FinNex.Application.Services.Communication
{
    public class WebPushService : IWebPushService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebPushService> _logger;

        public WebPushService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<WebPushService> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task AboneOlAsync(int isciId, string endpoint, string p256dh, string auth)
        {
            // Eyni endpoint artıq varsa yaratma
            var movcud = await _unitOfWork.Repository<PushAbonelik>()
                .GetirAsync(x => x.IsciId == isciId && x.Endpoint == endpoint);

            if (movcud != null) return;

            var abonelik = new PushAbonelik
            {
                IsciId = isciId,
                Endpoint = endpoint,
                P256dh = p256dh,
                Auth = auth
            };

            await _unitOfWork.Repository<PushAbonelik>().YaratAsync(abonelik);
            await _unitOfWork.YaddaSaxlaAsync();
        }

        public async Task AbonelikSilAsync(int isciId, string endpoint)
        {
            var abonelik = await _unitOfWork.Repository<PushAbonelik>()
                .GetirAsync(x => x.IsciId == isciId && x.Endpoint == endpoint);

            if (abonelik != null)
            {
                await _unitOfWork.Repository<PushAbonelik>().YumshakSilAsync(abonelik.Id);
                await _unitOfWork.YaddaSaxlaAsync();
            }
        }

        public async Task BildirisSonderAsync(int isciId, string bashliq, string metn, string? url = null)
        {
            var abonelikler = await _unitOfWork.Repository<PushAbonelik>()
                .HamisiniGetirAsync(x => x.IsciId == isciId);

            foreach (var ab in abonelikler)
            {
                await GonderAsync(ab, bashliq, metn, url);
            }
        }

        public async Task HamisinaSonderAsync(string bashliq, string metn, string? url = null)
        {
            var abonelikler = await _unitOfWork.Repository<PushAbonelik>()
                .HamisiniGetirAsync();

            foreach (var ab in abonelikler)
            {
                await GonderAsync(ab, bashliq, metn, url);
            }
        }

        private async Task GonderAsync(PushAbonelik ab, string bashliq, string metn, string? url)
        {
            try
            {
                var vapidSubject = _configuration["Vapid:Subject"] ?? "mailto:admin@finnex.az";
                var vapidPublicKey = _configuration["Vapid:PublicKey"] ?? "";
                var vapidPrivateKey = _configuration["Vapid:PrivateKey"] ?? "";

                if (string.IsNullOrEmpty(vapidPublicKey) || string.IsNullOrEmpty(vapidPrivateKey))
                {
                    _logger.LogWarning("VAPID açarları konfiqurasiya olunmayıb.");
                    return;
                }

                var client = new WebPushClient();
                var subscription = new PushSubscription(ab.Endpoint, ab.P256dh, ab.Auth);
                var vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);

                var payload = JsonSerializer.Serialize(new
                {
                    bashliq,
                    metn,
                    url = url ?? "/"
                });

                await client.SendNotificationAsync(subscription, payload, vapidDetails);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                                                ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Abonelik artıq keçərsizdir - sil
                await _unitOfWork.Repository<PushAbonelik>().YumshakSilAsync(ab.Id);
                await _unitOfWork.YaddaSaxlaAsync();
                _logger.LogInformation("Keçərsiz push abonelik silindi: {Endpoint}", ab.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push bildiriş göndərilə bilmədi: {Endpoint}", ab.Endpoint);
            }
        }
    }
}
