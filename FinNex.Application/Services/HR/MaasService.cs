using AutoMapper;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MaasService
    : ServiceAsync<Maas, MaasListDto, MaasCreateDto, MaasUpdateDto>, IMaasService
{
    public MaasService(IUnitOfWork uow, IMapper mapper)
        : base(uow, mapper)
    {
    }

    public async Task<IList<MaasListDto>> IsciUzreAsync(int isciId)
    {
        var entities = await _unitOfWork.Repository<Maas>()
            .HamisiniGetirAsync(
                x => x.IsciId == isciId,
                include: q => q.Include(x => x.Isci),
                izlemeden: true);

        return _mapper.Map<IList<MaasListDto>>(entities);
    }

    public async Task<IList<MaasListDto>> AyUzreAsync(int il, int ay)
    {
        var entities = await _unitOfWork.Repository<Maas>()
            .HamisiniGetirAsync(
                x => x.Il == il && x.Ay == ay,
                include: q => q
                    .Include(x => x.Isci)
                        .ThenInclude(i => i.Sobe)
                    .Include(x => x.Isci)
                        .ThenInclude(i => i.Vezife),
                izlemeden: true);

        return _mapper.Map<IList<MaasListDto>>(entities);
    }

    public async Task<bool> MovcuddurmuAsync(int isciId, int il, int ay)
    {
        return await _unitOfWork.Repository<Maas>()
            .MovcuddurmuAsync(x => x.IsciId == isciId && x.Il == il && x.Ay == ay);
    }
}
