using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinNex.Application.BackgroundJobs;

/// <summary>
/// ZKTeco cihazına HTTP ilə qoşulub davamiyyət məlumatlarını çəkən background servis.
/// Cihaz ADMS mode-dadır, ona görə HTTP (port 4370) ilə sorğulanır.
/// </summary>
public class ZkTecoSdkService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ZkTecoSdkService> _logger;
    private readonly HttpClient _httpClient;

    private const string DeviceIp = "192.168.0.94";
    private const int DevicePort = 4370;
    private const string DeviceSN = "test";
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public static DateTime? SonElaqa { get; private set; }
    public static bool IsOnline { get; private set; }

    public ZkTecoSdkService(
        IServiceScopeFactory scopeFactory,
        ILogger<ZkTecoSdkService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ZkTecoSdkService başladı. Cihaz: {Ip}:{Port} (HTTP polling)", DeviceIp, DevicePort);

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollDeviceAsync();
            }
            catch (Exception ex)
            {
                IsOnline = false;
                _logger.LogError(ex, "ZkTecoSdkService xətası.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PollDeviceAsync()
    {
        var url = $"http://{DeviceIp}:{DevicePort}/iclock/cdata?SN={DeviceSN}";

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                IsOnline = true;
                SonElaqa = DateTime.Now;
                _logger.LogInformation("Cihaz onlayndır. Cavab: {Len} simvol", body.Length);

                // Əgər cihaz attendance data qaytarırsa, parse et
                if (!string.IsNullOrWhiteSpace(body) && body.Contains('\t'))
                {
                    await ProcessAttendanceDataAsync(body);
                }
            }
            else
            {
                _logger.LogWarning("Cihaz HTTP {Status} qaytardı.", response.StatusCode);
                IsOnline = false;
            }
        }
        catch (TaskCanceledException)
        {
            IsOnline = false;
            _logger.LogWarning("Cihaz cavab vermədi (timeout).");
        }
        catch (HttpRequestException ex)
        {
            IsOnline = false;
            _logger.LogWarning("Cihaza qoşulmaq mümkün olmadı: {Msg}", ex.Message);
        }
    }

    private async Task ProcessAttendanceDataAsync(string body)
    {
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int processed = 0;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var line in lines)
        {
            try
            {
                var parts = line.Trim().Split('\t');
                if (parts.Length < 3) continue;

                if (!int.TryParse(parts[0].Trim(), out int isciId)) continue;
                if (!DateTime.TryParse(parts[1].Trim(), out DateTime vaxt)) continue;

                int nov = parts.Length > 2 && int.TryParse(parts[2].Trim(), out int n) ? n : 0;
                bool girisdir = nov == 0 || nov == 4;

                var tarix = vaxt.Date;

                var movcud = await db.Davamiyyetler
                    .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Tarix == tarix);

                if (movcud == null)
                {
                    await db.Davamiyyetler.AddAsync(new Davamiyyet
                    {
                        IsciId = isciId,
                        Tarix = tarix,
                        GirisVaxti = girisdir ? vaxt : null,
                        CixisVaxti = girisdir ? null : vaxt,
                        Status = HesablaStatus(vaxt, girisdir)
                    });
                }
                else
                {
                    if (girisdir && (movcud.GirisVaxti == null || vaxt < movcud.GirisVaxti))
                    {
                        movcud.GirisVaxti = vaxt;
                        movcud.Status = HesablaStatus(vaxt, true);
                    }
                    else if (!girisdir && (movcud.CixisVaxti == null || vaxt > movcud.CixisVaxti))
                    {
                        movcud.CixisVaxti = vaxt;
                    }
                }

                await db.SaveChangesAsync();
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AttLog parse xətası: {Line}", line);
            }
        }

        if (processed > 0)
        {
            _logger.LogInformation("{Count} davamiyyət qeydi yazıldı.", processed);
        }
    }

    private static DavamiyyetStatus HesablaStatus(DateTime girisVaxti, bool girisdir)
    {
        if (!girisdir) return DavamiyyetStatus.Isde;
        var isBaslamaVaxti = girisVaxti.Date.AddHours(9);
        return girisVaxti > isBaslamaVaxti.AddMinutes(5) ? DavamiyyetStatus.Gecikme : DavamiyyetStatus.Isde;
    }

    public override void Dispose()
    {
        _httpClient.Dispose();
        base.Dispose();
    }
}
