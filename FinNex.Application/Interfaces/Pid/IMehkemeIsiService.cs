using FinNex.Application.DTOs.Oracle;
using FinNex.Application.DTOs.Pid;
using FinNex.Domain.Entities.Pid;
using Microsoft.AspNetCore.Http;

namespace FinNex.Application.Interfaces.Pid;

public interface IMehkemeIsiService
{
    Task<IList<MehkemeIsiListDto>> HamisiniGetirAsync();
    Task<IList<IcraIsiListDto>> IcraIsleriGetirAsync(string? status);
    Task<IList<YaxinlasanGorusDto>> YaxinlasanGoruslerAsync();
    Task<MehkemeIsiDetailDto?> DetailGetirAsync(int id);

    // Monitorinq dashboard — KPI/paylanma/siyahıları yüngül SQL sorğuları ilə hesablayır (Oracle yox)
    Task<MehkemeMonitorDto> MonitorGetirAsync();

    // Kataloq — PID departamentinin aktiv Oracle sorğuları (combobox) + seçilmiş sorğunun xam icrası
    Task<IList<KataloqSecimDto>> KataloqlarAsync();
    Task<OracleNetice> KataloqIcraEtAsync(int sorguId, int maxRows);

    // ── Bildirişə düşənlər (Oracle sorğusu → borcalan + zaminlər) ──
    Task<BildirisViewDto> BildirisSiyahiAsync();
    Task<BildirisSetirDto?> BildirisSetirTapAsync(string sk, string hes);

    // ── Məhkəməyə hazırlıq (Oracle sorğusu → borcalan + zaminlərin ətraflı məlumatı) ──
    Task<MehkemeHazirliqViewDto> MehkemeHazirliqSiyahiAsync();
    Task<MehkemeIsi> YaratAsync(MehkemeIsiCreateDto dto, int yaradanIsciId);
    Task<bool> YenileAsync(int id, MehkemeIsiUpdateDto dto, int yenileyenIsciId);
    Task<bool> MehkemeFazaYenileAsync(int id, MehkemeIsiUpdateDto dto, int yenileyenIsciId);
    Task<bool> IcraFazaYenileAsync(int id, MehkemeIsiUpdateDto dto, int yenileyenIsciId);
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

    // Təkrar zamin qeydlərini (eyni iş + ad) birləşdirir (soft-delete). onizleme=true yalnız sayır.
    Task<(int qrup, int silinen)> ZaminDublikatTemizleAsync(int isciId, bool onizleme);

    // Oracle: müştərinin bütün aktiv kreditlərini qaytarır (sütun adı → dəyər)
    Task<IList<Dictionary<string, string>>> OracleKreditlerGetirAsync(string qeydiyyatNomresi);

    // Yeni siyahı modeli: Oracle-dan bütün problemli kreditlər (canlı) + proqram izləməsi (SQL)
    Task<MehkemeSiyahiResultDto> SiyahiGetirAsync();

    // Excel export: siyahı sorğusunun TAM xam nəticəsi (bütün sütunlar/sətirlər, sıra ilə)
    Task<OracleNetice> SiyahiXamGetirAsync();

    // Qərardad yaz (qeyd yoxdursa kompozit açarla yaradır — upsert)
    Task<int> QerardadYazAsync(MehkemeKreditAcarDto acar, string? qerardad, int isciId);

    // İş aç: kompozit açarla izləmə qeydi yaradır (varsa mövcudu qaytarır)
    Task<MehkemeIsi> IsAchAsync(MehkemeKreditAcarDto acar, int isciId);

    // Excel "Məhkəmə" sheet → MehkemeIsi (arxiv idxalı; təkrarları atlayır)
    Task<(int isSayi, int merheleSayi)> ExcelImportAsync(IList<MehkemeCedvelImportDto> rows, int isciId, bool onizleme = false);

    // Excel "İCRADA OLAN İŞLƏR" sheet → MehkemeIsi (icra fazası) + MehkemeZamin.
    // hesab+subkod ilə dedup; onizleme=true olduqda DB-yə yazmır (yalnız sayır).
    Task<IcraImportNeticeDto> IcraCedvelImportAsync(IList<IcraCedvelImportDto> isler, int isciId, bool onizleme);
}
