using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Kredit;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class KreditMuracietController : Controller
{
    private readonly IKreditMuracietService _muracietService;
    private readonly IKreditQerarService _qerarService;
    private readonly IKreditZaminService _zaminService;
    private readonly IKreditRandevuService _randevuService;
    private readonly IKreditSmsService _smsService;
    private readonly IKreditBaxanIsciService _baxanIsciService;
    private readonly IKomiteUzvuService _komiteService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public KreditMuracietController(
        IKreditMuracietService muracietService,
        IKreditQerarService qerarService,
        IKreditZaminService zaminService,
        IKreditRandevuService randevuService,
        IKreditSmsService smsService,
        IKreditBaxanIsciService baxanIsciService,
        IKomiteUzvuService komiteService,
        UserManager<AppUser> userManager,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _muracietService = muracietService;
        _qerarService = qerarService;
        _zaminService = zaminService;
        _randevuService = randevuService;
        _smsService = smsService;
        _baxanIsciService = baxanIsciService;
        _komiteService = komiteService;
        _userManager = userManager;
        _env = env;
        _config = config;
    }

    // ══════════════════════════════════════════════════════
    // İcazə yoxlaması
    // ══════════════════════════════════════════════════════
    // Admin/KreditAdmin — hər yerə, həm işçi həm komitə səlahiyyəti ilə
    // KreditBaxanIsci — işçi səhifələrinə (Index, Detail, MKR, AsanFinance,
    //                   Zamin, SMS, Randevu, Yenile)
    // KomiteUzvu     — komitə səhifələrinə (Komite, KomiteDetail, KomiteQerar,
    //                   Qerarlar)
    public sealed record KreditAccess(int? IsciId, bool IsAdmin, bool IsBaxan, bool IsKomite)
    {
        public bool CanSeeIsciPages => IsAdmin || IsBaxan;
        public bool CanSeeKomitePages => IsAdmin || IsKomite;
        public bool Any => IsAdmin || IsBaxan || IsKomite;
    }

    private async Task<KreditAccess> GetAccessAsync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        var isciId = appUser?.IsciId;
        var isAdmin = User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.KreditAdmin);

        bool isBaxan = false, isKomite = false;
        if (isciId.HasValue)
        {
            isBaxan = await _baxanIsciService.BaxaBilerMiAsync(isciId.Value);
            isKomite = await _komiteService.IsciKomiteUzvuMu(isciId.Value);
        }
        return new KreditAccess(isciId, isAdmin, isBaxan, isKomite);
    }

    private IActionResult Icazesiz() => Forbid();

    // ══════════════════════════════════════════════════════
    // İŞÇİ SƏHİFƏLƏRİ
    // ══════════════════════════════════════════════════════

    // GET /User/KreditMuraciet
    public async Task<IActionResult> Index(int? status)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        ViewData["Title"] = "Kredit Müraciətləri";

        var hamisi = await _muracietService.SiyahiAsync();

        var muracietler = status.HasValue
            ? hamisi.Where(x => (int)x.Status == status.Value).ToList()
            : hamisi.ToList();

        ViewBag.SecilmisStatus = status;
        ViewBag.StatusSaylari = new Dictionary<int, int>
        {
            [0] = hamisi.Count(x => x.Status == KreditMuracietStatus.Yeni),
            [1] = hamisi.Count(x => x.Status == KreditMuracietStatus.Yoxlanilir),
            [2] = hamisi.Count(x => x.Status == KreditMuracietStatus.KomiteyeGonderildi),
            [3] = hamisi.Count(x => x.Status == KreditMuracietStatus.Tesdiqlenib),
            [4] = hamisi.Count(x => x.Status == KreditMuracietStatus.ReddEdilib)
        };
        ViewBag.Erisim = erisim;

        return View(muracietler);
    }

    // GET /User/KreditMuraciet/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        ViewData["Title"] = "Yeni Müraciət";
        return View();
    }

    // POST /User/KreditMuraciet/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string adSoyadAtaAdi, string? fin, string? telefon,
        string? isYeri, decimal? emekHaqqi, decimal? kreditMeblegi, string? valyuta,
        string? kreditMuddeti, string? meqsed)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        if (string.IsNullOrWhiteSpace(adSoyadAtaAdi))
            adSoyadAtaAdi = "Naməlum";

        var muraciet = new KreditMuraciet
        {
            AdSoyadAtaAdi = adSoyadAtaAdi.Trim(),
            FIN = fin?.Trim(),
            Telefon = telefon?.Trim(),
            IsYeri = isYeri?.Trim(),
            EmekHaqqi = emekHaqqi,
            KreditMeblegi = kreditMeblegi ?? 0,
            Valyuta = string.IsNullOrWhiteSpace(valyuta) ? "AZN" : valyuta.Trim(),
            KreditMuddeti = kreditMuddeti?.Trim(),
            Meqsed = meqsed?.Trim(),
            MuracietTarixi = DateTime.Now,
            Menbe = KreditMuracietMenbe.Filial
        };

        await _muracietService.YaratAsync(muraciet, erisim.IsciId);

        TempData["Success"] = "Müraciət əlavə edildi.";
        return RedirectToAction("Detail", new { id = muraciet.Id });
    }

    // GET /User/KreditMuraciet/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        var muraciet = await _muracietService.IdIleGetirAsync(id);
        if (muraciet == null) return NotFound();

        ViewBag.Tarixce = await _muracietService.FinUzreTarixceAsync(muraciet.FIN, id);
        ViewBag.Zaminler = await _zaminService.MuracietZaminleriniGetirAsync(id);
        ViewBag.SmsLoglar = await _smsService.MuracietUzreGetirAsync(id);
        ViewBag.Erisim = erisim;

        // Mail-i arxa planda oxunmuş et
        if (!string.IsNullOrEmpty(muraciet.MailMessageId))
        {
            var messageId = muraciet.MailMessageId;
            _ = Task.Run(() => MarkMailAsReadAsync(messageId));
        }

        ViewData["Title"] = "Müraciət #" + id;
        return View(muraciet);
    }

    // POST /User/KreditMuraciet/IsciQiymetlendir
    // yeniStatus: 1 = Yoxlanılır (baxmağa götür), 2 = Komitəyə göndər
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IsciQiymetlendir(int id, int yeniStatus, string? qeyd)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();
        if (erisim.IsciId is null) { TempData["Error"] = "İşçi məlumatı tapılmadı."; return RedirectToAction("Detail", new { id }); }

        try
        {
            if (yeniStatus == (int)KreditMuracietStatus.Yoxlanilir)
            {
                await _muracietService.BaxilmagaGoturAsync(id, erisim.IsciId.Value, qeyd);
                TempData["Success"] = "Status yeniləndi.";
            }
            else if (yeniStatus == (int)KreditMuracietStatus.KomiteyeGonderildi)
            {
                await _muracietService.KomiteyeGonderAsync(id, erisim.IsciId.Value, qeyd);
                TempData["Success"] = "Müraciət Kredit Komitəsinə göndərildi.";
            }
            else
            {
                TempData["Error"] = "Bu status keçidi İşçi üçün icazəli deyil.";
            }
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }

        return RedirectToAction("Detail", new { id });
    }

    // POST /User/KreditMuraciet/MkrYaz
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MkrYaz(int id, string netice)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();
        if (erisim.IsciId is null) { TempData["Error"] = "İşçi məlumatı tapılmadı."; return RedirectToAction("Detail", new { id }); }
        if (string.IsNullOrWhiteSpace(netice)) { TempData["Error"] = "MKR nəticəsi boş ola bilməz."; return RedirectToAction("Detail", new { id }); }

        try
        {
            await _muracietService.MkrNeticesiYazAsync(id, netice.Trim(), erisim.IsciId.Value);
            TempData["Success"] = "MKR nəticəsi qeydə alındı.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }

        return RedirectToAction("Detail", new { id });
    }

    // POST /User/KreditMuraciet/AsanFinanceYaz
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsanFinanceYaz(int id, string netice)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();
        if (erisim.IsciId is null) { TempData["Error"] = "İşçi məlumatı tapılmadı."; return RedirectToAction("Detail", new { id }); }
        if (string.IsNullOrWhiteSpace(netice)) { TempData["Error"] = "AsanFinance nəticəsi boş ola bilməz."; return RedirectToAction("Detail", new { id }); }

        try
        {
            await _muracietService.AsanFinanceNeticesiYazAsync(id, netice.Trim(), erisim.IsciId.Value);
            TempData["Success"] = "AsanFinance nəticəsi qeydə alındı.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }

        return RedirectToAction("Detail", new { id });
    }

    // POST /User/KreditMuraciet/ZaminEkle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ZaminEkle(int muracietId, string adSoyadAtaAdi, string? fin,
        string? telefon, string? isYeri, decimal? emekHaqqi, string? unvan, string? qeyd)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        if (string.IsNullOrWhiteSpace(adSoyadAtaAdi))
        {
            TempData["Error"] = "Zaminin ad-soyadı boş ola bilməz.";
            return RedirectToAction("Detail", new { id = muracietId });
        }

        var zamin = new KreditZamin
        {
            KreditMuracietId = muracietId,
            AdSoyadAtaAdi = adSoyadAtaAdi.Trim(),
            FIN = fin?.Trim(),
            Telefon = telefon?.Trim(),
            IsYeri = isYeri?.Trim(),
            EmekHaqqi = emekHaqqi,
            Unvan = unvan?.Trim(),
            Qeyd = qeyd?.Trim()
        };
        await _zaminService.YaratAsync(zamin, erisim.IsciId);

        TempData["Success"] = "Zamin əlavə edildi.";
        return RedirectToAction("Detail", new { id = muracietId });
    }

    // POST /User/KreditMuraciet/ZaminSil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ZaminSil(int id, int muracietId)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        await _zaminService.SilAsync(id, erisim.IsciId);
        TempData["Success"] = "Zamin silindi.";
        return RedirectToAction("Detail", new { id = muracietId });
    }

    // GET /User/KreditMuraciet/ZaminTarixce
    public async Task<IActionResult> ZaminTarixce(string fin, int? muracietId)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        var siyahi = await _zaminService.FinUzreTarixceAsync(fin ?? "", muracietId);
        return PartialView("_ZaminTarixce", siyahi);
    }

    // POST /User/KreditMuraciet/SmsGonder
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SmsGonder(int muracietId, string telefon, string metn, string? sablon)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();
        if (erisim.IsciId is null) { TempData["Error"] = "İşçi məlumatı tapılmadı."; return RedirectToAction("Detail", new { id = muracietId }); }

        if (string.IsNullOrWhiteSpace(telefon) || string.IsNullOrWhiteSpace(metn))
        {
            TempData["Error"] = "Telefon və SMS mətni tələb olunur.";
            return RedirectToAction("Detail", new { id = muracietId });
        }

        await _smsService.GonderAsync(muracietId, telefon.Trim(), metn.Trim(), sablon, erisim.IsciId.Value);
        TempData["Success"] = "SMS göndərmə sorğusu qeydə alındı.";
        return RedirectToAction("Detail", new { id = muracietId });
    }

    // ══════════════════════════════════════════════════════
    // KREDİT KOMİTƏSİ
    // ══════════════════════════════════════════════════════

    // GET /User/KreditMuraciet/Komite
    public async Task<IActionResult> Komite()
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeKomitePages) return Icazesiz();

        ViewData["Title"] = "Kredit Komitəsi";
        var muracietler = await _muracietService.SiyahiAsync(status: KreditMuracietStatus.KomiteyeGonderildi);
        ViewBag.Erisim = erisim;
        return View(muracietler);
    }

    // GET /User/KreditMuraciet/KomiteDetail/5
    public async Task<IActionResult> KomiteDetail(int id)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeKomitePages) return Icazesiz();

        var muraciet = await _muracietService.IdIleGetirAsync(id);
        if (muraciet == null) return NotFound();

        ViewBag.Tarixce = await _muracietService.FinUzreTarixceAsync(muraciet.FIN, id);
        ViewBag.Zaminler = await _zaminService.MuracietZaminleriniGetirAsync(id);
        ViewBag.Qerar = await _qerarService.MuracietUzreGetirAsync(id);
        ViewBag.KomiteUzvleri = await _komiteService.HamisiniGetirAsync(yalnizAktiv: true);
        ViewBag.Erisim = erisim;

        ViewData["Title"] = "Komitə — Müraciət #" + id;
        return View(muraciet);
    }

    // POST /User/KreditMuraciet/KomiteQerar
    // Komitə offline yığışır → Word protokol imzalanıb skan edilir → bura upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
    public async Task<IActionResult> KomiteQerar(int muracietId, int qerarTipi, string protokolNo,
        DateTime qerarTarixi, decimal? tesdiqMebleg, string? tesdiqMuddet, decimal? faizDerecesi,
        string? teminat, string? qeyd, List<int> imzalayanlar, IFormFile? protokolFayl)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeKomitePages) return Icazesiz();
        if (erisim.IsciId is null)
        {
            TempData["Error"] = "İşçi məlumatı tapılmadı.";
            return RedirectToAction("KomiteDetail", new { id = muracietId });
        }

        if (qerarTipi != (int)KreditQerarTipi.Tesdiqlenib && qerarTipi != (int)KreditQerarTipi.ReddEdilib)
        {
            TempData["Error"] = "Qərar tipi düzgün deyil.";
            return RedirectToAction("KomiteDetail", new { id = muracietId });
        }
        if (string.IsNullOrWhiteSpace(protokolNo))
        {
            TempData["Error"] = "Protokol nömrəsi tələb olunur.";
            return RedirectToAction("KomiteDetail", new { id = muracietId });
        }
        if (imzalayanlar == null || imzalayanlar.Count == 0)
        {
            TempData["Error"] = "Ən azı bir imzalayan üzv seçilməlidir.";
            return RedirectToAction("KomiteDetail", new { id = muracietId });
        }

        // Faylı saxla
        string? faylAdi = null;
        string? faylYolu = null;
        if (protokolFayl != null && protokolFayl.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "Files", "Kredit", "Qerarlar");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(protokolFayl.FileName);
            faylAdi = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsDir, faylAdi);
            using var stream = new FileStream(fullPath, FileMode.Create);
            await protokolFayl.CopyToAsync(stream);
            faylYolu = $"/Files/Kredit/Qerarlar/{faylAdi}";
        }

        var qerar = new KreditQerar
        {
            KreditMuracietId = muracietId,
            Qerar = (KreditQerarTipi)qerarTipi,
            ProtokolNo = protokolNo.Trim(),
            QerarTarixi = qerarTarixi == default ? DateTime.Now : qerarTarixi,
            ProtokolFaylAdi = protokolFayl?.FileName,
            ProtokolFaylYolu = faylYolu,
            TesdiqMebleg = qerarTipi == (int)KreditQerarTipi.Tesdiqlenib ? tesdiqMebleg : null,
            TesdiqMuddet = qerarTipi == (int)KreditQerarTipi.Tesdiqlenib ? tesdiqMuddet?.Trim() : null,
            FaizDerecesi = qerarTipi == (int)KreditQerarTipi.Tesdiqlenib ? faizDerecesi : null,
            Teminat = qerarTipi == (int)KreditQerarTipi.Tesdiqlenib ? teminat?.Trim() : null,
            Qeyd = qeyd?.Trim()
        };

        try
        {
            await _qerarService.QebulEtAsync(qerar, imzalayanlar, erisim.IsciId.Value);
            TempData["Success"] = "Komitə qərarı qeydə alındı.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }

        return RedirectToAction("Komite");
    }

    // GET /User/KreditMuraciet/Qerarlar
    public async Task<IActionResult> Qerarlar(int? tip)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeKomitePages && !erisim.CanSeeIsciPages) return Icazesiz();

        ViewData["Title"] = "Komitə Qərarları";
        KreditQerarTipi? filter = tip.HasValue ? (KreditQerarTipi)tip.Value : null;
        var qerarlar = await _qerarService.HamisiniGetirAsync(filter);
        ViewBag.SecilmisTip = tip;
        ViewBag.Erisim = erisim;
        return View(qerarlar);
    }

    // ══════════════════════════════════════════════════════
    // RANDEVU
    // ══════════════════════════════════════════════════════

    // GET /User/KreditMuraciet/Randevular?gun=2026-04-22
    public async Task<IActionResult> Randevular(DateTime? gun)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        var secilenGun = gun?.Date ?? DateTime.Today;
        ViewData["Title"] = "Randevular — " + secilenGun.ToString("dd MMM yyyy");

        var randevular = await _randevuService.GunUzreGetirAsync(secilenGun);

        // Həftəlik sayım (calendar zolaq üçün)
        var hefteBaslangic = secilenGun.AddDays(-(int)secilenGun.DayOfWeek + 1);
        if (secilenGun.DayOfWeek == DayOfWeek.Sunday) hefteBaslangic = secilenGun.AddDays(-6);
        var hefteSon = hefteBaslangic.AddDays(6);
        var hefteRandevular = await _randevuService.AraliqUzreGetirAsync(hefteBaslangic, hefteSon);

        ViewBag.SecilenGun = secilenGun;
        ViewBag.HefteBaslangic = hefteBaslangic;
        ViewBag.HefteRandevular = hefteRandevular;
        ViewBag.Erisim = erisim;
        return View(randevular);
    }

    // POST /User/KreditMuraciet/RandevuTeyin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RandevuTeyin(int muracietId, DateTime tarix, int? muddetDeqiqe, string? qeyd)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();
        if (erisim.IsciId is null)
        {
            TempData["Error"] = "İşçi məlumatı tapılmadı.";
            return RedirectToAction("Detail", new { id = muracietId });
        }

        try
        {
            await _randevuService.TeyinEtAsync(muracietId, tarix, muddetDeqiqe ?? 30, qeyd, erisim.IsciId.Value);
            TempData["Success"] = "Randevu təyin edildi.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }

        return RedirectToAction("Detail", new { id = muracietId });
    }

    // POST /User/KreditMuraciet/RandevuDurumDeyis
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RandevuDurumDeyis(int randevuId, int yeniDurum, DateTime? gun)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        await _randevuService.DurumDeyisAsync(randevuId, (KreditRandevuStatus)yeniDurum, erisim.IsciId);
        TempData["Success"] = "Randevu statusu yeniləndi.";
        return RedirectToAction("Randevular", new { gun });
    }

    // POST /User/KreditMuraciet/RandevuLegv
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RandevuLegv(int randevuId, DateTime? gun)
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        await _randevuService.LegvEtAsync(randevuId, erisim.IsciId);
        TempData["Success"] = "Randevu ləğv edildi.";
        return RedirectToAction("Randevular", new { gun });
    }

    // ══════════════════════════════════════════════════════
    // SİLMƏ
    // ══════════════════════════════════════════════════════

    // POST /User/KreditMuraciet/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var erisim = await GetAccessAsync();
        // Yalnız admin silə bilər
        if (!erisim.IsAdmin) return Icazesiz();

        await _muracietService.SilAsync(id, erisim.IsciId);
        TempData["Success"] = "Müraciət silindi.";
        return RedirectToAction("Index");
    }

    // ══════════════════════════════════════════════════════
    // MAİL — IMAP-dan yeni müraciət gətir
    // ══════════════════════════════════════════════════════

    // POST /User/KreditMuraciet/Yenile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yenile()
    {
        var erisim = await GetAccessAsync();
        if (!erisim.CanSeeIsciPages) return Icazesiz();

        try
        {
            var server = _config["KreditMail:ImapServer"] ?? "imap.titan.email";
            var port = _config.GetValue("KreditMail:Port", 993);
            var email = _config["KreditMail:Email"] ?? "";
            var password = _config["KreditMail:Password"] ?? "";

            if (string.IsNullOrEmpty(password) || password == "PAROL_BURA_YAZIN")
            {
                TempData["Error"] = "Mail parolu konfiqurasiya olunmayıb.";
                return RedirectToAction("Index");
            }

            using var client = new ImapClient();
            await client.ConnectAsync(server, port, true);
            await client.AuthenticateAsync(email, password);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var uids = await inbox.SearchAsync(
                SearchQuery.SubjectContains("Online Kredit").And(SearchQuery.NotSeen));

            int yeniSay = 0;

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid);
                var messageId = message.MessageId ?? uid.ToString();

                if (await _muracietService.MailMessageIdVarsa(messageId)) continue;

                var body = message.TextBody;
                if (string.IsNullOrWhiteSpace(body)) body = message.HtmlBody ?? "";
                body = CleanHtml(body);

                var muraciet = ParseMailBody(body, messageId);
                if (muraciet != null)
                {
                    await _muracietService.YaratAsync(muraciet, erisim.IsciId);
                    yeniSay++;
                }
            }

            await client.DisconnectAsync(true);

            TempData["Success"] = yeniSay > 0
                ? $"{yeniSay} yeni müraciət gətirildi."
                : "Yeni müraciət yoxdur.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Mail xətası: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    private async Task MarkMailAsReadAsync(string messageId)
    {
        try
        {
            var server = _config["KreditMail:ImapServer"] ?? "imap.titan.email";
            var port = _config.GetValue("KreditMail:Port", 993);
            var email = _config["KreditMail:Email"] ?? "";
            var password = _config["KreditMail:Password"] ?? "";

            if (string.IsNullOrEmpty(password) || password == "PAROL_BURA_YAZIN") return;

            using var client = new ImapClient();
            await client.ConnectAsync(server, port, true);
            await client.AuthenticateAsync(email, password);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var uids = await inbox.SearchAsync(SearchQuery.SubjectContains("Online Kredit"));
            foreach (var uid in uids)
            {
                var msg = await inbox.GetMessageAsync(uid);
                var msgId = msg.MessageId ?? "";
                if (msgId == messageId || uid.ToString() == messageId)
                {
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                    break;
                }
            }

            await client.DisconnectAsync(true);
        }
        catch { }
    }

    // ══════════════════════════════════════════════════════
    // PARSER
    // ══════════════════════════════════════════════════════

    private static string CleanHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<br\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"</(p|div|tr|li)>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n+", "\n");
        return text.Trim();
    }

    private static KreditMuraciet? ParseMailBody(string body, string messageId)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        var lines = body.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx > 0 && colonIdx < line.Length - 1)
            {
                var key = line[..colonIdx].Trim();
                var val = line[(colonIdx + 1)..].Trim();
                if (!string.IsNullOrEmpty(val) && val.Length < 300)
                    fields[key] = val;
            }
        }

        if (fields.Count < 2) return null;

        var m = new KreditMuraciet
        {
            MailMessageId = messageId,
            MuracietTarixi = DateTime.Now,
            AdSoyadAtaAdi = FindField(fields, "Soyad") ?? FindField(fields, "ad v") ?? "Naməlum",
            FIN = FindField(fields, "FİN") ?? FindField(fields, "FIN"),
            Valyuta = FindField(fields, "valyuta") ?? "AZN",
            KreditMuddeti = FindField(fields, "müddət") ?? FindField(fields, "muddet"),
            IsYeri = FindField(fields, "İş yer") ?? FindField(fields, "Is yer") ?? FindField(fields, "yeriniz"),
            Telefon = FindField(fields, "telefon"),
            Meqsed = FindField(fields, "məqsəd") ?? FindField(fields, "meqsed"),
            IP = FindField(fields, "Remote IP"),
            Menbe = KreditMuracietMenbe.Online
        };

        var meblegStr = FindField(fields, "kredit məbləğ") ?? FindField(fields, "kredit mebleq");
        if (meblegStr != null)
        {
            var numStr = new string(meblegStr.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (decimal.TryParse(numStr.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var mebleg))
                m.KreditMeblegi = mebleg;
        }

        var haqqiStr = FindField(fields, "əmək haqq") ?? FindField(fields, "emek haqq");
        if (haqqiStr != null)
        {
            var numStr = new string(haqqiStr.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (decimal.TryParse(numStr.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var haqqi))
                m.EmekHaqqi = haqqi;
        }

        var dateStr = FindField(fields, "Date");
        var timeStr = FindField(fields, "Time");
        if (!string.IsNullOrEmpty(dateStr))
        {
            if (DateTime.TryParse(dateStr + " " + (timeStr ?? ""),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
                m.MuracietTarixi = dt;
        }

        return m;
    }

    private static string? FindField(Dictionary<string, string> fields, string search)
    {
        search = search.ToLowerInvariant();
        foreach (var kv in fields)
        {
            if (kv.Key.ToLowerInvariant().Contains(search))
                return kv.Value;
        }
        return null;
    }
}
