using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Sorgular;
using FinNex.Application.Interfaces.Sorgular;
using FinNex.Domain.Entities.Sorgular;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FinNex.Application.Services.Sorgular;

public class OracleSorguService : IOracleSorguService
{
    private readonly IUnitOfWork _uow;

    public OracleSorguService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IList<OracleSorguDto>>> HamisiniGetirAsync()
    {
        var list = await _uow.Repository<OracleSorgu>().Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Departament)
            .OrderBy(x => x.Departament.Ad).ThenBy(x => x.SorguAdi)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<IList<OracleSorguDto>>.Ok(list);
    }

    public async Task<Result<IList<OracleSorguDto>>> HamisiniGetirAsync(
        Expression<Func<OracleSorgu, bool>>? predicate = null,
        Func<IQueryable<OracleSorgu>, IIncludableQueryable<OracleSorgu, object>>? include = null,
        bool izlemeden = true)
    {
        var query = _uow.Repository<OracleSorgu>().Query()
            .Where(x => !x.Silinib);

        if (predicate != null) query = query.Where(predicate);

        var list = await query
            .Include(x => x.Departament)
            .OrderBy(x => x.Departament.Ad).ThenBy(x => x.SorguAdi)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<IList<OracleSorguDto>>.Ok(list);
    }

    public async Task<Result<bool>> MovcuddurmuAsync(Expression<Func<OracleSorgu, bool>> predicate)
    {
        var exists = await _uow.Repository<OracleSorgu>().MovcuddurmuAsync(predicate);
        return Result<bool>.Ok(exists);
    }

    public async Task<Result<OracleSorguDto?>> IdIleGetirAsync(int id)
    {
        var entity = await _uow.Repository<OracleSorgu>().Query()
            .Where(x => x.Id == id && !x.Silinib)
            .Include(x => x.Departament)
            .FirstOrDefaultAsync();

        if (entity == null) return Result<OracleSorguDto?>.Fail("Sorğu tapılmadı.");
        return Result<OracleSorguDto?>.Ok(ToDto(entity));
    }

    public async Task<Result<OracleSorguDto>> YaratAsync(OracleSorguCreateDto dto)
    {
        var entity = new OracleSorgu
        {
            SorguAdi = dto.SorguAdi.Trim(),
            Mahiyyet = dto.Mahiyyet?.Trim(),
            SorguMetni = dto.SorguMetni.Trim(),
            Aktiv = dto.Aktiv,
            DepartamentId = dto.DepartamentId,
            YaradilmaTarixi = DateTime.Now
        };

        await _uow.Repository<OracleSorgu>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();

        var created = await IdIleGetirAsync(entity.Id);
        return Result<OracleSorguDto>.Ok(created.Data!);
    }

    public async Task<Result> YenileAsync(OracleSorguUpdateDto dto)
    {
        var entity = await _uow.Repository<OracleSorgu>().Query()
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib);

        if (entity == null) return Result.Fail("Sorğu tapılmadı.");

        entity.SorguAdi = dto.SorguAdi.Trim();
        entity.Mahiyyet = dto.Mahiyyet?.Trim();
        entity.SorguMetni = dto.SorguMetni.Trim();
        entity.Aktiv = dto.Aktiv;
        entity.DepartamentId = dto.DepartamentId;
        entity.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<OracleSorgu>().YenileAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok();
    }

    public async Task<Result> SilAsync(int id)
    {
        var entity = await _uow.Repository<OracleSorgu>().Query()
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

        if (entity == null) return Result.Fail("Sorğu tapılmadı.");

        entity.Silinib = true;
        entity.SilinmeTarixi = DateTime.Now;

        await _uow.Repository<OracleSorgu>().YenileAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok();
    }

    public async Task<Result<List<OracleSorguDto>>> DepartamenteSoruBaxAsync(int departamentId)
    {
        var list = await _uow.Repository<OracleSorgu>().Query()
            .Where(x => !x.Silinib && x.Aktiv && x.DepartamentId == departamentId)
            .Include(x => x.Departament)
            .OrderBy(x => x.SorguAdi)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<OracleSorguDto>>.Ok(list);
    }

    private static OracleSorguDto ToDto(OracleSorgu x) => new()
    {
        Id = x.Id,
        SorguAdi = x.SorguAdi,
        Mahiyyet = x.Mahiyyet,
        SorguMetni = x.SorguMetni,
        Aktiv = x.Aktiv,
        DepartamentId = x.DepartamentId,
        DepartamentAd = x.Departament?.Ad ?? "",
        YaradilmaTarixi = x.YaradilmaTarixi
    };
}
