using System.Text.RegularExpressions;
using FinNex.Application.Interfaces.Pid;
using FinNex.Application.Interfaces.Sms;
using FinNex.Domain.Entities.Pid;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinNex.Application.Services.Pid;

public class PidSmsService : IPidSmsService
{
    private readonly IUnitOfWork _uow;
    private readonly ISmsGateway _gateway;
    private readonly ILogger<PidSmsService> _logger;

    public PidSmsService(IUnitOfWork uow, ISmsGateway gateway, ILogger<PidSmsService> logger)
    {
        _uow = uow;
        _gateway = gateway;
        _logger = logger;
    }

    public string TelefonNormallasdir(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        // Yalnız rəqəmlər qalsın
        var digits = Regex.Replace(raw, @"\D", "");
        if (digits.Length == 0) return "";

        // Lokal "0xxxxxxxxx" → "994xxxxxxxxx"
        if (digits.StartsWith("0") && digits.Length == 10)
            return "994" + digits.Substring(1);

        // "994xxxxxxxxx" — artıq düzgün
        if (digits.StartsWith("994") && digits.Length == 12)
            return digits;

        // 9 rəqəmli "55xxxxxxx" — ölkə kodu yoxdur, əlavə et
        if (digits.Length == 9)
            return "994" + digits;

        return digits;
    }

    public async Task<PidSmsLog> GonderAsync(string telefon, string metn, int? sablonId, int gonderenIsciId)
    {
        var norm = TelefonNormallasdir(telefon);

        var log = new PidSmsLog
        {
            Telefon = norm,
            Metn = metn?.Trim() ?? "",
            SablonId = sablonId,
            Status = PidSmsStatus.Gozleyir,
            GonderenIsciId = gonderenIsciId,
            YaradanIcraciId = gonderenIsciId,
            YaradilmaTarixi = DateTime.Now
        };

        try
        {
            var result = await _gateway.SendAsync(norm, log.Metn);
            log.GatewayCavabi = result.RawResponse;

            if (result.Success)
            {
                log.GoyercinId = result.ExternalId;
                log.Status = PidSmsStatus.Gonderildi;
                log.GonderilmeTarixi = DateTime.Now;
            }
            else
            {
                log.Status = PidSmsStatus.Xeta;
                log.Xeta = result.ErrorMessage;
                _logger.LogWarning("PİD SMS xəta: {Tel} → {Err}", norm, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            log.Status = PidSmsStatus.Xeta;
            log.Xeta = ex.Message;
            _logger.LogError(ex, "PİD SMS gateway istisna: {Tel}", norm);
        }

        await _uow.Repository<PidSmsLog>().YaratAsync(log);
        await _uow.YaddaSaxlaAsync();
        return log;
    }

    public async Task<(int Ugur, int Xeta)> TopluGonderAsync(
        IEnumerable<(string Ad, string Telefon)> alicilar,
        string smsMetni,
        int gonderenIsciId)
    {
        int ugur = 0, xeta = 0;
        foreach (var (ad, telefon) in alicilar)
        {
            var metn = smsMetni
                .Replace("{{1}}", ad)
                .Replace("{AD}", ad, StringComparison.OrdinalIgnoreCase);
            var log = await GonderAsync(telefon, metn, sablonId: null, gonderenIsciId);
            if (log.Status == PidSmsStatus.Xeta) xeta++;
            else ugur++;
        }
        return (ugur, xeta);
    }

    public async Task<IList<PidSmsLog>> SonGonderilenler(int say = 100)
    {
        return await _uow.Repository<PidSmsLog>().Query()
            .Where(x => !x.Silinib)
            .OrderByDescending(x => x.YaradilmaTarixi)
            .Take(say)
            .Include(x => x.Sablon)
            .Include(x => x.GonderenIsci)
            .ToListAsync();
    }
}
