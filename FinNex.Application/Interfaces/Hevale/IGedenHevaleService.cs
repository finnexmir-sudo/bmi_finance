using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Hevale;

namespace FinNex.Application.Interfaces.Hevale;

public interface IGedenHevaleService
{
    // Filtr açılan siyahılarının mənbəyi — jurnalda REAL mövcud illər və icraçı nömrələri
    Task<HevaleFiltrMenbeDto> FiltrMenbeleriAsync();

    // Gedən həvalələr — filtrlənmiş və səhifələnmiş.
    // filtr null olanda cari il götürülür (HevaleFiltrDto.Normalla).
    Task<HevaleSehifeDto<GedenHevaleListDto>> HamisiniGetirAsync(HevaleFiltrDto? filtr = null);

    // Yeni həvalə — Həvalə № il üzrə avtomatik. yaradanUserId (AppUser id) həm sahiblik,
    // həm də icraçı nömrəsini (Isci.IcraciNo) tapmaq üçün. Qaytarır: yeni Həvalə №.
    Task<Result<string>> YaratAsync(GedenHevaleCreateDto dto, int yaradanUserId, string? faylYolu = null);

    Task<GedenHevaleEditDto?> RedakteMelumatiAsync(int id);
    Task<Result> YenileAsync(GedenHevaleEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null);
    Task<Result> SilAsync(int id, int userId, bool isAdmin);
}
