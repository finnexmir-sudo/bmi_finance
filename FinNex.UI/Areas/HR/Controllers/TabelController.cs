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

            // ── Şablondan dəyərləri oxu ──────────────────────────────────────
            var templatePath = Path.Combine(_env.ContentRootPath, "App_Data", "Templates", "Tabel_isci.xlsx");
            string tplTesdiqName = "";               // şablonun row 3 sağ tərəfi — müdir adı
            string tplSigNameVal = "müdir müavini ___________________________";
            string tplSigImzaVal = "İmza";

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

                var tplWs = wb.Worksheets.First();

                // TƏSDİQ adı: şablonun row 3 sağ tərəfindən oxu (col 34 = 30 günlük şablon AH3)
                var r3v = tplWs.Cell(3, 34).Value.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(r3v) && !r3v.StartsWith("("))
                    tplTesdiqName = r3v;

                // İmza bölməsi: row 8
                var r8v = tplWs.Cell(8, 2).Value.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(r8v)) tplSigNameVal = r8v;
                var r8e = tplWs.Cell(8, 5).Value.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(r8e)) tplSigImzaVal = r8e;
            }
            else
            {
                wb = new XLWorkbook();
            }
            using var _ = wb;

            // ── Worksheet hazırla ────────────────────────────────────────────
            IXLWorksheet ws;
            if (wb.Worksheets.Count > 0)
            {
                ws      = wb.Worksheets.First();
                ws.Name = "Tabel";
                foreach (var mr in ws.MergedRanges.ToList())
                    ws.MergedRanges.Remove(mr);
                int lastUsed = ws.LastRowUsed()?.RowNumber() ?? 50;
                ws.Range(1, 1, lastUsed + 10, 60).Clear();
            }
            else
            {
                ws = wb.Worksheets.Add("Tabel");
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

            // ── Row 1: Tam en "TABEL" başlığı ────────────────────────────────
            ws.Cell(1, 1).Value = "TABEL — Əsas iş saatları";
            ws.Range(1, 1, 1, totalCols).Merge();
            ws.Cell(1, 1).Style.Fill.BackgroundColor = cHeader;
            ws.Cell(1, 1).Style.Font.FontColor       = cHeaderFont;
            ws.Cell(1, 1).Style.Font.Bold            = true;
            ws.Cell(1, 1).Style.Font.FontSize        = 13;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 1).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 24;

            // ── Row 2: Dövr (sol) + TƏSDİQ EDİRƏM: (sağ) ─────────────────────
            var ayAdlar = new[] { "Yanvar","Fevral","Mart","Aprel","May","İyun",
                                  "İyul","Avqust","Sentyabr","Oktyabr","Noyabr","Dekabr" };
            var ayBaslangicDate = new DateTime(il, ay, 1);
            var ayBitisDate     = new DateTime(il, ay, gunSayi);
            string dovrText = $"Dövr: {il} il, {ayAdlar[ay - 1]} ({ayBaslangicDate:dd.MM.yyyy} – {ayBitisDate:dd.MM.yyyy})";

            int centerEnd = sumStart - 1;

            ws.Cell(2, 1).Value = dovrText;
            ws.Range(2, 1, 2, centerEnd).Merge();
            ws.Cell(2, 1).Style.Font.FontSize        = 10;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 1).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Row(2).Height = 20;

            ws.Cell(2, sumStart).Value = "TƏSDİQ EDİRƏM:";
            ws.Range(2, sumStart, 2, totalCols).Merge();
            ws.Cell(2, sumStart).Style.Font.Bold            = true;
            ws.Cell(2, sumStart).Style.Font.FontSize        = 11;
            ws.Cell(2, sumStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, sumStart).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;

            // ── Row 3: boş (sol) + müdir adı (sağ, şablondan) ───────────────
            if (!string.IsNullOrWhiteSpace(tplTesdiqName))
            {
                ws.Cell(3, sumStart).Value = tplTesdiqName;
                ws.Range(3, sumStart, 3, totalCols).Merge();
                ws.Cell(3, sumStart).Style.Font.FontSize        = 10;
                ws.Cell(3, sumStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(3, sumStart).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            }
            ws.Row(3).Height = 18;

            // ── Row 4: Boşluq ────────────────────────────────────────────────
            ws.Row(4).Height = 6;

            // ── Row 4: Sütun başlıqları (tək sətir) ──────────────────────────
            const int HDR = 4;

            ws.Cell(HDR, 1).Value = "#";
            ws.Cell(HDR, 2).Value = "Soyadı, Adı, Ata adı";
            ws.Cell(HDR, 3).Value = "Vəzifəsi";

            for (int d = 1; d <= gunSayi; d++)
                ws.Cell(HDR, 3 + d).Value = d;

            var sumHdrs = new[] { "İş\ngünü", "İş\nsaatı", "Məz.", "Ezam.", "Xəst." };
            for (int i = 0; i < 5; i++)
                ws.Cell(HDR, sumStart + i).Value = sumHdrs[i];

            var hdrRange = ws.Range(HDR, 1, HDR, totalCols);
            hdrRange.Style.Fill.BackgroundColor = cHeader;
            hdrRange.Style.Font.FontColor       = cHeaderFont;
            hdrRange.Style.Font.Bold            = true;
            hdrRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hdrRange.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            hdrRange.Style.Alignment.WrapText   = true;
            hdrRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            hdrRange.Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
            ws.Row(HDR).Height = 22;

            // ── Məlumat sətirləri ─────────────────────────────────────────────
            int dr      = HDR + 1;
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

            // ── CƏMİ sətri ────────────────────────────────────────────────────
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
            ws.Row(dr).Height = 16;
            dr++;

            // ── İmza bölməsi ──────────────────────────────────────────────────
            dr += 2;
            ws.Range(dr, 2, dr, 10).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            ws.Row(dr).Height = 20;
            dr++;

            ws.Cell(dr, 2).Value = tplSigNameVal;
            ws.Range(dr, 2, dr, 10).Merge();
            ws.Cell(dr, 2).Style.Font.FontSize        = 9;
            ws.Cell(dr, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(dr, 12).Value = tplSigImzaVal;
            ws.Range(dr, 12, dr, 18).Merge();
            ws.Cell(dr, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(dr).Height = 14;
            dr++;

            // ── Şərti işarələr ────────────────────────────────────────────────
            dr += 1;
            ws.Cell(dr, 1).Value = "Şərti işarələr:";
            ws.Cell(dr, 1).Style.Font.Bold     = true;
            ws.Cell(dr, 1).Style.Font.FontSize = 9;
            ws.Row(dr).Height = 16;
            dr++;

            var legends = new (string kod, string desc, XLColor color)[]
            {
                ("İ", "– İstirahət günü", cIstirahit),
                ("B", "– Bayram günü",    cBayram),
                ("M", "– Məzuniyyət",     cMez),
                ("X", "– Xəstəlik",       cXest),
                ("E", "– Ezamiyyət",      cEzam),
            };

            foreach (var (kod, desc, color) in legends)
            {
                ws.Cell(dr, 1).Value = kod;
                ws.Cell(dr, 1).Style.Fill.BackgroundColor = color;
                ws.Cell(dr, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(dr, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(dr, 1).Style.Font.Bold            = true;
                ws.Cell(dr, 1).Style.Font.FontSize        = 9;
                ws.Cell(dr, 2).Value = desc;
                ws.Cell(dr, 2).Style.Font.Italic          = true;
                ws.Cell(dr, 2).Style.Font.FontSize        = 9;
                ws.Row(dr).Height = 14;
                dr++;
            }

            // ── Sütun eni ─────────────────────────────────────────────────────
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

            ws.SheetView.FreezeRows(HDR);
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
