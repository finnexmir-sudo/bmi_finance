using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Pid;
using FinNex.Domain.Entities.Pid;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

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
            q = q.Where(x => x.BorcluAd.Contains(a) || (x.KreditHesabi != null && x.KreditHesabi.Contains(a)));
        }

        var list = await q.Include(x => x.Iclaslar)
            .OrderBy(x => x.Sira).ThenByDescending(x => x.Id).AsNoTracking().ToListAsync();

        return list.Select(x => new MehkemeCedvelListDto
        {
            Id = x.Id,
            Sira = x.Sira,
            Status = x.Status,
            BorcluAd = x.BorcluAd,
            KreditNovu = x.KreditNovu,
            KreditHesabi = x.KreditHesabi,
            Subkod = x.Subkod,
            MehkemeyeVerilmeTarixi = x.MehkemeyeVerilmeTarixi,
            MehkemeSenedi = x.MehkemeSenedi,
            QetnameTarixi = x.QetnameTarixi,
            Qeyd = x.Qeyd,
            Iclaslar = x.Iclaslar.Where(i => !i.Silinib).OrderBy(i => i.Tarix)
                .Select(i => new MehkemeCedvelIclasDto { Id = i.Id, Tarix = i.Tarix, Saat = i.Saat, Netice = i.Netice })
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
                Status = d.Status?.Trim(),
                BorcluAd = d.BorcluAd.Trim(),
                KreditNovu = d.KreditNovu?.Trim(),
                MehkemeyeVerilmeTarixi = d.MehkemeyeVerilmeTarixi,
                MehkemeSenedi = d.MehkemeSenedi?.Trim(),
                YaradanIcraciId = isciId,
                YaradilmaTarixi = DateTime.Now
            };
            foreach (var ic in d.Iclaslar)
            {
                e.Iclaslar.Add(new MehkemeCedvelIclas { Tarix = ic.Tarix, Saat = ic.Saat?.Trim(), YaradilmaTarixi = DateTime.Now });
                iclasS++;
            }
            await _uow.Repository<MehkemeCedvel>().YaratAsync(e);
            isS++;
        }
        await _uow.YaddaSaxlaAsync();
        return (isS, iclasS);
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
