using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.DTOs.HR.Vezife;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class VezifeService
    : ServiceAsync<Vezife, VezifeListDto, VezifeCreateDto, VezifeUpdateDto>, IVezifeService
{
    public VezifeService(IUnitOfWork uow, IMapper mapper)
        : base(uow, mapper)
    {
    }

    public async Task<IList<VezifeListDto>> AktivOlanlarAsync()
    {
        var entities = await _unitOfWork.Repository<Vezife>()
            .HamisiniGetirAsync(x => x.Aktivdir, izlemeden: true);

        return _mapper.Map<IList<VezifeListDto>>(entities);
    }

    // Yalnız aktiv (silinməmiş) qeydlər arasında yoxlayır
    public async Task<bool> AdMovcuddurmuAsync(string ad)
    {
        return await _unitOfWork.Repository<Vezife>()
            .MovcuddurmuAsync(x => x.Ad == ad && !x.Silinib);
    }

    // Silinmiş qeydlər arasında eyni adlı tapır
    public async Task<Vezife?> SilinmisAdIleGetirAsync(string ad)
    {
        return await _unitOfWork.Repository<Vezife>()
            .GetirAsync(x => x.Ad == ad && x.Silinib);
    }

    // Silinmiş vəzifəni bərpa edir
    public async Task<Result> BerpaEtAsync(int id)
    {
        var vezife = await _unitOfWork.Repository<Vezife>()
            .GetirAsync(x => x.Id == id && x.Silinib);

        if (vezife == null)
            return Result.Fail("Silinmiş vəzifə tapılmadı.");

        vezife.Silinib = false;
        vezife.SilinmeTarixi = null;

        await _unitOfWork.Repository<Vezife>().YenileAsync(vezife);
        await _unitOfWork.YaddaSaxlaAsync();

        return Result.Ok("Vəzifə uğurla bərpa edildi.");
    }

    // Override — base Result<VezifeListDto> qaytarır, biz də eyni tipi saxlayırıq
    public override async Task<Result<VezifeListDto>> YaratAsync(VezifeCreateDto dto)
    {
        // 1. Aktiv qeyddə eyni ad var?
        if (await AdMovcuddurmuAsync(dto.Ad))
            return Result<VezifeListDto>.Fail("Bu adda aktiv vəzifə artıq mövcuddur.");

        // 2. Silinmiş qeyddə eyni ad var?
        var silinmis = await SilinmisAdIleGetirAsync(dto.Ad);
        if (silinmis != null)
            return Result<VezifeListDto>.Fail(
                $"BERPA_TELEB:{silinmis.Id}:" +
                "Bu adda silinmiş vəzifə mövcuddur. Bərpa etmək istəyirsiniz?");

        // 3. Base-i çağır
        return await base.YaratAsync(dto);
    }

    public override async Task<Result<IList<VezifeListDto>>> HamisiniGetirAsync()
    {
        var data = await _unitOfWork
            .Repository<Vezife>()
            .Query()
            .Where(v => !v.Silinib)
            .Select(v => new VezifeListDto
            {
                Id = v.Id,
                Ad = v.Ad,
                DepartamentId = v.DepartamentId,
                DepartamentAd = v.Departament != null ? v.Departament.Ad : "—",
                IsActive = v.Aktivdir,
                IsciSayi = v.IsciTeyinatlar
                    .Count(t => t.BitmeTarixi == null &&
                                t.Isci.Status == IsciStatus.Aktiv)
            })
            .ToListAsync();

        return Result<IList<VezifeListDto>>.Ok(data);
    }
}