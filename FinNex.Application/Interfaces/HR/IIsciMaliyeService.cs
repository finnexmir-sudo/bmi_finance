using FinNex.Application.Common.Results;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    public interface IIsciMaliyeService : IServiceAsync<IsciMaliye, IsciMaliyeDto, CreateIsciMaliyeDto, UpdateIsciMaliyeDto>
    {
        Task<Result<IsciMaliyeDto?>> IsciIdIleGetirAsync(int isciId);
    }
}