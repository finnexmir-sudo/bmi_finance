using System.Globalization;
using System.Security.Claims;
using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Application.Helpers.Emeliyyat;
using FinNex.Application.Helpers.Kredit;
using FinNex.Application.Interfaces.Emeliyyat;
using FinNex.Domain;
using FinNex.UI.Services.Kredit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

// Pul köçürməsi və Tələbə köçürməsi üçün ortaq baza — Novu ilə ayrılır.
[Area("Emeliyyat")]
[Authorize]
public abstract class KocurmeControllerBase : Controller
{
    protected readonly IKocurmeService _service;
    private readonly IWebHostEnvironment _env;

    // 20 000 USD limitinin əsaslandırma sənədləri — seçim siyahısı + inline əlavə
    private readonly IKocurmeSenedNovuService _senedNovu;

    protected KocurmeControllerBase(IKocurmeService service, IWebHostEnvironment env,
                                    IKocurmeSenedNovuService senedNovu)
    {
        _service = service;
        _env = env;
        _senedNovu = senedNovu;
    }

    protected abstract string Novu { get; }     // "Pul" / "Telebe"
    protected abstract string Baslik { get; }    // səhifə başlığı

    private const string V = "~/Areas/Emeliyyat/Views/Kocurme/";

    protected int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected bool IsAdmin() => User.IsInRole(RoleNames.Admin);

    private void Baza()
    {
        ViewBag.Baslik = Baslik;
        ViewBag.Novu = Novu;
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
    }

    /// <summary>
    /// Sənəd növləri siyahısı — formanın HƏR render-ində lazımdır.
    ///
    /// ⚠️ POST UĞURSUZ OLANDA DA çağırılmalıdır: `View(dto)` ilə qayıdanda
    /// `ViewBag` yenidən qurulur və doldurulmasa açılan siyahı BOŞ gələr —
    /// operator limit xətasını görər, amma sənədi seçə bilməz və qıfıla düşər.
    /// </summary>
    private async Task SenedNovleriniYukleAsync()
        => ViewBag.SenedNovleri = await _senedNovu.AktivlerAsync();

    public async Task<IActionResult> Index(int? il)
    {
        var model = await _service.HamisiniGetirAsync(Novu, il);
        ViewBag.Il = il;
        Baza();
        return View($"{V}Index.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Yarat()
    {
        Baza();
        await SenedNovleriniYukleAsync();
        var dto = new KocurmeCreateDto { Tarix = DateTime.Today, HevaleNo = await _service.NovbetiHevaleNoAsync(Novu) };
        return View($"{V}Yarat.cshtml", dto);
    }

    // Mövcud köçürməni təkrar göndər — məlumat dolu, yeni № ilə yeni qeyd
    [HttpGet]
    public async Task<IActionResult> Tekrarla(int id)
    {
        var dto = await _service.TekrarMelumatiAsync(id, Novu);
        if (dto == null)
        {
            TempData["Error"] = "Köçürmə tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        Baza();
        await SenedNovleriniYukleAsync();
        ViewBag.Tekrar = true;
        return View($"{V}Yarat.cshtml", dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(KocurmeCreateDto dto)
    {
        var res = await _service.YaratAsync(Novu, dto, GetUserId());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
        {
            Baza();
            await SenedNovleriniYukleAsync();   // yoxsa siyahı boş gələr, operator qıfıla düşər
            return View($"{V}Yarat.cshtml", dto);
        }
        // Qeyddən sonra Detal-a keç və Word+Excel avtomatik yüklənsin
        TempData["AvtoYukle"] = "1";
        return RedirectToAction(nameof(Detal), new { id = res.Data });
    }

    public async Task<IActionResult> Detal(int id)
    {
        var model = await _service.DetalAsync(id, Novu);
        if (model == null)
        {
            TempData["Error"] = "Köçürmə tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        Baza();
        return View($"{V}Detal.cshtml", model);
    }

    // Canlı voucher preview (yadda saxlanmadan) — JSON
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VoucherPreview(KocurmeCreateDto dto)
    {
        var setirler = _service.VoucherHesabla(dto, string.IsNullOrWhiteSpace(dto.HevaleNo) ? "" : dto.HevaleNo);
        return Json(setirler.Select(s => new { s.Debet, s.Kredit, Mebleg = s.Mebleg, s.Teyinat }));
    }

    // Excel ixracı — başqa proqrama import üçün: sheet "PK_Iran", D=debet, F=kredit, G=məbləğ, J=təyinat
    public async Task<IActionResult> ExcelIxrac(int id)
    {
        var m = await _service.DetalAsync(id, Novu);
        if (m == null) { TempData["Error"] = "Köçürmə tapılmadı."; return RedirectToAction(nameof(Index)); }

        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("PK_Iran");
        for (int i = 0; i < m.Setirler.Count; i++)
        {
            var r = sh.CreateRow(i + 1);                 // 2-ci sətirdən (index 1)
            var s = m.Setirler[i];
            r.CreateCell(3).SetCellValue(s.Debet ?? "");   // D
            r.CreateCell(5).SetCellValue(s.Kredit ?? "");  // F
            r.CreateCell(6).SetCellValue((double)s.Mebleg);// G
            r.CreateCell(9).SetCellValue(s.Teyinat ?? ""); // J
        }
        using var ms = new MemoryStream();
        wb.Write(ms, true);
        var ad = $"PK_Kocur_{(m.HevaleNo ?? id.ToString()).Replace("/", "-")}.xls";
        return File(ms.ToArray(), "application/vnd.ms-excel", ad);
    }

    // Word ərizə ixracı — Erize1.docx şablonu token doldurma
    public async Task<IActionResult> WordIxrac(int id)
    {
        var m = await _service.DetalAsync(id, Novu);
        if (m == null) { TempData["Error"] = "Köçürmə tapılmadı."; return RedirectToAction(nameof(Index)); }

        var sablon = Path.Combine(_env.WebRootPath, "Files", "Word", "Emeliyyat", "Erize1.docx");
        if (!System.IO.File.Exists(sablon)) { TempData["Error"] = "Ərizə şablonu tapılmadı."; return RedirectToAction(nameof(Detal), new { id }); }

        // ── Word dəyərləri BMI-nin köhnə formasından ÖLÇÜLÜB (18.08.2026) ──
        // İstinad sənəd 26-T-24 (giriş: Məbləğ 900, İran Rial kursu 850000):
        //   Məbləğ rəqəmlə   765000000                     ← Mebleg × kurs (KÖÇÜRÜLƏN)
        //   Məbləğ yazı ilə  yeddi yüz altmış beş milyon   ← valyuta sözü YOX
        //   Valyuta növü     İran Rialı
        //   Alınan           900
        //   Satılan          765000000 İran rialı
        //   Məzənnə          850000
        //
        // ƏVVƏL SƏHV İDİ: «Məbləğ rəqəmlə» yerinə `Mebleg` (900.00) yazılırdı —
        // yəni müştəridən alınan məbləğ, köçürülən yox. Sənədin ƏSAS rəqəmi səhv
        // çıxırdı. «Məbləğ yazı ilə» isə sabit boş idi.
        //
        // Hesablama `KocurmeValyuta` helper-indədir — eyni qayda Gedən həvalə
        // jurnalına yazılan məbləğ üçün də lazımdır (KocurmeService), iki nüsxə
        // saxlansa biri mütləq köhnə qalar.
        bool konversiya = KocurmeValyuta.Konversiya(m.KocurulenValyuta);
        decimal mebleg  = m.Mebleg ?? 0m;
        decimal kurs    = m.IranRial ?? 0m;
        decimal geden   = KocurmeValyuta.KocurulenMebleg(m.KocurulenValyuta, m.Mebleg, m.IranRial);   // sənədin əsas rəqəmi

        // Rəqəm formatı: köhnə forma qrup ayırıcısı və artıq sıfır yazmır (765000000).
        // Onluq yalnız real varsa görünür. Mədəniyyət az-AZ — sənəd insan üçündür,
        // ekranlardakı format da belədir (CSS/JS-dən fərqli olaraq invariant DEYİL).
        var azAz = CultureInfo.GetCultureInfo("az-Latn-AZ");
        string N(decimal v) => v.ToString("0.##", azAz);

        // Valyuta adları — köhnə sənəddəki yazılışla eyni.
        // Diqqət: «Valyuta növü» sətrində «İran Rialı» (böyük R), «Satılan» sətrində
        // «İran rialı» (kiçik r) — köhnə formada belədir, qəsdən fərqli saxlanılıb.
        string ValyutaNovu(string? v) => KocurmeValyuta.Adi(v);
        string SatilanAdi(string? v) => v == "Rial" ? "İran rialı" : (v ?? "");

        var tokenler = new Dictionary<string, string?>
        {
            ["{T}"] = m.HevaleNo,
            ["{g_adi}"] = m.GonderenAd, ["{g_soyadi}"] = m.GonderenSoyad, ["{g_ataadi}"] = m.GonderenAtaAd,
            ["{G_passport}"] = m.GonderenPassport, ["{g_telefon}"] = m.GonderenTelefon,
            ["{bank_adi}"] = m.BankAd, ["{filial}"] = m.Filial,
            ["{a_adi}"] = m.AlanAd, ["{a_soyadi}"] = m.AlanSoyad, ["{a_ataadi}"] = m.AlanAtaAd,
            ["{a_hesab}"] = m.AlanHesab, ["{a_passport}"] = m.AlanPassport,
            ["{mebleg}"] = N(geden),
            // Tam hissə, valyuta sözü OLMADAN — `MebleghSoze` işlətməyin, o «manat»/
            // «qəpik» sözlərini SABİT əlavə edir (kredit müqaviləsi üçün yazılıb) və
            // dollar/rial köçürməsində səhv olar. Bax: CLAUDE.md — valyuta tələsi.
            ["{m_yaziile}"] = KreditSozeCevir.MebleghSozeQepiksiz(geden),
            ["{valyuta_novu}"] = ValyutaNovu(m.KocurulenValyuta),
            ["{meqsed}"] = m.Meqsed, ["{elave}"] = m.Elave, ["{qeyd}"] = m.Qeyd,
            ["{alinan_valyuta}"] = $"{N(mebleg)} {ValyutaNovu(m.MedaxilValyuta)}".Trim(),
            ["{satilan_valyuta}"] = konversiya ? $"{N(geden)} {SatilanAdi(m.KocurulenValyuta)}".Trim() : "",
            ["{mezenne}"] = konversiya ? N(kurs) : "",
            ["{tarix}"] = m.Tarix?.ToString("dd.MM.yyyy")
        };
        var bytes = KreditWordService.Doldur(sablon, tokenler);
        var ad = $"Erize_{(m.HevaleNo ?? id.ToString()).Replace("/", "-")}.docx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ad);
    }

    [HttpGet]
    public async Task<IActionResult> Redakte(int id)
    {
        var dto = await _service.RedakteMelumatiAsync(id, Novu);
        if (dto == null)
        {
            TempData["Error"] = "Köçürmə tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        if (!IsAdmin() && dto.YaradanId != GetUserId())
        {
            TempData["Error"] = "Bu köçürməni yalnız yaradan və ya Admin dəyişə bilər.";
            return RedirectToAction(nameof(Index));
        }
        Baza();
        await SenedNovleriniYukleAsync();
        return View($"{V}Redakte.cshtml", dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redakte(KocurmeEditDto dto)
    {
        var res = await _service.YenileAsync(Novu, dto, GetUserId(), IsAdmin());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
        {
            Baza();
            await SenedNovleriniYukleAsync();   // yoxsa siyahı boş gələr
            return View($"{V}Redakte.cshtml", dto);
        }
        return RedirectToAction(nameof(Index));
    }

    // ══ 20 000 USD AYLIQ LİMİTİ ═══════════════════════════════════════════

    /// <summary>
    /// Formadakı CANLI yoxlama. Cavabı SERVER hesablayır — yadda saxlama yolu
    /// ilə eyni metod (`LimitYoxlaAsync`). JS burada heç nə hesablamır.
    ///
    /// `ValidateAntiForgeryToken` YOXDUR — bu, yalnız OXUYAN endpoint-dir,
    /// heç nə yazmır. Token tələb etsəydik hər fetch-ə əlavə iş düşərdi.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> LimitYoxla(
        string? fin, decimal? mebleg, string? medaxilValyuta, DateTime? tarix, int? id)
    {
        var n = await _service.LimitYoxlaAsync(Novu, fin, mebleg, medaxilValyuta, tarix, id);

        return Json(new
        {
            cemiUsd          = n.Movcud.CemiUsd,
            sayi             = n.Movcud.Sayi,
            yeniUsd          = n.YeniUsd,
            sonraCem         = n.SonraCem,
            limit            = n.Movcud.Limit,
            senedTelebOlunur = n.SenedTelebOlunur,
            kursAlinmadi     = n.KursAlinmadi,
            mesaj            = n.Mesaj
        });
    }

    /// <summary>
    /// Siyahı səhifəsindəki FİN axtarışı — bir şəxsin bir ayının cəmi.
    /// Rəqəm forma ilə EYNİ mənbədən gəlir (`FinAyliqCemAsync`).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> FinAyliqCem(string? fin, int? il, int? ay)
    {
        var indi = DateTime.Today;
        var n = await _service.FinAyliqCemAsync(fin, il ?? indi.Year, ay ?? indi.Month);

        var mesaj = string.IsNullOrWhiteSpace(n.Fin)
            ? "FİN yazın."
            : n.Sayi == 0
                ? $"«{n.Fin}» üzrə bu ayda köçürmə tapılmadı."
                : $"«{n.Fin}» — {n.Sayi} əməliyyat, cəmi {n.CemiUsd:N2} USD. " +
                  (n.Asilib
                      ? $"Hədd ({n.Limit:N0} USD) AŞILIB — {(n.CemiUsd - n.Limit):N2} USD artıq."
                      : $"Qalıq: {n.Qaliq:N2} USD.");

        return Json(new
        {
            fin     = n.Fin,
            sayi    = n.Sayi,
            cemiUsd = n.CemiUsd,
            limit   = n.Limit,
            qaliq   = n.Qaliq,
            asilib  = n.Asilib,
            mesaj
        });
    }

    /// <summary>
    /// Siyahıda olmayan sənəd növünü ELƏ FORMADAN əlavə etmək (istifadəçi
    /// qərarı 07.09.2026). Növ ortaq cədvələ düşür — növbəti dəfə hamı
    /// siyahıdan seçir, «hərə nə gəldi yazmasın» qaydası belə qorunur.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedNovuYarat(string ad)
    {
        var res = await _senedNovu.YaratAsync(ad, GetUserId());
        return Json(new
        {
            ugur  = res.Success,
            id    = res.Success ? res.Data : 0,
            ad    = (ad ?? "").Trim(),
            mesaj = res.Message
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var res = await _service.SilAsync(id, GetUserId(), IsAdmin());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
