using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;

public interface IMaasService : IServiceAsync<Maas, MaasListDto, MaasCreateDto, MaasUpdateDto>
{
    Task<Result> CalculateMonthlyPayrollAsync(int isciId, int il, int ay);
}