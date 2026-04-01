using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers;

[Route("iclock")]
[ApiController]
public class ADMSController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ADMSController> _logger;

    // Cihazın son əlaqə vaxtı — static ki bütün requestlər paylaşsın
    public static DateTime? SonElaqa { get; private set; }
    public static string? SonSN { get; private set; }

    public ADMSController(AppDbContext db, ILogger<ADMSController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("cdata")]
    public IActionResult Handshake([FromQuery] string SN)
    {
        SonElaqa = DateTime.Now;
        SonSN = SN;
        _logger.LogInformation("ADMS Handshake: Cihaz {SN} qoşuldu.", SN);

        var response = $"GET OPTION FROM: {SN}\n" +
                       "ATTLOGStamp=0\n" +
                       "OPERLOGStamp=0\n" +
                       "ATTPHOTOStamp=0\n" +
                       "ErrorDelay=30\n" +
                       "Delay=10\n" +
                       "TransTimes=00:00;14:05\n" +
                       "TransInterval=1\n" +
                       "TransFlag=1111000000\n" +
                       "TimeZone=4\n" +
                       "Realtime=1\n" +
                       "Encrypt=None\n";

        return Content(response, "text/plain");
    }

    [HttpPost("cdata")]
    public async Task<IActionResult> ReceiveAttendance(
        [FromQuery] string SN,
        [FromQuery] string? table)
    {
        SonElaqa = DateTime.Now;
        SonSN = SN;

        using var reader = new StreamReader(Request.Body);
        string body = await reader.ReadToEndAsync();

        _logger.LogInformation("ADMS Data [{Table}] from {SN}:\n{Body}", table, SN, body);

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
    public IActionResult GetRequest([FromQuery] string SN)
    {
        SonElaqa = DateTime.Now;
        SonSN = SN;
        _logger.LogInformation("ADMS GetRequest: Cihaz {SN}", SN);
        return Content("OK", "text/plain");
    }

    [HttpPost("devicecmd")]
    public IActionResult DeviceCmd([FromQuery] string SN)
    {
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
                    GirisVaxti = girisdir ? vaxt : null,
                    CixisVaxti = girisdir ? null : vaxt,
                    Status = HesablaStatus(vaxt, girisdir)
                };
                await _db.Davamiyyetler.AddAsync(yeni);
            }
            else
            {
                if (girisdir)
                {
                    if (movcud.GirisVaxti == null || vaxt < movcud.GirisVaxti)
                    {
                        movcud.GirisVaxti = vaxt;
                        movcud.Status = HesablaStatus(vaxt, true);
                    }
                }
                else
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