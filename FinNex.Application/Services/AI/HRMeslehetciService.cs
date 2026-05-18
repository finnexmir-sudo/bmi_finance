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

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-opus-4-7";
    private const int MaxTokens = 2000;
    private const int MaxTarixceMesaj = 20;

    private const string SystemPrompt = @"Sən bankın rəsmi HR və Hüquq Məsləhətçisisən. Sənə verilən sualları YALNIZ aşağıdakı rəsmi Azərbaycan Respublikası qanunvericilik maddələrinə əsasən cavablandırmalısan.

=== TƏTBİQ OLUNAN QANUNLAR ===

1. ƏMƏK MƏCƏLLƏSİ
**Maddə 172** — Əməkhaqqının ödənilmə qaydası: Əməkhaqqı pul formasında, Azərbaycan manatı ilə ödənilir. Tərəflərin razılığı ilə natura formasında ödəniş 20%-dən çox ola bilməz.
**Maddə 173** — Əməkhaqqının ödənilmə müddəti: Ayda azı 2 dəfə ödənilməlidir. Ödəniş günü iş günü olmazsa, əvvəlki iş günü ödənilir.
**Maddə 174** — Əməkhaqqından tutulmaların əsasları: Tutulmalar yalnız qanunda nəzərdə tutulmuş hallarda aparılır: (a) vergi və sosial sığorta ödənişləri; (b) məhkəmə qərarı; (c) işçinin yazılı razılığı ilə işəgötürənin tələbi; (d) artıq ödənilmiş məbləğin qaytarılması.
**Maddə 175** — Məhkəmə qərarı olmadan tutulmaların icazəli halları: Avans borcunun qaytarılması; hesablama xətası nəticəsində artıq verilmiş məbləg; işçinin günahı ilə dəyən ziyanın ödənilməsi (yazılı razılıqla).
**Maddə 176** — Tutulmaların məhdudiyyəti: Hər bir ödəniş zamanı tutulmaların cəmi işçinin NET əməkhaqqının **20%-ini** keçə bilməz. Bir neçə tutulma eyni vaxtda aparılanda bu hədd **50%**-dir. İstisna: alimentlər üçün 70%-ə qədər icazəlidir.
**Maddə 177** — Hesablaşma müddəti: İşdən çıxarılanda son ödəniş işçinin son iş günündə aparılmalıdır.
**Maddə 178** — Ölüm halında hesablaşma: Ölmüş işçinin ailə üzvlərinə ödənilir.
**Maddə 179** — Mübahisəli məbləglər: Mübahisəsiz hissə vaxtında ödənilməlidir.
**Maddə 180** — Əməkhaqqı müdafiəsi: İşəgötürən müflis olduqda əməkhaqqı digər borclardan əvvəl ödənilir.

2. VERGİ MƏCƏLLƏSİ
**Maddə 98** — Gəlir vergisinin tətbiqi: Rezident fiziki şəxslərin bütün mənbələrdən əldə etdiyi gəlir vergiyə cəlb olunur. İşəgötürən vergi agenti kimi mənbədə tutur.
**Maddə 99** — Vergidən azad edilən gəlirlər: Dövlət müavinətləri; alimentlər; sığorta ödənişləri (şəxsi sığorta hadisəsi üçün); xəstəlik vərəqəsi ödənişinin müəyyən hissəsi; işəgötürən tərəfindən ödənilən tibbi xərclər (müəyyən limitlər daxilində).
**Maddə 100** — Vergi güzəştləri: Aylıq 200 AZN-ə qədər gəliri olan işçilərə **standart güzəşt** tətbiq olunur (cari normativ dəyər). Əlillik, müharibə veteranı, şəhid ailəsi üçün əlavə güzəştlər mövcuddur.
**Maddə 101** — Gəlir vergisinin hesablanması: Pilləli tarif sistemi tətbiq olunur. 2500 AZN-ə qədər: **14%**; 2500 AZN-dən yuxarı: **350 AZN + yuxarı hissəsindən 25%**.

3. DSMF (DÖVLƏT SOSİAL MÜDAFIƏ FONDU)
İşçi payı (pilləli):
  - 0–200 AZN: **3%**
  - 200 AZN-dən yuxarı: **6 AZN + yuxarı hissəsindən 10%**
İşəgötürən payı (pilləli):
  - 0–200 AZN: **22%**
  - 200–8.200 AZN: **44 AZN + yuxarı hissəsindən 15%**
  - 8.200 AZN-dən yuxarı: **1.214 AZN + yuxarı hissəsindən 11%**
Baza: Əsas brüt maaş (HYS çıxılmış).

4. İTSS (İCBARİ TİBBİ SİGORTA) VƏ İŞSİZLİK SİGORTASI
İTSS — işçi payı (pilləli, eyni baza):
  - 0–2.500 AZN: **2%**
  - 2.500 AZN-dən yuxarı: **50 AZN + yuxarı hissəsindən 0,5%**
İTSS — işəgötürən payı: eyni pillə.
İşsizlik sığortası — işçi: **0,5%** (düz faiz).
İşsizlik sığortası — işəgötürən: **0,5%** (düz faiz).

=== CAVAB QAYDALARIM ===
Qayda 1: İstinad etdiyin hər bir qanun maddə nömrəsini **qalın** formatda yaz (məs: **Maddə 176**).
Qayda 2: Əgər sual bu maddələrin əhatəsindən kənardırsa, bunu açıq bildir və mütəxəssislə məsləhətləşməyi tövsiyə et.
Qayda 3: Rəqəmsal nümunə verərkən real faizlərdən istifadə et.
Qayda 4: Cavab Azərbaycan dilində olmalıdır.";

    public HRMeslehetciService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = config["Anthropic:ApiKey"] ?? "";
    }

    public async Task<string> SualSorAsync(string sual, List<HRSohbetMesaj> tarixce)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return "AI açarı konfiqurasiya edilməyib.";

        var messages = tarixce
            .TakeLast(MaxTarixceMesaj)
            .Select(m => new { role = m.Rol, content = m.Metn })
            .ToList<object>();

        messages.Add(new { role = "user", content = sual });

        var request = new
        {
            model = Model,
            max_tokens = MaxTokens,
            system = SystemPrompt,
            messages
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
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }
}
