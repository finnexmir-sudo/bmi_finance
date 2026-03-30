// Areas/User/Controllers/KomandaController.cs
using FinNex.Application.DTOs.HR.IsciStrukturRolu;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.User.ViewModels.Komanda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class KomandaController : Controller
    {
        private readonly IIsciStrukturRoluService _strukturRoluService;
        private readonly IIsciService _isciService;
        private readonly ITapshiriqService _tapshiriqService;
        private readonly IGorushService _gorushService;
        private readonly UserManager<AppUser> _userManager;

        public KomandaController(
            IIsciStrukturRoluService strukturRoluService,
            IIsciService isciService,
            ITapshiriqService tapshiriqService,
            IGorushService gorushService,
            UserManager<AppUser> userManager)
        {
            _strukturRoluService = strukturRoluService;
            _isciService = isciService;
            _tapshiriqService = tapshiriqService;
            _gorushService = gorushService;
            _userManager = userManager;
        }

        // ── GET /User/Komanda ────────────────────────────────
        // Giriş nöqtəsi — rola görə avtomatik yönləndirir
        public async Task<IActionResult> Index()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var rolResult = await _strukturRoluService.GetByIsciIdAsync(isciId.Value);
            var roller = rolResult.Success ? rolResult.Data ?? new List<IsciStrukturRoluDto>()
                                          : new List<IsciStrukturRoluDto>();

            var aktivRoller = roller.Where(r => r.Aktivdir).Select(r => r.RolTipi).ToList();

            // Ən yüksək rola görə yönləndir
            if (aktivRoller.Contains(StrukturRolTipi.Hr))
                return RedirectToAction(nameof(HrIcmal));
            if (aktivRoller.Contains(StrukturRolTipi.Rehber))
                return RedirectToAction(nameof(RehberIcmal));
            if (aktivRoller.Contains(StrukturRolTipi.SobeReisi))
                return RedirectToAction(nameof(SobeIcmal));

            TempData["Error"] = "Komanda icmalına giriş icazəniz yoxdur.";
            return RedirectToAction("Index", "Dashboard");
        }

        // ── GET /User/Komanda/SobeIcmal ──────────────────────
        // Şöbə rəisi — öz departamentini görür
        public async Task<IActionResult> SobeIcmal()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var rolResult = await _strukturRoluService.GetByIsciIdAsync(isciId.Value);
            var sobeRolu = rolResult.Success
                ? rolResult.Data?.FirstOrDefault(r => r.RolTipi == StrukturRolTipi.SobeReisi && r.Aktivdir)
                : null;

            if (sobeRolu == null)
            {
                TempData["Error"] = "Bu səhifəyə giriş icazəniz yoxdur.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Şöbə işçilərini çək
            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv);

            // Departament üzvlərini filtrləmək üçün struktur rollarını çək
            var departamentIsciIdler = new List<int>();
            if (sobeRolu.DepartamentId.HasValue)
            {
                var depRolResult = await _strukturRoluService
                    .GetByDepartamentRolAsync(sobeRolu.DepartamentId.Value, StrukturRolTipi.Rehber);
                // Bütün işçiləri (rol fərqindən asılı olmayaraq) departamentdən götürürük
                var depHerRolResult = await _strukturRoluService
                    .GetByDepartamentRolAsync(sobeRolu.DepartamentId.Value, StrukturRolTipi.SobeReisi);

                // Departamentin bütün işçi ID-lərini yığ
                if (depRolResult.Success)
                    departamentIsciIdler.AddRange(depRolResult.Data!.Select(r => r.IsciId));
                if (depHerRolResult.Success)
                    departamentIsciIdler.AddRange(depHerRolResult.Data!.Select(r => r.IsciId));

                departamentIsciIdler = departamentIsciIdler.Distinct().ToList();
            }

            var isciler = iscilerResult.Success
                ? iscilerResult.Data!
                    .Where(x => departamentIsciIdler.Count == 0 || departamentIsciIdler.Contains(x.Id))
                    .ToList()
                : new();

            var vm = new KomandaIcmalVM
            {
                RolBasliq = "Şöbə İcmalı",
                DepartamentAdi = sobeRolu.DepartamentAd ?? "—",
                Isciler = isciler.Select(x => new KomandaIsciSatiriVM
                {
                    Id = x.Id,
                    TamAd = x.TamAd,
                    Vezife = x.VezifeAdi ?? "—",
                    Status = x.Status
                }).ToList()
            };

            ViewData["Title"] = "Şöbə İcmalı";
            return View("Icmal", vm);
        }

        // ── GET /User/Komanda/RehberIcmal ────────────────────
        // Rəhbər — bütün şöbələrin xülasəsini görür
        public async Task<IActionResult> RehberIcmal()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var rolResult = await _strukturRoluService.GetByIsciIdAsync(isciId.Value);
            var rehberRolu = rolResult.Success
                ? rolResult.Data?.FirstOrDefault(r => r.RolTipi == StrukturRolTipi.Rehber && r.Aktivdir)
                : null;

            if (rehberRolu == null)
            {
                TempData["Error"] = "Bu səhifəyə giriş icazəniz yoxdur.";
                return RedirectToAction("Index", "Dashboard");
            }

            var iscilerResult = await _isciService.HamisiniGetirAsync(x => x.Status == IsciStatus.Aktiv);
            var isciler = iscilerResult.Success ? iscilerResult.Data!.ToList() : new();

            var vm = new KomandaIcmalVM
            {
                RolBasliq = "Rəhbər İcmalı",
                DepartamentAdi = "Bütün şöbələr",
                Isciler = isciler.Select(x => new KomandaIsciSatiriVM
                {
                    Id = x.Id,
                    TamAd = x.TamAd,
                    Vezife = x.VezifeAdi ?? "—",
                    Status = x.Status
                }).ToList()
            };

            ViewData["Title"] = "Rəhbər İcmalı";
            return View("Icmal", vm);
        }

        // ── GET /User/Komanda/HrIcmal ─────────────────────────
        // HR — statistik icmal: işçi sayı, aktiv/passiv, cins bölgüsü
        public async Task<IActionResult> HrIcmal()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var rolResult = await _strukturRoluService.GetByIsciIdAsync(isciId.Value);
            var hrRolu = rolResult.Success
                ? rolResult.Data?.FirstOrDefault(r => r.RolTipi == StrukturRolTipi.Hr && r.Aktivdir)
                : null;

            if (hrRolu == null)
            {
                TempData["Error"] = "Bu səhifəyə giriş icazəniz yoxdur.";
                return RedirectToAction("Index", "Dashboard");
            }

            var hamisiResult = await _isciService.HamisiniGetirAsync(x => true);
            var hamisi = hamisiResult.Success ? hamisiResult.Data!.ToList() : new();

            var vm = new HrKomandaIcmalVM
            {
                CəmiIsci = hamisi.Count,
                AktivIsci = hamisi.Count(x => x.Status == IsciStatus.Aktiv),
                PassivIsci = hamisi.Count(x => x.Status != IsciStatus.Aktiv),
                KisaySayi = hamisi.Count(x => x.Cins == Cins.Kisi && x.Status == IsciStatus.Aktiv),
                QadınSayi = hamisi.Count(x => x.Cins == Cins.Qadin && x.Status == IsciStatus.Aktiv),
                Isciler = hamisi
                    .Where(x => x.Status == IsciStatus.Aktiv)
                    .Select(x => new KomandaIsciSatiriVM
                    {
                        Id = x.Id,
                        TamAd = x.TamAd,
                        Vezife = x.VezifeAdi ?? "—",
                        Status = x.Status
                    }).ToList()
            };

            ViewData["Title"] = "HR İcmalı";
            return View("HrIcmal", vm);
        }

        // ── GET /User/Komanda/IsciKart/5 ─────────────────────
        // İşçi kart — tapşırıq + görüş xülasəsi
        public async Task<IActionResult> IsciKart(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            // Rolu yoxla
            var rolResult = await _strukturRoluService.GetByIsciIdAsync(isciId.Value);
            var hasRol = rolResult.Success && rolResult.Data!
                .Any(r => r.Aktivdir && (
                    r.RolTipi == StrukturRolTipi.SobeReisi ||
                    r.RolTipi == StrukturRolTipi.Rehber ||
                    r.RolTipi == StrukturRolTipi.Hr));

            if (!hasRol)
            {
                TempData["Error"] = "Giriş icazəniz yoxdur.";
                return RedirectToAction("Index", "Dashboard");
            }

            var isciResult = await _isciService.IdIleGetirAsync(id);
            if (!isciResult.Success || isciResult.Data == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var tapshiriqResult = await _tapshiriqService.GetMenimTapshiriqlarimAsync(id);
            var gorushResult = await _gorushService.GetMenimGorushlerimAsync(id);

            var vm = new IsciKartVM
            {
                IsciId = id,
                TamAd = isciResult.Data.TamAd,
                Vezife = isciResult.Data.VezifeAdi ?? "—",
                Email = isciResult.Data.Email,
                Telefon = isciResult.Data.Telefon,
                IsheQebulTarixi = isciResult.Data.YaradilmaTarixi,
                AktivTapshiriqlar = tapshiriqResult.Success
                    ? tapshiriqResult.Data!
                        .Where(t => t.Status != TapshiriqStatus.Tamamlandi
                                 && t.Status != TapshiriqStatus.LegvEdildi)
                        .Count()
                    : 0,
                TamamlananTapshiriqlar = tapshiriqResult.Success
                    ? tapshiriqResult.Data!.Count(t => t.Status == TapshiriqStatus.Tamamlandi)
                    : 0,
                YaxinGorushler = gorushResult.Success
                    ? gorushResult.Data!
                        .Where(g => g.Tarix >= DateTime.Today)
                        .Count()
                    : 0
            };

            ViewData["Title"] = vm.TamAd + " — Kart";
            return View(vm);
        }

        private async Task<int?> GetIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });
    }
}