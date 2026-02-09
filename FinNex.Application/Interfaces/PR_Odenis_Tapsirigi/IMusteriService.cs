using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.Application.Interfaces.PR_Odenis_Tapsirigi
{
    public interface IMusteriService : IServiceAsync<Musteri, MusteriDetailDto, MusteriCreateDto, MusteriUpdateDto>
    {
    }
}
