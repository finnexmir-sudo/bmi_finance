using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinNex.Application.Interfaces.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinNex.Application.Services.Sms;

/// <summary>
/// Posta Güvercini (Göyərçin) SMS gateway implementasiyası.
/// Endpoint: POST /gateway/api/sms/v1/message/send?publicKey={PublicKey}
/// Auth: Bearer {PrivateKey} header-də.
/// Sənəd: PG365 SMS API v1.0.4
/// </summary>
public class GoyercinSmsGateway : ISmsGateway
{
    private readonly HttpClient _http;
    private readonly GoyercinSettings _settings;
    private readonly ILogger<GoyercinSmsGateway> _logger;

    public GoyercinSmsGateway(HttpClient http, IOptions<GoyercinSettings> opts, ILogger<GoyercinSmsGateway> logger)
    {
        _http = http;
        _settings = opts.Value;
        _logger = logger;
    }

    public async Task<SmsGatewayResult> SendAsync(string telefon, string metn, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(telefon))
            return SmsGatewayResult.Fail("Telefon nömrəsi boşdur");
        if (string.IsNullOrWhiteSpace(metn))
            return SmsGatewayResult.Fail("Mətn boşdur");
        if (metn.Length > 1000)
            return SmsGatewayResult.Fail("Mətn 1000 simvoldan uzundur");

        if (_settings.DryRun)
        {
            _logger.LogInformation("Göyərçin DryRun: {Telefon} → {Metn}", telefon, metn);
            return SmsGatewayResult.Ok($"dryrun-{Guid.NewGuid():N}", "{\"DryRun\":true}");
        }

        if (string.IsNullOrWhiteSpace(_settings.PublicKey) || string.IsNullOrWhiteSpace(_settings.PrivateKey))
            return SmsGatewayResult.Fail("Göyərçin PublicKey / PrivateKey konfiqurasiya edilməyib");

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/gateway/api/sms/v1/message/send?publicKey={Uri.EscapeDataString(_settings.PublicKey)}";

        var body = new
        {
            Text = metn,
            Purpose = _settings.Purpose,
            Options = new
            {
                Originator = _settings.Originator,
                Encoding = _settings.Encoding,
                SmsType = "SMS"
            },
            Receivers = new[]
            {
                new { Receiver = telefon }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.PrivateKey);

        try
        {
            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            // Cavab həm uğurda, həm də xətada eyni model ilə gəlir (AppResponse: Status, Description, Result)
            var raw2k = raw.Length > 2000 ? raw.Substring(0, 2000) : raw;

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Göyərçin HTTP {Status}: {Body}", (int)resp.StatusCode, raw2k);
                return SmsGatewayResult.Fail($"HTTP {(int)resp.StatusCode}", raw2k);
            }

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var status = root.TryGetProperty("Status", out var s) ? s.GetInt32() : 0;
            var desc = root.TryGetProperty("Description", out var d) ? d.GetString() ?? "" : "";

            if (status != 200)
                return SmsGatewayResult.Fail($"Göyərçin status {status}: {desc}", raw2k);

            // Result.ReceiversAccepted[0].id — bu id-ni saxlayırıq ki, sonra /status ilə sorğu edə bilək
            string? acceptedId = null;
            if (root.TryGetProperty("Result", out var result)
                && result.TryGetProperty("ReceiversAccepted", out var accepted)
                && accepted.ValueKind == JsonValueKind.Array
                && accepted.GetArrayLength() > 0)
            {
                var first = accepted[0];
                if (first.TryGetProperty("id", out var idProp))
                    acceptedId = idProp.GetString();
            }

            // ReceiversRejected boş deyilsə — SMS qəbul edilməyib
            if (root.TryGetProperty("Result", out var result2)
                && result2.TryGetProperty("ReceiversRejected", out var rejected)
                && rejected.ValueKind == JsonValueKind.Array
                && rejected.GetArrayLength() > 0)
            {
                var first = rejected[0];
                var err = first.TryGetProperty("ErrorMessage", out var em) ? em.GetString() : "Rejected";
                return SmsGatewayResult.Fail($"Göyərçin rədd etdi: {err}", raw2k);
            }

            if (string.IsNullOrEmpty(acceptedId))
                return SmsGatewayResult.Fail("Göyərçin cavabında alıcı id yoxdur", raw2k);

            return SmsGatewayResult.Ok(acceptedId, raw2k);
        }
        catch (TaskCanceledException)
        {
            return SmsGatewayResult.Fail("Göyərçin sorğusu vaxt aşımı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Göyərçin SMS göndərmə xətası");
            return SmsGatewayResult.Fail($"İstisna: {ex.Message}");
        }
    }
}
