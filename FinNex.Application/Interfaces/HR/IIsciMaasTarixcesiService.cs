using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    public interface IIsciMaasTarixcesiService
        : IServiceAsync<IsciMaasTarixcesi, DTOs.HR.Isci.IsciMaasTarixcesiDto, CreateIsciMaasTarixcesiDto, UpdateIsciMaasTarixcesiDto>
    {
        Task<Result<IList<DTOs.HR.Isci.IsciMaasTarixcesiDto>>> IsciyeGoreGetirAsync(int isciId);
    }
}