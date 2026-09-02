using System.Security.Claims;
using FinNex.Application.DTOs.Kredit.Arayis;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Helpers.Kredit;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Domain;
using FinNex.UI.Areas.User.ViewModels;
using FinNex.UI.Services.Kredit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

/// <summary>
/// KREDİT ARAYIŞLARI — BMI «Kredit DP → Arayışlar» menyusunun portu (02.09.2026).
///
/// BMI-də 7 bənd var idi, amma **yalnız 4-ü işləyirdi** — «BTİ arayış»,
/// «Qeydiyyata düşmə» və «İcarə məktubu» handler-siz ölü menyu bəndləri idi
/// (nə forma, nə sorğu, nə şablon). Buraya yalnız işləyən 4-ü köçürüldü.
///
/// HAMISI EYNİ SXEMDƏDİR:
///   giriş → (bəzən Oracle axtarışı) → Qeydiyyat № → jurnal sətri → Word.
///
/// NÖMRƏ VƏ JURNAL — `IXaricMektubService`:
/// BMI hər dördündə Oracle-dakı `odb.xaric_mektub`-a INSERT edirdi. Həmin
/// jurnal 12.08.2026-da FinNex-ə köçürülüb (`XaricMektub`), Oracle-a yazı
/// tamamilə bağlanıb. Yəni burada Oracle YALNIZ OXUNUR.
///
/// ⚠️ BMI-DƏKİ NÖMRƏ YARIŞI TƏKRARLANMIR: orada nömrə `max+1` ilə hesablanıb
/// Word-ə yazılır, sonra INSERT edilirdi — özü də `qey_nom` sütununu YAZMADAN
/// (onu Oracle təyin edirdi). Yəni sənəddəki nömrə ilə jurnaldakı nömrə
/// fərqlənə bilərdi. Burada `YaratAsync` nömrəni ayırır, jurnala yazır və
/// ELƏ HƏMİN nömrəni qaytarır — sənədə yazılan nömrə jurnaldakıdır.
///
/// PREVIEW REJİMİ: `KreditArayis:NomreYaz = false` (defolt) olduqda jurnala
/// HEÇ NƏ yazılmır və sənəddə növbəti nömrənin ÖNİZLƏMƏSİ görünür. Yoxlamadan
/// sonra `true` edilir. Nömrə bir dəfə veriləndən sonra geri qaytarılmır.
/// </summary>
[Area("User")]
[Authorize]
public class KreditArayisController : Controller
{
    private readonly IKreditArayisService _arayis;
    private readonly IXaricMektubService _mektub;
    private readonly IKreditBaxanIsciService _baxanIsci;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly bool _nomreYaz;

    public KreditArayisController(
        IKreditArayisService arayis,
        IXaricMektubService mektub,
        IKreditBaxanIsciService baxanIsci,
        UserManager<AppUser> userManager,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _arayis = arayis;
        _mektub = mektub;
        _baxanIsci = baxanIsci;
        _userManager = userManager;
        _env = env;
        _config = config;
        _nomreYaz = config.GetValue("KreditArayis:NomreYaz", false);
    }

    // ── Giriş nəzarəti ───────────────────────────────────────────────
    // Kredit müraciətləri ilə EYNİ qayda: Admin/KreditAdmin, ya da Admin
    // panelindən təyin edilmiş «kredit baxan işçi». Ayrıca siyahı QURULMUR —
    // iki siyahı saxlansa biri mütləq köhnə qalır.
    // ⚠️ «IsciId» adlı CLAIM YOXDUR — işçi bağı `AppUser.IsciId` sahəsindədir və
    // yalnız UserManager ilə oxunur. `KreditMuracietController.GetAccessAsync`
    // da eyni yolla işləyir; claim-dən oxumağa çalışsaq dəyər həmişə null olar
    // və HEÇ BİR XƏTA ÇIXMADAN hər kəs 403 alar.
    private async Task<int?> IsciIdAsync()
        => (await _userManager.GetUserAsync(User))?.IsciId;

    private bool IsAdmin => User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.KreditAdmin);

    private async Task<bool> GirisVarAsync()
    {
        if (IsAdmin) return true;
        var id = await IsciIdAsync();
        return id.HasValue && await _baxanIsci.BaxaBilerMiAsync(id.Value);
    }

    private IActionResult Icazesiz() => Forbid();

    // ══════════════════════════════════════════════════════════════════
    //  DYP ARAYIŞ — avtomobilin girovdan çıxması (axtarış yoxdur)
    // ══════════════════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Dyp()
    {
        if (!await GirisVarAsync()) return Icazesiz();
        await NomreOnizleAsync();
        ViewData["Title"] = "DYP arayış";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dyp(DypArayisVM m)
    {
        if (!await GirisVarAsync()) return Icazesiz();

        var sablon = SablonYolu("DYP_arayis_girovdan_cixma.docx");
        if (!System.IO.File.Exists(sablon))
        {
            TempData["Error"] = $"Şablon tapılmadı: {Path.GetFileName(sablon)}. " +
                                "Fayl wwwroot/Files/Word/Kredit/Arayis qovluğunda olmalıdır. " +
                                "Nömrə ayrılmadı, jurnala heç nə yazılmadı.";
            await NomreOnizleAsync();
            return View(m);
        }

        var (mekNo, xeta) = await NomreAyirAsync(
            gonYer: "DYP",
            qisaMez: $"avto gir çıx {m.Musteri}".Trim(),
            tarix: m.MektubTarixi ?? DateTime.Today);
        if (xeta != null) { TempData["Error"] = xeta; await NomreOnizleAsync(); return View(m); }

        var tokenler = new Dictionary<string, string?>
        {
            ["{mekNo}"]     = mekNo,
            ["{mektarixi}"] = KreditSozeCevir.TarixiSoze(m.MektubTarixi ?? DateTime.Today),
            ["{borcalan}"]  = m.Musteri,
            ["{muqtar}"]    = m.MuqavileTarixi.HasValue ? KreditSozeCevir.TarixiSoze(m.MuqavileTarixi.Value) : "",
            ["{muqNo}"]     = m.MuqavileNo,
            ["{avtoNo}"]    = m.AvtoNo,
            ["{marka}"]     = m.Marka,
            ["{avtoil}"]    = m.BuraxilisIli,
            ["{muh}"]       = m.Muherrik,
            ["{ban}"]       = m.Ban,
            ["{reng}"]      = m.Reng,
        };

        var data = KreditWordService.Doldur(sablon, tokenler);
        return WordCavab(data, $"DYP arayis {m.AvtoNo}");
    }

    // ══════════════════════════════════════════════════════════════════
    //  BORCALAN TƏMİZLİK ARAYIŞI — axtarış: qeydiyyat kodu (regnom)
    // ══════════════════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Borcalan(string? regnom)
    {
        if (!await GirisVarAsync()) return Icazesiz();
        await NomreOnizleAsync();

        ViewBag.Regnom = regnom;
        ViewBag.Netice = string.IsNullOrWhiteSpace(regnom)
            ? new List<BorcalanArayisSatirDto>()
            : await SorguAsync(() => _arayis.BorcalanAxtarAsync(regnom!));

        ViewData["Title"] = "Borcalan təmizlik arayışı";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borcalan(BorcalanArayisVM m)
    {
        if (!await GirisVarAsync()) return Icazesiz();

        var sablon = SablonYolu("Borcalan_temizlik_arayisi.docx");
        if (!System.IO.File.Exists(sablon))
        {
            TempData["Error"] = $"Şablon tapılmadı: {Path.GetFileName(sablon)}. Nömrə ayrılmadı.";
            return RedirectToAction(nameof(Borcalan), new { regnom = m.Regnom });
        }

        var (mekNo, xeta) = await NomreAyirAsync(
            gonYer: m.Borcalan,
            qisaMez: "Borcalan təmizlik arayışı",
            tarix: m.MektubTarixi ?? DateTime.Today);
        if (xeta != null)
        {
            TempData["Error"] = xeta;
            return RedirectToAction(nameof(Borcalan), new { regnom = m.Regnom });
        }

        var tokenler = new Dictionary<string, string?>
        {
            ["{mekNo}"]     = mekNo,
            ["{mektarixi}"] = KreditSozeCevir.TarixiSoze(m.MektubTarixi ?? DateTime.Today),
            ["{muqtar}"]    = m.MuqavileTarixi.HasValue ? KreditSozeCevir.TarixiSoze(m.MuqavileTarixi.Value) : "",
            ["{borcalan}"]  = m.Borcalan,
            ["{muqno}"]     = m.MuqavileNo,
            ["{mebleg}"]    = MeblegMetni(m.Mebleg, m.Valyuta),
        };

        var data = KreditWordService.Doldur(sablon, tokenler);
        return WordCavab(data, $"Borcalan arayis {m.Borcalan}");
    }

    // ══════════════════════════════════════════════════════════════════
    //  ZAMİN TƏMİZLİK ARAYIŞI — axtarış: zaminin FİN kodu
    // ══════════════════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Zamin(string? fin)
    {
        if (!await GirisVarAsync()) return Icazesiz();
        await NomreOnizleAsync();

        ViewBag.Fin = fin;
        ViewBag.Netice = string.IsNullOrWhiteSpace(fin)
            ? new List<ZaminArayisSatirDto>()
            : await SorguAsync(() => _arayis.ZaminAxtarAsync(fin!));

        ViewData["Title"] = "Zamin təmizlik arayışı";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Zamin(ZaminArayisVM m)
    {
        if (!await GirisVarAsync()) return Icazesiz();

        var sablon = SablonYolu("Zamin_temizlik_arayisi.docx");
        if (!System.IO.File.Exists(sablon))
        {
            TempData["Error"] = $"Şablon tapılmadı: {Path.GetFileName(sablon)}. Nömrə ayrılmadı.";
            return RedirectToAction(nameof(Zamin), new { fin = m.Fin });
        }

        var (mekNo, xeta) = await NomreAyirAsync(
            gonYer: m.Zamin,
            qisaMez: "Zamin borcun baglanması",   // BMI mətni — dəyişdirilmədi
            tarix: m.MektubTarixi ?? DateTime.Today);
        if (xeta != null)
        {
            TempData["Error"] = xeta;
            return RedirectToAction(nameof(Zamin), new { fin = m.Fin });
        }

        // ⚠️ `{krtar}` QƏSDƏN YOXDUR. BMI onu doldururdu (üstəlik zaminin adı ilə —
        // səhv görünürdü), amma `Zaminarayis1.docx` şablonunda belə token
        // ÜMUMİYYƏTLƏ YOXDUR — yəni dəyər heç yerə düşmürdü. Yoxlanıldı 02.09.2026.
        var tokenler = new Dictionary<string, string?>
        {
            ["{mekNo}"]     = mekNo,
            ["{mektarixi}"] = KreditSozeCevir.TarixiSoze(m.MektubTarixi ?? DateTime.Today),
            ["{muqtar}"]    = m.MuqavileTarixi.HasValue ? KreditSozeCevir.TarixiSoze(m.MuqavileTarixi.Value) : "",
            ["{borcalan}"]  = m.Borcalan,
            ["{zamin}"]     = m.Zamin,
            ["{mebleg}"]    = MeblegMetni(m.Mebleg, m.Valyuta),
        };

        var data = KreditWordService.Doldur(sablon, tokenler);
        return WordCavab(data, $"Zamin arayis {m.Zamin}");
    }

    // ══════════════════════════════════════════════════════════════════
    //  SAİPA — girovdan çıxma / texpasport dəyişmə (iki rejim, iki şablon)
    // ══════════════════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Saipa()
    {
        if (!await GirisVarAsync()) return Icazesiz();
        await NomreOnizleAsync();
        ViewData["Title"] = "Saipa — girovdan çıxma";
        return View(new SaipaArayisVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Saipa(SaipaArayisVM m)
    {
        if (!await GirisVarAsync()) return Icazesiz();

        var texpasport = m.Rejim == SaipaArayisVM.RejimTexpasport;

        // Texpasport rejimində şəhadətnamə nömrəsi sənədin ƏSAS rəqəmidir —
        // boş getsə məktub mənasız olar. Nömrədən ƏVVƏL yoxlanır.
        if (texpasport && string.IsNullOrWhiteSpace(m.TexpasportNo))
        {
            TempData["Error"] = "Texpasport dəyişmə üçün şəhadətnamə nömrəsi yazılmalıdır. Nömrə ayrılmadı.";
            await NomreOnizleAsync();
            return View(m);
        }

        var sablonAdi = texpasport ? "Saipa_texpasport_deyisme.docx" : "Saipa_girovdan_cixma.docx";
        var sablon = SablonYolu(sablonAdi);
        if (!System.IO.File.Exists(sablon))
        {
            TempData["Error"] = $"Şablon tapılmadı: {sablonAdi}. Nömrə ayrılmadı.";
            await NomreOnizleAsync();
            return View(m);
        }

        var (mekNo, xeta) = await NomreAyirAsync(
            gonYer: "DYP",   // BMI-də də sabit "DYP"-dir
            qisaMez: m.AvtoNo + (texpasport ? " avto texpasport dəyişmə Cars" : " avto gir azad Cars"),
            tarix: m.MektubTarixi ?? DateTime.Today);
        if (xeta != null) { TempData["Error"] = xeta; await NomreOnizleAsync(); return View(m); }

        // ⚠️ `carsgirovcix1.docx`-də `{mektarixi}` tokeni YOXDUR (02.09.2026
        // yoxlanıldı) — məktubun tarixi o sənəddə çap olunmur. Token yenə də
        // göndərilir: şablona sonradan əlavə edilsə kod dəyişməsin.
        var tokenler = new Dictionary<string, string?>
        {
            ["{mekNo}"]     = mekNo,
            ["{mektarixi}"] = KreditSozeCevir.TarixiSoze(m.MektubTarixi ?? DateTime.Today),
            ["{muqtar}"]    = m.MuqavileTarixi.HasValue ? KreditSozeCevir.TarixiSoze(m.MuqavileTarixi.Value) : "",
            ["{avtoNo}"]    = m.AvtoNo,
            ["{avtoil}"]    = m.BuraxilisIli,
            ["{muh}"]       = m.Muherrik,
            ["{ban}"]       = m.Ban,
            ["{reng}"]      = m.Reng,
            ["{texpNo}"]    = m.TexpasportNo,
        };

        var data = KreditWordService.Doldur(sablon, tokenler);
        var ad = texpasport ? $"Saipa texpasport {m.AvtoNo}" : $"Saipa girovdan cixma {m.AvtoNo}";
        return WordCavab(data, ad);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Köməkçilər
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Qeydiyyat nömrəsini ayırır və jurnala sətir yazır.
    ///
    /// `NomreYaz=false` (preview) olduqda HEÇ NƏ YAZILMIR — yalnız növbəti
    /// nömrənin önizləməsi qaytarılır. Önizləmə ilə real nömrə EYNİ düsturdan
    /// gəlir (`NovbetiNomreAsync`), ona görə ekranda görünən rəqəm real rejimə
    /// keçəndə dəyişmir (jurnal arada dəyişməyibsə).
    ///
    /// Qaytarır: (nömrə mətni, xəta). Xəta null deyilsə HEÇ NƏ yazılmayıb.
    /// </summary>
    private async Task<(string mekNo, string? xeta)> NomreAyirAsync(string? gonYer, string? qisaMez, DateTime tarix)
    {
        var il = tarix.Year;

        if (!_nomreYaz)
            return ($"{il}-{await _mektub.NovbetiNomreAsync(il)} (ÖNİZLƏMƏ)", null);

        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : 0;
        if (userId == 0) return ("", "İstifadəçi təyin olunmadı — nömrə ayrılmadı.");

        var res = await _mektub.YaratAsync(new XaricMektubCreateDto
        {
            Tarix   = tarix,
            GonYer  = gonYer,
            QisaMez = qisaMez
        }, userId);

        return res.Success
            ? ($"{il}-{res.Data}", null)
            : ("", res.Message ?? "Qeydiyyat nömrəsi ayrıla bilmədi — jurnala heç nə yazılmadı.");
    }

    /// <summary>Ekranda «növbəti nömrə» göstərmək üçün — heç nə yazmır.</summary>
    private async Task NomreOnizleAsync()
    {
        var il = DateTime.Today.Year;
        try { ViewBag.NovbetiNomre = $"{il}-{await _mektub.NovbetiNomreAsync(il)}"; }
        catch { ViewBag.NovbetiNomre = null; }
        ViewBag.NomreYaz = _nomreYaz;
    }

    /// <summary>
    /// Oracle sorğusunu icra edir; xəta olsa səhifə açıq qalsın deyə boş siyahı
    /// qaytarır və mesajı TempData-ya yazır (BMI-də boş `catch` var idi — orada
    /// xəta izsiz itirdi).
    /// </summary>
    private async Task<List<T>> SorguAsync<T>(Func<Task<List<T>>> sorgu)
    {
        try { return await sorgu(); }
        catch (Exception ex)
        {
            TempData["Error"] = "Oracle sorğusu icra olunmadı: " + ex.Message;
            return new List<T>();
        }
    }

    private string SablonYolu(string faylAdi)
    {
        // Müqavilə şablonları ilə eyni kök; arayışlar öz alt qovluğundadır.
        var root = _config["KreditMuqavile:TemplateRoot"];
        var koku = string.IsNullOrWhiteSpace(root)
            ? Path.Combine(_env.WebRootPath, "Files", "Word", "Kredit")
            : root;
        return Path.Combine(koku, "Arayis", faylAdi);
    }

    /// <summary>
    /// BMI: `məbləğ + " (" + sözlə + ")"`. Şablonda tokendən sonra onsuz da
    /// valyuta kodu yazılıb («{mebleg} AZN məbləğində»), ona görə rəqəm hissəsi
    /// valyutasızdır — BMI ilə eyni.
    /// </summary>
    private static string MeblegMetni(decimal? mebleg, string? valyuta)
    {
        var m = mebleg ?? 0m;
        var reqem = m.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return $"{reqem} ({KreditSozeCevir.MebleghSozeValyuta(m, valyuta)})";
    }

    private IActionResult WordCavab(byte[] data, string ad)
    {
        var temiz = new string(ad.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
        return File(data,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"{temiz}.docx");
    }
}
