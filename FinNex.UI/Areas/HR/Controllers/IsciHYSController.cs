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
                .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
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
            [Bind("IsciId,Sirket,Mebleg,BaslamaTarixi,BitmeTarixi,Qeyd")] IsciHYS model)
        {
            ModelState.Remove(nameof(model.Isci));

            if (model.IsciId == 0)
                ModelState.AddModelError(nameof(model.IsciId), "İşçi seçin.");
            if (model.Mebleg <= 0)
                ModelState.AddModelError(nameof(model.Mebleg), "Məbləğ 0-dan böyük olmalıdır.");
            if (model.BitmeTarixi.HasValue && model.BitmeTarixi < model.BaslamaTarixi)
                ModelState.AddModelError(nameof(model.BitmeTarixi),
                    "Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            // 50% limit yoxlaması — HYS məbləği əsas maaşın 50%-ni keçə bilməz
            if (ModelState.IsValid && model.IsciId > 0)
            {
                var maliye = await _unitOfWork.Repository<IsciMaliye>()
                    .Query()
                    .FirstOrDefaultAsync(x => x.IsciId == model.IsciId && !x.Silinib);
                if (maliye != null)
                {
                    var maxFaizParam = await _unitOfWork.Repository<MaasParametri>()
                        .Query()
                        .Where(x => x.Aktivdir && !x.Silinib && x.Nov == MaasParametrNovu.HysMaxMaasFaizi)
                        .OrderByDescending(x => x.BaslamaTarixi)
                        .FirstOrDefaultAsync();
                    decimal maxFaiz = maxFaizParam?.Deyer ?? 50m;
                    decimal maxHys = Math.Round(maliye.CariMaas * (maxFaiz / 100m), 2);

                    if (model.Mebleg > maxHys)
                        ModelState.AddModelError(nameof(model.Mebleg),
                            $"HYS məbləği əsas maaşın {maxFaiz:G29}%-ni keçə bilməz. " +
                            $"Maaş: {maliye.CariMaas:N2} ₼, max HYS: {maxHys:N2} ₼");
                }
            }

            // İşçi bir neçə şirkətdə HYS aça bilər. Yalnız EYNİ İŞÇİ + EYNİ ŞİRKƏT
            // dövr kəsişməsi qadağandır (dublikat). Şirkət boşdursa, eyni cür
            // boş olan başqa qeydlərlə dövr kəsişməsinə icazə verilmir.
            if (ModelState.IsValid)
            {
                var sirketNorm = string.IsNullOrWhiteSpace(model.Sirket) ? null : model.Sirket.Trim();
                var mevcud = await _unitOfWork.Repository<IsciHYS>()
                    .Query()
                    .AnyAsync(x =>
                        !x.Silinib &&
                        x.IsciId == model.IsciId &&
                        x.Sirket == sirketNorm &&
                        x.BaslamaTarixi <= (model.BitmeTarixi ?? DateTime.MaxValue) &&
                        (x.BitmeTarixi ?? DateTime.MaxValue) >= model.BaslamaTarixi);
                if (mevcud)
                    ModelState.AddModelError(nameof(model.Sirket),
                        sirketNorm == null
                            ? "Bu işçi üçün şirkəti göstərilməyən HYS-də artıq dövr kəsişməsi var. Şirkət adını daxil edin."
                            : $"Bu işçi üçün \"{sirketNorm}\" şirkətində seçilən dövrdə artıq HYS mövcuddur.");
            }

            if (!ModelState.IsValid)
            {
                await FillDropdownsAsync();
                return View(model);
            }

            var entity = new IsciHYS
            {
                IsciId = model.IsciId,
                Sirket = string.IsNullOrWhiteSpace(model.Sirket) ? null : model.Sirket.Trim(),
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
            [Bind("Id,IsciId,Sirket,Mebleg,BaslamaTarixi,BitmeTarixi,Qeyd")] IsciHYS model)
        {
            ModelState.Remove(nameof(model.Isci));

            if (model.Mebleg <= 0)
                ModelState.AddModelError(nameof(model.Mebleg), "Məbləğ 0-dan böyük olmalıdır.");
            if (model.BitmeTarixi.HasValue && model.BitmeTarixi < model.BaslamaTarixi)
                ModelState.AddModelError(nameof(model.BitmeTarixi),
                    "Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            // 50% limit yoxlaması
            if (ModelState.IsValid && model.IsciId > 0)
            {
                var maliye = await _unitOfWork.Repository<IsciMaliye>()
                    .Query()
                    .FirstOrDefaultAsync(x => x.IsciId == model.IsciId && !x.Silinib);
                if (maliye != null)
                {
                    var maxFaizParam = await _unitOfWork.Repository<MaasParametri>()
                        .Query()
                        .Where(x => x.Aktivdir && !x.Silinib && x.Nov == MaasParametrNovu.HysMaxMaasFaizi)
                        .OrderByDescending(x => x.BaslamaTarixi)
                        .FirstOrDefaultAsync();
                    decimal maxFaiz = maxFaizParam?.Deyer ?? 50m;
                    decimal maxHys = Math.Round(maliye.CariMaas * (maxFaiz / 100m), 2);

                    if (model.Mebleg > maxHys)
                        ModelState.AddModelError(nameof(model.Mebleg),
                            $"HYS məbləği əsas maaşın {maxFaiz:G29}%-ni keçə bilməz. " +
                            $"Maaş: {maliye.CariMaas:N2} ₼, max HYS: {maxHys:N2} ₼");
                }
            }

            // Eyni işçi + eyni şirkət birləşməsində dövr kəsişməsi yoxlanılır
            // (özünü istisna et). Fərqli şirkətlər ilə dövr kəsişməsinə icazə var.
            if (ModelState.IsValid)
            {
                var sirketNorm = string.IsNullOrWhiteSpace(model.Sirket) ? null : model.Sirket.Trim();
                var mevcud = await _unitOfWork.Repository<IsciHYS>()
                    .Query()
                    .AnyAsync(x =>
                        !x.Silinib &&
                        x.Id != id &&
                        x.IsciId == model.IsciId &&
                        x.Sirket == sirketNorm &&
                        x.BaslamaTarixi <= (model.BitmeTarixi ?? DateTime.MaxValue) &&
                        (x.BitmeTarixi ?? DateTime.MaxValue) >= model.BaslamaTarixi);
                if (mevcud)
                    ModelState.AddModelError(nameof(model.Sirket),
                        sirketNorm == null
                            ? "Bu işçi üçün şirkəti göstərilməyən başqa HYS qeydi ilə dövr kəsişir."
                            : $"Bu işçi üçün \"{sirketNorm}\" şirkətində seçilən dövrdə artıq HYS mövcuddur.");
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

            entity.Sirket = string.IsNullOrWhiteSpace(model.Sirket) ? null : model.Sirket.Trim();
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
            var result = await _unitOfWork.Repository<IsciHYS>().YumshakSilAsync(id);
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
                .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
                .ToListAsync();
        }
    }
}
