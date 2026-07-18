using ClosedXML.Excel;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.HR.ViewModels.Mezuniyyet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize]
    public class MezuniyyetController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IBildirisService _bildirisService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly IJetonService _jetonService;

        private static readonly string[] _icazeSenedTipler =
            [".pdf", ".jpg", ".jpeg", ".png"];

        public MezuniyyetController(
            IMezuniyyetService mezuniyyetService,
            IIsciService isciService,
            UserManager<AppUser> userManager,
            IBildirisService bildirisService,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment env,
            IConfiguration config,
            IJetonService jetonService)
        {
            _mezuniyyetService = mezuniyyetService;
            _isciService = isciService;
            _userManager = userManager;
            _bildirisService = bildirisService;
            _unitOfWork = unitOfWork;
            _env = env;
            _config = config;
            _jetonService = jetonService;
        }

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private async Task<int?> GetCurrentDepartamentIdAsync(int isciId)
        {
            var result = await _isciService.GetAktivDepartamentIdAsync(isciId);
            return result.Success ? result.Data : null;
        }

        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> Rehber()
        {
            var result = await _mezuniyyetService.GetRehberTesdiqindeAsync();
            var vm = new HrMezuniyyetIndexVM
            {
                Mezuniyyetler = result.Success ? result.Data!.ToList() : new(),
                PageTitle = "Rəhbər — Gözləyən Müraciətlər",
                TesdiqAction = "RehberTesdiq"
            };
            ViewData["Title"] = "Rəhbər Təsdiqi";
            return View("TesdiqIndex", vm);
        }

        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Hr(string tab = "tesdiq", string? axtaris = null)
        {
            var aktivTab = tab switch
            {
                "proses" => "proses",
                "tarixce" => "tarixce",
                _ => "tesdiq"
            };

            var tesdiqResult = await _mezuniyyetService.GetHrTesdiqindeAsync();
            var prosesResult = await _mezuniyyetService.GetProsesdeOlanlarAsync();
            var tarixceResult = await _mezuniyyetService.GetTarixceAsync(
                aktivTab == "tarixce" ? axtaris : null);

            var tesdiqList = tesdiqResult.Success ? tesdiqResult.Data!.ToList() : new();
            var prosesList = prosesResult.Success ? prosesResult.Data!.ToList() : new();
            var tarixceList = tarixceResult.Success ? tarixceResult.Data!.ToList() : new();

            int tarixceTotalSayi = tarixceList.Count;
            if (aktivTab == "tarixce" && !string.IsNullOrWhiteSpace(axtaris))
            {
                var hamisi = await _mezuniyyetService.GetTarixceAsync(null);
                tarixceTotalSayi = hamisi.Success ? hamisi.Data!.Count : tarixceList.Count;
            }

            var mezuniyyetler = aktivTab switch
            {
                "proses" => prosesList,
                "tarixce" => tarixceList,
                _ => tesdiqList
            };

            var pageTitle = aktivTab switch
            {
                "proses" => "Prosesədə olan müraciətlər",
                "tarixce" => "Təsdiq tarixçəsi",
                _ => "HR — Son Təsdiq"
            };

            var vm = new HrMezuniyyetIndexVM
            {
                Mezuniyyetler = mezuniyyetler,
                PageTitle = pageTitle,
                TesdiqAction = aktivTab == "tesdiq" ? "HrTesdiq" : "",
                AktivTab = aktivTab,
                TesdiqSayi = tesdiqList.Count,
                ProsesSayi = prosesList.Count,
                TarixceSayi = tarixceTotalSayi,
                Axtaris = axtaris
            };

            ViewData["Title"] = aktivTab switch
            {
                "proses" => "HR — İzləmə",
                "tarixce" => "HR — Tarixçə",
                _ => "HR Təsdiqi"
            };
            return View("TesdiqIndex", vm);
        }

        [Authorize(Roles = RoleNames.SobeReisi + "," + RoleNames.Rehber + "," + RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Detal(int id, string returnAction = "Hr")
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success || result.Data == null) return NotFound();

            var overlap = await _mezuniyyetService.GetOverlapMezuniyyetlerAsync(id);
            var konflikt = await _mezuniyyetService.GetEvezediciKonfliktiAsync(id);

            var vm = new HrMezuniyyetDetalVM
            {
                Mezuniyyet = result.Data,
                ReturnAction = returnAction,
                OverlapMezuniyyetler = overlap.Success ? overlap.Data!.ToList() : new(),
                EvezediciKonfliktleri = konflikt.Success ? konflikt.Data!.ToList() : new()
            };

            ViewData["Title"] = "Müraciət Detalı";
            return View("Detail", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> RehberTesdiq(int id, bool status, string? qeyd)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();
            var result = await _mezuniyyetService.RehberTesdiqAsync(id, status, qeyd, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Rehber));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> HrTesdiq(int id, bool status, string? qeyd)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();
            var result = await _mezuniyyetService.HrTesdiqAsync(id, status, qeyd, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Hr));
        }

        // HR düzəliş: təsdiqlənmiş məzuniyyətin ödəniş tipini dəyiş (ay-sonu ↔ qabaqcadan).
        // Avans artıq icra olunubsa servis bloklayır. Detal səhifəsinə qayıdır.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> HrOdenisTipiDeyis(int id, MezuniyyetOdenisTipi yeniTipi)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();
            var result = await _mezuniyyetService.HrOdenisTipiDeyisAsync(id, yeniTipi, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Detal), new { id });
        }

        // HR təsdiqlənmiş məzuniyyəti ləğv edir. Uğur halında qeyd soft-delete olur,
        // ona görə Detal NotFound verər → siyahıya qayıt. Bloklanarsa Detal-da xəta göstər.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> HrLegvEt(int id, string? sebeb)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();
            var result = await _mezuniyyetService.HrLegvEtAsync(id, sebeb, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return result.Success
                ? RedirectToAction(nameof(LegvTelebleri))
                : RedirectToAction(nameof(Detal), new { id });
        }

        // İşçinin ləğv müraciətini HR RƏDD edir — məzuniyyət qalır, bayraq təmizlənir.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> LegvTelebiRedEt(int id, string? sebeb)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();
            var result = await _mezuniyyetService.LegvTelebiRedEtAsync(id, isciId.Value, sebeb);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(LegvTelebleri));
        }

        // HR — gözləyən ləğv müraciətləri siyahısı.
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> LegvTelebleri()
        {
            var result = await _mezuniyyetService.GetLegvTelebleriAsync();
            ViewData["Title"] = "Ləğv Müraciətləri";
            return View("LegvTelebleri", result.Success ? result.Data! : new List<MezuniyyetListDto>());
        }

        // HR təsdiqlənmiş məzuniyyətin tarixlərini dəyişir. Qeyd qalır → Detal-a qayıt.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> HrTarixDeyis(int id, DateTime yeniBaslama, DateTime yeniBitme, string? sebeb)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Forbid();
            var result = await _mezuniyyetService.HrTarixDeyisAsync(id, yeniBaslama, yeniBitme, sebeb, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Detal), new { id });
        }

        // ADMIN: səhv daxil edilmiş məzuniyyətin tarixini düzəldir — məzuniyyət artıq
        // başlayıb/keçmiş olsa belə (HR-in edə bilmədiyi hallar). Yalnız Admin rolu.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> AdminTarixDeyis(int id, DateTime yeniBaslama, DateTime yeniBitme, string? sebeb)
        {
            // Admin hesabı işçiyə bağlı olmaya bilər (IsciId null) — bu, düzəlişi bloklamamalıdır.
            // Rol qoruması yuxarıdakı [Authorize(Roles = Admin)] ilə təmin olunur; buradakı
            // 'isciId == null → Forbid()' yoxlaması səhv idi (admin AccessDenied alırdı).
            // adminId yalnız audit konteksti üçündür (servisdə FK/DB yazısında işlədilmir) — 0 təhlükəsizdir.
            var isciId = await GetCurrentIsciIdAsync();
            var result = await _mezuniyyetService.AdminTarixDeyisAsync(id, yeniBaslama, yeniBitme, sebeb, isciId ?? 0);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Detal), new { id });
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> GeriyeQeyd()
        {
            var vm = new GeriyeQeydVM
            {
                Isciler = await GetAktivIsciSelectListAsync()
            };
            ViewData["Title"] = "Birbaşa Məzuniyyət Qeydi";
            return View(vm);
        }

        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Xeta"] = "Məzuniyyət tapılmadı.";
                return RedirectToAction("Index");
            }

            var m = result.Data;
            var editStatuses = new[]
            {
                MezuniyyetStatus.Gozlemede,
                MezuniyyetStatus.SobeReisiTesdiqinde,
                MezuniyyetStatus.RehberTesdiqinde,
                MezuniyyetStatus.HrTesdiqinde
            };

            if (!editStatuses.Contains(m.Status))
            {
                TempData["Xeta"] = "Yalnız prosesədə olan məzuniyyətlər redaktə edilə bilər.";
                return RedirectToAction("Index");
            }

            var dto = new MezuniyyetUpdateDto
            {
                Id             = m.Id,
                Nov            = m.Nov,
                BaslamaTarixi  = m.BaslamaTarixi,
                BitmeTarixi    = m.BitmeTarixi,
                EvezEdenIsciId = m.EvezEdenIsciId,
                Qeyd           = m.Qeyd,
                Status         = m.Status,
                ImtinaSebebi   = m.ImtinaSebebi
            };
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MezuniyyetUpdateDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var result = await _mezuniyyetService.YenileAsync(dto);
            if (!result.Success)
            {
                TempData["Xeta"] = result.Message;
                return View(dto);
            }
            TempData["Ugur"] = result.Message ?? "Məzuniyyət uğurla yeniləndi.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Aktiv(int qabaqcaGun = 30)
        {
            if (qabaqcaGun < 0) qabaqcaGun = 0;
            if (qabaqcaGun > 365) qabaqcaGun = 365;
            var result = await _mezuniyyetService.GetAktivVeYaxinlardakilarAsync(qabaqcaGun);
            var list = result.Success ? result.Data!.ToList() : new();
            ViewBag.QabaqcaGun = qabaqcaGun;
            ViewData["Title"] = "Aktiv Məzuniyyətlər";
            return View(list);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // MƌZUNİYYƌT YENİLƌNMƌLƌRİ (illik balıs yənilənmə tarixi)
        // ─────────────────────────────────────────────────────────────────────────────
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Yenilenmeler(int gun = 30, int? departamentId = null, string? search = null)
        {
            if (gun < 1)   gun = 30;
            if (gun > 365) gun = 365;

            var allRows = await LoadAllRowsAsync();

            // Departament dropdown — bütün aktiv departamentlər (filtr-dən asılı olmadan)
            var depts = allRows
                .Where(r => r.DepartamentId > 0 && r.DepartamentAd != "—")
                .GroupBy(r => r.DepartamentId)
                .Select(g => new SelectListItem(
                    g.First().DepartamentAd,
                    g.Key.ToString(),
                    g.Key == departamentId))
                .OrderBy(s => s.Text)
                .ToList();

            // Filtrlər
            var rows = ApplyFilters(allRows, gun, departamentId, search);

            var vm = new MezuniyyetYenilenmeIndexVM
            {
                Rows           = rows,
                Gun            = gun,
                DepartamentId  = departamentId,
                Search         = search,
                Departamentler = depts
            };

            ViewData["Title"] = "Məzuniyyət Yenilənmələri";
            return View(vm);
        }

        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> YenilenmelerExcel(int gun = 30, int? departamentId = null, string? search = null)
        {
            if (gun < 1)   gun = 30;
            if (gun > 365) gun = 365;

            var allRows = await LoadAllRowsAsync();
            var rows = ApplyFilters(allRows, gun, departamentId, search);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Yenilənmələr");

            var headers = new[]
            {
                "#", "Ad", "Soyad", "Ata Adı", "FIN", "Şəxs. Vəsiqəsi",
                "Doğum Tarixi", "Cins", "Telefon", "Email", "Ünvan",
                "Departament", "Vəzifə", "İşə Qəbul",
                "Növbəti Yenilənmə", "Qalan Gün", "Stajlı İl",
                "Cari Maaş", "IBAN", "Cari İl Balansı (gün)", "Status"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                int c = 1;
                ws.Cell(r + 2, c++).Value = r + 1;
                ws.Cell(r + 2, c++).Value = row.Ad;
                ws.Cell(r + 2, c++).Value = row.Soyad;
                ws.Cell(r + 2, c++).Value = row.AtaAdi ?? "";
                ws.Cell(r + 2, c++).Value = row.FIN;
                ws.Cell(r + 2, c++).Value = row.SeriyaNomre ?? "";
                ws.Cell(r + 2, c++).Value = row.DogumTarixi;
                ws.Cell(r + 2, c++).Value = row.CinsAd;
                ws.Cell(r + 2, c++).Value = row.Telefon ?? "";
                ws.Cell(r + 2, c++).Value = row.Email ?? "";
                ws.Cell(r + 2, c++).Value = row.Unvan ?? "";
                ws.Cell(r + 2, c++).Value = row.DepartamentAd;
                ws.Cell(r + 2, c++).Value = row.VezifeAd;
                ws.Cell(r + 2, c++).Value = row.IsheQebulTarixi;
                ws.Cell(r + 2, c++).Value = row.NextRenewDate;
                ws.Cell(r + 2, c++).Value = row.QalanGun;
                ws.Cell(r + 2, c++).Value = row.IslediyiIl;
                ws.Cell(r + 2, c++).Value = row.CariMaas ?? 0m;
                ws.Cell(r + 2, c++).Value = row.Iban ?? "";
                ws.Cell(r + 2, c++).Value = row.CariIldeToplamGun ?? 0;
                ws.Cell(r + 2, c++).Value = row.CariIldeBalansVar ? "Balans mövcuddur" : "Yenilənməlidir";
            }

            ws.Column(7).Style.DateFormat.Format = "dd.MM.yyyy";
            ws.Column(14).Style.DateFormat.Format = "dd.MM.yyyy";
            ws.Column(15).Style.DateFormat.Format = "dd.MM.yyyy";
            ws.Column(18).Style.NumberFormat.Format = "#,##0.00";

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            var fileName = $"Mezuniyyet_Yenilenmeler_{DateTime.Today:yyyy-MM-dd}.xlsx";
            return File(
                ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Bütün aktiv işçilər üçün yenilənmə sətrlərini hazırlayır (filtrsiz).
        /// </summary>
        private async Task<List<MezuniyyetYenilenmeRowVM>> LoadAllRowsAsync()
        {
            var today = DateTime.Today;
            var currentYear = today.Year;

            var isciler = await _unitOfWork.Repository<Isci>().SorguHazirla(
                x => x.Status == IsciStatus.Aktiv,
                include: q => q
                    .Include(i => i.IsciTeyinatlari).ThenInclude(t => t.Departament)
                    .Include(i => i.IsciTeyinatlari).ThenInclude(t => t.Vezife)
                    .Include(i => i.Maliye)
                    .Include(i => i.MezuniyyetBalanslari),
                izlemeden: true).ToListAsync();

            return isciler.Select(i =>
            {
                var aktiv = i.IsciTeyinatlari.FirstOrDefault(t => t.Aktivdir);
                var illik = i.MezuniyyetBalanslari
                    .FirstOrDefault(b => b.Il == currentYear && b.Nov == MezuniyyetNovu.Illik);

                var next = NextRenewDate(i.IsheQebulTarixi, today);
                var qalan = (next - today).Days;
                var stajIl = today.Year - i.IsheQebulTarixi.Year;
                if (i.IsheQebulTarixi.Date > today.AddYears(-stajIl)) stajIl--;

                return new MezuniyyetYenilenmeRowVM
                {
                    IsciId            = i.Id,
                    Ad                = i.Ad,
                    Soyad             = i.Soyad,
                    AtaAdi            = i.AtaAdi,
                    FIN               = i.FIN,
                    SeriyaNomre       = i.SeriyaNomre,
                    DogumTarixi       = i.DogumTarixi,
                    Cins              = i.Cins,
                    Telefon           = i.Telefon,
                    Email             = i.Email,
                    Unvan             = i.Unvan,
                    IsheQebulTarixi   = i.IsheQebulTarixi,
                    NextRenewDate     = next,
                    QalanGun          = qalan,
                    IslediyiIl        = Math.Max(0, stajIl),
                    DepartamentId     = aktiv?.DepartamentId ?? 0,
                    DepartamentAd     = aktiv?.Departament?.Ad ?? "—",
                    VezifeAd          = aktiv?.Vezife?.Ad ?? "—",
                    CariMaas          = i.Maliye?.CariMaas,
                    Iban              = i.Maliye?.BankHesabNo,
                    CariIldeBalansVar = illik != null,
                    CariIldeToplamGun = illik?.ToplamGun
                };
            }).ToList();
        }

        /// <summary>
        /// Gün, departament və axtarış filtrlərini tətbiq edir.
        /// </summary>
        private static List<MezuniyyetYenilenmeRowVM> ApplyFilters(
            List<MezuniyyetYenilenmeRowVM> all,
            int gun,
            int? departamentId,
            string? search)
        {
            var rows = all
                .Where(r => r.QalanGun >= 0 && r.QalanGun <= gun);

            if (departamentId.HasValue && departamentId.Value > 0)
                rows = rows.Where(r => r.DepartamentId == departamentId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                rows = rows.Where(r =>
                    (r.Ad?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Soyad?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.FIN?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return rows.OrderBy(r => r.QalanGun).ThenBy(r => r.Soyad).ToList();
        }

        /// <summary>
        /// İşə qəbul tarixinin ildönümünü bu il (əgər keçməyibsə) və ya gələn il qaytarır.
        /// 29 fevral kimi hüdud hallarını idarə edir (ayın son gününə clamp).
        /// </summary>
        private static DateTime NextRenewDate(DateTime hireDate, DateTime today)
        {
            int year  = today.Year;
            int month = hireDate.Month;
            int day   = Math.Min(hireDate.Day, DateTime.DaysInMonth(year, month));
            var thisYear = new DateTime(year, month, day);

            if (thisYear < today)
            {
                year++;
                day = Math.Min(hireDate.Day, DateTime.DaysInMonth(year, month));
                return new DateTime(year, month, day);
            }
            return thisYear;
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> GetJetonBalansi(int isciId)
        {
            if (isciId <= 0) return Json(new { saat = 0m, gun = 0m });
            var saat = await _jetonService.AktivSaatBalansiAsync(isciId);
            return Json(new { saat, gun = Math.Floor(saat / 8) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> GeriyeQeyd(GeriyeQeydVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Isciler = await GetAktivIsciSelectListAsync(vm.IsciId);
                return View(vm);
            }

            var hrIsciId = await GetCurrentIsciIdAsync();
            if (hrIsciId == null)
            {
                TempData["Error"] = "HR istifadəçisinin işçi qeydi tapılmadı.";
                vm.Isciler = await GetAktivIsciSelectListAsync(vm.IsciId);
                return View(vm);
            }

            var dto = new GeriyeMezuniyyetCreateDto
            {
                IsciId = vm.IsciId,
                Nov = vm.Nov,
                BaslamaTarixi = vm.BaslamaTarixi,
                BitmeTarixi = vm.BitmeTarixi,
                Sebeb = vm.Sebeb,
                EmrSuffiks = vm.EmrSuffiks,
                EmrRegem = vm.EmrRegem,
                EmrIl = vm.EmrIl,
                JetonIleEvezlesdir = vm.JetonIleEvezlesdir
            };

            var result = await _mezuniyyetService.GeriyeQeydEtAsync(dto, hrIsciId.Value);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                vm.Isciler = await GetAktivIsciSelectListAsync(vm.IsciId);
                return View(vm);
            }

            TempData["Success"] = result.Message ?? "Birbaşa qeyd uğurla rəsmiləşdirildi.";
            return RedirectToAction(nameof(Hr));
        }

        private async Task<List<SelectListItem>> GetAktivIsciSelectListAsync(int? selected = null)
        {
            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv,
                izlemeden: true);

            if (!iscilerResult.Success || iscilerResult.Data == null)
                return new();

            return iscilerResult.Data
                .OrderBy(x => x.TamAd)
                .Select(x => new SelectListItem(
                    x.TamAd, x.Id.ToString(), x.Id == selected))
                .ToList();
        }

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Index()
        {
            var result = await _mezuniyyetService.GetListAsync();
            var vm = new HrMezuniyyetIndexVM
            {
                Mezuniyyetler = result.Success ? result.Data!.ToList() : new(),
                PageTitle = "Bütün Müraciətlər",
                TesdiqAction = ""
            };
            ViewData["Title"] = "Bütün Məzuniyyət Müraciətləri";
            return View("TesdiqIndex", vm);
        }

        // DÖVLƌT VƌZİFƌSİ KORREKSİYASI — Maddə 173

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Korreksiya([FromForm] MezuniyyetKorreksiyaDto dto)
        {
            var hrIsciId = await GetCurrentIsciIdAsync();
            if (hrIsciId == null)
                return Json(new { ok = false, xeta = "İstifadəçi tapılmadı." });

            var yollar = new List<string>();
            if (dto.Senedler != null && dto.Senedler.Count > 0)
            {
                var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
                var dir = Path.Combine(dmsRoot, "dovlet-vezife");
                Directory.CreateDirectory(dir);

                foreach (var sened in dto.Senedler)
                {
                    if (sened == null || sened.Length == 0) continue;

                    var ext = Path.GetExtension(sened.FileName).ToLowerInvariant();
                    if (!_icazeSenedTipler.Contains(ext))
                        return Json(new
                        {
                            ok = false,
                            xeta = $"\"{sened.FileName}\" — format qəbul edilmir. İcazə verilən: PDF, JPG, PNG."
                        });

                    if (sened.Length > 10 * 1024 * 1024)
                        return Json(new
                        {
                            ok = false,
                            xeta = $"\"{sened.FileName}\" — 10 MB-dan böyük ola bilməz."
                        });

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    await using (var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                        await sened.CopyToAsync(fs);

                    yollar.Add($"/dms/dovlet-vezife/{fileName}");
                }
            }

            var senedYolu = string.Join("|", yollar);

            var result = await _mezuniyyetService.KorreksiyaEtAsync(dto, hrIsciId.Value, senedYolu);
            if (!result.Success)
                return Json(new { ok = false, xeta = result.Message });

            return Json(new
            {
                ok      = true,
                mesaj   = result.Message,
                yeniId  = result.Data?.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KorreksiyaSenedElave(int mezuniyyetId, List<IFormFile> senedler)
        {
            if (senedler == null || senedler.Count == 0)
                return Json(new { ok = false, xeta = "ƌn azı bir sənəd seçin." });

            var dmsRoot2 = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
            var dir = Path.Combine(dmsRoot2, "dovlet-vezife");
            Directory.CreateDirectory(dir);

            var yollar = new List<string>();
            foreach (var sened in senedler)
            {
                if (sened == null || sened.Length == 0) continue;

                var ext = Path.GetExtension(sened.FileName).ToLowerInvariant();
                if (!_icazeSenedTipler.Contains(ext))
                    return Json(new
                    {
                        ok = false,
                        xeta = $"\"{sened.FileName}\" — format qəbul edilmir. İcazə verilən: PDF, JPG, PNG."
                    });

                if (sened.Length > 10 * 1024 * 1024)
                    return Json(new
                    {
                        ok = false,
                        xeta = $"\"{sened.FileName}\" — 10 MB-dan böyük ola bilməz."
                    });

                var fileName = $"{Guid.NewGuid()}{ext}";
                await using (var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                    await sened.CopyToAsync(fs);

                yollar.Add($"/dms/dovlet-vezife/{fileName}");
            }

            if (yollar.Count == 0)
                return Json(new { ok = false, xeta = "Heç bir sənəd emal edilmədi." });

            var result = await _mezuniyyetService.SenedElavetEtAsync(
                mezuniyyetId, string.Join("|", yollar));

            if (!result.Success)
                return Json(new { ok = false, xeta = result.Message });

            return Json(new { ok = true, mesaj = result.Message });
        }
    }
}
