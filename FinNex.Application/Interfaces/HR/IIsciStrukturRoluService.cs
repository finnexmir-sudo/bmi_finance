using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.IsciStrukturRolu;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces
{
    public interface IIsciStrukturRoluService
        : IServiceAsync<FinNex.Domain.Entities.HR.IsciStrukturRolu, IsciStrukturRoluDto, IsciStrukturRoluCreateDto, IsciStrukturRoluUpdateDto>
    {
        Task<Result<IList<IsciStrukturRoluDto>>> GetByIsciIdAsync(int isciId);
        Task<Result<IList<IsciStrukturRoluDto>>> GetByRolAsync(StrukturRolTipi rolTipi);
        Task<Result<IList<IsciStrukturRoluDto>>> GetByDepartamentRolAsync(int departamentId, StrukturRolTipi rolTipi);
    }
}