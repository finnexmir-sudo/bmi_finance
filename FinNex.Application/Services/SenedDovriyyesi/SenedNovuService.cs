using System.Linq.Expressions;
using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Services;
using FinNex.DataAccess.UnitOfWorks;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

public class SenedNovuService
    : ServiceAsync<SenedNovu, SenedNovuListDto, SenedNovuCreateDto, SenedNovuUpdateDto>,
      ISenedNovuService
{
    public SenedNovuService(IUnitOfWork unitOfWork, IMapper mapper)
        : base(unitOfWork, mapper)
    {
    }

    // ================================
    // Şöbəyə görə sənəd növləri
    // ================================
    public async Task<Result<List<SenedNovuListDto>>> GetByDepartmentAsync(int departmentId)
    {
        var entities = await _unitOfWork.Repository<SenedNovu>()
            .HamisiniGetirAsync(x =>
                x.DepartmentId == departmentId &&
                !x.Silinib);

        var dtoList = _mapper.Map<List<SenedNovuListDto>>(entities);

        return Result<List<SenedNovuListDto>>.Ok(dtoList);
    }

    // ================================
    // Yarat (userId ilə)
    // ================================
    /// <summary>
    /// Ad-dan avtomatik kod generasiya edir: "Müqavilə" → "MUQ"
    /// </summary>
    private string GenerateKod(string ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return "SND";
        // Azərbaycan hərflərini latın əvəzləyicilərinə çevir
        var clean = ad.Trim().ToUpper()
            .Replace("Ə", "E").Replace("Ü", "U").Replace("Ö", "O")
            .Replace("Ş", "S").Replace("Ç", "C").Replace("Ğ", "G")
            .Replace("I", "I").Replace("İ", "I");
        // Yalnız hərfləri saxla
        var letters = new string(clean.Where(char.IsLetter).ToArray());
        return letters.Length >= 3 ? letters.Substring(0, 3) : letters.PadRight(3, 'X');
    }

    public async Task<Result<int>> CreateAsync(SenedNovuCreateDto dto, int userId)
    {
        var kod = string.IsNullOrWhiteSpace(dto.Kod) ? GenerateKod(dto.Ad) : dto.Kod.Trim().ToUpper();

        // Eyni kod varsa sonuna rəqəm əlavə et
        var baseKod = kod;
        int suffix = 1;
        while (await _unitOfWork.Repository<SenedNovu>()
            .MovcuddurmuAsync(x => x.DepartmentId == dto.DepartmentId && x.Kod == kod && !x.Silinib))
        {
            suffix++;
            kod = baseKod + suffix;
        }

        var entity = _mapper.Map<SenedNovu>(dto);
        entity.Kod = kod;
        entity.Ad = dto.Ad.Trim();
        entity.Aktiv = true;
        entity.YaradanIcraciId = userId;

        await _unitOfWork.Repository<SenedNovu>().YaratAsync(entity);
        await _unitOfWork.YaddaSaxlaAsync();

        return Result<int>.Ok(entity.Id, "Sənəd növü yaradıldı.");
    }

    // ================================
    // Redaktə
    // ================================
    public async Task<Result> UpdateNovAsync(int id, string ad, int userId)
    {
        var entity = await _unitOfWork.Repository<SenedNovu>().IdIleGetirAsync(id);
        if (entity == null || entity.Silinib)
            return Result.Fail("Sənəd növü tapılmadı.");

        entity.Ad = ad.Trim();
        entity.YenileyenIcraciId = userId;
        entity.YenilenmeTarixi = DateTime.Now;

        await _unitOfWork.YaddaSaxlaAsync();
        return Result.Ok("Sənəd növü yeniləndi.");
    }

    // ================================
    // Soft Delete
    // ================================
    public async Task<Result> SoftDeleteAsync(int id, int userId)
    {
        var entity = await _unitOfWork.Repository<SenedNovu>()
            .IdIleGetirAsync(id);

        if (entity == null || entity.Silinib)
            return Result.Fail("Sənəd növü tapılmadı.");

        entity.Silinib = true;
        entity.SilinmeTarixi = DateTime.Now;
        entity.SilenIcraciId = userId;

        await _unitOfWork.YaddaSaxlaAsync();

        return Result.Ok("Sənəd növü silindi.");
    }

    // ================================
    // Aktiv / Deaktiv
    // ================================
    public async Task<Result> ToggleAktivAsync(int id, int userId)
    {
        var entity = await _unitOfWork.Repository<SenedNovu>()
            .IdIleGetirAsync(id);

        if (entity == null || entity.Silinib)
            return Result.Fail("Sənəd növü tapılmadı.");

        entity.Aktiv = !entity.Aktiv;
        entity.YenileyenIcraciId = userId;
        entity.YenilenmeTarixi = DateTime.Now;

        await _unitOfWork.YaddaSaxlaAsync();

        var message = entity.Aktiv
            ? "Sənəd növü aktivləşdirildi."
            : "Sənəd növü deaktiv edildi.";

        return Result.Ok(message);
    }

    public override async Task<Result<IList<SenedNovuListDto>>> HamisiniGetirAsync(Expression<Func<SenedNovu, bool>> predicate = null,
    Func<IQueryable<SenedNovu>, IIncludableQueryable<SenedNovu, object>> include = null,
    bool izlemeden = true)
    {
        var list = await _unitOfWork.Repository<SenedNovu>().HamisiniGetirAsync(izlemeden: true, include: a => a.Include(b => b.Department));

        if (list !=null)
        {
            var dtoList = _mapper.Map<List<SenedNovuListDto>>(list);

            return Result<IList<SenedNovuListDto>>.Ok(dtoList);
        }

        return Result<IList<SenedNovuListDto>>.Fail("");
    }
}
