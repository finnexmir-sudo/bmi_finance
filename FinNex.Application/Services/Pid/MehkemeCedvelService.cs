using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Pid;
using FinNex.Domain.Entities.Pid;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FinNex.Application.Services.Pid;

public class MehkemeCedvelService : IMehkemeCedvelService
{
    private readonly IUnitOfWork _uow;
    public MehkemeCedvelService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<MehkemeCedvelListDto>> HamisiniGetirAsync(string? axtaris = null)
    {
        var q = _uow.Repository<MehkemeCedvel>().Query().Where(x => !x.Silinib);

        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var a = axtaris.Trim();
            q = q.Where(x => x.BorcluAd.Contains(a)
                          || (x.MehkemeIsNomresi != null && x.MehkemeIsNomresi.Contains(a)));
        }

        var list = await q.Include(x => x.Iclaslar)
            .OrderBy(x => x.Sira).ThenByDescending(x => x.Id).AsNoTracking().ToListAsync();

        return list.Select(x => new MehkemeCedvelListDto
        {
            Id = x.Id,
            Sira = x.Sira,
            BorcluAd = x.BorcluAd,
            GirovunNovu = x.GirovunNovu,
            MehkemeyeVerilmeTarixi = x.MehkemeyeVerilmeTarixi,
            MehkemeIsNomresi = x.MehkemeIsNomresi,
            Hakim = x.Hakim,
            Iclaslar = x.Iclaslar.Where(i => !i.Silinib).OrderBy(i => i.Tarix)
                .Select(i => new MehkemeCedvelIclasDto { Id = i.Id, Tarix = i.Tarix, Saat = i.Saat })
                .ToList()
        }).ToList();
    }

    public async Task<(int isSayi, int iclasSayi)> ImportAsync(List<MehkemeCedvelImportDto> isler, int isciId)
    {
        if (isler == null || isler.Count == 0) return (0, 0);

        int isS = 0, iclasS = 0;
        foreach (var d in isler)
        {
            if (string.IsNullOrWhiteSpace(d.BorcluAd)) continue;

            var e = new MehkemeCedvel
            {
                Sira = d.Sira,
                BorcluAd = d.BorcluAd.Trim(),
                GirovunNovu = d.GirovunNovu?.Trim(),
                MehkemeyeVerilmeTarixi = d.MehkemeyeVerilmeTarixi,
                MehkemeIsNomresi = d.MehkemeIsNomresi?.Trim(),
                Hakim = d.Hakim?.Trim(),
                YaradanIcraciId = isciId,
                YaradilmaTarixi = DateTime.Now
            };
            foreach (var ic in d.Iclaslar)
            {
                if (ic.Tarix == null && string.IsNullOrWhiteSpace(ic.Saat)) continue;
                e.Iclaslar.Add(new MehkemeCedvelIclas { Tarix = ic.Tarix, Saat = ic.Saat?.Trim(), YaradilmaTarixi = DateTime.Now });
                iclasS++;
            }
            await _uow.Repository<MehkemeCedvel>().YaratAsync(e);
            isS++;
        }
        await _uow.YaddaSaxlaAsync();
        return (isS, iclasS);
    }

    public async Task<int> YaratAsync(MehkemeCedvelCreateDto dto, int isciId)
    {
        var e = new MehkemeCedvel
        {
            Sira = dto.Sira,
            BorcluAd = (dto.BorcluAd ?? "").Trim(),
            GirovunNovu = dto.GirovunNovu?.Trim(),
            MehkemeyeVerilmeTarixi = ParseDate(dto.MehkemeyeVerilmeTarixi),
            MehkemeIsNomresi = dto.MehkemeIsNomresi?.Trim(),
            Hakim = dto.Hakim?.Trim(),
            YaradanIcraciId = isciId,
            YaradilmaTarixi = DateTime.Now
        };

        if (dto.IclasTarix != null)
        {
            for (int i = 0; i < dto.IclasTarix.Count; i++)
            {
                var t = ParseDate(dto.IclasTarix[i]);
                var saat = (dto.IclasSaat != null && i < dto.IclasSaat.Count) ? dto.IclasSaat[i]?.Trim() : null;
                if (t == null && string.IsNullOrWhiteSpace(saat)) continue;
                e.Iclaslar.Add(new MehkemeCedvelIclas { Tarix = t, Saat = saat, YaradilmaTarixi = DateTime.Now });
            }
        }

        await _uow.Repository<MehkemeCedvel>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();
        return e.Id;
    }

    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        string[] f = { "yyyy-MM-dd", "dd.MM.yyyy", "dd-MM-yyyy", "d.M.yyyy" };
        if (DateTime.TryParseExact(s, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
        return null;
    }

    public async Task<bool> SilAsync(int id, int isciId)
    {
        var e = await _uow.Repository<MehkemeCedvel>().Query()
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return false;
        e.Silinib = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = isciId;
        await _uow.Repository<MehkemeCedvel>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return true;
    }
}
