using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Kredit;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Application.Interfaces.Kurval;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using DereceEntity = FinNex.Domain.Entities.HR.KreditFaizDerecesi;

namespace FinNex.Application.Services.Kredit;

/// <summary>
/// VM 98.2.1 — bazar faiz dərəcəsi tarixçəsi.
///
/// Dərəcə DƏYİŞMİR, yeni sətir ƏLAVƏ OLUNUR: keçmiş dövr yenidən hesablananda
/// öz vaxtındakı dərəcə tapılsın. Ona görə «cari dərəcə» anlayışı yoxdur —
/// həmişə tarix üzrə axtarılır (<see cref="QuvvededirAsync"/>).
/// </summary>
public class KreditFaizDerecesiService : IKreditFaizDerecesiService
{
    private readonly IUnitOfWork _uow;
    private readonly IBmiValyutaService _valyuta;

    public KreditFaizDerecesiService(IUnitOfWork uow, IBmiValyutaService valyuta)
    {
        _uow = uow;
        _valyuta = valyuta;
    }

    // BMI `kurval` Oracle-dadır; siyahı gəlməsə ad boş qalır, amma DƏRƏCƏ İŞLƏYİR.
    // Oracle əlçatmazlığı yalnız etiketi yoxa çıxarmalıdır, hesablamanı yox.
    private async Task<Dictionary<string, string>> ValyutaAdMapAsync()
    {
        try
        {
            var list = await _valyuta.SiyahiAsync();
            return list
                .Where(v => !string.IsNullOrWhiteSpace(v.Kod))
                .GroupBy(v => v.Kod.Trim())
                .ToDictionary(g => g.Key, g => g.First().Ad, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? Ad(Dictionary<string, string> map, string kod)
    {
        if (kod == DereceEntity.AznKodu) return "AZN";
        return map.TryGetValue(kod, out var ad) ? ad : null;
    }

    public async Task<IList<KreditFaizDerecesiDto>> HamisiniGetirAsync()
    {
        var list = await _uow.Repository<DereceEntity>().Query()
            .AsNoTracking()
            .OrderBy(x => x.ValyutaKodu)
            .ThenByDescending(x => x.Tarix)
            .ToListAsync();

        var map = await ValyutaAdMapAsync();

        return list.Select(x => new KreditFaizDerecesiDto
        {
            Id          = x.Id,
            Tarix       = x.Tarix,
            ValyutaKodu = x.ValyutaKodu,
            ValyutaAdi  = Ad(map, x.ValyutaKodu),
            Derece      = x.Derece,
            Qeyd        = x.Qeyd
        }).ToList();
    }

    public async Task<KreditFaizDerecesiDto?> QuvvededirAsync(DateTime hedef, string valyutaKodu)
    {
        var kod = (valyutaKodu ?? "").Trim();
        if (kod.Length == 0) return null;

        var t = hedef.Date;

        // `Tarix <= hedef` olan ƏN SON sətir. Bərabər tarixli iki sətir olsa
        // sonuncu yazılan (böyük Id) götürülür — mühasib səhvi düzəldəndə
        // eyni günə ikinci sətir yaza bilər.
        var e = await _uow.Repository<DereceEntity>().Query()
            .AsNoTracking()
            .Where(x => x.ValyutaKodu == kod && x.Tarix.Date <= t)
            .OrderByDescending(x => x.Tarix)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (e == null) return null;

        var map = await ValyutaAdMapAsync();

        return new KreditFaizDerecesiDto
        {
            Id          = e.Id,
            Tarix       = e.Tarix,
            ValyutaKodu = e.ValyutaKodu,
            ValyutaAdi  = Ad(map, e.ValyutaKodu),
            Derece      = e.Derece,
            Qeyd        = e.Qeyd
        };
    }

    // Yazma yollarının ortaq validasiyası — biri unudulmasın.
    private static Result? Yoxla(KreditFaizDerecesiCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ValyutaKodu))
            return Result.Fail("Valyuta seçilməlidir.");

        // 0 və mənfi dərəcə düsturda məxrəcdir (faiz × (bazar − dərəcə) / dərəcə) —
        // sıfıra bölmə və mənasız nəticə verər.
        if (dto.Derece <= 0)
            return Result.Fail("Dərəcə sıfırdan böyük olmalıdır.");

        if (dto.Tarix == default)
            return Result.Fail("Tarix seçilməlidir.");

        return null;
    }

    public async Task<Result<int>> YaratAsync(KreditFaizDerecesiCreateDto dto, int userId)
    {
        var xeta = Yoxla(dto);
        if (xeta != null) return Result<int>.Fail(xeta.Message ?? "Məlumat yanlışdır.");

        var e = new DereceEntity
        {
            Tarix           = dto.Tarix.Date,
            ValyutaKodu     = dto.ValyutaKodu.Trim(),
            Derece          = dto.Derece,
            Qeyd            = dto.Qeyd?.Trim(),
            YaradanIcraciId = userId
        };

        await _uow.Repository<DereceEntity>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();

        return Result<int>.Ok(e.Id, $"Dərəcə əlavə edildi — {e.Derece:0.####}% ({e.Tarix:dd.MM.yyyy}-dan).");
    }

    public async Task<Result> YenileAsync(KreditFaizDerecesiCreateDto dto, int userId)
    {
        var xeta = Yoxla(dto);
        if (xeta != null) return xeta;

        var e = await _uow.Repository<DereceEntity>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Dərəcə tapılmadı.");

        e.Tarix             = dto.Tarix.Date;
        e.ValyutaKodu       = dto.ValyutaKodu.Trim();
        e.Derece            = dto.Derece;
        e.Qeyd              = dto.Qeyd?.Trim();
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<DereceEntity>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Dərəcə yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId)
    {
        var e = await _uow.Repository<DereceEntity>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Dərəcə tapılmadı.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<DereceEntity>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Dərəcə silindi.");
    }
}
