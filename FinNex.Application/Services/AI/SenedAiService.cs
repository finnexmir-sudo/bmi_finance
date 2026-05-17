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

        var raw = await CallApiAsync(BuildRiskPrompt(text, fileName), 2000);
        if (raw == null)
            return new RiskAnalizResult { Xeta = "AI cavabı alınmadı." };

        return ParseRiskResponse(raw);
    }

    public async Task<KonstruktorResult> ConstructDocumentAsync(
        string senedNovu, string musteriAd, int gecikmeGun, decimal meble, string? elaveMelumat,
        string? senedMovzusu, bool tercumeEdilsinmi, string? hedafDil)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new KonstruktorResult { Xeta = "AI açarı konfiqurasiya edilməyib." };

        string prompt;
        bool isHtml = false;

        if (tercumeEdilsinmi)
        {
            var sourceText = !string.IsNullOrWhiteSpace(senedMovzusu) ? senedMovzusu : BuildDocTextForTranslation(senedNovu, musteriAd, gecikmeGun, meble, elaveMelumat);
            prompt = BuildTercumePrompt(sourceText, hedafDil ?? "İngilis dili");
            isHtml = true;
        }
        else if (!string.IsNullOrWhiteSpace(senedMovzusu))
        {
            prompt = BuildSerbrestPrompt(senedMovzusu, musteriAd);
        }
        else
        {
            prompt = BuildParametrikPrompt(senedNovu, musteriAd, gecikmeGun, meble, elaveMelumat);
        }

        var raw = await CallApiAsync(prompt, 2000);
        if (raw == null)
            return new KonstruktorResult { Xeta = "AI cavabı alınmadı." };

        return new KonstruktorResult { GeneratedContent = raw.Trim(), IsHtml = isHtml };
    }

    // ── HTTP ─────────────────────────────────────────────────────────────────

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
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiUrl, httpContent);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
    }

    // ── Prompt Builders ───────────────────────────────────────────────────────

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
        sb.AppendLine("Risk səviyyəsi: Red=yüksək risk, Yellow=orta risk, Green=aşkar risk yoxdur");
        sb.AppendLine();
        sb.AppendLine($"Fayl adı: {fileName}");
        sb.AppendLine("Sənəd mətni:");
        sb.AppendLine(truncated);
        return sb.ToString();
    }

    private static string BuildParametrikPrompt(string senedNovu, string musteriAd,
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
        sb.AppendLine("Tələblər: Azərbaycan dili, rəsmi-işgüzar üslub, düz mətn (markdown/HTML yox).");
        sb.AppendLine("Sənəd strukturu: başlıq → müraciət → əsas hissə → xəbərdarlıq/nəticə → imza sahəsi.");
        sb.AppendLine("Yalnız sənədi yazın, izahat əlavə etməyin.");
        return sb.ToString();
    }

    private static string BuildSerbrestPrompt(string senedMovzusu, string musteriAd)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sən bankın rəsmi sənəd mütəxəssisisən. İstifadəçinin aşağıdakı sərbəst təlimatını oxu:");
        sb.AppendLine("- Mətndəki şəxs adlarını, tarixləri, məbləğləri, müqavilə nömrələrini, gecikmiş gün sayını və digər əsas detalları intellektual olaraq müəyyən et.");
        sb.AppendLine("- Bu detalları özbaşına sənədin müvafiq bölümlərinə peşəkar şəkildə yerləşdir.");
        sb.AppendLine("- Mətndə açıq göstərilməyən detallar üçün bank üslubu ilə standart formul işlət.");
        sb.AppendLine($"Tarix: {DateTime.Now:dd MMMM yyyy}");
        sb.AppendLine();
        sb.AppendLine($"İstifadəçi təlimatı: {senedMovzusu}");
        if (!string.IsNullOrWhiteSpace(musteriAd))
            sb.AppendLine($"Əlaqədar şəxs: {musteriAd}");
        sb.AppendLine();
        sb.AppendLine("Tələblər: Azərbaycan dili, rəsmi-işgüzar üslub, düz mətn (markdown/HTML yox).");
        sb.AppendLine("Struktur: başlıq → müraciət → əsas hissə → nəticə → imza sahəsi.");
        sb.AppendLine("Yalnız sənədi yazın, izahat əlavə etməyin.");
        return sb.ToString();
    }

    private static string BuildTercumePrompt(string sourceText, string hedafDil)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Siz peşəkar bank və hüquq tərcüməçisisiniz. Aşağıdakı mətni {hedafDil} dilinə tərcümə edin.");
        sb.AppendLine();
        sb.AppendLine("Tələblər:");
        sb.AppendLine("- Terminoloji cəhətdən qüsursuz, rəsmi bank-hüquq dilinə uyğun tərcümə edin.");
        sb.AppendLine("- Orijinalın strukturunu, bölmə başlıqlarını və rəsmi tonunu tam qoruyun.");
        sb.AppendLine("- ÇIXIŞI MÜTLƏQ aşağıdakı HTML formatında verin (başqa heç nə yazmayın):");
        sb.AppendLine("<div class=\"sa-translated-doc\">");
        sb.AppendLine("  <h2>Başlıq</h2>");
        sb.AppendLine("  <p>Məzmun...</p>");
        sb.AppendLine("</div>");
        sb.AppendLine();
        sb.AppendLine("Tərcümə ediləcək mətn:");
        sb.AppendLine(sourceText.Length > 6000 ? sourceText.Substring(0, 6000) + "\n[...kəsildi]" : sourceText);
        return sb.ToString();
    }

    private static string BuildDocTextForTranslation(string senedNovu, string musteriAd,
        int gecikmeGun, decimal meble, string? elaveMelumat)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Sənəd növü: {senedNovu}");
        sb.AppendLine($"Müştəri: {musteriAd}");
        sb.AppendLine($"Gecikmiş gün: {gecikmeGun}");
        sb.AppendLine($"Məbləğ: {meble:F2} AZN");
        if (!string.IsNullOrWhiteSpace(elaveMelumat))
            sb.AppendLine(elaveMelumat);
        return sb.ToString();
    }

    public async Task<KonstruktorResult> OcrImageAsync(byte[] imageBytes, string mediaType, bool tercumeEdilsinmi, string hedafDil)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new KonstruktorResult { Xeta = "AI açarı konfiqurasiya edilməyib." };

        var prompt = BuildOcrPrompt(tercumeEdilsinmi, hedafDil);
        var raw = await CallApiWithImageAsync(imageBytes, mediaType, prompt, 3000);
        if (raw == null)
            return new KonstruktorResult { Xeta = "AI cavabı alınmadı." };

        return new KonstruktorResult { GeneratedContent = raw.Trim(), IsHtml = tercumeEdilsinmi };
    }

    private async Task<string?> CallApiWithImageAsync(byte[] imageBytes, string mediaType, string textPrompt, int maxTokens)
    {
        var request = new
        {
            model = Model,
            max_tokens = maxTokens,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = mediaType,
                                data = Convert.ToBase64String(imageBytes)
                            }
                        },
                        new { type = "text", text = textPrompt }
                    }
                }
            }
        };

        var client = _httpClientFactory.CreateClient("Anthropic");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var json = JsonSerializer.Serialize(request);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(ApiUrl, httpContent);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
    }

    private static string BuildOcrPrompt(bool withTranslation, string targetLang)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sən peşəkar bank OCR və skaner köməkçisisən.");
        sb.AppendLine("Bu şəkildəki / sənəddəki bütün mətnləri qüsursuz şəkildə oxu, strukturu (başlıq, abzaslar, cədvəl) itirmə.");
        if (withTranslation)
        {
            sb.AppendLine($"Oxuduğun mətni birbaşa {targetLang} dilinə bank-hüquq standartları ilə çevir.");
            sb.AppendLine("ÇIXIŞI MÜTLƏQ aşağıdakı HTML formatında ver (başqa heç nə yazma):");
            sb.AppendLine("<div class=\"sa-translated-doc\"><h2>Başlıq</h2><p>Məzmun...</p></div>");
        }
        else
        {
            sb.AppendLine("Mətni düz formatda (markdown/HTML yox) ver, strukturu qoru.");
            sb.AppendLine("Yalnız oxunan mətni ver, izahat əlavə etmə.");
        }
        return sb.ToString();
    }

    // ── Risk Response Parser ──────────────────────────────────────────────────

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
            var level = levelStr switch { "Red" => RiskLevel.Red, "Green" => RiskLevel.Green, _ => RiskLevel.Yellow };

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
