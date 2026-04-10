using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
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
    private readonly IWebHostEnvironment _env;

    private const int MesajLimit = 50;
    private const int KohneMesajGunLimiti = 30;
    private const long MaxFaylOlcusu = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] IcazaliTipler = { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };

    public ChatController(IUnitOfWork unitOfWork, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _env = env;
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

    // ── GET /User/Chat/GetMessages?isciId=5&beforeId=0 ──────
    [HttpGet]
    public async Task<IActionResult> GetMessages(int isciId, int beforeId = 0)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (menim == null)
            return Json(new { mesajlar = Array.Empty<object>(), dahaVar = false });

        var query = _unitOfWork.Repository<ChatMesaj>()
            .Query()
            .Where(x =>
                (x.GonderenIsciId == menim.Id && x.AlanIsciId == isciId) ||
                (x.GonderenIsciId == isciId && x.AlanIsciId == menim.Id));

        if (beforeId > 0)
        {
            query = query.Where(x => x.Id < beforeId);
        }

        var totalCount = await query.CountAsync();

        var mesajlar = await query
            .OrderByDescending(x => x.Id)
            .Take(MesajLimit)
            .OrderBy(x => x.Id)
            .ToListAsync();

        // İlk yükləmədə oxunmamışları oxundu et
        if (beforeId == 0)
        {
            var oxunmamislar = mesajlar.Where(x => x.AlanIsciId == menim.Id && !x.Oxunub).ToList();
            var now = DateTime.Now;
            foreach (var m in oxunmamislar)
            {
                m.Oxunub = true;
                m.OxunmaTarixi = now;
                await _unitOfWork.Repository<ChatMesaj>().YenileAsync(m);
            }
            if (oxunmamislar.Any())
                await _unitOfWork.YaddaSaxlaAsync();
        }

        var dahaVar = beforeId > 0
            ? totalCount > MesajLimit
            : totalCount > mesajlar.Count;

        var data = mesajlar.Select(m => new
        {
            id = m.Id,
            gonderenIsciId = m.GonderenIsciId,
            metn = m.Metn,
            tarix = m.GonderilmeTarixi.ToString("dd.MM.yyyy HH:mm"),
            saatStr = m.GonderilmeTarixi.ToString("HH:mm"),
            menimdir = m.GonderenIsciId == menim.Id,
            oxunub = m.Oxunub,
            faylAdi = m.FaylAdi,
            faylYolu = m.FaylYolu,
            faylTipi = m.FaylTipi,
            faylOlcusu = m.FaylOlcusu
        });

        return Json(new { mesajlar = data, dahaVar });
    }

    // ── POST /User/Chat/Send ───────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ChatSendDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Metn) || dto.AlanIsciId <= 0)
            return Json(new { ok = false });

        if (dto.Metn.Length > 250)
            dto.Metn = dto.Metn[..250];

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

    // ── POST /User/Chat/SendWithFile ───────────────────────
    [HttpPost]
    public async Task<IActionResult> SendWithFile(int alanIsciId, string? metn, IFormFile? fayl)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (menim == null) return Json(new { ok = false, mesaj = "İşçi tapılmadı" });
        if (alanIsciId <= 0) return Json(new { ok = false, mesaj = "Alıcı seçilməyib" });

        string? faylAdi = null, faylYolu = null, faylTipi = null;
        long? faylOlcusu = null;

        if (fayl != null && fayl.Length > 0)
        {
            if (fayl.Length > MaxFaylOlcusu)
                return Json(new { ok = false, mesaj = "Fayl ölçüsü 10MB-dan çox ola bilməz" });

            var ext = Path.GetExtension(fayl.FileName).ToLowerInvariant();
            if (!IcazaliTipler.Contains(ext))
                return Json(new { ok = false, mesaj = "Yalnız PDF, Word, Excel faylları qəbul olunur" });

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chat");
            Directory.CreateDirectory(uploadsDir);

            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsDir, uniqueName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await fayl.CopyToAsync(stream);
            }

            faylAdi = fayl.FileName;
            faylYolu = $"/uploads/chat/{uniqueName}";
            faylTipi = ext.TrimStart('.');
            faylOlcusu = fayl.Length;
        }

        if (string.IsNullOrWhiteSpace(metn) && faylAdi == null)
            return Json(new { ok = false, mesaj = "Mesaj və ya fayl lazımdır" });

        var mesaj = new ChatMesaj
        {
            GonderenIsciId = menim.Id,
            AlanIsciId = alanIsciId,
            Metn = metn?.Trim() ?? "",
            Oxunub = false,
            GonderilmeTarixi = DateTime.Now,
            FaylAdi = faylAdi,
            FaylYolu = faylYolu,
            FaylTipi = faylTipi,
            FaylOlcusu = faylOlcusu
        };

        await _unitOfWork.Repository<ChatMesaj>().YaratAsync(mesaj);
        await _unitOfWork.YaddaSaxlaAsync();

        return Json(new
        {
            ok = true,
            id = mesaj.Id,
            tarix = mesaj.GonderilmeTarixi.ToString("HH:mm"),
            faylAdi,
            faylYolu,
            faylTipi,
            faylOlcusu
        });
    }

    // ── GET /User/Chat/GetDepartments ──────────────────────
    [HttpGet]
    public async Task<IActionResult> GetDepartments()
    {
        var departamentler = await _unitOfWork.Repository<Departament>()
            .Query()
            .Where(x => !x.Silinib)
            .OrderBy(x => x.Ad)
            .Select(d => new { id = d.Id, ad = d.Ad })
            .ToListAsync();

        return Json(new { departamentler });
    }

    // ── GET /User/Chat/GetEmployees?departamentId=0 ────────
    [HttpGet]
    public async Task<IActionResult> GetEmployees(int? departamentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        var menimId = menim?.Id ?? 0;

        IQueryable<IsciTeyinat> query = _unitOfWork.Repository<IsciTeyinat>()
            .Query()
            .Where(t => t.Aktivdir && !t.Isci.Silinib && t.Isci.Status == IsciStatus.Aktiv && t.IsciId != menimId);

        if (departamentId.HasValue && departamentId > 0)
        {
            query = query.Where(t => t.DepartamentId == departamentId.Value);
        }

        var isciler = await query
            .Select(t => new { id = t.IsciId, ad = t.Isci.Ad, soyad = t.Isci.Soyad, departament = t.Departament.Ad })
            .Distinct()
            .OrderBy(x => x.ad).ThenBy(x => x.soyad)
            .ToListAsync();

        return Json(new { isciler });
    }

    // ── POST /User/Chat/SendBulk ───────────────────────────
    [HttpPost]
    public async Task<IActionResult> SendBulk([FromBody] ChatBulkSendDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Metn))
            return Json(new { ok = false, mesaj = "Mesaj mətni boş ola bilməz" });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (menim == null) return Json(new { ok = false, mesaj = "İşçi tapılmadı" });

        List<int> hederIsciIdler;

        if (dto.IsciIdler != null && dto.IsciIdler.Any())
        {
            hederIsciIdler = dto.IsciIdler.Where(id => id != menim.Id).Distinct().ToList();
        }
        else
        {
            IQueryable<IsciTeyinat> teyinatQuery = _unitOfWork.Repository<IsciTeyinat>()
                .Query()
                .Where(t => t.Aktivdir && !t.Isci.Silinib && t.Isci.Status == IsciStatus.Aktiv && t.IsciId != menim.Id);

            if (dto.DepartamentId.HasValue && dto.DepartamentId > 0)
            {
                teyinatQuery = teyinatQuery.Where(t => t.DepartamentId == dto.DepartamentId.Value);
            }

            hederIsciIdler = await teyinatQuery
                .Select(t => t.IsciId)
                .Distinct()
                .ToListAsync();
        }

        if (!hederIsciIdler.Any())
            return Json(new { ok = false, mesaj = "Göndəriləcək işçi tapılmadı" });

        var grupId = Guid.NewGuid();
        var now = DateTime.Now;

        foreach (var isciId in hederIsciIdler)
        {
            var mesaj = new ChatMesaj
            {
                GonderenIsciId = menim.Id,
                AlanIsciId = isciId,
                Metn = dto.Metn.Trim(),
                Oxunub = false,
                GonderilmeTarixi = now,
                TopluMesajGrupId = grupId
            };
            await _unitOfWork.Repository<ChatMesaj>().YaratAsync(mesaj);
        }

        await _unitOfWork.YaddaSaxlaAsync();

        return Json(new { ok = true, say = hederIsciIdler.Count, tarix = now.ToString("HH:mm") });
    }

    // ── POST /User/Chat/MarkAsRead ─────────────────────────
    [HttpPost]
    public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadDto dto)
    {
        if (dto?.GonderenIsciId <= 0) return Json(new { ok = false });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (menim == null) return Json(new { ok = false });

        var oxunmamis = await _unitOfWork.Repository<ChatMesaj>()
            .Query()
            .Where(x => x.GonderenIsciId == dto.GonderenIsciId
                      && x.AlanIsciId == menim.Id
                      && !x.Oxunub)
            .ToListAsync();

        var now = DateTime.Now;
        foreach (var m in oxunmamis)
        {
            m.Oxunub = true;
            m.OxunmaTarixi = now;
            await _unitOfWork.Repository<ChatMesaj>().YenileAsync(m);
        }

        if (oxunmamis.Any())
            await _unitOfWork.YaddaSaxlaAsync();

        return Json(new { ok = true, say = oxunmamis.Count });
    }

    // ── GET /User/Chat/GetReadStatus?isciId=5 ──────────────
    [HttpGet]
    public async Task<IActionResult> GetReadStatus(int isciId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (menim == null)
            return Json(new { oxunmuslar = Array.Empty<int>() });

        var oxunmuslar = await _unitOfWork.Repository<ChatMesaj>()
            .Query()
            .Where(x => x.GonderenIsciId == menim.Id && x.AlanIsciId == isciId && x.Oxunub)
            .Select(x => x.Id)
            .ToListAsync();

        return Json(new { oxunmuslar });
    }

    // ── POST /User/Chat/Cleanup ────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Cleanup()
    {
        var limit = DateTime.Now.AddDays(-KohneMesajGunLimiti);

        var kohneMesajlar = await _unitOfWork.Repository<ChatMesaj>()
            .Query()
            .Where(x => x.GonderilmeTarixi < limit)
            .ToListAsync();

        if (!kohneMesajlar.Any())
            return Json(new { ok = true, say = 0 });

        // Faylları sil
        foreach (var m in kohneMesajlar)
        {
            if (!string.IsNullOrEmpty(m.FaylYolu))
            {
                var fullPath = Path.Combine(_env.WebRootPath, m.FaylYolu.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
        }

        // DB-dən sil (hard delete)
        foreach (var m in kohneMesajlar)
        {
            await _unitOfWork.Repository<ChatMesaj>().DeleteAsync(m.Id);
        }
        await _unitOfWork.YaddaSaxlaAsync();

        return Json(new { ok = true, say = kohneMesajlar.Count });
    }

    // ── POST /User/Chat/DeleteMessage ─────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteMessage([FromBody] DeleteMessageDto dto)
    {
        if (dto?.MesajId <= 0) return Json(new { ok = false });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var menim = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (menim == null) return Json(new { ok = false });

        var mesaj = await _unitOfWork.Repository<ChatMesaj>()
            .GetirAsync(x => x.Id == dto.MesajId && x.GonderenIsciId == menim.Id);

        if (mesaj == null)
            return Json(new { ok = false, mesaj = "Mesaj tapılmadı və ya sizin deyil" });

        // Fayl varsa sil
        if (!string.IsNullOrEmpty(mesaj.FaylYolu))
        {
            var fullPath = Path.Combine(_env.WebRootPath, mesaj.FaylYolu.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        await _unitOfWork.Repository<ChatMesaj>().DeleteAsync(mesaj.Id);
        await _unitOfWork.YaddaSaxlaAsync();

        return Json(new { ok = true });
    }

    // ── DTOs ────────────────────────────────────────────────
    public class ChatSendDto
    {
        public int AlanIsciId { get; set; }
        public string Metn { get; set; } = "";
    }

    public class ChatBulkSendDto
    {
        public int? DepartamentId { get; set; }
        public List<int>? IsciIdler { get; set; }
        public string Metn { get; set; } = "";
    }

    public class MarkAsReadDto
    {
        public int GonderenIsciId { get; set; }
    }

    public class DeleteMessageDto
    {
        public int MesajId { get; set; }
    }
}
