using DocumentFormat.OpenXml.Spreadsheet;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<AppUser> _userManager;

        public IsciController(IIsciService isciService, IDepartmentService departmentService, IVezifeService vezifeService, UserManager<AppUser> userManager)
        {
            _isciService = isciService;
            _departmentService = departmentService;
            _vezifeService = vezifeService;
            _userManager = userManager;
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
        [HttpGet]
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
            var userYoxla = await _userManager.FindByNameAsync(vm.IstifadeciAd);
            if (userYoxla != null)
            {
                ModelState.AddModelError("IstifadeciAd", "Bu istifadəçi adı artıq mövcuddur");
                await ReloadDepartments(vm);
                return View(vm);
            }

            var user = new AppUser
            {
                UserName = vm.IstifadeciAd, // 🔥 LOGIN BUNUNLA OLACAQ
                Ad = vm.Ad,
                Soyad = vm.Soyad,
                Email = vm.Email,
                EmailConfirmed = true,
                Aktivdir = true,
                
            };

            var resultUser = await _userManager.CreateAsync(user, "user123");

            if (!resultUser.Succeeded)
            {
                foreach (var error in resultUser.Errors)
                    ModelState.AddModelError("", error.Description);
                await ReloadDepartments(vm);
                return View(vm);
            }

            var dto = new IsciCreateDto
            {
                Ad = vm.Ad,
                Soyad = vm.Soyad,
                AtaAdi = vm.AtaAdi ?? "",
                Email = vm.Email,
                VezifeId = vm.VezifeId,
                Telefon = vm.Telefon,
                DepartamentId = vm.DepartamentId,
                IsheBaslamaTarixi = vm.IseQebulTarixi,
                FIN = vm.FIN,
                Unvan = vm.Unvan,
                SeriyaNomre = vm.SeriyaNomre,
                DogumTarixi = vm.DogumTarixi,
                Cins = (Cins)vm.CinsId,
                UserId=vm.UserId
            };

            var resultIsci = await _isciService.YaratAsync(dto);

            if (!resultIsci.Success)
            {
                // Əgər işçi yaradıla bilməsə, yuxarıda yaranan user-i silmək olar (Rollback)
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError("", resultIsci.Message ?? "İşçi yaradıla bilmədi");
                return View(vm);
            }

            // 5. 🔥 USER-I YENİDƏN UPDATE EDİRİK (IsciId-ni mənimsətmək üçün)
            if (resultIsci.Data != null)
            {
                user.IsciId = resultIsci.Data.Id; // 🔥 resultIsci.Data yox, resultIsci.Data.Id yazılmalıdır
                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = "İşçi və İstifadəçi uğurla yaradıldı.";
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
