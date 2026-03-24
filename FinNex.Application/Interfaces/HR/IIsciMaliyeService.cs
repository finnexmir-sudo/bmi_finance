using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.IsciMaliyeDtos;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    public interface IIsciMaliyeService : IServiceAsync<IsciMaliye, IsciMaliyeDto, CreateIsciMaliyeDto, UpdateIsciMaliyeDto>
    {
        Task<Result<IsciMaliyeDto?>> IsciIdIleGetirAsync(int isciId);
    }
}