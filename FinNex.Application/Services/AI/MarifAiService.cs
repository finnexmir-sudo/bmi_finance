using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Marif;
using FinNex.Application.Interfaces.AI;
using FinNex.Domain.Entities.Marif;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace FinNex.Application.Services.AI
{
    /// <summary>
    /// Daxili Maarifləndirmə — qayda mətnindən sual qaralaması hazırlayan AI
    /// çağırışı (27.10.2026). <see cref="HRMeslehetciService"/> ilə eyni
    /// Anthropic açarını/klientini işlədir (ayrıca abunəlik lazım deyil),
    /// amma AYRI servisdir — bu, kontekstual Soru-Cavab yox, bir dəfəlik
    /// məzmun generasiyasıdır.
    /// </summary>
    public class MarifAiService : IMarifAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;

        private const string ApiUrl = "https://api.anthropic.com/v1/messages";
        private const string Model = "claude-opus-4-7";
        private const int MaxKontekstChar = 150_000;

        public MarifAiService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = config["Anthropic:ApiKey"] ?? "";
        }

        private const string SystemPrompt = @"Sən bankın daxili maarifləndirmə mütəxəssisisən. Sənə bir qayda/sənəd mətni
verilir, sən ondan işçilər üçün bilik-yoxlama sualları hazırlayırsan.

QAYDALAR:
1. Yalnız verilmiş mətndəki məlumata əsaslan — uydurma qayda/rəqəm yazma.
2. Sualların TƏXMİNƏN YARISI real iş vəziyyəti üzərindən olsun (""Bu halda nə edərdiniz?"" tipli), quru tərif sorğusu yox.
3. Hər sualın izahı DƏRHAL cavabdan sonra göstəriləcək — izahda NİYƏ düzgün/səhv olduğunu və mətndəki müvafiq bəndə/qaydaya qısa istinadı yaz.

YANLIŞ VARİANTLARIN (çoxseçimli) QAYDASI — bu, ən vacib hissədir, işçi forma oxuyub
yox, QAYDANI bilərək tapmalıdır:
4. Dörd variant da OXŞAR uzunluqda və detal səviyyəsində olsun — düzgün cavab digərlərindən
   açıq-aşkar daha uzun/dəqiq/professional səslənməsin (uzunluq özü ipucu olmasın).
5. Yanlış variantlar TƏSADÜFİ və ya absurd olmasın — mətndəki REAL qaydaya YAXIN, amma
   konkret bir nöqtədə səhv olan ""ağlabatan səhvlər"" olsun: yaxın rəqəm/faiz/müddət
   (məs. qaydada 20% limit varsa yanlış variant 10% və ya 50% yaza bilər, 99% yox),
   qonşu/tez-tez qarışdırılan bir qaydanın şərti, köhnəlmiş/ləğv olunmuş qayda, yarımçıq
   və ya natamam tətbiq olunan şərt. Hər yanlış variant elə yazılsın ki, mətni SƏTHİ
   oxuyan adam onu doğru sana bilsin.
6. ""Yuxarıdakıların hamısı"" / ""Heç biri düz deyil"" kimi variantlar İŞLƏTMƏ — bunlar
   real bilik yoxlamadan yan keçən, tənbəl seçimlərdir.
7. Düzgün cavabın yeri (A/B/C/D) təsadüfi paylansın — eyni sorğuda ardıcıl suallarda
   həmişə eyni hərfə (məs. hamısı B) cəmləmə.
8. Cavabını YALNIZ aşağıdakı JSON formatında qaytar, başqa heç nə yazma (izahat, markdown kod bloku, giriş cümləsi YOX):

{
  ""suallar"": [
    {
      ""tip"": ""coxsecimli"",
      ""sual"": ""..."",
      ""variantA"": ""..."",
      ""variantB"": ""..."",
      ""variantC"": ""..."",
      ""variantD"": ""..."",
      ""duzgunVariant"": ""B"",
      ""izah"": ""...""
    },
    {
      ""tip"": ""aciq"",
      ""sual"": ""..."",
      ""duzgunCavab"": ""..."",
      ""izah"": ""...""
    }
  ]
}";

        public async Task<Result<List<MarifSualDto>>> SuallarHazirlaAsync(string menbeMetn, int sualSayi)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return Result<List<MarifSualDto>>.Fail("AI açarı konfiqurasiya edilməyib.");

            if (string.IsNullOrWhiteSpace(menbeMetn))
                return Result<List<MarifSualDto>>.Fail("Mənbə mətn boşdur.");

            if (sualSayi < 1) sualSayi = 1;
            if (sualSayi > 15) sualSayi = 15;

            var truncated = menbeMetn.Length > MaxKontekstChar
                ? menbeMetn[..MaxKontekstChar] + "\n\n[... sənəd kəsildi, həcm limiti aşıldı ...]"
                : menbeMetn;

            var userMessage = $@"Aşağıdakı mətndən {sualSayi} sual hazırla (yarısı çoxseçimli, yarısı açıq — tam bərabər bölünmürsə çoxseçimli üstünlük tutsun):

--- MƏTN BAŞLAYIR ---
{truncated}
--- MƏTN BİTİR ---";

            var raw = await CallApiAsync(userMessage, Math.Max(1500, sualSayi * 500));
            if (raw == null)
                return Result<List<MarifSualDto>>.Fail("AI cavabı alınmadı. Zəhmət olmasa yenidən cəhd edin.");

            var suallar = Parse(raw);
            if (suallar == null || suallar.Count == 0)
                return Result<List<MarifSualDto>>.Fail("AI cavabı anlaşılmadı (JSON formatı gözlənildiyi kimi deyil). Yenidən cəhd edin.");

            return Result<List<MarifSualDto>>.Ok(suallar);
        }

        // ── JSON parse — AI bəzən ```json ... ``` bloku ilə əhatələyir, qoruyucu təmizlik ──
        private static List<MarifSualDto>? Parse(string raw)
        {
            var text = raw.Trim();
            if (text.StartsWith("```"))
            {
                var ilkSetirSonu = text.IndexOf('\n');
                if (ilkSetirSonu >= 0) text = text[(ilkSetirSonu + 1)..];
                var sonFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (sonFence >= 0) text = text[..sonFence];
                text = text.Trim();
            }

            try
            {
                using var doc = JsonDocument.Parse(text);
                if (!doc.RootElement.TryGetProperty("suallar", out var array)) return null;

                var netice = new List<MarifSualDto>();
                int sira = 0;
                foreach (var s in array.EnumerateArray())
                {
                    var tipStr = s.TryGetProperty("tip", out var t) ? t.GetString() ?? "" : "";
                    var tip = tipStr.Trim().ToLowerInvariant() == "aciq"
                        ? MarifSualTipi.Acig
                        : MarifSualTipi.CoxSecimli;

                    var dto = new MarifSualDto
                    {
                        Tip = tip,
                        SualMetni = S(s, "sual"),
                        IzahMetni = S(s, "izah"),
                        Sira = sira++
                    };

                    if (tip == MarifSualTipi.CoxSecimli)
                    {
                        dto.VariantA = S(s, "variantA");
                        dto.VariantB = S(s, "variantB");
                        dto.VariantC = S(s, "variantC");
                        dto.VariantD = S(s, "variantD");
                        dto.DuzgunVariant = S(s, "duzgunVariant").Trim().ToUpperInvariant();
                    }
                    else
                    {
                        dto.DuzgunCavabMetni = S(s, "duzgunCavab");
                    }

                    if (!string.IsNullOrWhiteSpace(dto.SualMetni))
                        netice.Add(dto);
                }

                return netice;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string S(JsonElement e, string prop) =>
            e.TryGetProperty(prop, out var v) ? (v.GetString() ?? "") : "";

        private async Task<string?> CallApiAsync(string userMessage, int maxTokens)
        {
            var request = new
            {
                model = Model,
                max_tokens = maxTokens,
                system = SystemPrompt,
                messages = new object[] { new { role = "user", content = userMessage } }
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
    }
}
