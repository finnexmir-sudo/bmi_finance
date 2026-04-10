using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class ChatController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ChatController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── GET /User/Chat ──────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Daxili Chat";
        return View();
    }

    // ── GET /User/Chat/GetContacts ──────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetContacts()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (menim == null)
            return Json(new { contacts = Array.Empty<object>(), menimIsciId = 0 });

        var isciler = await _unitOfWork.Repository<Isci>()
            .Query()
            .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv && x.Id != menim.Id)
            .OrderBy(x => x.Ad)
            .ThenBy(x => x.Soyad)
            .ToListAsync();

        // Oxunmamish mesajlar
        var oxunmamis = await _unitOfWork.Repository<ChatMesaj>()
            .Query()
            .Where(x => x.AlanIsciId == menim.Id && !x.Oxunub)
            .GroupBy(x => x.GonderenIsciId)
            .Select(g => new { isciId = g.Key, say = g.Count() })
            .ToListAsync();

        var contacts = isciler.Select(i => new
        {
            isciId = i.Id,
            ad = i.Ad,
            soyad = i.Soyad,
            tamAd = i.TamAd,
            oxunmamis = oxunmamis.FirstOrDefault(o => o.isciId == i.Id)?.say ?? 0
        });

        return Json(new { contacts, menimIsciId = menim.Id });
    }

    // ── GET /User/Chat/GetMessages?isciId=5 ─────────────────
    [HttpGet]
    public async Task<IActionResult> GetMessages(int isciId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (menim == null)
            return Json(new { mesajlar = Array.Empty<object>() });

        var mesajlar = await _unitOfWork.Repository<ChatMesaj>()
            .Query()
            .Where(x =>
                (x.GonderenIsciId == menim.Id && x.AlanIsciId == isciId) ||
                (x.GonderenIsciId == isciId && x.AlanIsciId == menim.Id))
            .OrderBy(x => x.GonderilmeTarixi)
            .ToListAsync();

        // Mark as read
        var oxunmamislar = mesajlar.Where(x => x.AlanIsciId == menim.Id && !x.Oxunub).ToList();
        foreach (var m in oxunmamislar)
        {
            m.Oxunub = true;
            await _unitOfWork.Repository<ChatMesaj>().YenileAsync(m);
        }
        if (oxunmamislar.Any())
            await _unitOfWork.YaddaSaxlaAsync();

        var data = mesajlar.Select(m => new
        {
            id = m.Id,
            gonderenIsciId = m.GonderenIsciId,
            metn = m.Metn,
            tarix = m.GonderilmeTarixi.ToString("dd.MM.yyyy HH:mm"),
            saatStr = m.GonderilmeTarixi.ToString("HH:mm"),
            menimdir = m.GonderenIsciId == menim.Id
        });

        return Json(new { mesajlar = data });
    }

    // ── POST /User/Chat/Send ───────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ChatSendDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Metn) || dto.AlanIsciId <= 0)
            return Json(new { ok = false });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (menim == null) return Json(new { ok = false });

        var mesaj = new ChatMesaj
        {
            GonderenIsciId = menim.Id,
            AlanIsciId = dto.AlanIsciId,
            Metn = dto.Metn.Trim(),
            Oxunub = false,
            GonderilmeTarixi = DateTime.Now
        };

        await _unitOfWork.Repository<ChatMesaj>().YaratAsync(mesaj);
        await _unitOfWork.YaddaSaxlaAsync();

        return Json(new { ok = true, id = mesaj.Id, tarix = mesaj.GonderilmeTarixi.ToString("HH:mm") });
    }

    public class ChatSendDto
    {
        public int AlanIsciId { get; set; }
        public string Metn { get; set; } = "";
    }
}
