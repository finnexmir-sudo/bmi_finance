using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.Maas_If
{
    public interface IMaasService : IServiceAsync<Maas, MaasListDto, MaasCreateDto, MaasUpdateDto>
    {
        Task<Result<IList<MaasDto>>> IsciyeGoreGetirAsync(int isciId);
        Task<Result<MaasDto?>> IsciAyUzreGetirAsync(int isciId, int il, int ay);
        Task<Result> StatusDeyisAsync(int maasId, MaasStatus yeniStatus);
    }
}