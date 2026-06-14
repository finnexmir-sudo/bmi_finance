using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Pid;
using FinNex.Domain.Entities.Pid;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FinNex.Application.Services.Pid;

public class OdenisNezaretiService : IOdenisNezaretiService
{
    private readonly IUnitOfWork _uow;
    public OdenisNezaretiService(IUnitOfWork uow) => _uow = uow;

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
}
