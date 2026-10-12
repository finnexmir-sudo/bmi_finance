using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication;

public interface IGedenMailService
{
    Task<List<GedenMailListDto>> GetListAsync(int? sahibUserId = null, string? axtaris = null, DateTime? tarixden = null, DateTime? tarixe = null, bool? qosmali = null, int page = 1, int pageSize = 50, List<string>? izlenenEmaillar = null);
    Task<GedenMailDetailDto?> GetDetailAsync(int id, int? sahibUserId = null);
    Task SilAsync(int id, int? sahibUserId = null);
}
