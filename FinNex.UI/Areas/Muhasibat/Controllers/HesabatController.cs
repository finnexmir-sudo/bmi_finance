using System.Globalization;
using System.IO;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Muhasibat;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;

namespace FinNex.UI.Areas.Muhasibat.Controllers;

// Mühasibat → Hesabatlar. Requlyativ/analitik hesabatlar (IFRS 9 ECL, AMB MHBS 9 ...).
// Giriş qaydası Dashboard ilə eyni: Admin/Muhasib/Rehber avtomatik + "muhasibat_dashboard_bax" icazəsi.
[Area("Muhasibat")]
[Authorize]
public class HesabatController : Controller
{
    private readonly IMuhasibatService _service;
    private readonly IUserPermissionService _perm;
    private readonly UserManager<AppUser> _userManager;

    public HesabatController(
        IMuhasibatService service,
        IUserPermissionService perm,
        UserManager<AppUser> userManager)
    {
        _service = service;
        _perm = perm;
        _userManager = userManager;
    }

    private async Task<bool> IcazeVarAsync()
    {
        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Muhasib) || User.IsInRole(RoleNames.Rehber))
            return true;

        var u = await _userManager.GetUserAsync(User);
        if (u == null) return false;

        var res = await _perm.HasPermissionAsync(u.Id, DashboardController.IcazeKod);
        return res.Success && res.Data == true;
    }

    // Hesabatlar açılış səhifəsi — mövcud hesabatların siyahısı.
    public async Task<IActionResult> Index()
    {
        if (!await IcazeVarAsync())
            return Forbid();

        return View();
    }

    // IFRS 9 ECL — gözlənilən kredit itkiləri (roll-rate stage keçid modeli).
    public async Task<IActionResult> Ifrs9(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.Ifrs9EclAsync(ParseTarix(t));
        return View(model);
    }

    // IFRS 9 detalını (kredit-kredit) Excel-ə çıxar.
    public async Task<IActionResult> Ifrs9Excel(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var m = await _service.Ifrs9EclAsync(ParseTarix(t));

        var wb = new HSSFWorkbook();

        // Vərəq 1 — Stage xülasəsi
        var sx = wb.CreateSheet("Stage xulase");
        int r = 0;
        sx.CreateRow(r++).CreateCell(0).SetCellValue($"IFRS 9 ECL — {m.Tarix:dd.MM.yyyy}");
        r++;
        var sh = sx.CreateRow(r++);
        sh.CreateCell(0).SetCellValue("Stage");
        sh.CreateCell(1).SetCellValue("Say");
        sh.CreateCell(2).SetCellValue("Portfel (EAD)");
        sh.CreateCell(3).SetCellValue("Risk %");
        sh.CreateCell(4).SetCellValue("ECL (ehtiyat)");
        sh.CreateCell(5).SetCellValue("FINA ehtiyat");
        foreach (var s in m.Stagelar)
        {
            var row = sx.CreateRow(r++);
            row.CreateCell(0).SetCellValue(s.Stage);
            row.CreateCell(1).SetCellValue(s.Say);
            row.CreateCell(2).SetCellValue((double)s.Ead);
            row.CreateCell(3).SetCellValue((double)s.RiskFaiz);
            row.CreateCell(4).SetCellValue((double)s.Ecl);
            row.CreateCell(5).SetCellValue((double)s.BankEhtiyat);
        }
        r++;
        var tr = sx.CreateRow(r++);
        tr.CreateCell(0).SetCellValue("CƏMİ");
        tr.CreateCell(1).SetCellValue(m.Say);
        tr.CreateCell(2).SetCellValue((double)m.UmumiPortfel);
        tr.CreateCell(3).SetCellValue((double)m.EclFaiz);
        tr.CreateCell(4).SetCellValue((double)m.UmumiEcl);
        tr.CreateCell(5).SetCellValue((double)m.BankEhtiyat);

        // Vərəq 2 — kredit-kredit detal
        var sd = wb.CreateSheet("Detal");
        r = 0;
        var dh = sd.CreateRow(r++);
        dh.CreateCell(0).SetCellValue("Hesab");
        dh.CreateCell(1).SetCellValue("Tip");
        dh.CreateCell(2).SetCellValue("Sahe kodu");
        dh.CreateCell(3).SetCellValue("Sahe");
        dh.CreateCell(4).SetCellValue("Stage");
        dh.CreateCell(5).SetCellValue("Gecikme gunu");
        dh.CreateCell(6).SetCellValue("EAD");
        dh.CreateCell(7).SetCellValue("Risk %");
        dh.CreateCell(8).SetCellValue("ECL");
        dh.CreateCell(9).SetCellValue("FINA ehtiyat");
        foreach (var x in m.Setirler)
        {
            var row = sd.CreateRow(r++);
            row.CreateCell(0).SetCellValue(x.Hesab);
            row.CreateCell(1).SetCellValue(x.Tip);
            row.CreateCell(2).SetCellValue(x.SaheKodu);
            row.CreateCell(3).SetCellValue(x.SaheAdi);
            row.CreateCell(4).SetCellValue(x.Stage);
            row.CreateCell(5).SetCellValue(x.Dpd);
            row.CreateCell(6).SetCellValue((double)x.Ead);
            row.CreateCell(7).SetCellValue((double)x.RiskFaiz);
            row.CreateCell(8).SetCellValue((double)x.Ecl);
            row.CreateCell(9).SetCellValue((double)x.BankEhtiyat);
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        var ad = $"IFRS9_ECL_{m.Tarix:yyyyMMdd}.xls";
        return File(ms.ToArray(), "application/vnd.ms-excel", ad);
    }

    private static DateTime? ParseTarix(string? t)
    {
        if (!string.IsNullOrWhiteSpace(t) &&
            DateTime.TryParseExact(t.Trim(),
                new[] { "dd-MM-yyyy", "yyyy-MM-dd", "dd/MM/yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }
        return null;
    }
}
