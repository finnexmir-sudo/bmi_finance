using FinNex.Application.DTOs.Avtopark;
using FinNex.Application.Interfaces.Avtopark;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.Avtopark.Controllers
{
    /// <summary>
    /// Maşın müddətləri (sığorta, texniki baxış, yağ dəyişmə…), müddət növləri
    /// və xəbərdarlıq alıcıları — Admin.
    ///
    /// FAYL SAXLAMA: sənədlər `C:\FinNex_DMS\avtopark\` qovluğuna yazılır,
    /// bazada YALNIZ nisbi yol saxlanılır. `wwwroot`-a fayl YAZILMIR — publish
    /// zamanı silinir (CLAUDE.md — sənəd saxlama qaydası).
    /// </summary>
    [Area("Avtopark")]
    [Authorize(Roles = RoleNames.Admin)]
    public class MuddetController : AvtoparkControllerBase
    {
        private readonly IMasinMuddetService _muddet;
        private readonly IMasinService _masin;
        private readonly IIsciService _isci;
        private readonly IConfiguration _config;

        /// <summary>DMS-də bu modulun alt qovluğu — nisbi yolun prefiksi.</summary>
        private const string DmsQovluq = "avtopark";

        /// <summary>İcazə verilən sənəd uzantıları — icra olunan fayl yüklənməsin.</summary>
        private static readonly string[] IcazeliUzantilar =
            { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };

        private const long MaxFaylBayt = 10 * 1024 * 1024;   // 10 MB

        public MuddetController(
            IMasinMuddetService muddet,
            IMasinService masin,
            IIsciService isci,
            IConfiguration config,
            UserManager<AppUser> userManager) : base(userManager)
        {
            _muddet = muddet;
            _masin = masin;
            _isci = isci;
            _config = config;
        }

        // ══ MÜDDƏT QEYDLƏRİ ═══════════════════════════════════════════════

        public async Task<IActionResult> Index(int? masinId, bool tarixce = false)
        {
            ViewBag.MasinId = masinId;
            ViewBag.Tarixce = tarixce;
            await DoldurAsync();

            var list = await _muddet.HamisiniGetirAsync(masinId, yalnizAktiv: !tarixce);
            return View(list);
        }

        /// <summary>Bitməsinə yaxınlaşanlar + vaxtı keçmişlər.</summary>
        public async Task<IActionResult> Yaxinlasanlar(int gun = 30)
        {
            ViewBag.Gun = gun;
            var list = await _muddet.YaxinlasanlarAsync(gun);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? masinId)
        {
            await DoldurAsync();
            return View(new MasinMuddetCreateDto
            {
                MasinId = masinId ?? 0,
                SonTarix = DateTime.Today.AddYears(1)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MasinMuddetCreateDto dto, IFormFile? sened)
        {
            if (!ModelState.IsValid)
            {
                await DoldurAsync();
                return View(dto);
            }

            // Fayl QEYDDƏN ƏVVƏL yazılır: fayl xətası olsa qeyd ümumiyyətlə
            // yaranmasın. Əksi olsaydı bazada sənədsiz sətir qalardı.
            var faylNetice = await FayliYazAsync(sened);
            if (!faylNetice.ugurlu)
            {
                ModelState.AddModelError(string.Empty, faylNetice.xeta ?? "Fayl yüklənmədi.");
                await DoldurAsync();
                return View(dto);
            }

            dto.SenedFaylYolu = faylNetice.yol;
            dto.SenedFaylAdi = faylNetice.ad;

            var res = await _muddet.YaratAsync(dto, GetUserId());
            if (!res.Success)
            {
                // TempData YOX — layout onu `.fn-alert` kimi göstərir və
                // `user-area.js` 4 saniyəyə silir; istifadəçi səbəbi oxumağa
                // macal tapmır (CLAUDE.md — «submit heç nə etmir» tələsi).
                // ModelState isə qalıcı validasiya xülasəsində qalır.
                ModelState.AddModelError(string.Empty, res.Message ?? "Əməliyyat alınmadı.");
                await DoldurAsync();
                return View(dto);
            }

            TempData["Success"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _muddet.GetirAsync(id);
            if (m == null) return NotFound();

            await DoldurAsync();
            ViewBag.MovcudFaylAdi = m.SenedFaylAdi;
            ViewBag.MovcudFaylYolu = m.SenedFaylYolu;

            return View(new MasinMuddetCreateDto
            {
                Id = m.Id,
                MasinId = m.MasinId,
                NovId = m.NovId,
                SonTarix = m.SonTarix,
                XeberdarliqGun = m.XeberdarliqGun,
                Mebleg = m.Mebleg,
                Qeyd = m.Qeyd
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MasinMuddetCreateDto dto, IFormFile? sened)
        {
            if (!ModelState.IsValid)
            {
                await DoldurAsync();
                return View(dto);
            }

            var faylNetice = await FayliYazAsync(sened);
            if (!faylNetice.ugurlu)
            {
                ModelState.AddModelError(string.Empty, faylNetice.xeta ?? "Fayl yüklənmədi.");
                await DoldurAsync();
                return View(dto);
            }

            // Fayl seçilməyibsə `yol` NULL qalır və servis mövcud sənədə
            // TOXUNMUR. Şərtsiz yazsaq redaktə sənədi səssizcə silərdi.
            dto.SenedFaylYolu = faylNetice.yol;
            dto.SenedFaylAdi = faylNetice.ad;

            var res = await _muddet.YenileAsync(dto, GetUserId());
            if (!res.Success)
            {
                // TempData YOX — layout onu `.fn-alert` kimi göstərir və
                // `user-area.js` 4 saniyəyə silir; istifadəçi səbəbi oxumağa
                // macal tapmır (CLAUDE.md — «submit heç nə etmir» tələsi).
                // ModelState isə qalıcı validasiya xülasəsində qalır.
                ModelState.AddModelError(string.Empty, res.Message ?? "Əməliyyat alınmadı.");
                await DoldurAsync();
                return View(dto);
            }

            TempData["Success"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Uzat(int id, DateTime yeniSonTarix, decimal? mebleg, string? qeyd)
        {
            var res = await _muddet.UzatAsync(id, yeniSonTarix, mebleg, qeyd, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _muddet.SilAsync(id, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        // ══ NÖVLƏR ════════════════════════════════════════════════════════

        public async Task<IActionResult> Novler()
        {
            var list = await _muddet.NovleriGetirAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovYarat(MasinMuddetNovuDto dto)
        {
            var res = await _muddet.NovYaratAsync(dto, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Novler));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovYenile(MasinMuddetNovuDto dto)
        {
            var res = await _muddet.NovYenileAsync(dto, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Novler));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovSil(int id)
        {
            var res = await _muddet.NovSilAsync(id, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Novler));
        }

        // ══ XƏBƏRDARLIQ ALICILARI ═════════════════════════════════════════

        public async Task<IActionResult> Alicilar()
        {
            var isciler = await _isci.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv && !x.Silinib, izlemeden: true);

            ViewBag.Isciler = isciler.Success && isciler.Data != null
                ? isciler.Data
                    .OrderBy(x => x.Sira).ThenBy(x => x.TamAd)
                    .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                    .ToList()
                : new List<SelectListItem>();

            var list = await _muddet.AlicilarAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AliciElaveEt(int isciId)
        {
            var res = await _muddet.AliciElaveEtAsync(isciId, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Alicilar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AliciSil(int id)
        {
            var res = await _muddet.AliciSilAsync(id, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Alicilar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AliciAktivlik(int id, bool aktivdir)
        {
            var res = await _muddet.AliciAktivlikDeyisAsync(id, aktivdir, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Alicilar));
        }

        // ══ KÖMƏKÇİLƏR ════════════════════════════════════════════════════

        private async Task DoldurAsync()
        {
            var masinlar = await _masin.HamisiniGetirAsync();
            ViewBag.Masinlar = masinlar
                .Select(m => new SelectListItem(m.TamAd, m.Id.ToString()))
                .ToList();

            var novler = await _muddet.NovleriGetirAsync(yalnizAktiv: true);
            ViewBag.Novler = novler
                .Select(n => new SelectListItem(n.Ad, n.Id.ToString()))
                .ToList();

            // Yeni qeyd formasında növ seçiləndə xəbərdarlıq günü avtomatik
            // dolsun deyə növ→gün xəritəsi JS-ə ötürülür.
            ViewBag.NovXeberdarliqGun = novler.ToDictionary(n => n.Id, n => n.XeberdarliqGun);
        }

        /// <summary>
        /// Sənədi DMS-ə yazır. Fayl seçilməyibsə (<c>null</c>) uğurlu sayılır və
        /// yol boş qalır — «fayl yoxdur» xəta deyil.
        /// </summary>
        private async Task<(bool ugurlu, string? yol, string? ad, string? xeta)> FayliYazAsync(IFormFile? fayl)
        {
            if (fayl == null || fayl.Length == 0)
                return (true, null, null, null);

            if (fayl.Length > MaxFaylBayt)
                return (false, null, null, "Fayl 10 MB-dan böyük ola bilməz.");

            var uzanti = Path.GetExtension(fayl.FileName).ToLowerInvariant();
            if (!IcazeliUzantilar.Contains(uzanti))
                return (false, null, null,
                    $"«{uzanti}» faylı qəbul edilmir. İcazəli formatlar: {string.Join(", ", IcazeliUzantilar)}.");

            try
            {
                var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
                var qovluq = Path.Combine(dmsRoot, DmsQovluq);
                Directory.CreateDirectory(qovluq);

                var faylAdi = $"{Guid.NewGuid()}{uzanti}";
                await using var fs = new FileStream(Path.Combine(qovluq, faylAdi), FileMode.Create);
                await fayl.CopyToAsync(fs);

                // Bazada YALNIZ nisbi yol — DMS kökü konfiqurasiyadan gəlir və
                // serverdən-serverə dəyişə bilir.
                return (true, $"{DmsQovluq}/{faylAdi}", fayl.FileName, null);
            }
            catch (Exception ex)
            {
                return (false, null, null, $"Fayl yazılmadı: {ex.Message}");
            }
        }
    }
}
