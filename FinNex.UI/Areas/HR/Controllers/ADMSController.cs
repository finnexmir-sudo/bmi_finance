using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            var movcud = await _db.Davamiyyetler
                .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Tarix == tarix);

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
                    movcud.GirisVaxti = vaxt;
                    movcud.Status = HesablaStatus(vaxt, true);
                }
                else if (vaxt > movcud.GirisVaxti)
                {
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AttLog parse xətası: {Line}", line);
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
