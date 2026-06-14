using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.User.ViewModels.Tesdiq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class TesdiqController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IIcazeService _icazeService;
        private readonly IIsciStrukturRoluService _strukturRoluService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IJetonService _jetonService;
        private readonly IUnitOfWork _unitOfWork;

        public TesdiqController(
            IMezuniyyetService mezuniyyetService,
            IIcazeService icazeService,
            IIsciStrukturRoluService strukturRoluService,
            UserManager<AppUser> userManager,
            IJetonService jetonService,
            IUnitOfWork unitOfWork)
        {
            _mezuniyyetService = mezuniyyetService;
            _icazeService = icazeService;
            _strukturRoluService = strukturRoluService;
            _userManager = userManager;
            _jetonService = jetonService;
            _unitOfWork = unitOfWork;
        }

        // GET /User/Tesdiq/SobeReisi
        // TesdiqController.SobeReisi() — belə olmalıdır:
        // TesdiqController.SobeReisi() — belə olmalıdır:
        public async Task<IActionResult> SobeReisi()
        {
            if (!await HasRolAsync(StrukturRolTipi.SobeReisi))
            {
                TempData["Error"] = "Bu səhifəyə giriş icazəniz yoxdur.";
                return RedirectToAction("Index", "Dashboard");
            }

            var appUser = await _userManager.GetUserAsync(User);
            var isciId = appUser?.IsciId;
            if (isciId == null) return Forbid();

            // İşçinin departament ID-sini al
            var strukturResult = await _strukturRoluService.GetByIsciIdAsync(isciId.Value);
            var departamentId = strukturResult.Success
                ? strukturResult.Data?
                    .FirstOrDefault(r => r.RolTipi == StrukturRolTipi.SobeReisi && r.Aktivdir)
                    ?.DepartamentId
                : null;

            // Departamentə görə filtrləyib çək — həm Mezuniyyet həm də İcazə üçün
            Task<Result<IList<MezuniyyetListDto>>> mezTask = departamentId.HasValue
                ? _mezuniyyetService.GetSobeyeGoreMezuniyyetlerAsync(departamentId.Value, isciId.Value)
                : _mezuniyyetService.GetGozlemededeAsync(); // fallback

            Task<Result<IList<IcazeListDto>>> icazeTask = departamentId.HasValue
                ? _icazeService.GetSobeyeGoreIcazelerAsync(departamentId.Value, isciId.Value)
                : _icazeService.GetGozlemededeAsync(); // fallback — şöbə tapılmayıbsa hamısı

            var mezResult = await mezTask;
            var icazeResult = await icazeTask;

            var vm = new TesdiqIndexVM
            {
                Mezuniyyetler = mezResult.Success ? mezResult.Data!.ToList() : new(),
                Icazeler = icazeResult.Success ? icazeResult.Data!.ToList() : new(),
                RolBasliq = "Şöbə Rəhbəri Paneli",
                RolAciqlamasi = "Gözləmədə olan məzuniyyət və icazə müraciətlərini nəzərdən keçirin",
                Rol = StrukturRolTipi.SobeReisi
            };

            ViewData["Title"] = "Şöbə Rəhbəri Paneli";
            return View(vm);
        }

        // GET /User/Tesdiq/Rehber
        public async Task<IActionResult> Rehber()
        {
            if (!await HasRolAsync(StrukturRolTipi.Rehber))
            {
                TempData["Error"] = "Bu səhifəyə giriş icazəniz yoxdur.";
                return RedirectToAction("Index", "Dashboard");
            }

            var mezResult = await _mezuniyyetService.GetRehberTesdiqindeAsync();
            var icazeResult = await _icazeService.GetRehberTesdiqindeAsync();

            var vm = new TesdiqIndexVM
            {
                Mezuniyyetler = mezResult.Success ? mezResult.Data!.ToList() : new(),
                Icazeler = icazeResult.Success ? icazeResult.Data!.ToList() : new(),
                RolBasliq = "Rəhbər Paneli",
                RolAciqlamasi = "Şöbə rəhbəri tərəfindən təsdiqlənmiş müraciətləri nəzərdən keçirin",
                Rol = StrukturRolTipi.Rehber
            };

            ViewData["Title"] = "Rəhbər Paneli";
            return View(vm);
        }

        // GET /User/Tesdiq/Hr
        public async Task<IActionResult> Hr()
        {
            if (!await HasRolAsync(StrukturRolTipi.Hr))
            {
                TempData["Error"] = "Bu səhifəyə giriş icazəniz yoxdur.";
                return RedirectToAction("Index", "Dashboard");
            }

            var mezResult = await _mezuniyyetService.GetHrTesdiqindeAsync();
            var icazeResult = await _icazeService.GetHrTesdiqindeAsync();

            var vm = new TesdiqIndexVM
            {
                Mezuniyyetler = mezResult.Success ? mezResult.Data!.ToList() : new(),
                Icazeler = icazeResult.Success ? icazeResult.Data!.ToList() : new(),
                RolBasliq = "HR Paneli",
                RolAciqlamasi = "Son mərhələ — müraciətləri rəsmiləşdirin",
                Rol = StrukturRolTipi.Hr
            };

            ViewData["Title"] = "HR Paneli";
            return View(vm);
        }

        // GET /User/Tesdiq/MezuniyyetDetal/5?rol=SobeReisi
        public async Task<IActionResult> MezuniyyetDetal(int id, string rol)
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "Müraciət tapılmadı.";
                return RedirectToAction(rol);
            }

            var viewerRol = Enum.TryParse<StrukturRolTipi>(rol, out var parsedViewerRol) ? (StrukturRolTipi?)parsedViewerRol : null;
            var overlap = await _mezuniyyetService.GetOverlapMezuniyyetlerAsync(id, viewerRol);
            var konflikt = await _mezuniyyetService.GetEvezediciKonfliktiAsync(id);

            var dto = result.Data;
            var vm = new TesdiqDetalVM
            {
                Id = dto.Id,
                IsciAdSoyad = dto.IsciAdSoyad,
                SobeAdi = dto.SobeAdi,
                VezifeAdi = dto.VezifeAdi,
                EvezEdenIsciAdSoyad = dto.EvezEdenIsciAdSoyad,
                IsMezuniyyet = true,
                OverlapMezuniyyetler = overlap.Success ? overlap.Data!.ToList() : new(),
                EvezediciKonfliktleri = konflikt.Success ? konflikt.Data!.ToList() : new(),
                NovText = (int)dto.Nov switch
                {
                    1 => "Əmək məzuniyyəti",
                    2 => "Xəstəlik məzuniyyəti",
                    3 => "Ezamiyyət",
                    _ => dto.Nov.ToString()
                },
                BaslamaTarixi = dto.BaslamaTarixi,
                BitmeTarixi = dto.BitmeTarixi,
                IsGunlerininSayi = dto.IsGunlerininSayi,
                Qeyd = dto.Qeyd,
                Status = (int)dto.Status,
                StatusText = (int)dto.Status switch
                {
                    1 => "Gözləmədə",
                    2 => "Şöbə rəisi təsdiqində",
                    3 => "Rəhbər təsdiqində",
                    4 => "HR təsdiqində",
                    5 => "Təsdiqlənib",
                    6 => "İmtina edildi",
                    _ => dto.Status.ToString()
                },
                ImtinaSebebi = dto.ImtinaSebebi,
                SobeReisiTesdiq = dto.SobeReisiTesdiq,
                SobeReisiTesdiqTarixi = dto.SobeReisiTesdiqTarixi,
                RehberTesdiq = dto.RehberTesdiq,
                RehberTesdiqTarixi = dto.RehberTesdiqTarixi,
                HrTesdiq = dto.HrTesdiq,
                HrTesdiqTarixi = dto.HrTesdiqTarixi,
                EmrRegem = dto.EmrRegem,
                EmrSuffiks = dto.EmrSuffiks,
                EmrIl = dto.EmrIl,
                TesdiqciRol = Enum.TryParse<StrukturRolTipi>(rol, out var mezTesdiqciRol) ? mezTesdiqciRol : StrukturRolTipi.SobeReisi
            };

            ViewBag.Rol = rol;
            ViewData["Title"] = "Məzuniyyət Detalı";
            ViewData["TopbarTarix"] = dto.BaslamaTarixi.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("az-Latn-AZ"));
            return View(vm);
        }

        // GET /User/Tesdiq/IcazeDetal/5?rol=SobeReisi
        public async Task<IActionResult> IcazeDetal(int id, string rol)
        {
            var result = await _icazeService.GetDetayAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "İcazə tapılmadı.";
                return RedirectToAction(rol);
            }

            var dto = result.Data;
            var vm = new TesdiqDetalVM
            {
                Id = dto.Id,
                IsciAdSoyad = dto.IsciAdSoyad,
                SobeAdi = dto.SobeAdi,
                VezifeAdi = "-",
                EvezEdenIsciAdSoyad = dto.EvezEdenAdSoyad,
                IsMezuniyyet = false,
                IcazeTarixi = dto.IcazeTarixi,
                BaslamaSaati = dto.BaslamaSaati,
                BitisSaati = dto.BitisSaati,
                IcazeSaati = dto.IcazeSaati,
                NaharNezereAlinmasin = dto.NaharNezereAlinmasin,
                Sebeb = dto.Sebeb,
                Status = (int)dto.Status,
                StatusText = (int)dto.Status switch
                {
                    1 => "Gözləmədə",
                    2 => "Şöbə rəisi təsdiqində",
                    3 => "Rəhbər təsdiqində",
                    4 => "HR təsdiqində",
                    5 => "Təsdiqlənib",
                    6 => "İmtina edildi",
                    _ => dto.Status.ToString()
                },
                ImtinaSebebi = dto.ImtinaSebebi,
                SobeReisiTesdiq = dto.SobeReisiTesdiq,
                SobeReisiTesdiqTarixi = dto.SobeReisiTesdiqTarixi,
                RehberTesdiq = dto.RehberTesdiq,
                RehberTesdiqTarixi = dto.RehberTesdiqTarixi,
                HrTesdiq = dto.HrTesdiq,
                HrTesdiqTarixi = dto.HrTesdiqTarixi,
                TesdiqciRol = Enum.TryParse<StrukturRolTipi>(rol, out var r) ? r : StrukturRolTipi.SobeReisi
            };

            ViewBag.Rol = rol;
            ViewBag.JetonBalansi = await _jetonService.AktivSaatBalansiAsync(dto.IsciId);
            ViewBag.JetonOdenenSaat = dto.JetonOdenenSaat;

            // Nahar parametrləri — rəhbər panelindəki checkbox üçün
            var isParam = await _unitOfWork.Repository<IsParametri>()
                .Query().Where(x => !x.Silinib).FirstOrDefaultAsync();
            ViewBag.NaharBaslamaSaati = isParam?.NaharBaslamaSaati ?? new TimeSpan(13, 0, 0);
            ViewBag.NaharMuddetDeqiqe = isParam?.NaharMuddetDeqiqe ?? 45;
            ViewData["Title"] = "İcazə Detalı";
            ViewData["TopbarTarix"] = dto.IcazeTarixi.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("az-Latn-AZ"));
            return View(vm);
        }

        // TesdiqController.cs — MezuniyyetTesdiq metodu, DƏYİŞ:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MezuniyyetTesdiq(int id, bool status, string? qeyd, string rol,
            int? gunSayiManual = null, string? duzelisSebebi = null)
        {
            var appUser = await _userManager.GetUserAsync(User);
            var isciId = appUser?.IsciId ?? 0;

            Result result;
            switch (rol)
            {
                case "SobeReisi":
                    result = await _mezuniyyetService.SobeReisiTesdiqAsync(id, status, qeyd, isciId);
                    break;
                case "Rehber":
                    result = await _mezuniyyetService.RehberTesdiqAsync(id, status, qeyd, isciId);
                    break;
                case "Hr":
                    // HR üçün opsional gün sayı düzəlişi
                    result = await _mezuniyyetService.HrTesdiqAsync(id, status, qeyd, isciId,
                        gunSayiManual, duzelisSebebi);
                    break;
                default:
                    TempData["Error"] = "Naməlum rol.";
                    return RedirectToAction("Index", "Dashboard");
            }

            if (rol == "Rehber" && status && result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(TapsiriqTeklif), new { mezuniyyetId = id });
            }

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(rol);
        }

        // GET /User/Tesdiq/TapsiriqTeklif?mezuniyyetId=5
        public async Task<IActionResult> TapsiriqTeklif(int mezuniyyetId)
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(mezuniyyetId);
            if (!result.Success || result.Data == null)
                return RedirectToAction(nameof(Rehber));

            var dto = result.Data;
            var az = new System.Globalization.CultureInfo("az-Latn-AZ");
            var baslama = dto.BaslamaTarixi.ToString("dd MMMM yyyy", az);
            var bitis   = dto.BitmeTarixi.ToString("dd MMMM yyyy", az);

            ViewBag.IsciAd  = dto.IsciAdSoyad;
            ViewBag.Baslama = baslama;
            ViewBag.Bitis   = bitis;
            ViewBag.Gun     = dto.IsGunlerininSayi;
            ViewBag.Tesvir  = Uri.EscapeDataString(
                $"{dto.IsciAdSoyad} məzuniyyətdə olacaq: {baslama} – {bitis} ({dto.IsGunlerininSayi} iş günü). Müvəqqəti əvəzetmə tapşırığı.");

            ViewData["Title"] = "Tapşırıq Teklifi";
            return View();
        }

        // POST /User/Tesdiq/IcazeTesdiq
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IcazeTesdiq(int id, bool status, string? qeyd, string rol, bool birdefelik = false, decimal jetonOdenenSaat = 0, bool naharNezereAlinmasin = false)
        {
            var appUser = await _userManager.GetUserAsync(User);
            var tesdiqciIsciId = appUser?.IsciId ?? 0;

            Result result;
            switch (rol)
            {
                case "SobeReisi":
                    result = await _icazeService.SobeReisiTesdiqAsync(id, status, qeyd, tesdiqciIsciId);
                    break;
                case "Rehber":
                    result = await _icazeService.RehberTesdiqAsync(id, status, qeyd, tesdiqciIsciId, jetonOdenenSaat, naharNezereAlinmasin, birdefelik);
                    break;
                case "Hr":
                    result = await _icazeService.HrTesdiqAsync(id, status, qeyd, tesdiqciIsciId, birdefelik);
                    break;
                default:
                    TempData["Error"] = "Naməlum rol.";
                    return RedirectToAction("Index", "Dashboard");
            }

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(rol);
        }

        private async Task<bool> HasRolAsync(StrukturRolTipi rolTipi)
        {
            // Admin həmişə keçə bilir
            if (User.IsInRole(RoleNames.Admin)) return true;

            // Identity rol fallback — sistem-səviyyəli rolu olan istifadəçilər
            // (struktur rolu olmasa belə keçə bilsinlər)
            var identityRolAdi = rolTipi switch
            {
                StrukturRolTipi.SobeReisi => RoleNames.SobeReisi,
                StrukturRolTipi.Rehber    => RoleNames.Rehber,
                StrukturRolTipi.Hr        => RoleNames.HR,
                _ => null
            };
            if (identityRolAdi != null && User.IsInRole(identityRolAdi))
                return true;

            // Struktur rolu yoxlaması (departament səviyyəsində)
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return false;

            var rolResult = await _strukturRoluService.GetByIsciIdAsync(appUser.IsciId.Value);
            if (!rolResult.Success || rolResult.Data == null) return false;

            return rolResult.Data.Any(r => r.RolTipi == rolTipi && r.Aktivdir);
        }
    }
}
