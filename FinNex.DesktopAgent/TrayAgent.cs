using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace FinNex.DesktopAgent;

/// <summary>
/// Arxa fonda işləyən system tray agenti.
/// SignalR vasitəsilə NotificationHub-a qoşulur, gələn bildirişləri
/// Windows balloon-tip kimi ekranda göstərir.
/// Bağlantı kəsilsə exponential backoff ilə yenidən qoşulur.
///
/// Hub URL-i: /notificationHub?access_token={token}&isciId={isciId}
/// IsciId həm token-dən həm query-dən göndərilir;
/// server tərəf yalnız query-dən oxuyur (claim mapping problemini aradan qaldırır).
///
/// NotifyIcon Control-dan törəmədiyinə görə Invoke() yoxdur;
/// Shell_NotifyIcon Win32 API-si thread-safe olduğundan birbaşa çağırıla bilər.
/// </summary>
public class TrayAgent : ApplicationContext
{
    private readonly AppConfig _config;
    private readonly string _token;
    private readonly int _isciId;
    private readonly string _ad;

    private readonly NotifyIcon _trayIcon;
    private HubConnection? _connection;
    private int _reconnectAttempt;

    public TrayAgent(AppConfig config, string token, int isciId, string ad)
    {
        _config = config;
        _token = token;
        _isciId = isciId;
        _ad = ad;

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            Text = $"FinNex Agent — {_ad}",
            ContextMenuStrip = BuildContextMenu()
        };

        // UI thread-ini bloklamadan SignalR qoşulmasını başlat
        _ = ConnectAsync();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("FinNex — " + _ad).Enabled = false;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Çıxış", null, (_, _) => ExitAgent());
        return menu;
    }

    private async Task ConnectAsync()
    {
        while (true)
        {
            try
            {
                // access_token: JWT Bearer auth üçün (Program.cs-dəki JwtBearerEvents oxuyur)
                // isciId: Hub-un OnConnectedAsync-i qrup adını query-dən qurur
                var hubUrl = $"{_config.FullHubUrl}?access_token={_token}&isciId={_isciId}";

                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, opts =>
                    {
                        // İnkişaf mühitlərindəki self-signed sertifikatları qəbul et
                        opts.HttpMessageHandlerFactory = _ =>
                            new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                            };
                    })
                    .WithAutomaticReconnect(
                        new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15) })
                    .Build();

                _connection.On<JsonElement>("ReceiveDesktopNotification", payload =>
                {
                    var bashliq = payload.TryGetProperty("bashliq", out var b)
                        ? b.GetString() ?? "Bildiriş" : "Bildiriş";
                    var metn = payload.TryGetProperty("metn", out var m)
                        ? m.GetString() ?? "" : "";
                    var tarix = payload.TryGetProperty("tarix", out var t)
                        ? t.GetString() ?? "" : "";

                    ShowBalloon(bashliq, string.IsNullOrEmpty(tarix) ? metn : $"{metn}\n{tarix}");
                });

                _connection.Closed += async ex =>
                {
                    SetTrayTooltip(bağlandi: false);
                    // WithAutomaticReconnect bütün cəhdlərini bitirdikdən sonra
                    // Closed fire olur. Tam yeni bağlantı qurulmalıdır.
                    await ReconnectWithBackoffAsync();
                    _ = ConnectAsync(); // loop-u yenidən başlat
                };

                _connection.Reconnected += _ =>
                {
                    _reconnectAttempt = 0;
                    SetTrayTooltip(bağlandi: true);
                    return Task.CompletedTask;
                };

                await _connection.StartAsync();
                _reconnectAttempt = 0;
                SetTrayTooltip(bağlandi: true);
                return; // Uğurlu qoşulma — loopdan çıx
            }
            catch
            {
                SetTrayTooltip(bağlandi: false);
                await ReconnectWithBackoffAsync();
            }
        }
    }

    private async Task ReconnectWithBackoffAsync()
    {
        _reconnectAttempt++;
        // 2s, 4s, 8s, 16s, 30s (cap)
        var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, _reconnectAttempt), 30));
        await Task.Delay(delay);
    }

    private void SetTrayTooltip(bool bağlandi)
    {
        if (_trayIcon.IsDisposed) return;
        _trayIcon.Text = bağlandi
            ? $"FinNex Agent — {_ad} (Qoşuldu)"
            : $"FinNex Agent — {_ad} (Qoşulmadı)";
    }

    private void ShowBalloon(string bashliq, string metn)
    {
        if (_trayIcon.IsDisposed) return;
        _trayIcon.BalloonTipTitle = bashliq;
        _trayIcon.BalloonTipText = metn;
        _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
        _trayIcon.ShowBalloonTip(8000);
    }

    private void ExitAgent()
    {
        _trayIcon.Visible = false;
        _ = _connection?.StopAsync();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _connection?.DisposeAsync().AsTask().Wait(2000);
        }
        base.Dispose(disposing);
    }
}
