using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.MaasNovuDtos;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    public interface IMaasNovuService : IServiceAsync<MaasNovu, MaasNovuDto, CreateMaasNovuDto, UpdateMaasNovuDto>
    {
        Task<Result<IList<MaasNovuDto>>> AktivOlanlariGetirAsync();
    }
}