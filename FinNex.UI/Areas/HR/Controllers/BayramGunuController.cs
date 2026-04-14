using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
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
                herIlTeyinOlunur = entity.HerIlTeyinOlunur,
                tip = (int)entity.Tip
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] string ad, [FromForm] DateTime baslangicTarix, [FromForm] DateTime? bitisTarix, [FromForm] bool herIlTeyinOlunur, [FromForm] int tip = 1)
        {
            if (string.IsNullOrWhiteSpace(ad))
                return Json(new { success = false, message = "Ad daxil edin." });

            var son = bitisTarix ?? baslangicTarix;
            if (son < baslangicTarix)
                return Json(new { success = false, message = "Bitis tarixi baslangicdan evvel ola bilmez." });

            var gunTipi = tip == 2 ? GunTipi.IsGunu : GunTipi.Bayram;

            var repo = _unitOfWork.Repository<BayramGunu>();
            int sayi = 0;

            for (var gun = baslangicTarix; gun <= son; gun = gun.AddDays(1))
            {
                var entity = new BayramGunu
                {
                    Ad = ad.Trim(),
                    Tarix = gun,
                    HerIlTeyinOlunur = herIlTeyinOlunur,
                    Tip = gunTipi
                };
                await repo.YaratAsync(entity);
                sayi++;
            }

            await _unitOfWork.YaddaSaxlaAsync();
            var tipAd = gunTipi == GunTipi.IsGunu ? "iş günü" : "bayram";
            var msg = sayi == 1 ? $"Qeyd uğurla əlavə edildi ({tipAd})." : $"{sayi} {tipAd} qeydi uğurla əlavə edildi.";
            return Json(new { success = true, message = msg });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] int id, [FromForm] string ad, [FromForm] DateTime baslangicTarix, [FromForm] bool herIlTeyinOlunur, [FromForm] int tip = 1)
        {
            if (string.IsNullOrWhiteSpace(ad))
                return Json(new { success = false, message = "Ad daxil edin." });

            var repo = _unitOfWork.Repository<BayramGunu>();
            var entity = await repo.IdIleGetirAsync(id);

            if (entity == null || entity.Silinib)
                return Json(new { success = false, message = "Qeyd tapilmadi." });

            entity.Ad = ad.Trim();
            entity.Tarix = baslangicTarix;
            entity.HerIlTeyinOlunur = herIlTeyinOlunur;
            entity.Tip = tip == 2 ? GunTipi.IsGunu : GunTipi.Bayram;
            entity.YenilenmeTarixi = DateTime.Now;

            await repo.YenileAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Qeyd uğurla yenilendi." });
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
                herIlTeyinOlunur = x.HerIlTeyinOlunur,
                tip = (int)x.Tip,
                tipAd = x.Tip == GunTipi.IsGunu ? "İş günü" : "Bayram"
            });

            return Json(new { records = data, stats = new { cemi = ordered.Count, gelecek } });
        }
    }
}
