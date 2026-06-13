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
                List<(int userId, string imapHost, string email, string password)> hesablar;
                using (var scope = _scopeFactory.CreateScope())
                    hesablar = await GetAllImapCredentialsAsync(scope);

                if (hesablar.Count == 0)
                {
                    _logger.LogWarning("GelenMail: heç bir istifadəçidə IMAP məlumatları tapılmadı.");
                }
                else
                {
                    // Hər istifadəçinin qutusu AYRICA sinxronlaşır və mail öz sahibinə möhürlənir.
                    // Hər biri öz scope-unda işləyir ki, EF tracking/fixup sahiblər arasında qarışmasın.
                    foreach (var hesab in hesablar)
                    {
                        try
                        {
                            using var syncScope = _scopeFactory.CreateScope();
                            var syncer = syncScope.ServiceProvider.GetRequiredService<IGelenMailImapSyncer>();
                            var count = await syncer.SyncNowAsync(hesab.imapHost, hesab.email, hesab.password, hesab.userId, stoppingToken);
                            if (count > 0)
                                await SendOwnerBildirisAsync(syncScope, hesab.userId, count, stoppingToken);
                        }
                        catch (Exception ex) { _logger.LogError(ex, "GelenMail sync xətası (user {UserId})", hesab.userId); }
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GelenMailSyncService xətası"); }

            try { await MaybeRunEscalationAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Eskalasiya hesabatı xətası"); }

            try { await Task.Delay(_interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    // Mail məlumatı konfiqurasiya edilmiş BÜTÜN Rəhbər/Admin istifadəçiləri (hərə öz qutusu)
    private static async Task<List<(int userId, string imapHost, string email, string password)>> GetAllImapCredentialsAsync(IServiceScope scope)
    {
        var dpProvider  = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var protector   = dpProvider.CreateProtector("MailSmtpParol");
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var rehberUsers = await userManager.GetUsersInRoleAsync(RoleNames.Rehber);
        var adminUsers  = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
        var candidates  = rehberUsers.Concat(adminUsers)
            .GroupBy(u => u.Id)
            .Select(g => g.First());

        var list = new List<(int userId, string imapHost, string email, string password)>();
        foreach (var user in candidates)
        {
            if (string.IsNullOrWhiteSpace(user.MailSmtpEmail) || string.IsNullOrWhiteSpace(user.MailSmtpParol))
                continue;

            try
            {
                var password = protector.Unprotect(user.MailSmtpParol);
                var imapHost = GelenMailImapSyncer.DeriveImapHost(user.MailSmtpHost, user.MailSmtpEmail);
                list.Add((user.Id, imapHost, user.MailSmtpEmail, password));
            }
            catch { }
        }

        return list;
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

        // Deadline-lı maillər sahibə görə qruplaşır — hər sahibə YALNIZ öz maillərinin
        // xülasəsi gedir (mövzular başqa istifadəçiyə sızmasın).
        var deadlineMails = await db.Set<GelenMail>()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.DedlaynTarix.HasValue && x.SahibUserId != null)
            .Select(x => new { x.SahibUserId, x.Movzu, x.DedlaynTarix })
            .ToListAsync(ct);

        foreach (var grup in deadlineMails.GroupBy(x => x.SahibUserId!.Value))
        {
            var geciken    = grup.Where(m => m.DedlaynTarix!.Value.Date < today).ToList();
            var yaxinlasan = grup.Where(m => m.DedlaynTarix!.Value.Date >= today && m.DedlaynTarix.Value.Date <= inSevenDays).ToList();

            if (geciken.Count == 0 && yaxinlasan.Count == 0) continue;

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
            await SendOwnerNotificationAsync(scope, grup.Key, bashliq, lines.ToString());
        }
    }

    // Yeni mail bildirişi — yalnız qutu sahibinə
    private async Task SendOwnerBildirisAsync(IServiceScope scope, int ownerUserId, int count, CancellationToken ct)
    {
        var bashliq = count == 1 ? "Yeni mail" : $"{count} yeni mail";
        var metn    = count == 1
            ? "Gələn qutunuza 1 yeni mail daxil oldu."
            : $"Gələn qutunuza {count} yeni mail daxil oldu.";

        await SendOwnerNotificationAsync(scope, ownerUserId, bashliq, metn);
    }

    // Ortaq köməkçi — bildirişi yalnız sahibin işçi profilinə göndərir
    private async Task SendOwnerNotificationAsync(IServiceScope scope, int ownerUserId, string bashliq, string metn)
    {
        try
        {
            var userManager     = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var bildirisService = scope.ServiceProvider.GetRequiredService<IBildirisService>();

            var owner = await userManager.FindByIdAsync(ownerUserId.ToString());
            if (owner == null || owner.IsciId == null) return;

            await bildirisService.YaratAsync(owner.IsciId.Value, BildirisNovu.YeniGelenMail, bashliq, metn, "/User/GelenMail");
        }
        catch (Exception ex) { _logger.LogError(ex, "Mail bildiriş göndərmə xətası (user {UserId})", ownerUserId); }
    }
}
