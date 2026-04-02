using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = "HR,Admin")]
    public class BayramGunuController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public BayramGunuController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var repo = _unitOfWork.Repository<BayramGunu>();
            var list = await repo.HamisiniGetirAsync(x => !x.Silinib);
            var ordered = list.OrderBy(x => x.Tarix).ToList();

            var bugun = DateTime.Today;
            var gelecek = ordered.Count(x => x.Tarix.Date >= bugun);

            ViewBag.Cemi = ordered.Count;
            ViewBag.Gelecek = gelecek;

            return View(ordered);
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var repo = _unitOfWork.Repository<BayramGunu>();
            var entity = await repo.IdIleGetirAsync(id);

            if (entity == null || entity.Silinib)
                return NotFound();

            return Json(new
            {
                id = entity.Id,
                ad = entity.Ad,
                tarix = entity.Tarix.ToString("yyyy-MM-dd"),
                herIlTeyinOlunur = entity.HerIlTeyinOlunur
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] string ad, [FromForm] DateTime tarix, [FromForm] bool herIlTeyinOlunur)
        {
            if (string.IsNullOrWhiteSpace(ad))
                return Json(new { success = false, message = "Bayram adini daxil edin." });

            var repo = _unitOfWork.Repository<BayramGunu>();

            var entity = new BayramGunu
            {
                Ad = ad.Trim(),
                Tarix = tarix,
                HerIlTeyinOlunur = herIlTeyinOlunur
            };

            await repo.YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Bayram ugurla elave edildi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] int id, [FromForm] string ad, [FromForm] DateTime tarix, [FromForm] bool herIlTeyinOlunur)
        {
            if (string.IsNullOrWhiteSpace(ad))
                return Json(new { success = false, message = "Bayram adini daxil edin." });

            var repo = _unitOfWork.Repository<BayramGunu>();
            var entity = await repo.IdIleGetirAsync(id);

            if (entity == null || entity.Silinib)
                return Json(new { success = false, message = "Bayram tapilmadi." });

            entity.Ad = ad.Trim();
            entity.Tarix = tarix;
            entity.HerIlTeyinOlunur = herIlTeyinOlunur;
            entity.YenilenmeTarixi = DateTime.Now;

            await repo.YenileAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Bayram ugurla yenilendi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var repo = _unitOfWork.Repository<BayramGunu>();
            var result = await repo.YumshakSilAsync(id);
            await _unitOfWork.YaddaSaxlaAsync();

            if (!result)
                return Json(new { success = false, message = "Bayram tapilmadi." });

            return Json(new { success = true, message = "Bayram ugurla silindi." });
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var repo = _unitOfWork.Repository<BayramGunu>();
            var list = await repo.HamisiniGetirAsync(x => !x.Silinib);
            var ordered = list.OrderBy(x => x.Tarix).ToList();

            var bugun = DateTime.Today;
            var gelecek = ordered.Count(x => x.Tarix.Date >= bugun);

            var data = ordered.Select((x, i) => new
            {
                index = i + 1,
                id = x.Id,
                ad = x.Ad,
                tarix = x.Tarix.ToString("dd.MM.yyyy"),
                herIlTeyinOlunur = x.HerIlTeyinOlunur
            });

            return Json(new { records = data, stats = new { cemi = ordered.Count, gelecek } });
        }
    }
}
