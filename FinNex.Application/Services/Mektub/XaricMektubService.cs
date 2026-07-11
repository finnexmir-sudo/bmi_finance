using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Mektub;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Mektub;

public class XaricMektubService : IXaricMektubService
{
    private readonly IUnitOfWork _uow;

    public XaricMektubService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // QEY_NOM mətn ola bilər (köhnə data) — nömrələmə üçün rəqəm hissəsini oxu
    private static int ParseNum(string? s) => int.TryParse(s?.Trim(), out var n) ? n : 0;

    public async Task<IList<XaricMektubListDto>> HamisiniGetirAsync(int? il = null)
    {
        var list = await _uow.Repository<XaricMektub>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && (il == null || x.Il == il),
            izlemeden: true);

        return list
            .OrderByDescending(x => x.Il).ThenByDescending(x => ParseNum(x.QeyNom))
            .Select(x => new XaricMektubListDto
            {
                Id        = x.Id,
                QeyNom    = x.QeyNom,
                Tarix     = x.Tarix,
                GonYer    = x.GonYer,
                QisaMez   = x.QisaMez,
                Icraci    = x.Icraci,
                Il        = x.Il,
                YaradanId = x.YaradanIcraciId,
                FaylYolu  = x.FaylYolu,
                FaylVar   = !string.IsNullOrEmpty(x.FaylYolu)
            })
            .ToList();
    }

    public async Task<Result<int>> YaratAsync(XaricMektubCreateDto dto, int yaradanUserId, string? faylYolu = null)
    {
        // İcraçı adı — cari istifadəçinin işçisindən (Isci.AppUserId → TamAd)
        var isci = (await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        var icraciAd = isci?.TamAd;
        if (!string.IsNullOrEmpty(icraciAd) && icraciAd.Length > 50)
            icraciAd = icraciAd.Substring(0, 50);   // ICRACI NVARCHAR(50)

        var il = dto.Tarix?.Year ?? DateTime.Now.Year;

        // İl üzrə növbəti Qeydiyyat № (yüklənən data + yeni qeydlərdən max+1)
        var heminIl = await _uow.Repository<XaricMektub>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.Il == il, izlemeden: true);
        var novbeti = heminIl
            .Select(x => ParseNum(x.QeyNom))
            .DefaultIfEmpty(0)
            .Max() + 1;

        var entity = new XaricMektub
        {
            QeyNom     = novbeti.ToString(),
            GonYer     = dto.GonYer?.Trim(),
            Tarix      = dto.Tarix,
            QisaMez    = dto.QisaMez?.Trim(),
            Icraci     = icraciAd,
            MektubMetn = string.IsNullOrWhiteSpace(dto.MektubMetn) ? null : dto.MektubMetn.Trim(),
            Il         = il,
            FaylYolu   = string.IsNullOrWhiteSpace(faylYolu) ? null : faylYolu,
            YaradanIcraciId = yaradanUserId
        };

        await _uow.Repository<XaricMektub>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();

        return Result<int>.Ok(novbeti, $"Xaric məktub qeydə alındı — Qeydiyyat № {novbeti}/{il}.");
    }
}
