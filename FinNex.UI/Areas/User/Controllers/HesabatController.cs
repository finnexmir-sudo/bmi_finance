// Areas/User/Controllers/HesabatController.cs
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using FinNex.UI.Areas.User.ViewModels.Hesabat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO.Packaging;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize(Roles = RoleNames.SobeReisi + "," + RoleNames.Rehber + "," + RoleNames.HR + "," + RoleNames.Admin)]
public class HesabatController : Controller
{
    private readonly IMezuniyyetService _mezuniyyetService;
    private readonly IIcazeService _icazeService;
    private readonly IDepartmentService _departamentService;
    private readonly UserManager<AppUser> _userManager;

    public HesabatController(
        IMezuniyyetService mezuniyyetService,
        IIcazeService icazeService,
        IDepartmentService departamentService,
        UserManager<AppUser> userManager)
    {
        _mezuniyyetService = mezuniyyetService;
        _icazeService = icazeService;
        _departamentService = departamentService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(HesabatFilterVM? filter, bool search = false)
    {


        // Departament dropdown
        var deptResult = await _departamentService.HamisiniGetirAsync();
        var deptList = deptResult.Success
            ? deptResult.Data!.Select(d => new SelectListItem(d.Ad, d.Id.ToString())).ToList()
            : new();
        deptList.Insert(0, new SelectListItem("Bütün şöbələr", ""));

        // Filtrli məlumatlar
        var mezResult = await _mezuniyyetService.GetFiltrliAsync(
            filter.BaslamaTarixFrom,
            filter.BaslamaTarixTo,
            filter.DepartamentId,
            filter.Nov == "Icaze" ? null : filter.Status,
            filter.Axtaris);

        var icazeResult = await _icazeService.GetFiltrliAsync(
            filter.BaslamaTarixFrom,
            filter.BaslamaTarixTo,
            filter.DepartamentId,
            filter.Nov == "Mezuniyyet" ? null : filter.Status,
            filter.Axtaris);

        var mezuniyyetler = mezResult.Success ? mezResult.Data! : new List<Application.DTOs.HR.Mezuniyyet.MezuniyyetListDto>();
        var icazeler = icazeResult.Success ? icazeResult.Data! : new List<Application.DTOs.HR.Icaze.IcazeListDto>();

        // Satirlara çevir
        var satirlar = new List<HesabatSatirVM>();

        if (filter.Nov != "Icaze")
        {
            satirlar.AddRange(mezuniyyetler.Select(m => new HesabatSatirVM
            {
                Id = m.Id,
                Nov = "Məzuniyyət",
                NovIcon = "calendar",
                IsciAdSoyad = m.IsciAdSoyad,
                SobeAdi = m.SobeAdi,
                VezifeAdi = m.VezifeAdi,
                MezuniyyetNov = m.NovText,
                BaslamaTarixi = m.BaslamaTarixi,
                BitmeTarixi = m.BitmeTarixi,
                IsGunlerininSayi = m.IsGunlerininSayi,
                StatusText = m.WorkflowMerhele,
                StatusValue = (int)m.Status,
                EvezEden = m.EvezEdenIsciAdSoyad ?? "-",
                SobeReisiTesdiq = m.SobeReisiTesdiq,
                SobeReisiAd = m.SobeReisiAdSoyad,
                SobeReisiTesdiqTarixi = m.SobeReisiTesdiqTarixi,
                RehberTesdiq = m.RehberTesdiq,
                RehberAd = m.RehberAdSoyad,
                RehberTesdiqTarixi = m.RehberTesdiqTarixi,
                HrTesdiq = m.HrTesdiq,
                HrAd = m.HrAdSoyad,
                HrTesdiqTarixi = m.HrTesdiqTarixi,
                ImtinaSebebi = m.ImtinaSebebi,
                YaradilmaTarixi = m.YaradilmaTarixi,
            }));
        }

        if (filter.Nov != "Mezuniyyet")
        {
            satirlar.AddRange(icazeler.Select(ic => new HesabatSatirVM
            {
                Id = ic.Id,
                Nov = "İcazə",
                NovIcon = "clock",
                IsciAdSoyad = ic.IsciAdSoyad,
                SobeAdi = ic.SobeAdi,
                IcazeTarixi = ic.IcazeTarixi,
                BaslamaSaati = ic.BaslamaSaati,
                BitisSaati = ic.BitisSaati,
                IcazeSaati = ic.IcazeSaati,
                StatusText = ic.WorkflowMerhele,
                StatusValue = (int)ic.Status,
                YaradilmaTarixi = ic.IcazeTarixi,
            }));
        }

        satirlar = satirlar.OrderByDescending(x => x.YaradilmaTarixi).ToList();

        // ── Səhifələmə ──
        var totalCount = satirlar.Count;
        var pageSize = filter.PageSize <= 0 ? 25 : Math.Min(filter.PageSize, 200);
        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;
        var page = Math.Max(1, filter.Page);
        if (page > totalPages) page = totalPages;
        var pagedSatirlar = satirlar.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var buAy = DateTime.Now;
        var vm = new HesabatIndexVM
        {
            Filter = filter,
            Satirlar = pagedSatirlar,
            Departamentler = deptList,
            CemiMezuniyyet = mezuniyyetler.Count,
            CemiIcaze = icazeler.Count,
            // Statistika SİYAHININ TAM məbləğindən hesablanır (səhifələnməmiş)
            Gozleyen = satirlar.Count(x => x.StatusValue is 1 or 2 or 3 or 4),
            Tesdiqlenib = satirlar.Count(x => x.StatusValue == 5),
            ImtinaEdildi = satirlar.Count(x => x.StatusValue == 6),
            BuAyMezuniyyet = mezuniyyetler.Count(x =>
                x.BaslamaTarixi.Month == buAy.Month && x.BaslamaTarixi.Year == buAy.Year),
            BuAyIcaze = icazeler.Count(x =>
                x.IcazeTarixi.Month == buAy.Month && x.IcazeTarixi.Year == buAy.Year),
            TotalCount = totalCount,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
        };


        ViewData["Title"] = "Hesabat";
        return View(vm);
    }

    // Excel export
    [HttpGet]
    public async Task<IActionResult> ExportExcel(HesabatFilterVM filter)
    {
        var mezResult = await _mezuniyyetService.GetFiltrliAsync(
            filter.BaslamaTarixFrom, filter.BaslamaTarixTo,
            filter.DepartamentId, filter.Status, filter.Axtaris);

        var icazeResult = await _icazeService.GetFiltrliAsync(
            filter.BaslamaTarixFrom, filter.BaslamaTarixTo,
            filter.DepartamentId, filter.Status, filter.Axtaris);

        using var wb = new ClosedXML.Excel.XLWorkbook();

        // Məzuniyyət sheet
        var wsMez = wb.Worksheets.Add("Məzuniyyətlər");
        var mezHeaders = new[] { "ID", "İşçi", "Şöbə", "Vəzifə", "Növ", "Başlama", "Bitmə", "İş Günü", "Status", "Əvəzedici", "Şöbə Rəisi", "Rəhbər", "HR", "İmtina Səbəbi" };
        for (int i = 0; i < mezHeaders.Length; i++)
            wsMez.Cell(1, i + 1).Value = mezHeaders[i];

        if (mezResult.Success)
        {
            int row = 2;
            foreach (var m in mezResult.Data!)
            {
                wsMez.Cell(row, 1).Value = m.Id;
                wsMez.Cell(row, 2).Value = m.IsciAdSoyad;
                wsMez.Cell(row, 3).Value = m.SobeAdi;
                wsMez.Cell(row, 4).Value = m.VezifeAdi;
                wsMez.Cell(row, 5).Value = m.NovText;
                wsMez.Cell(row, 6).Value = m.BaslamaTarixi.ToString("dd.MM.yyyy");
                wsMez.Cell(row, 7).Value = m.BitmeTarixi.ToString("dd.MM.yyyy");
                wsMez.Cell(row, 8).Value = m.IsGunlerininSayi;
                wsMez.Cell(row, 9).Value = m.WorkflowMerhele;
                wsMez.Cell(row, 10).Value = m.EvezEdenIsciAdSoyad ?? "-";
                wsMez.Cell(row, 11).Value = m.SobeReisiAdSoyad ?? "-";
                wsMez.Cell(row, 12).Value = m.RehberAdSoyad ?? "-";
                wsMez.Cell(row, 13).Value = m.HrAdSoyad ?? "-";
                wsMez.Cell(row, 14).Value = m.ImtinaSebebi ?? "-";
                row++;
            }
        }

        wsMez.Columns().AdjustToContents();

        // İcazə sheet
        var wsIcaze = wb.Worksheets.Add("İcazələr");
        var icazeHeaders = new[] { "ID", "İşçi", "Şöbə", "Tarix", "Başlama", "Bitmə", "Saat", "Status", "Əvəzedici" };
        for (int i = 0; i < icazeHeaders.Length; i++)
            wsIcaze.Cell(1, i + 1).Value = icazeHeaders[i];

        if (icazeResult.Success)
        {
            int row = 2;
            foreach (var ic in icazeResult.Data!)
            {
                wsIcaze.Cell(row, 1).Value = ic.Id;
                wsIcaze.Cell(row, 2).Value = ic.IsciAdSoyad;
                wsIcaze.Cell(row, 3).Value = ic.SobeAdi;
                wsIcaze.Cell(row, 4).Value = ic.IcazeTarixi.ToString("dd.MM.yyyy");
                wsIcaze.Cell(row, 5).Value = ic.BaslamaSaati.ToString(@"hh\:mm");
                wsIcaze.Cell(row, 6).Value = ic.BitisSaati.ToString(@"hh\:mm");
                wsIcaze.Cell(row, 7).Value = ic.IcazeSaati.ToString("0.##");
                wsIcaze.Cell(row, 8).Value = ic.WorkflowMerhele;
                row++;
            }
        }

        wsIcaze.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var fileName = $"Hesabat_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
