using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class TelimController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public TelimController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── GET /HR/Telim ────────────────────────────────────────
        public async Task<IActionResult> Index(string? tab)
        {
            ViewBag.ActiveTab = tab ?? "telimler";
            ViewData["Title"] = "T\u0259lim v\u0259 \u0130nki\u015faf";

            // Sertifikatlar tab ucun
            var sertifikatlar = await _unitOfWork.Repository<Sertifikat>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                .OrderByDescending(x => x.VerilmeTarixi)
                .ToListAsync();

            ViewBag.Sertifikatlar = sertifikatlar;

            // Isci siyahisi sertifikat yaratma ucun
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .OrderBy(x => x.Soyad)
                .ToListAsync();

            ViewBag.Isciler = isciler
                .Select(x => new SelectListItem($"{x.Soyad} {x.Ad}", x.Id.ToString()))
                .ToList();

            return View();
        }

        // ── GET /HR/Telim/GetData ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            var telimler = await _unitOfWork.Repository<Telim>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Ishtirakcilar)
                .OrderByDescending(x => x.BaslamaTarixi)
                .ToListAsync();

            var data = telimler.Select(x => new
            {
                x.Id,
                x.Ad,
                x.Tesviqci,
                x.Mekan,
                BaslamaTarixi = x.BaslamaTarixi.ToString("dd.MM.yyyy"),
                BitmeTarixi = x.BitmeTarixi?.ToString("dd.MM.yyyy") ?? "\u2014",
                MuddetSaat = x.MuddетSaat,
                Status = (int)x.Status,
                StatusAd = StatusAdi(x.Status),
                x.DaxiliTelimdir,
                Xerc = x.Xerc?.ToString("N2") ?? "\u2014",
                IshtirakciSayi = x.Ishtirakcilar.Count
            });

            return Json(data);
        }

        // ── GET /HR/Telim/Create ─────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await TelimFormSiyahilariDoldur();
            ViewData["Title"] = "Yeni T\u0259lim";
            return View();
        }

        // ── POST /HR/Telim/Create ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string ad, string? tesvir, string? tesviqci,
            string? mekan, DateTime baslamaTarixi, DateTime? bitmeTarixi,
            int muddetSaat, bool daxiliTelimdir, decimal? xerc, List<int>? ishtirakcilar)
        {
            if (string.IsNullOrWhiteSpace(ad))
            {
                TempData["Error"] = "T\u0259lim ad\u0131 bo\u015f ola bilm\u0259z.";
                await TelimFormSiyahilariDoldur();
                return View();
            }

            var telim = new Telim
            {
                Ad = ad.Trim(),
                Tesvir = tesvir?.Trim(),
                Tesviqci = tesviqci?.Trim(),
                Mekan = mekan?.Trim(),
                BaslamaTarixi = baslamaTarixi,
                BitmeTarixi = bitmeTarixi,
                MuddетSaat = muddetSaat,
                DaxiliTelimdir = daxiliTelimdir,
                Xerc = xerc,
                Status = TelimStatus.Planlanib
            };

            await _unitOfWork.Repository<Telim>().YaratAsync(telim);
            await _unitOfWork.YaddaSaxlaAsync();

            // Ishtirakcilar elave et
            if (ishtirakcilar != null && ishtirakcilar.Any())
            {
                foreach (var isciId in ishtirakcilar)
                {
                    var ishtiraki = new TelimIshtiraki
                    {
                        TelimId = telim.Id,
                        IsciId = isciId,
                        Istirakdir = false
                    };
                    await _unitOfWork.Repository<TelimIshtiraki>().YaratAsync(ishtiraki);
                }
                await _unitOfWork.YaddaSaxlaAsync();
            }

            TempData["Success"] = "T\u0259lim u\u011furla yarad\u0131ld\u0131.";
            return RedirectToAction(nameof(Detal), new { id = telim.Id });
        }

        // ── GET /HR/Telim/Detal/5 ────────────────────────────────
        public async Task<IActionResult> Detal(int id)
        {
            var telim = await _unitOfWork.Repository<Telim>()
                .Query()
                .Where(x => x.Id == id && !x.Silinib)
                .Include(x => x.Ishtirakcilar)
                    .ThenInclude(i => i.Isci)
                        .ThenInclude(isc => isc.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                            .ThenInclude(t => t.Departament)
                .FirstOrDefaultAsync();

            if (telim == null)
            {
                TempData["Error"] = "T\u0259lim tap\u0131lmad\u0131.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"T\u0259lim \u2014 {telim.Ad}";
            return View(telim);
        }

        // ── POST /HR/Telim/SertifikatCreate ──────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SertifikatCreate(int isciId, string ad,
            string? verenQurum, DateTime verilmeTarixi, DateTime? bitirmeTarixi)
        {
            if (string.IsNullOrWhiteSpace(ad) || isciId <= 0)
            {
                TempData["Error"] = "\u0130\u015f\u00e7i v\u0259 sertifikat ad\u0131 se\u00e7ilm\u0259lidir.";
                return RedirectToAction(nameof(Index), new { tab = "sertifikatlar" });
            }

            var sertifikat = new Sertifikat
            {
                IsciId = isciId,
                Ad = ad.Trim(),
                VerenQurum = verenQurum?.Trim(),
                VerilmeTarixi = verilmeTarixi,
                BitirmeTarixi = bitirmeTarixi
            };

            await _unitOfWork.Repository<Sertifikat>().YaratAsync(sertifikat);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Sertifikat u\u011furla \u0259lav\u0259 edildi.";
            return RedirectToAction(nameof(Index), new { tab = "sertifikatlar" });
        }

        // ── K\u00f6m\u0259k\u00e7il\u0259r ──────────────────────────────────────
        private string StatusAdi(TelimStatus s) => s switch
        {
            TelimStatus.Planlanib => "Planlan\u0131b",
            TelimStatus.Davam => "Davam edir",
            TelimStatus.Tamamlandi => "Tamamland\u0131",
            TelimStatus.LegvEdildi => "L\u0259\u011fv edildi",
            _ => "\u2014"
        };

        private async Task TelimFormSiyahilariDoldur()
        {
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .OrderBy(x => x.Soyad)
                .ToListAsync();

            ViewBag.Isciler = isciler
                .Select(x => new SelectListItem($"{x.Soyad} {x.Ad}", x.Id.ToString()))
                .ToList();
        }
    }
}
