using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// Vergi güzəşti kataloqu — HR/Muhasib/Admin tərəfindən idarə olunur.
    /// İşçi təyinatları ayrıca IsciGuzestController-dadır.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class GuzestController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public GuzestController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _unitOfWork.Repository<Guzest>()
                .Query()
                .Where(x => !x.Silinib)
                .OrderByDescending(x => x.Aktivdir)
                .ThenBy(x => x.Ad)
                .ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create() => View(new Guzest { Aktivdir = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guzest model)
        {
            if (string.IsNullOrWhiteSpace(model.Ad))
                ModelState.AddModelError(nameof(model.Ad), "Ad daxil edin.");
            if (model.Mebleg < 0)
                ModelState.AddModelError(nameof(model.Mebleg), "Məbləğ mənfi ola bilməz.");

            if (!ModelState.IsValid) return View(model);

            var entity = new Guzest
            {
                Ad = model.Ad.Trim(),
                Mebleg = Math.Round(model.Mebleg, 2),
                Madde = string.IsNullOrWhiteSpace(model.Madde) ? null : model.Madde.Trim(),
                Aktivdir = model.Aktivdir
            };
            await _unitOfWork.Repository<Guzest>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();
            TempData["Success"] = $"\"{entity.Ad}\" güzəşti əlavə olundu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _unitOfWork.Repository<Guzest>().IdIleGetirAsync(id);
            if (entity == null || entity.Silinib) return NotFound();
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guzest model)
        {
            if (string.IsNullOrWhiteSpace(model.Ad))
                ModelState.AddModelError(nameof(model.Ad), "Ad daxil edin.");
            if (model.Mebleg < 0)
                ModelState.AddModelError(nameof(model.Mebleg), "Məbləğ mənfi ola bilməz.");

            if (!ModelState.IsValid) return View(model);

            var entity = await _unitOfWork.Repository<Guzest>().IdIleGetirAsync(model.Id);
            if (entity == null || entity.Silinib) return NotFound();

            entity.Ad = model.Ad.Trim();
            entity.Mebleg = Math.Round(model.Mebleg, 2);
            entity.Madde = string.IsNullOrWhiteSpace(model.Madde) ? null : model.Madde.Trim();
            entity.Aktivdir = model.Aktivdir;
            entity.YenilenmeTarixi = DateTime.Now;

            await _unitOfWork.Repository<Guzest>().YenileAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();
            TempData["Success"] = $"\"{entity.Ad}\" güzəşti yeniləndi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _unitOfWork.Repository<Guzest>().YumshakSilAsync(id);
            await _unitOfWork.YaddaSaxlaAsync();
            TempData[result ? "Success" : "Error"] = result
                ? "Güzəşt silindi."
                : "Güzəşt tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
    }
}
