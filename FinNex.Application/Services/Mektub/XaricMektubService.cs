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

    public async Task<XaricMektubEditDto?> RedakteMelumatiAsync(int id)
    {
        var e = await _uow.Repository<XaricMektub>().GetirAsync(
            x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new XaricMektubEditDto
        {
            Id             = e.Id,
            Tarix          = e.Tarix,
            GonYer         = e.GonYer,
            QisaMez        = e.QisaMez,
            MektubMetn     = e.MektubMetn,
            QeyNom         = e.QeyNom,
            Il             = e.Il,
            MovcudFaylYolu = e.FaylYolu,
            YaradanId      = e.YaradanIcraciId
        };
    }

    public async Task<Result> YenileAsync(XaricMektubEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.GonYer))
            return Result.Fail("Göndərilən yer (təyinat) boş ola bilməz.");

        var e = await _uow.Repository<XaricMektub>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Məktub tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin dəyişə bilər.");

        // Qeydiyyat № (QeyNom) və İl dəyişməz qalır — jurnal nömrəsi yaradılışda təyin olunur
        e.Tarix      = dto.Tarix;
        e.GonYer     = dto.GonYer?.Trim();
        e.QisaMez    = dto.QisaMez?.Trim();
        e.MektubMetn = string.IsNullOrWhiteSpace(dto.MektubMetn) ? null : dto.MektubMetn.Trim();
        if (!string.IsNullOrWhiteSpace(yeniFaylYolu))
            e.FaylYolu = yeniFaylYolu;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<XaricMektub>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Məktub yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<XaricMektub>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Məktub tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin silə bilər.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<XaricMektub>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Məktub silindi.");
    }
}
