using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;

namespace FinNex.Application.Interfaces.Mektub;

public interface IDaxilMektubService
{
    // Filtrli siyahı. filtr.Il: null → cari il (defolt), 0 → bütün illər.
    // Jurnal 30 mindən çox sətir saxlayır — filtrsiz tam yükləmə QƏSDƏN yoxdur.
    Task<MektubSehifeDto<DaxilMektubListDto>> HamisiniGetirAsync(MektubFiltrDto? filtr = null);

    // Filtr açılan siyahıları — jurnalda REAL mövcud olan illər və icraçı nömrələri.
    Task<MektubFiltrMenbeDto> FiltrMenbeleriAsync();

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
