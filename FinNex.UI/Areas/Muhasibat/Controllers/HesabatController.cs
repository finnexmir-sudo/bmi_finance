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

    // AMB MHBS 9 — Cədvəl A1 (amortizasiya olunmuş dəyərdə kredit portfeli) Excel ixracı.
    // A alt-cədvəli (bütün valyuta) + B (xarici valyuta). C/D (FVOCI) — bankda yoxdur, 0.
    public async Task<IActionResult> AmbA1Excel(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var m = await _service.AmbA1Async(ParseTarix(t));

        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("A1");
        int r = 0;
        sh.CreateRow(r++).CreateCell(1).SetCellValue($"CƏDVƏL A1 — {m.Tarix:dd.MM.yyyy}");
        sh.CreateRow(r++).CreateCell(1).SetCellValue("(min manatla)");
        r++;
        r = AmbSubCedvel(sh, r, "A. Amortizasiya olunmuş dəyərdə kredit portfeli",
                         "(bütün kreditlər, xarici valyutada kreditlər də daxil olmaqla)", m.Butun);
        r = AmbSubCedvel(sh, r, "B. Amortizasiya olunmuş dəyərdə kredit portfeli",
                         "(xarici valyutada)", m.Xarici);
        r = AmbSubCedvel(sh, r, "C. Digər məcmu gəlirdə ədalətli dəyərlə ölçülmüş kredit portfeli",
                         "(bütün kreditlər — bankda yoxdur)", new Dictionary<string, AmbHuceyre>());
        r = AmbSubCedvel(sh, r, "D. Digər məcmu gəlirdə ədalətli dəyərlə ölçülmüş kredit portfeli",
                         "(xarici valyutada — bankda yoxdur)", new Dictionary<string, AmbHuceyre>());

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return File(ms.ToArray(), "application/vnd.ms-excel", $"AMB_MHBS9_A1_{m.Tarix:yyyyMMdd}.xls");
    }

    // Bir A1 alt-cədvəlini yaz (17 sətir: cəmi, biznes+1.1–1.7, istehlak+2.1–2.5, daşınmaz əmlak, digər).
    // Sütunlar AMB şablonu ilə eyni: B=ad, D=Cəmi E=M1 F=M2 G=M3 H=POCI, I=ECL Cəmi J=M1 K=M2 L=M3 M=POCI.
    private static int AmbSubCedvel(ISheet sh, int r, string title, string subtitle,
                                    Dictionary<string, AmbHuceyre> map)
    {
        var biznes = new[] { "1_1", "1_2", "1_3", "1_4", "1_5", "1_6", "1_7" };
        var istehlak = new[] { "2_1", "2_2", "2_3", "2_4", "2_5" };
        var hamisi = biznes.Concat(istehlak).Concat(new[] { "3", "4" }).ToArray();

        sh.CreateRow(r++).CreateCell(1).SetCellValue(title);
        sh.CreateRow(r++).CreateCell(1).SetCellValue(subtitle);
        var h1 = sh.CreateRow(r++);
        h1.CreateCell(1).SetCellValue("Kreditlərin növləri");
        h1.CreateCell(3).SetCellValue("Ümumi məbləğ");
        h1.CreateCell(8).SetCellValue("Gözlənilən kredit zərəri");
        var h2 = sh.CreateRow(r++);
        string[] basliq = { "Cəmi", "Mərhələ 1", "Mərhələ 2", "Mərhələ 3", "POCI" };
        for (int i = 0; i < 5; i++) { h2.CreateCell(3 + i).SetCellValue(basliq[i]); h2.CreateCell(8 + i).SetCellValue(basliq[i]); }

        AmbSetir(sh, r++, "Müştərilərə verilmiş kreditlər, cəmi", AmbCem(map, hamisi));
        AmbSetir(sh, r++, "1. Biznes kreditləri", AmbCem(map, biznes));
        AmbSetir(sh, r++, "1.1 Sənaye", AmbAl(map, "1_1"));
        AmbSetir(sh, r++, "1.2 Kənd təsərrüfatı", AmbAl(map, "1_2"));
        AmbSetir(sh, r++, "1.3 Tikinti sahəsi", AmbAl(map, "1_3"));
        AmbSetir(sh, r++, "1.4 Nəqliyyat", AmbAl(map, "1_4"));
        AmbSetir(sh, r++, "1.5 İnformasiya və Rabitə", AmbAl(map, "1_5"));
        AmbSetir(sh, r++, "1.6 Ticarət müəssisələrinə kredit", AmbAl(map, "1_6"));
        AmbSetir(sh, r++, "1.7 Digər qeyri-istehsal və xidmət sahələri", AmbAl(map, "1_7"));
        AmbSetir(sh, r++, "2. İstehlak kreditləri", AmbCem(map, istehlak));
        AmbSetir(sh, r++, "2.1 Yaşayış sahəsinin təmirinə", AmbAl(map, "2_1"));
        AmbSetir(sh, r++, "2.2 Avtomobil alınmasına", AmbAl(map, "2_2"));
        AmbSetir(sh, r++, "2.3 Məişət avadanlıqlarının alınmasına", AmbAl(map, "2_3"));
        AmbSetir(sh, r++, "2.4 Kredit kartları", AmbAl(map, "2_4"));
        AmbSetir(sh, r++, "2.5 Digər", AmbAl(map, "2_5"));
        AmbSetir(sh, r++, "3. Daşınmaz əmlak kreditləri", AmbAl(map, "3"));
        AmbSetir(sh, r++, "4. Digər kreditlər", AmbAl(map, "4"));
        return r + 2;
    }

    private static AmbHuceyre AmbAl(Dictionary<string, AmbHuceyre> m, string k)
        => m.TryGetValue(k, out var h) ? h : new AmbHuceyre();

    private static AmbHuceyre AmbCem(Dictionary<string, AmbHuceyre> m, string[] keys)
    {
        var t = new AmbHuceyre();
        foreach (var k in keys)
            if (m.TryGetValue(k, out var h))
            { t.G1 += h.G1; t.G2 += h.G2; t.G3 += h.G3; t.E1 += h.E1; t.E2 += h.E2; t.E3 += h.E3; }
        return t;
    }

    private static void AmbSetir(ISheet sh, int r, string ad, AmbHuceyre h)
    {
        static double K(decimal v) => (double)Math.Round(v / 1000m, 1);   // AZN → min manat
        var row = sh.CreateRow(r);
        row.CreateCell(1).SetCellValue(ad);
        row.CreateCell(3).SetCellValue(K(h.GCem));
        row.CreateCell(4).SetCellValue(K(h.G1));
        row.CreateCell(5).SetCellValue(K(h.G2));
        row.CreateCell(6).SetCellValue(K(h.G3));
        row.CreateCell(7).SetCellValue(0);          // POCI — bankda yoxdur
        row.CreateCell(8).SetCellValue(K(h.ECem));
        row.CreateCell(9).SetCellValue(K(h.E1));
        row.CreateCell(10).SetCellValue(K(h.E2));
        row.CreateCell(11).SetCellValue(K(h.E3));
        row.CreateCell(12).SetCellValue(0);         // POCI
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
