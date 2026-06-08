using FinNex.Application.DTOs.Pid;
using FinNex.Domain.Entities.Pid;
using Microsoft.AspNetCore.Http;

namespace FinNex.Application.Interfaces.Pid;

public interface IMehkemeIsiService
{
    Task<IList<MehkemeIsiListDto>> HamisiniGetirAsync();
    Task<MehkemeIsiDetailDto?> DetailGetirAsync(int id);
    Task<MehkemeIsi> YaratAsync(MehkemeIsiCreateDto dto, int yaradanIsciId);
    Task<bool> YenileAsync(int id, MehkemeIsiUpdateDto dto, int yenileyenIsciId);
    Task<bool> SilAsync(int id, int silenIsciId);

    Task<MehkemeMerhelesi> MerheleElavEtAsync(MehkemeMerheleCreateDto dto, IFormFile? fayl, string dmsRoot, int yaradanIsciId);
    Task<bool> MerheleSilAsync(int merheleId, int silenIsciId);

    // Oracle: müştərinin bütün aktiv kreditlərini qaytarır (sütun adı → dəyər)
    Task<IList<Dictionary<string, string>>> OracleKreditlerGetirAsync(string qeydiyyatNomresi);
}
