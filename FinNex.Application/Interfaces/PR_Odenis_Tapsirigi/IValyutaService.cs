using FinNex.Application.DTOs;

namespace FinNex.Application.Interfaces.PR_Odenis_Tapsirigi
{
    public interface IValyutaService
    {
        Task<List<ValyutaListDto>> GetAktivAsync();
    }
}
