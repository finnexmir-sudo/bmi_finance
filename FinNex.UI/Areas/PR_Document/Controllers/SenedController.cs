using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.Interfaces.PR_Document;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.PR_Document.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.PR_Document.Controllers
{
    [Authorize]
    [Area("PR_Document")]
    public class SenedController : Controller
    {
        private readonly ISenedIdareetmeService _senedService;
        private readonly IUnitOfWork _uow;

        public SenedController(ISenedIdareetmeService senedService, IUnitOfWork uow)
        {
            _senedService = senedService;
            _uow = uow;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _senedService.DashboardGetirAsync();
            var vm = new SenedDashboardVM
            {
                UmumiSened = data.UmumiSened,
                YeniSened = data.YeniSened,
                TesdiqSened = data.TesdiqSened,
                ArxivSened = data.ArxivSened,
                SonSenedler = data.SonSenedler
            };
            return View(vm);
        }

        public async Task<IActionResult> Yukle()
        {
            var vm = new SenedYukleVM
            {
                Sobeler = (await _uow.Repository<Sobe>().HamisiniGetirAsync()).ToList(),
                SenedNovleri = (await _uow.Repository<SenedNovu>().HamisiniGetirAsync()).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yukle(SenedYukleVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Sobeler = (await _uow.Repository<Sobe>().HamisiniGetirAsync()).ToList();
                vm.SenedNovleri = (await _uow.Repository<SenedNovu>().HamisiniGetirAsync()).ToList();
                return View(vm);
            }

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(vm.Fayl.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("Fayl", "Yalnız PDF, JPG və PNG faylları yüklənə bilər.");
                vm.Sobeler = (await _uow.Repository<Sobe>().HamisiniGetirAsync()).ToList();
                vm.SenedNovleri = (await _uow.Repository<SenedNovu>().HamisiniGetirAsync()).ToList();
                return View(vm);
            }

            const long maxSize = 10 * 1024 * 1024; // 10 MB
            if (vm.Fayl.Length > maxSize)
            {
                ModelState.AddModelError("Fayl", "Fayl ölçüsü 10 MB-dan çox ola bilməz.");
                vm.Sobeler = (await _uow.Repository<Sobe>().HamisiniGetirAsync()).ToList();
                vm.SenedNovleri = (await _uow.Repository<SenedNovu>().HamisiniGetirAsync()).ToList();
                return View(vm);
            }

            var dto = new SenedUploadDto
            {
                SobeId = vm.SobeId,
                SenedNovuId = vm.SenedNovuId,
                Basliq = vm.Basliq,
                AcarSoz = vm.AcarSoz,
                Fayl = vm.Fayl
            };

            var senedId = await _senedService.YukleAsync(dto);
            return RedirectToAction(nameof(Detay), new { id = senedId });
        }

        public async Task<IActionResult> Siyahi(
            int? sobeId, int? senedNovuId, int? status,
            string? acarSoz, int sehife = 1)
        {
            const int sehifeOlcusu = 10;

            var senedler = await _senedService.SiyahiGetirAsync(
                sobeId, senedNovuId, status, acarSoz, sehife, sehifeOlcusu);

            var umumiSay = await _senedService.SayGetirAsync(
                sobeId, senedNovuId, status, acarSoz);

            var vm = new SenedSiyahiVM
            {
                Senedler = senedler.ToList(),
                SobeId = sobeId,
                SenedNovuId = senedNovuId,
                Status = status,
                AcarSoz = acarSoz,
                Sehife = sehife,
                SehifeOlcusu = sehifeOlcusu,
                UmumiSay = umumiSay,
                Sobeler = (await _uow.Repository<Sobe>().HamisiniGetirAsync()).ToList(),
                SenedNovleri = (await _uow.Repository<SenedNovu>().HamisiniGetirAsync()).ToList()
            };

            return View(vm);
        }

        public async Task<IActionResult> Detay(int id)
        {
            var sened = await _senedService.DetayGetirAsync(id);
            if (sened == null)
                return NotFound();

            return View(sened);
        }

        public async Task<IActionResult> FaylYukle(int id)
        {
            var result = await _senedService.FaylYukleAsync(id);
            if (result == null)
                return NotFound();

            return File(result.Value.bytes, result.Value.contentType, result.Value.fileName);
        }

        [HttpGet]
        public async Task<IActionResult> SenedNovleriGetir(int sobeId)
        {
            var novler = await _uow.Repository<SenedNovu>()
                .HamisiniGetirAsync(predicate: n => n.SobeId == sobeId && n.Aktiv);
            return Json(novler.Select(n => new { n.Id, n.Ad }));
        }
    }
}
