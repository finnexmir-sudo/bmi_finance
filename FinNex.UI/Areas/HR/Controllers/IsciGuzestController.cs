using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// İşçilərə vergi güzəşti təyin edir. Hesablamada aktiv dövrü olan qeydlərin
    /// ən böyüyü götürülür.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class IsciGuzestController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public IsciGuzestController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── GET: bütün təyinatların siyahısı (filter: isciId optional) ──
        public async Task<IActionResult> Index(int? isciId = null)
        {
            var query = _unitOfWork.Repository<IsciGuzest>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                .Include(x => x.Guzest)
                .AsQueryable();

            if (isciId.HasValue)
                query = query.Where(x => x.IsciId == isciId.Value);

            var list = await query
                .OrderByDescending(x => x.BaslamaTarixi)
                .ThenBy(x => x.Isci.Ad)
                .ToListAsync();

            ViewBag.IsciId = isciId;
            ViewBag.Isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? isciId = null)
        {
            await FillDropdownsAsync(isciId);
            return View(new IsciGuzest
            {
                IsciId = isciId ?? 0,
                BaslamaTarixi = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IsciGuzest model)
        {
            if (model.IsciId == 0)
                ModelState.AddModelError(nameof(model.IsciId), "İşçi seçin.");
            if (model.GuzestId == 0)
                ModelState.AddModelError(nameof(model.GuzestId), "Güzəşt seçin.");
            if (model.BitmeTarixi.HasValue && model.BitmeTarixi < model.BaslamaTarixi)
                ModelState.AddModelError(nameof(model.BitmeTarixi), "Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            // Eyni güzəşt həmin işçiyə kəsişən dövrdə ikinci dəfə təyin olunmasın
            if (ModelState.IsValid)
            {
                var mevcud = await _unitOfWork.Repository<IsciGuzest>()
                    .Query()
                    .AnyAsync(x =>
                        !x.Silinib &&
                        x.IsciId == model.IsciId &&
                        x.GuzestId == model.GuzestId &&
                        x.BaslamaTarixi <= (model.BitmeTarixi ?? DateTime.MaxValue) &&
                        (x.BitmeTarixi ?? DateTime.MaxValue) >= model.BaslamaTarixi);
                if (mevcud)
                    ModelState.AddModelError(nameof(model.GuzestId),
                        "Bu işçi üçün həmin güzəşt seçilən dövrdə artıq təyin olunub.");
            }

            if (!ModelState.IsValid)
            {
                await FillDropdownsAsync(model.IsciId);
                return View(model);
            }

            var entity = new IsciGuzest
            {
                IsciId = model.IsciId,
                GuzestId = model.GuzestId,
                BaslamaTarixi = model.BaslamaTarixi.Date,
                BitmeTarixi = model.BitmeTarixi?.Date,
                Qeyd = string.IsNullOrWhiteSpace(model.Qeyd) ? null : model.Qeyd.Trim()
            };
            await _unitOfWork.Repository<IsciGuzest>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();
            TempData["Success"] = "Güzəşt işçiyə təyin olundu.";
            return RedirectToAction(nameof(Index), new { isciId = model.IsciId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _unitOfWork.Repository<IsciGuzest>().YumshakSilAsync(id);
            await _unitOfWork.YaddaSaxlaAsync();
            TempData[result ? "Success" : "Error"] = result
                ? "Təyinat silindi."
                : "Təyinat tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        private async Task FillDropdownsAsync(int? isciId)
        {
            ViewBag.Isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            ViewBag.Guzestler = await _unitOfWork.Repository<Guzest>()
                .Query()
                .Where(x => !x.Silinib && x.Aktivdir)
                .OrderBy(x => x.Ad)
                .ToListAsync();
        }
    }
}
