using System.Text;
using FinNex.Application.DTOs.Goaml;
using FinNex.Application.Interfaces.Goaml;
using FinNex.Domain;
using FinNex.Domain.Entities.Goaml;
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
        string? debet, string? kredit, decimal? summaVNacval, decimal? summaVInval, int? kodValuti,
        string? innDebet, string? innKredit, string? primechanie,
        string? nameLicschDe, string? nameLicschKre, string? documentSource,
        string? registracNomerDe, string? registracNomerKre)
    {
        ViewData["Title"] = "GOAML — Təsdiqlə";

        // 17.11.2026, KRİTİK — "Debet = əsas tərəf" fərziyyəsi AYSEL-in real ixracı
        // ilə tutuşdurularkən YANLIŞ çıxdı: Debet="10020" (kassa/GL), Kredit="40160…"
        // (real müştəri, "MÜŞTƏRİ ŞİRKƏTİ-A MMC") olan bir namizəddə AYSEL HƏR İKİ Entity blokunu
        // (Sender VƏ Receiver) "MÜŞTƏRİ ŞİRKƏTİ-A MMC" ilə yazırdı, biz isə Debet-in ÖZ display
        // adını ("KASSA - USD") yazırdıq — kassa/GL hesabının "adı" real müştəri
        // adı DEYİL. Eyni "100"-prefiks qaydası (`TrnTipTeyinEt`-in artıq işlətdiyi)
        // burada da — hansı tərəf kassadırsa, DİGƏR tərəfin adı/registrac_nomer-i
        // ÜSTÜN tutulur.
        var debetKassadir = Pref3(debet, "100");
        var kreditKassadir = !debetKassadir && Pref3(kredit, "100");

        var dto = new GoamlTesdiqDto
        {
            CandidateFilialNumber = filialNumber,
            CandidateDateOper = dateOper,
            CandidateRecNum = recNum,
            Debet = debet,
            Kredit = kredit,
            SummaVNacval = summaVNacval,
            // 16.11.2026: eyni "heç yerə köçürülmürdü" bugu — namizəd sorğusu
            // SummaVInval-ı (xarici valyuta məbləği) onsuz da daşıyır, AMMA GET
            // action onu heç qəbul etmirdi → mübadilə-stolu əməliyyatlarında
            // sender_curr_amount həmişə 0 yazılırdı (bax CLAUDE.md).
            SummaVInval = summaVInval,
            KodValuti = kodValuti,
            Primechanie = primechanie,
            DateOper = dateOper,
            // NameUr HƏM Sender.Entity, HƏM Receiver.Entity-ni (SenderQurAsync/
            // ReceiverQurAsync-ın HƏR İKİSİ) qidalandırır — kassa/GL tərəfindən
            // DEYİL, REAL müştəri tərəfindən gəlməlidir (yuxarıdaki şərhə bax).
            NameUr = debetKassadir ? nameLicschKre : nameLicschDe,
            ToAdi = nameLicschKre,
            DocumentSource = documentSource,
            // 16.11.2026: AYSEL-in real ixracı ilə tutuşdurularkən üzə çıxdı —
            // "trn_detailed_id" elə bu sahədən (REGISTRAC_NOMER) gəlir, namizəd
            // sorğusu onu artıq daşıyır (RegistracNomerDe/Kre), amma heç yerə
            // köçürülmürdü — operator üçün forma sahəsi olmadığı üçün HƏMİŞƏ
            // boş qalırdı.
            //
            // 17.11.2026, KRİTİK — sadə "DE əvvəl" DÜZ DEYİL: AYSEL "016901"
            // (Kredit-in registrac_nomer-i) yazırdı, biz isə "000001" (Debet/kassa
            // tərəfinin registrac_nomer-i, çox güman GL hesabının sabit/sentinel
            // dəyəri) yazırdıq — kassa tərəfinin registrac_nomer-i REAL müştəri
            // qeydiyyat nömrəsi DEYİL.
            RegistracNomer = debetKassadir ? (registracNomerKre ?? registracNomerDe)
                : kreditKassadir ? (registracNomerDe ?? registracNomerKre)
                : (registracNomerDe ?? registracNomerKre),
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

        // 17.11.2026 — VÖEN (`IncorporationNumber`) NameUr İLƏ EYNİ "hansı tərəf
        // əsl müştəridir" qaydasını izləyir (kassadırsa digər tərəf) — Tip-dən
        // ASILI DEYİL, çünki bu, hesabın ÖZ xüsusiyyətidir, əməliyyatın
        // istiqaməti deyil. Mənbə: `odb.regnom.INN_REGNOM` (istifadəçi tapıntısı).
        dto.IncorporationNumber = debetKassadir ? herIkiTeref.Kredit?.Voen : herIkiTeref.Debet?.Voen;

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

    /// <summary>`GoamlService.Pref3`-ün (private) ƏYNİSİ — bir hesabın birinci 3
    /// simvolunun verilmiş prefikslə üst-üstə düşüb-düşmədiyini yoxlayır. Burada
    /// yalnız kassa/GL ("100") tərəfini tapmaq üçün — `dto.RegistracNomer`-in
    /// seçimində istifadə olunur (17.11.2026).</summary>
    private static bool Pref3(string? hesab, string teq) =>
        !string.IsNullOrEmpty(hesab) && hesab.Length >= 3 && hesab.Substring(0, 3) == teq;

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
        // 23.11.2026 — `Snn` (SIG-in FİN-i) burada ARTIQ ƏLDƏ İDİ (`kyc.TekSexs.Pin`,
        // ConPin-in elə eyni mənbəyi), AMMA heç bir dto sahəsinə YAZILMIRDI — nəticə,
        // `HeaderZenginlesdirAsync`-in regnom+fin axtarışı (sphereOfActivity,
        // businessStartDate və s.) HƏMİŞƏ boş qalırdı, çünki `fin` arqumenti
        // (Snn) heç vaxt dolu deyildi. Bax CLAUDE.md.
        dto.Snn = kyc.TekSexs.Pin;
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
    //    17.11.2026 — "Məlumatın növü" artıq ÇOX seçimlidir (checkbox qrupu,
    //    bax Tesdiq.cshtml): eyni əməliyyat HƏM CTR, HƏM BLR, hətta HƏM PEP
    //    ola bilər (AML məsul şəxsin tələbi). `_goaml.TesdiqleAsync` özü
    //    TƏK bir ReportCode qəbul edir (dəyişmədi — "Metod İmzası
    //    Dəyişikliyi" riskini artırmamaq üçün) — bu action HƏR seçilmiş kod
    //    üçün onu AYRI çağırır, HƏR çağırış ÖZ `GoamlEmeliyyat` sətrini
    //    yaradır (eyni KYC, fərqli ReportCode). Dedup açarı indi `ReportCode`-u
    //    da daşıdığı üçün (bax GoamlService.TesdiqleAsync) bu dövrələr
    //    bir-birini BLOKLAMIR.
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

        var secilenKodlar = (dto.ReportCodes ?? new List<int>()).Distinct().ToList();
        if (secilenKodlar.Count == 0)
        {
            TempData["Error"] = "Məlumatın növü — ən azı bir hesabat növü seçilməlidir.";
            return View(dto);
        }

        var ugurlular = new List<string>();
        var xetalar = new List<string>();
        foreach (var kod in secilenKodlar)
        {
            dto.ReportCode = (GoamlHesabatNovu)kod;
            var netice = await _goaml.TesdiqleAsync(dto, isciId.Value);
            if (netice.Success)
                ugurlular.Add(dto.ReportCode.Value.Kod());
            else
                xetalar.Add($"{dto.ReportCode.Value.Kod()}: {netice.Message}");
        }

        if (ugurlular.Count > 0)
            TempData["Success"] = $"Təsdiqləndi: {string.Join(", ", ugurlular)}" +
                (xetalar.Count > 0 ? $" — AMMA uğursuz oldu: {string.Join(" | ", xetalar)}" : ".");

        if (ugurlular.Count == 0)
        {
            TempData["Error"] = string.Join(" | ", xetalar);
            return View(dto);
        }

        return RedirectToAction(nameof(Siyahi));
    }

    // ── GET /Goaml/Emeliyyatlar/RedakteEt/{id} — mövcud ("Qaralama" statuslu)
    //    qeydi redaktə formasında açır. Tesdiq.cshtml-in EYNİ görünüşünü paylaşır
    //    (23.11.2026, real hadisə: "Qaralamaya qaytar"dan sonra qeydə YENİDƏN
    //    toxuna bilmirdilər). Oracle-a HEÇ müraciət olunmur, auto-doldurma
    //    banner-ləri/dropdown-ları söndürülür (qeyd ARTIQ bir dəfə doldurulub,
    //    operatorun özünün yazdığı/düzəltdiyi dəyərlər canlı Oracle ilə
    //    ÜSTƏLƏNMƏMƏLİDİR).
    public async Task<IActionResult> RedakteEt(int id)
    {
        ViewData["Title"] = "GOAML — Redaktə et";
        var netice = await _goaml.RedakteFormuAsync(id);
        if (!netice.Success || netice.Data == null)
        {
            TempData["Error"] = netice.Message ?? "Qeyd tapılmadı.";
            return RedirectToAction(nameof(Siyahi));
        }

        ViewBag.RedaktaId = id;
        ViewBag.ElaqaliSexsler = new List<GoamlElaqaliSexsDto>();
        ViewBag.AvtoDolduruldu = false;
        ViewBag.EmitentDenDolduruldu = false;
        ViewBag.KycDebet = null;
        ViewBag.KycKredit = null;
        ViewBag.HerIkiTerefdeKycVar = false;
        return View("Tesdiq", netice.Data);
    }

    // ── POST /Goaml/Emeliyyatlar/RedakteEt/{id} — mövcud qeydi YERİNDƏ yeniləyir
    //    (YENİ sətir yaratmır) ──────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedakteEt(int id, GoamlTesdiqDto dto)
    {
        var netice = await _goaml.RedakteSaxlaAsync(id, dto);
        if (!netice.Success)
        {
            TempData["Error"] = netice.Message;
            ViewBag.RedaktaId = id;
            ViewBag.ElaqaliSexsler = new List<GoamlElaqaliSexsDto>();
            ViewBag.AvtoDolduruldu = false;
            ViewBag.EmitentDenDolduruldu = false;
            ViewBag.KycDebet = null;
            ViewBag.KycKredit = null;
            ViewBag.HerIkiTerefdeKycVar = false;
            return View("Tesdiq", dto);
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

    // ── POST /Goaml/Emeliyyatlar/IxracGeriQaytar/{id} — ixrac edilmiş qeydi
    //    "Qaralama"ya qaytarır ki, operator onu YENİDƏN (bu dəfə, məs., tək-tək)
    //    ixrac edə bilsin (23.11.2026, real hadisə). ──────────────────────────
    //    23.11.2026, İKİNCİ DALĞA — əvvəl Redirect+TempData idi, tam səhifə
    //    yenilənib yuxarı sıçrayırdı (istifadəçi şikayəti). İndi `HamisiniOxunduIsareEt`
    //    (Gələn Mail) ilə EYNİ naxış — HƏMİŞƏ Json qaytarır, Siyahi.cshtml-in
    //    JS-i sətri AJAX-la yeniləyir, səhifə YENİDƏN YÜKLƏNMİR.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IxracGeriQaytar(int id)
    {
        var netice = await _goaml.IxracGeriQaytarAsync(id);
        return Json(new { success = netice.Success, message = netice.Message });
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
    //    işlətdiyi EYNİ naxış). 16.11.2026-dan: `JsonIxracAsync` BİR YOX, bir
    //    neçə fayl (hər ReportCode üçün ayrı) qaytara bilər — hamısı AYRI-AYRI
    //    DMS-ə yazılır, TempData-ya `;;`-ayırıcı ilə SİYAHI kimi qoyulur
    //    (TempData default olaraq `List<T>`-i saxlaya bilmir, sadə mətn lazımdır).
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
        //    açıq xəbərdarlıq veririk ki, əlaqəli heç nə itməyib. HƏR fayl AYRI
        //    `try/catch`-dədir — biri uğursuz olsa belə, qalan fayllar diskə
        //    yazılmağa davam edir (bir ReportCode-un Oracle-sı sınsa, o birini
        //    bloklamasın).
        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var dir = Path.Combine(dmsRoot, "goaml-ixrac");
        var yazilanlar = new List<string>();
        var yazilaBilmeyenler = new List<string>();
        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GOAML Export — DMS qovluğu yaradıla bilmədi: {Dir}", dir);
            TempData["Error"] = $"{netice.Data.UmumiSetirSayi} qeyd ixrac edildi (statusu artıq \"İxrac edilib\"-dir), " +
                "AMMA DMS qovluğuna (C:\\FinNex_DMS\\goaml-ixrac) çıxış yoxdur. Mühəndisə bildirin, qeydlərin özü İTMƏYİB.";
            return RedirectToAction(nameof(Export));
        }

        foreach (var fayl in netice.Data.Fayllar)
        {
            try
            {
                await System.IO.File.WriteAllTextAsync(Path.Combine(dir, fayl.TeklifOlunanFaylAdi), fayl.JsonMetni, Encoding.UTF8);
                yazilanlar.Add(fayl.TeklifOlunanFaylAdi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GOAML Export — DMS-ə yazma uğursuz oldu: {Fayl}", fayl.TeklifOlunanFaylAdi);
                yazilaBilmeyenler.Add(fayl.TeklifOlunanFaylAdi);
            }
        }

        if (yazilanlar.Count == 0)
        {
            TempData["Error"] = $"{netice.Data.UmumiSetirSayi} qeyd ixrac edildi (statusu artıq \"İxrac edilib\"-dir), " +
                "AMMA HEÇ BİR fayl diskə yazıla bilmədi. Mühəndisə bildirin, qeydlərin özü İTMƏYİB.";
            return RedirectToAction(nameof(Export));
        }

        var fayllarTesviri = string.Join(", ", netice.Data.Fayllar.Select(f => $"{f.ReportCodeEtiketi} ({f.SetirSayi})"));
        var esasMetin = netice.Data.Fayllar.Count == 1
            ? $"{netice.Data.UmumiSetirSayi} qeyd ixrac edildi — \"{netice.Data.Fayllar[0].TeklifOlunanFaylAdi}\"."
            : $"{netice.Data.UmumiSetirSayi} qeyd, {netice.Data.Fayllar.Count} AYRI fayla ixrac edildi: {fayllarTesviri}.";
        TempData["Success"] = netice.Data.Xeberdarliqlar.Count > 0
            ? esasMetin + " " + string.Join(" | ", netice.Data.Xeberdarliqlar)
            : esasMetin;
        TempData["IxracFayllari"] = string.Join(";;", yazilanlar);
        if (yazilaBilmeyenler.Count > 0)
            TempData["Error"] = "Bu fayl(lar) diskə yazıla bilmədi, yalnız əl ilə yoxlanmalıdır: " + string.Join(", ", yazilaBilmeyenler);

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
