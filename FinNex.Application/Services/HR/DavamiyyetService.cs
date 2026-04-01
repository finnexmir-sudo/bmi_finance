using AutoMapper;
using FinNex.Application.DTOs.HR.Davamiyyet;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class DavamiyyetService
    : ServiceAsync<Davamiyyet, DavamiyyetListDto, DavamiyyetCreateDto, DavamiyyetUpdateDto>, IDavamiyyetService
{
    public DavamiyyetService(IUnitOfWork uow, IMapper mapper)
        : base(uow, mapper)
    {
    }

    public async Task<IList<DavamiyyetListDto>> TarixUzreAsync(DateTime tarix)
    {
        var entities = await _unitOfWork.Repository<Davamiyyet>()
            .HamisiniGetirAsync(
                x => x.Tarix.Date == tarix.Date,
                include: q => q.Include(x => x.Isci),
                izlemeden: true);

        return _mapper.Map<IList<DavamiyyetListDto>>(entities);
    }

    public async Task<IList<DavamiyyetListDto>> IsciUzreAsync(int isciId)
    {
        var entities = await _unitOfWork.Repository<Davamiyyet>()
            .HamisiniGetirAsync(
                x => x.IsciId == isciId,
                include: q => q.Include(x => x.Isci),
                izlemeden: true);

        return _mapper.Map<IList<DavamiyyetListDto>>(entities);
    }

    public async Task<bool> BuGunMovcuddurmuAsync(int isciId, DateTime tarix)
    {
        return await _unitOfWork.Repository<Davamiyyet>()
            .MovcuddurmuAsync(x => x.IsciId == isciId && x.Tarix.Date == tarix.Date);
    }
    public async Task<IList<DavamiyyetListDto>> AraliqUzreAsync(DateTime baslangic, DateTime son)
    {
        var entities = await _unitOfWork.Repository<Davamiyyet>()
            .HamisiniGetirAsync(
                x => x.Tarix.Date >= baslangic.Date && x.Tarix.Date <= son.Date,
                include: q => q.Include(x => x.Isci),
                izlemeden: true);

        return _mapper.Map<IList<DavamiyyetListDto>>(entities);
    }
}
