using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.Interfaces.Structur;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class IsciController : Controller
    {
        private readonly IIsciService _isciService;
        private readonly IDepartmentService _departmentService;

        public IsciController(IIsciService isciService,IDepartmentService departmentService)
        {
            _isciService = isciService;
            _departmentService = departmentService;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _isciService.HamisiniGetirAsync();
            return View(result.Data);
        }
        // GET
        public async Task<IActionResult> Create()
        {
            var result = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);

            var vm = new IsciCreateVM();

            if (result.Success && result.Data != null)
            {
                vm.Departments = result.Data
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Ad
                    }).ToList();
            }

            return View(vm);
        }


        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IsciCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                await ReloadDepartments(vm);
                return View(vm);
            }

            var dto = new IsciCreateDto
            {
                Ad = vm.Ad,
                Soyad = vm.Soyad,
                Email = vm.Email,
                Telefon = vm.Telefon,
                DepartamentId = vm.DepartmentId,
                IsheBaslamaTarixi = vm.IseQebulTarixi
            };

            var result = await _isciService.YaratAsync(dto);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message ?? "Xəta baş verdi");
                await ReloadDepartments(vm);
                return View(vm);
            }

            TempData["Success"] = "İşçi uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ReloadDepartments(IsciCreateVM vm)
        {
            var result = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);

            if (result.Success && result.Data != null)
            {
                vm.Departments = result.Data
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Ad
                    }).ToList();
            }
        }

    }

}
