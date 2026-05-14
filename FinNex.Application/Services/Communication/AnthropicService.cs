using FinNex.Application.DTOs.Communication;
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

    public async Task<AIMailTahlilNetic> MailTahlilEtAsync(string kimden, string movzu, string metin,
        List<(string FileName, string ContentType, string Content)> qosmalar)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new AIMailTahlilNetic { Xulase = "AI təhlili üçün API açarı konfiqurasiya edilməyib." };

        var prompt = BuildPrompt(kimden, movzu, metin, qosmalar);

        var request = new
        {
            model = Model,
            max_tokens = 1500,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var client = _httpClientFactory.CreateClient("Anthropic");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiUrl, content);
        if (!response.IsSuccessStatusCode)
            return new AIMailTahlilNetic { Xulase = $"AI cavabı alınmadı (HTTP {(int)response.StatusCode})." };

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";

        return ParseResponse(text);
    }

    private static AIMailTahlilNetic ParseResponse(string raw)
    {
        // AI cavabı ```json ... ``` formatında olur
        var jsonStart = raw.IndexOf('{');
        var jsonEnd = raw.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            try
            {
                var jsonStr = raw.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var jdoc = JsonDocument.Parse(jsonStr);
                var root = jdoc.RootElement;

                var netic = new AIMailTahlilNetic
                {
                    Xulase = root.TryGetProperty("xulase", out var x) ? x.GetString() ?? "" : raw
                };

                if (root.TryGetProperty("dedlayn_tarix", out var dt) && dt.ValueKind != JsonValueKind.Null)
                {
                    if (DateTime.TryParse(dt.GetString(), out var parsed))
                        netic.DedlaynTarix = parsed;
                }
                if (root.TryGetProperty("dedlayn_nov", out var dn) && dn.ValueKind != JsonValueKind.Null)
                    netic.DedlaynNov = dn.GetString();
                if (root.TryGetProperty("dedlayn_qeyd", out var dq) && dq.ValueKind != JsonValueKind.Null)
                    netic.DedlaynQeyd = dq.GetString();

                return netic;
            }
            catch { }
        }
        return new AIMailTahlilNetic { Xulase = raw };
    }

    private static string BuildPrompt(string kimden, string movzu, string metin,
        List<(string FileName, string ContentType, string Content)> qosmalar)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Siz şirkətin daxili AI köməkçisisiniz. Aşağıdakı e-poçtu Azərbaycan dilində təhlil edin.");
        sb.AppendLine();
        sb.AppendLine("CAVABI MÜTLƏQ aşağıdakı JSON formatında verin (başqa heç nə yazmayın):");
        sb.AppendLine(@"{
  ""xulase"": ""## 1. Məzmun\n...\n## 2. Əsas tələblər\n...\n## 3. Təcililik\nYüksək/Orta/Aşağı\n## 4. Növbəti addım\n..."",
  ""dedlayn_tarix"": ""YYYY-MM-DD ya null"",
  ""dedlayn_nov"": ""Gorus ya Hesabat ya Muqavile ya SonTarix ya Diger ya null"",
  ""dedlayn_qeyd"": ""qısa izah ya null""
}");
        sb.AppendLine();
        sb.AppendLine("Dedlayn: maildə görüş tarixi, hesabat son tarixi, qeydiyyat bitmə tarixi, müqavilə imzalanma tarixi kimi konkret tarix varsa onu çıxar. Yoxdursa null.");
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
