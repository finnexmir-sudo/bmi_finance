using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Vezife;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;

public interface IVezifeService
    : IServiceAsync<Vezife, VezifeListDto, VezifeCreateDto, VezifeUpdateDto>
{
    Task<IList<VezifeListDto>> AktivOlanlarAsync();
    Task<bool> AdMovcuddurmuAsync(string ad, int departamentId);
    Task<Vezife?> SilinmisAdIleGetirAsync(string ad, int departamentId);
    Task<Result> BerpaEtAsync(int id);
    Task<IList<VezifeListDto>> DepartamentUzreGetirAsync(int departamentId);
}