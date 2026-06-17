using ClosedXML.Excel;
using FinNex.Application.DTOs.HR.DigerTutulma;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.UI.Areas.HR.ViewModels.DigerTutulma;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// İşçidən "digər tutulma" (məs. xəstəlik sığortası payı) — işçi və
    /// məbləğ + müddət daxil edilir, sistem aylıq paya bölür. Maaşa təsir
    /// etmir; yalnız provodkada net ödənişdən aylıq pay çıxılır.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.Muhasib + "," + RoleNames.Admin + "," + RoleNames.HR)]
    public class DigerTutulmaController : Controller
    {
        private readonly IDigerTutulmaService _service;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;

        public DigerTutulmaController(
            IDigerTutulmaService service,
            IIsciService isciService,
            UserManager<AppUser> userManager)
        {
            _service = service;
            _isciService = isciService;
            _userManager = userManager;
        }

        // ── GET /HR/DigerTutulma ───────────────────────────────
        public async Task<IActionResult> Index()
        {
            var r = await _service.HamisiniGetirAsync();
            ViewData["Title"] = "İşçidən Digər Tutulma";
            return View(r.Success ? r.Data : new List<DigerTutulmaListDto>());
        }

        // ── GET /HR/DigerTutulma/ExcelIxrac — mühasib yoxlaması üçün ──
        public async Task<IActionResult> ExcelIxrac()
        {
            var r = await _service.HamisiniGetirAsync();
            var list = r.Success && r.Data != null ? r.Data : new List<DigerTutulmaListDto>();
            string[] ayAdlari = { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
                "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Digər Tutulma");

            var basliqlar = new[] { "№", "İşçi", "Ümumi məbləğ", "Müddət (ay)", "Aylıq",
                "Son ay", "Tutulub", "Qalıq", "Dövr", "Açıqlama", "Yaradılıb" };
            for (int c = 0; c < basliqlar.Length; c++)
                ws.Cell(1, c + 1).Value = basliqlar[c];
            var headRange = ws.Range(1, 1, 1, basliqlar.Length);
            headRange.Style.Font.Bold = true;
            headRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
            headRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var t in list)
            {
                ws.Cell(row, 1).Value = (double)(row - 1);
                ws.Cell(row, 2).Value = t.IsciAdSoyad;
                ws.Cell(row, 3).Value = (double)t.Mebleg;
                ws.Cell(row, 4).Value = (double)t.MuddetAy;
                ws.Cell(row, 5).Value = (double)t.AyliqMebleg;
                ws.Cell(row, 6).Value = (double)t.SonAyMebleg;
                ws.Cell(row, 7).Value = $"{t.TutulubSayi} / {t.MuddetAy}";
                ws.Cell(row, 8).Value = (double)t.QalanMebleg;
                ws.Cell(row, 9).Value = $"{ayAdlari[t.BaslamaAy]} {t.BaslamaIl} — {ayAdlari[t.BitmeAy]} {t.BitmeIl}";
                ws.Cell(row, 10).Value = t.Aciqlama ?? "";
                ws.Cell(row, 11).Value = t.YaradilmaTarixi.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }

            // CƏMİ sətri
            ws.Cell(row, 2).Value = "CƏMİ";
            ws.Cell(row, 3).Value = (double)list.Sum(t => t.Mebleg);
            ws.Cell(row, 5).Value = (double)list.Sum(t => t.AyliqMebleg);
            ws.Cell(row, 6).Value = (double)list.Sum(t => t.SonAyMebleg);
            ws.Cell(row, 8).Value = (double)list.Sum(t => t.QalanMebleg);
            var totalRange = ws.Range(row, 1, row, basliqlar.Length);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f5f9");
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Isciden_Diger_Tutulma_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ── GET /HR/DigerTutulma/Yarat ─────────────────────────
        public async Task<IActionResult> Yarat()
        {
            var vm = new DigerTutulmaYaratVM();
            await IsciDropdownDoldur(vm);
            return View(vm);
        }

        // ── POST /HR/DigerTutulma/Yarat ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yarat(DigerTutulmaYaratVM vm)
        {
            if (!ModelState.IsValid)
            {
                await IsciDropdownDoldur(vm);
                return View(vm);
            }

            var istifadeci = await _userManager.GetUserAsync(User);
            var yaradanIsciId = istifadeci?.IsciId ?? 0;

            var dto = new DigerTutulmaYaratDto
            {
                IsciId = vm.IsciId,
                Mebleg = vm.Mebleg,
                MuddetAy = vm.MuddetAy,
                BaslamaIl = vm.BaslamaIl,
                BaslamaAy = vm.BaslamaAy,
                Aciqlama = vm.Aciqlama
            };

            var r = await _service.YaratAsync(dto, yaradanIsciId);
            if (!r.Success)
            {
                ModelState.AddModelError("", r.Message ?? "Yadda saxlama uğursuz oldu.");
                await IsciDropdownDoldur(vm);
                return View(vm);
            }

            TempData["Success"] = r.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── POST /HR/DigerTutulma/Sil/{id} ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var r = await _service.LegvEtAsync(id);
            TempData[r.Success ? "Success" : "Error"] = r.Success ? "Qeyd silindi." : r.Message;
            return RedirectToAction(nameof(Index));
        }

        private async Task IsciDropdownDoldur(DigerTutulmaYaratVM vm)
        {
            var iscilerR = await _isciService.HamisiniGetirAsync();
            var isciler = iscilerR.Success && iscilerR.Data != null
                ? iscilerR.Data
                : new List<FinNex.Application.DTOs.HR.Isci.IsciListDto>();

            vm.Isciler = isciler
                .OrderBy(x => x.Sira).ThenBy(x => x.TamAd)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.TamAd })
                .ToList();
        }
    }
}
