using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Policy = Configurations.PolicyNames.HR_View)]
    public class IsciController : Controller
    {
        private const int PageSize = 25;

        private readonly IIsciService _isciService;
        private readonly IDepartmentService _departmentService;
        private readonly IVezifeService _vezifeService;
        private readonly IIsciTeyinatService _teyinatService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public IsciController(
            IIsciService isciService,
            IDepartmentService departmentService,
            IVezifeService vezifeService,
            IIsciTeyinatService teyinatService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _isciService = isciService;
            _departmentService = departmentService;
            _vezifeService = vezifeService;
            _teyinatService = teyinatService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        // ─────────── INDEX (paginated) ───────────
        public async Task<IActionResult> Index(
            string tab = "aktiv",
            string? search = null,
            int page = 1)
        {
            if (page < 1) page = 1;
            if (tab != "cixmis") tab = "aktiv";

            var (items, total, aktiv, cixmis) =
                await _isciService.GetPagedAsync(tab, search, page, PageSize);

            var totalPages = total == 0 ? 1 : (int)Math.Ceiling((double)total / PageSize);
            if (page > totalPages) page = totalPages;

            var vm = new IsciIndexVM
            {
                Items       = items.ToList(),
                CurrentPage = page,
                TotalPages  = totalPages,
                TotalCount  = total,
                AktivCount  = aktiv,
                CixmisCount = cixmis,
                Tab         = tab,
                Search      = search
            };

            return View(vm);
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
                Iban = isci.BankHesabNo,
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

                // IBAN verilibsə yadda saxla
                if (!string.IsNullOrWhiteSpace(vm.Iban))
                {
                    var ibanResult = await _isciService.IbanYenileAsync(resultIsci.Data.Id, vm.Iban);
                    if (!ibanResult.Success)
                        TempData["Error"] = $"İşçi yaradıldı, lakin IBAN qeydə alınmadı: {ibanResult.Message}";
                }
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
                Status = isci.Status,
                Iban = isci.BankHesabNo
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

            // IBAN-ı ayrıca yenilə (IsciMaliye.BankHesabNo)
            var ibanResult = await _isciService.IbanYenileAsync(vm.Id, vm.Iban);
            if (!ibanResult.Success)
            {
                ModelState.AddModelError("Iban", ibanResult.Message ?? "IBAN yenilənmədi.");
                return View(vm);
            }

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
                BaslamaTarixi = DateTime.Today.ToString("yyyy-MM-dd")
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
            // Tarix string gəlir (input type=date → yyyy-MM-dd), invariant parse edilir —
            // az-Latn-AZ mədəniyyətində DateTime birbaşa bind olmadığı üçün.
            var tarixOk = DateTime.TryParseExact((vm.BaslamaTarixi ?? "").Trim(),
                new[] { "yyyy-MM-dd", "dd.MM.yyyy", "dd-MM-yyyy" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var baslamaTarixi);
            if (!tarixOk)
                ModelState.AddModelError(nameof(vm.BaslamaTarixi), "Başlama tarixi düzgün formatda deyil.");

            if (!ModelState.IsValid)
            {
                Serilog.Log.Warning("TeyinatDeyis POST validasiya xətası (IsciId={IsciId}, Tarix={Tarix}): {Xetalar}",
                    vm.IsciId, vm.BaslamaTarixi,
                    string.Join(" | ", ModelState.Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {string.Join("; ", x.Value!.Errors.Select(e => e.ErrorMessage))}")));
                await TeyinatFormunuBerpaEt(vm);
                return View(vm);
            }

            var result = await _isciService.TeyinatDeyisAsync(
                vm.IsciId,
                vm.YeniDepartamentId,
                vm.YeniVezifeId,
                baslamaTarixi);

            if (!result.Success)
            {
                Serilog.Log.Warning("TeyinatDeyis servis xətası (IsciId={IsciId}): {Mesaj}", vm.IsciId, result.Message);
                ModelState.AddModelError("", result.Message ?? "Təyinat dəyişikliyi zamanı xəta baş verdi.");
                await TeyinatFormunuBerpaEt(vm);
                return View(vm);
            }

            TempData["Success"] = "Təyinat uğurla dəyişdirildi.";
            return RedirectToAction(nameof(Detail), new { id = vm.IsciId });
        }

        // POST xəta yolunda formu TAM bərpa et — "Mövcud vəziyyət" (Kohne*) daxil,
        // yoxsa səhifə yalançı "Təyin edilməyib" göstərir (işçinin təyinatı yerindədir).
        private async Task TeyinatFormunuBerpaEt(TeyinatDeyisVM vm)
        {
            var isci = await _isciService.GetIsciDetailsAsync(vm.IsciId);
            if (isci != null)
            {
                vm.IsciTamAd = isci.TamAd;
                var aktivTeyinat = await _teyinatService.GetAktivTeyinatAsync(vm.IsciId);
                vm.KohneDepartament = aktivTeyinat.Success ? aktivTeyinat.Data?.DepartamentAd : isci.SobeAdi;
                vm.KohneVezife = aktivTeyinat.Success ? aktivTeyinat.Data?.VezifeAd : isci.VezifeAdi;
            }
            await ReloadTeyinatLists(vm);
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
                VezifeId = aktivTeyinat.Success && aktivTeyinat.Data != null ? aktivTeyinat.Data.VezifeId : 0,
                BitmeTarixi = aktivTeyinat.Success && aktivTeyinat.Data != null ? aktivTeyinat.Data.BitmeTarixi : null
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
                vm.VezifeId,
                vm.BitmeTarixi);

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

            if (vm.YeniMaas == vm.KohneMaas)
            {
                ModelState.AddModelError("YeniMaas", "Yeni maaş cari maaşla eyni ola bilməz.");
                return View(vm);
            }

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

        // ─────────── MAAS TARİXÇƌSİ REDAKTƌ GET ───────────
        [HttpGet]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> MaasTarixcesiRedakte(int id)
        {
            var tarixce = await _unitOfWork.Repository<IsciMaasTarixcesi>()
                .IdIleGetirAsync(id);
            if (tarixce == null)
            {
                TempData["Error"] = "Qeyd tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var isciDetail = await _isciService.GetIsciDetailsAsync(tarixce.IsciId);

            var enSon = await _unitOfWork.Repository<IsciMaasTarixcesi>()
                .SorguHazirla(x => x.IsciId == tarixce.IsciId, izlemeden: true)
                .OrderByDescending(x => x.DeyismeTarixi)
                .FirstOrDefaultAsync();

            var vm = new MaasTarixcesiRedakteVM
            {
                Id            = tarixce.Id,
                IsciId        = tarixce.IsciId,
                IsciTamAd     = isciDetail?.TamAd,
                KohneMaas     = tarixce.KohneMaas,
                YeniMaas      = tarixce.YeniMaas,
                EmrNomresi    = tarixce.EmrinNomresi,
                DeyismeTarixi = tarixce.DeyismeTarixi,
                EnSonQeyddirmi = enSon?.Id == tarixce.Id
            };
            return View(vm);
        }

        // ─────────── MAAS TARİXÇƌSİ REDAKTƌ POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Configurations.PolicyNames.HR_Full)]
        public async Task<IActionResult> MaasTarixcesiRedakte(MaasTarixcesiRedakteVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var tarixce = await _unitOfWork.Repository<IsciMaasTarixcesi>()
                .IdIleGetirAsync(vm.Id);
            if (tarixce == null)
            {
                TempData["Error"] = "Qeyd tapılmadı.";
                return RedirectToAction(nameof(Detail), new { id = vm.IsciId });
            }

            tarixce.YeniMaas      = vm.YeniMaas;
            tarixce.EmrinNomresi  = vm.EmrNomresi;
            tarixce.YenilenmeTarixi = DateTime.Now;
            await _unitOfWork.Repository<IsciMaasTarixcesi>().YenileAsync(tarixce);

            if (vm.EnSonQeyddirmi)
            {
                var maliye = await _unitOfWork.Repository<IsciMaliye>()
                    .GetirAsync(x => x.IsciId == vm.IsciId);
                if (maliye != null)
                {
                    maliye.CariMaas = vm.YeniMaas;
                    maliye.YenilenmeTarixi = DateTime.Now;
                    await _unitOfWork.Repository<IsciMaliye>().YenileAsync(maliye);
                }
            }

            await _unitOfWork.YaddaSaxlaAsync();
            TempData["Success"] = "Maaş qeydi uğurla yeniləndi.";
            return RedirectToAction(nameof(Detail), new { id = vm.IsciId });
        }

        // ─────────── İşdən Çıxarma ───────────

        [HttpPost]
        public async Task<IActionResult> IsdenCixar([FromBody] IsdenCixarRequest req)
        {
            if (req == null || req.IsciId <= 0)
                return Json(new { success = false, message = "Məlumat natamamdır." });
            if (string.IsNullOrWhiteSpace(req.Sebeb))
                return Json(new { success = false, message = "Çıxma səbəbi daxil edilməlidir." });
            if (req.Tarix == default)
                return Json(new { success = false, message = "İşdən ayrılma tarixi daxil edilməlidir." });

            var isci = await _unitOfWork.Repository<Isci>()
                .Query().FirstOrDefaultAsync(x => x.Id == req.IsciId);
            if (isci == null)
                return Json(new { success = false, message = "İşçi tapılmadı." });
            if (isci.Status == IsciStatus.IshtenCixib)
                return Json(new { success = false, message = "İşçi artıq işdən çıxmış statusundadır." });

            isci.Status = IsciStatus.IshtenCixib;
            isci.IsdenAyrilmaTarixi = req.Tarix.Date;
            isci.FesihSebebi = req.Sebeb.Trim();
            await _unitOfWork.Repository<Isci>().YenileAsync(isci);
            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "İşçi işdən çıxarıldı." });
        }

        [HttpPost]
        public async Task<IActionResult> IseyiGeri([FromBody] int isciId)
        {
            var isci = await _unitOfWork.Repository<Isci>()
                .Query().FirstOrDefaultAsync(x => x.Id == isciId);
            if (isci == null)
                return Json(new { success = false, message = "İşçi tapılmadı." });

            isci.Status = IsciStatus.Aktiv;
            isci.IsdenAyrilmaTarixi = null;
            isci.FesihSebebi = null;
            await _unitOfWork.Repository<Isci>().YenileAsync(isci);
            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "İşçi yenidən aktiv edildi." });
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

        // ─────────── Request Models ───────────

        public class IsdenCixarRequest
        {
            public int IsciId { get; set; }
            public DateTime Tarix { get; set; }
            public string Sebeb { get; set; } = null!;
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
