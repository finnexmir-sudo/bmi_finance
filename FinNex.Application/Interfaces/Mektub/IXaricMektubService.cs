using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;

namespace FinNex.Application.Interfaces.Mektub;

public interface IXaricMektubService
{
    // Xaric olan məktublar (istəyə görə il üzrə)
    Task<IList<XaricMektubListDto>> HamisiniGetirAsync(int? il = null);

    // Yeni məktub — Qeydiyyat № il üzrə avtomatik. yaradanUserId (AppUser id) həm sahiblik,
    // həm də icraçı adını (Isci.TamAd) tapmaq üçün istifadə olunur.
    // faylYolu — DMS-də nisbi yol (istəyə bağlı qoşma). Qaytarır: yeni Qeydiyyat №.
    Task<Result<int>> YaratAsync(XaricMektubCreateDto dto, int yaradanUserId, string? faylYolu = null);
}
