using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;

namespace FinNex.Application.Interfaces.Mektub;

public interface IDaxilMektubService
{
    // Daxil olan məktublar (istəyə görə il üzrə), icraçı adı ilə
    Task<IList<DaxilMektubListDto>> HamisiniGetirAsync(int? il = null);

    // Yeni məktub — Qeydiyyat № il üzrə avtomatik. yaradanUserId (AppUser id) həm sahiblik,
    // həm də icraçı nömrəsini (Isci.IcraciNo) tapmaq üçün istifadə olunur.
    // faylYolu — DMS-də nisbi yol (istəyə bağlı qoşma). Qaytarır: yeni Qeydiyyat №.
    Task<Result<int>> YaratAsync(DaxilMektubCreateDto dto, int yaradanUserId, string? faylYolu = null);

    // Redaktə üçün mövcud dəyərlər (tapılmasa null)
    Task<DaxilMektubEditDto?> RedakteMelumatiAsync(int id);

    // Yenilə — yalnız sahib (YaradanIcraciId) və ya Admin. yeniFaylYolu boşdursa köhnə qoşma qalır.
    Task<Result> YenileAsync(DaxilMektubEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null);

    // Yumşaq sil — yalnız sahib və ya Admin
    Task<Result> SilAsync(int id, int userId, bool isAdmin);
}
