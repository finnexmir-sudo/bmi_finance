using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MaasService : ServiceAsync<Maas, MaasListDto, MaasCreateDto, MaasUpdateDto>, IMaasService
{
    public MaasService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }

    public async Task<Result<IList<MaasDto>>> IsciyeGoreGetirAsync(int isciId)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Maas>()
                .HamisiniGetirAsync(
                    x => x.IsciId == isciId,
                    q => q.Include(x => x.Detallar).ThenInclude(d => d.MaasNovu),
                    true);

            var dtos = _mapper.Map<IList<MaasDto>>(entities);
            return Result<IList<MaasDto>>.Ok(dtos);
        }
        catch
        {
            return Result<IList<MaasDto>>.Fail("Isci maas melumatları getirilirken xeta bas verdi.");
        }
    }

    public async Task<Result<MaasDto?>> IsciAyUzreGetirAsync(int isciId, int il, int ay)
    {
        try
        {
            var entity = await _unitOfWork.Repository<Maas>()
                .GetirAsync(
                    x => x.IsciId == isciId && x.Il == il && x.Ay == ay,
                    q => q.Include(x => x.Detallar).ThenInclude(d => d.MaasNovu),
                    true);

            if (entity == null)
                return Result<MaasDto?>.Fail("Bu ay ucun maas tapilmadi.");

            var dto = _mapper.Map<MaasDto>(entity);
            return Result<MaasDto?>.Ok(dto);
        }
        catch
        {
            return Result<MaasDto?>.Fail("Maas melumatı getirilirken xeta bas verdi.");
        }
    }

    public async Task<Result> StatusDeyisAsync(int maasId, MaasStatus yeniStatus)
    {
        try
        {
            var repo = _unitOfWork.Repository<Maas>();
            var entity = await repo.IdIleGetirAsync(maasId);

            if (entity == null)
                return Result.Fail("Maas tapilmadi.");

            // Tesdiqlenmis maas Layihe-ye qaytarila bilmez
            if (entity.Status == MaasStatus.Tesdiqlendi && yeniStatus == MaasStatus.Layihe)
                return Result.Fail("Tesdiqlenmis maas Layihe statusuna qaytarila bilmez.");

            // Odenilmis maas artiq deyisdirile bilmez
            if (entity.Status == MaasStatus.Odenildi)
                return Result.Fail("Odenilmis maasda status deyisikliyine icaze verilmir.");

            entity.Status = yeniStatus;

            if (yeniStatus == MaasStatus.Tesdiqlendi)
                entity.TesdiqTarixi = DateTime.UtcNow;

            if (yeniStatus == MaasStatus.Odenildi)
                entity.OdenisTarixi = DateTime.UtcNow;

            await repo.YenileAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Result.Ok("Maas statusu ugurla deyisdirildi.");
        }
        catch
        {
            return Result.Fail("Status deyisdiriilirken xeta bas verdi.");
        }
    }
}
