using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Hevale;
using FinNex.Application.Interfaces.Hevale;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Hevale;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Hevale;

public class GelenHevaleService : IGelenHevaleService
{
    private readonly IUnitOfWork _uow;

    public GelenHevaleService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private static int SonReqem(string? hevNom)
    {
        if (string.IsNullOrWhiteSpace(hevNom)) return 0;
        var son = hevNom.Trim().Split('-').LastOrDefault();
        return int.TryParse(son, out var n) ? n : 0;
    }

    private async Task<Dictionary<int, string?>> IcraciAdMapAsync()
    {
        var isciler = await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.IcraciNo != null, izlemeden: true);
        return isciler.Where(i => i.IcraciNo.HasValue)
            .GroupBy(i => i.IcraciNo!.Value)
            .ToDictionary(g => g.Key, g => g.First().TamAd);
    }

    public async Task<IList<GelenHevaleListDto>> HamisiniGetirAsync(int? il = null)
    {
        var list = await _uow.Repository<GelenHevale>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && (il == null || (x.Tarix != null && x.Tarix.Value.Year == il)),
            izlemeden: true);

        var adMap = await IcraciAdMapAsync();

        return list
            .OrderByDescending(x => x.Tarix)
            .ThenByDescending(x => SonReqem(x.HevNom))
            .Select(x => new GelenHevaleListDto
            {
                Id        = x.Id,
                HevNom    = x.HevNom,
                Tarix     = x.Tarix,
                Saa       = x.Saa,
                Mebleg    = x.Mebleg,
                ValTip    = x.ValTip,
                GelOlke   = x.GelOlke,
                AlBank    = x.AlBank,
                Icra      = x.Icra,
                IcraciAd  = (x.Icra.HasValue && adMap.TryGetValue(x.Icra.Value, out var ad)) ? ad : null,
                YaradanId = x.YaradanIcraciId,
                FaylYolu  = x.FaylYolu,
                FaylVar   = !string.IsNullOrEmpty(x.FaylYolu)
            })
            .ToList();
    }

    public async Task<Result<string>> YaratAsync(GelenHevaleCreateDto dto, int yaradanUserId, string? faylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Saa))
            return Result<string>.Fail("Soyad Ad Ata (S.A.A.) boş ola bilməz.");

        var isci = (await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        short? icraNo = (isci?.IcraciNo is int no && no > 0 && no <= short.MaxValue) ? (short)no : (short?)null;

        var tarix = dto.Tarix ?? DateTime.Now;
        var il = tarix.Year;

        var heminIl = await _uow.Repository<GelenHevale>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.Tarix != null && x.Tarix.Value.Year == il, izlemeden: true);
        var novbeti = heminIl.Select(x => SonReqem(x.HevNom)).DefaultIfEmpty(0).Max() + 1;
        var hevNom = $"{il % 100:D2}-{novbeti}";

        var entity = new GelenHevale
        {
            HevNom   = hevNom,
            Tarix    = dto.Tarix,
            Saa      = dto.Saa?.Trim(),
            HesNom   = dto.HesNom?.Trim(),
            TipRes   = dto.TipRes?.Trim(),
            Mebleg   = dto.Mebleg,
            ValTip   = dto.ValTip?.Trim(),
            MenOlke  = dto.MenOlke?.Trim(),
            GelOlke  = dto.GelOlke?.Trim(),
            AlBank   = dto.AlBank?.Trim(),
            HevTip   = dto.HevTip?.Trim(),
            GonTip   = dto.GonTip?.Trim(),
            DecNom   = dto.DecNom?.Trim(),
            Icra     = icraNo,
            FaylYolu = string.IsNullOrWhiteSpace(faylYolu) ? null : faylYolu,
            YaradanIcraciId = yaradanUserId
        };

        await _uow.Repository<GelenHevale>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();

        return Result<string>.Ok(hevNom, $"Gələn həvalə qeydə alındı — № {hevNom}.");
    }

    public async Task<GelenHevaleEditDto?> RedakteMelumatiAsync(int id)
    {
        var e = await _uow.Repository<GelenHevale>().GetirAsync(
            x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new GelenHevaleEditDto
        {
            Id             = e.Id,
            Tarix          = e.Tarix,
            Saa            = e.Saa,
            HesNom         = e.HesNom,
            TipRes         = e.TipRes,
            Mebleg         = e.Mebleg,
            ValTip         = e.ValTip,
            MenOlke        = e.MenOlke,
            GelOlke        = e.GelOlke,
            AlBank         = e.AlBank,
            HevTip         = e.HevTip,
            GonTip         = e.GonTip,
            DecNom         = e.DecNom,
            HevNom         = e.HevNom,
            MovcudFaylYolu = e.FaylYolu,
            YaradanId      = e.YaradanIcraciId
        };
    }

    public async Task<Result> YenileAsync(GelenHevaleEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Saa))
            return Result.Fail("Soyad Ad Ata (S.A.A.) boş ola bilməz.");

        var e = await _uow.Repository<GelenHevale>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Həvalə tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin dəyişə bilər.");

        e.Tarix   = dto.Tarix;
        e.Saa     = dto.Saa?.Trim();
        e.HesNom  = dto.HesNom?.Trim();
        e.TipRes  = dto.TipRes?.Trim();
        e.Mebleg  = dto.Mebleg;
        e.ValTip  = dto.ValTip?.Trim();
        e.MenOlke = dto.MenOlke?.Trim();
        e.GelOlke = dto.GelOlke?.Trim();
        e.AlBank  = dto.AlBank?.Trim();
        e.HevTip  = dto.HevTip?.Trim();
        e.GonTip  = dto.GonTip?.Trim();
        e.DecNom  = dto.DecNom?.Trim();
        if (!string.IsNullOrWhiteSpace(yeniFaylYolu))
            e.FaylYolu = yeniFaylYolu;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<GelenHevale>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Həvalə yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<GelenHevale>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Həvalə tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin silə bilər.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<GelenHevale>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Həvalə silindi.");
    }
}
