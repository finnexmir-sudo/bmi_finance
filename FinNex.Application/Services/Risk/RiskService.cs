using System.Globalization;
using FinNex.Application.DTOs.Oracle;
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
            .Select(x => new RiskHesabatDto { Id = x.Id, Ad = x.SorguAdi, Mahiyyet = DrillStrip(x.Mahiyyet) })
            .ToList();
    }

    // Mahiyyət-in əvvəlindəki widget tag-ini oxuyur: [KPI]/[PIE]/[BAR]/[LINE]
    private static (string? tip, string? alt) TagOxu(string? mahiyyet)
    {
        var m = (mahiyyet ?? "").TrimStart();
        foreach (var t in new[] { "KPI", "PIE", "BAR", "LINE" })
        {
            var pre = "[" + t + "]";
            if (m.StartsWith(pre, StringComparison.OrdinalIgnoreCase))
            {
                var rest = m.Substring(pre.Length).Trim();
                return (t, string.IsNullOrWhiteSpace(rest) ? null : rest);
            }
        }
        return (null, string.IsNullOrWhiteSpace(m) ? null : m);
    }

    // Oracle xanasını decimal-a çevirir (rəqəm deyilsə null)
    private static decimal? Eded(object? v)
    {
        switch (v)
        {
            case null: return null;
            case decimal d: return d;
            case double db: return (decimal)db;
            case float f: return (decimal)f;
            case int i: return i;
            case long l: return l;
            case short s: return s;
        }
        var str = v.ToString();
        if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var r)) return r;
        return null;
    }

    // Mahiyyətdən drill direktivini oxuyur: {DRILL:hədəf ad|param|sütun}
    private static (string? hedef, string? param, string? sutun) DrillOxu(string? mahiyyet)
    {
        if (string.IsNullOrEmpty(mahiyyet)) return (null, null, null);
        var i = mahiyyet.IndexOf("{DRILL:", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return (null, null, null);
        var j = mahiyyet.IndexOf('}', i);
        if (j < 0) return (null, null, null);
        var parts = mahiyyet.Substring(i + 7, j - (i + 7)).Split('|');
        if (parts.Length < 3) return (null, null, null);
        var hedef = parts[0].Trim(); var param = parts[1].Trim(); var sutun = parts[2].Trim();
        if (hedef.Length == 0 || param.Length == 0 || sutun.Length == 0) return (null, null, null);
        return (hedef, param, sutun);
    }

    // {DRILL:...} hissəsini Mahiyyətdən çıxarır (göstərilməsin)
    private static string? DrillStrip(string? mahiyyet)
    {
        if (string.IsNullOrEmpty(mahiyyet)) return mahiyyet;
        var i = mahiyyet.IndexOf("{DRILL:", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return mahiyyet;
        var j = mahiyyet.IndexOf('}', i);
        if (j < 0) return mahiyyet;
        return mahiyyet.Remove(i, j - i + 1).Trim();
    }

    // Etiket "YYYY-MM" formatındadırmı? (zaman seriyası → line)
    private static bool AyEtiketi(string? s)
        => s != null && s.Length == 7 && s[4] == '-'
           && char.IsDigit(s[0]) && char.IsDigit(s[1]) && char.IsDigit(s[2]) && char.IsDigit(s[3])
           && char.IsDigit(s[5]) && char.IsDigit(s[6]);

    private const int KpiCap = 20000;   // detal-KPI üçün maksimum yüklənən sətir

    // KPI kartı: nəticə 1 sətirdirsə → o rəqəm (count sorğusu);
    // çox sətirdirsə → sətir SAYI (detal siyahı — kart sayı göstərir, klik siyahını açır).
    private RiskKpiDto KpiQur(int id, string ad, string? alt, OracleNetice xam, bool kesildi = false)
    {
        var kpi = new RiskKpiDto { Id = id, Ad = ad, Alt = alt };
        if (xam.Setirler.Count == 0) { kpi.Deyer = "0"; return kpi; }
        if (xam.Setirler.Count == 1)
        {
            var row = xam.Setirler[0];
            var cell = row.Length > 0 ? row[^1] : null;
            var ed = Eded(cell);
            if (ed.HasValue) kpi.Deyer = ed.Value.ToString("#,0.##", CultureInfo.InvariantCulture);
            else { kpi.Deyer = cell?.ToString() ?? "0"; kpi.Reqem = false; }
        }
        else
        {
            kpi.Deyer = xam.Setirler.Count.ToString("#,0", CultureInfo.InvariantCulture) + (kesildi ? "+" : "");
        }
        return kpi;
    }

    // Qrafik DTO-sunu qurur (sütun0 = etiket, sonuncu sütun = dəyər)
    private RiskChartDto ChartQur(int id, string ad, string tip, OracleNetice xam)
    {
        var ch = new RiskChartDto { Id = id, Ad = ad, Tip = tip };
        foreach (var row in xam.Setirler.Take(50))
        {
            ch.Etiketler.Add(row.Length > 0 ? (row[0]?.ToString() ?? "") : "");
            ch.Deyerler.Add(Eded(row.Length > 1 ? row[^1] : null) ?? 0m);
        }
        return ch;
    }

    // Dashboard paneli: KPI kartları + qrafiklər + adi hesabat kartları.
    // Tag ([KPI]/[PIE]/[BAR]/[LINE]) varsa ona görə; yoxdursa parametrsiz sorğu
    // nəticəsinin FORMASINA görə AVTOMATİK təsnif olunur (1 rəqəm→KPI, 2 sütun→qrafik).
    public async Task<RiskPanelDto> PanelAsync(int maxRows = 5000)
    {
        var panel = new RiskPanelDto();
        var res = await _sorguService.HamisiniGetirAsync();
        var list = res?.Data?
            .Where(x => x.Aktiv && Norm(x.DepartamentAd).Contains("risk"))
            .OrderBy(x => x.SorguAdi)
            .ToList() ?? new();

        foreach (var s in list)
        {
            var (tip, alt) = TagOxu(s.Mahiyyet);
            var tokenli = s.SorguMetni?.Contains('{') ?? false;

            // Parametrli (token-li) sorğu widget ola bilməz — dəyər girişi lazımdır
            if (tokenli)
            {
                panel.Hesabatlar.Add(new RiskHesabatDto { Id = s.Id, Ad = s.SorguAdi, Mahiyyet = DrillStrip(s.Mahiyyet) });
                continue;
            }

            // Açıq TAG varsa — ona güvən
            if (tip == "KPI")
            {
                try
                {
                    // count sorğusu → 1 sətir; detal siyahı → çox sətir (kart sayı göstərir)
                    var xkpi = await _oracle.SelectXamAsync(s.SorguMetni!, KpiCap);
                    panel.Kpiler.Add(KpiQur(s.Id, s.SorguAdi, alt, xkpi, xkpi.Setirler.Count >= KpiCap));
                }
                catch (Exception ex) { panel.Kpiler.Add(new RiskKpiDto { Id = s.Id, Ad = s.SorguAdi, Alt = alt, Xeta = ex.Message }); }
                continue;
            }
            if (tip is "PIE" or "BAR" or "LINE")
            {
                try { panel.Qrafikler.Add(ChartQur(s.Id, s.SorguAdi, tip.ToLowerInvariant(), await _oracle.SelectXamAsync(s.SorguMetni!, maxRows))); }
                catch (Exception ex) { panel.Qrafikler.Add(new RiskChartDto { Id = s.Id, Ad = s.SorguAdi, Xeta = ex.Message }); }
                continue;
            }

            // TAG YOXDUR — nəticənin formasına görə avtomatik təsnif et
            OracleNetice xam;
            try { xam = await _oracle.SelectXamAsync(s.SorguMetni!, 51); }
            catch { panel.Hesabatlar.Add(new RiskHesabatDto { Id = s.Id, Ad = s.SorguAdi, Mahiyyet = DrillStrip(s.Mahiyyet) }); continue; }

            var setirSay = xam.Setirler.Count;
            var sutunSay = xam.Sutunlar.Count;

            // 1 sətir + sonuncu xana rəqəm → KPI
            if (setirSay == 1 && Eded(xam.Setirler[0].Length > 0 ? xam.Setirler[0][^1] : null).HasValue)
            {
                panel.Kpiler.Add(KpiQur(s.Id, s.SorguAdi, s.Mahiyyet, xam));
                continue;
            }

            // 2 sütun, 2..50 sətir, dəyər sütunu rəqəm → qrafik
            if (sutunSay == 2 && setirSay >= 2 && setirSay <= 50
                && xam.Setirler.All(r => Eded(r.Length > 1 ? r[1] : null).HasValue))
            {
                var hamsiAy = xam.Setirler.All(r => AyEtiketi(r.Length > 0 ? r[0]?.ToString() : null));
                var avtoTip = hamsiAy ? "line" : (setirSay <= 6 ? "pie" : "bar");
                panel.Qrafikler.Add(ChartQur(s.Id, s.SorguAdi, avtoTip, xam));
                continue;
            }

            // Əks halda — adi hesabat kartı
            panel.Hesabatlar.Add(new RiskHesabatDto { Id = s.Id, Ad = s.SorguAdi, Mahiyyet = DrillStrip(s.Mahiyyet) });
        }
        return panel;
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

        // Drill-down direktivi: {DRILL:hədəf ad|param|sütun}
        // Klikləyəndə həmin sütunun dəyəri hədəf hesabata (param kimi) ötürülür.
        string? drillSutunAd = null;
        var (dHedef, dParam, dSutun) = DrillOxu(sorgu.Mahiyyet);
        if (dHedef != null)
        {
            netice.Mahiyyet = DrillStrip(sorgu.Mahiyyet);
            var hedef = res!.Data!.FirstOrDefault(z => z.Aktiv
                && Norm(z.DepartamentAd).Contains("risk")
                && string.Equals(z.SorguAdi?.Trim(), dHedef, StringComparison.OrdinalIgnoreCase));
            if (hedef != null)
            {
                netice.DrillId = hedef.Id;
                netice.DrillParam = dParam;
                drillSutunAd = dSutun;
            }
        }

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

            // Drill sütununun indeksini tap (sütun adına görə)
            if (netice.DrillId.HasValue && !string.IsNullOrEmpty(drillSutunAd))
                netice.DrillSutun = xam.Sutunlar.FindIndex(s =>
                    string.Equals(s?.Trim(), drillSutunAd, StringComparison.OrdinalIgnoreCase));
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
