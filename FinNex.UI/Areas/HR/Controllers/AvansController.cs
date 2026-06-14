using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class AvansController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IBildirisRouter _bildirisRouter;
        private readonly IMuhasibatHesabService _muhasibatHesabService;
        private readonly IWebHostEnvironment _env;

        public AvansController(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IBildirisRouter bildirisRouter,
            IMuhasibatHesabService muhasibatHesabService,
            IWebHostEnvironment env)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _bildirisRouter = bildirisRouter;
            _muhasibatHesabService = muhasibatHesabService;
            _env = env;
        }

        // ── GET /HR/Avans ────────────────────────────────────
        public async Task<IActionResult> Index(int? statusFilter = null)
        {
            var query = _unitOfWork.Repository<Avans>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.Maliye)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Muhasib)
                .AsQueryable();

            if (statusFilter.HasValue)
                query = query.Where(x => (int)x.Status == statusFilter.Value);

            var list = await query
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.GozlemedeSayi = list.Count(x => x.Status == AvansStatus.Gozlemede);
            ViewBag.TesdiqSayi = list.Count(x => x.Status == AvansStatus.Tesdiqlenib || x.Status == AvansStatus.Odenilib);
            ViewBag.ImtinaSayi = list.Count(x => x.Status == AvansStatus.ImtinaEdildi);

            ViewData["Title"] = "Avans Müraciətləri";
            return View(list);
        }

        // ── Əməliyyatın yazılışı (Excel) — seçilmiş ay üzrə təsdiqlənmiş avanslar ──
        // Hər avans bir sətir: Debet sabit (ayardan), Kredit = işçinin bank hesabı.
        // .xls (NPOI/HSSF) — bank idxalı formatı. Avans statusu DƏYİŞMİR (təkrar export olar).
        public async Task<IActionResult> PravodkaExport(int il, int ay)
        {
            if (ay < 1 || ay > 12)
            {
                TempData["Error"] = "Ay düzgün deyil.";
                return RedirectToAction(nameof(Index));
            }

            var debetHesab = await _muhasibatHesabService.HesabAlAsync("AvansDebet");
            if (string.IsNullOrWhiteSpace(debetHesab))
            {
                TempData["Error"] = "Avans Debet hesabı təyin edilməyib — Mühasibat Hesabları səhifəsindən təyin edin.";
                return RedirectToAction(nameof(Index));
            }

            var avanslar = await _unitOfWork.Repository<Avans>()
                .Query()
                .Where(x => !x.Silinib && x.Il == il && x.Ay == ay
                         && (x.Status == AvansStatus.Tesdiqlenib || x.Status == AvansStatus.Odenilib))
                .Include(x => x.Isci).ThenInclude(i => i.Maliye)
                .OrderBy(x => x.Isci.Soyad).ThenBy(x => x.Isci.Ad)
                .ToListAsync();

            if (avanslar.Count == 0)
            {
                TempData["Error"] = $"{il}/{ay:D2} üçün təsdiqlənmiş avans tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var ayAdlar = new[] { "", "yanvar", "fevral", "mart", "aprel", "may", "iyun",
                                  "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr" };
            var qeyd = $"{il}-{IlSuffiks(il)} il {ayAdlar[ay]} ayı üzrə avans əmək haqqı";

            // Şablonu aç — fontlar, sütun enləri, başlıqlar, "Ipoteka" sheet qorunur
            var templatePath = Path.Combine(_env.ContentRootPath, "App_Data", "Muhasibat", "Avans hesablama.xls");
            if (!System.IO.File.Exists(templatePath))
            {
                TempData["Error"] = "Şablon tapılmadı: App_Data/Muhasibat/Avans hesablama.xls";
                return RedirectToAction(nameof(Index));
            }

            HSSFWorkbook wb;
            using (var tfs = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                wb = new HSSFWorkbook(tfs);

            var sheet = wb.GetSheet("Ipoteka") ?? wb.GetSheetAt(0);

            // Nümunə data sətrinin (index 1) cell stillərini tut — format (mətn/rəqəm) qorunsun
            var ornek = sheet.GetRow(1);
            var colStil = new ICellStyle[10];
            for (int c = 0; c < 10; c++) colStil[c] = ornek?.GetCell(c)?.CellStyle;

            // Fallback stillər (şablonda nümunə sətir yoxdursa)
            var textStyle = wb.CreateCellStyle(); textStyle.DataFormat = wb.CreateDataFormat().GetFormat("@");
            var pulStyle = wb.CreateCellStyle();  pulStyle.DataFormat  = wb.CreateDataFormat().GetFormat("0.00");

            // Başlıqdan (row 0) sonrakı bütün sətirləri sil, yalnız header qalsın
            for (int rr = sheet.LastRowNum; rr >= 1; rr--)
            {
                var rw = sheet.GetRow(rr);
                if (rw != null) sheet.RemoveRow(rw);
            }

            int r = 1;
            foreach (var a in avanslar)
            {
                var iban = a.Isci?.Maliye?.BankHesabNo?.Trim() ?? "";
                var row = sheet.CreateRow(r);

                var c0 = row.CreateCell(0); c0.SetCellValue(r);               if (colStil[0] != null) c0.CellStyle = colStil[0]; // Sənədin № (sıra)
                var c3 = row.CreateCell(3); c3.SetCellValue(debetHesab);      c3.CellStyle = colStil[3] ?? textStyle;             // Debet (sabit)
                var c5 = row.CreateCell(5); c5.SetCellValue(iban);            c5.CellStyle = colStil[5] ?? textStyle;             // Kredit (işçi hesabı)
                var c6 = row.CreateCell(6); c6.SetCellValue((double)a.Mebleg); c6.CellStyle = colStil[6] ?? pulStyle;            // Məbləğ
                var c9 = row.CreateCell(9); c9.SetCellValue(qeyd);            if (colStil[9] != null) c9.CellStyle = colStil[9];  // Qeyd
                r++;
            }

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                wb.Write(ms, true);   // leaveOpen — stream-i bağlama
                bytes = ms.ToArray();
            }
            return File(bytes, "application/vnd.ms-excel", $"Avans_hesablama_{il}_{ay:D2}.xls");
        }

        // Azərbaycan sıra sonluğu — ilin son rəqəminə görə (2026 → "cı")
        private static string IlSuffiks(int il) => (il % 10) switch
        {
            1 or 2 or 5 or 7 or 8 => "ci",
            3 or 4                => "cü",
            9                     => "cu",
            _                     => "cı"   // 0, 6
        };

        // ── POST /HR/Avans/Tesdiq ────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Tesdiq(int id, bool tesdiq, string? imtinaSebebi)
        {
            var avans = await _unitOfWork.Repository<Avans>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (avans == null)
            {
                TempData["Error"] = "Müraciət tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            if (avans.Status != AvansStatus.Gozlemede)
            {
                TempData["Error"] = "Yalnız gözləmədə olan müraciətləri təsdiq/rədd etmək olar.";
                return RedirectToAction(nameof(Index));
            }

            var appUser = await _userManager.GetUserAsync(User);
            var muhasibIsciId = appUser?.IsciId;

            if (tesdiq)
            {
                avans.Status = AvansStatus.Tesdiqlenib;
                avans.MuhasibId = muhasibIsciId;
                avans.TesdiqTarixi = DateTime.Now;
                TempData["Success"] = $"Avans təsdiqləndi — {avans.Mebleg:N2} ₼";
            }
            else
            {
                avans.Status = AvansStatus.ImtinaEdildi;
                avans.MuhasibId = muhasibIsciId;
                avans.TesdiqTarixi = DateTime.Now;
                avans.ImtinaSebebi = string.IsNullOrWhiteSpace(imtinaSebebi) ? null : imtinaSebebi.Trim();
                TempData["Success"] = "Müraciət rədd edildi.";
            }

            await _unitOfWork.YaddaSaxlaAsync();

            // İşçiyə bildiriş — qərar nəticəsi
            var redirectUrl = Url.Action("Index", "Avans", new { area = "User" });
            if (tesdiq)
            {
                await _bildirisRouter.NotifyIsciAsync(
                    avans.IsciId,
                    BildirisNovu.AvansTesdiq,
                    "Avansınız təsdiqləndi",
                    $"{avans.Il}/{avans.Ay:D2} ayı üçün {avans.Mebleg:N2} ₼ avans müraciətiniz təsdiqləndi.",
                    redirectUrl: redirectUrl);
            }
            else
            {
                var sebebMetn = string.IsNullOrWhiteSpace(avans.ImtinaSebebi) ? "" : $" Səbəb: {avans.ImtinaSebebi}";
                await _bildirisRouter.NotifyIsciAsync(
                    avans.IsciId,
                    BildirisNovu.AvansImtina,
                    "Avansınız rədd edildi",
                    $"{avans.Il}/{avans.Ay:D2} ayı üçün {avans.Mebleg:N2} ₼ avans müraciətiniz rədd edildi.{sebebMetn}",
                    redirectUrl: redirectUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
