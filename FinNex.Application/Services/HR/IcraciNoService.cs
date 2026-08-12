using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.IcraciNo;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR;

public class IcraciNoService : IIcraciNoService
{
    private readonly IUnitOfWork _uow;

    public IcraciNoService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IList<IcraciNoSetirDto>> HamisiniGetirAsync()
    {
        // İşçi Sıralaması qaydası ilə eyni: yalnız aktiv işçilər, Sira → Ad → Soyad
        var isciler = await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.Status == IsciStatus.Aktiv,
            include: q => q
                .Include(i => i.IsciTeyinatlari).ThenInclude(t => t.Vezife)
                .Include(i => i.IsciTeyinatlari).ThenInclude(t => t.Departament),
            izlemeden: true);

        return isciler
            .OrderBy(i => i.Sira)
            .ThenBy(i => i.Ad).ThenBy(i => i.Soyad)
            .Select(i =>
            {
                var teyinat = i.IsciTeyinatlari.FirstOrDefault(t => t.Aktivdir);
                return new IcraciNoSetirDto
                {
                    IsciId      = i.Id,
                    AdSoyad     = $"{i.Ad} {i.Soyad} {i.AtaAdi}".Trim(),
                    Vezife      = teyinat?.Vezife?.Ad,
                    Departament = teyinat?.Departament?.Ad,
                    Aktivdir    = i.Status == IsciStatus.Aktiv,
                    IcraciNo    = i.IcraciNo
                };
            })
            .ToList();
    }

    public async Task<int?> AppUserIcraciNoAsync(int appUserId)
    {
        var isci = (await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == appUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        return (isci?.IcraciNo is int no && no > 0) ? no : (int?)null;
    }

    public async Task<Result> TopluTeyinEtAsync(IList<IcraciNoTeyinDto> teyinler)
    {
        if (teyinler == null || teyinler.Count == 0)
            return Result.Fail("Məlumat gəlmədi.");

        // Boş/0/mənfi → NULL (silinir); müsbət → təyin olunur
        static int? Normalla(int? n) => (n.HasValue && n.Value > 0) ? n : null;
        var map = teyinler
            .GroupBy(t => t.IsciId)
            .ToDictionary(g => g.Key, g => Normalla(g.Last().IcraciNo));

        // Unikallıq — eyni müsbət nömrə iki işçidə ola bilməz
        var tekrar = map.Values
            .Where(v => v.HasValue)
            .GroupBy(v => v!.Value)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (tekrar.Count > 0)
            return Result.Fail($"Eyni icraçı nömrəsi bir neçə işçiyə verilib: {string.Join(", ", tekrar)}. Hər nömrə yalnız bir işçidə ola bilər.");

        var repo = _uow.Repository<Isci>();
        var isciler = await repo.HamisiniGetirAsync(predicate: x => !x.Silinib);
        var deyisenSay = 0;

        foreach (var isci in isciler)
        {
            if (!map.TryGetValue(isci.Id, out var yeni)) continue;
            if (isci.IcraciNo == yeni) continue;
            isci.IcraciNo = yeni;
            await repo.YenileAsync(isci);
            deyisenSay++;
        }

        if (deyisenSay == 0)
            return Result.Ok("Dəyişiklik yoxdur.");

        try
        {
            await _uow.YaddaSaxlaAsync();
        }
        catch (DbUpdateException)
        {
            // DB-də unikal index — nadir yarış halı
            return Result.Fail("Nömrələr yadda saxlanmadı — icraçı nömrəsi təkrarlana bilər. Yenidən yoxlayın.");
        }

        return Result.Ok($"{deyisenSay} işçinin icraçı nömrəsi yeniləndi.");
    }
}
