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
            int sumStart  = 4 + gunSayi;
            int totalCols = sumStart + 4;
            int leftEnd   = 3;
            int tesdiqStart = sumStart;

            // ── Şablonu aç, dəyərləri ƏVVƏLCƏ oxu, sonra sil-yaz ───────
            var templatePath = Path.Combine(_env.ContentRootPath, "App_Data", "Templates", "Tabel_isci.xlsx");
            XLWorkbook wb;
            if (System.IO.File.Exists(templatePath))
            {
                // FileShare.ReadWrite — StaticWebAssets fingerprinting ilə kilidlənmə olmasın
                using var fs = new System.IO.FileStream(
                    templatePath, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                var tplMs = new MemoryStream();
                await fs.CopyToAsync(tplMs);
                tplMs.Position = 0;
                wb = new XLWorkbook(tplMs);
            }
            else
            {
                wb = new XLWorkbook();
            }
            using var _ = wb;

            // Şablondan oxunan dəyərlərin default-ları
            string tplTesdiqRow2   = "müdir ___________________________";
            string tplSigLineVal   = "";
            bool   tplSigLineHasBorder = false;
            string tplSigNameVal   = "müdir müavini ___________________________";
            string tplSigImzaVal   = "İmza";
            string tplLegendVal    = "İşarələr: istirahət günü (İ), bayram günü (B), ezamiyyə günü (E), xəstəlik günü (X), məzuniyyət günü (M), işə gəlmədiyi günlər (G)";

            IXLWorksheet ws;
            if (wb.Worksheets.Count > 0)
            {
                ws      = wb.Worksheets.First();
                ws.Name = "Tabel";

                // ── Şablon dəyərləri oxunur (sil-yazdan ƏVVƏL) ──────────
                // TƏSDİQ sağ tərəf row 2 (col 34 = 30 günlük şablon üçün AH2)
                var r2v = ws.Cell(2, 34).Value.ToString();
                if (!string.IsNullOrWhiteSpace(r2v)) tplTesdiqRow2 = r2v;

                // İmza bölməsi — row 7 (xətt/boşluq), row 8 (ad + İmza)
                tplSigLineVal = ws.Cell(7, 2).Value.ToString();
                tplSigLineHasBorder = ws.Cell(7, 2).Style.Border.BottomBorder != XLBorderStyleValues.None;

                var r8v = ws.Cell(8, 2).Value.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(r8v)) tplSigNameVal = r8v;

                var r8e = ws.Cell(8, 5).Value.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(r8e)) tplSigImzaVal = r8e;

                // Legend — row 12
                var legV = ws.Cell(12, 2).Value.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(legV)) tplLegendVal = legV;

                // ── İndi sil ────────────────────────────────────────────
                foreach (var mr in ws.MergedRanges.ToList())
                {
                    var fc = mr.FirstCell().Address;
                    if (fc.RowNumber <= 3 && fc.ColumnNumber > leftEnd)
                        ws.MergedRanges.Remove(mr);
                }
                for (int r = 1; r <= 3; r++)
                    for (int c = leftEnd + 1; c <= 50; c++)
                        ws.Cell(r, c).Clear();

                foreach (var mr in ws.MergedRanges.ToList())
                    if (mr.FirstCell().Address.RowNumber >= 4)
                        ws.MergedRanges.Remove(mr);
                int lastUsed = ws.LastRowUsed()?.RowNumber() ?? 20;
                ws.Range(4, 1, lastUsed + 5, 50).Clear();
            }
            else
            {
                ws = wb.Worksheets.Add("Tabel");
                ws.Cell(1, 1).Value = "\"Bank Melli İran\" Bakı filialı";
                ws.Range(1, 1, 1, leftEnd).Merge();
                ws.Cell(1, 1).Style.Font.Bold     = true;
                ws.Cell(1, 1).Style.Font.FontSize = 11;
                ws.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                ws.Cell(2, 1).Value = "(müəssisənin, idarənin və təşkilatın adı)";
                ws.Range(2, 1, 2, leftEnd).Merge();
                ws.Cell(2, 1).Style.Font.Italic   = true;
                ws.Cell(2, 1).Style.Font.FontSize = 8;
                ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // ── Rənglər ──────────────────────────────────────────────────
            var cHeader    = XLColor.FromArgb(0x1F, 0x49, 0x7D);
            var cHeaderFont= XLColor.White;
            var cIstirahit = XLColor.FromArgb(0xD0, 0xD0, 0xD0);
            var cBayram    = XLColor.FromArgb(0xFF, 0xCC, 0x80);
            var cMez       = XLColor.FromArgb(0xBB, 0xDE, 0xFB);
            var cXest      = XLColor.FromArgb(0xFF, 0xCC, 0xCC);
            var cEzam      = XLColor.FromArgb(0xC8, 0xF0, 0xC8);
            var cAzSaat    = XLColor.FromArgb(0xFF, 0xF0, 0xCC);
            var cYekun     = XLColor.FromArgb(0xE2, 0xEF, 0xDA);

            // ── TƏSDİQ bölməsi (şablondan oxunan dəyərlərlə) ────────────
            ws.Cell(1, tesdiqStart).Value = "TƏSDİQ EDİRƏM:";
            ws.Range(1, tesdiqStart, 1, totalCols).Merge();
            ws.Cell(1, tesdiqStart).Style.Font.Bold     = true;
            ws.Cell(1, tesdiqStart).Style.Font.FontSize = 11;
            ws.Cell(1, tesdiqStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(1).Height = 20;

            ws.Cell(2, tesdiqStart).Value = tplTesdiqRow2;   // ← şablondan
            ws.Range(2, tesdiqStart, 2, totalCols).Merge();
            ws.Cell(2, tesdiqStart).Style.Font.FontSize        = 10;
            ws.Cell(2, tesdiqStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(2).Height = 18;

            ws.Cell(3, tesdiqStart).Value = "(vəzifəsi, soyadı, adı, atasının adı)";
            ws.Range(3, tesdiqStart, 3, totalCols).Merge();
            ws.Cell(3, tesdiqStart).Style.Font.Italic   = true;
            ws.Cell(3, tesdiqStart).Style.Font.FontSize = 8;
            ws.Cell(3, tesdiqStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(3).Height = 14;

            // ── Sətir 4: Boşluq ─────────────────────────────────────────
            ws.Row(4).Height = 6;

            // ── Sətirlər 5-6: İki sıralı sütun başlığı ──────────────────
            const int HR1 = 5;
            const int HR2 = 6;

            ws.Range(HR1, 1, HR2, 1).Merge();
            ws.Cell(HR1, 1).Value = "Sıra\nsayı";

            ws.Range(HR1, 2, HR2, 2).Merge();
            ws.Cell(HR1, 2).Value = "Soyadı, Adı, Ata adı";

            ws.Range(HR1, 3, HR2, 3).Merge();
            ws.Cell(HR1, 3).Value = "Vəzifəsi";

            ws.Range(HR1, 4, HR1, 3 + gunSayi).Merge();
            ws.Cell(HR1, 4).Value = "Ayın günləri";

            for (int d = 1; d <= gunSayi; d++)
                ws.Cell(HR2, 3 + d).Value = d;

            var sumHdrs = new[] { "İş\ngünü", "İş\nsaatı", "Məz.", "Ezam.", "Xəst." };
            for (int i = 0; i < 5; i++)
            {
                int sc = sumStart + i;
                ws.Range(HR1, sc, HR2, sc).Merge();
                ws.Cell(HR1, sc).Value = sumHdrs[i];
            }

            for (int hr = HR1; hr <= HR2; hr++)
            {
                var hrRange = ws.Range(hr, 1, hr, totalCols);
                hrRange.Style.Fill.BackgroundColor = cHeader;
                hrRange.Style.Font.FontColor       = cHeaderFont;
                hrRange.Style.Font.Bold            = true;
                hrRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hrRange.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                hrRange.Style.Alignment.WrapText   = true;
                hrRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                hrRange.Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
            }
            ws.Row(HR1).Height = 22;
            ws.Row(HR2).Height = 18;

            // ── Məlumat sətirləri ─────────────────────────────────────────
            int dr      = HR2 + 1;
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

            // ── CƏMİ sətri ────────────────────────────────────────────────
            ws.Cell(dr, 1).Value = "CƏMİ";
            ws.Range(dr, 1, dr, 3).Merge();
            ws.Cell(dr, 1).Style.Font.Bold            = true;
            ws.Cell(dr, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            for (int i = 0; i <= 4; i++)
            {
                int sc = sumStart + i;
                var fc = ws.Cell(firstDr, sc);
                var lc = ws.Cell(dr - 1, sc);
                var yc = ws.Cell(dr, sc);
                yc.FormulaA1                  = $"=SUM({fc.Address}:{lc.Address})";
                yc.Style.Font.Bold            = true;
                yc.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            ws.Range(dr, 1, dr, totalCols).Style.Fill.BackgroundColor = cYekun;
            ws.Range(dr, 1, dr, totalCols).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            ws.Range(dr, 1, dr, totalCols).Style.Border.InsideBorder  = XLBorderStyleValues.Thin;

            // ── İmza bölməsi (şablondan oxunan dəyərlərlə) ───────────────
            dr += 3;

            // Xətt sırası (şablonun row 7-si)
            ws.Cell(dr, 1).Value = tplSigLineVal;
            ws.Range(dr, 1, dr, 3).Merge();
            ws.Cell(dr, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(dr, 1, dr, 3).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            ws.Row(dr).Height = 20;
            dr++;

            // Ad sırası (şablonun row 8-i) + İmza
            ws.Cell(dr, 1).Value = tplSigNameVal;
            ws.Range(dr, 1, dr, 3).Merge();
            ws.Cell(dr, 1).Style.Font.FontSize = 9;
            ws.Cell(dr, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(dr, 5).Value = tplSigImzaVal;
            ws.Range(dr, 5, dr, 11).Merge();
            ws.Cell(dr, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(dr).Height = 14;

            ws.Row(dr + 1).Height = 6;

            // ── İşarələr (şablondan) ──────────────────────────────────────
            dr += 2;
            ws.Cell(dr, 1).Value = tplLegendVal;
            ws.Range(dr, 1, dr, totalCols).Merge();
            ws.Cell(dr, 1).Style.Font.Italic   = true;
            ws.Cell(dr, 1).Style.Font.FontSize = 9;
            ws.Row(dr).Height = 14;

            // ── Sütun eni ─────────────────────────────────────────────────
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
