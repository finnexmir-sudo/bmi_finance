using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.Application.Interfaces.PR_Odenis_Tapsirigi
{
    public interface IMusteriHesabiService : IServiceAsync<MusteriHesabi, MusteriHesabiDetailDto, MusteriHesabiCreateDto, MusteriHesabiUpdateDto>
    {
    }
}
