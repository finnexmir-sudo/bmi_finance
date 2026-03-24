using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.MaasNovuDtos;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    public interface IMaasDetayService
        : IServiceAsync<MaasDetay, MaasDetayDto, CreateMaasDetayDto, UpdateMaasDetayDto>
    {
        Task<Result<IList<MaasDetayDto>>> MaasIdIleGetirAsync(int maasId);
    }
}