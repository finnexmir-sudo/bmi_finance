using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Services.Communication;
using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinNex.Infrastructure.BackgroundJobs;

public class GelenMailSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GelenMailSyncService> _logger;
    private readonly TimeSpan _interval;
    private DateTime _lastEscalationDate = DateTime.MinValue;

    public GelenMailSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<GelenMailSyncService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var minutes = config.GetValue("GelenMail:CheckIntervalMinutes", 5);
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GelenMailSyncService başladı.");

        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var (imapHost, email, password) = await GetImapCredentialsAsync(scope);

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("GelenMail: heç bir istifadəçidə IMAP məlumatları tapılmadı.");
                }
                else
                {
                    var syncer = scope.ServiceProvider.GetRequiredService<IGelenMailImapSyncer>();
                    var count = await syncer.SyncNowAsync(imapHost, email, password, stoppingToken);
                    if (count > 0)
                        await SendRehberBildirisAsync(scope, count, stoppingToken);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GelenMailSyncService xətası"); }

            try { await MaybeRunEscalationAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Eskalasiya hesabatı xətası"); }

            try { await Task.Delay(_interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    // AppUser-dən ilk konfiqurasiya edilmiş IMAP məlumatlarını götür
    private static async Task<(string imapHost, string email, string password)> GetImapCredentialsAsync(IServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dpProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var protector = dpProvider.CreateProtector("MailSmtpParol");
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var rehberUsers = await userManager.GetUsersInRoleAsync(RoleNames.Rehber);
        var adminUsers  = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
        var candidates  = rehberUsers.Concat(adminUsers);

        foreach (var user in candidates)
        {
            if (string.IsNullOrWhiteSpace(user.MailSmtpEmail) || string.IsNullOrWhiteSpace(user.MailSmtpParol))
                continue;

            try
            {
                var password = protector.Unprotect(user.MailSmtpParol);
                var imapHost = GelenMailImapSyncer.DeriveImapHost(user.MailSmtpHost, user.MailSmtpEmail);
                return (imapHost, user.MailSmtpEmail, password);
            }
            catch { }
        }

        return ("", "", "");
    }

    private async Task MaybeRunEscalationAsync(CancellationToken ct)
    {
        var now = DateTime.Now;
        if (now.Date == _lastEscalationDate.Date) return;
        if (now.Hour != 9 || now.Minute > 10) return;

        _lastEscalationDate = now;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = now.Date;
        var inSevenDays = today.AddDays(7);

        var deadlineMails = await db.Set<GelenMail>()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.DedlaynTarix.HasValue)
            .Select(x => new { x.Movzu, x.DedlaynTarix })
            .ToListAsync(ct);

        var geciken    = deadlineMails.Where(m => m.DedlaynTarix!.Value.Date < today).ToList();
        var yaxinlasan = deadlineMails.Where(m => m.DedlaynTarix!.Value.Date >= today && m.DedlaynTarix.Value.Date <= inSevenDays).ToList();

        if (geciken.Count == 0 && yaxinlasan.Count == 0) return;

        var lines = new System.Text.StringBuilder();
        if (geciken.Count > 0)
        {
            lines.Append($"Gecikənlər ({geciken.Count}): ");
            lines.Append(string.Join("; ", geciken.Take(3).Select(m => $"{m.Movzu} ({m.DedlaynTarix!.Value:dd.MM})")));
            if (geciken.Count > 3) lines.Append($" +{geciken.Count - 3}");
        }
        if (yaxinlasan.Count > 0)
        {
            if (lines.Length > 0) lines.Append(" | ");
            lines.Append($"Yaxınlaşanlar ({yaxinlasan.Count}): ");
            lines.Append(string.Join("; ", yaxinlasan.Take(3).Select(m => $"{m.Movzu} ({m.DedlaynTarix!.Value:dd.MM})")));
            if (yaxinlasan.Count > 3) lines.Append($" +{yaxinlasan.Count - 3}");
        }

        var bashliq = $"Dedlayn xülasəsi — {geciken.Count} gecikən, {yaxinlasan.Count} yaxınlaşan";
        await SendEscalationBildirisAsync(scope, bashliq, lines.ToString(), ct);
    }

    private async Task SendEscalationBildirisAsync(IServiceScope scope, string bashliq, string metn, CancellationToken ct)
    {
        try
        {
            var userManager    = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var bildirisService = scope.ServiceProvider.GetRequiredService<IBildirisService>();
            var rehberUsers    = await userManager.GetUsersInRoleAsync(RoleNames.Rehber);
            foreach (var user in rehberUsers)
            {
                if (user.IsciId is null) continue;
                await bildirisService.YaratAsync(user.IsciId.Value, BildirisNovu.YeniGelenMail, bashliq, metn, "/User/GelenMail");
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Eskalasiya bildiris göndərmə xətası"); }
    }

    private async Task SendRehberBildirisAsync(IServiceScope scope, int count, CancellationToken ct)
    {
        try
        {
            var userManager    = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var bildirisService = scope.ServiceProvider.GetRequiredService<IBildirisService>();
            var rehberUsers    = await userManager.GetUsersInRoleAsync(RoleNames.Rehber);

            var bashliq = count == 1 ? "Yeni mail" : $"{count} yeni mail";
            var metn    = count == 1
                ? "Gələn qutunuza 1 yeni mail daxil oldu."
                : $"Gələn qutunuza {count} yeni mail daxil oldu.";

            foreach (var user in rehberUsers)
            {
                if (user.IsciId is null) continue;
                await bildirisService.YaratAsync(user.IsciId.Value, BildirisNovu.YeniGelenMail, bashliq, metn, "/User/GelenMail");
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Rehber bildiris göndərmə xətası"); }
    }
}
