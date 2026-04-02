using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;

public interface IIsciService : IServiceAsync<Isci, IsciListDto, IsciCreateDto, IsciUpdateDto>
{
    Task<IsciDetailDto?> GetIsciDetailsAsync(int id);
    Task<IList<IsciListDto>> GetIscilerBySobeIdAsync(int sobeId);
    Task<bool> CheckFinExistsAsync(string fin);
    Task<Result<List<IsciListDto>>> SearchIscilerByFinAsync(string fin);
    Task<Result> UpdateSalaryWithHistoryAsync(int isciId, decimal yeniMaas, string emrNo, string? sebeb = null);
    Task<Result<IList<IsciMaasTarixcesiDto>>> GetMaasTarixcesiAsync(int isciId);
    Task<Result> TeyinatDeyisAsync(int isciId, int departamentId, int vezifeId, DateTime baslamaTarixi);
    Task<Result<int?>> GetAktivDepartamentIdAsync(int isciId);
}