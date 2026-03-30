using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.DTOs.HR.MaasNovuDtos;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    public interface IIsciMaasTarixcesiService
        : IServiceAsync<IsciMaasTarixcesi, DTOs.HR.Isci.IsciMaasTarixcesiDto, DTOs.HR.Maas.CreateIsciMaasTarixcesiDto, DTOs.HR.Maas.UpdateIsciMaasTarixcesiDto>
    {
        Task<Result<IList<DTOs.HR.Isci.IsciMaasTarixcesiDto>>> IsciyeGoreGetirAsync(int isciId);
    }
}