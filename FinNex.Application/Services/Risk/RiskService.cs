using System.Globalization;
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

    // Tarixi TO_DATE ifadəsinə çevirir (yalnız düzgün tarix — injeksiyaya qapalı)
    private static bool TarixCevir(string? xam, out string toDate)
    {
        toDate = "";
        if (string.IsNullOrWhiteSpace(xam)) return false;
        string[] formatlar = { "yyyy-MM-dd", "dd-MM-yyyy", "dd.MM.yyyy" };
        if (DateTime.TryParseExact(xam.Trim(), formatlar, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d))
        {
            toDate = $"TO_DATE('{d:dd-MM-yyyy}','DD-MM-YYYY')";
            return true;
        }
        return false;
    }

    public async Task<RiskNeticeDto?> IcraEtAsync(int sorguId, RiskParametrDeyer? deyerler = null, int maxRows = 100000)
    {
        var res = await _sorguService.HamisiniGetirAsync();
        var sorgu = res?.Data?.FirstOrDefault(x =>
            x.Id == sorguId && x.Aktiv && Norm(x.DepartamentAd).Contains("risk"));
        if (sorgu == null) return null;

        deyerler ??= new RiskParametrDeyer();
        var sql = sorgu.SorguMetni;

        // Hansı parametr token-ləri var?
        var p = new RiskParametrler
        {
            BasTarix = sql.Contains("{BASTARIX}"),
            SonTarix = sql.Contains("{SONTARIX}"),
            Tarix    = sql.Contains("{TARIX}"),
            Hedd     = sql.Contains("{HEDD}"),
            Il       = sql.Contains("{IL}")
        };

        var netice = new RiskNeticeDto
        {
            Id = sorgu.Id, Ad = sorgu.SorguAdi, Mahiyyet = sorgu.Mahiyyet,
            Parametrler = p, Deyerler = deyerler
        };

        // Tarix parametrləri
        if (p.BasTarix)
        {
            if (!TarixCevir(deyerler.BasTarix, out var t)) return TamamlanmayibReturn(netice);
            sql = sql.Replace("{BASTARIX}", t);
        }
        if (p.SonTarix)
        {
            if (!TarixCevir(deyerler.SonTarix, out var t)) return TamamlanmayibReturn(netice);
            sql = sql.Replace("{SONTARIX}", t);
        }
        if (p.Tarix)
        {
            if (!TarixCevir(deyerler.Tarix, out var t)) return TamamlanmayibReturn(netice);
            sql = sql.Replace("{TARIX}", t);
        }
        // Ədəd parametrləri (yalnız rəqəm — injeksiyaya qapalı)
        if (p.Hedd)
        {
            if (!decimal.TryParse(deyerler.Hedd, NumberStyles.Number, CultureInfo.InvariantCulture, out var h)
                && !decimal.TryParse(deyerler.Hedd, NumberStyles.Number, new CultureInfo("az-AZ"), out h))
                return TamamlanmayibReturn(netice);
            sql = sql.Replace("{HEDD}", h.ToString(CultureInfo.InvariantCulture));
        }
        if (p.Il)
        {
            if (!int.TryParse(deyerler.Il, out var il) || il < 1900 || il > 2200)
                return TamamlanmayibReturn(netice);
            sql = sql.Replace("{IL}", il.ToString(CultureInfo.InvariantCulture));
        }

        try
        {
            // SelectXamAsync daxilində YalnızSelect yoxlanılır — DML/DDL bloklanır
            var xam = await _oracle.SelectXamAsync(sql, maxRows);
            netice.Netice = xam;
            netice.Say = xam.Setirler.Count;
            netice.IcraOlundu = true;
        }
        catch (Exception ex)
        {
            netice.IcraOlundu = false;
            netice.Xeta = ex.Message;
        }
        return netice;
    }

    private static RiskNeticeDto TamamlanmayibReturn(RiskNeticeDto n)
    {
        n.IcraOlundu = false;
        return n;
    }
}
