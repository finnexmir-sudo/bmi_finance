using FinNex.Application.Interfaces.AI;
using FinNex.Domain.Entities.AI;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FinNex.Application.Services.AI;

public class HRMeslehetciService : IHRMeslehetciService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    private const string ApiUrl    = "https://api.anthropic.com/v1/messages";
    private const string Model     = "claude-opus-4-7";
    private const int    MaxTokens = 2000;
    private const int    MaxTarixceMesaj = 20;

    // Fayldan oxunanda kontekstin maksimum uzunluğu (~150k karakter ≈ ~40k token)
    private const int MaxKontekstChar = 150_000;

    // ── Əsas sistem promptu (fallback: fayl olmadıqda istifadə olunur) ─────────
    private const string BaseSystemPrompt = @"Sən bankın rəsmi HR və Hüquq Məsləhətçisisən. Sənə verilən sualları Azərbaycan Respublikasının qanunvericiliyinə əsasən cavablandırmalısan.

=== STANDART QANUN BİLİYİ (Fayl yüklənmədikdə istifadə et) ===

1. ƏMƏK MƏCƏLLƏSİ
**Maddə 172** — Əməkhaqqının ödənilmə qaydası: pul formasında, Azərbaycan manatı ilə. Natura forması 20%-dən çox ola bilməz.
**Maddə 173** — Ödənilmə müddəti: ayda azı 2 dəfə.
**Maddə 174** — Tutulmaların əsasları: yalnız qanunda nəzərdə tutulmuş hallarda.
**Maddə 175** — Məhkəməsiz tutulma halları: avans borcu, hesablama xətası, yazılı razılıqla ziyan ödənişi.
**Maddə 176** — Tutulma limiti: hər ödənişdə NET-in **20%**-i; paralel tutulmalarda **50%**; alimentlər üçün 70%-ə qədər.
**Maddə 177** — İşdən çıxışda son ödəniş son iş günündə aparılır.
**Maddə 178** — Ölüm halında ailə üzvlərinə ödənilir.
**Maddə 179** — Mübahisəsiz hissə vaxtında ödənilməlidir.
**Maddə 180** — İflasda əməkhaqqı digər borclardan öndə gəlir.

2. VERGİ MƏCƏLLƏSİ
**Maddə 98** — Gəlir vergisi: rezident fiziki şəxslərin bütün gəlirlərinə tətbiq olunur; işəgötürən mənbədə tutur.
**Maddə 99** — İstisnalar: dövlət müavinətləri, alimentlər, sığorta hadisəsi ödənişləri.
**Maddə 100** — Standart güzəşt: aylıq gəliri müəyyən həddə qədər olan işçilər üçün.
**Maddə 101** — Tarif: 2500 AZN-ə qədər **14%**; yuxarısı üçün **350 AZN + 25%**.

3. DSMF
İşçi: 0–200 AZN → **3%**; 200+ AZN → **6 AZN + 10%**.
İşəgötürən: 0–200 → **22%**; 200–8200 → **44 AZN + 15%**; 8200+ → **1.214 AZN + 11%**.

4. İTSS VƏ İŞSİZLİK
İTSS (işçi+işəgötürən): 0–2500 → **2%**; 2500+ → **50 AZN + 0,5%**.
İşsizlik (hər iki tərəf): **0,5%** (düz faiz).

=== CAVAB QAYDALARIM ===
Qayda 1: İstinad etdiyin hər qanun maddəsini **qalın** formatda yaz (məs: **Maddə 176**).
Qayda 2: Sual kateqoriya xaricindədirsə, bunu bildir.
Qayda 3: Cavab Azərbaycan dilində olmalıdır.";

    // ── Kateqoriya təyini üçün prompt ────────────────────────────────────────
    private const string KateqoriyaPrompt = @"Aşağıdakı sual hansı Azərbaycan qanun kateqoriyasına aiddir?
Yalnız bu sözlərdən BİRİNİ qaytar (başqa heç nə yazma):
emek
vergi
dsmf
itss
hamisi

Sual: {0}";

    public HRMeslehetciService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = config["Anthropic:ApiKey"] ?? "";
    }

    // ── Mərhələ 1: Kateqoriya təyini ─────────────────────────────────────────
    public async Task<string> KateqoriyaTapAsync(string sual)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return "hamisi";

        var raw = await CallApiAsync(
            systemPrompt: "Yalnız verilmiş sözlərdən birini qaytar. Heç bir izahat yazma.",
            userMessage: string.Format(KateqoriyaPrompt, sual),
            maxTokens: 10);

        var result = (raw ?? "hamisi").Trim().ToLowerInvariant();
        return result is "emek" or "vergi" or "dsmf" or "itss" or "hamisi"
            ? result
            : "hamisi";
    }

    // ── Mərhələ 2: Kontekstual cavab ─────────────────────────────────────────
    public async Task<string> SualSorAsync(
        string sual,
        List<HRSohbetMesaj> tarixce,
        string? qanunKonteksti = null,
        string? menbeAdi = null)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return "AI açarı konfiqurasiya edilməyib.";

        string systemPrompt;

        if (!string.IsNullOrWhiteSpace(qanunKonteksti))
        {
            // Yüklənmiş fayl varsa — onu əsas götür
            var truncated = qanunKonteksti.Length > MaxKontekstChar
                ? qanunKonteksti[..MaxKontekstChar] + "\n\n[... sənəd kəsildi, həcm limiti aşıldı ...]"
                : qanunKonteksti;

            systemPrompt = $@"{BaseSystemPrompt}

=== YÜKLƏNMIŞ RƏSMİ SƏNƏD: {menbeAdi ?? "Naməlum"} ===
Aşağıdakı mətn rəsmi olaraq sistemə yüklənmiş sənədin tam mətnidir.
Cavabı ƏVVƏLCƏ bu sənəddən tap. Sənəddə olmayan məlumat üçün yuxarıdakı standart bilikdən istifadə et.
Cavabının SONUNA mütləq bu cümləni əlavə et: ""Mənbə: {menbeAdi ?? "Yüklənmiş rəsmi fayl"} (Yüklənmiş rəsmi sənəd)""

--- SƏNƏD MƏTNİ BAŞLAYIR ---
{truncated}
--- SƏNƏD MƏTNİ BİTİR ---";
        }
        else
        {
            systemPrompt = BaseSystemPrompt;
        }

        var messages = tarixce
            .TakeLast(MaxTarixceMesaj)
            .Select(m => new { role = m.Rol, content = m.Metn })
            .ToList<object>();

        messages.Add(new { role = "user", content = sual });

        var cavab = await CallApiAsync(systemPrompt, messages, MaxTokens);
        return cavab ?? "AI cavabı alınmadı. Zəhmət olmasa yenidən cəhd edin.";
    }

    // ── Köməkçi: Tək mesaj sorğusu ───────────────────────────────────────────
    private async Task<string?> CallApiAsync(string systemPrompt, string userMessage, int maxTokens)
    {
        return await CallApiAsync(systemPrompt,
            new List<object> { new { role = "user", content = userMessage } },
            maxTokens);
    }

    // ── Köməkçi: Çox mesajlı sorğu ───────────────────────────────────────────
    private async Task<string?> CallApiAsync(string systemPrompt, List<object> messages, int maxTokens)
    {
        var request = new { model = Model, max_tokens = maxTokens, system = systemPrompt, messages };

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
