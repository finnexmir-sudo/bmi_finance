using FinNex.Application.DTOs.Kassa;
using FinNex.Application.Interfaces.Kassa;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.Kassa.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Kassa.Controllers
{
    /// <summary>
    /// Kassanın günlük valyuta alış-satış kursunu daxil etdiyi ekran — BMI
    /// "frmExchange" modulunun köçürülməsi (09.10.2026). Yalnız RoleNames.Kassa
    /// roluna sahib işçilər (+ Admin) buraya giriş yaza bilər — Avtopark →
    /// Açar jurnalı ilə EYNİ rol, istifadəçinin öz qərarı (09.10.2026).
    /// </summary>
    [Area("Kassa")]
    [Authorize(Roles = RoleNames.Kassa + "," + RoleNames.Admin)]
    public class ExchangeController : Controller
    {
        private readonly IKassaKursService _kassaKursService;
        private readonly UserManager<AppUser> _userManager;

        public ExchangeController(IKassaKursService kassaKursService, UserManager<AppUser> userManager)
        {
            _kassaKursService = kassaKursService;
            _userManager = userManager;
        }

        // GET /Kassa/Exchange?tarix=2026-10-09
        public async Task<IActionResult> Index(DateTime? tarix)
        {
            ViewData["Title"] = "Exchange — Valyuta kursu";

            var secilenTarix = (tarix ?? DateTime.Today).Date;
            var vm = new ExchangeIndexVM
            {
                Gunluk = await _kassaKursService.GunlukGetirAsync(secilenTarix),
                SonQeydler = await _kassaKursService.SonBeyannameleriGetirAsync()
            };

            return View(vm);
        }

        // POST /Kassa/Exchange/Saxla
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Saxla(ExchangeSaxlaVM vm)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
            {
                TempData["Error"] = "İşçi profiliniz tapılmadı.";
                return RedirectToAction(nameof(Index), new { tarix = vm.Tarix });
            }

            var dto = new KassaKursSaxlaDto
            {
                Tarix = vm.Tarix,
                Setirler = new List<KassaKursSetriDto>
                {
                    new() { Valyuta = "USD",  NagdAlis = vm.USD_NA,  NagdSatis = vm.USD_NS,  QeyriNagdAlis = vm.USD_QNA,  QeyriNagdSatis = vm.USD_QNS },
                    new() { Valyuta = "AVRO", NagdAlis = vm.AVRO_NA, NagdSatis = vm.AVRO_NS, QeyriNagdAlis = vm.AVRO_QNA, QeyriNagdSatis = vm.AVRO_QNS },
                    new() { Valyuta = "IRR",  NagdAlis = vm.IRR_NA,  NagdSatis = vm.IRR_NS,  QeyriNagdAlis = vm.IRR_QNA,  QeyriNagdSatis = vm.IRR_QNS },
                    new() { Valyuta = "AED",  NagdAlis = vm.AED_NA,  NagdSatis = vm.AED_NS,  QeyriNagdAlis = vm.AED_QNA,  QeyriNagdSatis = vm.AED_QNS },
                    new() { Valyuta = "RUB",  NagdAlis = vm.RUB_NA,  NagdSatis = vm.RUB_NS,  QeyriNagdAlis = vm.RUB_QNA,  QeyriNagdSatis = vm.RUB_QNS },
                }
            };

            var sonuc = await _kassaKursService.SaxlaAsync(dto, isciId.Value);
            // User-area ortaq layout TempData["Success"]/["Error"] gözləyir
            // (_UserLayout.cshtml) — "StatusMessage" YAZMA, görünməz qalar.
            if (sonuc.Success)
                TempData["Success"] = sonuc.Message;
            else
                TempData["Error"] = sonuc.Message;

            return RedirectToAction(nameof(Index), new { tarix = vm.Tarix });
        }

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }
    }
}
