using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.Application.Interfaces.PR_Odenis_Tapsirigi
{
    public interface IMusteriService : IServiceAsync<Musteri, MusteriDetailDto, MusteriCreateDto, MusteriUpdateDto>
    {
        Task<MusteriHesabiDto?> VoenleAxtar(string voen);
        Task YaratAsync(MusteriCreateDto? dto);
    }
}
