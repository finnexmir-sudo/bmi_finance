using System.Globalization;
using System.IO;
using FinNex.Application.DTOs.Dsmf;
using FinNex.Application.DTOs.Muhasibat;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Dsmf;
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
    private readonly IHesabatSecilmisService _secilmis;
    private readonly IDsmfHesabatService _dsmf;

    public HesabatController(
        IMuhasibatService service,
        IUserPermissionService perm,
        UserManager<AppUser> userManager,
        IConfiguration config,
        IHesabatSecilmisService secilmis,
        IDsmfHesabatService dsmf)
    {
        _service = service;
        _perm = perm;
        _userManager = userManager;
        _config = config;
        _secilmis = secilmis;
        _dsmf = dsmf;
    }

    // "Maliyyə Hesabatları" kataloqu — TƏK yerdə saxlanılır (29.10.2026, "Kassa
    // valyutaları"/"Qara Jeton" ilə eyni "statik siyahı" prinsipi). Yeni hesabat
    // əlavə etmək lazımdırsa, YALNIZ bura əlavə et.
    //
    // ⚠️ 29.10.2026 — DÜZƏLİŞ: canlı Balans İcmalı tab-larının (Balans İcmalı,
    // Mənfəət/Zərər, Depozit & Müştəri, Kredit Portfeli, Likvidlik) qısayol
    // kartları BURADAN ÇIXARILDI. İstifadəçi ekranda görəndə fikrini dəyişdi:
    // "icmal, kreditler olan hisseni tekrar niye gosterdin ki? burda yalniz
    // hesabatlarin hazirlanmasi olacq, o biriler ise sorgular idi, qatisdirma"
    // — yəni bu səhifə YALNIZ həqiqi HESABAT (sənəd/export hazırlanan) üçündür,
    // canlı KPI/sorğu dashboard-ları (Dashboard controller-in tabları) bura
    // qarışdırılmır. BMI-nin köhnə "Maliyyə DP → Hesabatlar → Günlük/Aylıq/
    // Rüblük" menyusu (əmsal nümunə kimi göstərildi) bu kataloqun gələcək
    // doldurulma istiqamətidir — hesabatlar oradan "yavaş-yavaş" bura köçürüləcək.
    //
    // ⚠️ 29.10.2026 — İKİNCİ DÜZƏLİŞ: "AMB MHBS 9" ayrıca kart olaraq SİLİNDİ.
    // Səbəb: o, "IFRS 9 — ECL" kartı İLƏ EYNİ səhifəyə (Action="Ifrs9") açılırdı
    // — AMB A1 Excel ixracı həqiqətən həmin səhifənin daxilindəki bir düymədir
    // (bax "AmbA1Excel" action-u, Ifrs9.cshtml-dəki "AMB MHBS 9 · A1" düyməsi),
    // ayrıca məzmunu/başlığı yoxdur. İstifadəçi iki kartın eyni nəticəyə
    // (eyni başlıqla: "IFRS 9 — Gözlənilən Kredit İtkiləri") açıldığını görüb
    // bunu qarışıqlıq saydı: "ikiside eyni seyi acir, ikiside eyni addadir axi,
    // gozlenilen kredit itkileri". Düzgün həll — iki kart yox, BİR kart: AMB
    // A1 ixracı "IFRS 9 — ECL" kartının öz daxilindəki düymə ilə əlçatandır,
    // bu, kataloqda təkrarlanmır. AMB export action-unun (`AmbA1Excel`) özü
    // SİLİNMƏDİ — yalnız kataloqdan (göstərişdən) çıxarıldı.
    private static readonly List<HesabatKartDto> Katalog = new()
    {
        new() { Kod = "ifrs9-ecl", Ad = "IFRS 9 — ECL", Tesvir = "Gözlənilən kredit zərərləri (roll-rate stage modeli) · AMB MHBS 9 A1 ixracı da buradadır",
                Kateqoriya = "Risk", Ikon = "bi-clipboard2-pulse", Area = "Muhasibat", Controller = "Hesabat", Action = "Ifrs9" },
        // DSMF Vahid Hesabat Forması — TƏK kataloq kartı (29.10.2026, istifadəçi
        // tələbi: "Vahid hesabat adini yaz, hazirladiqlarini onun icinde goster,
        // altisinida" — altı rəsmi əlavədən ikisi hazırdır). Əvvəl burada İKİ ayrı
        // kart var idi ("DSMF — Əlavə №1 Hissə 3" + "DSMF — Əlavə №4 Bölmə 3-4-5") —
        // istifadəçi bunları bir "Vahid Hesabat" girişi altında, daxilində 6-nın da
        // statusunu göstərən bir "hub" səhifəsi istədi. `DsmfVahid` action-u həmin
        // hub-dır, Dsmf3/Bolme2345-ə buradan keçid verilir — KATALOQDA artıq ikisi
        // AYRI görünmür (bax CLAUDE.md "DSMF — Vahid Hub" bölməsi).
        new() { Kod = "dsmf-vahid", Ad = "DSMF — Vahid Hesabat Forması", Tesvir = "Dövlət Sosial Müdafiə Fonduna rüblük hesabat — 6 rəsmi əlavə, hazır olanlar işarəli",
                Kateqoriya = "Rüblük", Ikon = "bi-bank2", Area = "Muhasibat", Controller = "Hesabat", Action = "DsmfVahid" },
        // ⚠️ "AMB MHBS 9" ayrıca kart kimi 29.10.2026-da BURADAN ÇIXARILDI —
        // yuxarıdaki şərhə bax. AMB A1 Excel ixracı yuxarıdakı "IFRS 9 — ECL"
        // kartının səhifəsindəki düymədən əlçatandır. Ayrıca kart kimi geri
        // qaytarmaq lazımdırsa, əvvəlcə ONA XAS, fərqli bir məzmun/səhifə
        // (Action) qurulmalıdır — əks halda eyni qarışıqlıq təkrarlanar.
        //
        // "Xərc Hesabatı" (HR/XercHesabat) 29.10.2026-da BURADAN ÇIXARILDI —
        // istifadəçi: "heleki islenilmir" (modul hələ real istifadədə deyil).
        // Yenidən lazım olsa, yalnız bura geri əlavə et (Kod="xerc-hesabati",
        // Kateqoriya="Xərclər", Area="HR", Controller="XercHesabat", Action="Index").
    };

    private async Task<bool> IcazeVarAsync()
    {
        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Muhasib) || User.IsInRole(RoleNames.Rehber))
            return true;

        var u = await _userManager.GetUserAsync(User);
        if (u == null) return false;

        var res = await _perm.HasPermissionAsync(u.Id, DashboardController.IcazeKod);
        return res.Success && res.Data == true;
    }

    // Hesabatlar açılış səhifəsi — mövcud hesabatların siyahısı + istifadəçinin ★ seçimi.
    public async Task<IActionResult> Index()
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var u = await _userManager.GetUserAsync(User);
        var secili = u != null ? await _secilmis.SeciliKodlarAsync(u.Id) : new HashSet<string>();

        var model = Katalog.Select(k => new HesabatKartDto
        {
            Kod = k.Kod,
            Ad = k.Ad,
            Tesvir = k.Tesvir,
            Kateqoriya = k.Kateqoriya,
            Ikon = k.Ikon,
            Area = k.Area,
            Controller = k.Controller,
            Action = k.Action,
            Secilib = secili.Contains(k.Kod)
        }).ToList();

        return View(model);
    }

    // ★ Seçilmişlər — toggle. Kataloqda olmayan kod göndərilsə belə zərərsizdir
    // (sadəcə heç bir kartda görünməyən boş bir sətir qalar) — əlavə yoxlama lazım deyil.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeciliDeyis(string kod)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var u = await _userManager.GetUserAsync(User);
        if (u == null) return Json(new { ok = false });

        var secilib = await _secilmis.SecVeyaCixarAsync(u.Id, kod);
        return Json(new { ok = true, secilib });
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

    // DSMF Vahid Hesabat Forması — hub səhifəsi. 6 rəsmi əlavənin hamısını adı ilə
    // sadalayır. 29.10.2026-da Hissə 1 əlavə olunandan sonra HAMISI "Hazırdır" —
    // bax CLAUDE.md "DSMF — Vahid Hub" və "Əlavə №1 Hissə 1".
    public async Task<IActionResult> DsmfVahid()
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var komponentler = new List<DsmfVahidKomponentDto>
        {
            new() { ResmiAd = "Əlavə №1 — Hissə 1", Tesvir = "İşçilər (sığortaolunanlar) üzrə ümumi məlumat — muzdlu işə aid fəaliyyətlər üzrə (maaş tərkibi)",
                     Hazirdir = true, Action = "Dsmf1" },
            new() { ResmiAd = "Əlavə №1 — Hissə 3", Tesvir = "İşçinin işləmədiyi iş günləri haqqında məlumat",
                     Hazirdir = true, Action = "Dsmf3" },
            // 29.10.2026, istifadəçi: "hansını AÇDISA O EXCELƏ AİD OLANI GÖSTƏR" —
            // hər "Aç" indi `bolmeNo` route dəyəri ilə YALNIZ öz bölməsini açır
            // (əvvəl hamısı eyni, dördünü BİRLİKDƏ göstərən səhifəyə gedirdi).
            new() { ResmiAd = "Əlavə №4 — Bölmə 2", Tesvir = "Gəlir vergisi üzrə güzəşt və azadolmalar barədə məlumat",
                     Hazirdir = true, Action = "Bolme2345", BolmeNo = 2 },
            new() { ResmiAd = "Əlavə №4 — Bölmə 3", Tesvir = "Məcburi dövlət sosial sığorta haqqına cəlb olunmayan gəlirlər barədə məlumat",
                     Hazirdir = true, Action = "Bolme2345", BolmeNo = 3 },
            new() { ResmiAd = "Əlavə №4 — Bölmə 4", Tesvir = "İşsizlikdən sığorta haqqına cəlb olunmayan gəlirlər barədə məlumat",
                     Hazirdir = true, Action = "Bolme2345", BolmeNo = 4 },
            new() { ResmiAd = "Əlavə №4 — Bölmə 5", Tesvir = "İcbari tibbi sığorta haqqına cəlb olunmayan gəlirlər barədə məlumat",
                     Hazirdir = true, Action = "Bolme2345", BolmeNo = 5 },
        };
        return View(komponentler);
    }

    // ── DSMF Vahid Hesabat Forması — Əlavə №1 Hissə 1 (29.10.2026) ──
    // İşçilər üzrə ümumi məlumat (maaş tərkibi). İl + Rüb seçimi, Hissə 3 ilə
    // EYNİ naxış (önizləmə + ayrıca Excel ixracı).
    public async Task<IActionResult> Dsmf1(int? il, int? rub)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var bugun = DateTime.Today;
        var cariRub = (bugun.Month - 1) / 3 + 1;
        var m = await _dsmf.Hissesi1HazirlaAsync(il ?? bugun.Year, rub ?? cariRub);
        return View(m);
    }

    public async Task<IActionResult> Dsmf1Excel(int il, int rub)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var m = await _dsmf.Hissesi1HazirlaAsync(il, rub);

        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var sablonPath = Path.Combine(dmsRoot, "hesabat-sablonlari", "muhasibat", "dsmf",
                                       "UNIFIED_APPENDIX_1_EMPLOYEE_LIST.xlsx");
        var adı = $"DSMF_Elave1_Hisse1_{il}_R{rub}.xlsx";

        if (System.IO.File.Exists(sablonPath))
        {
            var dolu = Dsmf1SablonDoldur(sablonPath, m);
            return File(dolu, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", adı);
        }

        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Əlavə №1 - Hissə 1");
        int r = 0;
        sh.CreateRow(r++).CreateCell(0).SetCellValue(
            $"Əlavə №1 - Hissə 1. İşçilər (sığortaolunanlar) üzrə ümumi məlumat — {il}, Rüb {rub}");
        r++;
        var h = sh.CreateRow(r++);
        var basliq = new List<string> { "Sıra №", "Soyadı, adı, atasının adı", "FİN" };
        foreach (var ayAdi in new[] { m.Ay1Adi, m.Ay2Adi, m.Ay3Adi })
        {
            basliq.Add($"{ayAdi} — faktiki iş günü");
            basliq.Add($"{ayAdi} — hesablanmış məbləğ");
            basliq.Add($"{ayAdi} — azad güzəşt cəmi");
            basliq.Add($"{ayAdi} — DSMF azad cəmi");
            basliq.Add($"{ayAdi} — işsizlik azad cəmi");
            basliq.Add($"{ayAdi} — İTSS azad cəmi");
        }
        for (int i = 0; i < basliq.Count; i++) h.CreateCell(i).SetCellValue(basliq[i]);

        foreach (var s in m.Setirler)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(s.SiraNo);
            row.CreateCell(1).SetCellValue(s.AdSoyadAta);
            row.CreateCell(2).SetCellValue(s.Fin);
            int c = 3;
            foreach (var ay in new[] { s.Ay1, s.Ay2, s.Ay3 })
            {
                row.CreateCell(c++).SetCellValue(ay.FaktikiIsGunu);
                row.CreateCell(c++).SetCellValue((double)ay.HesablananMebleg);
                row.CreateCell(c++).SetCellValue((double)ay.AzadGuzestCemi);
                row.CreateCell(c++).SetCellValue((double)ay.DsmfAzadCemi);
                row.CreateCell(c++).SetCellValue((double)ay.IssizlikAzadCemi);
                row.CreateCell(c++).SetCellValue((double)ay.ItssAzadCemi);
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return File(ms.ToArray(), "application/vnd.ms-excel", $"DSMF_Elave1_Hisse1_{il}_R{rub}.xls");
    }

    // Rəsmi şablonu (UNIFIED_APPENDIX_1_EMPLOYEE_LIST.xlsx) doldur. ⚠️ Vərəqin
    // öz adı "Əlavə1_Hissə1_Muzdlu işə"dir (hüceyrə A1-dəki mətn "Hissə 1" deyir,
    // vərəq adı da bu dəfə mətnlə UYĞUNDUR — Bölmə 2-nin "Hissə 1" adlı tab-ı
    // ilə QARIŞDIRMA, o AYRI bir fayldır). Data 10-cu sətirdən (0-əsaslı 9)
    // başlayır. Şablon artıq 27 REAL işçinin (mühasibin özünün doldurduğu)
    // Sıra№+Əsas iş yeri sətirlərini daşıyır — bunlar bizim real nəticəmizlə
    // ÜST-ÜSTƏ DÜŞMƏYƏ BİLƏR (sayı fərqli ola bilər), ona görə Bölmə 2345-in
    // "sızma qoruyucusu" naxışı (yazılan sətirdən SONRA, hələ Sıra№-si dolu
    // qalan HƏR sətri təmizlə) tətbiq olunur — sabit "numuneSetirSayi" YOX.
    private static byte[] Dsmf1SablonDoldur(string sablonPath, DsmfHissesi1Dto m)
    {
        IWorkbook wb;
        using (var fs = new FileStream(sablonPath, FileMode.Open, FileAccess.Read))
            wb = new XSSFWorkbook(fs);

        var ws = wb.GetSheet("Əlavə1_Hissə1_Muzdlu işə");
        if (ws != null)
        {
            const int basSetir = 9; // NPOI 0-əsaslı — şablonun 10-cu sətri
            // Ay bloklarının başlanğıc sütunu (0-əsaslı): D=3 (1-ci ay), O=14
            // (2-ci ay), Z=25 (3-cü ay) — hər blokda 11 sütun (2.1..6.1).
            var ayBazlari = new[] { 3, 14, 25 };

            for (int i = 0; i < m.Setirler.Count; i++)
            {
                var s = m.Setirler[i];
                var row = ws.GetRow(basSetir + i) ?? ws.CreateRow(basSetir + i);
                row.CreateCell(0).SetCellValue(s.SiraNo);
                row.CreateCell(1).SetCellValue(s.AdSoyadAta);
                row.CreateCell(2).SetCellValue(s.Fin);

                var aylar = new[] { s.Ay1, s.Ay2, s.Ay3 };
                for (int a = 0; a < 3; a++)
                {
                    var ay = aylar[a];
                    var baz = ayBazlari[a];
                    row.CreateCell(baz).SetCellValue(ay.EsasIsYeri ? "Bəli" : "Xeyr");  // 2.1
                    row.CreateCell(baz + 1).SetCellValue(ay.FaktikiIsGunu);              // 2.2
                    row.CreateCell(baz + 2).SetCellValue((double)ay.HesablananMebleg);   // 2.3
                    row.CreateCell(baz + 3).SetBlank();                                  // 2.3.1 — həmişə boş
                    row.CreateCell(baz + 4).SetBlank();                                  // 2.5 — həmişə boş
                    row.CreateCell(baz + 5).SetBlank();                                  // 2.6 — N/A (bank)
                    row.CreateCell(baz + 6).SetCellValue((double)ay.AzadGuzestCemi);      // 3.1
                    row.CreateCell(baz + 7).SetCellValue((double)ay.DsmfAzadCemi);        // 4.1
                    row.CreateCell(baz + 8).SetBlank();                                  // 4.5 — həmişə boş
                    row.CreateCell(baz + 9).SetCellValue((double)ay.IssizlikAzadCemi);    // 5.1
                    row.CreateCell(baz + 10).SetCellValue((double)ay.ItssAzadCemi);       // 6.1
                }
            }

            int sonrakiSetir = basSetir + m.Setirler.Count;
            while (true)
            {
                var row = ws.GetRow(sonrakiSetir);
                var siraXana = row?.GetCell(0);
                if (row == null || siraXana == null || string.IsNullOrWhiteSpace(siraXana.ToString()))
                    break;
                for (int c = 0; c <= 36; c++)
                    row.GetCell(c)?.SetBlank();
                sonrakiSetir++;
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return ms.ToArray();
    }

    // ── DSMF Vahid Hesabat Forması — Əlavə №1 Hissə 3 (29.10.2026, pilot) ──
    // İl + Rüb seçimi, rüb = 3 ay. Önizləmə ekranı — Excel ixracı ayrıca action-dadır.
    public async Task<IActionResult> Dsmf3(int? il, int? rub)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var bugun = DateTime.Today;
        var cariRub = (bugun.Month - 1) / 3 + 1;
        var m = await _dsmf.Hissesi3HazirlaAsync(il ?? bugun.Year, rub ?? cariRub);
        return View(m);
    }

    // Hissə 3-ü Excel-ə ixrac et — DMS-dəki rəsmi şablonu doldurur (varsa),
    // yoxdursa sadə, formatlanmamış cədvəl yaradır (fallback, AmbA1Excel ilə eyni naxış).
    public async Task<IActionResult> Dsmf3Excel(int il, int rub)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var m = await _dsmf.Hissesi3HazirlaAsync(il, rub);

        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var sablonPath = Path.Combine(dmsRoot, "hesabat-sablonlari", "muhasibat", "dsmf",
                                       "UNIFIED_APPENDIX_1_UNEMPLOYED_DAYS.xlsx");
        var adı = $"DSMF_Elave1_Hisse3_{il}_R{rub}.xlsx";

        if (System.IO.File.Exists(sablonPath))
        {
            var dolu = Dsmf3SablonDoldur(sablonPath, m);
            return File(dolu, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", adı);
        }

        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Əlavə №1 - Hissə 3");
        int r = 0;
        sh.CreateRow(r++).CreateCell(0).SetCellValue(
            $"Əlavə №1 - Hissə 3. İşçinin işləmədiyi iş günləri haqqında məlumat — {il}, Rüb {rub}");
        r++;
        var h = sh.CreateRow(r++);
        string[] basliq = { "Sıra №", "Soyadı, adı, atasının adı", "FİN", m.Ay1Adi, m.Ay2Adi, m.Ay3Adi, "Səbəb" };
        for (int i = 0; i < basliq.Length; i++) h.CreateCell(i).SetCellValue(basliq[i]);
        foreach (var s in m.Setirler)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(s.SiraNo);
            row.CreateCell(1).SetCellValue(s.AdSoyadAta);
            row.CreateCell(2).SetCellValue(s.Fin);
            row.CreateCell(3).SetCellValue(s.Ay1Gun);
            row.CreateCell(4).SetCellValue(s.Ay2Gun);
            row.CreateCell(5).SetCellValue(s.Ay3Gun);
            row.CreateCell(6).SetCellValue(s.SebebMetni);
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return File(ms.ToArray(), "application/vnd.ms-excel", $"DSMF_Elave1_Hisse3_{il}_R{rub}.xls");
    }

    // Rəsmi DSMF şablonunu (UNIFIED_APPENDIX_1_UNEMPLOYED_DAYS.xlsx) doldur.
    // Vərəqin öz sətirləri 7-dən başlayır (1-6 başlıq/sub-nömrələmə) — şablonun
    // özü artıq 986 sətirlik yer ayırıb, formatlaşdırma/data validation-a toxunulmur.
    private static byte[] Dsmf3SablonDoldur(string sablonPath, DsmfHissesi3Dto m)
    {
        IWorkbook wb;
        using (var fs = new FileStream(sablonPath, FileMode.Open, FileAccess.Read))
            wb = new XSSFWorkbook(fs);

        var ws = wb.GetSheet("Əlavə №1 - Hissə 3");
        if (ws != null)
        {
            const int basSetir = 6; // NPOI 0-əsaslı — şablonun 7-ci sətri
            // Şablonun özü 7-8-ci sətirlərdə (0-əsaslı 6-7) nümunə "sıra №=1/2,
            // səbəb=1-İşçinin ödənişli əsas məzuniyyətdən..." mətni ilə doldurulmuş
            // GƏLİR — bu, real data DEYİL, yalnız nümunədir. Real sətir sayı 0 və ya
            // 1-dirsə, bu iki nümunə sətirdən örtülməyəni AÇIQ TƏMİZLƏMƏK lazımdır,
            // yoxsa yarımçıq/yalançı bir sətir (ad/FİN boş, səbəb mətni dolu) ixrac
            // olunan Excel-ə düşər.
            const int numuneSetirSayi = 2;
            for (int i = 0; i < m.Setirler.Count; i++)
            {
                var s = m.Setirler[i];
                var row = ws.GetRow(basSetir + i) ?? ws.CreateRow(basSetir + i);
                row.CreateCell(0).SetCellValue(s.SiraNo);
                row.CreateCell(1).SetCellValue(s.AdSoyadAta);
                row.CreateCell(2).SetCellValue(s.Fin);
                row.CreateCell(3).SetCellValue(s.Ay1Gun);
                row.CreateCell(4).SetCellValue(s.Ay2Gun);
                row.CreateCell(5).SetCellValue(s.Ay3Gun);
                row.CreateCell(6).SetCellValue(s.SebebMetni);
            }
            for (int i = m.Setirler.Count; i < numuneSetirSayi; i++)
            {
                var row = ws.GetRow(basSetir + i);
                if (row == null) continue;
                for (int c = 0; c <= 6; c++)
                    row.GetCell(c)?.SetBlank();
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return ms.ToArray();
    }

    // ── DSMF Vahid Hesabat Forması — Əlavə №4 Bölmə 2/3/4/5 (29.10.2026) ──
    // Dördü EYNİ struktur (qanun maddəsi-sətri) işlətdiyi üçün TƏK controller
    // action/view paylaşılır, AMMA 29.10.2026-da istifadəçi aydın tələb etdi:
    // "hansını AÇDISA O EXCELƏ AİD OLANI GÖSTƏR" — yəni hub-dan "Bölmə 2" açanda
    // YALNIZ Bölmə 2, "Bölmə 3" açanda YALNIZ Bölmə 3 görünməlidir, dördü BİRLİKDƏ
    // YOX. `bolmeNo` route dəyəri bunu həll edir: veriləndə (2/3/4/5) modelə TƏK
    // elementli siyahı qoyulur; verilməyəndə (əl ilə URL-ə girilsə) köhnə "hamısı
    // birlikdə" davranışı qalır — geriyə uyğunluq üçün.
    public async Task<IActionResult> Bolme2345(int? bolmeNo, int? il, int? rub)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var bugun = DateTime.Today;
        var cariRub = (bugun.Month - 1) / 3 + 1;
        var yil = il ?? bugun.Year;
        var r = rub ?? cariRub;

        var model = bolmeNo switch
        {
            2 => new List<DsmfIstisnaBolmesiDto> { await _dsmf.Bolme2HazirlaAsync(yil, r) },
            3 => new List<DsmfIstisnaBolmesiDto> { await _dsmf.Bolme3HazirlaAsync(yil, r) },
            4 => new List<DsmfIstisnaBolmesiDto> { await _dsmf.Bolme4HazirlaAsync(yil, r) },
            5 => new List<DsmfIstisnaBolmesiDto> { await _dsmf.Bolme5HazirlaAsync(yil, r) },
            _ => new List<DsmfIstisnaBolmesiDto>
            {
                await _dsmf.Bolme2HazirlaAsync(yil, r),
                await _dsmf.Bolme3HazirlaAsync(yil, r),
                await _dsmf.Bolme4HazirlaAsync(yil, r),
                await _dsmf.Bolme5HazirlaAsync(yil, r),
            },
        };
        return View(model);
    }

    // ⚠️ Rəsmi şablonun (UNIFIED_APPENDIX_4_DISCOUNTS.xlsx) VƏRƏQ TAB ADI
    // "Əlavə №4 - Hissə 1"-dir (hüceyrə A1-dəki başlıq mətni "Bölmə 2" desə
    // də) — dövlət şablonunun özündə qalan köhnə/səhv ad, DÜZƏLDİLMƏSİN.
    // `BolmeSablonDoldur` `wb.GetSheet(vereqAdi)` ilə tab ADINA görə axtarır —
    // "Bölmə 2" yazsaq heç nə tapılmaz, ws==null, şablon SƏSSİZCƏ boş qalar.
    public async Task<IActionResult> Bolme2Excel(int il, int rub)
    {
        if (!await IcazeVarAsync())
            return Forbid();
        var m = await _dsmf.Bolme2HazirlaAsync(il, rub);
        return BolmeExcelQaytar(m, "UNIFIED_APPENDIX_4_DISCOUNTS.xlsx", "Əlavə №4 - Hissə 1", "Bolme2");
    }

    public async Task<IActionResult> Bolme3Excel(int il, int rub)
    {
        if (!await IcazeVarAsync())
            return Forbid();
        var m = await _dsmf.Bolme3HazirlaAsync(il, rub);
        return BolmeExcelQaytar(m, "UNIFIED_APPENDIX_4_NON_INVOLVED_MDSS.xlsx", "Əlavə №4 - Bölmə 3", "Bolme3");
    }

    public async Task<IActionResult> Bolme4Excel(int il, int rub)
    {
        if (!await IcazeVarAsync())
            return Forbid();
        var m = await _dsmf.Bolme4HazirlaAsync(il, rub);
        return BolmeExcelQaytar(m, "UNIFIED_APPENDIX_4_NON_INVOLVED_UNEMPLOYED.xlsx", "Əlavə №4 - Bölmə 4", "Bolme4");
    }

    public async Task<IActionResult> Bolme5Excel(int il, int rub)
    {
        if (!await IcazeVarAsync())
            return Forbid();
        var m = await _dsmf.Bolme5HazirlaAsync(il, rub);
        return BolmeExcelQaytar(m, "UNIFIED_APPENDIX_4_NON_INVOLVED_ITS.xlsx", "Əlavə №4 - Bölmə 5", "Bolme5");
    }

    private IActionResult BolmeExcelQaytar(DsmfIstisnaBolmesiDto m, string sablonAdi, string vereqAdi, string ixracKodu)
    {
        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var sablonPath = Path.Combine(dmsRoot, "hesabat-sablonlari", "muhasibat", "dsmf", sablonAdi);
        var adı = $"DSMF_{ixracKodu}_{m.Il}_R{m.Rub}.xlsx";

        if (System.IO.File.Exists(sablonPath))
        {
            var dolu = BolmeSablonDoldur(sablonPath, vereqAdi, m);
            return File(dolu, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", adı);
        }

        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet(vereqAdi);
        int r = 0;
        sh.CreateRow(r++).CreateCell(0).SetCellValue($"{m.Basliq} — {m.Il}, Rüb {m.Rub}");
        r++;
        var h = sh.CreateRow(r++);
        string[] basliq = { "Sıra №", "Göstəricilər", $"{m.Ay1Adi} — işçi sayı", $"{m.Ay1Adi} — məbləğ",
                             $"{m.Ay2Adi} — işçi sayı", $"{m.Ay2Adi} — məbləğ", $"{m.Ay3Adi} — işçi sayı", $"{m.Ay3Adi} — məbləğ" };
        for (int i = 0; i < basliq.Length; i++) h.CreateCell(i).SetCellValue(basliq[i]);
        foreach (var s in m.Setirler)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(s.SiraNo);
            row.CreateCell(1).SetCellValue(s.Gostərici);
            row.CreateCell(2).SetCellValue(s.Ay1IsciSayi);
            row.CreateCell(3).SetCellValue((double)s.Ay1Mebleg);
            row.CreateCell(4).SetCellValue(s.Ay2IsciSayi);
            row.CreateCell(5).SetCellValue((double)s.Ay2Mebleg);
            row.CreateCell(6).SetCellValue(s.Ay3IsciSayi);
            row.CreateCell(7).SetCellValue((double)s.Ay3Mebleg);
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return File(ms.ToArray(), "application/vnd.ms-excel", $"DSMF_{ixracKodu}_{m.Il}_R{m.Rub}.xls");
    }

    // Rəsmi Bölmə 3/4/5 şablonlarını doldur — üçü AYNI sütun düzümünü (A=Sıra№,
    // B=Göstəricilər, C/D=1-ci ay (sayı/məbləğ), E/F=2-ci ay, G/H=3-cü ay) və AYNI
    // başlanğıc sətrini (7-ci, 0-əsaslı 6) paylaşır — bir helper kifayətdir.
    //
    // ⚠️ Şablonların özü artıq YALNIZ sistemin hesabladığı maddələri (mühasibin əl
    // ilə əlavə etdiyi nümunə sətirlər — 5.2.9/5.2.12/6.3/7.2) daşıyır, sayı bizim
    // Setirler.Count ilə TAM uyğundur — amma gələcəkdə şablon dəyişsə/daha çox
    // nümunə sətir əlavə olunsa deyə, yazdığımız sətirdən SONRA gələn, hələ
    // mətni olan (Göstəricilər boş olmayan) HƏR sətri açıq təmizləyirik (Hissə
    // 3-dəki "numuneSetirSayi" qoruyucusunun ÜMUMİLƏŞDİRİLMİŞ forması).
    private static byte[] BolmeSablonDoldur(string sablonPath, string vereqAdi, DsmfIstisnaBolmesiDto m)
    {
        IWorkbook wb;
        using (var fs = new FileStream(sablonPath, FileMode.Open, FileAccess.Read))
            wb = new XSSFWorkbook(fs);

        var ws = wb.GetSheet(vereqAdi);
        if (ws != null)
        {
            const int basSetir = 6; // NPOI 0-əsaslı — şablonun 7-ci sətri
            for (int i = 0; i < m.Setirler.Count; i++)
            {
                var s = m.Setirler[i];
                var row = ws.GetRow(basSetir + i) ?? ws.CreateRow(basSetir + i);
                row.CreateCell(0).SetCellValue(s.SiraNo);
                row.CreateCell(1).SetCellValue(s.Gostərici);
                row.CreateCell(2).SetCellValue(s.Ay1IsciSayi);
                row.CreateCell(3).SetCellValue((double)s.Ay1Mebleg);
                row.CreateCell(4).SetCellValue(s.Ay2IsciSayi);
                row.CreateCell(5).SetCellValue((double)s.Ay2Mebleg);
                row.CreateCell(6).SetCellValue(s.Ay3IsciSayi);
                row.CreateCell(7).SetCellValue((double)s.Ay3Mebleg);
            }

            int sonrakiSetir = basSetir + m.Setirler.Count;
            while (true)
            {
                var row = ws.GetRow(sonrakiSetir);
                var govdeXana = row?.GetCell(1);
                if (row == null || govdeXana == null || string.IsNullOrWhiteSpace(govdeXana.ToString()))
                    break;
                for (int c = 0; c <= 7; c++)
                    row.GetCell(c)?.SetBlank();
                sonrakiSetir++;
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return ms.ToArray();
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
