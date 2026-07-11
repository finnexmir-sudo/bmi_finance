using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Hevale;

namespace FinNex.Application.Interfaces.Hevale;

public interface IGedenHevaleService
{
    // Gedən həvalələr (istəyə görə il üzrə — Tarix ilinə görə)
    Task<IList<GedenHevaleListDto>> HamisiniGetirAsync(int? il = null);

    // Yeni həvalə — Həvalə № il üzrə avtomatik. yaradanUserId (AppUser id) həm sahiblik,
    // həm də icraçı nömrəsini (Isci.IcraciNo) tapmaq üçün. Qaytarır: yeni Həvalə №.
    Task<Result<string>> YaratAsync(GedenHevaleCreateDto dto, int yaradanUserId, string? faylYolu = null);

    Task<GedenHevaleEditDto?> RedakteMelumatiAsync(int id);
    Task<Result> YenileAsync(GedenHevaleEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null);
    Task<Result> SilAsync(int id, int userId, bool isAdmin);
}
