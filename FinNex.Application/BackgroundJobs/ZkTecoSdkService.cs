using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinNex.Application.BackgroundJobs;

public class ZkTecoSdkService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ZkTecoSdkService> _logger;
    private readonly HttpClient _httpClient;

    private const string DeviceIp = "192.168.0.95";
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
        _logger.LogInformation("ZkTecoSdkService başladı.");
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
                IsOnline = true;
                SonElaqa = DateTime.Now;
            }
            else
            {
                IsOnline = false;
            }
        }
        catch
        {
            IsOnline = false;
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
