using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Pid;
using FinNex.Application.Interfaces.Sorgular;
using FinNex.Domain.Entities.Pid;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Pid;

public class MehkemeIsiService : IMehkemeIsiService
{
    private readonly IUnitOfWork _uow;
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorguService;
    private readonly ISistemAyarService _sistemAyar;

    public MehkemeIsiService(
        IUnitOfWork uow,
        IOracleService oracle,
        IOracleSorguService sorguService,
        ISistemAyarService sistemAyar)
    {
        _uow = uow;
        _oracle = oracle;
        _sorguService = sorguService;
        _sistemAyar = sistemAyar;
    }

    public async Task<IList<MehkemeIsiListDto>> HamisiniGetirAsync()
    {
        var list = await _uow.Repository<MehkemeIsi>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Merheleler)
            .OrderByDescending(x => x.YaradilmaTarixi)
            .ToListAsync();

        return list.Select(x => new MehkemeIsiListDto
        {
            Id                = x.Id,
            QeydiyyatNomresi  = x.QeydiyyatNomresi,
            BorcluAd          = x.BorcluAd,
            EsasBorc          = x.EsasBorc,
            MehkemeXerci      = x.MehkemeXerci,
            Nov               = x.Nov,
            Status            = x.Status,
            BaslamaTarixi     = x.BaslamaTarixi,
            MerheleCount      = x.Merheleler.Count(m => !m.Silinib),
            YaradilmaTarixi   = x.YaradilmaTarixi
        }).ToList();
    }

    public async Task<MehkemeIsiDetailDto?> DetailGetirAsync(int id)
    {
        var x = await _uow.Repository<MehkemeIsi>()
            .Query()
            .Where(m => m.Id == id && !m.Silinib)
            .Include(m => m.Merheleler)
            .FirstOrDefaultAsync();

        if (x == null) return null;

        return new MehkemeIsiDetailDto
        {
            Id                = x.Id,
            QeydiyyatNomresi  = x.QeydiyyatNomresi,
            BorcluAd          = x.BorcluAd,
            EsasBorc          = x.EsasBorc,
            MehkemeXerci      = x.MehkemeXerci,
            Nov               = x.Nov,
            Status            = x.Status,
            BaslamaTarixi     = x.BaslamaTarixi,
            Qeyd              = x.Qeyd,
            YaradilmaTarixi   = x.YaradilmaTarixi,
            Merheleler        = x.Merheleler
                .Where(m => !m.Silinib)
                .OrderByDescending(m => m.Tarix)
                .Select(m => new MehkemeMerheleDto
                {
                    Id           = m.Id,
                    MerheleTipi  = m.MerheleTipi,
                    Tarix        = m.Tarix,
                    Hakim        = m.Hakim,
                    IcraciMemur  = m.IcraciMemur,
                    Qeyd         = m.Qeyd,
                    SenedYolu    = m.SenedYolu
                }).ToList()
        };
    }

    public async Task<MehkemeIsi> YaratAsync(MehkemeIsiCreateDto dto, int yaradanIsciId)
    {
        var entity = new MehkemeIsi
        {
            QeydiyyatNomresi = dto.QeydiyyatNomresi.Trim(),
            BorcluAd         = dto.BorcluAd.Trim(),
            EsasBorc         = dto.EsasBorc,
            MehkemeXerci     = dto.MehkemeXerci,
            Nov              = dto.Nov,
            Status           = dto.Status,
            BaslamaTarixi    = dto.BaslamaTarixi,
            Qeyd             = string.IsNullOrWhiteSpace(dto.Qeyd) ? null : dto.Qeyd.Trim(),
            YaradanIcraciId  = yaradanIsciId,
            YaradilmaTarixi  = DateTime.Now
        };
        await _uow.Repository<MehkemeIsi>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return entity;
    }

    public async Task<bool> YenileAsync(int id, MehkemeIsiUpdateDto dto, int yenileyenIsciId)
    {
        var entity = await _uow.Repository<MehkemeIsi>().IdIleGetirAsync(id);
        if (entity == null || entity.Silinib) return false;

        entity.MehkemeXerci      = dto.MehkemeXerci;
        entity.Nov               = dto.Nov;
        entity.Status            = dto.Status;
        entity.BaslamaTarixi     = dto.BaslamaTarixi;
        entity.Qeyd              = string.IsNullOrWhiteSpace(dto.Qeyd) ? null : dto.Qeyd.Trim();
        entity.YenileyenIcraciId = yenileyenIsciId;
        entity.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<MehkemeIsi>().YenileAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<bool> SilAsync(int id, int silenIsciId)
    {
        var entity = await _uow.Repository<MehkemeIsi>().IdIleGetirAsync(id);
        if (entity == null || entity.Silinib) return false;

        entity.Silinib         = true;
        entity.SilenIcraciId   = silenIsciId;
        entity.SilinmeTarixi   = DateTime.Now;

        await _uow.Repository<MehkemeIsi>().YenileAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<MehkemeMerhelesi> MerheleElavEtAsync(
        MehkemeMerheleCreateDto dto,
        IFormFile? fayl,
        string dmsRoot,
        int yaradanIsciId)
    {
        string? senedYolu = null;
        if (fayl != null && fayl.Length > 0)
        {
            var dir = Path.Combine(dmsRoot, "mehkeme");
            Directory.CreateDirectory(dir);
            var ext = Path.GetExtension(fayl.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            await using var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await fayl.CopyToAsync(fs);
            senedYolu = $"mehkeme/{fileName}";
        }

        var m = new MehkemeMerhelesi
        {
            MehkemeIsiId    = dto.MehkemeIsiId,
            MerheleTipi     = dto.MerheleTipi,
            Tarix           = dto.Tarix,
            Hakim           = string.IsNullOrWhiteSpace(dto.Hakim)       ? null : dto.Hakim.Trim(),
            IcraciMemur     = string.IsNullOrWhiteSpace(dto.IcraciMemur) ? null : dto.IcraciMemur.Trim(),
            Qeyd            = string.IsNullOrWhiteSpace(dto.Qeyd)        ? null : dto.Qeyd.Trim(),
            SenedYolu       = senedYolu,
            YaradanIcraciId = yaradanIsciId,
            YaradilmaTarixi = DateTime.Now
        };
        await _uow.Repository<MehkemeMerhelesi>().YaratAsync(m);
        await _uow.YaddaSaxlaAsync();
        return m;
    }

    public async Task<bool> MerheleSilAsync(int merheleId, int silenIsciId)
    {
        var m = await _uow.Repository<MehkemeMerhelesi>().IdIleGetirAsync(merheleId);
        if (m == null || m.Silinib) return false;

        m.Silinib       = true;
        m.SilenIcraciId = silenIsciId;
        m.SilinmeTarixi = DateTime.Now;

        await _uow.Repository<MehkemeMerhelesi>().YenileAsync(m);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<IList<Dictionary<string, string>>> OracleKreditlerGetirAsync(string qeydiyyatNomresi)
    {
        var ayar = await _sistemAyar.GetirAsync();
        if (ayar?.PidMehkemeSorguId == null)
            throw new InvalidOperationException("Sistem ayarlarında məhkəmə Oracle sorğusu seçilməyib.");

        var sorguResult = await _sorguService.IdIleGetirAsync(ayar.PidMehkemeSorguId.Value);
        if (!sorguResult.Success || sorguResult.Data is null || !sorguResult.Data.Aktiv)
            throw new InvalidOperationException("Oracle sorğusu tapılmadı və ya deaktivdir.");

        var sql = sorguResult.Data.SorguMetni.Replace("&nomre", qeydiyyatNomresi.Trim());
        var rows = await _oracle.SelectAsync(sql);
        return rows.Select(r => r.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "")).ToList();
    }

    private static string? GetStr(Dictionary<string, object?> row, string key)
    {
        foreach (var k in row.Keys)
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                return row[k]?.ToString()?.Trim();
        return null;
    }
}
