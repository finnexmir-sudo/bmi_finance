// Areas/HR/Controllers/DashboardController.cs
using FinNex.Domain;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class DashboardController : Controller
    {
        private readonly IIsciService _isciService;
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IDavamiyyetService _davamiyyetService;
        private readonly IBildirisService _bildirisService;
        private readonly IUnitOfWork _unitOfWork;

        public DashboardController(
            IIsciService isciService,
            IMezuniyyetService mezuniyyetService,
            IDavamiyyetService davamiyyetService,
            IBildirisService bildirisService,
            IUnitOfWork unitOfWork)
        {
            _isciService = isciService;
            _mezuniyyetService = mezuniyyetService;
            _davamiyyetService = davamiyyetService;
            _bildirisService = bildirisService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var bugun = DateTime.Today;
            var buAyBaslangic = new DateTime(bugun.Year, bugun.Month, 1);

            // ── İşçilər ──────────────────────────────────────
            var iscilerResult = await _isciService.HamisiniGetirAsync();
            var isciler = iscilerResult.Success && iscilerResult.Data != null
                ? iscilerResult.Data.ToList()
                : new();

            var aktivisciSayi = isciler.Count(x => x.Status == IsciStatus.Aktiv);

            ViewBag.UmumiIsci = isciler.Count;
            ViewBag.AktivIsci = aktivisciSayi;
            ViewBag.SonIsciler = isciler
                .OrderByDescending(x => x.Id)
                .Take(5)
                .Select(x => new
                {
                    TamAd = x.TamAd,
                    Departament = x.SobeAdi ?? "—",
                    Vezife = x.VezifeAdi ?? "—",
                    ElaveTarixi = "—"
                })
                .ToList();

            // ── Məzuniyyətlər ─────────────────────────────────
            var mezResult = await _mezuniyyetService.GetListAsync();
            var mezler = mezResult.Success && mezResult.Data != null
                ? mezResult.Data.ToList()
                : new();

            ViewBag.Mezuniyyetde = mezler.Count(x =>
                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                x.BaslamaTarixi.Date <= bugun &&
                x.BitmeTarixi.Date >= bugun);

            ViewBag.MezuniyyetBitir = mezler.Count(x =>
                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                x.BitmeTarixi.Date > bugun &&
                x.BitmeTarixi.Date <= bugun.AddDays(7));

            // ── Bugünün davamiyyəti ───────────────────────────
            var davResult = await _davamiyyetService.TarixUzreAsync(bugun);
            var davlar = davResult?.ToList() ?? new();

            var gelenSayi = davlar.Count(x => x.Status == DavamiyyetStatus.Isde ||
                                                 x.Status == DavamiyyetStatus.Gecikme ||
                                                 x.Status == DavamiyyetStatus.Icazeli ||
                                                 x.Status == DavamiyyetStatus.Xestelik ||
                                                 x.Status == DavamiyyetStatus.Ezamiyyet);
            var gecikenSayi = davlar.Count(x => x.Status == DavamiyyetStatus.Gecikme);
            var cixisEden = davlar.Count(x => x.CixisVaxti != null);

            ViewBag.Gelenler = gelenSayi;
            ViewBag.Gecikenler = gecikenSayi;
            ViewBag.Gecikme = gecikenSayi;
            ViewBag.Mezuniyyetdebugun = (int)ViewBag.Mezuniyyetde;
            ViewBag.CixisEden = cixisEden;

            int gelmeyen = aktivisciSayi - gelenSayi - (int)ViewBag.Mezuniyyetde;
            ViewBag.Gelmeyen = gelmeyen < 0 ? 0 : gelmeyen;

            // ── Davamiyyət faizi (bu ay) ──────────────────────
            var isGunleriSayi = 0;
            for (var d = buAyBaslangic; d <= bugun; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    isGunleriSayi++;

            var buAyDavResult = await _davamiyyetService.AraliqUzreAsync(buAyBaslangic, bugun);
            var buAyDavlar = buAyDavResult?.ToList() ?? new();

            var gelmelerSayi = buAyDavlar.Count(x =>
                x.Status == DavamiyyetStatus.Isde ||
                x.Status == DavamiyyetStatus.Gecikme);
            var maxGelmeMumkun = aktivisciSayi * isGunleriSayi;

            ViewBag.DavamiyyetFaizi = maxGelmeMumkun > 0
                ? (int)((double)gelmelerSayi / maxGelmeMumkun * 100)
                : 0;

            // ── Son girişlər (bugün, ən yeni 15 qeyd) ────────
            ViewBag.SonGirisler = davlar
                .Where(x => x.GirisVaxti != null)
                .OrderByDescending(x => x.GirisVaxti)
                .Take(15)
                .Select(x => new
                {
                    Ad = x.IsciTamAd ?? "—",
                    Departament = x.DepartamentAd ?? "—",
                    GirisVaxti = x.GirisVaxti?.ToString("HH:mm"),
                    CixisVaxti = x.CixisVaxti?.ToString("HH:mm"),
                    Status = (int)x.Status
                })
                .ToList();

            // ── Müqavilə yenilənmə bildirişi (5 gün qalmış) ────
            await MuqavileYenilenmeYoxlaAsync();

            ViewData["Title"] = "HR Dashboard";
            return View();
        }

        /// <summary>
        /// İşə qəbul tarixinin il dönümünə 5 gün qalmış HR-a bildiriş yaradır.
        /// Hər işçi üçün ildə yalnız 1 dəfə bildiriş yaranır (dublikat qoruması).
        /// </summary>
        private async Task MuqavileYenilenmeYoxlaAsync()
        {
            try
            {
                var bugun = DateTime.Today;
                var hedeftarix = bugun.AddDays(5); // 5 gün sonra

                // Aktiv işçiləri gətir
                var isciler = await _unitOfWork.Repository<Isci>()
                    .Query()
                    .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                    .ToListAsync();

                // HR rollu işçiləri tap (bildirişi onlara göndərəcəyik)
                var hrIsciler = await _unitOfWork.Repository<IsciStrukturRolu>()
                    .Query()
                    .Where(x => x.Aktivdir && x.RolTipi == StrukturRolTipi.Hr)
                    .Select(x => x.IsciId)
                    .ToListAsync();

                // HR işçi yoxdursa, bildiriş göndərə bilmərik
                if (!hrIsciler.Any()) return;

                foreach (var isci in isciler)
                {
                    // İl dönümü tarixini hesabla (bu il və ya növbəti il)
                    var ilDonumu = new DateTime(bugun.Year, isci.IsheQebulTarixi.Month, isci.IsheQebulTarixi.Day);
                    if (ilDonumu < bugun)
                        ilDonumu = ilDonumu.AddYears(1);

                    // 5 gün ərzindədirsə
                    var qalangGun = (ilDonumu - bugun).Days;
                    if (qalangGun < 0 || qalangGun > 5) continue;

                    // Bu il üçün artıq bildiriş yaradılıbmı?
                    var artiqVar = await _unitOfWork.Repository<Bildiris>()
                        .MovcuddurmuAsync(x =>
                            x.Nov == BildirisNovu.MuqavileYenilenme &&
                            x.Metn.Contains(isci.Ad) &&
                            x.Metn.Contains(isci.Soyad) &&
                            x.YaradilmaTarixi.Year == bugun.Year);

                    if (artiqVar) continue;

                    var staj = ilDonumu.Year - isci.IsheQebulTarixi.Year;
                    var metn = qalangGun == 0
                        ? $"{isci.Ad} {isci.Soyad} — müqavilə yenilənmə tarixi bu gündür! ({staj} il staj)"
                        : $"{isci.Ad} {isci.Soyad} — müqavilə yenilənməsinə {qalangGun} gün qalıb ({ilDonumu:dd.MM.yyyy}, {staj} il staj)";

                    // Hər HR işçisinə bildiriş göndər
                    foreach (var hrIsciId in hrIsciler)
                    {
                        await _bildirisService.YaratAsync(
                            hrIsciId,
                            BildirisNovu.MuqavileYenilenme,
                            "Müqavilə yenilənməsi",
                            metn,
                            $"/HR/Isci/Detail/{isci.Id}");
                    }
                }
            }
            catch
            {
                // Bildiriş xətası dashboard-ı bloklamamalıdır
            }
        }
    }
}