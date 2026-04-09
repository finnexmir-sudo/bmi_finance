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
            ViewData["Title"] = "Təlim və İnkişaf";

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
                BitmeTarixi = x.BitmeTarixi?.ToString("dd.MM.yyyy") ?? "—",
                MuddetSaat = x.MuddetSaat,
                Status = (int)x.Status,
                StatusAd = StatusAdi(x.Status),
                x.DaxiliTelimdir,
                Xerc = x.Xerc?.ToString("N2") ?? "—",
                IshtirakciSayi = x.Ishtirakcilar.Count
            });

            return Json(data);
        }

        // ── GET /HR/Telim/Create ─────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await TelimFormSiyahilariDoldur();
            ViewData["Title"] = "Yeni Təlim";
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
                TempData["Error"] = "Təlim adı boş ola bilməz.";
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
                MuddetSaat = muddetSaat,
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

            TempData["Success"] = "Təlim uğurla yaradıldı.";
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
                TempData["Error"] = "Təlim tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"Təlim — {telim.Ad}";
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
                TempData["Error"] = "İşçi və sertifikat adı seçilməlidir.";
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

            TempData["Success"] = "Sertifikat uğurla əlavə edildi.";
            return RedirectToAction(nameof(Index), new { tab = "sertifikatlar" });
        }

        // ── Köməkçilər ──────────────────────────────────────
        private string StatusAdi(TelimStatus s) => s switch
        {
            TelimStatus.Planlanib => "Planlanıb",
            TelimStatus.Davam => "Davam edir",
            TelimStatus.Tamamlandi => "Tamamlandı",
            TelimStatus.LegvEdildi => "Ləğv edildi",
            _ => "—"
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
