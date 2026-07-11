using System.Security.Claims;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.SenedDovriyyesi.Controllers;

[Area("SenedDovriyyesi")]
[Authorize]
public class XaricMektubController : Controller
{
    private readonly IXaricMektubService _service;
    private readonly IConfiguration _config;

    public XaricMektubController(IXaricMektubService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin() => User.IsInRole(RoleNames.Admin);

    // İstəyə bağlı qoşmanı DMS-ə yazır, nisbi yolu qaytarır (fayl yoxdursa null)
    private async Task<string?> QosmaYazAsync(IFormFile? fayl)
    {
        if (fayl == null || fayl.Length == 0) return null;
        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var dir = Path.Combine(dmsRoot, "mektublar");
        Directory.CreateDirectory(dir);
        var ad = $"{Guid.NewGuid()}{Path.GetExtension(fayl.FileName)}";
        await using var fs = new FileStream(Path.Combine(dir, ad), FileMode.Create);
        await fayl.CopyToAsync(fs);
        return $"mektublar/{ad}";
    }

    // Köhnə BMI "Göndərilən yer" hazır siyahısı — combobox təklifi (istifadəçi əl ilə də yaza bilər)
    private static readonly string[] GonYerSiyahi =
    {
        "Azsığorta ASC",
        "Azərsell Telecom MMC",
        "Azərfon",
        "Bakcell",
        "Bakı Apellyasiya Məhkəməsi",
        "Baş Dövlət Yol Polis İdarəsinə",
        "BMİ Dubay",
        "BMİ Tehran",
        "BTİ",
        "Vergi",
        "DYP",
        "DSMF",
        "Dövlət Gömrük Komitəsi",
        "DƏDRX",
        "Yasamal rayon Məhkəməsi",
        "Mega sığorta",
        "Megaservice MMC",
        "Millikart MMC",
        "Mərkəzi Bank",
        "Nəzarət Palatası",
        "Sabunçu rayon Məhkəməsi",
        "Suraxanı rayon Məhkəməsi",
        "Səbail rayon Məhkəməsi",
        "Xalq Sığorta",
        "Xətayi rayon Məhkəməsi",
        "Hərbi Prokurorluq"
    };

    // GET: /SenedDovriyyesi/XaricMektub
    public async Task<IActionResult> Index(int? il)
    {
        var model = await _service.HamisiniGetirAsync(il);
        ViewBag.Il = il;
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
        return View(model);
    }

    // GET: /SenedDovriyyesi/XaricMektub/Yarat
    [HttpGet]
    public IActionResult Yarat()
    {
        ViewBag.GonYerler = GonYerSiyahi;
        return View(new XaricMektubCreateDto { Tarix = DateTime.Today });
    }

    // POST: /SenedDovriyyesi/XaricMektub/Yarat
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Yarat(XaricMektubCreateDto dto, IFormFile? fayl)
    {
        if (string.IsNullOrWhiteSpace(dto.GonYer))
        {
            TempData["Error"] = "Göndərilən yer (təyinat) boş ola bilməz.";
            ViewBag.GonYerler = GonYerSiyahi;
            return View(dto);
        }

        // İstəyə bağlı qoşma — DMS-ə (C:\FinNex_DMS\mektublar\), DB-yə yalnız nisbi yol
        var faylYolu = await QosmaYazAsync(fayl);

        var res = await _service.YaratAsync(dto, GetUserId(), faylYolu);
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    // GET: /SenedDovriyyesi/XaricMektub/Redakte/5
    [HttpGet]
    public async Task<IActionResult> Redakte(int id)
    {
        var dto = await _service.RedakteMelumatiAsync(id);
        if (dto == null)
        {
            TempData["Error"] = "Məktub tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        if (!IsAdmin() && dto.YaradanId != GetUserId())
        {
            TempData["Error"] = "Bu məktubu yalnız yaradan və ya Admin dəyişə bilər.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.GonYerler = GonYerSiyahi;
        return View(dto);
    }

    // POST: /SenedDovriyyesi/XaricMektub/Redakte
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Redakte(XaricMektubEditDto dto, IFormFile? fayl)
    {
        var yeniFaylYolu = await QosmaYazAsync(fayl);
        var res = await _service.YenileAsync(dto, GetUserId(), IsAdmin(), yeniFaylYolu);
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
        {
            ViewBag.GonYerler = GonYerSiyahi;
            return View(dto);
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /SenedDovriyyesi/XaricMektub/Sil/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var res = await _service.SilAsync(id, GetUserId(), IsAdmin());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
