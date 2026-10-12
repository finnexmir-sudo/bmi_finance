using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.UI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FinNex.UI.Services;

/// <summary>
/// IDesktopBildirisService-in SignalR implementasiyasi.
/// IHubContext-u inject edərək işçinin bağli olan masaüstü
/// proqramlarına anlıq mesaj göndərir.
/// </summary>
public class SignalRDesktopBildirisService : IDesktopBildirisService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRDesktopBildirisService> _logger;

    public SignalRDesktopBildirisService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRDesktopBildirisService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PushAsync(int isciId, string bashliq, string metn, string? url = null, BildirisNovu nov = default)
    {
        if (isciId <= 0) return;

        var group = $"desktopUser_{isciId}";
        _logger.LogInformation("Desktop push → group={Group} bashliq={Bashliq}", group, bashliq);

        // 12.10.2026 — `url` MƏQSƏDLİ OLARAQ masaüstü agentə GÖNDƏRİLMİR.
        // Masaüstü agentin klik-edəndə-brauzer-aça bilən versiyası real maşınlarda
        // login səhifəsi açırdı (istifadəçi bu login-dən çıxa bilmirdi, baxmayaraq
        // ki, sayt artıq açıq idi) — çünki agent URL-i sistemin DEFOLT brauzerində
        // açır, öz sessiyası yoxdur, və agentın baza ünvanı (appsettings.json) real
        // giriş etdiyi ünvanla üst-üstə düşməyə bilir. Həmin agent bu reponun
        // HİSSƏSİ DEYİL (`FinNex.DesktopAgent/TrayAgent.cs`-in bu repoda heç bir
        // branch/tarixçəsində klik-brauzer məntiqi YOXDUR — yoxlanıldı), ona görə
        // bu reponun kodu agentın özünü düzəldə bilmir. İstifadəçi qərarı: "əgər
        // düzgün işləmirsə, heç işləməsin" — hər komputerdə agenti yenidən
        // yükləməkdən (deploy) qaçmaq üçün, link GÖNDƏRMƏMƏK seçildi.
        // Agent düzgün (eyni brauzer sessiyasında açan) klik məntiqi ilə
        // yenilənəndə, bu sətri `url` qaytararaq geri aç.
        await _hubContext.Clients
            .Group(group)
            .SendAsync("ReceiveDesktopNotification", new
            {
                bashliq,
                metn,
                tarix = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                url = (string?)null,
                nov = (int)nov
            });

        _logger.LogInformation("Desktop push göndərildi → group={Group}", group);
    }
}
