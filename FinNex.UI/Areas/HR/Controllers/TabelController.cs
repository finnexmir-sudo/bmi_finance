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
        private readonly IWebHostEnvironment _env;

        public TabelController(ITabelService tabelService, IWebHostEnvironment env)
        {
            _tabelService = tabelService;
            _env          = env;
        }

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

            var data      = await _tabelService.GenerateTabelAsync(il, ay);
            int gunSayi   = data.GunSayi;
            int sumStart  = 4 + gunSayi;   // birinci yekun sütunu (İş günü)
            int totalCols = sumStart + 4;   // son sütun (Xəst.)

            // ── Şablonu aç ────────────────────────────────────────────────────
            var templatePath = Path.Combine(_env.ContentRootPath, "App_Data", "Templates", "Tabel_isci.xlsx");

            string tplTesdiqTitle = "TƏSDİQ EDİRƏM:";
            string tplTesdiqLine2 = " ___________________________";
            string tplMudirAd     = "";
            string tplSigLineVal  = "___________________________";
            string tplImzaLineVal = "___________________";
            string tplSigNameVal  = "müdir müavini ___________________________";
            string tplSigImzaVal  = "İmza";
            string tplLegendVal   = "İşarələr: istirahət günü (İ), bayram günü (B), ezamiyyə günü (E), xəstəlik günü (X), məzuniyyət günü (M), işə gəlmədiyi günlər (G)";

            XLWorkbook wb;
            if (System.IO.File.Exists(templatePath))
            {
                using var fs = new System.IO.FileStream(
                    templatePath, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                var tplMs = new MemoryStream();
                await fs.CopyToAsync(tplMs);
                tplMs.Position = 0;
                wb = new XLWorkbook(tplMs);
            }
            else { wb = new XLWorkbook(); }
            using var _ = wb;

            IXLWorksheet ws;
            if (wb.Worksheets.Count > 0)
            {
                ws      = wb.Worksheets.First();
                ws.Name = "Tabel";

                // ── Şablon dəyərlərini oxu (məhv etmədən ƏVVƏL) ────────────
                // TƏSDİQ sütununu tap (row 1, cols 20-55)
                int tplTesdiqCol = 0;
                for (int c = 20; c <= 55; c++)
                {
                    var v = ws.Cell(1, c).Value.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(v)) { tplTesdiqCol = c; tplTesdiqTitle = v; break; }
                }
                if (tplTesdiqCol > 0)
                {
                    // Row 2: imza boşluğu (alt xətt)
                    var v2 = ws.Cell(2, tplTesdiqCol).Value.ToString();
                    if (!string.IsNullOrWhiteSpace(v2)) tplTesdiqLine2 = v2;
                    // Row 3: müdir adı və ya (vəzifəsi...) işarəsi
                    var v3 = ws.Cell(3, tplTesdiqCol).Value.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(v3))
                        tplMudirAd = v3; // "(" ilə başlasa da yazılsın
                }

                // İmza xətti: B7, E7
                var r7b = ws.Cell(7, 2).Value.ToString();
                if (!string.IsNullOrWhiteSpace(r7b)) tplSigLineVal = r7b;
                var r7e = ws.Cell(7, 5).Value.ToString();
                if (!string.IsNullOrWhiteSpace(r7e)) tplImzaLineVal = r7e;

                // İmza adı: B8, E8
                var r8b = ws.Cell(8, 2).Value.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(r8b)) tplSigNameVal = r8b;
                var r8e = ws.Cell(8, 5).Value.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(r8e)) tplSigImzaVal = r8e;

                // Legend: B12
                var r12 = ws.Cell(12, 2).Value.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(r12)) tplLegendVal = r12;

                // ── Rows 1-3: yalnız MƏRKƏZ (cols 4..totalCols) silinir ─────
                // Sol (A:C) və sağ (sumStart+) TOXUNULMUR → şablon qalır
                foreach (var mr in ws.MergedRanges.ToList())
                {
                    var fc = mr.FirstCell().Address;
                    if (fc.RowNumber <= 3 && fc.ColumnNumber >= 4)
                        ws.MergedRanges.Remove(mr);
                }
                for (int r = 1; r <= 3; r++)
                    for (int c = 4; c <= 60; c++)
                        ws.Cell(r, c).Clear();

                // ── Row 4+: hamısını sil ──────────────────────────────────────
                foreach (var mr in ws.MergedRanges.ToList())
                    if (mr.FirstCell().Address.RowNumber >= 4)
                        ws.MergedRanges.Remove(mr);
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 50;
                ws.Range(4, 1, lastRow + 5, 60).Clear();
            }
            else
            {
                ws = wb.Worksheets.Add("Tabel");
                ws.Cell(1, 1).Value = "\"Bank Melli İran\" Bakı filialı";
                ws.Range(1, 1, 1, 3).Merge();
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(2, 1).Value = "(müəssisənin, idarənin və təşkilatın adı)";
                ws.Range(2, 1, 2, 3).Merge();
                ws.Cell(2, 1).Style.Font.Italic   = true;
                ws.Cell(2, 1).Style.Font.FontSize = 8;
                ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // ── Rənglər ──────────────────────────────────────────────────────
            var cHeader    = XLColor.FromArgb(0x1F, 0x49, 0x7D);
            var cHeaderFont= XLColor.White;
            var cIstirahit = XLColor.FromArgb(0xD0, 0xD0, 0xD0);
            var cBayram    = XLColor.FromArgb(0xFF, 0xCC, 0x80);
            var cMez       = XLColor.FromArgb(0xBB, 0xDE, 0xFB);
            var cXest      = XLColor.FromArgb(0xFF, 0xCC, 0xCC);
            var cEzam      = XLColor.FromArgb(0xC8, 0xF0, 0xC8);
            var cAzSaat    = XLColor.FromArgb(0xFF, 0xF0, 0xCC);
            var cYekun     = XLColor.FromArgb(0xE2, 0xEF, 0xDA);

            // ── Rows 2-3 MƏRKƏZ: TABEL CƏDVƏLİ ─────────────────────────────
            var ayAdlar = new[] {"Yanvar","Fevral","Mart","Aprel","May","İyun",
                                 "İyul","Avqust","Sentyabr","Oktyabr","Noyabr","Dekabr"};
            int cStart = 4, cEnd = sumStart - 1;

            ws.Cell(2, cStart).Value = "TABEL CƏDVƏLİ";
            ws.Range(2, cStart, 2, cEnd).Merge();
            ws.Cell(2, cStart).Style.Font.Bold     = true;
            ws.Cell(2, cStart).Style.Font.FontSize = 12;
            ws.Cell(2, cStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, cStart).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Row(2).Height = 20;

            ws.Cell(3, cStart).Value = $"{il}-ci il {ayAdlar[ay - 1]} ayı";
            ws.Range(3, cStart, 3, cEnd).Merge();
            ws.Cell(3, cStart).Style.Font.FontSize = 10;
            ws.Cell(3, cStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(3, cStart).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Row(3).Height = 16;

            // ── Rows 1-3 SAĞ: TƏSDİQ bölməsi (şablondan oxunan dəyərlərlə) ──
            ws.Cell(1, sumStart).Value = tplTesdiqTitle;
            ws.Range(1, sumStart, 1, totalCols).Merge();
            ws.Cell(1, sumStart).Style.Font.Bold     = true;
            ws.Cell(1, sumStart).Style.Font.FontSize = 11;
            ws.Cell(1, sumStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, sumStart).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 20;

            // Row 2 sağ: imza boşluğu (şablondan oxundu — alt xətt)
            ws.Cell(2, sumStart).Value = tplTesdiqLine2;
            ws.Range(2, sumStart, 2, totalCols).Merge();
            ws.Cell(2, sumStart).Style.Font.FontSize        = 10;
            ws.Cell(2, sumStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, sumStart).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;

            // Row 3 sağ: müdir adı (şablondan oxundu)
            ws.Cell(3, sumStart).Value = string.IsNullOrWhiteSpace(tplMudirAd)
                                            ? "müdir ___________________________"
                                            : tplMudirAd;
            ws.Range(3, sumStart, 3, totalCols).Merge();
            ws.Cell(3, sumStart).Style.Font.FontSize        = 9;
            ws.Cell(3, sumStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(3, sumStart).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;

            // ── Row 4: Boşluq ─────────────────────────────────────────────────
            ws.Row(4).Height = 6;

            // ── Rows 5-6: İki sıralı sütun başlıqları ─────────────────────────
            const int HR1 = 5, HR2 = 6;

            ws.Range(HR1, 1, HR2, 1).Merge(); ws.Cell(HR1, 1).Value = "Sıra\nsayı";
            ws.Range(HR1, 2, HR2, 2).Merge(); ws.Cell(HR1, 2).Value = "Soyadı, Adı, Ata adı";
            ws.Range(HR1, 3, HR2, 3).Merge(); ws.Cell(HR1, 3).Value = "Vəzifəsi";
            ws.Range(HR1, 4, HR1, 3 + gunSayi).Merge(); ws.Cell(HR1, 4).Value = "Ayın günləri";
            for (int d = 1; d <= gunSayi; d++) ws.Cell(HR2, 3 + d).Value = d;

            var sumHdrs = new[] { "İş\ngünü", "İş\nsaatı", "Məz.", "Ezam.", "Xəst." };
            for (int i = 0; i < 5; i++)
            {
                ws.Range(HR1, sumStart + i, HR2, sumStart + i).Merge();
                ws.Cell(HR1, sumStart + i).Value = sumHdrs[i];
            }
            for (int hr = HR1; hr <= HR2; hr++)
            {
                var hrRng = ws.Range(hr, 1, hr, totalCols);
                hrRng.Style.Fill.BackgroundColor = cHeader;
                hrRng.Style.Font.FontColor       = cHeaderFont;
                hrRng.Style.Font.Bold            = true;
                hrRng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hrRng.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                hrRng.Style.Alignment.WrapText   = true;
                hrRng.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                hrRng.Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
            }
            ws.Row(HR1).Height = 22;
            ws.Row(HR2).Height = 18;

            // ── Row 7+: Məlumat ───────────────────────────────────────────────
            int dr = HR2 + 1, firstDr = dr, rowNum = 1;

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
                        case "İ": cell.Value = "İ"; cell.Style.Fill.BackgroundColor = cIstirahit; cell.Style.Font.FontColor = XLColor.Gray; break;
                        case "B": cell.Value = "B"; cell.Style.Fill.BackgroundColor = cBayram; cell.Style.Font.Bold = true; break;
                        case "M": cell.Value = "M"; cell.Style.Fill.BackgroundColor = cMez; break;
                        case "X": cell.Value = "X"; cell.Style.Fill.BackgroundColor = cXest; break;
                        case "E": cell.Value = "E"; cell.Style.Fill.BackgroundColor = cEzam; break;
                        default:
                            int saat = int.Parse(kod);
                            cell.Value = saat;
                            if (data.BayramErtesiGunler.Contains(d + 1))
                                cell.Style.Fill.BackgroundColor = cAzSaat;
                            break;
                    }
                }

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

            // ── CƏMİ ──────────────────────────────────────────────────────────
            ws.Cell(dr, 1).Value = "CƏMİ";
            ws.Range(dr, 1, dr, 3).Merge();
            ws.Cell(dr, 1).Style.Font.Bold            = true;
            ws.Cell(dr, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            for (int i = 0; i <= 4; i++)
            {
                int sc = sumStart + i;
                ws.Cell(dr, sc).FormulaA1 = $"=SUM({ws.Cell(firstDr,sc).Address}:{ws.Cell(dr-1,sc).Address})";
                ws.Cell(dr, sc).Style.Font.Bold            = true;
                ws.Cell(dr, sc).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            ws.Range(dr, 1, dr, totalCols).Style.Fill.BackgroundColor = cYekun;
            ws.Range(dr, 1, dr, totalCols).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            ws.Range(dr, 1, dr, totalCols).Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
            ws.Row(dr).Height = 16;
            dr++;

            // ── İmza ──────────────────────────────────────────────────────────
            dr += 2;
            ws.Cell(dr, 2).Value = tplSigLineVal;
            ws.Range(dr, 2, dr, 10).Merge();
            ws.Cell(dr, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(dr, 12).Value = tplImzaLineVal;
            ws.Range(dr, 12, dr, 18).Merge();
            ws.Cell(dr, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(dr).Height = 18;
            dr++;

            ws.Cell(dr, 2).Value = tplSigNameVal;
            ws.Range(dr, 2, dr, 10).Merge();
            ws.Cell(dr, 2).Style.Font.FontSize        = 9;
            ws.Cell(dr, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(dr, 12).Value = tplSigImzaVal;
            ws.Range(dr, 12, dr, 18).Merge();
            ws.Cell(dr, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(dr).Height = 14;
            dr += 3;

            // ── Legend ────────────────────────────────────────────────────────
            ws.Cell(dr, 2).Value = tplLegendVal;
            ws.Range(dr, 2, dr, totalCols).Merge();
            ws.Cell(dr, 2).Style.Font.Italic   = true;
            ws.Cell(dr, 2).Style.Font.FontSize = 9;
            ws.Row(dr).Height = 14;

            // ── Sütun eni ─────────────────────────────────────────────────────
            ws.Column(1).Width = 4.5;
            ws.Column(2).Width = 27;
            ws.Column(3).Width = 22;
            for (int d = 1; d <= gunSayi; d++) ws.Column(3 + d).Width = 3.2;
            ws.Column(sumStart).Width     = 7;
            ws.Column(sumStart + 1).Width = 7;
            ws.Column(sumStart + 2).Width = 6;
            ws.Column(sumStart + 3).Width = 6;
            ws.Column(sumStart + 4).Width = 6;

            ws.SheetView.FreezeRows(HR2);
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
    }
}
