using ClosedXML.Excel;
using FinNex.Application.Services.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = "Admin,HR")]
    public class TabelController : Controller
    {
        private readonly ITabelService _tabelService;
        public TabelController(ITabelService tabelService) => _tabelService = tabelService;

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Il = DateTime.Today.Year;
            ViewBag.Ay = DateTime.Today.Month;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(int il, int ay)
        {
            if (il < 2020 || il > 2100 || ay < 1 || ay > 12)
                return BadRequest();

            var data    = await _tabelService.GenerateTabelAsync(il, ay);
            int gunSayi = data.GunSayi;
            int sumStart = 4 + gunSayi;        // summary cols start (1-indexed)
            int totalCols = sumStart + 4;       // 5 summary cols: isgunu, issaati, mez, ezam, xest

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Tabel");

            // ── Rənglər ──────────────────────────────────────────
            var cHeader    = XLColor.FromArgb(0x1F, 0x49, 0x7D);
            var cHeaderFont= XLColor.White;
            var cIstirahit = XLColor.FromArgb(0xD0, 0xD0, 0xD0);
            var cBayram    = XLColor.FromArgb(0xFF, 0xCC, 0x80);
            var cMez       = XLColor.FromArgb(0xBB, 0xDE, 0xFB);
            var cXest      = XLColor.FromArgb(0xFF, 0xCC, 0xCC);
            var cEzam      = XLColor.FromArgb(0xC8, 0xF0, 0xC8);
            var cAzSaat    = XLColor.FromArgb(0xFF, 0xF0, 0xCC); // 7 və ya 6 saat (əlil / bayram ərəfəsi)
            var cYekun     = XLColor.FromArgb(0xE2, 0xEF, 0xDA);

            var ayAdlari = new[] { "Yanvar","Fevral","Mart","Aprel","May","İyun",
                                   "İyul","Avqust","Sentyabr","Oktyabr","Noyabr","Dekabr" };

            // ── Sətir 1: Başlıq ──────────────────────────────────
            ws.Cell(1, 1).Value = $"TABEL — Əsas iş saatları";
            ws.Range(1, 1, 1, totalCols).Merge();
            ApplyTitle(ws.Cell(1, 1), cHeader, cHeaderFont, 13);

            // ── Sətir 2: Dövr ────────────────────────────────────
            ws.Cell(2, 1).Value = $"Dövr: {il} il, {ayAdlari[ay - 1]}  " +
                                  $"(01.{ay:D2}.{il} – {gunSayi:D2}.{ay:D2}.{il})";
            ws.Range(2, 1, 2, totalCols).Merge();
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0xD6, 0xE4, 0xF0);
            ws.Row(2).Height = 18;

            ws.Row(3).Height = 5;

            // ── Sətir 4: Başlıq sütunları ────────────────────────
            const int HR = 4;
            ws.Cell(HR, 1).Value = "#";
            ws.Cell(HR, 2).Value = "Soyadı, Adı, Ata adı";
            ws.Cell(HR, 3).Value = "Vəzifəsi";

            for (int d = 1; d <= gunSayi; d++)
                ws.Cell(HR, 3 + d).Value = d;

            ws.Cell(HR, sumStart).Value     = "İş\ngünü";
            ws.Cell(HR, sumStart + 1).Value = "İş\nsaatı";
            ws.Cell(HR, sumStart + 2).Value = "Məz.";
            ws.Cell(HR, sumStart + 3).Value = "Ezam.";
            ws.Cell(HR, sumStart + 4).Value = "Xəst.";

            var hrRange = ws.Range(HR, 1, HR, totalCols);
            hrRange.Style.Fill.BackgroundColor        = cHeader;
            hrRange.Style.Font.FontColor              = cHeaderFont;
            hrRange.Style.Font.Bold                   = true;
            hrRange.Style.Alignment.Horizontal        = XLAlignmentHorizontalValues.Center;
            hrRange.Style.Alignment.Vertical          = XLAlignmentVerticalValues.Center;
            hrRange.Style.Alignment.WrapText          = true;
            hrRange.Style.Border.OutsideBorder        = XLBorderStyleValues.Medium;
            hrRange.Style.Border.InsideBorder         = XLBorderStyleValues.Thin;
            ws.Row(HR).Height = 32;

            // ── Məlumat sətirləri ─────────────────────────────────
            int dr      = HR + 1;
            int firstDr = dr;
            int rowNum  = 1;

            foreach (var satir in data.Satirlar)
            {
                ws.Cell(dr, 1).Value = rowNum++;
                ws.Cell(dr, 2).Value = satir.IsciAd;
                ws.Cell(dr, 3).Value = satir.Vezife;
                ws.Cell(dr, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(dr, 2).Style.Font.FontSize = 9;
                ws.Cell(dr, 3).Style.Font.FontSize = 9;

                for (int d = 0; d < gunSayi; d++)
                {
                    var kod  = satir.GunKodlari[d];
                    var cell = ws.Cell(dr, 4 + d);
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Font.FontSize        = 9;

                    switch (kod)
                    {
                        case "İ":
                            cell.Value = "İ";
                            cell.Style.Fill.BackgroundColor = cIstirahit;
                            cell.Style.Font.FontColor       = XLColor.Gray;
                            break;
                        case "B":
                            cell.Value = "B";
                            cell.Style.Fill.BackgroundColor = cBayram;
                            cell.Style.Font.Bold            = true;
                            break;
                        case "M":
                            cell.Value = "M";
                            cell.Style.Fill.BackgroundColor = cMez;
                            break;
                        case "X":
                            cell.Value = "X";
                            cell.Style.Fill.BackgroundColor = cXest;
                            break;
                        case "E":
                            cell.Value = "E";
                            cell.Style.Fill.BackgroundColor = cEzam;
                            break;
                        default:
                            int saat = int.Parse(kod);
                            cell.Value = saat;
                            // Sarı rəng yalnız bayram ərəfəsi günlərə (d 0-indekslidir)
                            if (data.BayramErtesiGunler.Contains(d + 1))
                                cell.Style.Fill.BackgroundColor = cAzSaat;
                            break;
                    }
                }

                // Yekun sütunlar
                ws.Cell(dr, sumStart).Value     = satir.IsGunSayi;
                ws.Cell(dr, sumStart + 1).Value = satir.IsSaatSayi;
                ws.Cell(dr, sumStart + 2).Value = satir.MezuniyyetGun;
                ws.Cell(dr, sumStart + 3).Value = satir.EzamiyyetGun;
                ws.Cell(dr, sumStart + 4).Value = satir.XestelikGun;

                for (int sc = sumStart; sc <= totalCols; sc++)
                    ws.Cell(dr, sc).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Range(dr, 1, dr, totalCols).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(dr, 1, dr, totalCols).Style.Border.InsideBorder  = XLBorderStyleValues.Hair;
                ws.Row(dr).Height = 15;

                dr++;
            }

            // ── CƏMİ sətri ────────────────────────────────────────
            ws.Cell(dr, 1).Value = "CƏMİ";
            ws.Range(dr, 1, dr, 3).Merge();
            ws.Cell(dr, 1).Style.Font.Bold            = true;
            ws.Cell(dr, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            for (int i = 0; i <= 4; i++)
            {
                int sc   = sumStart + i;
                var fc   = ws.Cell(firstDr, sc);
                var lc   = ws.Cell(dr - 1, sc);
                var yc   = ws.Cell(dr, sc);
                yc.FormulaA1                      = $"=SUM({fc.Address}:{lc.Address})";
                yc.Style.Font.Bold                = true;
                yc.Style.Alignment.Horizontal     = XLAlignmentHorizontalValues.Center;
            }

            ws.Range(dr, 1, dr, totalCols).Style.Fill.BackgroundColor  = cYekun;
            ws.Range(dr, 1, dr, totalCols).Style.Border.OutsideBorder  = XLBorderStyleValues.Medium;
            ws.Range(dr, 1, dr, totalCols).Style.Border.InsideBorder   = XLBorderStyleValues.Thin;

            // ── Şərti işarələr (legend) ───────────────────────────
            dr += 2;
            ws.Cell(dr, 1).Value = "Şərti işarələr:";
            ws.Cell(dr, 1).Style.Font.Bold = true;
            dr++;

            var legend = new (string Kod, string Ad, XLColor Renk)[]
            {
                ("İ", "İstirahət günü",   cIstirahit),
                ("B", "Bayram günü",       cBayram),
                ("M", "Məzuniyyət",        cMez),
                ("X", "Xəstəlik",          cXest),
                ("E", "Ezamiyyət",         cEzam),
                ("7",   "Bayram ərəfəsi (azaldılmış saat)",             cAzSaat),
            };
            foreach (var (k, v, r) in legend)
            {
                ws.Cell(dr, 1).Value = k;
                ws.Cell(dr, 1).Style.Fill.BackgroundColor = r;
                ws.Cell(dr, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(dr, 2).Value = "–  " + v;
                dr++;
            }

            // ── Sütun eni ─────────────────────────────────────────
            ws.Column(1).Width = 4.5;
            ws.Column(2).Width = 27;
            ws.Column(3).Width = 22;
            for (int d = 1; d <= gunSayi; d++)
                ws.Column(3 + d).Width = 3.2;
            ws.Column(sumStart).Width     = 7;
            ws.Column(sumStart + 1).Width = 7;
            ws.Column(sumStart + 2).Width = 6;
            ws.Column(sumStart + 3).Width = 6;
            ws.Column(sumStart + 4).Width = 6;

            // Sətirləri dond (başlıq görünsün sürüşdürəndə)
            ws.SheetView.FreezeRows(HR);
            ws.SheetView.FreezeColumns(3);

            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize       = XLPaperSize.A3Paper;
            ws.PageSetup.FitToPages(1, 0);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Tabel_{il}_{ay:D2}.xlsx");
        }

        private static void ApplyTitle(IXLCell cell, XLColor bg, XLColor fg, double fontSize)
        {
            cell.Style.Font.Bold              = true;
            cell.Style.Font.FontSize          = fontSize;
            cell.Style.Font.FontColor         = fg;
            cell.Style.Fill.BackgroundColor   = bg;
            cell.Style.Alignment.Horizontal   = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical     = XLAlignmentVerticalValues.Center;
        }
    }
}
