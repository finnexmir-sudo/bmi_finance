using FinNex.Application.DTOs.Kredit.Muqavile;
using FinNex.Application.Helpers.Kredit;
using FinNex.Application.Interfaces.Kredit;
using FinNex.UI.Services.Kredit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class KreditMuqavileController : Controller
{
    private readonly IKreditMuqavileService _muqavileService;
    private readonly IKreditMuqavileNomreService _nomreService;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    // Müqavilə tipləri — BMI-dəki "Müqavilə tipi" dropdown ilə eyni.
    public static readonly string[] MuqavileTipleri =
    {
        "Avtomobil", "Zaminlik", "Daşınmaz Əmlak", "Qızıl girovu", "Əlavə"
    };

    public KreditMuqavileController(
        IKreditMuqavileService muqavileService,
        IKreditMuqavileNomreService nomreService,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _muqavileService = muqavileService;
        _nomreService = nomreService;
        _config = config;
        _env = env;
    }

    // Səviyyə 1 — Seçim səhifəsi: tarixə görə verilmiş kreditlərin siyahısı.
    public async Task<IActionResult> Index(DateTime? tarix, string? tip)
    {
        var seciliTarix = tarix ?? DateTime.Today;

        var kreditler = new List<KreditMuqavileSatirDto>();
        try
        {
            kreditler = await _muqavileService.KreditleriGetirAsync(seciliTarix);
        }
        catch (Exception ex)
        {
            ViewBag.Xeta = "Oracle-dan məlumat alınmadı: " + ex.Message;
        }

        ViewBag.SeciliTarix = seciliTarix;
        ViewBag.SeciliTip = MuqavileTipleri.Contains(tip) ? tip : "Daşınmaz Əmlak";
        ViewBag.Tipler = MuqavileTipleri;

        return View(kreditler);
    }

    // Səviyyə 2 — Daşınmaz Əmlak hazırlama formasını göstərir
    [HttpGet]
    public async Task<IActionResult> Hazirla(string hesabNo, string ks, DateTime? tarix)
    {
        var seciliTarix = tarix ?? DateTime.Today;
        KreditMuqavileSatirDto? kredit = null;
        try
        {
            kredit = await _muqavileService.KrediGetirAsync(hesabNo, ks, seciliTarix);
        }
        catch (Exception ex)
        {
            ViewBag.Xeta = "Oracle-dan məlumat alınmadı: " + ex.Message;
        }

        if (kredit == null)
        {
            ViewBag.Xeta ??= "Kredit tapılmadı.";
            return View("Hazirla", new KreditMuqavileSatirDto { HesabNo = hesabNo, Ks = ks });
        }

        ViewBag.SeciliTarix = seciliTarix;
        return View("Hazirla", kredit);
    }

    // Səviyyə 2 — sənədləri yaradır (kredit + ipoteka + BTİ məktubu + zaminliklər) → .zip
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MenzilYarat(MenzilMuqavileYaratDto dto, CancellationToken ct)
    {
        var kredit = await _muqavileService.KrediGetirAsync(dto.HesabNo, dto.Ks, dto.KreditTarixi, ct);
        if (kredit == null)
        {
            TempData["Error"] = "Kredit tapılmadı — nömrələr ayrılmadı, heç nə yazılmadı.";
            return RedirectToAction("Index", new { tarix = dto.KreditTarixi.ToString("yyyy-MM-dd"), tip = "Daşınmaz Əmlak" });
        }

        var zaminler = (dto.Zaminler ?? new()).Where(z => !string.IsNullOrWhiteSpace(z.Ad)).ToList();

        // Nömrələri ayır (NomreYaz=false olduqda preview — Oracle-a yazılmır)
        var nomreler = await _nomreService.MenzilNomreleriAyirAsync(zaminler.Count, ct);
        var mekno = await _nomreService.MektubQeydiyyatiAsync(dto.MuqavileTarixi, User.Identity?.Name ?? "FinNex", ct);

        // ── Ortaq token dəsti (kredit + ipoteka + məktub) ──
        var ferqli = dto.GirovSahibiFerqli;
        var ortak = new Dictionary<string, string?>
        {
            // Kredit / borcalan
            ["{k_mno}"] = nomreler.KreditNo.ToString(),
            ["{k_tar_soz}"] = KreditSozeCevir.TarixiSoze(dto.MuqavileTarixi),
            ["{k_saa}"] = kredit.Adi,
            ["{k_olke}"] = kredit.Olke,
            ["{k_ves}"] = VesiqeMetni(kredit),
            ["{k_mud}"] = kredit.Muddet,
            ["{k_mud_soz}"] = KreditSozeCevir.MuddetSoze(IntParse(kredit.Muddet)),
            ["{k_meb}"] = Pul(kredit.Mebleg),
            ["{k_meb_soz}"] = KreditSozeCevir.MebleghSoze(kredit.Mebleg ?? 0),
            ["{k_val}"] = "AZN",
            ["{k_faiz}"] = Faiz(kredit.Faiz),
            ["{k_cfaiz}"] = Faiz(kredit.VkFaiz),
            ["{k_ay_odenis}"] = Pul(kredit.Ayliq),
            ["{k_ay_odenis_soz}"] = KreditSozeCevir.MebleghSoze(kredit.Ayliq ?? 0),
            ["{k_fifd}"] = kredit.Fifd,
            ["{k_teyinat}"] = kredit.Teyinat,
            ["{k_unvan}"] = kredit.Unvan,
            ["{k_tel}"] = kredit.Mobil,
            ["{k_teminatavto}"] = "",
            ["{k_teminat1}"] = zaminler.Count > 0 ? zaminler[0].Ad : "",
            ["{k_teminat2}"] = zaminler.Count > 1 ? zaminler[1].Ad : "",
            ["{k_teminat3}"] = zaminler.Count > 2 ? zaminler[2].Ad : "",

            // İpoteka / girov
            ["{i_mno}"] = nomreler.IpotekaNo.ToString(),
            ["{i_erazi}"] = dto.Erazi,
            ["{i_cixarisNo}"] = dto.CixarisNo,
            ["{i_tarix}"] = kredit.CixarisTarixi?.ToString("dd-MM-yyyy"),
            ["{i_qeydiyyatNo}"] = dto.QeydiyyatNo,
            ["{i_reyestrNo}"] = dto.ReyestrNo,
            ["{i_ipoteka_unvan}"] = dto.IpotekaUnvan,
            ["{i_ipnovu}"] = dto.IpotekaNovu,
            ["{i_diger_cixaris_melumati}"] = dto.DigerCixarisMelumati,
            ["{Ipoteka_deyer}"] = Pul(dto.GirovDeyeri),
            ["{Ipoteka_deyer_soz}"] = KreditSozeCevir.MebleghSozeQepiksiz(dto.GirovDeyeri ?? 0),

            // Girov sahibi — fərqli olduqda əl ilə, əks halda borcalan
            ["{i_saa}"] = ferqli ? dto.SahibAd : kredit.Adi,
            ["{i_ves}"] = ferqli ? dto.SahibVesiqe : VesiqeMetni(kredit),
            ["{i_unvan}"] = ferqli ? dto.SahibUnvan : kredit.Unvan,
            ["{i_olke}"] = ferqli ? dto.SahibOlke : kredit.Olke,
            ["{i_tel}"] = ferqli ? dto.SahibTel : kredit.Mobil,

            // Məktub
            ["{mekno}"] = mekno,
            ["{girov_tipi}"] = GirovTipiGenitiv(dto.GirovNovu),
        };

        var templateRoot = SablonQovlugu();
        string T(string ad) => Path.Combine(templateRoot, ad);

        var ipotekaSablon = ferqli ? "Dasinmaz_emlak_ipoteka_muqavilesi.docx" : "Dasinmaz_emlak_ipoteka_muqavilesiTek.docx";
        var mektubSablon = ferqli ? "BTI_salinma.docx" : "BTI_salinma_Tek.docx";

        var eksikSablon = new[] { "Kredit_muqavili_yeni.docx", ipotekaSablon, mektubSablon, "Zaminlik_muqavilesi.docx" }
            .FirstOrDefault(f => !System.IO.File.Exists(T(f)));
        if (eksikSablon != null)
        {
            TempData["Error"] = $"Şablon tapılmadı: {eksikSablon} ({templateRoot})";
            return RedirectToAction("Index", new { tarix = dto.KreditTarixi.ToString("yyyy-MM-dd"), tip = "Daşınmaz Əmlak" });
        }

        var ad = (kredit.Adi ?? "muqavile").Trim();
        var senedler = new List<(string ad, byte[] data)>
        {
            ($"Kredit müqaviləsi - {ad}.docx", KreditWordService.Doldur(T("Kredit_muqavili_yeni.docx"), ortak)),
            ($"İpoteka müqaviləsi - {ad}.docx", KreditWordService.Doldur(T(ipotekaSablon), ortak)),
            ($"BTİ məktubu - {ad}.docx", KreditWordService.Doldur(T(mektubSablon), ortak)),
        };

        // Hər zamin üçün ayrıca zaminlik müqaviləsi
        for (var i = 0; i < zaminler.Count; i++)
        {
            var z = zaminler[i];
            var zdict = new Dictionary<string, string?>(ortak)
            {
                ["{zsaa1}"] = z.Ad,
                ["{zves1}"] = string.Join(",", new[] { z.Pasport, z.Fin }.Where(s => !string.IsNullOrWhiteSpace(s))),
                ["{ztel}"] = z.Telefon,
                ["{zunvan}"] = z.Unvan,
                ["{zolke1}"] = z.Olke,
                ["{zmno1}"] = (i < nomreler.ZaminNolar.Count ? nomreler.ZaminNolar[i] : 0).ToString(),
                ["{ztar1_soz}"] = KreditSozeCevir.TarixiSoze(dto.MuqavileTarixi),
            };
            senedler.Add(($"Zaminlik - {z.Ad}.docx", KreditWordService.Doldur(T("Zaminlik_muqavilesi.docx"), zdict)));
        }

        var zip = KreditWordService.ZipYarat(senedler);
        var zipAd = $"Muqavile_{ad}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        return File(zip, "application/zip", zipAd);
    }

    // ── Köməkçilər ──
    private string SablonQovlugu()
    {
        var root = _config["KreditMuqavile:TemplateRoot"];
        return string.IsNullOrWhiteSpace(root)
            ? Path.Combine(_env.ContentRootPath, "Files", "Word", "Kredit")
            : root;
    }

    private static string VesiqeMetni(KreditMuqavileSatirDto k)
    {
        // {k_ves} — DİQQƏT: mətnin dəqiq formatı BMI ilə tutuşdurulmalıdır.
        var hisseler = new[]
        {
            k.SeriyaNo,
            string.IsNullOrWhiteSpace(k.VerenOrqan) ? null : $"{k.VerenOrqan} tərəfindən verilmişdir",
            k.SenedVerilmeTarixi?.ToString("dd.MM.yyyy")
        };
        return string.Join(", ", hisseler.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    // {girov_tipi} — girov növü → yiyəlik hal (BTİ məktubu üçün), BMI Menzil.girovTipiniTap()
    private static string GirovTipiGenitiv(string? girovNovu) => girovNovu switch
    {
        "Mənzil" => "mənzilin",
        "Qeyri yaşayış sahəsi" => "Qeyri yaşayış sahəsinin",
        "Qeyri yaşayış binası" => "Qeyri yaşayış binasının",
        "Fərdi yaşayış evi" => "Fərdi yaşayış evinin",
        "Torpaq sahəsi" => "Torpaq sahəsinin",
        _ => girovNovu ?? ""
    };

    private static string Pul(decimal? v) => (v ?? 0).ToString("#,##0.##");
    private static string Faiz(decimal? v) => (v ?? 0).ToString("0.##");
    private static int IntParse(string? s) => int.TryParse(new string((s ?? "").Where(char.IsDigit).ToArray()), out var r) ? r : 0;
}
