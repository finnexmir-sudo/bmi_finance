using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.HR.ViewModels.Mezuniyyet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize]
    public class MezuniyyetController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IBildirisService _bildirisService;
        private readonly IUnitOfWork _unitOfWork;

        public MezuniyyetController(
            IMezuniyyetService mezuniyyetService,
            IIsciService isciService,
            UserManager<AppUser> userManager,
            IBildirisService bildirisService,
            IUnitOfWork unitOfWork)
        {
            _mezuniyyetService = mezuniyyetService;
            _isciService = isciService;
            _userManager = userManager;
            _bildirisService = bildirisService;
            _unitOfWork = unitOfWork;
        }

        // ── Köməkçi: cari istifadəçinin IsciId-sini alır ──────
        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        // ── Köməkçi: cari işçinin aktiv departament ID-sini alır ──
        private async Task<int?> GetCurrentDepartamentIdAsync(int isciId)
        {
            var result = await _isciService.GetAktivDepartamentIdAsync(isciId);
            return result.Success ? result.Data : null;
        }

        // ══════════════════════════════════════════════════════
        // ŞÖBƏ RƏİSİ BÖLÜMÜ
        // Rol: "SobeReisi"
        // ══════════════════════════════════════════════════════

        //[Authorize(Roles = "SobeReisi,Admin")]
        //public async Task<IActionResult> SobeReisi()
        //{
        //    var isciId = await GetCurrentIsciIdAsync();
        //    if (isciId == null) return Forbid();

        //    var departamentId = await GetCurrentDepartamentIdAsync(isciId.Value);
        //    if (departamentId == null) return Forbid();

        //    var result = await _mezuniyyetService.GetSobeyeGoreMezuniyyetlerAsync(departamentId.Value);

        //    var vm = new HrMezuniyyetIndexVM
        //    {
        //        Mezuniyyetler = result.Success ? result.Data!.ToList() : new(),
        //        PageTitle = "Şöbə Rəisi — Gözləyən Müraciətlər",
        //        TesdiqAction = "SobeReisiTesdiq"
        //    };

        //    ViewData["Title"] = "Şöbə Rəisi Təsdiqi";
        //    return View("TesdiqIndex", vm);
        //}

        // ══════════════════════════════════════════════════════
        // RƏHBƏR BÖLÜMÜ
        // Rol: "Rehber"
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> Rehber()
        {
            var result = await _mezuniyyetService.GetRehberTesdiqindeAsync();

            var vm = new HrMezuniyyetIndexVM
            {
                Mezuniyyetler = result.Success ? result.Data!.ToList() : new(),
                PageTitle = "Rəhbər — Gözləyən Müraciətlər",
                TesdiqAction = "RehberTesdiq"
            };

            ViewData["Title"] = "Rəhbər Təsdiqi";
            return View("TesdiqIndex", vm);
        }

        // ══════════════════════════════════════════════════════
        // HR BÖLÜMÜ
        // Rol: "HR"
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Hr()
        {
            var result = await _mezuniyyetService.GetHrTesdiqindeAsync();

            var vm = new HrMezuniyyetIndexVM
            {
                Mezuniyyetler = result.Success ? result.Data!.ToList() : new(),
                PageTitle = "HR — Son Təsdiq",
                TesdiqAction = "HrTesdiq"
            };

            ViewData["Title"] = "HR Təsdiqi";
            return View("TesdiqIndex", vm);
        }

        // ══════════════════════════════════════════════════════
        // DETAL SƏHİFƏSİ (3 rol üçün ortaq)
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = RoleNames.SobeReisi + "," + RoleNames.Rehber + "," + RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Detal(int id, string returnAction = "Hr")
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success || result.Data == null) return NotFound();

            var vm = new HrMezuniyyetDetalVM
            {
                Mezuniyyet = result.Data,
                ReturnAction = returnAction
            };

            ViewData["Title"] = "Müraciət Detalı";
            return View("Detail", vm);
        }

        // ══════════════════════════════════════════════════════
        // POST — ŞÖBƏ RƏİSİ TƏSDİQİ
        // ══════════════════════════════════════════════════════

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "SobeReisi,Admin")]
        //public async Task<IActionResult> SobeReisiTesdiq(int id, bool status, string? qeyd)
        //{
        //    var isciId = await GetCurrentIsciIdAsync();
        //    if (isciId == null) return Forbid();

        //    // Məzuniyyəti yükləyib SobeReisiId-ni set edirik
        //    var mezResult = await _mezuniyyetService.IdIleGetirAsync(id);
        //    if (!mezResult.Success || mezResult.Data == null) return NotFound();

        //    var result = await _mezuniyyetService.SobeReisiTesdiqAsync(id, status, qeyd, isciId.Value);

        //    TempData[result.Success ? "Success" : "Error"] = result.Message;
        //    return RedirectToAction(nameof(SobeReisi));
        //}

        // ══════════════════════════════════════════════════════
        // POST — RƏHBƏR TƏSDİQİ
        // ══════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> RehberTesdiq(int id, bool status, string? qeyd)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();

            var result = await _mezuniyyetService.RehberTesdiqAsync(id, status, qeyd, isciId.Value);

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Rehber));
        }

        // ══════════════════════════════════════════════════════
        // POST — HR TƏSDİQİ
        // ══════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> HrTesdiq(int id, bool status, string? qeyd)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();

            var result = await _mezuniyyetService.HrTesdiqAsync(id, status, qeyd, isciId.Value);

            // HR müsbət qərar verdi VƏ qabaqcadan ödəniş seçilibsə —
            // Mühasibə “ödəniş gözləyir” bildirişi göndər.
            if (result.Success && status)
            {
                await NotifyMuhasibIfAdvanceAsync(id);
            }

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Hr));
        }

        private async Task NotifyMuhasibIfAdvanceAsync(int mezuniyyetId)
        {
            try
            {
                var m = await _unitOfWork.Repository<Mezuniyyet>()
                    .GetirAsync(x => x.Id == mezuniyyetId);
                if (m == null || m.OdenisTipi != MezuniyyetOdenisTipi.QabaqcadanOdenis) return;

                var isci = await _unitOfWork.Repository<Isci>()
                    .GetirAsync(x => x.Id == m.IsciId);
                var isciAd = isci?.TamAd ?? $"İşçi #{m.IsciId}";

                var redirectUrl = Url.Action("Detail", "MezuniyyetOdenis",
                    new { area = "HR", id = mezuniyyetId });

                var muhasibler = await _userManager.GetUsersInRoleAsync(RoleNames.Muhasib);
                var adminler = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                var alicilar = muhasibler.Concat(adminler)
                    .Where(u => u.IsciId.HasValue)
                    .GroupBy(u => u.IsciId!.Value)
                    .Select(g => g.First())
                    .ToList();

                var bashliq = "Məzuniyyət ödənişi — qabaqcadan";
                var metn = $"{isciAd} üçün {m.BaslamaTarixi:dd.MM.yyyy}–{m.BitmeTarixi:dd.MM.yyyy} " +
                           $"məzuniyyət ödənişi gözləyir (ilkin hesablama: {m.OdenenMebleg:N2} ₼).";

                foreach (var u in alicilar)
                {
                    await _bildirisService.YaratAsync(
                        isciId: u.IsciId!.Value,
                        nov: BildirisNovu.TesdiqSorgusu,
                        bashliq: bashliq,
                        metn: metn,
                        redirectUrl: redirectUrl,
                        mezuniyyetId: mezuniyyetId
                    );
                }
            }
            catch
            {
                // Bildiriş xətası əsas işləməni pozmasın.
            }
        }

        // ══════════════════════════════════════════════════════
        // ADMIN — hamısını görür
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Index()
        {
            var result = await _mezuniyyetService.GetListAsync();

            var vm = new HrMezuniyyetIndexVM
            {
                Mezuniyyetler = result.Success ? result.Data!.ToList() : new(),
                PageTitle = "Bütün Müraciətlər",
                TesdiqAction = ""
            };

            ViewData["Title"] = "Bütün Məzuniyyət Müraciətləri";
            return View("TesdiqIndex", vm);
        }

        // Xəstəlik/Ezamiyyət əməliyyatları XestelikEzamiyyetController-ə köçürülüb
    }
}