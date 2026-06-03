using FinNex.Application.DTOs.HR.Tabel;

namespace FinNex.Application.Services.HR
{
    public interface ITabelService
    {
        Task<TabelAyDto> GenerateTabelAsync(int il, int ay);
    }
}
