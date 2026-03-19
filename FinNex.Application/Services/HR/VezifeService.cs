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

    public async Task<bool> AdMovcuddurmuAsync(string ad)
    {
        return await _unitOfWork.Repository<Vezife>()
            .MovcuddurmuAsync(x => x.Ad == ad);
    }
    public override async Task<Result<IList<VezifeListDto>>> HamisiniGetirAsync()
    {
        var vezifeler = await _unitOfWork.Repository<Vezife>() .HamisiniGetirAsync(izlemeden: true,include: x=>x.Include(a=>a.Departament));

        var data = _mapper.Map<IList<VezifeListDto>>(vezifeler);

        return Result<IList<VezifeListDto>>.Ok(data);
    }
}
