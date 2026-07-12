using FinNex.Application.DTOs.Risk;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Risk;
using FinNex.Application.Interfaces.Sorgular;

namespace FinNex.Application.Services.Risk;

public class RiskService : IRiskService
{
    private readonly IOracleSorguService _sorguService;
    private readonly IOracleService _oracle;

    public RiskService(IOracleSorguService sorguService, IOracleService oracle)
    {
        _sorguService = sorguService;
        _oracle = oracle;
    }

    private static string Norm(string? s) => (s ?? "").Replace(" ", "").ToLowerInvariant();

    // "Risk" departamentinə aid aktiv sorğular
    public async Task<IList<RiskHesabatDto>> HesabatlarAsync()
    {
        var res = await _sorguService.HamisiniGetirAsync();
        if (res?.Data == null) return new List<RiskHesabatDto>();

        return res.Data
            .Where(x => x.Aktiv && Norm(x.DepartamentAd).Contains("risk"))
            .OrderBy(x => x.SorguAdi)
            .Select(x => new RiskHesabatDto { Id = x.Id, Ad = x.SorguAdi, Mahiyyet = x.Mahiyyet })
            .ToList();
    }

    public async Task<RiskNeticeDto?> IcraEtAsync(int sorguId, int maxRows = 100000)
    {
        var res = await _sorguService.HamisiniGetirAsync();
        var sorgu = res?.Data?.FirstOrDefault(x =>
            x.Id == sorguId && x.Aktiv && Norm(x.DepartamentAd).Contains("risk"));
        if (sorgu == null) return null;

        // SelectXamAsync daxilində YalnızSelect yoxlanılır — DML/DDL bloklanır
        var netice = await _oracle.SelectXamAsync(sorgu.SorguMetni, maxRows);

        return new RiskNeticeDto
        {
            Id       = sorgu.Id,
            Ad       = sorgu.SorguAdi,
            Mahiyyet = sorgu.Mahiyyet,
            Netice   = netice,
            Say      = netice.Setirler.Count
        };
    }
}
