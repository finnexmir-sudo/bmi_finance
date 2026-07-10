using System.Globalization;
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

    // Kredit təyinatları — BMI dialoqundakı siyahı ({k_teyinat})
    public static readonly string[] Teyinatlar =
    {
        "Avtomobil almaq", "Məişət əşyalarının alınması", "Mənzil təmiri", "Təhsil haqqı",
        "Xeyir iş", "Müalicə xərci", "İstirahət Xərci", "Dövriyyə vəsaitinin artırılması"
    };

    // Ölkələr — BMI dialoqundakı siyahı ({k_olke}/{i_olke}/{zolke1})
    public static readonly string[] Olkeler =
    {
        "Azərbaycan Respublikası", "İran İslam Respublikası", "Rusiya Respublikası",
        "Türkiyə Respublikası", "Gürcüstan Respublikası"
    };

    // İpoteka obyektinin tipi — köhnə BMI "Hüquq obyektinin adı" (seçiliyə uyğun sahələr).
    // Kod → görünən ad. Kod formadan gəlir, sənəddə ad işlədilir.
    public static readonly (string Kod, string Ad)[] ObyektTipleri =
    {
        ("menzil", "Mənzil"),
        ("ferdi",  "Fərdi yaşayış evi (həyət evi)"),
        ("torpaq", "Torpaq sahəsi"),
        ("qeyri",  "Qeyri-yaşayış obyekti"),
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

        // Zaminləri Oracle-dan avtomatik yüklə (neçə zamin varsa)
        var zaminler = new List<ZaminDaxilDto>();
        try { zaminler = await _muqavileService.ZaminleriGetirAsync(hesabNo, ks); }
        catch { /* zamin sorğusu uğursuz olarsa forma zaminsiz açılır */ }

        ViewBag.SeciliTarix = seciliTarix;
        ViewBag.Zaminler = zaminler;
        ViewBag.Teyinatlar = Teyinatlar;
        ViewBag.Olkeler = Olkeler;
        ViewBag.ObyektTipleri = ObyektTipleri;
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

        // 1.3 bənd — təminatlar (BMI Menzil.cs məntiqi)
        var tarixSoz = KreditSozeCevir.TarixiSoze(dto.MuqavileTarixi);
        var menzilSahibiAdi = ferqli ? (dto.SahibAd ?? "") : (kredit.Adi ?? "");
        var ipotekaTeminat = $"{tarixSoz} il tarixli {nomreler.IpotekaNo} nömrəli {menzilSahibiAdi} ilə bağlanmış DAŞINMAZ ƏMLAK {dto.IpotekaNovu} MÜQAVİLƏSİ";
        string ZaminTeminat(int idx)
        {
            if (idx >= zaminler.Count) return "";
            var no = idx < nomreler.ZaminNolar.Count ? nomreler.ZaminNolar[idx] : 0;
            return $",{tarixSoz} il tarixli {no} nömrəli {zaminler[idx].Ad} tərəfindən verilmiş zaminlik müqaviləsi";
        }

        var ortak = new Dictionary<string, string?>
        {
            // Kredit / borcalan
            ["{k_mno}"] = nomreler.KreditNo.ToString(),
            ["{k_tar_soz}"] = KreditSozeCevir.TarixiSoze(dto.MuqavileTarixi),
            ["{k_saa}"] = KreditSozeCevir.BaslikRegistri(kredit.Adi),
            ["{k_kimlik}"] = BorcaluKimlik(kredit, dto.BorcalanOlke, dto.DirektorAd, dto.DirektorVesiqe, dto.DirektorOlke),
            ["{k_olke}"] = dto.BorcalanOlke,
            ["{k_ves}"] = VesiqeVeyaVoen(kredit),
            ["{k_mud}"] = AyMuddet(kredit.Muddet).ToString(),
            ["{k_mud_soz}"] = KreditSozeCevir.MuddetSoze(AyMuddet(kredit.Muddet)),
            ["{k_meb}"] = Pul(kredit.Mebleg),
            ["{k_meb_soz}"] = KreditSozeCevir.MebleghSozeQepiksiz(kredit.Mebleg ?? 0),
            ["{k_val}"] = "AZN",
            ["{k_faiz}"] = Faiz(kredit.Faiz),
            ["{k_cfaiz}"] = Faiz(kredit.VkFaiz),
            ["{k_ay_odenis}"] = Pul(kredit.Ayliq),
            ["{k_ay_odenis_soz}"] = KreditSozeCevir.MebleghSoze(kredit.Ayliq ?? 0),
            ["{k_fifd}"] = Reqem(kredit.Fifd),
            ["{k_teyinat}"] = string.IsNullOrWhiteSpace(dto.Teyinat) ? kredit.Teyinat : dto.Teyinat,
            ["{k_unvan}"] = kredit.Unvan,
            ["{k_tel}"] = kredit.Mobil,
            ["{k_teminatavto}"] = ipotekaTeminat,
            ["{k_teminat1}"] = ZaminTeminat(0),
            ["{k_teminat2}"] = ZaminTeminat(1),
            ["{k_teminat3}"] = ZaminTeminat(2),

            // İpoteka / girov
            ["{i_mno}"] = nomreler.IpotekaNo.ToString(),
            ["{i_erazi}"] = dto.Erazi,
            ["{i_cixarisNo}"] = dto.CixarisNo,
            ["{i_tarix}"] = kredit.CixarisTarixi?.ToString("dd-MM-yyyy"),
            ["{i_qeydiyyatNo}"] = dto.QeydiyyatNo,
            ["{i_reyestrNo}"] = dto.ReyestrNo,
            ["{i_ipoteka_unvan}"] = dto.IpotekaUnvan,
            ["{i_ipnovu}"] = dto.IpotekaNovu,
            ["{i_diger_cixaris_melumati}"] = CixarisBlok(dto),
            ["{Ipoteka_deyer}"] = Pul(dto.GirovDeyeri),
            ["{Ipoteka_deyer_soz}"] = KreditSozeCevir.MebleghSozeQepiksiz(dto.GirovDeyeri ?? 0),

            // Girov sahibi — fərqli olduqda əl ilə, əks halda borcalan
            ["{i_saa}"] = ferqli ? dto.SahibAd : KreditSozeCevir.BaslikRegistri(kredit.Adi),
            ["{i_ves}"] = ferqli ? dto.SahibVesiqe : VesiqeVeyaVoen(kredit),
            ["{i_unvan}"] = ferqli ? dto.SahibUnvan : kredit.Unvan,
            ["{i_olke}"] = ferqli ? dto.SahibOlke : dto.BorcalanOlke,
            ["{i_tel}"] = ferqli ? dto.SahibTel : kredit.Mobil,

            // Məktub
            ["{mekno}"] = mekno,
            ["{girov_tipi}"] = GirovTipiGenitiv(dto.ObyektTipi),
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
                // Hüquqi zamin: {zolke1} prefiksinə "hüquqi şəxs {ad} (VÖEN: X), direktoru {ölkə}"
                // qoyulur, {zsaa1}=direktor, {zves1}=direktor vəsiqəsi → şablon "{zolke1} vətəndaşı
                // {zsaa1} (Vəsiqə məlumatı: {zves1})" tam hüquqi bəndi verir. Fiziki: adi zamin.
                ["{zsaa1}"] = z.Huquqi ? KreditSozeCevir.BaslikRegistri(z.DirektorAd) : z.Ad,
                ["{zves1}"] = z.Huquqi
                    ? (z.DirektorVesiqe ?? "")
                    : string.Join(",", new[] { z.Pasport, z.Fin }.Where(s => !string.IsNullOrWhiteSpace(s))),
                ["{ztel}"] = z.Telefon,
                ["{zunvan}"] = z.Unvan,
                ["{zolke1}"] = z.Huquqi
                    ? $"hüquqi şəxs {KreditSozeCevir.BaslikRegistri(z.Ad)} (VÖEN: {z.Voen}), direktoru {z.DirektorOlke}"
                    : z.Olke,
                ["{zmno1}"] = (i < nomreler.ZaminNolar.Count ? nomreler.ZaminNolar[i] : 0).ToString(),
                ["{ztar1_soz}"] = KreditSozeCevir.TarixiSoze(dto.MuqavileTarixi),
            };
            senedler.Add(($"Zaminlik - {z.Ad}.docx", KreditWordService.Doldur(T("Zaminlik_muqavilesi.docx"), zdict)));
        }

        var zip = KreditWordService.ZipYarat(senedler);
        var zipAd = $"Muqavile_{ad}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        return File(zip, "application/zip", zipAd);
    }

    // Səviyyə 2 — Zaminlik hazırlama formasını göstərir (ipoteka yoxdur, yalnız zaminlər)
    [HttpGet]
    public async Task<IActionResult> ZaminlikHazirla(string hesabNo, string ks, DateTime? tarix)
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
            return View("ZaminlikHazirla", new KreditMuqavileSatirDto { HesabNo = hesabNo, Ks = ks });
        }

        // Zaminləri Oracle SELECT-dən avtomatik yüklə (neçə zamin varsa o qədər)
        var zaminler = new List<ZaminDaxilDto>();
        try { zaminler = await _muqavileService.ZaminleriGetirAsync(hesabNo, ks); }
        catch { /* zamin sorğusu uğursuz olarsa forma zaminsiz açılır */ }

        ViewBag.SeciliTarix = seciliTarix;
        ViewBag.Zaminler = zaminler;
        ViewBag.Teyinatlar = Teyinatlar;
        ViewBag.Olkeler = Olkeler;
        return View("ZaminlikHazirla", kredit);
    }

    // Səviyyə 2 — Zaminlik sənədlərini yaradır (kredit müqaviləsi + hər zaminə zaminlik müqaviləsi) → .zip
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ZaminlikYarat(ZaminlikMuqavileYaratDto dto, CancellationToken ct)
    {
        var kredit = await _muqavileService.KrediGetirAsync(dto.HesabNo, dto.Ks, dto.KreditTarixi, ct);
        if (kredit == null)
        {
            TempData["Error"] = "Kredit tapılmadı — nömrələr ayrılmadı, heç nə yazılmadı.";
            return RedirectToAction("Index", new { tarix = dto.KreditTarixi.ToString("yyyy-MM-dd"), tip = "Zaminlik" });
        }

        var zaminler = (dto.Zaminler ?? new()).Where(z => !string.IsNullOrWhiteSpace(z.Ad)).ToList();
        if (zaminler.Count == 0)
        {
            TempData["Error"] = "Ən azı bir zamin daxil edilməlidir — zaminlik müqaviləsi zaminsiz yaradıla bilməz.";
            return RedirectToAction("ZaminlikHazirla", new { hesabNo = dto.HesabNo, ks = dto.Ks, tarix = dto.KreditTarixi.ToString("yyyy-MM-dd") });
        }

        // Nömrələri ayır (NomreYaz=false olduqda preview — Oracle-a yazılmır).
        // KreditNo = kr_zaminlik sayğacı, ZaminNolar = kr_zaminler sayğacı (ipoteka toxunulmur).
        var nomreler = await _nomreService.ZaminlikNomreleriAyirAsync(zaminler.Count, ct);

        var tarixSoz = KreditSozeCevir.TarixiSoze(dto.MuqavileTarixi);

        // Kredit müqaviləsinin təminat bəndi — zaminlər (BMI Menzil məntiqi ilə eyni, ipoteka yoxdur)
        string ZaminTeminat(int idx)
        {
            if (idx >= zaminler.Count) return "";
            var no = idx < nomreler.ZaminNolar.Count ? nomreler.ZaminNolar[idx] : 0;
            return $"{(idx == 0 ? "" : ",")}{tarixSoz} il tarixli {no} nömrəli {zaminler[idx].Ad} tərəfindən verilmiş zaminlik müqaviləsi";
        }

        var ortak = new Dictionary<string, string?>
        {
            ["{k_mno}"] = nomreler.KreditNo.ToString(),
            ["{k_tar_soz}"] = tarixSoz,
            ["{k_saa}"] = KreditSozeCevir.BaslikRegistri(kredit.Adi),
            ["{k_kimlik}"] = BorcaluKimlik(kredit, dto.BorcalanOlke, dto.DirektorAd, dto.DirektorVesiqe, dto.DirektorOlke),
            ["{k_olke}"] = dto.BorcalanOlke,
            ["{k_ves}"] = VesiqeVeyaVoen(kredit),
            ["{k_mud}"] = AyMuddet(kredit.Muddet).ToString(),
            ["{k_mud_soz}"] = KreditSozeCevir.MuddetSoze(AyMuddet(kredit.Muddet)),
            ["{k_meb}"] = Pul(kredit.Mebleg),
            ["{k_meb_soz}"] = KreditSozeCevir.MebleghSozeQepiksiz(kredit.Mebleg ?? 0),
            ["{k_val}"] = "AZN",
            ["{k_faiz}"] = Faiz(kredit.Faiz),
            ["{k_cfaiz}"] = Faiz(kredit.VkFaiz),
            ["{k_ay_odenis}"] = Pul(kredit.Ayliq),
            ["{k_ay_odenis_soz}"] = KreditSozeCevir.MebleghSoze(kredit.Ayliq ?? 0),
            ["{k_fifd}"] = Reqem(kredit.Fifd),
            ["{k_teyinat}"] = string.IsNullOrWhiteSpace(dto.Teyinat) ? kredit.Teyinat : dto.Teyinat,
            ["{k_unvan}"] = kredit.Unvan,
            ["{k_tel}"] = kredit.Mobil,
            // Zaminlik kreditində ipoteka təminatı yoxdur — yalnız zaminlər
            ["{k_teminatavto}"] = "",
            ["{k_teminat1}"] = ZaminTeminat(0),
            ["{k_teminat2}"] = ZaminTeminat(1),
            ["{k_teminat3}"] = ZaminTeminat(2),
        };

        var templateRoot = SablonQovlugu();
        string T(string ad) => Path.Combine(templateRoot, ad);

        var eksikSablon = new[] { "Kredit_muqavili_yeni.docx", "Zaminlik_muqavilesi.docx" }
            .FirstOrDefault(f => !System.IO.File.Exists(T(f)));
        if (eksikSablon != null)
        {
            TempData["Error"] = $"Şablon tapılmadı: {eksikSablon} ({templateRoot})";
            return RedirectToAction("Index", new { tarix = dto.KreditTarixi.ToString("yyyy-MM-dd"), tip = "Zaminlik" });
        }

        var ad = (kredit.Adi ?? "muqavile").Trim();
        var senedler = new List<(string ad, byte[] data)>
        {
            ($"Kredit müqaviləsi - {ad}.docx", KreditWordService.Doldur(T("Kredit_muqavili_yeni.docx"), ortak)),
        };

        // Hər zamin üçün ayrıca zaminlik müqaviləsi (neçə zamin gəlibsə o qədər)
        for (var i = 0; i < zaminler.Count; i++)
        {
            var z = zaminler[i];
            var zdict = new Dictionary<string, string?>(ortak)
            {
                // Hüquqi zamin: {zolke1} prefiksinə "hüquqi şəxs {ad} (VÖEN: X), direktoru {ölkə}"
                // qoyulur, {zsaa1}=direktor, {zves1}=direktor vəsiqəsi → şablon "{zolke1} vətəndaşı
                // {zsaa1} (Vəsiqə məlumatı: {zves1})" tam hüquqi bəndi verir. Fiziki: adi zamin.
                ["{zsaa1}"] = z.Huquqi ? KreditSozeCevir.BaslikRegistri(z.DirektorAd) : z.Ad,
                ["{zves1}"] = z.Huquqi
                    ? (z.DirektorVesiqe ?? "")
                    : string.Join(",", new[] { z.Pasport, z.Fin }.Where(s => !string.IsNullOrWhiteSpace(s))),
                ["{ztel}"] = z.Telefon,
                ["{zunvan}"] = z.Unvan,
                ["{zolke1}"] = z.Huquqi
                    ? $"hüquqi şəxs {KreditSozeCevir.BaslikRegistri(z.Ad)} (VÖEN: {z.Voen}), direktoru {z.DirektorOlke}"
                    : z.Olke,
                ["{zmno1}"] = (i < nomreler.ZaminNolar.Count ? nomreler.ZaminNolar[i] : 0).ToString(),
                ["{ztar1_soz}"] = tarixSoz,
            };
            senedler.Add(($"Zaminlik - {z.Ad}.docx", KreditWordService.Doldur(T("Zaminlik_muqavilesi.docx"), zdict)));
        }

        var zip = KreditWordService.ZipYarat(senedler);
        var zipAd = $"Zaminlik_{ad}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        return File(zip, "application/zip", zipAd);
    }

    // ── Köməkçilər ──
    private string SablonQovlugu()
    {
        // Şablonlar wwwroot/Files/Word/Kredit-də saxlanılır — istifadəçi birbaşa görə/dəyişə bilsin
        // (Bildiriş şablonları ilə eyni yer). Konfiqurasiyada TemplateRoot verilibsə, o üstünlük təşkil edir.
        var root = _config["KreditMuqavile:TemplateRoot"];
        return string.IsNullOrWhiteSpace(root)
            ? Path.Combine(_env.WebRootPath, "Files", "Word", "Kredit")
            : root;
    }

    // İpoteka obyekti tipi kodu → görünən ad (sənəddə işlədilir)
    private static string ObyektTipiAd(string? kod) =>
        ObyektTipleri.FirstOrDefault(t => t.Kod == kod).Ad ?? "";

    // Köhnə BMI "Girova aid məlumatlar" → ipoteka predmeti təsviri ({i_diger_cixaris_melumati}).
    // Yalnız doldurulmuş sahələr daxil edilir. Obyekt tipi başda, sonra "etiket: dəyər" siyahısı,
    // ən sonda əl ilə yazılmış "digər çıxarış məlumatı". Sahə dəyərləri sərbəst mətndir.
    private static string CixarisBlok(MenzilMuqavileYaratDto dto)
    {
        var hisseler = new List<string>();
        void Add(string etiket, string? deyer)
        {
            if (!string.IsNullOrWhiteSpace(deyer)) hisseler.Add($"{etiket}: {deyer.Trim()}");
        }

        Add("ərazi", dto.Erazi);
        Add("ünvan", dto.IpotekaUnvan);
        Add("çıxarış seriya və nömrəsi", dto.SeriyaNomre);
        Add("ümumi sahə", dto.UmumiSahe);
        Add("yaşayış sahəsi", dto.YasayisSahe);
        Add("yardımçı sahə", dto.YardimciSahe);
        Add("torpaq sahəsi", dto.TorpaqSahe);
        Add("otaqların sayı", dto.OtaqSayi);
        Add("reyestr nömrəsi", dto.ReyestrNo);
        Add("reyestr kitab nömrəsi", dto.ReyestrKitabNo);
        Add("reyestr vərəq nömrəsi", dto.ReyestrVereqNo);
        Add("qeydiyyat nömrəsi", dto.QeydiyyatNo);
        if (dto.QeydiyyatTarixi.HasValue)
            hisseler.Add($"qeydiyyat tarixi: {dto.QeydiyyatTarixi.Value:dd.MM.yyyy}");

        var tipAd = ObyektTipiAd(dto.ObyektTipi);
        var netice = tipAd.Length > 0
            ? tipAd + (hisseler.Count > 0 ? " — " + string.Join(", ", hisseler) : "")
            : string.Join(", ", hisseler);

        if (!string.IsNullOrWhiteSpace(dto.DigerCixarisMelumati))
            netice = (netice.Length > 0 ? netice + ". " : "") + dto.DigerCixarisMelumati.Trim();

        return netice;
    }

    private static string VesiqeMetni(KreditMuqavileSatirDto k)
    {
        // {k_ves} — BMI ilə eyni: pasport (seriya/nömrə) + FİN
        return string.Join(", ", new[] { k.SeriyaNo, k.Fin }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    // {k_ves} — hüquqi şəxsdə vəsiqə/FİN yerinə VÖEN, əks halda pasport+FİN
    private static string VesiqeVeyaVoen(KreditMuqavileSatirDto k) =>
        k.HuquqiSexs ? $"VÖEN: {k.Voen}" : VesiqeMetni(k);

    // {k_kimlik} — borcalanın kimlik bəndi (fiziki / hüquqi şəxs).
    // Fiziki:  "{ölkə}nın vətəndaşı {ad} (vəsiqə məlumatı: {vəsiqə})"
    // Hüquqi:  "hüquqi şəxs {şirkət} (VÖEN: {voen}), direktoru {ölkə}nın vətəndaşı {direktor} (vəsiqə məlumatı: {direktor vəsiqəsi})"
    private static string BorcaluKimlik(KreditMuqavileSatirDto k, string? olke, string? direktorAd, string? direktorVesiqe, string? direktorOlke)
    {
        if (k.HuquqiSexs)
        {
            // Hüquqi şəxsdə "vətəndaşı" ölkəsi DİREKTORA aiddir (şirkətə yox)
            var company = KreditSozeCevir.BaslikRegistri(k.Adi);
            var dir = KreditSozeCevir.BaslikRegistri(direktorAd);
            return $"hüquqi şəxs {company} (VÖEN: {k.Voen}), direktoru {direktorOlke}nın vətəndaşı {dir} (vəsiqə məlumatı: {direktorVesiqe})";
        }
        var country = string.IsNullOrWhiteSpace(olke) ? (k.Olke ?? "") : olke;
        return $"{country}nın vətəndaşı {KreditSozeCevir.BaslikRegistri(k.Adi)} (vəsiqə məlumatı: {VesiqeMetni(k)})";
    }

    // {girov_tipi} — obyekt tipi (Hüquq obyektinin adı) → yiyəlik hal (BTİ məktubu üçün).
    // Köhnə ayrıca "Girov növü" sahəsi silindi — mənbə eyni obyekt tipidir.
    private static string GirovTipiGenitiv(string? obyektKod) => obyektKod switch
    {
        "menzil" => "mənzilin",
        "ferdi"  => "fərdi yaşayış evinin",
        "torpaq" => "torpaq sahəsinin",
        "qeyri"  => "qeyri-yaşayış obyektinin",
        _ => ObyektTipiAd(obyektKod).ToLower(new System.Globalization.CultureInfo("az-Latn-AZ"))
    };

    // BMI ilə eyni format: min ayırıcı yox, onluq nöqtə (100000, 2406.16)
    private static string Pul(decimal? v) => (v ?? 0).ToString("0.##", CultureInfo.InvariantCulture);
    private static string Faiz(decimal? v) => (v ?? 0).ToString("0.##", CultureInfo.InvariantCulture);
    // Oracle-dan gələn rəqəm mətnini nöqtəli formata gətir (vergül → nöqtə)
    private static string Reqem(string? s) =>
        decimal.TryParse((s ?? "").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var r)
            ? r.ToString("0.##", CultureInfo.InvariantCulture) : (s ?? "");
    private static int IntParse(string? s) => int.TryParse(new string((s ?? "").Where(char.IsDigit).ToArray()), out var r) ? r : 0;
    // Müddət: srok gün-lə gəlir → aya çevir (÷30)
    public static int AyMuddet(string? srokGun) => (int)Math.Round(IntParse(srokGun) / 30.0, MidpointRounding.AwayFromZero);
}
