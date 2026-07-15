using System.Globalization;
using System.IO;
using FinNex.Application.DTOs.Muhasibat;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Muhasibat;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace FinNex.UI.Areas.Muhasibat.Controllers;

// Mühasibat / Maliyyə Dashboard.
// Giriş: icazə əsaslı — Admin və Muhasib avtomatik; digər şöbələr (audit, risk...)
// "muhasibat_dashboard_bax" icazəsi ilə (Admin panel → Permissions/UserPermissions).
[Area("Muhasibat")]
[Authorize]
public class DashboardController : Controller
{
    public const string IcazeKod = "muhasibat_dashboard_bax";

    private readonly IMuhasibatService _service;
    private readonly IUserPermissionService _perm;
    private readonly UserManager<AppUser> _userManager;

    public DashboardController(
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
        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Muhasib))
            return true;

        var u = await _userManager.GetUserAsync(User);
        if (u == null) return false;

        var res = await _perm.HasPermissionAsync(u.Id, IcazeKod);
        return res.Success && res.Data == true;
    }

    // Balans İcmalı. t = seçilmiş tarix (dd-MM-yyyy / yyyy-MM-dd).
    public async Task<IActionResult> Index(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.BalansAsync(ParseTarix(t));
        return View(model);
    }

    // Depozitlər.
    public async Task<IActionResult> Depozit(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.DepozitAsync(ParseTarix(t));
        return View(model);
    }

    // Kredit portfeli (cari vəziyyət).
    public async Task<IActionResult> Kredit()
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.KreditPortfelAsync();
        return View(model);
    }

    // Likvidlik.
    public async Task<IActionResult> Likvidlik(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.LikvidlikAsync(ParseTarix(t));
        return View(model);
    }

    // Valyuta əməliyyatları (tarix aralığı: bt=başlanğıc, st=son).
    public async Task<IActionResult> Valyuta(string? bt, string? st)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.ValyutaAsync(ParseTarix(bt), ParseTarix(st));
        return View(model);
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

    // ── Excel ixrac ────────────────────────────────────────────────────────

    public async Task<IActionResult> BalansExcel(string? t)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.BalansAsync(ParseTarix(t));
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Balans");
        int r = 0;
        Setir(sh, r++, $"Balans İcmalı — {m.Tarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Ümumi Aktiv", m.UmumiAktiv);
        KV(sh, r++, "Ümumi Öhdəlik", m.UmumiOhdelik);
        KV(sh, r++, "Kapital", m.Kapital);
        KV(sh, r++, "Xalis mənfəət (YTD)", m.XalisMenfeet);
        KV(sh, r++, "ROA %", m.Roa);
        KV(sh, r++, "ROE %", m.Roe);
        r++;
        r = Bolme(sh, r, "Aktivlərin strukturu", m.Aktivler);
        r = Bolme(sh, r, "Öhdəliklərin strukturu", m.Ohdelikler);
        r = Bolme(sh, r, "Aktivlərin valyuta strukturu", m.ValyutaBolgusu);
        return Yukle(wb, "Balans_Icmali");
    }

    public async Task<IActionResult> DepozitExcel(string? t)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.DepozitAsync(ParseTarix(t));
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Depozit");
        int r = 0;
        Setir(sh, r++, $"Depozit Portfeli — {m.Tarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Ümumi portfel", m.UmumiPortfel);
        KV(sh, r++, "Hüquqi şəxs", m.HuquqiCem);
        KV(sh, r++, "Fiziki şəxs", m.FizikiCem);
        KV(sh, r++, "Depozitor sayı", m.MusteriSayi);
        KV(sh, r++, "TOP-10 payı %", m.Top10Pay);
        KV(sh, r++, "TOP-20 payı %", m.Top20Pay);
        r++;
        r = Bolme(sh, r, "Fiziki / Hüquqi", m.TipBolgusu);
        r = Bolme(sh, r, "Valyuta strukturu", m.ValyutaBolgusu);
        r = Bolme(sh, r, "TOP-10 hüquqi depozitor", m.TopHuquqi);
        r = Bolme(sh, r, "TOP-10 fiziki depozitor", m.TopFiziki);
        return Yukle(wb, "Depozit_Portfeli");
    }

    public async Task<IActionResult> KreditExcel()
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.KreditPortfelAsync();
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Kredit");
        int r = 0;
        Setir(sh, r++, $"Kredit Portfeli — {m.Tarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Ümumi portfel", m.UmumiPortfel);
        KV(sh, r++, "Müqavilə sayı", m.MuqavileSayi);
        KV(sh, r++, "VK qalıq", m.VkMebleg);
        KV(sh, r++, "NPL (90+) məbləğ", m.NplMebleg);
        KV(sh, r++, "NPL %", m.NplFaiz);
        r++;
        r = Bolme(sh, r, "Müştəri seqmenti", m.TipBolgusu);
        r = Bolme(sh, r, "Təyinat üzrə", m.TeyinatBolgusu);
        r = Bolme(sh, r, "Gecikmə (aging)", m.GecikmeBolgusu);
        r = Bolme(sh, r, "Valyuta strukturu", m.ValyutaBolgusu);
        return Yukle(wb, "Kredit_Portfeli");
    }

    public async Task<IActionResult> LikvidlikExcel(string? t)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.LikvidlikAsync(ParseTarix(t));
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Likvidlik");
        int r = 0;
        Setir(sh, r++, $"Likvidlik — {m.Tarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Likvid aktivlər", m.LikvidAktiv);
        KV(sh, r++, "Ümumi öhdəlik", m.UmumiOhdelik);
        KV(sh, r++, "Ani likvidlik %", m.AniLikvidlik);
        KV(sh, r++, "Likvid / Aktiv %", m.LikvidAktivPay);
        r++;
        r = Bolme(sh, r, "Likvid aktivlərin strukturu", m.LikvidStruktur);
        r = Bolme(sh, r, "Valyuta strukturu", m.ValyutaBolgusu);
        return Yukle(wb, "Likvidlik");
    }

    public async Task<IActionResult> ValyutaExcel(string? bt, string? st)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.ValyutaAsync(ParseTarix(bt), ParseTarix(st));
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Valyuta");
        int r = 0;
        Setir(sh, r++, $"Valyuta əməliyyatları — {m.BasTarix:dd.MM.yyyy} — {m.SonTarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Ümumi alış (AZN)", m.AlisAzn);
        KV(sh, r++, "Ümumi satış (AZN)", m.SatisAzn);
        KV(sh, r++, "Xalis (satış−alış)", m.Xalis);
        KV(sh, r++, "Əməliyyat sayı", m.EmeliyyatSayi);
        r++;
        var h = sh.CreateRow(r++);
        string[] basliqlar = { "Valyuta", "Alış həcm", "Alış AZN", "Satış həcm", "Satış AZN", "Orta alış", "Orta satış", "Spred", "Açıq mövqe" };
        for (int c = 0; c < basliqlar.Length; c++) h.CreateCell(c).SetCellValue(basliqlar[c]);
        foreach (var s in m.Setirler)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(s.Valyuta);
            row.CreateCell(1).SetCellValue((double)s.AlisHecm);
            row.CreateCell(2).SetCellValue((double)s.AlisAzn);
            row.CreateCell(3).SetCellValue((double)s.SatisHecm);
            row.CreateCell(4).SetCellValue((double)s.SatisAzn);
            row.CreateCell(5).SetCellValue((double)s.OrtaAlisKurs);
            row.CreateCell(6).SetCellValue((double)s.OrtaSatisKurs);
            row.CreateCell(7).SetCellValue((double)s.Spred);
            row.CreateCell(8).SetCellValue((double)s.AcigMovqe);
        }
        return Yukle(wb, "Valyuta_Emeliyyatlari");
    }

    // ── Excel köməkçiləri ──────────────────────────────────────────────────

    private static void Setir(ISheet sh, int r, string mətn) =>
        sh.CreateRow(r).CreateCell(0).SetCellValue(mətn);

    private static void KV(ISheet sh, int r, string ad, decimal deyer)
    {
        var row = sh.CreateRow(r);
        row.CreateCell(0).SetCellValue(ad);
        row.CreateCell(1).SetCellValue((double)deyer);
    }

    private static int Bolme(ISheet sh, int r, string baslik, IEnumerable<BalansMaddeDto> list)
    {
        sh.CreateRow(r++).CreateCell(0).SetCellValue(baslik);
        var h = sh.CreateRow(r++);
        h.CreateCell(0).SetCellValue("Ad");
        h.CreateCell(1).SetCellValue("Məbləğ (AZN)");
        h.CreateCell(2).SetCellValue("Pay %");
        foreach (var x in list)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(x.Ad);
            row.CreateCell(1).SetCellValue((double)x.Mebleg);
            row.CreateCell(2).SetCellValue((double)x.Faiz);
        }
        return r + 1;
    }

    private FileContentResult Yukle(HSSFWorkbook wb, string ad)
    {
        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return File(ms.ToArray(), "application/vnd.ms-excel", $"{ad}_{DateTime.Now:yyyyMMdd}.xls");
    }
}
