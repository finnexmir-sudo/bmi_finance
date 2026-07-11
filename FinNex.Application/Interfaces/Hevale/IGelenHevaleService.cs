using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Hevale;

namespace FinNex.Application.Interfaces.Hevale;

public interface IGelenHevaleService
{
    // Gələn həvalələr (istəyə görə il üzrə — Tarix ilinə görə)
    Task<IList<GelenHevaleListDto>> HamisiniGetirAsync(int? il = null);

    // Yeni həvalə — Həvalə № il üzrə avtomatik. Qaytarır: yeni Həvalə №.
    Task<Result<string>> YaratAsync(GelenHevaleCreateDto dto, int yaradanUserId, string? faylYolu = null);

    Task<GelenHevaleEditDto?> RedakteMelumatiAsync(int id);
    Task<Result> YenileAsync(GelenHevaleEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null);
    Task<Result> SilAsync(int id, int userId, bool isAdmin);
}
