using FinNex.Application.DTOs.HR.Ezamiyyet;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.HR.ViewModels.Ezamiyyet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = "Admin,HR,Rehber,SobeReisi")]
    public class EzamiyyetController : Controller
    {
        private readonly IEzamiyyetService _service;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _uow;
        private readonly IBildirisRouter _bildirisRouter;

        public EzamiyyetController(
            IEzamiyyetService service,
            UserManager<AppUser> userManager,
            IUnitOfWork uow,
            IBildirisRouter bildirisRouter)
        {
            _service        = service;
            _userManager    = userManager;
            _uow            = uow;
            _bildirisRouter = bildirisRouter;
        }

        // ── Müraciət siyahısı (təsdiq paneli) ───────────────

        public async Task<IActionResult> Index()
        {
            var filtr = new EzamiyyetFiltrDto();
            var list  = await _service.HamisiniGetirAsync(filtr);
            var departamentler = await _uow.Repository<Departament>()
                .Query().AsNoTracking().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();
            var mekanlar = await _service.MekanlarAsync();
            ViewBag.Departamentler = departamentler;
            ViewBag.Mekanlar       = mekanlar;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetMuracietler(
            int? isciId, string? isciAd, int? mekanId, int? status, DateTime? baslangic, DateTime? son, int? departamentId)
        {
            var filtr = new EzamiyyetFiltrDto
            {
                IsciId         = isciId,
                IsciAd         = isciAd,
                MekanId        = mekanId,
                Status         = status.HasValue ? (EzamiyyetStatus?)status.Value : null,
                BaslangicTarix = baslangic,
                SonTarix       = son,
                DepartamentId  = departamentId
            };
            var list = await _service.HamisiniGetirAsync(filtr);
            return Json(list.Select(x => new
            {
                x.Id, x.IsciTamAd, x.IsciVezife, x.Baslig, x.MekanAd,
                baslamaTarixi  = x.BaslamaTarixi.ToString("dd.MM.yyyy"),
                bitmeTarixi    = x.BitmeTarixi.ToString("dd.MM.yyyy"),
                baslamaSaati   = x.BaslamaSaati.HasValue ? x.BaslamaSaati.Value.ToString(@"hh\:mm") : null,
                bitisSaati     = x.BitisSaati.HasValue   ? x.BitisSaati.Value.ToString(@"hh\:mm")   : null,
                x.GunSayi, x.TamGun,
                status         = (int)x.Status,
                x.SenedYolu, x.SenedAd,
                x.Qeyd, x.RehberTamAd,
                rehberTesdiqTarixi = x.RehberTesdiqTarixi?.ToString("dd.MM.yyyy HH:mm"),
                x.RehberQeydi, x.GeriDonusQeydi,
                cihazCixisVaxti   = x.CihazCixisVaxti?.ToString("dd.MM.yyyy HH:mm"),
                cihazQayidisVaxti = x.CihazQayidisVaxti?.ToString("dd.MM.yyyy HH:mm"),
                cihazCixisIso     = x.CihazCixisVaxti?.ToString("yyyy-MM-ddTHH:mm"),
                cihazQayidisIso   = x.CihazQayidisVaxti?.ToString("yyyy-MM-ddTHH:mm"),
                bitmeIso          = x.BitmeTarixi.ToString("yyyy-MM-dd"),
                yaradilmaTarixi   = x.YaradilmaTarixi.ToString("dd.MM.yyyy HH:mm")
            }));
        }

        // ── İşçi-qruplu izləmə (rəhbər/HR baxışı: cəmi, gün, faktiki saat + detay) ──
        public async Task<IActionResult> IsciIzleme(
            DateTime? tarixFrom, DateTime? tarixTo, int? departamentId,
            string? axtaris, int? status, string? sirala)
        {
            var filtr = new EzamiyyetFiltrDto
            {
                BaslangicTarix = tarixFrom,
                SonTarix       = tarixTo,
                DepartamentId  = departamentId,
                IsciAd         = string.IsNullOrWhiteSpace(axtaris) ? null : axtaris.Trim(),
                Status         = status.HasValue ? (EzamiyyetStatus?)status.Value : null
            };

            var isciler = (await _service.GetIsciEzamIzlemeAsync(filtr)).ToList();

            var s = string.IsNullOrEmpty(sirala) ? "cemi" : sirala;
            isciler = s switch
            {
                "gun"     => isciler.OrderByDescending(x => x.CemiGun).ToList(),
                "faktiki" => isciler.OrderByDescending(x => x.FaktikiSaat).ToList(),
                _         => isciler.OrderByDescending(x => x.CemiEzam).ToList(),
            };

            var departamentler = await _uow.Repository<Departament>()
                .Query().AsNoTracking().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();

            var vm = new EzamiyyetIzlemeVM
            {
                Isciler = isciler,
                Filtr   = filtr,
                Departamentler = departamentler
                    .Select(d => new SelectListItem(d.Ad, d.Id.ToString()))
                    .Prepend(new SelectListItem("Bütün şöbələr", ""))
                    .ToList()
            };
            ViewData["Sirala"] = s;
            return View(vm);
        }

        // ── Rəhbər təsdiq ────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> Tesdiq([FromBody] EzamTesdiqDto dto)
        {
            var rehberId = await GetIsciIdAsync();

            // Bildiriş üçün işçinin ID-sini öncədən götür
            var detay = await _service.DetayAsync(dto.Id);

            var (ok, error) = await _service.RehberTesdiqAsync(
                dto.Id, dto.Tesdiq, dto.Qeyd, rehberId ?? 0);

            if (ok && detay != null)
            {
                var nov  = dto.Tesdiq ? BildirisNovu.EzamiyyetTesdiq : BildirisNovu.EzamiyyetImtina;
                var metn = dto.Tesdiq
                    ? $"Ezamiyyət müraciətiniz ({detay.BaslamaTarixi:dd.MM.yyyy} – {detay.BitmeTarixi:dd.MM.yyyy}) təsdiqləndi."
                    : $"Ezamiyyət müraciətiniz ({detay.BaslamaTarixi:dd.MM.yyyy} – {detay.BitmeTarixi:dd.MM.yyyy}) rədd edildi.{(string.IsNullOrWhiteSpace(dto.Qeyd) ? "" : " Səbəb: " + dto.Qeyd)}";

                await _bildirisRouter.NotifyIsciAsync(
                    detay.IsciId,
                    nov,
                    dto.Tesdiq ? "Ezamiyyət təsdiqləndi" : "Ezamiyyət rədd edildi",
                    metn,
                    redirectUrl: Url.Action("Index", "EzamiyyetMuraciet", new { area = "User" }));
            }

            return Json(new { success = ok, message = error });
        }

        // ── HR əl ilə cihaz çıxış/qayıdış düzəlişi (insan faktoru) ──
        // "qayıtmayıb" və ya səhər icazəsi yanlış oxunan qeydləri HR əl ilə düzəldir.
        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.HR}")]
        public async Task<IActionResult> CihazQayidisDuzelt([FromBody] EzamCihazDuzeltDto dto)
        {
            DateTime? cixis   = DateTime.TryParse(dto.CixisVaxt,   out var c) ? c : (DateTime?)null;
            DateTime? qayidis = DateTime.TryParse(dto.QayidisVaxt, out var q) ? q : (DateTime?)null;

            var (ok, error) = await _service.CihazQayidisDuzeltAsync(dto.Id, cixis, qayidis);
            return Json(new { success = ok, message = ok ? "Cihaz çıxış/qayıdış vaxtı yeniləndi." : error });
        }

        // ── Statistika ────────────────────────────────────────

        public async Task<IActionResult> Statistika()
        {
            var departamentler = await _uow.Repository<Departament>()
                .Query().AsNoTracking().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();
            var mekanlar = await _service.MekanlarAsync();
            ViewBag.Departamentler = departamentler;
            ViewBag.Mekanlar       = mekanlar;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistika(
            DateTime? baslangic, DateTime? son, int? departamentId, int? mekanId)
        {
            var filtr = new EzamiyyetFiltrDto
            {
                BaslangicTarix = baslangic,
                SonTarix       = son,
                DepartamentId  = departamentId,
                MekanId        = mekanId
            };
            var stats = await _service.StatistikaAsync(filtr);
            return Json(stats);
        }

        [HttpGet]
        public async Task<IActionResult> GetAktivler()
        {
            var filtr = new EzamiyyetFiltrDto
            {
                Status         = EzamiyyetStatus.Tesdiqlendi,
                BaslangicTarix = DateTime.Today,
                SonTarix       = DateTime.Today
            };
            var list = await _service.HamisiniGetirAsync(filtr);
            return Json(list.Select(x => new
            {
                x.IsciTamAd, x.Baslig, x.MekanAd,
                baslamaTarixi = x.BaslamaTarixi.ToString("dd.MM.yyyy"),
                bitmeTarixi   = x.BitmeTarixi.ToString("dd.MM.yyyy"),
                baslamaSaati  = x.BaslamaSaati.HasValue ? x.BaslamaSaati.Value.ToString(@"hh\:mm") : null,
                bitisSaati    = x.BitisSaati.HasValue   ? x.BitisSaati.Value.ToString(@"hh\:mm")   : null
            }));
        }

        // ── Məkan idarəetməsi ─────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetMekanlar()
        {
            var list = await _service.MekanlarAsync();
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> MekanEkle([FromBody] string ad)
        {
            if (string.IsNullOrWhiteSpace(ad))
                return Json(new { success = false, message = "Ad boş ola bilməz." });
            var mekan = await _service.YeniMekanYaratAsync(ad.Trim());
            return Json(new { success = true, id = mekan!.Id, ad = mekan.Ad });
        }

        private async Task<int?> GetIsciIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.IsciId;
        }
    }

    public class EzamTesdiqDto
    {
        public int    Id     { get; set; }
        public bool   Tesdiq { get; set; }
        public string? Qeyd  { get; set; }
    }

    public class EzamCihazDuzeltDto
    {
        public int     Id          { get; set; }
        public string? CixisVaxt   { get; set; }
        public string? QayidisVaxt { get; set; }
    }
}
