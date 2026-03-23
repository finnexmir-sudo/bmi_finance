using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.IsciTeyinat;

namespace FinNex.Application.Interfaces
{
    public interface IIsciTeyinatService
        : IServiceAsync<FinNex.Domain.Entities.HR.IsciTeyinat, IsciTeyinatDto, IsciTeyinatCreateDto, IsciTeyinatUpdateDto>
    {
        Task<Result<IList<IsciTeyinatDto>>> GetByIsciIdAsync(int isciId);
        Task<Result<IsciTeyinatDto?>> GetAktivTeyinatAsync(int isciId);
    }
}