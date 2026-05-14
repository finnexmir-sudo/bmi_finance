using FinNex.Application.Interfaces.Communication;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FinNex.Application.Services.Communication;

public class AnthropicService : IAnthropicService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-opus-4-7";

    public AnthropicService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = config["Anthropic:ApiKey"] ?? "";
    }

    public async Task<string> MailTahlilEtAsync(string kimden, string movzu, string metin,
        List<(string FileName, string ContentType, string Content)> qosmalar)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return "AI təhlili üçün API açarı konfiqurasiya edilməyib.";

        var prompt = BuildPrompt(kimden, movzu, metin, qosmalar);

        var request = new
        {
            model = Model,
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var client = _httpClientFactory.CreateClient("Anthropic");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiUrl, content);
        if (!response.IsSuccessStatusCode)
            return $"AI cavabı alınmadı (HTTP {(int)response.StatusCode}).";

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "Cavab boşdur.";
    }

    private static string BuildPrompt(string kimden, string movzu, string metin,
        List<(string FileName, string ContentType, string Content)> qosmalar)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Siz şirkətin daxili AI köməkçisisiniz. Aşağıdakı e-poçtu Azərbaycan dilində təhlil edin.");
        sb.AppendLine("Xülasə formatlayın:\n1. Məzmun (2-3 cümlə)\n2. Əsas tələblər/tapşırıqlar\n3. Təcililik dərəcəsi (Yüksək/Orta/Aşağı)\n4. Tövsiyə olunan növbəti addım");
        sb.AppendLine();
        sb.AppendLine($"--- MƏKTUB ---");
        sb.AppendLine($"Göndərən: {kimden}");
        sb.AppendLine($"Mövzu: {movzu}");
        sb.AppendLine($"Məzmun:\n{metin.Substring(0, Math.Min(metin.Length, 3000))}");

        foreach (var q in qosmalar.Take(3))
        {
            if (!string.IsNullOrWhiteSpace(q.Content))
            {
                sb.AppendLine();
                sb.AppendLine($"--- QOŞMA: {q.FileName} ---");
                sb.AppendLine(q.Content.Substring(0, Math.Min(q.Content.Length, 2000)));
            }
        }

        return sb.ToString();
    }
}
