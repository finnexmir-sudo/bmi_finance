using FinNex.Application.Interfaces.Communication;
using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FinNex.UI.Areas.HR.Controllers;

[Route("iclock")]
public class ADMSController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<ADMSController> _logger;
    private readonly IJetonTeklifleriService _teklifService;
    private readonly IBildirisRouter _bildirisRouter;

    public static DateTime? SonElaqa { get; private set; }
    public static string? SonSN { get; private set; }

    public ADMSController(AppDbContext db, ILogger<ADMSController> logger, IJetonTeklifleriService teklifService, IBildirisRouter bildirisRouter)
    {
        _db = db;
        _logger = logger;
        _teklifService = teklifService;
        _bildirisRouter = bildirisRouter;
    }

    [HttpGet("cdata")]
    public IActionResult Handshake(string? SN)
    {
        var allParams = string.Join("&", Request.Query.Select(q => $"{q.Key}={q.Value}"));
        _logger.LogWarning("=== ADMS GET /iclock/cdata === IP: {IP}, Query: {Query}, SN: {SN}",
            HttpContext.Connection.RemoteIpAddress, allParams, SN ?? "NULL");

        SonElaqa = DateTime.Now;
        SonSN = SN ?? "unknown";

        var sn = SN ?? "unknown";
        var response = $"GET OPTION FROM: {sn}\n" +
                       "ATTLOGStamp=None\n" +
                       "OPERLOGStamp=None\n" +
                       "ATTPHOTOStamp=None\n" +
                       "ErrorDelay=30\n" +
                       "Delay=10\n" +
                       "TransTimes=00:00;14:05\n" +
                       "TransInterval=1\n" +
                       "TransFlag=TransData AttLog\tOpLog\tEnrollUser\tChgUser\tEnrollFP\tChgFP\tFACE\tUserPic\n" +
                       "TimeZone=4\n" +
                       "Realtime=1\n" +
                       "Encrypt=None\n";

        return Content(response, "text/plain");
    }

    [HttpPost("cdata")]
    public async Task<IActionResult> ReceiveAttendance(string? SN, string? table)
    {
        using var reader = new StreamReader(Request.Body);
        string body = await reader.ReadToEndAsync();

        var allParams = string.Join("&", Request.Query.Select(q => $"{q.Key}={q.Value}"));
        _logger.LogWarning("=== ADMS POST /iclock/cdata === IP: {IP}, Query: {Query}, Table: {Table}, Body: {Body}",
            HttpContext.Connection.RemoteIpAddress, allParams, table, body);

        SonElaqa = DateTime.Now;
        SonSN = SN ?? "unknown";

        if (string.IsNullOrWhiteSpace(body) || table != "ATTLOG")
            return Content("OK", "text/plain");

        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            await ProcessAttLogLineAsync(line.Trim());
        }

        return Content("OK", "text/plain");
    }

    [HttpGet("getrequest")]
    public IActionResult GetRequest(string? SN)
    {
        _logger.LogWarning("=== ADMS GET /iclock/getrequest === IP: {IP}, SN: {SN}",
            HttpContext.Connection.RemoteIpAddress, SN ?? "NULL");

        SonElaqa = DateTime.Now;
        SonSN = SN ?? "unknown";
        return Content("OK", "text/plain");
    }

    [HttpPost("devicecmd")]
    public IActionResult DeviceCmd(string? SN)
    {
        _logger.LogWarning("=== ADMS POST /iclock/devicecmd === IP: {IP}, SN: {SN}",
            HttpContext.Connection.RemoteIpAddress, SN ?? "NULL");
        return Content("OK", "text/plain");
    }

    [HttpGet("{**path}")]
    [HttpPost("{**path}")]
    public IActionResult CatchAll(string? path)
    {
        var allParams = string.Join("&", Request.Query.Select(q => $"{q.Key}={q.Value}"));
        _logger.LogWarning("=== ADMS CATCH-ALL === Method: {Method}, Path: /iclock/{Path}, IP: {IP}, Query: {Query}",
            Request.Method, path, HttpContext.Connection.RemoteIpAddress, allParams);

        SonElaqa = DateTime.Now;
        return Content("OK", "text/plain");
    }

    private async Task ProcessAttLogLineAsync(string line)
    {
        try
        {
            var parts = line.Split('\t');
            if (parts.Length < 3) return;

            if (!int.TryParse(parts[0].Trim(), out int isciId)) return;
            if (!DateTime.TryParse(parts[1].Trim(), out DateTime vaxt)) return;

            int nov = parts.Length > 2 && int.TryParse(parts[2].Trim(), out int n) ? n : 0;
            bool girisdir = nov == 0 || nov == 4;

            var tarix = vaxt.Date;

            // İş parametrləri — gecikme toleransı + erkən çıxış
            TimeSpan standartGiris;
            TimeSpan gecikTolerans;
            TimeSpan standartCixis;
            TimeSpan tezCixmaTolerans;
            try
            {
                var parametri = await _db.Set<IsParametri>()
                    .Where(x => !x.Silinib).FirstOrDefaultAsync();
                standartGiris     = parametri?.StandartGirisVaxti  ?? new TimeSpan(9, 0, 0);
                gecikTolerans     = TimeSpan.FromMinutes(parametri?.GecikmeToleransDeqiqe   ?? 5);
                standartCixis     = parametri?.StandartCixisVaxti  ?? new TimeSpan(17, 45, 0);
                tezCixmaTolerans  = TimeSpan.FromMinutes(parametri?.TezCixmaToleransDeqiqe ?? 15);
            }
            catch
            {
                standartGiris    = new TimeSpan(9, 0, 0);
                gecikTolerans    = TimeSpan.FromMinutes(5);
                standartCixis    = new TimeSpan(17, 45, 0);
                tezCixmaTolerans = TimeSpan.FromMinutes(15);
            }

            // Bu gün üçün təsdiqlənmiş İcazə varmı?
            var bugunIcaze = await _db.Icazeler
                .Where(x => x.IsciId == isciId && !x.Silinib &&
                             x.Status == IcazeStatus.Tesdiqlenib &&
                             x.IcazeTarixi.Date == tarix)
                .FirstOrDefaultAsync();

            // Bu gün üçün təsdiqlənmiş Ezamiyyət varmı?
            var bugunEzamiyyet = await _db.EzamiyyetMuracietler
                .Where(x => x.IsciId == isciId && !x.Silinib &&
                             x.Status == EzamiyyetStatus.Tesdiqlendi &&
                             x.BaslamaTarixi.Date <= tarix &&
                             x.BitmeTarixi.Date   >= tarix)
                .FirstOrDefaultAsync();

            // Həmin işçi+tarix üçün mövcud davamiyyət qeydini tap
            var movcud = await _db.Davamiyyetler
                .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Tarix == tarix);

            // İş başlama vaxtı — 9:00. Bu vaxta qədər hər yeni oxuma "duplikat giriş
            // cəhdi" kimi qəbul olunur (işçi qayıdıb ikinci dəfə barmağını basa bilər
            // və sistem bunu çıxış kimi yazmamalıdır). 9:00-dan sonra sonrakı oxumalar
            // normal çıxış kimi tutulur.
            var isBaslamaVaxti = tarix.AddHours(9);

            int? davamiyyetId = null;

            if (movcud == null)
            {
                var yeni = new Davamiyyet
                {
                    IsciId = isciId,
                    Tarix = tarix,
                    GirisVaxti = vaxt,
                    CixisVaxti = null,
                    Status = HesablaStatus(vaxt, bugunIcaze, bugunEzamiyyet, standartGiris, gecikTolerans)
                };
                await _db.Davamiyyetler.AddAsync(yeni);
                await _db.SaveChangesAsync();
                davamiyyetId = yeni.Id;
            }
            else
            {
                if (movcud.GirisVaxti == null || vaxt < movcud.GirisVaxti)
                {
                    // Giriş yazılmayıb və ya bu oxuma əvvəlkindən ERKƏNdir → girişi yenilə
                    movcud.GirisVaxti = vaxt;
                    movcud.Status = HesablaStatus(vaxt, bugunIcaze, bugunEzamiyyet, standartGiris, gecikTolerans);
                    await _db.SaveChangesAsync();
                    davamiyyetId = movcud.Id;
                }
                else if (vaxt < isBaslamaVaxti)
                {
                    // 9:00-dan əvvəl və mövcud girişdən sonradır → **duplikat giriş cəhdi**.
                    // Çıxış kimi qeydə alma, ilk giriş saxlanılsın.
                    _logger.LogInformation(
                        "Duplikat giriş cəhdi ignor edildi: IsciId={IsciId}, İlk giriş={Ilk}, Yeni oxuma={Yeni}",
                        isciId, movcud.GirisVaxti, vaxt);
                }
                else if (vaxt > movcud.GirisVaxti)
                {
                    // 9:00-dan sonra və girişdən sonradır → əsl çıxış
                    if (movcud.CixisVaxti == null || vaxt > movcud.CixisVaxti)
                    {
                        movcud.CixisVaxti = vaxt;
                        await _db.SaveChangesAsync();
                        davamiyyetId = movcud.Id;

                        // Erkən çıxış bildirişi
                        // Rəhbər "İş Bitdi" vurubsa → elan vaxtı hədd olur, yoxsa standart-tolerans
                        var bugunElan = await _db.Set<IsGunuBitdiElan>()
                            .Where(x => x.Tarix.Date == tarix)
                            .FirstOrDefaultAsync();
                        var tezCixmaHeddi = bugunElan != null
                            ? bugunElan.BitisVaxti
                            : standartCixis - tezCixmaTolerans;

                        // Ezamiyyət çıxışı? — BaslamaSaati ±30 dəq., çıxışı qeydə al
                        bool ezamiyyetCixisi = await QeydEzamiyyetCixisiAsync(
                            isciId, vaxt, tarix, bugunEzamiyyet);

                        // Görüş çıxışı? — offline görüşün başlama saatı ±30 dəq.
                        bool gorushCixisi = false;
                        if (!ezamiyyetCixisi && vaxt.TimeOfDay < tezCixmaHeddi)
                        {
                            gorushCixisi = await QeydGorushCixisiAsync(isciId, vaxt, tarix);
                        }

                        // Rəhbər erkən çıxış icazəsi verilibmi?
                        bool rehberIcazesi = await _db.Set<ErkenCixisIcaze>()
                            .AnyAsync(x => x.IsciId == isciId && x.Tarix == tarix);

                        if (vaxt.TimeOfDay < tezCixmaHeddi && !ezamiyyetCixisi && !gorushCixisi && !rehberIcazesi)
                        {
                            var qalan = (int)(tezCixmaHeddi - vaxt.TimeOfDay).TotalMinutes;
                            var heddStr = $"{(int)tezCixmaHeddi.TotalHours:D2}:{tezCixmaHeddi.Minutes:D2}";
                            var elanQeyd = bugunElan != null ? " (rəhbər elanına görə)" : "";
                            _ = _bildirisRouter.NotifyIsciAsync(
                                isciId,
                                BildirisNovu.TezCixma,
                                "Erkən çıxış qeydə alındı",
                                $"Siz iş vaxtından{elanQeyd} {qalan} dəqiqə tez çıxdınız. " +
                                $"Çıxış: {vaxt:HH:mm}, İş vaxtı: {heddStr}.");
                        }
                    }
                }
            }

            _logger.LogInformation(
                "Davamiyyət yazıldı: IsciId={IsciId}, Tarix={Tarix}, {Nov}={Vaxt}",
                isciId, tarix, girisdir ? "Giriş" : "Çıxış", vaxt);

            // Paralel: İcazə çıxış/qayıdış izlənməsi
            await ProcessIcazeCixisGirisAsync(isciId, vaxt);

            // Paralel: Görüş qayıdışı izlənməsi
            await ProcessGorushQayidisAsync(isciId, vaxt);

            // Paralel: Ezamiyyət qayıdışı izlənməsi
            await ProcessEzamiyyetQayidisAsync(isciId, vaxt);

            // Jeton tövsiyəsi yoxlaması (gecikme, iş norması aşma)
            if (davamiyyetId.HasValue)
                await _teklifService.DavamiyyetYoxlaAsync(davamiyyetId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AttLog parse xətası: {Line}", line);
        }
    }

    // Ezamiyyət çıxışını qeydə alır. BaslamaSaati ±30 dəq. aralığında çıxış olarsa
    // CihazCixisVaxti yazır və true qaytarır → Tez Çıxış bildirişi göndərilmir.
    private async Task<bool> QeydEzamiyyetCixisiAsync(
        int isciId, DateTime vaxt, DateTime tarix, EzamiyyetMuraciet? bugunEzamiyyet)
    {
        try
        {
            if (bugunEzamiyyet?.BaslamaSaati == null) return false;
            if (bugunEzamiyyet.CihazCixisVaxti != null) return true; // artıq qeydə alınıb

            var diff = Math.Abs((vaxt.TimeOfDay - bugunEzamiyyet.BaslamaSaati.Value).TotalMinutes);
            if (diff > 30) return false;

            bugunEzamiyyet.CihazCixisVaxti = vaxt;
            _db.Update(bugunEzamiyyet);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Ezamiyyət çıxışı: IsciId={IsciId}, EzamId={EzamId}, Vaxt={Vaxt}",
                isciId, bugunEzamiyyet.Id, vaxt);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ezamiyyət çıxış qeyd xətası: IsciId={IsciId}", isciId);
            return false;
        }
    }

    // Ezamiyyətdən qayıdışı qeydə alır.
    // CihazCixisVaxti var, CihazQayidisVaxti yoxdursa və skan ≥5 dəq. sonradırsa → yazır.
    private async Task ProcessEzamiyyetQayidisAsync(int isciId, DateTime vaxt)
    {
        try
        {
            var tarix = vaxt.Date;

            var ezam = await _db.EzamiyyetMuracietler
                .Where(x => x.IsciId == isciId
                         && !x.Silinib
                         && x.Status == EzamiyyetStatus.Tesdiqlendi
                         && x.BaslamaTarixi.Date <= tarix
                         && x.BitmeTarixi.Date   >= tarix
                         && x.CihazCixisVaxti    != null
                         && x.CihazQayidisVaxti  == null)
                .FirstOrDefaultAsync();

            if (ezam == null) return;

            if (vaxt < ezam.CihazCixisVaxti!.Value.AddMinutes(5)) return;

            ezam.CihazQayidisVaxti = vaxt;
            _db.Update(ezam);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Ezamiyyətdən qayıdış: IsciId={IsciId}, EzamId={EzamId}, Vaxt={Vaxt}",
                isciId, ezam.Id, vaxt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ezamiyyət qayıdış izlənmə xətası: IsciId={IsciId}", isciId);
        }
    }

    // Görüş çıxışını qeydə alır. Əgər işçinin bu gün offline görüşü varsa
    // və çıxış vaxtı görüşün başlama saatına ±30 dəq yaxındırsa → CihazCixisVaxti yazır.
    // True qaytarır — erkən çıxış bildirişi göndərilməsin.
    private async Task<bool> QeydGorushCixisiAsync(int isciId, DateTime vaxt, DateTime tarix)
    {
        try
        {
            var baslamadan = vaxt.TimeOfDay - TimeSpan.FromMinutes(30);
            var baslamaya  = vaxt.TimeOfDay + TimeSpan.FromMinutes(30);

            var gi = await _db.GorushIshtirakcilar
                .Include(x => x.Gorush)
                .Where(x => x.IsciId == isciId
                         && !x.Silinib
                         && x.Gorush.Tarix.Date == tarix
                         && x.Gorush.Nov == GorushNovu.Offline
                         && x.Gorush.Status != GorushStatus.LegvEdildi
                         && x.CihazCixisVaxti == null
                         && x.Status != IshtirakciStatus.Redd
                         && x.Status != IshtirakciStatus.IshtiraketmeyecekBildirib
                         && x.Gorush.BaslamaSaati >= baslamadan
                         && x.Gorush.BaslamaSaati <= baslamaya)
                .FirstOrDefaultAsync();

            if (gi == null) return false;

            gi.CihazCixisVaxti = vaxt;
            _db.Update(gi);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Görüş çıxışı: IsciId={IsciId}, GorushId={GorushId}, Vaxt={Vaxt}",
                isciId, gi.GorushId, vaxt);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görüş çıxış qeyd xətası: IsciId={IsciId}", isciId);
            return false;
        }
    }

    // İşçinin açıq görüş çıxışı varsa (görüşə gedib, hələ qayıtmayıb) qayıdışı qeydə alır.
    // Skan çıxışdan ən az 5 dəq. sonra olmalıdır ki duplikat skan sayılmasın.
    private async Task ProcessGorushQayidisAsync(int isciId, DateTime vaxt)
    {
        try
        {
            var tarix = vaxt.Date;

            var gi = await _db.GorushIshtirakcilar
                .Where(x => x.IsciId == isciId
                         && !x.Silinib
                         && x.Gorush.Tarix.Date == tarix
                         && x.CihazCixisVaxti != null
                         && x.CihazQayidisVaxti == null)
                .Include(x => x.Gorush)
                .FirstOrDefaultAsync();

            if (gi == null) return;

            // Duplikat skan qoruması
            if (vaxt < gi.CihazCixisVaxti!.Value.AddMinutes(5)) return;

            gi.CihazQayidisVaxti = vaxt;
            _db.Update(gi);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Görüşdən qayıdış: IsciId={IsciId}, GorushId={GorushId}, Vaxt={Vaxt}",
                isciId, gi.GorushId, vaxt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görüş qayıdış izlənmə xətası: IsciId={IsciId}", isciId);
        }
    }

    // İşçinin aktiv təsdiqlənmiş icazəsi varsa çıxış/qayıdış vaxtını qeyd edir.
    // Davamiyyət məntiqi ilə toqquşmur — həmin skan paralel olaraq hər iki cədvələ yazılır.
    private async Task ProcessIcazeCixisGirisAsync(int isciId, DateTime vaxt)
    {
        try
        {
            var tarix = vaxt.Date;

            // Bu günün Tesdiqlenib icazəsi
            var icaze = await _db.Icazeler
                .Where(x =>
                    x.IsciId == isciId &&
                    !x.Silinib &&
                    x.Status == IcazeStatus.Tesdiqlenib &&
                    x.IcazeTarixi.Date == tarix)
                .Include(x => x.CixisGiris)
                .FirstOrDefaultAsync();

            if (icaze?.CixisGiris == null) return;

            var cg = icaze.CixisGiris;

            if (cg.Status == IcazeCixisGirisStatus.LegvEdildi ||
                cg.Status == IcazeCixisGirisStatus.Tamamlandi)
                return;

            if (cg.CixisVaxt == null)
            {
                // İlk skan icazə başlama saatından sonradırsa çıxış say
                var icazeBaslamaDateTime = tarix + icaze.BaslamaSaati;
                if (vaxt >= icazeBaslamaDateTime.AddMinutes(-15))
                {
                    cg.CixisVaxt = vaxt;
                    cg.Status = cg.Birdefelik
                        ? IcazeCixisGirisStatus.Tamamlandi
                        : IcazeCixisGirisStatus.Cixdi;

                    _db.Update(cg);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation(
                        "İcazə çıxışı qeydə alındı: IsciId={IsciId}, IcazeId={IcazeId}, Vaxt={Vaxt}",
                        isciId, icaze.Id, vaxt);
                }
            }
            else if (cg.QayidisVaxt == null && !cg.Birdefelik)
            {
                // İkinci skan — qayıdış
                // Çıxışdan ən az 5 dəqiqə sonra olmalıdır ki, duplikat skan sayılmasın
                if (vaxt > cg.CixisVaxt.Value.AddMinutes(5))
                {
                    cg.QayidisVaxt = vaxt;
                    cg.Status = IcazeCixisGirisStatus.Tamamlandi;

                    _db.Update(cg);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation(
                        "İcazə qayıdışı qeydə alındı: IsciId={IsciId}, IcazeId={IcazeId}, Vaxt={Vaxt}",
                        isciId, icaze.Id, vaxt);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İcazə çıxış/qayıdış izlənmə xətası: IsciId={IsciId}", isciId);
        }
    }

    private static DavamiyyetStatus HesablaStatus(
        DateTime girisVaxti,
        Icaze? bugunIcaze,
        EzamiyyetMuraciet? bugunEzamiyyet,
        TimeSpan standartGiris,
        TimeSpan gecikTolerans)
    {
        var girisZaman = girisVaxti.TimeOfDay;

        // Aktiv icazə varmı və giriş icazə dövrünə düşürmü?
        if (bugunIcaze != null &&
            girisZaman >= bugunIcaze.BaslamaSaati &&
            girisZaman <= bugunIcaze.BitisSaati + gecikTolerans)
        {
            return DavamiyyetStatus.Icazeli;
        }

        // Səhər ezamiyyəti: giriş ezamiyyətin bitmə saatı + tolerans aralığındadırsa gecikməyib
        // Məs: ezamiyyət 09:00-11:00 → işçi 10:45-də gəlirsə gecikməyib
        if (bugunEzamiyyet?.BitisSaati != null &&
            girisZaman <= bugunEzamiyyet.BitisSaati.Value + gecikTolerans)
        {
            return DavamiyyetStatus.Isde;
        }

        // Normal gecikme yoxlaması
        return girisZaman > standartGiris + gecikTolerans
            ? DavamiyyetStatus.Gecikme
            : DavamiyyetStatus.Isde;
    }
}
