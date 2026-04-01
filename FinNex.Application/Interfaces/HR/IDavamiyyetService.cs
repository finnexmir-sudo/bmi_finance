using FinNex.Application.DTOs.HR.Davamiyyet;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;

public interface IDavamiyyetService
    : IServiceAsync<Davamiyyet, DavamiyyetListDto, DavamiyyetCreateDto, DavamiyyetUpdateDto>
{
    Task<IList<DavamiyyetListDto>> TarixUzreAsync(DateTime tarix);

    Task<IList<DavamiyyetListDto>> IsciUzreAsync(int isciId);

    Task<bool> BuGunMovcuddurmuAsync(int isciId, DateTime tarix);
    Task<IList<DavamiyyetListDto>> AraliqUzreAsync(DateTime baslangic, DateTime son);
}

