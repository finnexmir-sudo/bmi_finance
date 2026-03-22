using FinNex.Application.DTOs.HR.Vezife;
using FinNex.Application.Interfaces.Structur;
using FinNex.Application.Services.Structur;
using FinNex.UI.Areas.HR.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class VezifeController : Controller
    {
        private readonly IVezifeService _vezifeService;
        private readonly IDepartmentService _departmentService;

        public VezifeController(IVezifeService vezifeService, IDepartmentService departmentService)
        {
            _vezifeService = vezifeService;
            _departmentService = departmentService;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _vezifeService.HamisiniGetirAsync();

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(new List<VezifeListDto>());
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var result = await _departmentService.HamisiniGetirAsync();

            var vm = new VezifeCreateVM
            {
                Departamentlar = result.Success && result.Data != null
                    ? result.Data.Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Ad
                    }).ToList()
                    : new List<SelectListItem>()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VezifeCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                var deptResult = await _departmentService.HamisiniGetirAsync();
                vm.Departamentlar = deptResult.Success && deptResult.Data != null
                    ? deptResult.Data.Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Ad
                    }).ToList()
                    : new List<SelectListItem>();

                return View(vm);
            }

            var dto = new VezifeCreateDto
            {
                Ad = vm.Ad,
                DepartamentId = vm.DepartamentId,
                Tesvir = vm.Tesvir,
            };

            var result = await _vezifeService.YaratAsync(dto);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;

                var deptResult = await _departmentService.HamisiniGetirAsync();
                vm.Departamentlar = deptResult.Success && deptResult.Data != null
                    ? deptResult.Data.Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Ad
                    }).ToList()
                    : new List<SelectListItem>();

                return View(vm);
            }

            TempData["Success"] = "Vəzifə uğurla əlavə edildi!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _vezifeService.SilAsync(id);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true });
        }
    }
}
