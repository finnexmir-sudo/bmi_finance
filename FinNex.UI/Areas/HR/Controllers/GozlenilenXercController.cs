using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
public class GozlenilenXercController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public GozlenilenXercController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    // ── GET /HR/GozlenilenXerc ───────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Gözlənilən Xərclər";

        var kateqoriyalar = await _unitOfWork.Repository<XercKateqoriyasi>()
            .Query().Where(x => !x.Silinib && x.Aktivdir)
            .OrderBy(x => x.Ad).ToListAsync();

        var departamentlar = await _unitOfWork.Repository<Departament>()
            .Query().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();

        ViewBag.Kateqoriyalar  = kateqoriyalar;
        ViewBag.Departamentlar = departamentlar;

        return View();
    }

    // ── GET /HR/GozlenilenXerc/GetData ───────────────────────
    [HttpGet]
    public async Task<IActionResult> GetData(int? status, int? deptId)
    {
        var q = _unitOfWork.Repository<GozlenilenXerc>()
            .Query().Where(x => !x.Silinib);

        if (status.HasValue)
        {
            var s = (GozlenilenXercStatus)status.Value;
            q = q.Where(x => x.Status == s);
        }

        if (deptId.HasValue)
            q = q.Where(x => x.DepartamentId == deptId);

        var list = await q
            .Include(x => x.Kateqoriya)
            .Include(x => x.Departament)
            .OrderBy(x => x.GozlenilenTarix).ToListAsync();
        var bugun = DateTime.Today;

        return Json(list.Select(x => new
        {
            x.Id,
            x.Ad,
            x.Tesvir,
            kateqoriya     = x.Kateqoriya.Ad,
            kateqoriyaIkon = x.Kateqoriya.Ikon ?? "bi-tag",
            departament    = x.Departament?.Ad ?? "—",
            x.TahminiMebleg,
            gozlenilenTarix = x.GozlenilenTarix.ToString("dd.MM.yyyy"),
            prioritet       = PrioritetAdi(x.Prioritet),
            prioritetId     = (int)x.Prioritet,
            status          = StatusAdi(x.Status),
            statusId        = (int)x.Status,
            x.XercId,
            x.Qeyd,
            x.KateqoriyaId,
            x.DepartamentId,
            kecmis  = x.Status == GozlenilenXercStatus.Aktiv && x.GozlenilenTarix.Date < bugun,
            yaxinda = x.Status == GozlenilenXercStatus.Aktiv && x.GozlenilenTarix.Date >= bugun
                                                              && x.GozlenilenTarix.Date <= bugun.AddDays(7)
        }));
    }

    // ── POST /HR/GozlenilenXerc/Yarat ────────────────────────
    [HttpPost]
    public async Task<IActionResult> Yarat([FromBody] GozlenilenXercDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Ad))
            return Json(new { ok = false, xeta = "Ad boş ola bilməz." });
        if (dto.TahminiMebleg <= 0)
            return Json(new { ok = false, xeta = "Məbləğ sıfırdan böyük olmalıdır." });

        var gx = new GozlenilenXerc
        {
            Ad              = dto.Ad.Trim(),
            Tesvir          = dto.Tesvir?.Trim(),
            KateqoriyaId    = dto.KateqoriyaId,
            DepartamentId   = dto.DepartamentId == 0 ? null : dto.DepartamentId,
            TahminiMebleg   = dto.TahminiMebleg,
            GozlenilenTarix = dto.GozlenilenTarix,
            Prioritet       = (GozlenilenXercPrioritet)dto.PrioritetId,
            Status          = GozlenilenXercStatus.Aktiv,
            Qeyd            = dto.Qeyd?.Trim()
        };

        await _unitOfWork.Repository<GozlenilenXerc>().YaratAsync(gx);
        await _unitOfWork.YaddaSaxlaAsync();
        return Json(new { ok = true, id = gx.Id });
    }

    // ── POST /HR/GozlenilenXerc/Yenile ───────────────────────
    [HttpPost]
    public async Task<IActionResult> Yenile([FromBody] GozlenilenXercDto dto)
    {
        var gx = await _unitOfWork.Repository<GozlenilenXerc>()
            .Query().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib);

        if (gx == null) return Json(new { ok = false, xeta = "Tapılmadı." });
        if (gx.Status != GozlenilenXercStatus.Aktiv)
            return Json(new { ok = false, xeta = "Yalnız aktiv qeydlər redaktə edilə bilər." });

        gx.Ad              = dto.Ad.Trim();
        gx.Tesvir          = dto.Tesvir?.Trim();
        gx.KateqoriyaId    = dto.KateqoriyaId;
        gx.DepartamentId   = dto.DepartamentId == 0 ? null : dto.DepartamentId;
        gx.TahminiMebleg   = dto.TahminiMebleg;
        gx.GozlenilenTarix = dto.GozlenilenTarix;
        gx.Prioritet       = (GozlenilenXercPrioritet)dto.PrioritetId;
        gx.Qeyd            = dto.Qeyd?.Trim();

        await _unitOfWork.YaddaSaxlaAsync();
        return Json(new { ok = true });
    }

    // ── POST /HR/GozlenilenXerc/Reallashdir ──────────────────
    // Gözlənilən xərci faktiki Xerc-ə çevir
    [HttpPost]
    public async Task<IActionResult> Reallashdir([FromBody] ReallashtirDto dto)
    {
        var gx = await _unitOfWork.Repository<GozlenilenXerc>()
            .Query().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib
                                           && x.Status == GozlenilenXercStatus.Aktiv);

        if (gx == null) return Json(new { ok = false, xeta = "Tapılmadı." });

        // Faktiki xərc yarat
        var xerc = new Xerc
        {
            DepartamentId = gx.DepartamentId,
            KateqoriyaId  = gx.KateqoriyaId,
            ManualGiris   = true,
            Tesvir        = $"[Gözlənilən] {gx.Ad}" + (string.IsNullOrEmpty(gx.Tesvir) ? "" : $" — {gx.Tesvir}"),
            Mebleg        = dto.FaktikiMebleg > 0 ? dto.FaktikiMebleg : gx.TahminiMebleg,
            XercTarixi    = dto.OdenisTarixi != default ? dto.OdenisTarixi : DateTime.Now,
            Status        = XercStatus.Odenildi
        };
        await _unitOfWork.Repository<Xerc>().YaratAsync(xerc);
        await _unitOfWork.YaddaSaxlaAsync();

        gx.Status = GozlenilenXercStatus.Reallashdi;
        gx.XercId = xerc.Id;
        await _unitOfWork.YaddaSaxlaAsync();

        return Json(new { ok = true, xercId = xerc.Id });
    }

    // ── POST /HR/GozlenilenXerc/LegvEt ───────────────────────
    [HttpPost]
    public async Task<IActionResult> LegvEt([FromBody] SilDto dto)
    {
        var gx = await _unitOfWork.Repository<GozlenilenXerc>()
            .Query().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib
                                           && x.Status == GozlenilenXercStatus.Aktiv);

        if (gx == null) return Json(new { ok = false });
        gx.Status = GozlenilenXercStatus.LegvEdildi;
        await _unitOfWork.YaddaSaxlaAsync();
        return Json(new { ok = true });
    }

    // ── POST /HR/GozlenilenXerc/Sil ──────────────────────────
    [HttpPost]
    public async Task<IActionResult> Sil([FromBody] SilDto dto)
    {
        var gx = await _unitOfWork.Repository<GozlenilenXerc>()
            .Query().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib);

        if (gx == null) return Json(new { ok = false });
        gx.Silinib       = true;
        gx.SilinmeTarixi = DateTime.Now;
        await _unitOfWork.YaddaSaxlaAsync();
        return Json(new { ok = true });
    }

    // ── Köməkçi ──────────────────────────────────────────────
    private static string PrioritetAdi(GozlenilenXercPrioritet p) => p switch
    {
        GozlenilenXercPrioritet.Yuksek => "Yüksək",
        GozlenilenXercPrioritet.Orta   => "Orta",
        GozlenilenXercPrioritet.Asagi  => "Aşağı",
        _                              => "Orta"
    };

    private static string StatusAdi(GozlenilenXercStatus s) => s switch
    {
        GozlenilenXercStatus.Aktiv      => "Aktiv",
        GozlenilenXercStatus.Reallashdi => "Reallaşdı",
        GozlenilenXercStatus.LegvEdildi => "Ləğv edildi",
        _                               => "Aktiv"
    };

    // ── DTOs ─────────────────────────────────────────────────
    public class GozlenilenXercDto
    {
        public int      Id              { get; set; }
        public string   Ad              { get; set; } = "";
        public string?  Tesvir          { get; set; }
        public int      KateqoriyaId    { get; set; }
        public int?     DepartamentId   { get; set; }
        public decimal  TahminiMebleg   { get; set; }
        public DateTime GozlenilenTarix { get; set; }
        public int      PrioritetId     { get; set; } = 2;
        public string?  Qeyd            { get; set; }
    }

    public class ReallashtirDto
    {
        public int      Id            { get; set; }
        public decimal  FaktikiMebleg { get; set; }
        public DateTime OdenisTarixi  { get; set; }
    }

    public class SilDto { public int Id { get; set; } }
}
