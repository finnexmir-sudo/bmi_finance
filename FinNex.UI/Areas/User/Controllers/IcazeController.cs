using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Employee")]
    public class IcazeController : Controller
    {
        private readonly IIcazeService _icazeService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public IcazeController(
            IIcazeService icazeService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _icazeService = icazeService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        // GET /User/Icaze/Index
        public async Task<IActionResult> Index()
        {
            var isci = await GetCurrentIsciAsync();
            if (isci == null) return RedirectToLogin();

            var result = await _icazeService.GetIsciIcazeleriAsync(isci.Id);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(new List<IcazeListDto>());
            }

            return View(result.Data);
        }

        // GET /User/Icaze/Create
        public async Task<IActionResult> Create()
        {
            var isci = await GetCurrentIsciAsync();
            if (isci == null) return RedirectToLogin();

            await FillEvezEdenIsciListAsync(isci.Id);

            var dto = new IcazeCreateDto
            {
                IsciId = isci.Id,
                IcazeTarixi = DateTime.Today,
                BaslamaSaati = new TimeSpan(9, 0, 0),
                BitisSaati = new TimeSpan(11, 0, 0),
            };

            return View(dto);
        }

        // POST /User/Icaze/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IcazeCreateDto dto)
        {
            var isci = await GetCurrentIsciAsync();
            if (isci == null) return RedirectToLogin();

            dto.IsciId = isci.Id;

            if (!ModelState.IsValid)
            {
                await FillEvezEdenIsciListAsync(isci.Id);
                return View(dto);
            }

            if (dto.BitisSaati <= dto.BaslamaSaati)
            {
                ModelState.AddModelError("BitisSaati", "Bitmə saatı başlama saatından sonra olmalıdır.");
                await FillEvezEdenIsciListAsync(isci.Id);
                return View(dto);
            }

            var result = await _icazeService.YaratAsync(dto);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await FillEvezEdenIsciListAsync(isci.Id);
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST /User/Icaze/Legv
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Legv(int id)
        {
            var isci = await GetCurrentIsciAsync();
            if (isci == null) return RedirectToLogin();

            var result = await _icazeService.LegvEtAsync(id, isci.Id);

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── Köməkçi metodlar ──────────────────────────────────

        private async Task<Isci?> GetCurrentIsciAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return null;

            return await _unitOfWork.Repository<Isci>()
                .GetirAsync(x => x.Id == appUser.IsciId, izlemeden: true);
        }

        private async Task FillEvezEdenIsciListAsync(int currentIsciId)
        {
            var isciler = await _unitOfWork.Repository<Isci>()
                .HamisiniGetirAsync(
                    x => x.Id != currentIsciId && x.Status == IsciStatus.Aktiv,
                    izlemeden: true);

            ViewBag.EvezEdenList = isciler
                .OrderBy(x => x.Ad)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.TamAd
                }).ToList();
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });
    }
}