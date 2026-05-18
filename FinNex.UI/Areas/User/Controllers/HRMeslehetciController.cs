using FinNex.Application.Interfaces.AI;
using FinNex.DataAccess.Contexts;
using FinNex.Domain;
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
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public HRMeslehetciController(IHRMeslehetciService ai, AppDbContext db, UserManager<AppUser> userManager)
    {
        _ai = ai;
        _db = db;
        _userManager = userManager;
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

        ViewBag.Sohbetler = sohbetler;
        ViewBag.AktivSohbetId = 0;
        ViewBag.Mesajlar = new List<HRSohbetMesaj>();

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
            sohbet = new HRSohbet
            {
                AppUserId = userId,
                BaslanmaTarixi = DateTime.Now
            };
            _db.HRSohbetler.Add(sohbet);
            await _db.SaveChangesAsync();
        }

        // Cari sualdan əvvəlki tarixçəni al (xidmət cari sualı özü əlavə edir)
        var tarixce = sohbet.Mesajlar.ToList();

        var istifadeciMesaj = new HRSohbetMesaj
        {
            SohbetId = sohbet.Id,
            Rol = "user",
            Metn = dto.Sual.Trim(),
            Tarix = DateTime.Now
        };
        _db.HRSohbetMesajlar.Add(istifadeciMesaj);
        await _db.SaveChangesAsync();

        string cavab;
        try
        {
            cavab = await _ai.SualSorAsync(dto.Sual.Trim(), tarixce);
        }
        catch
        {
            cavab = "AI xidməti ilə əlaqə qurularkən xəta baş verdi. Zəhmət olmasa yenidən cəhd edin.";
        }

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
            tarix = aiMesaj.Tarix.ToString("HH:mm")
        });
    }

    // ── POST /User/HRMeslehetci/TarixiSil ────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> TarixiSil([FromBody] TarixiSilDto dto)
    {
        if (dto?.SohbetId <= 0)
            return Json(new { ok = false });

        var userId = int.Parse(_userManager.GetUserId(User)!);

        var sohbet = await _db.HRSohbetler
            .FirstOrDefaultAsync(s => s.Id == dto.SohbetId && s.AppUserId == userId);

        if (sohbet == null)
            return Json(new { ok = false });

        sohbet.Silinib = true;
        sohbet.SilinmeTarixi = DateTime.Now;
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class SorDto
    {
        public int SohbetId { get; set; }
        public string Sual { get; set; } = "";
    }

    public class TarixiSilDto
    {
        public int SohbetId { get; set; }
    }
}
