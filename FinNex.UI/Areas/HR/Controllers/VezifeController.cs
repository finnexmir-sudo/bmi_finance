// Areas/HR/Controllers/VezifeController.cs
using FinNex.Domain;
using FinNex.Application.DTOs.HR.Vezife;
using FinNex.Application.Interfaces.Structur;
using FinNex.UI.Areas.HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class VezifeController : Controller
    {
        private readonly IVezifeService _vezifeService;
        private readonly IDepartmentService _departmentService;

        public VezifeController(IVezifeService vezifeService, IDepartmentService departmentService)
        {
            _vezifeService = vezifeService;
            _departmentService = departmentService;
        }

        // ── GET: /HR/Vezife ──────────────────────────────
        public async Task<IActionResult> Index()
        {
            var result = await _vezifeService.HamisiniGetirAsync();

            var deptResult = await _departmentService.HamisiniGetirAsync();
            ViewBag.Departamentlar = deptResult.Success && deptResult.Data != null
                ? deptResult.Data.Select(d => new SelectListItem(d.Ad, d.Id.ToString())).ToList()
                : new List<SelectListItem>();

            ViewData["Title"] = "Vəzifələr";
            return View(result.Success ? result.Data : new List<VezifeListDto>());
        }

        // ── GET: /HR/Vezife/Create ───────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = await BuildCreateVM();
            ViewData["Title"] = "Yeni Vəzifə";
            return View(vm);
        }

        // ── POST: /HR/Vezife/Create ──────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VezifeCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                await FillDepartamentlar(vm);
                return View(vm);
            }

            var dto = new VezifeCreateDto
            {
                Ad = vm.Ad,
                DepartamentId = vm.DepartamentId,
                Tesvir = vm.Tesvir,
                EsasMezuniyyetGunu = vm.EsasMezuniyyetGunu
            };

            var result = await _vezifeService.YaratAsync(dto);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message ?? "boş");
                await FillDepartamentlar(vm);
                return View(vm);
                //// Bərpa təklifi — Create view-unda göstər
                //if (result.Message?.StartsWith("BERPA_TELEB:") == true)
                //{
                //    var parts = result.Message.Split(':', 3);
                //    TempData["BerpaId"] = int.Parse(parts[1]);
                //    TempData["BerpaMsg"] = parts[2];
                //    return RedirectToAction(nameof(Create)); // ← Create-ə qayıt
                //}

                //ModelState.AddModelError("", result.Message ?? "Xəta baş verdi");
                //await FillDepartamentlar(vm);
                //return View(vm);
            }

            TempData["Success"] = "Vəzifə uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST: /HR/Vezife/BerpaEt ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BerpaEt(int id)
        {
            var result = await _vezifeService.BerpaEtAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── POST: /HR/Vezife/Edit ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string Ad, int DepartamentId, string? Tesvir, bool Aktivdir, int EsasMezuniyyetGunu = 21, string? YonlukHal = null)
        {
            if (string.IsNullOrWhiteSpace(Ad))
            {
                TempData["Error"] = "Vəzifə adı boş ola bilməz.";
                return RedirectToAction(nameof(Index));
            }

            var dto = new VezifeUpdateDto
            {
                Id = id,
                Ad = Ad.Trim(),
                DepartamentId = DepartamentId,
                Tesvir = Tesvir?.Trim(),
                Aktivdir = Aktivdir,
                EsasMezuniyyetGunu = EsasMezuniyyetGunu == 30 ? 30 : 21,
                YonlukHal = string.IsNullOrWhiteSpace(YonlukHal) ? null : YonlukHal.Trim()
            };

            var result = await _vezifeService.YenileAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── POST: /HR/Vezife/Delete ──────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var listResult = await _vezifeService.HamisiniGetirAsync();
            if (listResult.Success && listResult.Data != null)
            {
                var vezife = listResult.Data.FirstOrDefault(x => x.Id == id);
                if (vezife != null && vezife.IsciSayi > 0)
                {
                    TempData["Error"] = $"\"{vezife.Ad}\" vəzifəsinə bağlı {vezife.IsciSayi} işçi var. Əvvəlcə işçiləri köçürün.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var result = await _vezifeService.SilAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── Köməkçi metodlar ─────────────────────────────
        private async Task<VezifeCreateVM> BuildCreateVM()
        {
            var vm = new VezifeCreateVM();
            await FillDepartamentlar(vm);
            return vm;
        }

        private async Task FillDepartamentlar(VezifeCreateVM vm)
        {
            var deptResult = await _departmentService.HamisiniGetirAsync();
            vm.Departamentlar = deptResult.Success && deptResult.Data != null
                ? deptResult.Data.Select(d => new SelectListItem(d.Ad, d.Id.ToString())).ToList()
                : new List<SelectListItem>();
        }
    }
}