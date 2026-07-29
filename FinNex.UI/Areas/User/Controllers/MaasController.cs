using FinNex.Application.DTOs.HR.Maas;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class MaasController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public MaasController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ── GET /User/Maas (köhnə bildiriş linklər üçün) ───────
        public IActionResult Index() => RedirectToAction(nameof(Tarixce));

        // ── GET /User/Maas/Tarixce ──────────────────────────
        public IActionResult Tarixce()
        {
            ViewData["Title"] = "Əmək haqqı tarixçəsi";
            return View();
        }

        // ── GET /User/Maas/GetTarixceData ───────────────────
        [HttpGet]
        public async Task<IActionResult> GetTarixceData()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return Json(new { success = false, message = "İşçi tapılmadı." });

            var isciId = appUser.IsciId.Value;

            // Son 12 ay
            var now = DateTime.Now;
            var son12Ay = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            var sonIl = son12Ay.Year;
            var sonAy = son12Ay.Month;

            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib &&
                            x.IsciId == isciId &&
                            (x.Il > sonIl || (x.Il == sonIl && x.Ay >= sonAy)))
                .OrderBy(x => x.Il)
                .ThenBy(x => x.Ay)
                .ToListAsync();

            var ayAdlar = new[]
            {
                "", "Yan", "Fev", "Mar", "Apr", "May", "İyn",
                "İyl", "Avq", "Sen", "Okt", "Noy", "Dek"
            };

            // Qabaqcadan ödənilmiş məzuniyyət — ödəniş ayına görə (brüt, net). Ayrıca ödənilib
            // və adi maaşdan çıxılıb; başlıqda ayın TAM gross/net-i görünsün deyə geri əlavə
            // edirik. Mənbə provodka ilə eynidir (Mezuniyyet entity) — həm köhnə, həm yeni datada işləyir.
            var mezByMonth = await MezQabaqAylikMapAsync(isciId, sonIl, sonAy);

            var data = maaslar.Select(m =>
            {
                var mez = mezByMonth.TryGetValue((m.Il, m.Ay), out var v) ? v : (Brut: 0m, Net: 0m);
                return new
                {
                    etiket = ayAdlar[m.Ay] + " " + m.Il,
                    brut = m.BrutMebleg + mez.Brut,
                    net = m.NetMebleg + AvansMebleginiTap(m.HesablamaIzahi) + mez.Net,
                    il = m.Il,
                    ay = m.Ay
                };
            }).ToList();

            return Json(new { success = true, data });
        }

        // Avans işçinin öz maaşıdır (qabaqcadan ödənilib) — işçinin səhifəsində "tutulma" kimi
        // göstərilmir, net də avans çıxılmadan göstərilir. (Hesablama/ödəniş/provodka dəyişmir.)
        private static decimal AvansMebleginiTap(string? hesablamaIzahi)
        {
            if (string.IsNullOrEmpty(hesablamaIzahi)) return 0m;
            try
            {
                var list = JsonSerializer.Deserialize<List<HesablamaIzahiDto>>(hesablamaIzahi);
                return list?.FirstOrDefault(x => x.Addim == "Avans Kəsintisi")?.Mebleg ?? 0m;
            }
            catch { return 0m; }
        }

        // Qabaqcadan ödənilmiş məzuniyyət — ödəniş ayına görə (brüt, net) map.
        // Provodka ilə eyni mənbə (Mezuniyyet.OdenenMeblegBrut / OdenenMebleg). Məzuniyyət
        // ayrıca ödənilib və adi maaşdan çıxılıb; başlıqda ayın TAM gross/net-i üçün geri əlavə olunur.
        private async Task<Dictionary<(int Il, int Ay), (decimal Brut, decimal Net)>> MezQabaqAylikMapAsync(
            int isciId, int sonIl, int sonAy)
        {
            var qeydler = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x => !x.Silinib && x.IsciId == isciId
                    && x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                    && x.OdenisStatus == MezuniyyetOdenisStatus.Odenilib
                    && x.OdenilmeTarixi.HasValue
                    && (x.OdenilmeTarixi.Value.Year > sonIl
                        || (x.OdenilmeTarixi.Value.Year == sonIl && x.OdenilmeTarixi.Value.Month >= sonAy)))
                .Select(x => new { x.OdenilmeTarixi, Brut = x.OdenenMeblegBrut ?? 0m, Net = x.OdenenMebleg ?? 0m })
                .ToListAsync();

            return qeydler
                .GroupBy(x => (Il: x.OdenilmeTarixi!.Value.Year, Ay: x.OdenilmeTarixi.Value.Month))
                .ToDictionary(g => g.Key, g => (Brut: g.Sum(x => x.Brut), Net: g.Sum(x => x.Net)));
        }

        // ── GET /User/Maas/GetDetay?il=2026&ay=4 ────────────
        [HttpGet]
        public async Task<IActionResult> GetDetay(int il, int ay)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return Json(new { success = false });

            var maas = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib && x.IsciId == appUser.IsciId.Value && x.Il == il && x.Ay == ay)
                .FirstOrDefaultAsync();

            if (maas == null)
                return Json(new { success = false, message = "Məlumat tapılmadı." });

            var tamSiyahi = string.IsNullOrEmpty(maas.HesablamaIzahi)
                ? new List<HesablamaIzahiDto>()
                : (JsonSerializer.Deserialize<List<HesablamaIzahiDto>>(maas.HesablamaIzahi)
                    ?? new List<HesablamaIzahiDto>());

            // Avans işçinin öz maaşıdır (qabaqcadan ödənilib) — işçinin səhifəsində tutulma kimi
            // göstərmirik; net da avans çıxılmadan (qazanılan net) verilir. Ödəniş/provodka dəyişmir.
            var avansMebleg = tamSiyahi
                .Where(x => x.Addim == "Avans Kəsintisi")
                .Sum(x => x.Mebleg);
            var addimlar = tamSiyahi
                .Where(x => x.Addim != "Avans Kəsintisi")
                .Select(x => (object)new { addim = x.Addim, izah = x.Izah, mebleg = x.Mebleg, tip = x.Tip })
                .ToList();

            return Json(new { success = true, il, ay, brut = maas.BrutMebleg, net = maas.NetMebleg + avansMebleg, addimlar });
        }

        // ── GET /User/Maas/HYS ─────────────────────────────────
        // İşçi öz HYS təyinatlarını görür (read-only)
        public async Task<IActionResult> HYS()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
            {
                TempData["Error"] = "İşçi məlumatı tapılmadı.";
                return RedirectToAction("Index", "Dashboard");
            }

            var isciId = appUser.IsciId.Value;
            var today = DateTime.Today;

            var hysList = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x => !x.Silinib && x.IsciId == isciId)
                .OrderByDescending(x => x.BaslamaTarixi)
                .ToListAsync();

            ViewData["Title"] = "Həyat Yığım Sığortası (HYS)";
            return View(hysList);
        }
    }
}
