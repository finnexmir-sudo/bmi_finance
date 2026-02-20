using FinNex.Application.DTOs.HR.Davamiyyet;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.DTOs.HR.Vezife;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;

public interface IIsciService
    : IServiceAsync<Isci, IsciListDto, IsciCreateDto, IsciUpdateDto>
{
    Task<IsciDetailDto?> DetailAsync(int id);

    Task<IList<IsciListDto>> SobeUzreAsync(int sobeId);

    Task<bool> FINMovcuddurmuAsync(string fin);
}
public interface IVezifeService
    : IServiceAsync<Vezife, VezifeListDto, VezifeCreateDto, VezifeUpdateDto>
{
    Task<IList<VezifeListDto>> AktivOlanlarAsync();

    Task<bool> AdMovcuddurmuAsync(string ad);
}

public interface IMaasService
    : IServiceAsync<Maas, MaasListDto, MaasCreateDto, MaasUpdateDto>
{
    Task<IList<MaasListDto>> IsciUzreAsync(int isciId);

    Task<IList<MaasListDto>> AyUzreAsync(int il, int ay);

    Task<bool> MovcuddurmuAsync(int isciId, int il, int ay);
}

public interface IDavamiyyetService
    : IServiceAsync<Davamiyyet, DavamiyyetListDto, DavamiyyetCreateDto, DavamiyyetUpdateDto>
{
    Task<IList<DavamiyyetListDto>> TarixUzreAsync(DateTime tarix);

    Task<IList<DavamiyyetListDto>> IsciUzreAsync(int isciId);

    Task<bool> BuGunMovcuddurmuAsync(int isciId, DateTime tarix);
}

public interface IMezuniyyetService
    : IServiceAsync<Mezuniyyet, MezuniyyetListDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>
{
    Task<IList<MezuniyyetListDto>> AktivOlanlarAsync();

    Task<IList<MezuniyyetListDto>> IsciUzreAsync(int isciId);

    Task<bool> TarixToqqusurmuAsync(int isciId, DateTime baslama, DateTime bitme);
}

