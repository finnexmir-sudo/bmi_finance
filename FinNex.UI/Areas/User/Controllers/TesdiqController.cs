using FinNex.Application.Common.Results;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.User.ViewModels.Tesdiq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

        public TesdiqController(
            IMezuniyyetService mezuniyyetService,
            IIcazeService icazeService,
            IIsciStrukturRoluService strukturRoluService,
            UserManager<AppUser> userManager)
        {
            _mezuniyyetService = mezuniyyetService;
            _icazeService = icazeService;
            _strukturRoluService = strukturRoluService;
            _userManager = userManager;
        }

        // GET /User/Tesdiq/SobeReisi
        public async Task<IActionResult> SobeReisi()
        {
            if (!await HasRolAsync(StrukturRolTipi.SobeReisi))
            {
                TempData["Error"] = "Bu səhifəyə giriş icazəniz yoxdur.";
                return RedirectToAction("Index", "Dashboard");
            }

            var mezResult = await _mezuniyyetService.GetGozlemededeAsync();
            var icazeResult = await _icazeService.GetGozlemededeAsync();

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

            var dto = result.Data;
            var vm = new TesdiqDetalVM
            {
                Id = dto.Id,
                IsciAdSoyad = dto.IsciAdSoyad,
                SobeAdi = dto.SobeAdi,
                VezifeAdi = dto.VezifeAdi,
                EvezEdenIsciAdSoyad = dto.EvezEdenIsciAdSoyad,
                IsMezuniyyet = true,
                NovText = (int)dto.Nov switch
                {
                    1 => "İllik məzuniyyət",
                    2 => "Xəstəlik məzuniyyəti",
                    3 => "Ezamiyyət",
                    _ => dto.Nov.ToString()
                },
                BaslamaTarixi = dto.BaslamaTarixi,
                BitmeTarixi = dto.BitmeTarixi,
                IsGunlerininSayi = dto.IsGunlerininSayi,
                Qeyd = dto.Qeyd,
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
            ViewData["Title"] = "Məzuniyyət Detalı";
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
                Sebeb = dto.Sebeb,
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
            ViewData["Title"] = "İcazə Detalı";
            return View(vm);
        }

        // POST /User/Tesdiq/MezuniyyetTesdiq
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MezuniyyetTesdiq(int id, bool status, string? qeyd, string rol)
        {
            Result result;
            switch (rol)
            {
                case "SobeReisi":
                    result = await _mezuniyyetService.SobeReisiTesdiqAsync(id, status, qeyd);
                    break;
                case "Rehber":
                    result = await _mezuniyyetService.RehberTesdiqAsync(id, status, qeyd);
                    break;
                case "Hr":
                    result = await _mezuniyyetService.HrTesdiqAsync(id, status, qeyd);
                    break;
                default:
                    TempData["Error"] = "Naməlum rol.";
                    return RedirectToAction("Index", "Dashboard");
            }

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(rol);
        }

        // POST /User/Tesdiq/IcazeTesdiq
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IcazeTesdiq(int id, bool status, string? qeyd, string rol)
        {
            Result result;
            switch (rol)
            {
                case "SobeReisi":
                    result = await _icazeService.SobeReisiTesdiqAsync(id, status, qeyd);
                    break;
                case "Rehber":
                    result = await _icazeService.RehberTesdiqAsync(id, status, qeyd);
                    break;
                case "Hr":
                    result = await _icazeService.HrTesdiqAsync(id, status, qeyd);
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
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return false;

            var rolResult = await _strukturRoluService.GetByIsciIdAsync(appUser.IsciId.Value);
            if (!rolResult.Success || rolResult.Data == null) return false;

            return rolResult.Data.Any(r => r.RolTipi == rolTipi && r.Aktivdir);
        }
    }
}
