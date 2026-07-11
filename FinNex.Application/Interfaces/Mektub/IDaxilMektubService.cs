using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;

namespace FinNex.Application.Interfaces.Mektub;

public interface IDaxilMektubService
{
    // Daxil olan məktublar (istəyə görə il üzrə), icraçı adı ilə
    Task<IList<DaxilMektubListDto>> HamisiniGetirAsync(int? il = null);

    // Yeni məktub — Qeydiyyat № il üzrə avtomatik. yaradanUserId (AppUser id) həm sahiblik,
    // həm də icraçı nömrəsini (Isci.IcraciNo) tapmaq üçün istifadə olunur. Qaytarır: yeni Qeydiyyat №.
    Task<Result<int>> YaratAsync(DaxilMektubCreateDto dto, int yaradanUserId);
}
