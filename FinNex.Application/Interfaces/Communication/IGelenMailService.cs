using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication;

public interface IGelenMailService
{
    Task<List<GelenMailListDto>> GetListAsync(bool? oxunmamis = null, int? tapalanIsciId = null, int page = 1, int pageSize = 50, string? axtaris = null, DateTime? tarixden = null, DateTime? tarixe = null, bool? tapshirildi = null, bool? qosmali = null, string? deadlineFilter = null, int? sahibUserId = null, List<string>? izlenenEmaillar = null);
    Task<GelenMailDetailDto?> GetDetailAsync(int id, int? sahibUserId = null);
    Task OxunduIsareEtAsync(int id, int? sahibUserId = null);
    // Hamısını oxundu et (12.10.2026) — nəticə işarələnən say, "0 yeni oxunmamış
    // qalmayıb" kimi mesaj üçün.
    Task<int> HamisiniOxunduIsareEtAsync(int sahibUserId);
    Task<bool> TapaAsync(int mailId, List<int> isciIds, string? qeyd, int rehberUserId);
    Task<int?> SenedeCevir(int mailId, int qosmaId, int yaradanUserId, int? isciId, int? sahibUserId = null);
    Task<int> GetOxunmamisSayAsync(int? sahibUserId = null);
    Task SaveAINeticAsync(int id, AIMailTahlilNetic netic, int? sahibUserId = null);
    Task SilAsync(int id, int? sahibUserId = null);
    Task<List<GelenMailTapshiriqDto>> GetMailTapshiriqlariAsync(int isciId);
    Task<List<MailTapshirilanDto>> GetRehberTapshirilanMaillerAsync(int rehberUserId);
    Task<bool> IcraEtAsync(int mailId, int isciId);
    Task<bool> ImtinaEtAsync(int mailId, int isciId, string? sebeb);
    Task IsciOxuduIsareEtAsync(int mailId, int isciId);
    Task CavabVerildiIsareEtAsync(int mailId, int? sahibUserId = null);

    /// <summary>
    /// Qoşmanı yükləmək üçün fayl məlumatlarını qaytarır.
    /// İşçinin həmin maila tapşırılmış olması yoxlanılır; deyilsə null qaytarılır.
    /// </summary>
    Task<(string FaylYolu, string FaylAdi, string ContentType)?> QosmaGetirAsync(int qosmaId, int isciId);

    /// <summary>
    /// Qoşmanı yükləmək/açmaq üçün fayl məlumatlarını qaytarır — qutunun SAHİBİ
    /// (12.10.2026) üçün (QosmaGetirAsync-dən fərqli: tapşırıq deyil, mülkiyyət yoxlanılır).
    /// Sahib verilən mail-in sahibi deyilsə null qaytarılır.
    /// </summary>
    Task<(string FaylYolu, string FaylAdi, string ContentType)?> QosmaYukleAsync(int qosmaId, int sahibUserId);
}
