using AutoMapper;
using FinNex.Application.DTOs.HR.Vezife;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;

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

    public async Task<bool> AdMovcuddurmuAsync(string ad)
    {
        return await _unitOfWork.Repository<Vezife>()
            .MovcuddurmuAsync(x => x.Ad == ad);
    }
}
