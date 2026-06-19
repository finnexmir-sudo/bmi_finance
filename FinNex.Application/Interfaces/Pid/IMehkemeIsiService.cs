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

    // Məhkəmə xərci (bir işdə bir neçə ola bilər — fərqli məhkəmələr)
    Task<int> XercElaveEtAsync(MehkemeXerciCreateDto dto, int isciId);
    Task<bool> XercSilAsync(int xerciId, int isciId);

    // Sənədlər (işə birbaşa yüklənən)
    Task<int> SenedYukleAsync(MehkemeSenedCreateDto dto, IFormFile fayl, string dmsRoot, int isciId);
    Task<bool> SenedSilAsync(int senedId, int isciId);

    // Zamin (icra subyekti)
    Task<int> ZaminElaveEtAsync(ZaminIcraCreateDto dto, int isciId);
    Task<bool> ZaminYenileAsync(ZaminIcraUpdateDto dto, int isciId);
    Task<bool> ZaminSilAsync(int zaminId, int isciId);
    Task<int> ZaminleriOracledanYukleAsync(int mehkemeIsiId, int isciId);
    Task<int> ZaminleriSnapshotEtAsync(int mehkemeIsiId, List<MehkemeZaminDto> zaminler, int isciId);

    // Oracle: müştərinin bütün aktiv kreditlərini qaytarır (sütun adı → dəyər)
    Task<IList<Dictionary<string, string>>> OracleKreditlerGetirAsync(string qeydiyyatNomresi);

    // Yeni siyahı modeli: Oracle-dan bütün problemli kreditlər (canlı) + proqram izləməsi (SQL)
    Task<MehkemeSiyahiResultDto> SiyahiGetirAsync();

    // Qərardad yaz (qeyd yoxdursa kompozit açarla yaradır — upsert)
    Task<int> QerardadYazAsync(MehkemeKreditAcarDto acar, string? qerardad, int isciId);

    // İş aç: kompozit açarla izləmə qeydi yaradır (varsa mövcudu qaytarır)
    Task<MehkemeIsi> IsAchAsync(MehkemeKreditAcarDto acar, int isciId);
}
