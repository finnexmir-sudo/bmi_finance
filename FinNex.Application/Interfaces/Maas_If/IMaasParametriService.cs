using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.MaasNovuDtos;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    public interface IMaasParametriService
        : IServiceAsync<MaasParametri, MaasParametriDto, CreateMaasParametriDto, UpdateMaasParametriDto>
    {
        Task<Result<IList<MaasParametriDto>>> AktivOlanlariGetirAsync();
        Task<Result<MaasParametriDto?>> NoveGoreGetirAsync(MaasParametrNovu nov, DateTime tarix);
    }
}