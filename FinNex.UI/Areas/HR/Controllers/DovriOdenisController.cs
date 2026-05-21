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
public class DovriOdenisController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public DovriOdenisController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    // ── GET /HR/DovriOdenis ──────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Dövri Ödənişlər";

        var kateqoriyalar = await _unitOfWork.Repository<XercKateqoriyasi>()
            .Query().Where(x => !x.Silinib && x.Aktivdir)
            .OrderBy(x => x.Ad).ToListAsync();

        var departamentlar = await _unitOfWork.Repository<Departament>()
            .Query().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();

        ViewBag.Kateqoriyalar  = kateqoriyalar;
        ViewBag.Departamentlar = departamentlar;

        return View();
    }

    // ── GET /HR/DovriOdenis/GetData ──────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetData(bool? yalnizAktiv)
    {
        var q = _unitOfWork.Repository<DovriOdenis>()
            .Query().Where(x => !x.Silinib);

        if (yalnizAktiv == true)
            q = q.Where(x => x.Aktiv);

        var list = await q
            .Include(x => x.Kateqoriya)
            .Include(x => x.Departament)
            .OrderByDescending(x => x.YaradilmaTarixi).ToListAsync();

        var bugun = DateTime.Today;

        return Json(list.Select(x => new
        {
            x.Id,
            x.Ad,
            kateqoriya  = x.Kateqoriya.Ad,
            kateqoriyaIkon = x.Kateqoriya.Ikon ?? "bi-tag",
            departament = x.Departament.Ad,
            x.Mebleg,
            dovruluq    = DovruluqAdi(x.Dovruluq),
            dovruluqId  = (int)x.Dovruluq,
            baslamaTarixi       = x.BaslamaTarixi.ToString("dd.MM.yyyy"),
            bitmeTarixi         = x.BitmeTarixi?.ToString("dd.MM.yyyy"),
            novbatiOdenisTarixi = x.NovbatiOdenisTarixi.ToString("dd.MM.yyyy"),
            x.Aktiv,
            x.Qeyd,
            x.KateqoriyaId,
            x.DepartamentId,
            gecikib = x.Aktiv && x.NovbatiOdenisTarixi.Date < bugun,
            buGun   = x.Aktiv && x.NovbatiOdenisTarixi.Date == bugun,
            yaxinda = x.Aktiv && x.NovbatiOdenisTarixi.Date > bugun
                               && x.NovbatiOdenisTarixi.Date <= bugun.AddDays(7)
        }));
    }

    // ── POST /HR/DovriOdenis/Yarat ───────────────────────────
    [HttpPost]
    public async Task<IActionResult> Yarat([FromBody] DovriOdenisDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Ad))
            return Json(new { ok = false, xeta = "Ad boş ola bilməz." });
        if (dto.Mebleg <= 0)
            return Json(new { ok = false, xeta = "Məbləğ sıfırdan böyük olmalıdır." });

        var ode = new DovriOdenis
        {
            Ad                  = dto.Ad.Trim(),
            KateqoriyaId        = dto.KateqoriyaId,
            DepartamentId       = dto.DepartamentId,
            Mebleg              = dto.Mebleg,
            Dovruluq            = (DovruluqNovu)dto.DovruluqId,
            BaslamaTarixi       = dto.BaslamaTarixi,
            BitmeTarixi         = dto.BitmeTarixi,
            NovbatiOdenisTarixi = dto.BaslamaTarixi,
            Aktiv               = true,
            Qeyd                = dto.Qeyd?.Trim()
        };

        await _unitOfWork.Repository<DovriOdenis>().YaratAsync(ode);
        await _unitOfWork.YaddaSaxlaAsync();

        return Json(new { ok = true, id = ode.Id });
    }

    // ── POST /HR/DovriOdenis/Yenile ──────────────────────────
    [HttpPost]
    public async Task<IActionResult> Yenile([FromBody] DovriOdenisDto dto)
    {
        var ode = await _unitOfWork.Repository<DovriOdenis>()
            .Query().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib);

        if (ode == null) return Json(new { ok = false, xeta = "Tapılmadı." });
        if (string.IsNullOrWhiteSpace(dto.Ad))
            return Json(new { ok = false, xeta = "Ad boş ola bilməz." });
        if (dto.Mebleg <= 0)
            return Json(new { ok = false, xeta = "Məbləğ sıfırdan böyük olmalıdır." });

        ode.Ad            = dto.Ad.Trim();
        ode.KateqoriyaId  = dto.KateqoriyaId;
        ode.DepartamentId = dto.DepartamentId;
        ode.Mebleg        = dto.Mebleg;
        ode.Dovruluq      = (DovruluqNovu)dto.DovruluqId;
        ode.BitmeTarixi   = dto.BitmeTarixi;
        ode.Aktiv         = dto.Aktiv;
        ode.Qeyd          = dto.Qeyd?.Trim();

        await _unitOfWork.YaddaSaxlaAsync();
        return Json(new { ok = true });
    }

    // ── POST /HR/DovriOdenis/OdeEt ───────────────────────────
    // Dövri ödənişi ödənilmiş kimi qeyd et → Xerc yaradır
    [HttpPost]
    public async Task<IActionResult> OdeEt([FromBody] OdeEtDto dto)
    {
        var ode = await _unitOfWork.Repository<DovriOdenis>()
            .Query().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib && x.Aktiv);

        if (ode == null) return Json(new { ok = false, xeta = "Tapılmadı." });

        // Xərc yarat
        var xerc = new Xerc
        {
            DepartamentId = ode.DepartamentId,
            KateqoriyaId  = ode.KateqoriyaId,
            ManualGiris   = true,
            Tesvir        = $"Dövri: {ode.Ad}",
            Mebleg        = ode.Mebleg,
            XercTarixi    = DateTime.Now,
            Status        = XercStatus.Odenildi
        };
        await _unitOfWork.Repository<Xerc>().YaratAsync(xerc);

        // Növbəti tarixi irəli çək
        ode.NovbatiOdenisTarixi = NovbatiTarixHesabla(ode.NovbatiOdenisTarixi, ode.Dovruluq);

        // Bitmə tarixi keçibsə deaktiv et
        if (ode.BitmeTarixi.HasValue && ode.NovbatiOdenisTarixi > ode.BitmeTarixi.Value)
            ode.Aktiv = false;

        await _unitOfWork.YaddaSaxlaAsync();
        return Json(new { ok = true, xercId = xerc.Id });
    }

    // ── POST /HR/DovriOdenis/StatusDeyis ─────────────────────
    [HttpPost]
    public async Task<IActionResult> StatusDeyis([FromBody] StatusDto dto)
    {
        var ode = await _unitOfWork.Repository<DovriOdenis>()
            .Query().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib);

        if (ode == null) return Json(new { ok = false });
        ode.Aktiv = dto.Aktiv;
        await _unitOfWork.YaddaSaxlaAsync();
        return Json(new { ok = true });
    }

    // ── POST /HR/DovriOdenis/Sil ─────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Sil([FromBody] SilDto dto)
    {
        var ode = await _unitOfWork.Repository<DovriOdenis>()
            .Query().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib);

        if (ode == null) return Json(new { ok = false });
        ode.Silinib       = true;
        ode.SilinmeTarixi = DateTime.Now;
        await _unitOfWork.YaddaSaxlaAsync();
        return Json(new { ok = true });
    }

    // ── Köməkçi ──────────────────────────────────────────────
    private static DateTime NovbatiTarixHesabla(DateTime indiki, DovruluqNovu dovruluq) =>
        dovruluq switch
        {
            DovruluqNovu.Hefte => indiki.AddDays(7),
            DovruluqNovu.Ay    => indiki.AddMonths(1),
            DovruluqNovu.Rubl  => indiki.AddMonths(3),
            DovruluqNovu.Il    => indiki.AddYears(1),
            _                  => indiki.AddMonths(1)
        };

    private static string DovruluqAdi(DovruluqNovu d) => d switch
    {
        DovruluqNovu.Hefte => "Həftəlik",
        DovruluqNovu.Ay    => "Aylıq",
        DovruluqNovu.Rubl  => "Rüblük",
        DovruluqNovu.Il    => "İllik",
        _                  => "Aylıq"
    };

    // ── DTOs ─────────────────────────────────────────────────
    public class DovriOdenisDto
    {
        public int      Id            { get; set; }
        public string   Ad            { get; set; } = "";
        public int      KateqoriyaId  { get; set; }
        public int      DepartamentId { get; set; }
        public decimal  Mebleg        { get; set; }
        public int      DovruluqId    { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi  { get; set; }
        public bool     Aktiv         { get; set; } = true;
        public string?  Qeyd          { get; set; }
    }
    public class OdeEtDto  { public int Id { get; set; } }
    public class StatusDto { public int Id { get; set; } public bool Aktiv { get; set; } }
    public class SilDto    { public int Id { get; set; } }
}
