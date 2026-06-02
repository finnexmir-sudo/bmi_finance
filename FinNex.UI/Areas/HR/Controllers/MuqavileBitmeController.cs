using ClosedXML.Excel;
using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.HR.ViewModels.MuqavileBitme;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class MuqavileBitmeController : Controller
    {
        private readonly AppDbContext _db;

        public MuqavileBitmeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int gun = 30, int? departamentId = null, string? search = null)
        {
            if (gun < 1)   gun = 30;
            if (gun > 365) gun = 365;

            var rows = await LoadRowsAsync();

            var depts = rows
                .GroupBy(r => new { r.DepartamentAd })
                .Where(g => g.Key.DepartamentAd != "—")
                .Select(g => new SelectListItem(g.Key.DepartamentAd, g.First().DepartamentId.ToString(), g.First().DepartamentId == departamentId))
                .OrderBy(s => s.Text)
                .ToList();

            rows = ApplyFilters(rows, gun, departamentId, search);

            return View(new MuqavileBitmeIndexVM
            {
                Rows           = rows,
                Gun            = gun,
                DepartamentId  = departamentId,
                Search         = search,
                Departamentler = depts
            });
        }

        public async Task<IActionResult> Excel(int gun = 30, int? departamentId = null, string? search = null)
        {
            if (gun < 1)   gun = 30;
            if (gun > 365) gun = 365;

            var rows = ApplyFilters(await LoadRowsAsync(), gun, departamentId, search);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Müqavilə Bitmə");

            var headers = new[] { "#", "Ad", "Soyad", "Ata Adı", "FIN", "Departament", "Vəzifə", "İşə Qəbul", "Müqavilə Bitir", "Qalan Gün" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var hRange = ws.Range(1, 1, 1, headers.Length);
            hRange.Style.Font.Bold = true;
            hRange.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                int c = 1;
                ws.Cell(i + 2, c++).Value = i + 1;
                ws.Cell(i + 2, c++).Value = r.Ad;
                ws.Cell(i + 2, c++).Value = r.Soyad;
                ws.Cell(i + 2, c++).Value = r.AtaAdi ?? "";
                ws.Cell(i + 2, c++).Value = r.FIN;
                ws.Cell(i + 2, c++).Value = r.DepartamentAd;
                ws.Cell(i + 2, c++).Value = r.VezifeAd;
                ws.Cell(i + 2, c++).Value = r.IsheQebulTarixi.ToString("dd.MM.yyyy");
                ws.Cell(i + 2, c++).Value = r.BitmeTarixi.ToString("dd.MM.yyyy");
                ws.Cell(i + 2, c++).Value = r.QalanGun;

                if (r.QalanGun < 0)
                    ws.Row(i + 2).Style.Fill.BackgroundColor = XLColor.LightPink;
                else if (r.QalanGun <= 7)
                    ws.Row(i + 2).Style.Fill.BackgroundColor = XLColor.LightYellow;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Muqavile_Bitme_{DateTime.Today:yyyy-MM-dd}.xlsx");
        }

        private async Task<List<MuqavileBitmeRowVM>> LoadRowsAsync()
        {
            var bugun = DateTime.Today;

            var teyinatlar = await _db.Set<IsciTeyinat>()
                .AsNoTracking()
                .Include(t => t.Isci)
                .Include(t => t.Departament)
                .Include(t => t.Vezife)
                .Where(t => !t.Silinib && t.Aktivdir && t.BitmeTarixi.HasValue)
                .ToListAsync();

            return teyinatlar
                .Select(t => new MuqavileBitmeRowVM
                {
                    IsciId          = t.IsciId,
                    TeyinatId       = t.Id,
                    DepartamentId   = t.DepartamentId,
                    Ad              = t.Isci.Ad,
                    Soyad           = t.Isci.Soyad,
                    AtaAdi          = t.Isci.AtaAdi,
                    FIN             = t.Isci.FIN ?? "",
                    DepartamentAd   = t.Departament?.Ad ?? "—",
                    VezifeAd        = t.Vezife?.Ad ?? "—",
                    IsheQebulTarixi = t.Isci.IsheQebulTarixi,
                    BitmeTarixi     = t.BitmeTarixi!.Value,
                    QalanGun        = (t.BitmeTarixi!.Value.Date - bugun).Days
                })
                .OrderBy(r => r.QalanGun)
                .ToList();
        }

        private static List<MuqavileBitmeRowVM> ApplyFilters(
            List<MuqavileBitmeRowVM> rows, int gun, int? departamentId, string? search)
        {
            rows = rows.Where(r => r.QalanGun <= gun).ToList();

            if (departamentId.HasValue)
                rows = rows.Where(r => r.DepartamentId == departamentId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLower();
                rows = rows.Where(r =>
                    r.TamAd.ToLower().Contains(q) ||
                    r.FIN.ToLower().Contains(q)).ToList();
            }

            return rows;
        }
    }
}
