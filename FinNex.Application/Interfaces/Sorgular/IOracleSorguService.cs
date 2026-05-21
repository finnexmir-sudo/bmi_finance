using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Sorgular;
using FinNex.Domain.Entities.Sorgular;

namespace FinNex.Application.Interfaces.Sorgular;

public interface IOracleSorguService : IServiceAsync<OracleSorgu, OracleSorguDto, OracleSorguCreateDto, OracleSorguUpdateDto>
{
    Task<Result<List<OracleSorguDto>>> DepartamenteSoruBaxAsync(int departamentId);
}
