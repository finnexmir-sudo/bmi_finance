using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FinNex.Application.Services.Communication
{
    public class WebPushService : IWebPushService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebPushService> _logger;
        private readonly HttpClient _httpClient;

        public WebPushService(IUnitOfWork unitOfWork, IConfiguration configuration,
            ILogger<WebPushService> logger, IHttpClientFactory httpClientFactory)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("WebPush");
        }

        public async Task AboneOlAsync(int isciId, string endpoint, string p256dh, string auth)
        {
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

                var payload = JsonSerializer.Serialize(new
                {
                    bashliq,
                    metn,
                    url = url ?? "/"
                });

                // VAPID JWT token yarat
                var audience = new Uri(ab.Endpoint).GetLeftPart(UriPartial.Authority);
                var jwt = CreateVapidJwt(audience, vapidSubject, vapidPrivateKey);

                var request = new HttpRequestMessage(HttpMethod.Post, ab.Endpoint)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                request.Headers.TryAddWithoutValidation("TTL", "86400");
                request.Headers.TryAddWithoutValidation("Authorization", $"vapid t={jwt},k={vapidPublicKey}");

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Gone ||
                    response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Abonelik keçərsizdir - sil
                    await _unitOfWork.Repository<PushAbonelik>().YumshakSilAsync(ab.Id);
                    await _unitOfWork.YaddaSaxlaAsync();
                    _logger.LogInformation("Keçərsiz push abonelik silindi: {Endpoint}", ab.Endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push bildiriş göndərilə bilmədi: {Endpoint}", ab.Endpoint);
            }
        }

        private static string CreateVapidJwt(string audience, string subject, string privateKey)
        {
            var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { typ = "JWT", alg = "ES256" }));
            var expiry = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds();
            var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
            {
                aud = audience,
                exp = expiry,
                sub = subject
            }));

            var unsignedToken = $"{header}.{payload}";
            var signature = SignEs256(unsignedToken, privateKey);

            return $"{unsignedToken}.{signature}";
        }

        private static string SignEs256(string input, string privateKeyBase64Url)
        {
            var keyBytes = Base64UrlDecode(privateKeyBase64Url);

            // P-256 private key-dən ECDsa yaradılır
            using var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = keyBytes
            });

            var dataBytes = Encoding.UTF8.GetBytes(input);
            var signatureBytes = ecdsa.SignData(dataBytes, HashAlgorithmName.SHA256);

            return Base64UrlEncode(signatureBytes);
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
