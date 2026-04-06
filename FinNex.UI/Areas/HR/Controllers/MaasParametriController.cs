using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class MaasParametriController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public MaasParametriController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET /HR/MaasParametri
        public async Task<IActionResult> Index()
        {
            var parametrler = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .Where(x => !x.Silinib)
                .OrderBy(x => x.Nov)
                .ToListAsync();

            ViewData["Title"] = "Vergi Parametrləri";
            return View(parametrler);
        }

        // GET /HR/MaasParametri/GetById/5
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (p == null)
                return Json(new { success = false, message = "Parametr tapılmadı." });

            return Json(new
            {
                success = true,
                data = new
                {
                    p.Id,
                    Nov = (int)p.Nov,
                    Tip = (int)p.Tip,
                    p.Deyer,
                    p.Aciqlama,
                    BaslamaTarixi = p.BaslamaTarixi.ToString("yyyy-MM-dd"),
                    BitmeTarixi = p.BitmeTarixi?.ToString("yyyy-MM-dd"),
                    p.Aktivdir
                }
            });
        }

        // POST /HR/MaasParametri/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            MaasParametrNovu nov,
            MaasParametrTipi tip,
            decimal deyer,
            string? aciqlama,
            DateTime baslamaTarixi,
            DateTime? bitmeTarixi)
        {
            if (deyer <= 0)
                return Json(new { success = false, message = "Dəyər 0-dan böyük olmalıdır." });

            if (bitmeTarixi.HasValue && bitmeTarixi <= baslamaTarixi)
                return Json(new { success = false, message = "Bitmə tarixi başlama tarixindən sonra olmalıdır." });

            var entity = new MaasParametri
            {
                Nov = nov,
                Tip = tip,
                Deyer = deyer,
                Aciqlama = aciqlama,
                BaslamaTarixi = baslamaTarixi,
                BitmeTarixi = bitmeTarixi,
                Aktivdir = true
            };

            await _unitOfWork.Repository<MaasParametri>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Parametr uğurla əlavə edildi." });
        }

        // POST /HR/MaasParametri/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            MaasParametrNovu nov,
            MaasParametrTipi tip,
            decimal deyer,
            string? aciqlama,
            DateTime baslamaTarixi,
            DateTime? bitmeTarixi,
            bool aktivdir)
        {
            var entity = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (entity == null)
                return Json(new { success = false, message = "Parametr tapılmadı." });

            if (deyer <= 0)
                return Json(new { success = false, message = "Dəyər 0-dan böyük olmalıdır." });

            if (bitmeTarixi.HasValue && bitmeTarixi <= baslamaTarixi)
                return Json(new { success = false, message = "Bitmə tarixi başlama tarixindən sonra olmalıdır." });

            entity.Nov = nov;
            entity.Tip = tip;
            entity.Deyer = deyer;
            entity.Aciqlama = aciqlama;
            entity.BaslamaTarixi = baslamaTarixi;
            entity.BitmeTarixi = bitmeTarixi;
            entity.Aktivdir = aktivdir;

            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Parametr uğurla yeniləndi." });
        }

        // POST /HR/MaasParametri/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (entity == null)
                return Json(new { success = false, message = "Parametr tapılmadı." });

            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.Aktivdir = false;

            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Parametr uğurla silindi." });
        }
    }
}
