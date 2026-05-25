using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.HR.ViewModels.Mezuniyyet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize]
    public class MezuniyyetController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IBildirisService _bildirisService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        private static readonly string[] _icazeSenedTipler =
            [".pdf", ".jpg", ".jpeg", ".png"];

        public MezuniyyetController(
            IMezuniyyetService mezuniyyetService,
            IIsciService isciService,
            UserManager<AppUser> userManager,
            IBildirisService bildirisService,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _mezuniyyetService = mezuniyyetService;
            _isciService = isciService;
            _userManager = userManager;
            _bildirisService = bildirisService;
            _unitOfWork = unitOfWork;
            _env = env;
            _config = config;
        }

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private async Task<int?> GetCurrentDepartamentIdAsync(int isciId)
        {
            var result = await _isciService.GetAktivDepartamentIdAsync(isciId);
            return result.Success ? result.Data : null;
        }

        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> Rehber()
        {
            var result = await _mezuniyyetService.GetRehberTesdiqindeAsync();
            var vm = new HrMezuniyyetIndexVM
            {
                Mezuniyyetler = result.Success ? result.Data!.ToList() : new(),
                PageTitle = "Rəhbər — Gözləyən Müraciətlər",
                TesdiqAction = "RehberTesdiq"
            };
            ViewData["Title"] = "Rəhbər Təsdiqi";
            return View("TesdiqIndex", vm);
        }

        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Hr(string tab = "tesdiq", string? axtaris = null)
        {
            var aktivTab = tab switch
            {
                "proses" => "proses",
                "tarixce" => "tarixce",
                _ => "tesdiq"
            };

            var tesdiqResult = await _mezuniyyetService.GetHrTesdiqindeAsync();
            var prosesResult = await _mezuniyyetService.GetProsesdeOlanlarAsync();
            var tarixceResult = await _mezuniyyetService.GetTarixceAsync(
                aktivTab == "tarixce" ? axtaris : null);

            var tesdiqList = tesdiqResult.Success ? tesdiqResult.Data!.ToList() : new();
            var prosesList = prosesResult.Success ? prosesResult.Data!.ToList() : new();
            var tarixceList = tarixceResult.Success ? tarixceResult.Data!.ToList() : new();

            int tarixceTotalSayi = tarixceList.Count;
            if (aktivTab == "tarixce" && !string.IsNullOrWhiteSpace(axtaris))
            {
                var hamisi = await _mezuniyyetService.GetTarixceAsync(null);
                tarixceTotalSayi = hamisi.Success ? hamisi.Data!.Count : tarixceList.Count;
            }

            var mezuniyyetler = aktivTab switch
            {
                "proses" => prosesList,
                "tarixce" => tarixceList,
                _ => tesdiqList
            };

            var pageTitle = aktivTab switch
            {
                "proses" => "Prosesdə olan müraciətlər",
                "tarixce" => "Təsdiq tarixçəsi",
                _ => "HR — Son Təsdiq"
            };

            var vm = new HrMezuniyyetIndexVM
            {
                Mezuniyyetler = mezuniyyetler,
                PageTitle = pageTitle,
                TesdiqAction = aktivTab == "tesdiq" ? "HrTesdiq" : "",
                AktivTab = aktivTab,
                TesdiqSayi = tesdiqList.Count,
                ProsesSayi = prosesList.Count,
                TarixceSayi = tarixceTotalSayi,
                Axtaris = axtaris
            };

            ViewData["Title"] = aktivTab switch
            {
                "proses" => "HR — İzləmə",
                "tarixce" => "HR — Tarixçə",
                _ => "HR Təsdiqi"
            };
            return View("TesdiqIndex", vm);
        }

        [Authorize(Roles = RoleNames.SobeReisi + "," + RoleNames.Rehber + "," + RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Detal(int id, string returnAction = "Hr")
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success || result.Data == null) return NotFound();

            var overlap = await _mezuniyyetService.GetOverlapMezuniyyetlerAsync(id);
            var konflikt = await _mezuniyyetService.GetEvezediciKonfliktiAsync(id);

            var vm = new HrMezuniyyetDetalVM
            {
                Mezuniyyet = result.Data,
                ReturnAction = returnAction,
                OverlapMezuniyyetler = overlap.Success ? overlap.Data!.ToList() : new(),
                EvezediciKonfliktleri = konflikt.Success ? konflikt.Data!.ToList() : new()
            };

            ViewData["Title"] = "Müraciət Detalı";
            return View("Detail", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> RehberTesdiq(int id, bool status, string? qeyd)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();
            var result = await _mezuniyyetService.RehberTesdiqAsync(id, status, qeyd, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Rehber));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> HrTesdiq(int id, bool status, string? qeyd)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();
            var result = await _mezuniyyetService.HrTesdiqAsync(id, status, qeyd, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Hr));
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> GeriyeQeyd()
        {
            var vm = new GeriyeQeydVM
            {
                Isciler = await GetAktivIsciSelectListAsync()
            };
            ViewData["Title"] = "Geriyə Məzuniyyət Qeyd et";
            return View(vm);
        }

        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Xeta"] = "Məzuniyyət tapılmadı.";
                return RedirectToAction("Index");
            }

            var m = result.Data;
            var editStatuses = new[]
            {
                MezuniyyetStatus.Gozlemede,
                MezuniyyetStatus.SobeReisiTesdiqinde,
                MezuniyyetStatus.RehberTesdiqinde,
                MezuniyyetStatus.HrTesdiqinde
            };

            if (!editStatuses.Contains(m.Status))
            {
                TempData["Xeta"] = "Yalnız prosesdə olan məzuniyyətlər redaktə edilə bilər.";
                return RedirectToAction("Index");
            }

            var dto = new MezuniyyetUpdateDto
            {
                Id             = m.Id,
                Nov            = m.Nov,
                BaslamaTarixi  = m.BaslamaTarixi,
                BitmeTarixi    = m.BitmeTarixi,
                EvezEdenIsciId = m.EvezEdenIsciId,
                Qeyd           = m.Qeyd,
                Status         = m.Status,
                ImtinaSebebi   = m.ImtinaSebebi
            };
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MezuniyyetUpdateDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var result = await _mezuniyyetService.YenileAsync(dto);
            if (!result.Success)
            {
                TempData["Xeta"] = result.Message;
                return View(dto);
            }
            TempData["Ugur"] = result.Message ?? "Məzuniyyət uğurla yeniləndi.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Aktiv(int qabaqcaGun = 30)
        {
            if (qabaqcaGun < 0) qabaqcaGun = 0;
            if (qabaqcaGun > 365) qabaqcaGun = 365;
            var result = await _mezuniyyetService.GetAktivVeYaxinlardakilarAsync(qabaqcaGun);
            var list = result.Success ? result.Data!.ToList() : new();
            ViewBag.QabaqcaGun = qabaqcaGun;
            ViewData["Title"] = "Aktiv Məzuniyyətlər";
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> GeriyeQeyd(GeriyeQeydVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Isciler = await GetAktivIsciSelectListAsync(vm.IsciId);
                return View(vm);
            }

            var hrIsciId = await GetCurrentIsciIdAsync();
            if (hrIsciId == null)
            {
                TempData["Error"] = "HR istifadəçisinin işçi qeydi tapılmadı.";
                vm.Isciler = await GetAktivIsciSelectListAsync(vm.IsciId);
                return View(vm);
            }

            var dto = new GeriyeMezuniyyetCreateDto
            {
                IsciId = vm.IsciId,
                Nov = vm.Nov,
                BaslamaTarixi = vm.BaslamaTarixi,
                BitmeTarixi = vm.BitmeTarixi,
                Sebeb = vm.Sebeb,
                EmrSuffiks = vm.EmrSuffiks,
                EmrRegem = vm.EmrRegem,
                EmrIl = vm.EmrIl
            };

            var result = await _mezuniyyetService.GeriyeQeydEtAsync(dto, hrIsciId.Value);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                vm.Isciler = await GetAktivIsciSelectListAsync(vm.IsciId);
                return View(vm);
            }

            TempData["Success"] = result.Message ?? "Geriyə qeyd uğurla rəsmiləşdirildi.";
            return RedirectToAction(nameof(Hr));
        }

        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetAktivIsciSelectListAsync(int? selected = null)
        {
            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv,
                izlemeden: true);

            if (!iscilerResult.Success || iscilerResult.Data == null)
                return new();

            return iscilerResult.Data
                .OrderBy(x => x.TamAd)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
                    x.TamAd, x.Id.ToString(), x.Id == selected))
                .ToList();
        }

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Index()
        {
            var result = await _mezuniyyetService.GetListAsync();
            var vm = new HrMezuniyyetIndexVM
            {
                Mezuniyyetler = result.Success ? result.Data!.ToList() : new(),
                PageTitle = "Bütün Müraciətlər",
                TesdiqAction = ""
            };
            ViewData["Title"] = "Bütün Məzuniyyət Müraciətləri";
            return View("TesdiqIndex", vm);
        }

        // DÖVLƏT VƏZİFƏSİ KORREKSİYASI — Maddə 173

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Korreksiya([FromForm] MezuniyyetKorreksiyaDto dto)
        {
            var hrIsciId = await GetCurrentIsciIdAsync();
            if (hrIsciId == null)
                return Json(new { ok = false, xeta = "İstifadəçi tapılmadı." });

            var yollar = new List<string>();
            if (dto.Senedler != null && dto.Senedler.Count > 0)
            {
                var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
                var dir = Path.Combine(dmsRoot, "dovlet-vezife");
                Directory.CreateDirectory(dir);

                foreach (var sened in dto.Senedler)
                {
                    if (sened == null || sened.Length == 0) continue;

                    var ext = Path.GetExtension(sened.FileName).ToLowerInvariant();
                    if (!_icazeSenedTipler.Contains(ext))
                        return Json(new
                        {
                            ok = false,
                            xeta = $"\"{sened.FileName}\" — format qəbul edilmir. İcazə verilən: PDF, JPG, PNG."
                        });

                    if (sened.Length > 10 * 1024 * 1024)
                        return Json(new
                        {
                            ok = false,
                            xeta = $"\"{sened.FileName}\" — 10 MB-dan böyük ola bilməz."
                        });

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    await using (var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                        await sened.CopyToAsync(fs);

                    yollar.Add($"/dms/dovlet-vezife/{fileName}");
                }
            }

            var senedYolu = string.Join("|", yollar);

            var result = await _mezuniyyetService.KorreksiyaEtAsync(dto, hrIsciId.Value, senedYolu);
            if (!result.Success)
                return Json(new { ok = false, xeta = result.Message });

            return Json(new
            {
                ok      = true,
                mesaj   = result.Message,
                yeniId  = result.Data?.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KorreksiyaSenedElave(int mezuniyyetId, List<IFormFile> senedler)
        {
            if (senedler == null || senedler.Count == 0)
                return Json(new { ok = false, xeta = "Ən azı bir sənəd seçin." });

            var dmsRoot2 = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
            var dir = Path.Combine(dmsRoot2, "dovlet-vezife");
            Directory.CreateDirectory(dir);

            var yollar = new List<string>();
            foreach (var sened in senedler)
            {
                if (sened == null || sened.Length == 0) continue;

                var ext = Path.GetExtension(sened.FileName).ToLowerInvariant();
                if (!_icazeSenedTipler.Contains(ext))
                    return Json(new
                    {
                        ok = false,
                        xeta = $"\"{sened.FileName}\" — format qəbul edilmir. İcazə verilən: PDF, JPG, PNG."
                    });

                if (sened.Length > 10 * 1024 * 1024)
                    return Json(new
                    {
                        ok = false,
                        xeta = $"\"{sened.FileName}\" — 10 MB-dan böyük ola bilməz."
                    });

                var fileName = $"{Guid.NewGuid()}{ext}";
                await using (var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                    await sened.CopyToAsync(fs);

                yollar.Add($"/dms/dovlet-vezife/{fileName}");
            }

            if (yollar.Count == 0)
                return Json(new { ok = false, xeta = "Heç bir sənəd emal edilmədi." });

            var result = await _mezuniyyetService.SenedElavetEtAsync(
                mezuniyyetId, string.Join("|", yollar));

            if (!result.Success)
                return Json(new { ok = false, xeta = result.Message });

            return Json(new { ok = true, mesaj = result.Message });
        }
    }
}
