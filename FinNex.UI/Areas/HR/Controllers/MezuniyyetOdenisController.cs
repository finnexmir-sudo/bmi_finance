using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// Qabaqcadan ödəniş seçilmiş məzuniyyətlər üçün Mühasib səhifəsi.
    /// HR təsdiqindən sonra müraciət buraya düşür (OdenisStatus = Gozleyir).
    /// Mühasib hesablamanın məntiqini görür (addım-addım izah), məbləği
    /// yoxlayır (lazım olarsa dəyişir) və “Ödənildi” vurur.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.Muhasib + "," + RoleNames.Admin)]
    public class MezuniyyetOdenisController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMaasHesablamaService _maasHesablamaService;
        private readonly IBildirisService _bildirisService;
        private readonly UserManager<AppUser> _userManager;

        public MezuniyyetOdenisController(
            IUnitOfWork unitOfWork,
            IMaasHesablamaService maasHesablamaService,
            IBildirisService bildirisService,
            UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _maasHesablamaService = maasHesablamaService;
            _bildirisService = bildirisService;
            _userManager = userManager;
        }

        // ── GET /HR/MezuniyyetOdenis ─────────────────────────────
        //  Filter: status = gozleyir (default) / odenilib / hamisi
        public async Task<IActionResult> Index(string filter = "gozleyir")
        {
            var query = _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x => !x.Silinib && x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis)
                .Include(x => x.Isci);

            IQueryable<Mezuniyyet> filtered = filter switch
            {
                "odenilib" => query.Where(x => x.OdenisStatus == MezuniyyetOdenisStatus.Odenilib),
                "hamisi" => query,
                _ => query.Where(x => x.OdenisStatus == MezuniyyetOdenisStatus.Gozleyir)
            };

            var list = await filtered
                .OrderByDescending(x => x.HrTesdiqTarixi)
                .ThenByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            ViewBag.Filter = filter;
            ViewBag.GozleyirSay = await _unitOfWork.Repository<Mezuniyyet>().Query()
                .CountAsync(x => !x.Silinib
                    && x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                    && x.OdenisStatus == MezuniyyetOdenisStatus.Gozleyir);

            return View(list);
        }

        // ── GET /HR/MezuniyyetOdenis/Detail/5 ────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var mez = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x => x.Id == id && !x.Silinib)
                .Include(x => x.Isci)
                .Include(x => x.HrIsci)
                .Include(x => x.OdeyenMuhasib)
                .FirstOrDefaultAsync();

            if (mez == null) return NotFound();

            // Yenidən canlı hesablama aparırıq — Mühasib kodlara baxmadan
            // məntiqi görə bilsin və cari rəqəmlərin eyni olduğundan əmin olsun.
            var hesab = await _maasHesablamaService
                .MezuniyyetOdenisiDetalliHesablaAsync(mez.IsciId, mez.BaslamaTarixi, mez.BitmeTarixi);

            ViewBag.Mezuniyyet = mez;
            return View(hesab);
        }

        // ── POST /HR/MezuniyyetOdenis/Odenildi ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Odenildi(int id, decimal? redakteEdilmisMebleg)
        {
            var mez = await _unitOfWork.Repository<Mezuniyyet>()
                .GetirAsync(x => x.Id == id);

            if (mez == null)
            {
                TempData["Error"] = "Müraciət tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            if (mez.OdenisTipi != MezuniyyetOdenisTipi.QabaqcadanOdenis)
            {
                TempData["Error"] = "Bu müraciət qabaqcadan ödənişə təyin olunmayıb.";
                return RedirectToAction(nameof(Index));
            }

            if (mez.OdenisStatus == MezuniyyetOdenisStatus.Odenilib)
            {
                TempData["Error"] = "Bu müraciət artıq ödənilmiş kimi qeyd olunub.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // Mühasib məbləği düzəldə bilər. Əgər verilməyibsə, HR təsdiq anında
            // hesablanmış məbləği istifadə edirik.
            decimal? yekunMebleg = redakteEdilmisMebleg.HasValue && redakteEdilmisMebleg.Value > 0
                ? Math.Round(redakteEdilmisMebleg.Value, 2)
                : mez.OdenenMebleg;

            if (yekunMebleg == null || yekunMebleg <= 0)
            {
                TempData["Error"] = "Ödəniş məbləği düzgün deyil.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var appUser = await _userManager.GetUserAsync(User);
            var muhasibIsciId = appUser?.IsciId;

            mez.OdenenMebleg = yekunMebleg;
            mez.OdenisStatus = MezuniyyetOdenisStatus.Odenilib;
            mez.OdenilmeTarixi = DateTime.Now;
            mez.OdeyenMuhasibId = muhasibIsciId;
            mez.YenilenmeTarixi = DateTime.Now;

            await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(mez);
            await _unitOfWork.YaddaSaxlaAsync();

            // İşçiyə bildiriş göndər
            try
            {
                await _bildirisService.YaratAsync(
                    isciId: mez.IsciId,
                    nov: BildirisNovu.MezuniyyetTesdiq,
                    bashliq: "Məzuniyyət ödənişi edildi",
                    metn: $"{mez.BaslamaTarixi:dd.MM.yyyy}–{mez.BitmeTarixi:dd.MM.yyyy} məzuniyyət ödənişiniz " +
                          $"({yekunMebleg:N2} ₼) Mühasibiyyat tərəfindən həyata keçirildi.",
                    redirectUrl: Url.Action("Detail", "Mezuniyyet", new { area = "User", id = mez.Id }),
                    mezuniyyetId: mez.Id
                );
            }
            catch { /* bildiriş xətası əsas işləməni pozmasın */ }

            TempData["Success"] = $"Məzuniyyət ödənişi təsdiqləndi ({yekunMebleg:N2} ₼).";
            return RedirectToAction(nameof(Index));
        }
    }
}
