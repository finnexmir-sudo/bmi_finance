using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Vezife;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;

public interface IVezifeService
    : IServiceAsync<Vezife, VezifeListDto, VezifeCreateDto, VezifeUpdateDto>
{
    Task<IList<VezifeListDto>> AktivOlanlarAsync();
    Task<bool> AdMovcuddurmuAsync(string ad);
    Task<Vezife?> SilinmisAdIleGetirAsync(string ad);
    Task<Result> BerpaEtAsync(int id);
}