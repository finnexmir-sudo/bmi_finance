using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Policy = Configurations.PolicyNames.HR_View)]
    public class IsciController : Controller
    {
        private readonly IIsciService _isciService;
        private readonly IDepartmentService _departmentService;
        private readonly IVezifeService _vezifeService;
        private readonly IIsciTeyinatService _teyinatService;
        private readonly UserManager<AppUser> _userManager;

        public IsciController(
            IIsciService isciService,
            IDepartmentService departmentService,
            IVezifeService vezifeService,
            IIsciTeyinatService teyinatService,
            UserManager<AppUser> userManager)
        {
            _isciService = isciService;
            _departmentService = departmentService;
            _vezifeService = vezifeService;
            _teyinatService = teyinatService;
            _userManager = userManager;
        }

        // ─────────── INDEX ───────────
        public async Task<IActionResult> Index()
        {
            var result = await _isciService.HamisiniGetirAsync();
            return View(result.Data ?? new List<IsciListDto>());
        }

        // ─────────── DETAIL ───────────
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var isci = await _isciService.GetIsciDetailsAsync(id);
            if (isci == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var teyinatResult = await _teyinatService.GetByIsciIdAsync(id);
            var maasResult = await _isciService.GetMaasTarixcesiAsync(id);
            var aktivTeyinat = await _teyinatService.GetAktivTeyinatAsync(id);

            var cariMaas = await _isciService.GetCariMaasAsync(id);

            var vm = new IsciDetailVM
            {
                Id = isci.Id,
                TamAd = isci.TamAd,
                Ad = isci.Ad,
                Soyad = isci.Soyad,
                AtaAdi = isci.AtaAdi,
                FIN = isci.FIN,
                SeriyaNomre = isci.SeriyaNomre,
                DogumTarixi = isci.DogumTarixi,
                CinsAd = isci.Cins == Cins.Kisi ? "Kişi" : "Qadın",
                Telefon = isci.Telefon,
                Email = isci.Email,
                Unvan = isci.Unvan,
                IsheQebulTarixi = isci.IsheQebulTarixi,
                IsdenAyrilmaTarixi = isci.IsdenAyrilmaTarixi,
                Status = isci.Status,
                LoginVar = isci.LoginVar,
                IstifadeciAd = _userManager.Users.FirstOrDefault(u => u.IsciId == id)?.UserName,
                CariDepartament = aktivTeyinat.Success ? aktivTeyinat.Data?.DepartamentAd : isci.SobeAdi,
                CariVezife = aktivTeyinat.Success ? aktivTeyinat.Data?.VezifeAd : isci.VezifeAdi,
                CariMaas = cariMaas,
                TeyinatTarixcesi = teyinatResult.Success ? teyinatResult.Data ?? new List<FinNex.Application.DTOs.HR.IsciTeyinat.IsciTeyinatDto>() : new List<FinNex.Application.DTOs.HR.IsciTeyinat.IsciTeyinatDto>(),
                MaasTarixcesi = maasResult.Success ? maasResult.Data ?? new List<IsciMaasTarixcesiDto>() : new List<IsciMaasTarixcesiDto>()
            };

            return View(vm);
        }

        // ─────────── CREATE GET ───────────
        [HttpGet]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> Create()
        {
            var vm = new IsciCreateVM();
            await ReloadDepartments(vm);
            return View(vm);
        }

        // ─────────── CREATE POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
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

            var finYoxla = await _isciService.CheckFinExistsAsync(vm.FIN);
            if (finYoxla)
            {
                ModelState.AddModelError("FIN", "Bu FIN artıq mövcuddur");
                await ReloadDepartments(vm);
                return View(vm);
            }

            var user = new AppUser
            {
                UserName = vm.IstifadeciAd,
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

            // Operator rolunu əlavə et (default rol)
            await _userManager.AddToRoleAsync(user, RoleNames.Operator);

            var dto = new IsciCreateDto
            {
                Ad = vm.Ad,
                Soyad = vm.Soyad,
                AtaAdi = vm.AtaAdi ?? "",
                Email = vm.Email,
                VezifeId = vm.VezifeId,
                Telefon = vm.Telefon,
                DepartamentId = vm.DepartamentId,
                IsheQebulTarixi = vm.IseQebulTarixi,
                FIN = vm.FIN,
                Unvan = vm.Unvan,
                SeriyaNomre = vm.SeriyaNomre,
                DogumTarixi = vm.DogumTarixi,
                Cins = (Cins)vm.CinsId,
                UserId = user.Id,
                BaslangicMaas = vm.BaslangicMaas,
                BaslangicMezuniyyet = vm.BaslangicMezuniyyet
            };

            var resultIsci = await _isciService.YaratAsync(dto);

            if (!resultIsci.Success)
            {
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError("", resultIsci.Message ?? "İşçi yaradıla bilmədi");
                await ReloadDepartments(vm);
                return View(vm);
            }

            if (resultIsci.Data != null)
            {
                user.IsciId = resultIsci.Data.Id;
                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = "İşçi və İstifadəçi uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }

        // ─────────── EDIT GET ───────────
        [HttpGet]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> Edit(int id)
        {
            var isci = await _isciService.GetIsciDetailsAsync(id);
            if (isci == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var appUser = _userManager.Users.FirstOrDefault(u => u.IsciId == id);

            var vm = new IsciEditVM
            {
                Id = isci.Id,
                AppUserId = appUser?.Id,
                IstifadeciAd = appUser?.UserName,
                Ad = isci.Ad,
                Soyad = isci.Soyad,
                AtaAdi = isci.AtaAdi,
                FIN = isci.FIN,
                SeriyaNomre = isci.SeriyaNomre,
                DogumTarixi = isci.DogumTarixi,
                CinsId = (int)isci.Cins,
                Telefon = isci.Telefon,
                Email = isci.Email,
                Unvan = isci.Unvan,
                IsheQebulTarixi = isci.IsheQebulTarixi,
                IsdenAyrilmaTarixi = isci.IsdenAyrilmaTarixi,
                Status = isci.Status
            };

            return View(vm);
        }

        // ─────────── EDIT POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> Edit(IsciEditVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var dto = new IsciUpdateDto
            {
                Id = vm.Id,
                UserId = vm.AppUserId,
                Ad = vm.Ad,
                Soyad = vm.Soyad,
                AtaAdi = vm.AtaAdi,
                FIN = vm.FIN,
                SeriyaNomre = vm.SeriyaNomre,
                DogumTarixi = vm.DogumTarixi,
                Cins = (Cins)vm.CinsId,
                Telefon = vm.Telefon,
                Email = vm.Email,
                Unvan = vm.Unvan,
                IsheQebulTarixi = vm.IsheQebulTarixi,
                IsdenAyrilmaTarixi = vm.IsdenAyrilmaTarixi,
                Status = vm.Status
            };

            var result = await _isciService.YenileAsync(dto);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message ?? "Yeniləmə zamanı xəta baş verdi.");
                return View(vm);
            }

            // AppUser-dəki Ad/Soyad/Email/UserName-i də yenilə
            var users = _userManager.Users.Where(u => u.IsciId == vm.Id).ToList();
            foreach (var appUser in users)
            {
                appUser.Ad = vm.Ad;
                appUser.Soyad = vm.Soyad;
                if (!string.IsNullOrEmpty(vm.Email))
                    appUser.Email = vm.Email;
                if (!string.IsNullOrEmpty(vm.IstifadeciAd) && appUser.UserName != vm.IstifadeciAd)
                {
                    var existingUser = await _userManager.FindByNameAsync(vm.IstifadeciAd);
                    if (existingUser != null && existingUser.Id != appUser.Id)
                    {
                        ModelState.AddModelError("IstifadeciAd", "Bu istifadəçi adı artıq mövcuddur");
                        return View(vm);
                    }
                    appUser.UserName = vm.IstifadeciAd;
                    appUser.NormalizedUserName = vm.IstifadeciAd.ToUpperInvariant();
                }
                await _userManager.UpdateAsync(appUser);
            }

            TempData["Success"] = "İşçi məlumatları uğurla yeniləndi.";
            return RedirectToAction(nameof(Detail), new { id = vm.Id });
        }

        // ─────────── TEYINAT DEYIS GET ───────────
        [HttpGet]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> TeyinatDeyis(int id)
        {
            var isci = await _isciService.GetIsciDetailsAsync(id);
            if (isci == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var aktivTeyinat = await _teyinatService.GetAktivTeyinatAsync(id);

            var vm = new TeyinatDeyisVM
            {
                IsciId = id,
                IsciTamAd = isci.TamAd,
                KohneDepartament = aktivTeyinat.Success ? aktivTeyinat.Data?.DepartamentAd : isci.SobeAdi,
                KohneVezife = aktivTeyinat.Success ? aktivTeyinat.Data?.VezifeAd : isci.VezifeAdi,
                BaslamaTarixi = DateTime.Today
            };

            await ReloadTeyinatLists(vm);
            return View(vm);
        }

        // ─────────── TEYINAT DEYIS POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> TeyinatDeyis(TeyinatDeyisVM vm)
        {
            if (!ModelState.IsValid)
            {
                var isciCheck = await _isciService.GetIsciDetailsAsync(vm.IsciId);
                if (isciCheck != null) vm.IsciTamAd = isciCheck.TamAd;
                await ReloadTeyinatLists(vm);
                return View(vm);
            }

            var result = await _isciService.TeyinatDeyisAsync(
                vm.IsciId,
                vm.YeniDepartamentId,
                vm.YeniVezifeId,
                vm.BaslamaTarixi);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message ?? "Təyinat dəyişikliyi zamanı xəta baş verdi.");
                var isciCheck = await _isciService.GetIsciDetailsAsync(vm.IsciId);
                if (isciCheck != null) vm.IsciTamAd = isciCheck.TamAd;
                await ReloadTeyinatLists(vm);
                return View(vm);
            }

            TempData["Success"] = "Təyinat uğurla dəyişdirildi.";
            return RedirectToAction(nameof(Detail), new { id = vm.IsciId });
        }

        // ─────────── TEYINAT REDAKTE GET ───────────
        [HttpGet]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> TeyinatRedakte(int id)
        {
            var isci = await _isciService.GetIsciDetailsAsync(id);
            if (isci == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var aktivTeyinat = await _teyinatService.GetAktivTeyinatAsync(id);

            var vm = new TeyinatRedakteVM
            {
                IsciId = id,
                IsciTamAd = isci.TamAd,
                DepartamentId = aktivTeyinat.Success && aktivTeyinat.Data != null ? aktivTeyinat.Data.DepartamentId : 0,
                VezifeId = aktivTeyinat.Success && aktivTeyinat.Data != null ? aktivTeyinat.Data.VezifeId : 0
            };

            await ReloadTeyinatRedakteLists(vm);
            return View(vm);
        }

        // ─────────── TEYINAT REDAKTE POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> TeyinatRedakte(TeyinatRedakteVM vm)
        {
            if (!ModelState.IsValid)
            {
                var isciCheck = await _isciService.GetIsciDetailsAsync(vm.IsciId);
                if (isciCheck != null) vm.IsciTamAd = isciCheck.TamAd;
                await ReloadTeyinatRedakteLists(vm);
                return View(vm);
            }

            var result = await _isciService.TeyinatRedakteEtAsync(
                vm.IsciId,
                vm.DepartamentId,
                vm.VezifeId);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message ?? "Redaktə zamanı xəta baş verdi.");
                var isciCheck = await _isciService.GetIsciDetailsAsync(vm.IsciId);
                if (isciCheck != null) vm.IsciTamAd = isciCheck.TamAd;
                await ReloadTeyinatRedakteLists(vm);
                return View(vm);
            }

            TempData["Success"] = "Təyinat uğurla redaktə edildi.";
            return RedirectToAction(nameof(Detail), new { id = vm.IsciId });
        }

        // ─────────── MAAS DEYIS GET ───────────
        [HttpGet]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> MaasDeyis(int id)
        {
            var isciDetail = await _isciService.GetIsciDetailsAsync(id);

            if (isciDetail == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var kohneMaas = await _isciService.GetCariMaasAsync(id);

            var vm = new MaasDeyisVM
            {
                IsciId = id,
                IsciTamAd = isciDetail.TamAd,
                KohneMaas = kohneMaas
            };

            return View(vm);
        }

        // ─────────── MAAS DEYIS POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> MaasDeyis(MaasDeyisVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var result = await _isciService.UpdateSalaryWithHistoryAsync(
                vm.IsciId,
                vm.YeniMaas,
                vm.EmrNomresi);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message ?? "Maaş dəyişikliyi zamanı xəta baş verdi.");
                return View(vm);
            }

            TempData["Success"] = "Maaş uğurla yeniləndi.";
            return RedirectToAction(nameof(Detail), new { id = vm.IsciId });
        }

        // ─────────── JSON Endpoints ───────────
        [HttpGet]
        public async Task<IActionResult> GetDepartamentler()
        {
            var result = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
            if (result.Success)
                return Json(result.Data?.Select(d => new { id = d.Id, ad = d.Ad }));
            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> GetVezifeler(int departamentId)
        {
            var result = await _vezifeService.HamisiniGetirAsync();
            if (result.Success && result.Data != null)
            {
                var filtered = result.Data
                    .Where(v => v.DepartamentId == departamentId && v.IsActive)
                    .Select(v => new { id = v.Id, ad = v.Ad });
                return Json(filtered);
            }
            return Json(Array.Empty<object>());
        }

        // ─────────── Helpers ───────────
        private async Task ReloadDepartments(IsciCreateVM vm)
        {
            var result = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
            if (result.Success && result.Data != null)
            {
                vm.Departments = result.Data
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Ad })
                    .ToList();
            }
        }

        private async Task ReloadTeyinatLists(TeyinatDeyisVM vm)
        {
            var deptResult = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
            if (deptResult.Success && deptResult.Data != null)
            {
                vm.Departamentler = deptResult.Data
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Ad })
                    .ToList();
            }

            if (vm.YeniDepartamentId > 0)
            {
                var vezResult = await _vezifeService.HamisiniGetirAsync(
                    x => x.DepartamentId == vm.YeniDepartamentId && !x.Silinib);
                if (vezResult.Success && vezResult.Data != null)
                {
                    vm.Vezifeler = vezResult.Data
                        .Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Ad })
                        .ToList();
                }
            }
        }

        private async Task ReloadTeyinatRedakteLists(TeyinatRedakteVM vm)
        {
            var deptResult = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
            if (deptResult.Success && deptResult.Data != null)
            {
                vm.Departamentler = deptResult.Data
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Ad })
                    .ToList();
            }

            if (vm.DepartamentId > 0)
            {
                var vezResult = await _vezifeService.HamisiniGetirAsync(
                    x => x.DepartamentId == vm.DepartamentId && !x.Silinib);
                if (vezResult.Success && vezResult.Data != null)
                {
                    vm.Vezifeler = vezResult.Data
                        .Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Ad })
                        .ToList();
                }
            }
        }
    }
}
