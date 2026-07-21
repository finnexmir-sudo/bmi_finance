using System.Globalization;
using System.IO;
using FinNex.Application.DTOs.Muhasibat;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Muhasibat;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

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
    private readonly IConfiguration _config;

    public HesabatController(
        IMuhasibatService service,
        IUserPermissionService perm,
        UserManager<AppUser> userManager,
        IConfiguration config)
    {
        _service = service;
        _perm = perm;
        _userManager = userManager;
        _config = config;
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

    // Yalnız tam maliyyə (Admin/Muhasib) floor parametrlərini dəyişə bilər — ehtiyata təsir edir.
    private bool TamMaliyye() => User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Muhasib);

    // IFRS 9 floor parametrləri (mənzil güzəşti + mərhələ floor-ları) — baxış.
    public async Task<IActionResult> Ifrs9Parametr()
    {
        if (!await IcazeVarAsync() || !TamMaliyye())
            return Forbid();

        var p = await _service.Ifrs9ParametrleriAsync();
        return View(p);
    }

    // IFRS 9 floor parametrlərini yadda saxla.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ifrs9Parametr(Ifrs9ParametrDto model)
    {
        if (!await IcazeVarAsync() || !TamMaliyye())
            return Forbid();

        model.MenzilFloor = Math.Max(0m, model.MenzilFloor);
        model.Stage1Floor = Math.Max(0m, model.Stage1Floor);
        model.Stage2Floor = Math.Max(0m, model.Stage2Floor);
        model.Metod = string.Equals(model.Metod, "MB", StringComparison.OrdinalIgnoreCase) ? "MB" : "Excel";
        await _service.Ifrs9ParametrYazAsync(model);
        TempData["Status"] = "IFRS 9 floor parametrləri yadda saxlanıldı. Nəticəni real data ilə yoxlayın.";
        return RedirectToAction(nameof(Ifrs9Parametr));
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

    // IFRS 9 iş kağızları (audit izi) — 4 vərəqli Excel: nəticəyə necə gəlindiyini göstərir.
    public async Task<IActionResult> Ifrs9IsKagizlari(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var a = await _service.Ifrs9IsKagizlariAsync(ParseTarix(t));
        var wb = new HSSFWorkbook();

        // 0 — Metod & parametrlər (audit izi: bu rəqəmlər hansı qaydaya görə çıxıb).
        // MB seçildikdə M sütunu Stage 3-də (F−G−H−J−K)×bərpa yox, F×(1−bərpa) olur —
        // auditorun M-i təkrar yoxlaya bilməsi üçün burada açıq yazılır.
        var s0 = wb.CreateSheet("0-Metod");
        int r0 = 0;
        void AddL(string ad, string deyer = "")
        {
            var row = s0.CreateRow(r0++);
            row.CreateCell(0).SetCellValue(ad);
            if (deyer.Length > 0) row.CreateCell(1).SetCellValue(deyer);
        }
        bool mbSec = a.Ecl.Metod == "MB";
        AddL("IFRS 9 ECL — is kagizlari (audit izi)", a.Tarix.ToString("dd.MM.yyyy"));
        AddL("");
        AddL("Aktiv hesablama metodu:", mbSec
            ? "AMB Metodoloji Rehberliyi — 23.12.2025, Protokol 45/2"
            : "Excel (tesdiqlenmis) model");
        AddL("Merhele 1 & 2 ECL:", "EAD × risk% (tarixi kecid faizi + minimum floor)");
        AddL("Merhele 3 (problemli) ECL:", mbSec
            ? "EAD × LGD;  LGD = 1 - berpa  (defolt olub, PD=100%)"
            : "EAD × risk%;  risk% = (batiqda qalma) × berpa");
        AddL("  -> M sutunu (Merhele 3 hesabi):", mbSec
            ? "M = F × (1 - berpa).  DIQQET: G/H/J/K sutunlari Merhele 3 M-e daxil deyil."
            : "M = (F - G - H - J - K) × berpa");
        AddL("Menzil guzesti (1902/1904):", a.Ecl.MenzilGuzest
            ? "ACIQ — menzil oz (yuksek) berpasini Q2 alir"
            : "BAGLI — menzil adi kredit kimi P2 alir");
        AddL("Floor — Menzil / Merhele 1 / Merhele 2:",
            $"{a.Ecl.MenzilFloor}% / {a.Ecl.Stage1Floor}% / {a.Ecl.Stage2Floor}%  (Merhele 3-de floor tesirsizdir)");
        AddL("Berpa emsali P2 (diger sahe, Merhele 3):", $"{Math.Round(a.P2 * 100m, 2)}%");
        AddL("Berpa emsali Q2 (menzil 1902/1904):", $"{Math.Round(a.Q2 * 100m, 2)}%");
        AddL("");
        AddL("UMUMI ECL (cemi):", $"{Math.Round(a.Ecl.UmumiEcl, 2)} AZN  ({Math.Round(a.Ecl.EclFaiz, 2)}%)");
        if (mbSec)
            AddL("Istinad:", "AMB Rehberliyi «Ehtiyatlanma»: ECL=EAD×LGD_ID, LGL=1-Total recovery%, teminat cedveli.");
        s0.SetColumnWidth(0, 14000);
        s0.SetColumnWidth(1, 20000);

        // 1 — Tarixi keçid matrisi
        var s1 = wb.CreateSheet("1-Tarixi kecid");
        int r = 0;
        s1.CreateRow(r++).CreateCell(0).SetCellValue($"IFRS 9 — Tarixi keçid matrisi (son 5 il) · {a.Tarix:dd.MM.yyyy}");
        var h1 = s1.CreateRow(r++);
        string[] b1 = { "Il", "Sahe kodu", "Sahe adi", "Merhele (start)", "F baslangic qaliq", "G ->Merhele1", "H ->Merhele2", "I ->Merhele3", "J baglanan", "K odenilen", "M risk mebleg", "N risk %" };
        for (int i = 0; i < b1.Length; i++) h1.CreateCell(i).SetCellValue(b1[i]);
        foreach (var k in a.Kechidler)
        {
            var row = s1.CreateRow(r++);
            row.CreateCell(0).SetCellValue(k.Il);
            row.CreateCell(1).SetCellValue(k.SaheKodu);
            row.CreateCell(2).SetCellValue(k.SaheAdi);
            row.CreateCell(3).SetCellValue(k.Stage);
            row.CreateCell(4).SetCellValue((double)k.F);
            row.CreateCell(5).SetCellValue((double)k.G);
            row.CreateCell(6).SetCellValue((double)k.H);
            row.CreateCell(7).SetCellValue((double)k.I);
            row.CreateCell(8).SetCellValue((double)k.J);
            row.CreateCell(9).SetCellValue((double)k.K);
            row.CreateCell(10).SetCellValue((double)k.M);
            row.CreateCell(11).SetCellValue((double)Math.Round(k.N * 100m, 4));
        }

        // 2 — Risk faizləri (sahə×mərhələ üzrə orta N) + bərpa əmsalları
        var s2 = wb.CreateSheet("2-Risk faizleri");
        r = 0;
        s2.CreateRow(r++).CreateCell(0).SetCellValue("Risk faizi = tarixi risk (M) / tarixi qaliq (F), sahe+merhele uzre orta (N)");
        var pr = s2.CreateRow(r++);
        pr.CreateCell(0).SetCellValue("Berpa emsali P2 (diger)"); pr.CreateCell(1).SetCellValue((double)Math.Round(a.P2 * 100m, 4)); pr.CreateCell(2).SetCellValue("%");
        var qr = s2.CreateRow(r++);
        qr.CreateCell(0).SetCellValue("Berpa emsali Q2 (menzil 1902/1904)"); qr.CreateCell(1).SetCellValue((double)Math.Round(a.Q2 * 100m, 4)); qr.CreateCell(2).SetCellValue("%");
        r++;
        var h2 = s2.CreateRow(r++);
        string[] b2 = { "Sahe kodu", "Sahe adi", "Merhele", "Risk % (orta N)", "Setir sayi" };
        for (int i = 0; i < b2.Length; i++) h2.CreateCell(i).SetCellValue(b2[i]);
        var qruplu = a.Kechidler
            .GroupBy(x => new { x.SaheKodu, x.Stage })
            .Select(g => new { g.Key.SaheKodu, Ad = g.First().SaheAdi, g.Key.Stage, Risk = g.Average(x => x.N), Say = g.Count() })
            .OrderBy(x => x.SaheKodu).ThenBy(x => x.Stage);
        foreach (var g in qruplu)
        {
            var row = s2.CreateRow(r++);
            row.CreateCell(0).SetCellValue(g.SaheKodu);
            row.CreateCell(1).SetCellValue(g.Ad);
            row.CreateCell(2).SetCellValue(g.Stage);
            row.CreateCell(3).SetCellValue((double)Math.Round(g.Risk * 100m, 4));
            row.CreateCell(4).SetCellValue(g.Say);
        }

        // 3 — Cari portfel (kredit-kredit)
        var s3 = wb.CreateSheet("3-Cari portfel");
        r = 0;
        var h3 = s3.CreateRow(r++);
        string[] b3 = { "Hesab", "Tip", "Valyuta", "Sahe kodu", "Sahe adi", "Merhele", "Gecikme gunu", "EAD", "Risk %", "ECL" };
        for (int i = 0; i < b3.Length; i++) h3.CreateCell(i).SetCellValue(b3[i]);
        foreach (var x in a.Ecl.Setirler)
        {
            var row = s3.CreateRow(r++);
            row.CreateCell(0).SetCellValue(x.Hesab);
            row.CreateCell(1).SetCellValue(x.Tip);
            row.CreateCell(2).SetCellValue(x.Valyuta);
            row.CreateCell(3).SetCellValue(x.SaheKodu);
            row.CreateCell(4).SetCellValue(x.SaheAdi);
            row.CreateCell(5).SetCellValue(x.Stage);
            row.CreateCell(6).SetCellValue(x.Dpd);
            row.CreateCell(7).SetCellValue((double)x.Ead);
            row.CreateCell(8).SetCellValue((double)x.RiskFaiz);
            row.CreateCell(9).SetCellValue((double)x.Ecl);
        }

        // 4 — Nəticə (mərhələ cəmi)
        var s4 = wb.CreateSheet("4-Netice");
        r = 0;
        s4.CreateRow(r++).CreateCell(0).SetCellValue($"IFRS 9 ECL neticesi · {a.Tarix:dd.MM.yyyy}");
        var h4 = s4.CreateRow(r++);
        string[] b4 = { "Merhele", "Say", "EAD", "Risk %", "ECL (ehtiyat)", "FINA ehtiyat" };
        for (int i = 0; i < b4.Length; i++) h4.CreateCell(i).SetCellValue(b4[i]);
        foreach (var st in a.Ecl.Stagelar)
        {
            var row = s4.CreateRow(r++);
            row.CreateCell(0).SetCellValue(st.Stage);
            row.CreateCell(1).SetCellValue(st.Say);
            row.CreateCell(2).SetCellValue((double)st.Ead);
            row.CreateCell(3).SetCellValue((double)st.RiskFaiz);
            row.CreateCell(4).SetCellValue((double)st.Ecl);
            row.CreateCell(5).SetCellValue((double)st.BankEhtiyat);
        }
        var tr4 = s4.CreateRow(r++);
        tr4.CreateCell(0).SetCellValue("CEMI");
        tr4.CreateCell(1).SetCellValue(a.Ecl.Say);
        tr4.CreateCell(2).SetCellValue((double)a.Ecl.UmumiPortfel);
        tr4.CreateCell(3).SetCellValue((double)a.Ecl.EclFaiz);
        tr4.CreateCell(4).SetCellValue((double)a.Ecl.UmumiEcl);
        tr4.CreateCell(5).SetCellValue((double)a.Ecl.BankEhtiyat);

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return File(ms.ToArray(), "application/vnd.ms-excel", $"IFRS9_is_kagizlari_{a.Tarix:yyyyMMdd}.xls");
    }

    // AMB MHBS 9 — Cədvəl A1 (amortizasiya olunmuş dəyərdə kredit portfeli) Excel ixracı.
    // A alt-cədvəli (bütün valyuta) + B (xarici valyuta). C/D (FVOCI) — bankda yoxdur, 0.
    public async Task<IActionResult> AmbA1Excel(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var pt = ParseTarix(t);
        var m = await _service.AmbA1Async(pt);
        var rf = await _service.AmbA1_1Async(pt);

        // DMS-də rəsmi AMB şablonu varsa onu doldur (formatlaşdırma + formullar qorunur),
        // yoxdursa təzə cədvəl generasiya et (fallback).
        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var sablonPath = Path.Combine(dmsRoot, "hesabat-sablonlari", "muhasibat", "amb-mhbs9", "AMB_MHBS9.xlsx");
        if (System.IO.File.Exists(sablonPath))
        {
            var dolu = AmbA1SablonDoldur(sablonPath, m, rf);
            return File(dolu, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"AMB_MHBS9_{m.Tarix:yyyyMMdd}.xlsx");
        }

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

    // AMB Metodoloji Rəhbərliyi (23.12.2025, Protokol № 45/2) — rəsmi sənədi DMS-dən yüklə.
    // Sənəd: C:\FinNex_DMS\hesabat-sablonlari\muhasibat\amb-mhbs9\AMB_Metodoloji_Rehberlik_23122025.docx
    public async Task<IActionResult> AmbQaydaSened()
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var yol = Path.Combine(dmsRoot, "hesabat-sablonlari", "muhasibat", "amb-mhbs9",
                               "AMB_Metodoloji_Rehberlik_23122025.docx");
        if (!System.IO.File.Exists(yol))
            return NotFound($"Sənəd DMS-də tapılmadı. Bu yola kopyalayın: {yol}");

        var bytes = await System.IO.File.ReadAllBytesAsync(yol);
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "AMB_Metodoloji_Rehberlik_23122025.docx");
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
        static double K(decimal v) => (double)(v / 1000m);   // AZN → min manat
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

    // Rəsmi AMB şablonunu (xlsx) doldur: A1 vərəqində alt-sahə sətirlərinin yalnız
    // E-H (brüt Mərhələ 1/2/3/POCI) və J-M (ECL) xanaları. D/I və cəm/qrup sətirləri
    // şablonun öz =SUM() formulları ilə avtomatik hesablanır (əl vurulmur).
    private static byte[] AmbA1SablonDoldur(string sablonPath, MuhasibatAmbA1Dto m, MuhasibatAmbA1_1Dto rf)
    {
        IWorkbook wb;
        using (var fs = new FileStream(sablonPath, FileMode.Open, FileAccess.Read))
            wb = new XSSFWorkbook(fs);

        var ws = wb.GetSheet("A1");
        if (ws != null)
        {
            // (şablon sətri, 1-əsaslı; kateqoriya açarı). A blok; B (xarici valyuta) = +25.
            var leaf = new (int satir, string kat)[]
            {
                (11,"1_1"),(12,"1_2"),(13,"1_3"),(14,"1_4"),(15,"1_5"),(16,"1_6"),(17,"1_7"),
                (19,"2_1"),(20,"2_2"),(21,"2_3"),(22,"2_4"),(23,"2_5"),(24,"3"),(25,"4")
            };
            foreach (var (satir, kat) in leaf) AmbLeafYaz(ws, satir - 1,      AmbAl(m.Butun, kat));   // A
            foreach (var (satir, kat) in leaf) AmbLeafYaz(ws, satir - 1 + 25, AmbAl(m.Xarici, kat));  // B
        }

        // A1.2 — mərhələ × gecikmə günü. Alt-sahə (leaf) sətirləri: (şablon sətri, qrup, mərhələ).
        var ws12 = wb.GetSheet("A1.2");
        if (ws12 != null)
        {
            var dpdLeaf = new (int satir, string qrup, int stage)[]
            {
                (11,"biznes",1),(12,"biznes",2),(13,"biznes",3),
                (15,"istehlak",1),(16,"istehlak",2),(17,"istehlak",3),
                (19,"dasinmaz",1),(20,"dasinmaz",2),(21,"dasinmaz",3),
                (23,"diger",1),(24,"diger",2),(25,"diger",3),
            };
            foreach (var (satir, qrup, stage) in dpdLeaf) AmbDpdYaz(ws12, satir - 1,      AmbDpdAl(m.Dpd, qrup, stage));        // A
            foreach (var (satir, qrup, stage) in dpdLeaf) AmbDpdYaz(ws12, satir - 1 + 23, AmbDpdAl(m.DpdXarici, qrup, stage));  // B
        }

        // A1.1 — roll-forward (brüt) + A2 (ECL). Qrup bazaları (şablon sətirləri, 1-əsaslı).
        var ws11 = wb.GetSheet("A1.1");
        if (ws11 != null && rf.Ugurlu)
        {
            var qruplar = new (string qrup, int acilisRow, int eclAcRow, int eclBagRow)[]
            {
                ("biznes",   11, 57, 58),
                ("istehlak", 21, 60, 61),
                ("dasinmaz", 31, 63, 64),
                ("diger",    41, 66, 67),
            };
            foreach (var (qrup, aRow, ecA, ecB) in qruplar)
            {
                var g = rf.Qruplar.TryGetValue(qrup, out var x) ? x : new AmbRollForward();
                int b = aRow - 1;   // NPOI 0-əsaslı açılış sətri
                // Cari ilin əvvəlinə qalıq / Verilmiş / Ödənilmiş — E(M1) F(M2) G(M3)
                AmbCell(ws11, b,     4, g.A1); AmbCell(ws11, b,     5, g.A2); AmbCell(ws11, b,     6, g.A3);
                AmbCell(ws11, b + 1, 4, g.V1); AmbCell(ws11, b + 1, 5, g.V2); AmbCell(ws11, b + 1, 6, g.V3);
                AmbCell(ws11, b + 2, 4, g.O1); AmbCell(ws11, b + 2, 5, g.O2); AmbCell(ws11, b + 2, 6, g.O3);
                // Köçürmələr — mənbə mərhələ sütunu MƏNFİ, hədəf sütunu şablon formulu ilə (əl vurma).
                // Mərhələ 1-ə köçürmə (b+3): F=-T21, G=-T31 (E formul)
                AmbCell(ws11, b + 3, 5, -g.T21); AmbCell(ws11, b + 3, 6, -g.T31);
                // Mərhələ 2-ə köçürmə (b+4): E=-T12, G=-T32 (F formul)
                AmbCell(ws11, b + 4, 4, -g.T12); AmbCell(ws11, b + 4, 6, -g.T32);
                // Mərhələ 3-ə köçürmə (b+5): E=-T13, F=-T23 (G formul)
                AmbCell(ws11, b + 5, 4, -g.T13); AmbCell(ws11, b + 5, 5, -g.T23);
                // Qaytarılmış (b+6), Silinmiş (b+7) — 0, toxunulmur. Dövr sonu qalıq (b+8) — formul.

                // A2 — ECL: dövr əvvəli + dövr sonu (E/F/G = Mərhələ 1/2/3)
                var ea = rf.EclAcilis.TryGetValue(qrup, out var y) ? y : new AmbHuceyre();
                var eb = rf.EclBaglanis.TryGetValue(qrup, out var z) ? z : new AmbHuceyre();
                AmbCell(ws11, ecA - 1, 4, ea.E1); AmbCell(ws11, ecA - 1, 5, ea.E2); AmbCell(ws11, ecA - 1, 6, ea.E3);
                AmbCell(ws11, ecB - 1, 4, eb.E1); AmbCell(ws11, ecB - 1, 5, eb.E2); AmbCell(ws11, ecB - 1, 6, eb.E3);
            }
        }

        // Cəm/qrup =SUM() formullarını server tərəfdə hesabla ki, hansı proqram açsa da
        // düzgün rəqəm görünsün (bu NPOI versiyasında IWorkbook.SetForceFormulaRecalculation yoxdur).
        try { wb.GetCreationHelper().CreateFormulaEvaluator().EvaluateAll(); } catch { /* formul yoxdursa keç */ }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return ms.ToArray();
    }

    // Bir alt-sahə (leaf) sətrinin mərhələ xanalarını yaz (NPOI 0-əsaslı sətir indeksi).
    private static void AmbLeafYaz(ISheet ws, int rIdx, AmbHuceyre h)
    {
        static void Set(ISheet ws, int r, int c, decimal v)
        {
            var row = ws.GetRow(r) ?? ws.CreateRow(r);
            var cell = row.GetCell(c) ?? row.CreateCell(c);
            cell.SetCellValue((double)(v / 1000m));   // AZN → min manat
        }
        Set(ws, rIdx, 4, h.G1);   // E — brüt Mərhələ 1
        Set(ws, rIdx, 5, h.G2);   // F — Mərhələ 2
        Set(ws, rIdx, 6, h.G3);   // G — Mərhələ 3
        Set(ws, rIdx, 7, 0m);     // H — POCI
        Set(ws, rIdx, 9, h.E1);   // J — ECL Mərhələ 1
        Set(ws, rIdx, 10, h.E2);  // K — ECL Mərhələ 2
        Set(ws, rIdx, 11, h.E3);  // L — ECL Mərhələ 3
        Set(ws, rIdx, 12, 0m);    // M — ECL POCI
    }

    // Bir xanaya min-manat dəyəri yaz (NPOI 0-əsaslı r,c). Formul xanalarına ÇAĞIRILMIR.
    private static void AmbCell(ISheet ws, int r, int c, decimal v)
    {
        var row = ws.GetRow(r) ?? ws.CreateRow(r);
        var cell = row.GetCell(c) ?? row.CreateCell(c);
        cell.SetCellValue((double)(v / 1000m));
    }

    private static AmbDpdSetir AmbDpdAl(Dictionary<string, AmbDpdSetir> m, string qrup, int stage)
        => m.TryGetValue($"{qrup}|{stage}", out var d) ? d : new AmbDpdSetir();

    // A1.2 leaf sətrinin gecikmə xanaları: E(Cari) F(1-30) G(31-90) H(90+) — NPOI 0-əsaslı.
    private static void AmbDpdYaz(ISheet ws, int rIdx, AmbDpdSetir d)
    {
        static void Set(ISheet ws, int r, int c, decimal v)
        {
            var row = ws.GetRow(r) ?? ws.CreateRow(r);
            var cell = row.GetCell(c) ?? row.CreateCell(c);
            cell.SetCellValue((double)(v / 1000m));   // AZN → min manat
        }
        Set(ws, rIdx, 4, d.Cari);    // E — Cari (0 gün)
        Set(ws, rIdx, 5, d.D1_30);   // F — 1-30 gün
        Set(ws, rIdx, 6, d.D31_90);  // G — 31-90 gün
        Set(ws, rIdx, 7, d.D90);     // H — 90+ gün
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
