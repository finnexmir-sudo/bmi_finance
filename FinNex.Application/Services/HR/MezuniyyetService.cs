using AutoMapper;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MezuniyyetService
    : ServiceAsync<Mezuniyyet, MezuniyyetListDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>, IMezuniyyetService
{
    public MezuniyyetService(IUnitOfWork uow, IMapper mapper)
        : base(uow, mapper)
    {
    }

    public async Task<IList<MezuniyyetListDto>> AktivOlanlarAsync()
    {
        var entities = await _unitOfWork.Repository<Mezuniyyet>()
            .HamisiniGetirAsync(
                x => x.Status == MezuniyyetStatus.Tesdiqlenib &&
                     x.BitmeTarixi >= DateTime.Now,
                include: q => q.Include(x => x.Isci),
                izlemeden: true);

        return _mapper.Map<IList<MezuniyyetListDto>>(entities);
    }

    public async Task<IList<MezuniyyetListDto>> IsciUzreAsync(int isciId)
    {
        var entities = await _unitOfWork.Repository<Mezuniyyet>()
            .HamisiniGetirAsync(
                x => x.IsciId == isciId,
                include: q => q.Include(x => x.Isci),
                izlemeden: true);

        return _mapper.Map<IList<MezuniyyetListDto>>(entities);
    }

    public async Task<bool> TarixToqqusurmuAsync(int isciId, DateTime baslama, DateTime bitme)
    {
        return await _unitOfWork.Repository<Mezuniyyet>()
            .MovcuddurmuAsync(x =>
                x.IsciId == isciId &&
                x.BaslamaTarixi <= bitme &&
                x.BitmeTarixi >= baslama);
    }
}
