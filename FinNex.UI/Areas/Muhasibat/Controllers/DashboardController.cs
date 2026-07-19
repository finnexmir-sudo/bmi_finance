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
        // Admin/Muhasib — tam giriş; Rehber — panel (icmal/risk) görüntüsü;
        // qıraqdan icazə (muhasibat_dashboard_bax) — yalnız bu panel.
        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Muhasib) || User.IsInRole(RoleNames.Rehber))
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

    // Kredit portfeli (tarix üzrə — arh_licschkre).
    public async Task<IActionResult> Kredit(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.KreditPortfelAsync(ParseTarix(t));
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

    // Rezident / qeyri-rezident.
    public async Task<IActionResult> Rezident(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.RezidentAsync(ParseTarix(t));
        return View(model);
    }

    // Günlük İcmal (executive) — bütün bölmələrin əsas göstəriciləri.
    public async Task<IActionResult> Icmal(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.GunlukIcmalAsync(ParseTarix(t));
        return View(model);
    }

    // Kredit Pul Axını (Maturity Ladder).
    public async Task<IActionResult> Maturity(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.MaturityAsync(ParseTarix(t));
        return View(model);
    }

    // Kredit Keyfiyyəti & Ehtiyat.
    public async Task<IActionResult> Keyfiyyet(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.KreditKeyfiyyetAsync(ParseTarix(t));
        return View(model);
    }

    // Yerləşdirilmiş vəsaitlər (bank yerləşdirmələri — arh_licsch_rs).
    public async Task<IActionResult> Yerlesdirme(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.YerlesdirmeAsync(ParseTarix(t));
        return View(model);
    }

    // Mənfəət / Zərər (P&L) — tarix aralığı: bt=başlanğıc, st=son (default YTD).
    public async Task<IActionResult> Menfeet(string? bt, string? st)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var model = await _service.MenfeetAsync(ParseTarix(bt), ParseTarix(st));
        return View(model);
    }

    // Drill-down — kart/sətrin arxasındakı hesab detalı (JSON).
    // sahe: balans / balans-valyuta / balans-menfeet / likvidlik / depozit / kredit / valyuta / rezident.
    [HttpGet]
    public async Task<IActionResult> Detal(string sahe, string madde, string? t, string? bt, string? st)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var dto = await _service.DetalAsync(sahe ?? "", madde ?? "",
            ParseTarix(t), ParseTarix(bt), ParseTarix(st));
        return Json(dto);
    }

    // Drill-down detalını Excel-ə çıxar (uzun siyahılar üçün).
    public async Task<IActionResult> DetalExcel(string sahe, string madde, string? t, string? bt, string? st)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        var d = await _service.DetalAsync(sahe ?? "", madde ?? "",
            ParseTarix(t), ParseTarix(bt), ParseTarix(st));

        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Detal");
        int r = 0;
        Setir(sh, r++, string.IsNullOrWhiteSpace(d.Baslik) ? "Detal" : d.Baslik);
        r++;
        var h = sh.CreateRow(r++);
        h.CreateCell(0).SetCellValue("Hesab / kod");
        h.CreateCell(1).SetCellValue("Ad");
        h.CreateCell(2).SetCellValue("Valyuta");
        h.CreateCell(3).SetCellValue("Öz valyutası");
        h.CreateCell(4).SetCellValue("Manat qarşılığı (AZN)");
        h.CreateCell(5).SetCellValue(string.IsNullOrWhiteSpace(d.ElaveBaslik) ? "Əlavə" : d.ElaveBaslik);
        foreach (var x in d.Setirler)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(x.Kod);
            row.CreateCell(1).SetCellValue(x.Ad);
            row.CreateCell(2).SetCellValue(x.Valyuta ?? "");
            if (x.MeblegInval.HasValue) row.CreateCell(3).SetCellValue((double)x.MeblegInval.Value);
            else row.CreateCell(3).SetCellValue("");
            row.CreateCell(4).SetCellValue((double)x.Mebleg);
            if (d.ElaveReqem)
            {
                if (x.ElaveMebleg.HasValue) row.CreateCell(5).SetCellValue((double)x.ElaveMebleg.Value);
                else row.CreateCell(5).SetCellValue("");
            }
            else row.CreateCell(5).SetCellValue(x.Elave ?? "");
        }
        r++;
        KV(sh, r++, "CƏMI", d.Cem);
        KV(sh, r++, "Sətir sayı", d.Say);

        var ad = "Detal_" + (sahe ?? "detal").Replace("-", "_");
        return Yukle(wb, ad);
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

    public async Task<IActionResult> KreditExcel(string? t)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.KreditPortfelAsync(ParseTarix(t));
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

    public async Task<IActionResult> MenfeetExcel(string? bt, string? st)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.MenfeetAsync(ParseTarix(bt), ParseTarix(st));
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Menfeet");
        int r = 0;
        Setir(sh, r++, $"Mənfəət / Zərər — {m.BasTarix:dd.MM.yyyy} — {m.SonTarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Faiz gəliri", m.FaizGeliri);
        KV(sh, r++, "Faiz xərci", m.FaizXerci);
        KV(sh, r++, "Xalis faiz gəliri (NII)", m.XalisFaizGeliri);
        KV(sh, r++, "NIM % (illik, təxmini)", m.Nim);
        KV(sh, r++, "Əməliyyat gəliri (sinif 6/7)", m.UmumiGelir);
        KV(sh, r++, "Əməliyyat xərci (ehtiyatsız)", m.UmumiXerc);
        KV(sh, r++, "Ehtiyatdan əvvəl mənfəət", m.EhtiyatdanEvvelMenfeet);
        KV(sh, r++, "Xalis ehtiyat (net, törədilmiş)", m.XalisEhtiyat);
        KV(sh, r++, "Xalis mənfəət (GL 50130)", m.XalisMenfeet);
        KV(sh, r++, "Ehtiyat (gross churn)", m.EhtiyatGross);
        KV(sh, r++, "Xərc/Gəlir %", m.XercGelirNisbeti);
        r++;
        r = Bolme(sh, r, "Gəlir strukturu", m.GelirBolgusu);
        r = Bolme(sh, r, "Əməliyyat xərci strukturu", m.XercBolgusu);
        return Yukle(wb, "Menfeet_Zerer");
    }

    public async Task<IActionResult> KeyfiyyetExcel(string? t)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.KreditKeyfiyyetAsync(ParseTarix(t));
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Keyfiyyet");
        int r = 0;
        Setir(sh, r++, $"Kredit Keyfiyyəti & Ehtiyat — {m.Tarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Portfel", m.Portfel);
        KV(sh, r++, "Ümumi ehtiyat", m.Ehtiyat);
        KV(sh, r++, "Ehtiyat / Portfel %", m.EhtiyatFaiz);
        KV(sh, r++, "Problemli qalıq (≥20%)", m.ProblemliQaliq);
        KV(sh, r++, "Ehtiyat örtüyü %", m.Ortuyu);
        KV(sh, r++, "Restrukt sayı", m.RestruktSay);
        KV(sh, r++, "Restrukt qalıq", m.RestruktQaliq);
        KV(sh, r++, "Girovlu qalıq", m.GirovluQaliq);
        KV(sh, r++, "Girovsuz qalıq", m.GirovsuzQaliq);
        KV(sh, r++, "Ümumi girov", m.GirovCem);
        KV(sh, r++, "Orta LTV %", m.OrtaLtv);
        r++;
        var h = sh.CreateRow(r++);
        string[] basliqlar = { "Kateqoriya", "Say", "Qalıq", "Ehtiyat", "Pay %" };
        for (int c = 0; c < basliqlar.Length; c++) h.CreateCell(c).SetCellValue(basliqlar[c]);
        foreach (var k in m.Kateqoriyalar)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(k.Ad);
            row.CreateCell(1).SetCellValue(k.Say);
            row.CreateCell(2).SetCellValue((double)k.Qaliq);
            row.CreateCell(3).SetCellValue((double)k.Ehtiyat);
            row.CreateCell(4).SetCellValue((double)k.Faiz);
        }
        return Yukle(wb, "Kredit_Keyfiyyet");
    }

    public async Task<IActionResult> YerlesdirmeExcel(string? t)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.YerlesdirmeAsync(ParseTarix(t));
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Yerlesdirme");
        int r = 0;
        Setir(sh, r++, $"Yerləşdirilmiş vəsaitlər — {m.Tarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Ümumi portfel", m.UmumiPortfel);
        KV(sh, r++, "Açıq yerləşdirmə sayı", m.Say);
        KV(sh, r++, "Ehtiyat (qarşılıq)", m.Ehtiyat);
        KV(sh, r++, "Ehtiyat / Portfel %", m.EhtiyatFaiz);
        KV(sh, r++, "Xalis portfel (ehtiyatdan sonra)", m.XalisPortfel);
        KV(sh, r++, "Vaxtı keçmiş (qayıtmayıb)", m.VaxtiKecmisMebleg);
        KV(sh, r++, "Vaxtı keçmiş sayı", m.VaxtiKecmisSay);
        KV(sh, r++, "Orta faiz %", m.OrtaFaiz);
        KV(sh, r++, "İllik gözlənilən faiz gəliri", m.IllikGelir);
        KV(sh, r++, "Ən böyük kontragent payı %", m.EnBoyukPay);
        KV(sh, r++, "TOP-3 kontragent payı %", m.Top3Pay);
        KV(sh, r++, "AMB overnight", m.AmbMebleg);
        KV(sh, r++, "AMB overnight sayı", m.AmbSay);
        KV(sh, r++, "Banklararası (AMB xaric)", m.BanklararasiMebleg);
        KV(sh, r++, "Banklararası sayı", m.BanklararasiSay);
        r++;
        var h = sh.CreateRow(r++);
        string[] basliqlar = { "Kontragent", "Say", "Qalıq (AZN)", "Orta faiz %", "Ehtiyat (AZN)", "Pay %" };
        for (int c = 0; c < basliqlar.Length; c++) h.CreateCell(c).SetCellValue(basliqlar[c]);
        foreach (var k in m.Kontragentler)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(k.Ad);
            row.CreateCell(1).SetCellValue(k.Say);
            row.CreateCell(2).SetCellValue((double)k.Qaliq);
            row.CreateCell(3).SetCellValue((double)k.Faiz);
            row.CreateCell(4).SetCellValue((double)k.Ehtiyat);
            row.CreateCell(5).SetCellValue((double)k.Pay);
        }
        r++;
        r = Bolme(sh, r, "Valyuta strukturu", m.ValyutaBolgusu);
        r = Bolme(sh, r, "Qalıq müddət (maturity)", m.MuddetBolgusu);
        return Yukle(wb, "Yerlesdirilmis_Vesaitler");
    }

    public async Task<IActionResult> MaturityExcel(string? t)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.MaturityAsync(ParseTarix(t));
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Maturity");
        int r = 0;
        Setir(sh, r++, $"Kredit Pul Axını (Maturity Ladder) — {m.Tarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Gələcək əsas cəmi", m.EsasCem);
        KV(sh, r++, "Gələcək faiz cəmi", m.FaizCem);
        KV(sh, r++, "Ümumi axın (əsas+faiz)", m.CemAxin);
        KV(sh, r++, "Növbəti 1 ay", m.Axin1Ay);
        KV(sh, r++, "Növbəti 3 ay (kum.)", m.Axin3Ay);
        KV(sh, r++, "Növbəti 12 ay (kum.)", m.Axin12Ay);
        KV(sh, r++, "Tələbli depozit bazası", m.TelebliDepozit);
        KV(sh, r++, "Likvid aktivlər (tampon)", m.Hqla);
        r++;
        var h = sh.CreateRow(r++);
        string[] basliqlar = { "Müddət", "Əsas", "Faiz", "Cəmi", "Kumulyativ", "Pay %" };
        for (int c = 0; c < basliqlar.Length; c++) h.CreateCell(c).SetCellValue(basliqlar[c]);
        foreach (var q in m.Qutular)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(q.Ad);
            row.CreateCell(1).SetCellValue((double)q.Esas);
            row.CreateCell(2).SetCellValue((double)q.Faiz);
            row.CreateCell(3).SetCellValue((double)q.Cem);
            row.CreateCell(4).SetCellValue((double)q.Kumulyativ);
            row.CreateCell(5).SetCellValue((double)q.Faiz_Pay);
        }
        return Yukle(wb, "Maturity_Ladder");
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

    public async Task<IActionResult> RezidentExcel(string? t)
    {
        if (!await IcazeVarAsync()) return Forbid();
        var m = await _service.RezidentAsync(ParseTarix(t));
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Rezident");
        int r = 0;
        Setir(sh, r++, $"Rezident / Qeyri-rezident — {m.Tarix:dd.MM.yyyy}");
        r++;
        KV(sh, r++, "Rezident", m.Rezident);
        KV(sh, r++, "Qeyri-rezident", m.QeyriRezident);
        KV(sh, r++, "Ümumi", m.Umumi);
        KV(sh, r++, "Qeyri-rezident payı %", m.QeyriRezidentPay);
        KV(sh, r++, "Rezident hesab sayı", m.RezidentSay);
        KV(sh, r++, "Qeyri-rezident hesab sayı", m.QeyriRezidentSay);
        return Yukle(wb, "Rezident_Qeyri_Rezident");
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
