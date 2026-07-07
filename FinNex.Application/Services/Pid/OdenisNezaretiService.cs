using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Pid;
using FinNex.Application.Interfaces.Sorgular;
using FinNex.Domain.Entities.Pid;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FinNex.Application.Services.Pid;

public class OdenisNezaretiService : IOdenisNezaretiService
{
    private readonly IUnitOfWork _uow;
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorguService;

    public OdenisNezaretiService(
        IUnitOfWork uow,
        IOracleService oracle,
        IOracleSorguService sorguService)
    {
        _uow = uow;
        _oracle = oracle;
        _sorguService = sorguService;
    }

    // ── Oracle canlı siyahı: "Ödənişə Nəzarət" (Aktiv Müştərilər + ARH_DD son ödəniş) ──
    // Sorğu Admin → Oracle Sorğular-da saxlanılır, adına görə tapılır (normalizasiya:
    // Azərbaycan hərfləri ASCII-yə, boşluqlar silinir; ad "Ödəniş" + "Nəzarət" saxlamalıdır).
    // Yalnız SELECT — Oracle-a heç bir yazı yoxdur.
    public async Task<OdenisNezaretSiyahiDto> OracleSiyahiAsync()
    {
        var vm = new OdenisNezaretSiyahiDto();
        var sql = await OdenisNezaretSorguMetniAsync();
        if (string.IsNullOrWhiteSpace(sql)) { vm.SorguTapildi = false; return vm; }
        vm.SorguTapildi = true;
        try
        {
            var rows = await _oracle.SelectAsync(sql, maxRows: 100000);
            vm.Setirler = rows.Select(MapOdenisSatir)
                              .OrderBy(x => x.Region).ThenBy(x => x.Musteri)
                              .ToList();
        }
        catch (Exception ex) { vm.Xeta = ex.Message; }
        return vm;
    }

    private async Task<string?> OdenisNezaretSorguMetniAsync()
    {
        var res = await _sorguService.HamisiniGetirAsync();
        if (!res.Success || res.Data is null) return null;
        static string Norm(string? s) => (s ?? "").ToLowerInvariant()
            .Replace("ə", "e").Replace("ş", "s").Replace("ç", "c").Replace("ğ", "g")
            .Replace("ı", "i").Replace("ö", "o").Replace("ü", "u").Replace(" ", "");
        var q = res.Data.FirstOrDefault(x => x.Aktiv
            && Norm(x.SorguAdi).Contains("odenis") && Norm(x.SorguAdi).Contains("nezaret"));
        return q?.SorguMetni;
    }

    private static OdenisNezaretSatirDto MapOdenisSatir(Dictionary<string, object?> row) => new()
    {
        Region          = GetStr(row, "region"),
        Musteri         = GetStr(row, "musteri", "name_regnom", "adi", "ad", "borclu_ad") ?? "(naməlum)",
        KreditHesabi    = GetStr(row, "kredit_hesabi", "licschkre", "hesab") ?? "",
        Ks              = GetStr(row, "ks", "subschkre", "sub") ?? "",
        KreditinNovu    = GetStr(row, "kreditin_novu"),
        TamQaliq        = GetDec(row, "tam_qaliq", "tamqaliq"),
        Qaliq           = GetDec(row, "qaliq", "summa"),
        VkQaliq         = GetDec(row, "vk_qaliq", "summa_19"),
        Status          = GetStr(row, "status", "item_01"),
        Item10          = GetStr(row, "item_10", "son_fealiyyet"),
        SistemSonEmel   = GetStr(row, "sistem_son_emel", "lastpaymentdate_ish"),
        SonOdenisTarixi  = GetStr(row, "son_odenis_tarixi"),
        SonOdenisMeblegi = GetDec(row, "son_odenis_meblegi"),
        OdenisCemi       = GetDec(row, "odenis_cemi"),
        OdenisSayi       = GetDec(row, "odenis_sayi") is decimal ds ? (int)Math.Round(ds) : (int?)null,
        RusumCemi        = GetDec(row, "rusum_cemi"),
    };

    public async Task<IList<OdenisNezaretiDto>> HamisiniGetirAsync(BalansNovu? balans = null, string? axtaris = null)
    {
        var q = _uow.Repository<OdenisNezareti>().Query().Where(x => !x.Silinib);

        if (balans.HasValue)
            q = q.Where(x => x.BalansNovu == balans.Value);

        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var a = axtaris.Trim();
            q = q.Where(x => x.MusteriAdi.Contains(a)
                          || (x.HesabNomresi != null && x.HesabNomresi.Contains(a)));
        }

        var list = await q.OrderByDescending(x => x.YaradilmaTarixi).AsNoTracking().ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<OdenisNezaretiDto?> IdIleGetirAsync(int id)
    {
        var e = await _uow.Repository<OdenisNezareti>().Query()
            .Where(x => x.Id == id && !x.Silinib).AsNoTracking().FirstOrDefaultAsync();
        return e == null ? null : Map(e);
    }

    public async Task<int> YaratAsync(OdenisNezaretiCreateDto dto, int isciId)
    {
        var e = new OdenisNezareti
        {
            BalansNovu      = dto.BalansNovu,
            MusteriAdi      = (dto.MusteriAdi ?? "").Trim(),
            HesabNomresi    = dto.HesabNomresi?.Trim(),
            Teyinat         = dto.Teyinat?.Trim(),
            SonOdenisTarixi = ParseDate(dto.SonOdenisTarixi),
            OdenisVeziyyeti = dto.OdenisVeziyyeti?.Trim(),
            Qeyd            = dto.Qeyd?.Trim(),
            YaradanIcraciId = isciId,
            YaradilmaTarixi = DateTime.Now
        };
        await _uow.Repository<OdenisNezareti>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();
        return e.Id;
    }

    public async Task<bool> YenileAsync(OdenisNezaretiUpdateDto dto, int isciId)
    {
        var e = await _uow.Repository<OdenisNezareti>().Query()
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return false;

        e.BalansNovu        = dto.BalansNovu;
        e.MusteriAdi        = (dto.MusteriAdi ?? "").Trim();
        e.HesabNomresi      = dto.HesabNomresi?.Trim();
        e.Teyinat           = dto.Teyinat?.Trim();
        e.SonOdenisTarixi   = ParseDate(dto.SonOdenisTarixi);
        e.OdenisVeziyyeti   = dto.OdenisVeziyyeti?.Trim();
        e.Qeyd              = dto.Qeyd?.Trim();
        e.YenileyenIcraciId = isciId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<OdenisNezareti>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<bool> SilAsync(int id, int isciId)
    {
        var e = await _uow.Repository<OdenisNezareti>().Query()
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return false;

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = isciId;

        await _uow.Repository<OdenisNezareti>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        string[] f = { "yyyy-MM-dd", "dd.MM.yyyy", "dd-MM-yyyy", "yyyy/MM/dd", "MM/dd/yyyy" };
        if (DateTime.TryParseExact(s, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            return d;
        return null;
    }

    private static OdenisNezaretiDto Map(OdenisNezareti e) => new()
    {
        Id              = e.Id,
        BalansNovu      = e.BalansNovu,
        BalansNovuAd    = e.BalansNovu == BalansNovu.BalansdanKenar ? "Balansdan kənar" : "Balans",
        MusteriAdi      = e.MusteriAdi,
        HesabNomresi    = e.HesabNomresi,
        Teyinat         = e.Teyinat,
        SonOdenisTarixi = e.SonOdenisTarixi,
        OdenisVeziyyeti = e.OdenisVeziyyeti,
        Qeyd            = e.Qeyd
    };

    // ── Oracle sətir oxuma köməkçiləri (adına görə, böyük/kiçik hərf fərqsiz) ──
    private static string? GetStr(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
            foreach (var kv in row)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    var s = kv.Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(s)) return s;
                }
        return null;
    }

    private static decimal? GetDec(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
            foreach (var kv in row)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase) && kv.Value != null)
                {
                    switch (kv.Value)
                    {
                        case decimal dm: return dm;
                        case double db:  return (decimal)db;
                        case float fl:   return (decimal)fl;
                        case int it:     return it;
                        case long lg:    return lg;
                    }
                    var s = kv.Value.ToString()?.Trim();
                    if (string.IsNullOrEmpty(s)) return null;
                    var d = ParseFlexible(s.Replace(" ", "").Replace("₼", ""));
                    if (d.HasValue) return d;
                }
        return null;
    }

    // Həm "16253.05", həm "16253,05", həm "16.253,05" formatını düzgün oxuyur
    private static decimal? ParseFlexible(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        bool hasDot = s.Contains('.');
        bool hasComma = s.Contains(',');
        if (hasDot && hasComma)
        {
            if (s.LastIndexOf(',') > s.LastIndexOf('.'))
                s = s.Replace(".", "").Replace(",", ".");   // vergül onluqdur
            else
                s = s.Replace(",", "");                       // nöqtə onluqdur
        }
        else if (hasComma)
        {
            s = s.Replace(",", ".");
        }
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
