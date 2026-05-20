using FinNex.Application.Interfaces.AI;
using FinNex.Application.Interfaces.Communication;
using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities;
using FinNex.Domain.Entities.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class HRMeslehetciController : Controller
{
    private readonly IHRMeslehetciService _ai;
    private readonly IAttachmentTextExtractor _extractor;
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] IcazaliFaylTipləri = { ".pdf", ".doc", ".docx" };
    private const long MaxFaylOlcusu = 20 * 1024 * 1024; // 20 MB

    public HRMeslehetciController(
        IHRMeslehetciService ai,
        IAttachmentTextExtractor extractor,
        AppDbContext db,
        UserManager<AppUser> userManager,
        IWebHostEnvironment env)
    {
        _ai = ai;
        _extractor = extractor;
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    // ── GET /User/HRMeslehetci ─────────────────────────────────────────────────
    public async Task<IActionResult> Index(int? sohbetId)
    {
        var userId = int.Parse(_userManager.GetUserId(User)!);

        var sohbetler = await _db.HRSohbetler
            .Where(s => s.AppUserId == userId && !s.Silinib)
            .OrderByDescending(s => s.BaslanmaTarixi)
            .Select(s => new { s.Id, s.BaslanmaTarixi })
            .ToListAsync();

        var qanunFayllar = await _db.HRQanunFayllar
            .Where(f => !f.Silinib)
            .OrderBy(f => f.Kateqoriya).ThenBy(f => f.Ad)
            .Select(f => new { f.Id, f.Ad, f.Kateqoriya })
            .ToListAsync();

        var movcudKateqoriyalar = qanunFayllar
            .Select(f => (string)f.Kateqoriya)
            .Distinct()
            .OrderBy(k => k)
            .ToList();

        ViewBag.Sohbetler          = sohbetler;
        ViewBag.AktivSohbetId      = 0;
        ViewBag.Mesajlar           = new List<HRSohbetMesaj>();
        ViewBag.QanunFayllar       = qanunFayllar;
        ViewBag.MovcudKateqoriyalar = movcudKateqoriyalar;
        ViewBag.IsAdmin            = User.IsInRole(RoleNames.Admin);

        if (sohbetId.HasValue && sohbetId > 0)
        {
            var sohbet = await _db.HRSohbetler
                .Include(s => s.Mesajlar.OrderBy(m => m.Tarix))
                .FirstOrDefaultAsync(s => s.Id == sohbetId && s.AppUserId == userId && !s.Silinib);

            if (sohbet != null)
            {
                ViewBag.AktivSohbetId = sohbet.Id;
                ViewBag.Mesajlar = sohbet.Mesajlar.ToList();
            }
        }

        return View();
    }

    // ── POST /User/HRMeslehetci/Sor ───────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Sor([FromBody] SorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Sual) || dto.Sual.Length > 2000)
            return Json(new { ok = false, xeta = "Sual boş və ya həddən uzun ola bilməz." });

        var userId = int.Parse(_userManager.GetUserId(User)!);

        // ── Söhbəti tap və ya yarat ────────────────────────────────────────────
        HRSohbet sohbet;
        if (dto.SohbetId > 0)
        {
            var existing = await _db.HRSohbetler
                .Include(s => s.Mesajlar.OrderBy(m => m.Tarix))
                .FirstOrDefaultAsync(s => s.Id == dto.SohbetId && s.AppUserId == userId && !s.Silinib);

            if (existing == null)
                return Json(new { ok = false, xeta = "Söhbət tapılmadı." });

            sohbet = existing;
        }
        else
        {
            sohbet = new HRSohbet { AppUserId = userId, BaslanmaTarixi = DateTime.Now };
            _db.HRSohbetler.Add(sohbet);
            await _db.SaveChangesAsync();
        }

        // Cari sualdan əvvəlki tarixçəni al
        var tarixce = sohbet.Mesajlar.ToList();

        // İstifadəçi mesajını DB-yə yaz
        _db.HRSohbetMesajlar.Add(new HRSohbetMesaj
        {
            SohbetId = sohbet.Id,
            Rol = "user",
            Metn = dto.Sual.Trim(),
            Tarix = DateTime.Now
        });
        await _db.SaveChangesAsync();

        // ── Mərhələ 1: Kateqoriya təyini ──────────────────────────────────────
        string? qanunKonteksti = null;
        string? menbeAdi = null;
        string? kateqoriya = null;

        var fayllarVar = await _db.HRQanunFayllar.AnyAsync(f => !f.Silinib);

        if (fayllarVar)
        {
            var kateqoriyaSiyahi = await _db.HRQanunFayllar
                .Where(f => !f.Silinib)
                .Select(f => f.Kateqoriya)
                .Distinct()
                .ToListAsync();

            kateqoriya = await _ai.KateqoriyaTapAsync(dto.Sual.Trim(), kateqoriyaSiyahi);

            // ── Mərhələ 2: Faylları DB-dən çək ───────────────────────────────
            List<HRQanunFayl> fayllar;
            if (kateqoriya == "hamisi")
            {
                fayllar = await _db.HRQanunFayllar
                    .Where(f => !f.Silinib)
                    .OrderBy(f => f.Kateqoriya)
                    .ToListAsync();
            }
            else
            {
                fayllar = await _db.HRQanunFayllar
                    .Where(f => !f.Silinib && f.Kateqoriya == kateqoriya)
                    .ToListAsync();

                // Uyğun fayl yoxdursa hamısını götür
                if (!fayllar.Any())
                {
                    fayllar = await _db.HRQanunFayllar
                        .Where(f => !f.Silinib)
                        .OrderBy(f => f.Kateqoriya)
                        .ToListAsync();
                }
            }

            if (fayllar.Any())
            {
                qanunKonteksti = string.Join("\n\n" + new string('─', 60) + "\n\n",
                    fayllar.Select(f => $"=== {f.Ad.ToUpperInvariant()} ===\n{f.MetnContent}"));
                menbeAdi = string.Join(", ", fayllar.Select(f => f.Ad));
            }
        }

        // ── Claude-dan cavab al ────────────────────────────────────────────────
        string cavab;
        try
        {
            cavab = await _ai.SualSorAsync(dto.Sual.Trim(), tarixce, qanunKonteksti, menbeAdi);
        }
        catch
        {
            cavab = "AI xidməti ilə əlaqə qurularkən xəta baş verdi. Zəhmət olmasa yenidən cəhd edin.";
        }

        // AI cavabını DB-yə yaz
        var aiMesaj = new HRSohbetMesaj
        {
            SohbetId = sohbet.Id,
            Rol = "assistant",
            Metn = cavab,
            Tarix = DateTime.Now
        };
        _db.HRSohbetMesajlar.Add(aiMesaj);
        await _db.SaveChangesAsync();

        return Json(new
        {
            ok = true,
            sohbetId = sohbet.Id,
            cavab,
            kateqoriya,
            menbeAdi,
            tarix = aiMesaj.Tarix.ToString("HH:mm")
        });
    }

    // ── POST /User/HRMeslehetci/YukleQanunFayl (yalnız Admin) ────────────────
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> YukleQanunFayl(IFormFile? fayl, string? ad, string? kateqoriya)
    {
        if (fayl == null || fayl.Length == 0)
            return Json(new { ok = false, xeta = "Fayl seçilməyib." });

        if (string.IsNullOrWhiteSpace(ad))
            return Json(new { ok = false, xeta = "Faylın adı daxil edilməyib." });

        if (string.IsNullOrWhiteSpace(kateqoriya) || kateqoriya.Length > 60)
            return Json(new { ok = false, xeta = "Kateqoriya adı boş və ya həddən uzun ola bilməz." });

        var ext = Path.GetExtension(fayl.FileName).ToLowerInvariant();
        if (!IcazaliFaylTipləri.Contains(ext))
            return Json(new { ok = false, xeta = "Yalnız PDF, DOC, DOCX fayllar qəbul olunur." });

        if (fayl.Length > MaxFaylOlcusu)
            return Json(new { ok = false, xeta = "Fayl ölçüsü 20 MB-dan çox ola bilməz." });

        // Faylı saxla
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "hr-qanun");
        Directory.CreateDirectory(uploadsDir);
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsDir, uniqueName);

        await using (var fs = System.IO.File.Create(fullPath))
            await fayl.CopyToAsync(fs);

        // Mətni çıxar
        string metn;
        try
        {
            metn = _extractor.Extract(fullPath, fayl.ContentType) ?? "";
        }
        catch
        {
            System.IO.File.Delete(fullPath);
            return Json(new { ok = false, xeta = "Fayldan mətn çıxarıla bilmədi." });
        }

        if (string.IsNullOrWhiteSpace(metn))
        {
            System.IO.File.Delete(fullPath);
            return Json(new { ok = false, xeta = "Faylda oxuna bilən mətn tapılmadı. Skan edilmiş PDF-lər dəstəklənmir." });
        }

        // DB-yə yaz
        var qanunFayl = new HRQanunFayl
        {
            Ad = ad.Trim(),
            Kateqoriya = kateqoriya,
            FaylYolu = $"/uploads/hr-qanun/{uniqueName}",
            MetnContent = metn,
            YaradilmaTarixi = DateTime.Now
        };

        _db.HRQanunFayllar.Add(qanunFayl);
        await _db.SaveChangesAsync();

        return Json(new
        {
            ok = true,
            id = qanunFayl.Id,
            ad = qanunFayl.Ad,
            kateqoriya = qanunFayl.Kateqoriya,
            xarakter = metn.Length
        });
    }

    // ── POST /User/HRMeslehetci/FayliSil (yalnız Admin) ──────────────────────
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> FayliSil([FromBody] FayliSilDto dto)
    {
        var qf = await _db.HRQanunFayllar.FirstOrDefaultAsync(f => f.Id == dto.FaylId && !f.Silinib);
        if (qf == null) return Json(new { ok = false });

        qf.Silinib = true;
        qf.SilinmeTarixi = DateTime.Now;
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    // ── POST /User/HRMeslehetci/TarixiSil ────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> TarixiSil([FromBody] TarixiSilDto dto)
    {
        if (dto?.SohbetId <= 0) return Json(new { ok = false });

        var userId = int.Parse(_userManager.GetUserId(User)!);
        var sohbet = await _db.HRSohbetler
            .FirstOrDefaultAsync(s => s.Id == dto.SohbetId && s.AppUserId == userId);

        if (sohbet == null) return Json(new { ok = false });

        sohbet.Silinib = true;
        sohbet.SilinmeTarixi = DateTime.Now;
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    // ── GET /User/HRMeslehetci/Qaydalar ──────────────────────────────────────
    public async Task<IActionResult> Qaydalar(string? kateqoriya, string? axtaris)
    {
        var query = _db.HRDaxiliQaydalar
            .Include(x => x.YazilanKim)
            .Where(x => !x.Silinib);

        if (!string.IsNullOrEmpty(kateqoriya))
            query = query.Where(x => x.Kateqoriya == kateqoriya);
        if (!string.IsNullOrEmpty(axtaris))
            query = query.Where(x => x.Ad.Contains(axtaris) || x.Mezmun.Contains(axtaris));

        var qaydalar = await query
            .OrderBy(x => x.Kod).ThenByDescending(x => x.Versiya)
            .ToListAsync();

        var kateqoriyalar = await _db.HRDaxiliQaydalar
            .Where(x => !x.Silinib)
            .Select(x => x.Kateqoriya).Distinct().OrderBy(x => x).ToListAsync();

        ViewBag.Kateqoriyalar  = kateqoriyalar;
        ViewBag.AktivKateqoriya = kateqoriya;
        ViewBag.Axtaris        = axtaris;
        ViewBag.IsAdmin        = User.IsInRole(RoleNames.Admin);
        ViewData["Title"]      = "Daxili Qaydalar";
        return View(qaydalar);
    }

    // ── GET /User/HRMeslehetci/QaydaDetal/{id} ────────────────────────────────
    public async Task<IActionResult> QaydaDetal(int id)
    {
        var qayda = await _db.HRDaxiliQaydalar
            .Include(x => x.YazilanKim)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
        if (qayda == null) return RedirectToAction(nameof(Qaydalar));

        var versiyalar = await _db.HRDaxiliQaydalar
            .Include(x => x.YazilanKim)
            .Where(x => x.Kod == qayda.Kod && !x.Silinib)
            .OrderByDescending(x => x.Versiya)
            .ToListAsync();

        ViewBag.Versiyalar = versiyalar;
        ViewBag.IsAdmin    = User.IsInRole(RoleNames.Admin);
        ViewData["Title"]  = qayda.Ad;
        return View(qayda);
    }

    // ── POST /User/HRMeslehetci/QaydaYarat (Admin) ───────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> QaydaYarat(string ad, string mezmun, string kateqoriya, IFormFile? sened)
    {
        if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(mezmun))
        { TempData["Error"] = "Ad və məzmun doldurulmalıdır."; return RedirectToAction(nameof(Qaydalar)); }

        var userId = int.Parse(_userManager.GetUserId(User)!);
        var sonKod = await _db.HRDaxiliQaydalar
            .Where(x => !x.Silinib)
            .OrderByDescending(x => x.Id)
            .Select(x => x.Kod).FirstOrDefaultAsync();

        int novbeti = 1;
        if (!string.IsNullOrEmpty(sonKod) && sonKod.StartsWith("HR-") &&
            int.TryParse(sonKod[3..], out int n)) novbeti = n + 1;

        var q = new HRDaxiliQayda
        {
            Kod = $"HR-{novbeti:D3}", Ad = ad.Trim(),
            Mezmun = mezmun.Trim(), Kateqoriya = kateqoriya?.Trim() ?? "",
            Versiya = 1, Aktiv = true, YazilanKimId = userId
        };

        if (sened != null && sened.Length > 0)
        {
            var (yol, originalAd) = await _SenedSaxla(sened);
            q.SenedYolu = yol;
            q.SenedAd   = originalAd;
        }

        _db.HRDaxiliQaydalar.Add(q);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Qayda '{q.Kod}' yaradıldı.";
        return RedirectToAction(nameof(QaydaDetal), new { id = q.Id });
    }

    // ── POST /User/HRMeslehetci/QaydaYenile (Admin — yeni versiya) ────────────
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> QaydaYenile(int kohneId, string ad, string mezmun, string kateqoriya, string sebeb, IFormFile? sened)
    {
        var kohne = await _db.HRDaxiliQaydalar.FirstOrDefaultAsync(x => x.Id == kohneId && !x.Silinib);
        if (kohne == null) return RedirectToAction(nameof(Qaydalar));

        kohne.Aktiv = false;
        var userId = int.Parse(_userManager.GetUserId(User)!);
        var yeni = new HRDaxiliQayda
        {
            Kod = kohne.Kod, Ad = ad.Trim(),
            Mezmun = mezmun.Trim(), Kateqoriya = kateqoriya?.Trim() ?? "",
            Versiya = kohne.Versiya + 1, Aktiv = true, YazilanKimId = userId,
            SenedAd = kohne.SenedAd, SenedYolu = kohne.SenedYolu
        };

        if (sened != null && sened.Length > 0)
        {
            var (yol, originalAd) = await _SenedSaxla(sened);
            yeni.SenedYolu = yol;
            yeni.SenedAd   = originalAd;
        }

        _db.HRDaxiliQaydalar.Add(yeni);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Qayda v{yeni.Versiya} olaraq yeniləndi.";
        return RedirectToAction(nameof(QaydaDetal), new { id = yeni.Id });
    }

    private async Task<(string yol, string ad)> _SenedSaxla(IFormFile fayl)
    {
        var ext = Path.GetExtension(fayl.FileName).ToLowerInvariant();
        var dir = Path.Combine(_env.WebRootPath, "uploads", "hr-qaydalar");
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, fileName);
        await using var fs = System.IO.File.Create(fullPath);
        await fayl.CopyToAsync(fs);
        return ($"/uploads/hr-qaydalar/{fileName}", fayl.FileName);
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class SorDto       { public int SohbetId { get; set; } public string Sual { get; set; } = ""; }
    public class TarixiSilDto { public int SohbetId { get; set; } }
    public class FayliSilDto  { public int FaylId   { get; set; } }
}
