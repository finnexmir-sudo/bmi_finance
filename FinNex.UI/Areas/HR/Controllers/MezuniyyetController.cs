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

        private static readonly string[] _icazeSenedTipler =
            [".pdf", ".jpg", ".jpeg", ".png"];

        public MezuniyyetController(
            IMezuniyyetService mezuniyyetService,
            IIsciService isciService,
            UserManager<AppUser> userManager,
            IBildirisService bildirisService,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment env)
        {
            _mezuniyyetService = mezuniyyetService;
            _isciService = isciService;
            _userManager = userManager;
            _bildirisService = bildirisService;
            _unitOfWork = unitOfWork;
            _env = env;
        }

        // ── Köməkçi: cari istifadəçinin IsciId-sini alır ──────
        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        // ── Köməkçi: cari işçinin aktiv departament ID-sini alır ──
        private async Task<int?> GetCurrentDepartamentIdAsync(int isciId)
        {
            var result = await _isciService.GetAktivDepartamentIdAsync(isciId);
            return result.Success ? result.Data : null;
        }

        // ══════════════════════════════════════════════════════
        // ŞÖBƏ RƏİSİ BÖLÜMÜ
        // Rol: "SobeReisi"
        // ══════════════════════════════════════════════════════

        //[Authorize(Roles = "SobeReisi,Admin")]
        //public async Task<IActionResult> SobeReisi()
        //{
        //    var isciId = await GetCurrentIsciIdAsync();
        //    if (isciId == null) return Forbid();

        //    var departamentId = await GetCurrentDepartamentIdAsync(isciId.Value);
        //    if (departamentId == null) return Forbid();

        //    var result = await _mezuniyyetService.GetSobeyeGoreMezuniyyetlerAsync(departamentId.Value);

        //    var vm = new HrMezuniyyetIndexVM
        //    {
        //        Mezuniyyetler = result.Success ? result.Data!.ToList() : new(),
        //        PageTitle = "Şöbə Rəisi — Gözləyən Müraciətlər",
        //        TesdiqAction = "SobeReisiTesdiq"
        //    };

        //    ViewData["Title"] = "Şöbə Rəisi Təsdiqi";
        //    return View("TesdiqIndex", vm);
        //}

        // ══════════════════════════════════════════════════════
        // RƏHBƏR BÖLÜMÜ
        // Rol: "Rehber"
        // ══════════════════════════════════════════════════════

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

        // ══════════════════════════════════════════════════════
        // HR BÖLÜMÜ
        // Rol: "HR"
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Hr(string tab = "tesdiq", string? axtaris = null)
        {
            var aktivTab = tab switch
            {
                "proses" => "proses",
                "tarixce" => "tarixce",
                _ => "tesdiq"
            };

            // Sayğaclar üçün bütün tablar sayğısını hesablayırıq
            var tesdiqResult = await _mezuniyyetService.GetHrTesdiqindeAsync();
            var prosesResult = await _mezuniyyetService.GetProsesdeOlanlarAsync();
            var tarixceResult = await _mezuniyyetService.GetTarixceAsync(
                aktivTab == "tarixce" ? axtaris : null);

            var tesdiqList = tesdiqResult.Success ? tesdiqResult.Data!.ToList() : new();
            var prosesList = prosesResult.Success ? prosesResult.Data!.ToList() : new();
            var tarixceList = tarixceResult.Success ? tarixceResult.Data!.ToList() : new();

            // Tarixçə üçün sayğacı axtarış olmadan götürürük ki, rəqəm sabit qalsın
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

        // ══════════════════════════════════════════════════════
        // DETAL SƏHİFƏSİ (3 rol üçün ortaq)
        // ══════════════════════════════════════════════════════

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

        // ══════════════════════════════════════════════════════
        // POST — ŞÖBƏ RƏİSİ TƏSDİQİ
        // ══════════════════════════════════════════════════════

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "SobeReisi,Admin")]
        //public async Task<IActionResult> SobeReisiTesdiq(int id, bool status, string? qeyd)
        //{
        //    var isciId = await GetCurrentIsciIdAsync();
        //    if (isciId == null) return Forbid();

        //    // Məzuniyyəti yükləyib SobeReisiId-ni set edirik
        //    var mezResult = await _mezuniyyetService.IdIleGetirAsync(id);
        //    if (!mezResult.Success || mezResult.Data == null) return NotFound();

        //    var result = await _mezuniyyetService.SobeReisiTesdiqAsync(id, status, qeyd, isciId.Value);

        //    TempData[result.Success ? "Success" : "Error"] = result.Message;
        //    return RedirectToAction(nameof(SobeReisi));
        //}

        // ══════════════════════════════════════════════════════
        // POST — RƏHBƏR TƏSDİQİ
        // ══════════════════════════════════════════════════════

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

        // ══════════════════════════════════════════════════════
        // POST — HR TƏSDİQİ
        // ══════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> HrTesdiq(int id, bool status, string? qeyd)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();

            var result = await _mezuniyyetService.HrTesdiqAsync(id, status, qeyd, isciId.Value);

            // Mühasib bildirişi artıq MezuniyyetService.HrTesdiqAsync daxilindədir
            // (həm qabaqcadan, həm ay-sonu üçün). Burada təkrar göndərilmir.

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Hr));
        }

        // ══════════════════════════════════════════════════════
        // GERİYƏ QEYD — HR keçmiş tarix üçün məzuniyyət rəsmiləşdirir
        // Emergency halları üçün: işçi işdə olmayıb, sonra HR sənədləşdirir.
        // Təsdiq axınını atlayır, işçinin Davamiyyətindəki Qayib → İcazəliyə çevirir.
        // ══════════════════════════════════════════════════════

        [HttpGet]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> GeriyeQeyd()
        {
            var vm = new GeriyeQeydVM
            {
                Isciler = await GetAktivIsciSelectListAsync()
            };

            ViewData["Title"] = "Geriyə Məzuniyyət Qeyd et";
            return View(vm);
        }

        // ══════════════════════════════════════════════════════
        // AKTİV MƏZUNİYYƏTLƏR — izləmə paneli
        // Hazırda məzuniyyətdə olan və yaxın günlərdə başlayacaqlar.
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
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
            // HR təsdiq panelinə qayıt (Index yalnız Admin üçündür).
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

        // ══════════════════════════════════════════════════════
        // ADMIN — hamısını görür
        // ══════════════════════════════════════════════════════

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

        // Xəstəlik/Ezamiyyət əməliyyatları XestelikEzamiyyetController-ə köçürülüb

        // ══════════════════════════════════════════════════════
        // DÖVLƏT VƏZİFƏSİ KORREKSİYASI — Maddə 173
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Dövlət vəzifəsi korreksiyası (hərbi çağırış, məhkəmə şahidliyi və s.).
        /// İşçi illik məzuniyyətdə olarkən dövlət vəzifəsi icra edirsə:
        ///   — həmin günlər balansdan geri qaytarılır
        ///   — DovletVezifelerininIcrasi növündə yeni qeyd yaranır
        ///   — Davamiyyətdə MaasdanKes=false — maaşdan kəsinti olmur
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Korreksiya([FromForm] MezuniyyetKorreksiyaDto dto)
        {
            var hrIsciId = await GetCurrentIsciIdAsync();
            if (hrIsciId == null)
                return Json(new { ok = false, xeta = "İstifadəçi tapılmadı." });

            // ── Sənəd saxla (opsional) ────────────────────────────────────
            string senedYolu = string.Empty;
            if (dto.Sened != null && dto.Sened.Length > 0)
            {
                var ext = Path.GetExtension(dto.Sened.FileName).ToLowerInvariant();
                if (!_icazeSenedTipler.Contains(ext))
                    return Json(new
                    {
                        ok = false,
                        xeta = "Sənəd formatı qəbul edilmir. İcazə verilən: PDF, JPG, PNG."
                    });

                if (dto.Sened.Length > 10 * 1024 * 1024)
                    return Json(new { ok = false, xeta = "Sənəd 10 MB-dan böyük ola bilməz." });

                var dir = Path.Combine(_env.WebRootPath, "uploads", "dovlet-vezife");
                Directory.CreateDirectory(dir);
                var fileName = $"{Guid.NewGuid()}{ext}";
                await using (var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                    await dto.Sened.CopyToAsync(fs);

                senedYolu = $"/uploads/dovlet-vezife/{fileName}";
            }

            // ── Servis çağır ──────────────────────────────────────────────
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
    }
}