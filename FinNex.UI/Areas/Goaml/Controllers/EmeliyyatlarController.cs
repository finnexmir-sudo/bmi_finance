using System.Text;
using FinNex.Application.DTOs.Goaml;
using FinNex.Application.Interfaces.Goaml;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinNex.UI.Areas.Goaml.Controllers;

/// <summary>
/// GOAML — "Əməliyyatlar" tabı. Namizəd Oracle-dan OXUNUR (yalnız SELECT),
/// operator seçib "Təsdiqlə" edəndə bizim ÖZ SQL Server-imizə yazılır.
/// </summary>
[Area("Goaml")]
[Authorize]
[Icaze(IcazeKodlari.GoamlBax)]
public class EmeliyyatlarController : Controller
{
    private readonly IGoamlService _goaml;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;
    private readonly ILogger<EmeliyyatlarController> _logger;

    public EmeliyyatlarController(IGoamlService goaml, UserManager<AppUser> userManager, IConfiguration config,
        ILogger<EmeliyyatlarController> logger)
    {
        _goaml = goaml;
        _userManager = userManager;
        _config = config;
        _logger = logger;
    }

    private async Task<int?> GetCurrentIsciIdAsync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        return appUser?.IsciId;
    }

    // ── GET /Goaml/Emeliyyatlar — axtarış ekranı ───────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Əməliyyatlar";
        return View(new GoamlAxtarisDto
        {
            BasTarix = DateTime.Today,
            SonTarix = DateTime.Today,
        });
    }

    // ── POST /Goaml/Emeliyyatlar/Axtar — AJAX, namizəd siyahısı ────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Axtar([FromForm] GoamlAxtarisDto dto)
    {
        if (dto.SonTarix < dto.BasTarix)
            return Json(new { success = false, message = "Bitmə tarixi başlanğıcdan əvvəl ola bilməz." });

        var netice = await _goaml.NamizedAxtarAsync(dto);
        if (!netice.Success)
            return Json(new { success = false, message = netice.Message });

        return Json(new { success = true, setirler = netice.Data });
    }

    // ── GET /Goaml/Emeliyyatlar/Tesdiq — seçilmiş namizəd üçün KYC forması ──
    public async Task<IActionResult> Tesdiq(int filialNumber, DateTime dateOper, long recNum,
        string? debet, string? kredit, decimal? summaVNacval, int? kodValuti,
        string? innDebet, string? innKredit, string? primechanie,
        string? nameLicschDe, string? nameLicschKre, string? documentSource)
    {
        ViewData["Title"] = "GOAML — Təsdiqlə";

        var dto = new GoamlTesdiqDto
        {
            CandidateFilialNumber = filialNumber,
            CandidateDateOper = dateOper,
            CandidateRecNum = recNum,
            Debet = debet,
            Kredit = kredit,
            SummaVNacval = summaVNacval,
            KodValuti = kodValuti,
            Primechanie = primechanie,
            DateOper = dateOper,
            NameUr = nameLicschDe,
            ToAdi = nameLicschKre,
            DocumentSource = documentSource,
        };

        // ── "Sistem gətirə bilmirsə, işçi doldurur" (09.11.2026, istifadəçi
        //    qərarı) — 12.11.2026-da KRİTİK DÜZƏLİŞ: artıq Debet-i "əsas"
        //    götürmürük. Debet VƏ Kredit-in HƏR İKİSİ yoxlanır:
        //      - YALNIZ BİRİNDƏ nəticə varsa — qeyri-müəyyənlik yoxdur,
        //        Tip-dən asılı olmadan DƏRHAL doldurulur (köhnə davranış).
        //      - HƏR İKİSİNDƏ nəticə varsa (məs. hüquqi-hüquqi, hər ikisinin
        //        öz Rəhbəri/Nümayəndəsi) — DOLDURMA, operator "Tip" seçəndə
        //        (OUT→Debet, IN→Kredit) JS client-side tətbiq edəcək; HToH/
        //        FToF kimi istiqamətsiz tiplərdə əməliyyatçı özü seçəcək
        //        (bax Tesdiq.cshtml-dəki "Debet/Kredit tərəfini götür" düymələri).
        //    Hər iki tərəfin xam datası ViewBag.KycDebet/KycKredit ilə görüntüyə
        //    ötürülür ki, Tip dəyişəndə əlavə server sorğusu lazım olmasın. ────
        var herIkiTeref = await _goaml.HesabSahibiKycTapAsync(filialNumber, debet, kredit);
        var yalnizBirTerefdeVar = (herIkiTeref.Debet != null) ^ (herIkiTeref.Kredit != null);
        var yeganeKyc = yalnizBirTerefdeVar ? (herIkiTeref.Debet ?? herIkiTeref.Kredit) : null;
        if (yeganeKyc != null)
            KycTetbiqEt(dto, yeganeKyc);

        ViewBag.KycDebet = herIkiTeref.Debet;
        ViewBag.KycKredit = herIkiTeref.Kredit;
        ViewBag.HerIkiTerefdeKycVar = herIkiTeref.Debet != null && herIkiTeref.Kredit != null;
        ViewBag.ElaqaliSexsler = yeganeKyc != null && !yeganeKyc.Fiziki
            ? yeganeKyc.ElaqaliSexsler
            : new List<GoamlElaqaliSexsDto>();
        ViewBag.AvtoDolduruldu = yeganeKyc?.TekSexs != null;

        // ── "Nümayəndə" (CON) — əməliyyatın ÖZÜNÜN DOC_ID=RECNUM-u ilə
        //    (13.11.2026, istifadəçi tapıntısı: odb.emitent_benefisiar).
        //    Hesab-əsaslı CON/SIG doldurmasından (yuxarı) TAMAMİLƏ MÜSTƏQİLDİR,
        //    AMMA tapılsa ONDAN DAHA DƏQİQDİR (konkret bu əməliyyatı aparan
        //    şəxsdir) — ona görə SONRA çağırılır ki, varsa köhnə hesab-əsaslı
        //    CON təxminini üstələsin. NUMAYENDE dropdown-u (yuxarıdakı
        //    gmlConSelectWrap) TOXUNULMUR — operator yenə ondan fərqli
        //    birini seçə bilər, "bosdursa bos qalacaq" (istifadəçi qərarı).
        var emitentKyc = await _goaml.EmitentKycAsync(recNum);
        if (emitentKyc != null)
        {
            dto.ConAdi = emitentKyc.Adi;
            dto.ConSoyadi = emitentKyc.Soyadi;
            dto.ConAtasininAdi = emitentKyc.AtaAdi;
            dto.ConDogulduguTarix = emitentKyc.DogulduguTarix;
            dto.ConDogulduguYer = emitentKyc.DogulduguYer;
            dto.ConCinsi = emitentKyc.Cinsi;
            dto.ConVetendasligi = emitentKyc.Vetendasligi;
            dto.ConPin = emitentKyc.Pin;
            dto.ConUnvaninNovu = emitentKyc.UnvaninNovu;
            dto.ConUnvan = emitentKyc.Unvan;
            dto.ConSeher = emitentKyc.Seher;
            dto.ConOlke = emitentKyc.Olke;
            dto.ConSenedinNovu = emitentKyc.SenedinNovu;
            dto.ConSenedinSeriyaNomresi = emitentKyc.SenedinSeriyaNomresi;
            dto.ConSenedinVerilmeTarixi = emitentKyc.SenedinVerilmeTarixi;
            dto.ConSenedinEtibarliliqMuddeti = emitentKyc.SenedinEtibarliliqMuddeti;
            dto.ConVerenOrqan = emitentKyc.VerenOrqan;
            dto.ConVerildiyiOlke = emitentKyc.VerildiyiOlke;
        }
        ViewBag.EmitentDenDolduruldu = emitentKyc != null;

        return View(dto);
    }

    /// <summary>Hesab sahibinin KYC-sini (fiziki — CON+SIG-in hər ikisinə; hüquqi —
    /// YALNIZ "Rəhbər", SIG-ə) <see cref="GoamlTesdiqDto"/>-ya yazır. GET Tesdiq-in
    /// "yalnız bir tərəfdə nəticə var" qolunda çağırılır — Tip-dən asılı seçimi
    /// Tesdiq.cshtml-in öz JS-i (eyni sahələri doldurur) edir.</summary>
    private static void KycTetbiqEt(GoamlTesdiqDto dto, GoamlHesabSahibiKycDto kyc)
    {
        if (kyc.TekSexs == null) return;

        if (kyc.Fiziki)
        {
            dto.ConAdi = kyc.TekSexs.Adi;
            dto.ConSoyadi = kyc.TekSexs.Soyadi;
            dto.ConAtasininAdi = kyc.TekSexs.AtaAdi;
            dto.ConDogulduguTarix = kyc.TekSexs.DogulduguTarix;
            dto.ConDogulduguYer = kyc.TekSexs.DogulduguYer;
            dto.ConCinsi = kyc.TekSexs.Cinsi;
            dto.ConUnvan = kyc.TekSexs.Unvan;
            dto.ConVetendasligi = kyc.TekSexs.Vetendasligi;
            dto.ConPin = kyc.TekSexs.Pin;
            dto.ConSenedinSeriyaNomresi = kyc.TekSexs.SenedinSeriyaNomresi;
            dto.ConSenedinVerilmeTarixi = kyc.TekSexs.SenedinVerilmeTarixi;
            dto.ConSenedinEtibarliliqMuddeti = kyc.TekSexs.SenedinEtibarliliqMuddeti;
            dto.ConVerenOrqan = kyc.TekSexs.VerenOrqan;
        }

        dto.SigAdi = kyc.TekSexs.Adi;
        dto.SigSoyadi = kyc.TekSexs.Soyadi;
        dto.SigAtasininAdi = kyc.TekSexs.AtaAdi;
        dto.SigDogulduguTarix = kyc.TekSexs.DogulduguTarix;
        dto.SigDogulduguYer = kyc.TekSexs.DogulduguYer;
        dto.SigVetendasligi = kyc.TekSexs.Vetendasligi;
        dto.SigUnvan = kyc.TekSexs.Unvan;
        dto.SigSenedinNovu = kyc.TekSexs.SenedinNovu;
        dto.SigSenedinSeriyaNomresi = kyc.TekSexs.SenedinSeriyaNomresi;
        dto.SigSenedinVerilmeTarixi = kyc.TekSexs.SenedinVerilmeTarixi;
        dto.SigSenedinEtibarliliqMuddeti = kyc.TekSexs.SenedinEtibarliliqMuddeti;
        dto.SigVerenOrqan = kyc.TekSexs.VerenOrqan;
        dto.SigVezife = kyc.TekSexs.Vezife;
        dto.SigIsYeriAdi = kyc.TekSexs.IsYeriAdi;
    }

    // ── POST /Goaml/Emeliyyatlar/Tesdiq — KYC formunu yaddaşa yazır ─────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tesdiq(GoamlTesdiqDto dto)
    {
        var isciId = await GetCurrentIsciIdAsync();
        if (isciId == null)
        {
            TempData["Error"] = "İşçi profiliniz tapılmadı.";
            return View(dto);
        }

        var netice = await _goaml.TesdiqleAsync(dto, isciId.Value);
        if (!netice.Success)
        {
            TempData["Error"] = netice.Message;
            return View(dto);
        }

        TempData["Success"] = netice.Message;
        return RedirectToAction(nameof(Siyahi));
    }

    // ── GET /Goaml/Emeliyyatlar/Siyahi — təsdiqlənmiş qeydlər ───────────────
    public async Task<IActionResult> Siyahi()
    {
        ViewData["Title"] = "GOAML — Təsdiqlənmiş əməliyyatlar";
        var netice = await _goaml.HamisiniGetirAsync();
        return View(netice.Data ?? new List<GoamlEmeliyyatListDto>());
    }

    // ── GET /Goaml/Emeliyyatlar/Detal/{id} — TƏK qeydin tam (sırf oxunuş) detalı ──
    public async Task<IActionResult> Detal(int id)
    {
        ViewData["Title"] = "GOAML — Əməliyyat detalı";
        var netice = await _goaml.DetalGetirAsync(id);
        if (!netice.Success || netice.Data == null)
        {
            TempData["Error"] = netice.Message ?? "Qeyd tapılmadı.";
            return RedirectToAction(nameof(Siyahi));
        }

        return View(netice.Data);
    }

    // ── POST /Goaml/Emeliyyatlar/Sil/{id} — təsdiqlənmiş qeydi geri götürür ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var netice = await _goaml.SilAsync(id);
        TempData[netice.Success ? "Success" : "Error"] = netice.Message;
        return RedirectToAction(nameof(Siyahi));
    }

    // ── GET /Goaml/Emeliyyatlar/Export — tarix/filtr forması ────────────────
    public IActionResult Export()
    {
        ViewData["Title"] = "GOAML — Export to file (JSON)";
        return View(new GoamlJsonIxracFiltrDto { BasTarix = DateTime.Today, SonTarix = DateTime.Today });
    }

    // ── POST /Goaml/Emeliyyatlar/Onizle — AJAX, "İxrac et"dən ƏVVƏL hansı
    //    qeydlərin uyğun gələcəyini göstərir (13.11.2026, istifadəçi tələbi:
    //    "hansı sənədlər ixrac olunacaq onu göstər"). HEÇ NƏYİ dəyişdirmir —
    //    JsonIxracAsync-in İSTİFADƏ ETDİYİ EYNİ filtr şərtini paylaşır. ───────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onizle([FromForm] GoamlJsonIxracFiltrDto filtr)
    {
        if (filtr.SonTarix < filtr.BasTarix)
            return Json(new { success = false, message = "Bitmə tarixi başlanğıcdan əvvəl ola bilməz." });

        var netice = await _goaml.IxracOnizlemeAsync(filtr);
        if (!netice.Success)
            return Json(new { success = false, message = netice.Message });

        return Json(new { success = true, setirler = netice.Data });
    }

    // ── POST /Goaml/Emeliyyatlar/Export — JSON hazırlayır, DMS-ə yazır, uğurlu
    //    olanda GÖRÜNƏN banner + yükləmə linki ilə geri qayıdır (13.11.2026 —
    //    ƏVVƏL birbaşa File() qaytarırdı, TempData["Success"] HEÇ VAXT görünmürdü,
    //    çünki fayl-yükləmə cavabı səhifəni YENİDƏN render ETMİR; PRG (Post-
    //    Redirect-Get) naxışına keçirildi — "Sil" action-unun ÖZÜNÜN artıq
    //    işlətdiyi EYNİ naxış). ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Export(GoamlJsonIxracFiltrDto filtr)
    {
        if (filtr.SonTarix < filtr.BasTarix)
        {
            TempData["Error"] = "Bitmə tarixi başlanğıcdan əvvəl ola bilməz.";
            return RedirectToAction(nameof(Export));
        }

        var netice = await _goaml.JsonIxracAsync(filtr);
        if (!netice.Success || netice.Data == null)
        {
            TempData["Error"] = netice.Message ?? "İxrac uğursuz oldu.";
            return RedirectToAction(nameof(Export));
        }

        // ── DMS-ə yazmaq İNDİ MƏCBURİDİR (əvvəl best-effort idi) — PRG axınında
        //    faktiki brauzer-yükləməsi AYRI bir GET sorğusu (IxracFayli) ilə,
        //    DİSKDƏKİ fayldan gəlir; bu POST cavabı artıq birbaşa bayt qaytarmır.
        //    Qeydlər ARTIQ "İxracEdilib"ə keçib (JsonIxracAsync-in özündə commit
        //    olunub) — yazı sınsa belə statusu geri qaytarmırıq, yalnız operatora
        //    açıq xəbərdarlıq veririk ki, əlaqəli heç nə itməyib.
        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var dir = Path.Combine(dmsRoot, "goaml-ixrac");
        try
        {
            Directory.CreateDirectory(dir);
            await System.IO.File.WriteAllTextAsync(Path.Combine(dir, netice.Data.TeklifOlunanFaylAdi), netice.Data.JsonMetni, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GOAML Export — DMS-ə yazma uğursuz oldu: {Fayl}", netice.Data.TeklifOlunanFaylAdi);
            TempData["Error"] = $"{netice.Data.SetirSayi} qeyd ixrac edildi (statusu artıq \"İxrac edilib\"-dir), " +
                "AMMA fayl diskə yazıla bilmədi — DMS qovluğuna (C:\\FinNex_DMS\\goaml-ixrac) çıxış yoxdur. " +
                "Mühəndisə bildirin, qeydlərin özü İTMƏYİB.";
            return RedirectToAction(nameof(Export));
        }

        TempData["Success"] = netice.Data.Xeberdarliqlar.Count > 0
            ? $"{netice.Data.SetirSayi} qeyd ixrac edildi — \"{netice.Data.TeklifOlunanFaylAdi}\". " + string.Join(" | ", netice.Data.Xeberdarliqlar)
            : $"{netice.Data.SetirSayi} qeyd ixrac edildi — \"{netice.Data.TeklifOlunanFaylAdi}\".";
        TempData["IxracFayli"] = netice.Data.TeklifOlunanFaylAdi;

        // 13.11.2026, KRİTİK DÜZƏLİŞ: `TempData["Success"]`-in yuxarıdakı
        // xəbərdarlıq mətni Export.cshtml-in ÖZ "İxrac tamamlandı" qutusunda
        // HEÇ VAXT OXUNMURDU (o qutu YALNIZ "IxracFayli"-ni göstərir) — real
        // hadisə: `malumatin_novu` sorğusu konkret bir ixracda keçici səbəbdən
        // uğursuz oldu (reportType/reportReason 0/0 yazıldı), server xəbərdarlığı
        // hazırladı, AMMA ekranda HEÇ YERDƏ görünmədi. İndi AYRI, PERSİSTENT
        // (fn-alert YOX, auto-hide olmur) bir TempData açarı — Export.cshtml
        // bunu "gml-warn" qutusu ilə HƏMİŞƏ göstərir.
        if (netice.Data.Xeberdarliqlar.Count > 0)
            TempData["IxracXeberdarliqlari"] = string.Join(" | ", netice.Data.Xeberdarliqlar);

        return RedirectToAction(nameof(Export));
    }

    // ── GET /Goaml/Emeliyyatlar/IxracFayli — PRG-dən sonra faylı DMS-dən oxuyub
    //    endirir (13.11.2026). `fayl` Path.GetFileName ilə TƏMİZLƏNİR — qovluq-
    //    keçişi (path traversal) riski yoxdur. ───────────────────────────────
    public IActionResult IxracFayli(string fayl)
    {
        var temizAd = Path.GetFileName(fayl ?? "");
        if (string.IsNullOrWhiteSpace(temizAd))
        {
            TempData["Error"] = "Fayl adı etibarsızdır.";
            return RedirectToAction(nameof(Export));
        }

        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var yol = Path.Combine(dmsRoot, "goaml-ixrac", temizAd);
        if (!System.IO.File.Exists(yol))
        {
            TempData["Error"] = "Fayl tapılmadı — DMS-dən silinmiş ola bilər.";
            return RedirectToAction(nameof(Export));
        }

        var bytes = System.IO.File.ReadAllBytes(yol);
        return File(bytes, "application/json", temizAd);
    }
}
