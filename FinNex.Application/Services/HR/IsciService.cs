using AutoMapper;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.Interfaces;
using FinNex.Application.Services;
using FinNex.DataAccess.UnitOfWorks;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class IsciService
    : ServiceAsync<Isci, IsciListDto, IsciCreateDto, IsciUpdateDto>, IIsciService
{
    public IsciService(IUnitOfWork unitOfWork, IMapper mapper)
        : base(unitOfWork, mapper)
    {
    }

    public async Task<IsciDetailDto?> DetailAsync(int id)
    {
        var entity = await _unitOfWork.Repository<Isci>()
            .GetirAsync(
                x => x.Id == id,
                include: q => q
                    .Include(x => x.Sobe)
                    .Include(x => x.Vezife)
            );

        return _mapper.Map<IsciDetailDto>(entity);
    }

    public async Task<IList<IsciListDto>> SobeUzreAsync(int sobeId)
    {
        var entities = await _unitOfWork.Repository<Isci>()
            .HamisiniGetirAsync(
                x => x.SobeId == sobeId,
                include: q => q
                    .Include(x => x.Sobe)
                    .Include(x => x.Vezife),
                izlemeden: true
            );

        return _mapper.Map<IList<IsciListDto>>(entities);
    }

    public async Task<bool> FINMovcuddurmuAsync(string fin)
    {
        return await _unitOfWork.Repository<Isci>()
            .MovcuddurmuAsync(x => x.FIN == fin);
    }
}
