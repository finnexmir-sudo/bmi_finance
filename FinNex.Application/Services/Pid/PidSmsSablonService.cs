using FinNex.Application.Interfaces.Pid;
using FinNex.Domain.Entities.Pid;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Pid;

public class PidSmsSablonService : IPidSmsSablonService
{
    private readonly IUnitOfWork _uow;

    public PidSmsSablonService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<PidSmsSablon>> HamisiniGetirAsync(bool yalnizAktiv = false)
    {
        var q = _uow.Repository<PidSmsSablon>().Query();
        if (yalnizAktiv) q = q.Where(x => x.Aktiv);
        return await q.OrderBy(x => x.Ad).ToListAsync();
    }

    public Task<PidSmsSablon?> IdIleGetirAsync(int id) =>
        _uow.Repository<PidSmsSablon>().IdIleGetirAsync(id);

    public async Task<PidSmsSablon> YaratAsync(string ad, string metn, string? aciqlama, int yaradanIsciId)
    {
        var s = new PidSmsSablon
        {
            Ad = ad.Trim(),
            Metn = metn.Trim(),
            Aciqlama = string.IsNullOrWhiteSpace(aciqlama) ? null : aciqlama.Trim(),
            Aktiv = true,
            YaradanIcraciId = yaradanIsciId,
            YaradilmaTarixi = DateTime.Now
        };
        await _uow.Repository<PidSmsSablon>().YaratAsync(s);
        await _uow.YaddaSaxlaAsync();
        return s;
    }

    public async Task<bool> YenileAsync(int id, string ad, string metn, string? aciqlama, bool aktiv, int yenileyenIsciId)
    {
        var s = await _uow.Repository<PidSmsSablon>().IdIleGetirAsync(id);
        if (s == null || s.Silinib) return false;
        s.Ad = ad.Trim();
        s.Metn = metn.Trim();
        s.Aciqlama = string.IsNullOrWhiteSpace(aciqlama) ? null : aciqlama.Trim();
        s.Aktiv = aktiv;
        s.YenileyenIcraciId = yenileyenIsciId;
        s.YenilenmeTarixi = DateTime.Now;
        await _uow.Repository<PidSmsSablon>().YenileAsync(s);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<bool> SilAsync(int id, int silenIsciId)
    {
        var s = await _uow.Repository<PidSmsSablon>().IdIleGetirAsync(id);
        if (s == null || s.Silinib) return false;
        s.Silinib = true;
        s.SilenIcraciId = silenIsciId;
        s.SilinmeTarixi = DateTime.Now;
        await _uow.Repository<PidSmsSablon>().YenileAsync(s);
        await _uow.YaddaSaxlaAsync();
        return true;
    }
}
