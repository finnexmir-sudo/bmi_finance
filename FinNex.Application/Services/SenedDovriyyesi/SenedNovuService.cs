using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Services;
using FinNex.DataAccess.UnitOfWorks;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;

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
    public async Task<Result<int>> CreateAsync(SenedNovuCreateDto dto, int userId)
    {
        var exists = await _unitOfWork.Repository<SenedNovu>()
            .MovcuddurmuAsync(x =>
                x.DepartmentId == dto.DepartmentId &&
                x.Kod == dto.Kod.ToUpper() &&
                !x.Silinib);

        if (exists)
            return Result<int>.Fail("Bu kod artıq mövcuddur.");

        var entity = _mapper.Map<SenedNovu>(dto);

        entity.Kod = dto.Kod.Trim().ToUpper();
        entity.Ad = dto.Ad.Trim();
        entity.Aktiv = true;
        entity.YaradanIcraciId = userId;

        await _unitOfWork.Repository<SenedNovu>().YaratAsync(entity);
        await _unitOfWork.YaddaSaxlaAsync();

        return Result<int>.Ok(entity.Id, "Sənəd növü yaradıldı.");
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
}
