using FinNex.Application.DTOs.AI;
using FinNex.Application.Interfaces.AI;
using FinNex.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FinNex.Application.Services.AI;

public class SenedAiService : ISenedAiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-opus-4-7";

    public SenedAiService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = config["Anthropic:ApiKey"] ?? "";
    }

    public async Task<RiskAnalizResult> AnalyzeRiskAsync(string text, string fileName)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new RiskAnalizResult { Xeta = "AI açarı konfiqurasiya edilməyib." };

        var prompt = BuildRiskPrompt(text, fileName);
        var raw = await CallApiAsync(prompt, 2000);
        if (raw == null)
            return new RiskAnalizResult { Xeta = "AI cavabı alınmadı." };

        return ParseRiskResponse(raw);
    }

    public async Task<KonstruktorResult> ConstructDocumentAsync(string senedNovu, string musteriAd,
        int gecikmeGun, decimal meble, string? elaveMelumat)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new KonstruktorResult { Xeta = "AI açarı konfiqurasiya edilməyib." };

        var prompt = BuildKonstruktorPrompt(senedNovu, musteriAd, gecikmeGun, meble, elaveMelumat);
        var raw = await CallApiAsync(prompt, 1500);
        if (raw == null)
            return new KonstruktorResult { Xeta = "AI cavabı alınmadı." };

        return new KonstruktorResult { GeneratedContent = raw.Trim() };
    }

    private async Task<string?> CallApiAsync(string prompt, int maxTokens)
    {
        var request = new
        {
            model = Model,
            max_tokens = maxTokens,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var client = _httpClientFactory.CreateClient("Anthropic");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiUrl, content);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
    }

    private static string BuildRiskPrompt(string text, string fileName)
    {
        var truncated = text.Length > 8000 ? text.Substring(0, 8000) + "\n[...mətn kəsildi]" : text;

        var sb = new StringBuilder();
        sb.AppendLine("Siz bank hüquq şöbəsinin ekspert AI köməkçisisiniz. Aşağıdakı sənədi analiz edin və bankın mənafeyinə zərər verə biləcək gizli tələ, qeyri-müəyyən ifadə və riskli bəndləri aşkar edin.");
        sb.AppendLine();
        sb.AppendLine("CAVABI MÜTLƏQ aşağıdakı JSON formatında verin (başqa heç nə yazmayın):");
        sb.AppendLine("{");
        sb.AppendLine("  \"risk_level\": \"Red|Yellow|Green\",");
        sb.AppendLine("  \"risky_clauses\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"madde_tipi\": \"məs: Faiz dərəcəsi, Vaxtından əvvəl ödəniş, Girov, Cərimə şərti...\",");
        sb.AppendLine("      \"riskli_cumle\": \"sənəddəki problematik cümlə/ifadə (sitat)\",");
        sb.AppendLine("      \"zarar_potensiali\": \"bu bəndin banka necə zərər verə biləcəyinin izahı\",");
        sb.AppendLine("      \"alternativ_teklif\": \"daha təhlükəsiz alternativ formul ya bənd\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Risk səviyyəsi qaydası:");
        sb.AppendLine("- Red: Bir və ya daha çox yüksək riskli bənd (bankın ciddi zərər çəkə biləcəyi)");
        sb.AppendLine("- Yellow: Orta risk — qeyri-müəyyən ifadələr, şərh mübahisəsi yarada biləcək bəndlər");
        sb.AppendLine("- Green: Aşkar risk yoxdur");
        sb.AppendLine();
        sb.AppendLine($"Fayl adı: {fileName}");
        sb.AppendLine("Sənəd mətni:");
        sb.AppendLine(truncated);

        return sb.ToString();
    }

    private static string BuildKonstruktorPrompt(string senedNovu, string musteriAd,
        int gecikmeGun, decimal meble, string? elaveMelumat)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Siz bankın rəsmi yazışma mütəxəssisisiniz. Aşağıdakı parametrlərə əsasən tam rəsmi bank sənədi hazırlayın.");
        sb.AppendLine();
        sb.AppendLine($"Sənəd növü: {senedNovu}");
        sb.AppendLine($"Müştəri adı: {musteriAd}");
        sb.AppendLine($"Gecikmiş gün sayı: {gecikmeGun}");
        sb.AppendLine($"Məbləğ (AZN): {meble:F2}");
        sb.AppendLine($"Tarix: {DateTime.Now:dd MMMM yyyy}");
        if (!string.IsNullOrWhiteSpace(elaveMelumat))
            sb.AppendLine($"Əlavə məlumat: {elaveMelumat}");
        sb.AppendLine();
        sb.AppendLine("Tələblər:");
        sb.AppendLine("- Dil: Azərbaycan dili, rəsmi-işgüzar üslub");
        sb.AppendLine("- Format: düz mətn (markdown yox, HTML yox)");
        sb.AppendLine("- Sənəd tam strukturlu olmalıdır: başlıq, müraciət, əsas hissə, nəticə/xəbərdarlıq, imza sahəsi");
        sb.AppendLine("- Bank standartlarına uyğun rəsmi terminologiya istifadə edin");
        sb.AppendLine("- Yalnız sənədi yazın, heç bir izahat əlavə etməyin");

        return sb.ToString();
    }

    private static RiskAnalizResult ParseRiskResponse(string raw)
    {
        var jsonStart = raw.IndexOf('{');
        var jsonEnd = raw.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
            return new RiskAnalizResult { Xeta = "AI cavabı formatı düzgün deyil.", RiskLevel = RiskLevel.Yellow };

        try
        {
            var jsonStr = raw.Substring(jsonStart, jsonEnd - jsonStart + 1);
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            var levelStr = root.TryGetProperty("risk_level", out var rl) ? rl.GetString() ?? "Yellow" : "Yellow";
            var level = levelStr switch
            {
                "Red" => RiskLevel.Red,
                "Green" => RiskLevel.Green,
                _ => RiskLevel.Yellow
            };

            var clauses = new List<RiskyClause>();
            if (root.TryGetProperty("risky_clauses", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    clauses.Add(new RiskyClause
                    {
                        MaddeTipi = item.TryGetProperty("madde_tipi", out var mt) ? mt.GetString() ?? "" : "",
                        RiskliCumle = item.TryGetProperty("riskli_cumle", out var rc) ? rc.GetString() ?? "" : "",
                        ZararPotensiali = item.TryGetProperty("zarar_potensiali", out var zp) ? zp.GetString() ?? "" : "",
                        AlternativTeklif = item.TryGetProperty("alternativ_teklif", out var at) ? at.GetString() ?? "" : ""
                    });
                }
            }

            return new RiskAnalizResult { RiskLevel = level, RiskyClauslar = clauses };
        }
        catch
        {
            return new RiskAnalizResult { Xeta = "AI cavabı parse edilə bilmədi.", RiskLevel = RiskLevel.Yellow };
        }
    }
}
