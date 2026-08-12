using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Hevale;

namespace FinNex.Application.Interfaces.Hevale;

public interface IGelenHevaleService
{
    // Filtr açılan siyahılarının mənbəyi — jurnalda REAL mövcud illər və icraçı nömrələri
    Task<HevaleFiltrMenbeDto> FiltrMenbeleriAsync();

    // Gələn həvalələr — filtrlənmiş və səhifələnmiş.
    // filtr null olanda cari il götürülür (HevaleFiltrDto.Normalla).
    Task<HevaleSehifeDto<GelenHevaleListDto>> HamisiniGetirAsync(HevaleFiltrDto? filtr = null);

    // Yeni həvalə — Həvalə № il üzrə avtomatik. Qaytarır: yeni Həvalə №.
    Task<Result<string>> YaratAsync(GelenHevaleCreateDto dto, int yaradanUserId, string? faylYolu = null);

    Task<GelenHevaleEditDto?> RedakteMelumatiAsync(int id);
    Task<Result> YenileAsync(GelenHevaleEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null);
    Task<Result> SilAsync(int id, int userId, bool isAdmin);
}
