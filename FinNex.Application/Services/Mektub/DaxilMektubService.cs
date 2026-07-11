using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Mektub;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Mektub;

public class DaxilMektubService : IDaxilMektubService
{
    private readonly IUnitOfWork _uow;

    public DaxilMektubService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IList<DaxilMektubListDto>> HamisiniGetirAsync(int? il = null)
    {
        var list = await _uow.Repository<DaxilMektub>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && (il == null || x.Il == il),
            izlemeden: true);

        // İcraçı nömrəsi → işçi adı (Isci.IcraciNo)
        var isciler = await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.IcraciNo != null, izlemeden: true);
        var adMap = isciler.Where(i => i.IcraciNo.HasValue)
            .GroupBy(i => i.IcraciNo!.Value)
            .ToDictionary(g => g.Key, g => g.First().TamAd);

        return list
            .OrderByDescending(x => x.Il).ThenByDescending(x => x.Nom1)
            .Select(x => new DaxilMektubListDto
            {
                Id       = x.Id,
                Nom1     = x.Nom1,
                DaxTarix = x.DaxTarix,
                IdareAdi = x.IdareAdi,
                GonTarix = x.GonTarix,
                DaxNom   = x.DaxNom,
                Il       = x.Il,
                IcraciNo = x.MekUnvan,
                IcraciAd = (x.MekUnvan.HasValue && adMap.TryGetValue(x.MekUnvan.Value, out var ad)) ? ad : null,
                YaradanId = x.YaradanIcraciId,
                FaylVar  = !string.IsNullOrEmpty(x.FaylYolu) || (x.Mezmun != null && x.Mezmun.Length > 0)
            })
            .ToList();
    }

    public async Task<Result<int>> YaratAsync(DaxilMektubCreateDto dto, int yaradanUserId)
    {
        // İcraçı nömrəsi — cari istifadəçinin işçisindən (Isci.AppUserId → IcraciNo)
        var isci = (await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        var icraciNo = isci?.IcraciNo;

        var il = dto.DaxTarix?.Year ?? DateTime.Now.Year;

        // İl üzrə növbəti Qeydiyyat № (yüklənən data + yeni qeydlərdən max+1)
        var heminIl = await _uow.Repository<DaxilMektub>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.Il == il, izlemeden: true);
        var novbeti = heminIl.Where(x => x.Nom1.HasValue)
            .Select(x => x.Nom1!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var entity = new DaxilMektub
        {
            Nom      = novbeti,
            Nom1     = novbeti,
            DaxTarix = dto.DaxTarix,
            IdareAdi = dto.IdareAdi?.Trim(),
            GonTarix = dto.GonTarix,
            DaxNom   = dto.DaxNom?.Trim(),
            MekUnvan = (icraciNo.HasValue && icraciNo.Value > 0) ? icraciNo : (int?)null,
            Il       = il,
            YaradanIcraciId = yaradanUserId
        };

        await _uow.Repository<DaxilMektub>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();

        return Result<int>.Ok(novbeti, $"Məktub qeydə alındı — Qeydiyyat № {novbeti}/{il}.");
    }
}
