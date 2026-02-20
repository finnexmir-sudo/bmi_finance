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
        private readonly IVezifeService _vezifeService;

        public IsciController(IIsciService isciService,IDepartmentService departmentService,IVezifeService vezifeService)
        {
            _isciService = isciService;
            _departmentService = departmentService;
            _vezifeService = vezifeService;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _isciService.HamisiniGetirAsync();
            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartamentler()
        {
            var result = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
            if (result.Success)
            {
                return Json(result.Data?.Select(d => new { id = d.Id, ad = d.Ad }));
            }
            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> GetVezifeler(int departamentId)
        {
            // Artıq x.DepartamentId tanınacaq
            var result = await _vezifeService.HamisiniGetirAsync(x => x.DepartamentId == departamentId && !x.Silinib);

            return Json(result.Data?.Select(v => new { id = v.Id, ad = v.Ad }));
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
                SobeId = vm.DepartmentId,
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
