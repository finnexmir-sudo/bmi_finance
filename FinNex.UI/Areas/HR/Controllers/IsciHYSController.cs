using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class IsciHYSController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public IsciHYSController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── GET: butun HYS teyinatlari (filter: isciId optional) ──
        public async Task<IActionResult> Index(int? isciId = null)
        {
            var query = _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
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
            await FillDropdownsAsync();
            return View(new IsciHYS
            {
                IsciId = isciId ?? 0,
                BaslamaTarixi = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("IsciId,Mebleg,BaslamaTarixi,BitmeTarixi,Qeyd")] IsciHYS model)
        {
            ModelState.Remove(nameof(model.Isci));

            if (model.IsciId == 0)
                ModelState.AddModelError(nameof(model.IsciId), "İşçi seçin.");
            if (model.Mebleg <= 0)
                ModelState.AddModelError(nameof(model.Mebleg), "Məbləğ 0-dan böyük olmalıdır.");
            if (model.BitmeTarixi.HasValue && model.BitmeTarixi < model.BaslamaTarixi)
                ModelState.AddModelError(nameof(model.BitmeTarixi),
                    "Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            // Eyni isci ucun kesisen dovrde ikinci HYS olmasin
            if (ModelState.IsValid)
            {
                var mevcud = await _unitOfWork.Repository<IsciHYS>()
                    .Query()
                    .AnyAsync(x =>
                        !x.Silinib &&
                        x.IsciId == model.IsciId &&
                        x.BaslamaTarixi <= (model.BitmeTarixi ?? DateTime.MaxValue) &&
                        (x.BitmeTarixi ?? DateTime.MaxValue) >= model.BaslamaTarixi);
                if (mevcud)
                    ModelState.AddModelError(nameof(model.IsciId),
                        "Bu işçi üçün seçilən dövrdə artıq HYS təyinatı mövcuddur.");
            }

            if (!ModelState.IsValid)
            {
                await FillDropdownsAsync();
                return View(model);
            }

            var entity = new IsciHYS
            {
                IsciId = model.IsciId,
                Mebleg = model.Mebleg,
                BaslamaTarixi = model.BaslamaTarixi.Date,
                BitmeTarixi = model.BitmeTarixi?.Date,
                Qeyd = string.IsNullOrWhiteSpace(model.Qeyd) ? null : model.Qeyd.Trim()
            };
            await _unitOfWork.Repository<IsciHYS>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();
            TempData["Success"] = "HYS təyinatı yaradıldı.";
            return RedirectToAction(nameof(Index), new { isciId = model.IsciId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x => x.Id == id && !x.Silinib)
                .Include(x => x.Isci)
                .FirstOrDefaultAsync();

            if (entity == null)
            {
                TempData["Error"] = "Təyinat tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            await FillDropdownsAsync();
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,IsciId,Mebleg,BaslamaTarixi,BitmeTarixi,Qeyd")] IsciHYS model)
        {
            ModelState.Remove(nameof(model.Isci));

            if (model.Mebleg <= 0)
                ModelState.AddModelError(nameof(model.Mebleg), "Məbləğ 0-dan böyük olmalıdır.");
            if (model.BitmeTarixi.HasValue && model.BitmeTarixi < model.BaslamaTarixi)
                ModelState.AddModelError(nameof(model.BitmeTarixi),
                    "Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            // Kesisen dovr yoxlamasi (ozunu istisna et)
            if (ModelState.IsValid)
            {
                var mevcud = await _unitOfWork.Repository<IsciHYS>()
                    .Query()
                    .AnyAsync(x =>
                        !x.Silinib &&
                        x.Id != id &&
                        x.IsciId == model.IsciId &&
                        x.BaslamaTarixi <= (model.BitmeTarixi ?? DateTime.MaxValue) &&
                        (x.BitmeTarixi ?? DateTime.MaxValue) >= model.BaslamaTarixi);
                if (mevcud)
                    ModelState.AddModelError(nameof(model.IsciId),
                        "Bu işçi üçün seçilən dövrdə artıq HYS təyinatı mövcuddur.");
            }

            if (!ModelState.IsValid)
            {
                await FillDropdownsAsync();
                return View(model);
            }

            var entity = await _unitOfWork.Repository<IsciHYS>().IdIleGetirAsync(id);
            if (entity == null || entity.Silinib)
            {
                TempData["Error"] = "Təyinat tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            entity.Mebleg = model.Mebleg;
            entity.BaslamaTarixi = model.BaslamaTarixi.Date;
            entity.BitmeTarixi = model.BitmeTarixi?.Date;
            entity.Qeyd = string.IsNullOrWhiteSpace(model.Qeyd) ? null : model.Qeyd.Trim();
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "HYS təyinatı yeniləndi.";
            return RedirectToAction(nameof(Index), new { isciId = entity.IsciId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _unitOfWork.Repository<IsciHYS>().YumshaqSilAsync(id);
            await _unitOfWork.YaddaSaxlaAsync();
            TempData[result ? "Success" : "Error"] = result
                ? "HYS təyinatı silindi."
                : "Təyinat tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        private async Task FillDropdownsAsync()
        {
            ViewBag.Isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .OrderBy(x => x.Ad)
                .ToListAsync();
        }
    }
}
