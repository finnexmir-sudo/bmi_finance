using FinNex.DataAccess.Contexts;
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

    public static DateTime? SonElaqa { get; private set; }
    public static string? SonSN { get; private set; }

    public ADMSController(AppDbContext db, ILogger<ADMSController> logger)
    {
        _db = db;
        _logger = logger;
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

            // Həmin işçi+tarix üçün mövcud davamiyyət qeydini tap
            var movcud = await _db.Davamiyyetler
                .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Tarix == tarix);

            // İş başlama vaxtı — 9:00. Bu vaxta qədər hər yeni oxuma "duplikat giriş
            // cəhdi" kimi qəbul olunur (işçi qayıdıb ikinci dəfə barmağını basa bilər
            // və sistem bunu çıxış kimi yazmamalıdır). 9:00-dan sonra sonrakı oxumalar
            // normal çıxış kimi tutulur.
            var isBaslamaVaxti = tarix.AddHours(9);

            if (movcud == null)
            {
                var yeni = new Davamiyyet
                {
                    IsciId = isciId,
                    Tarix = tarix,
                    GirisVaxti = vaxt,
                    CixisVaxti = null,
                    Status = HesablaStatus(vaxt, true)
                };
                await _db.Davamiyyetler.AddAsync(yeni);
            }
            else
            {
                if (movcud.GirisVaxti == null || vaxt < movcud.GirisVaxti)
                {
                    // Giriş yazılmayıb və ya bu oxuma əvvəlkindən ERKƏNdir → girişi yenilə
                    movcud.GirisVaxti = vaxt;
                    movcud.Status = HesablaStatus(vaxt, true);
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
                    }
                }
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Davamiyyət yazıldı: IsciId={IsciId}, Tarix={Tarix}, {Nov}={Vaxt}",
                isciId, tarix, girisdir ? "Giriş" : "Çıxış", vaxt);

            // Paralel: İcazə çıxış/qayıdış izlənməsi
            await ProcessIcazeCixisGirisAsync(isciId, vaxt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AttLog parse xətası: {Line}", line);
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

    private static DavamiyyetStatus HesablaStatus(DateTime girisVaxti, bool girisdir)
    {
        if (!girisdir) return DavamiyyetStatus.Isde;

        var isBaslamaVaxti = girisVaxti.Date.AddHours(9);

        return girisVaxti > isBaslamaVaxti.AddMinutes(5)
            ? DavamiyyetStatus.Gecikme
            : DavamiyyetStatus.Isde;
    }
}
