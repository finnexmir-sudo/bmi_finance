using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi.Icaze;

namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface ISenedDovriyyesiIstifadeciIcazesiService
        : IServiceAsync<
            FinNex.Domain.Entities.SenedDovriyyesi.SenedDovriyyesiIstifadeciIcazesi,
            SenedDovriyyesiIstifadeciIcazesiDto,
            SenedDovriyyesiIstifadeciIcazesiCreateDto,
            SenedDovriyyesiIstifadeciIcazesiUpdateDto>
    {
        Task<Result<IList<SenedDovriyyesiIstifadeciIcazesiDto>>> IstifadeciyeGoreGetirAsync(int istifadeciId);

        Task<Result<bool>> BaxaBilermiAsync(int istifadeciId, int sobeId);

        Task<Result<bool>> TamIcazesiVarmiAsync(int istifadeciId, int sobeId);
    }
}