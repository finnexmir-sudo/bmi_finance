using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication;

public interface IGedenMailService
{
    Task<List<GedenMailListDto>> GetListAsync(int? sahibUserId = null, string? axtaris = null, DateTime? tarixden = null, DateTime? tarixe = null, bool? qosmali = null, int page = 1, int pageSize = 50, List<string>? izlenenEmaillar = null);
    Task<GedenMailDetailDto?> GetDetailAsync(int id, int? sahibUserId = null);
    Task SilAsync(int id, int? sahibUserId = null);

    /// <summary>
    /// Qoşmanı yükləmək/açmaq üçün fayl məlumatlarını qaytarır — qutunun
    /// SAHİBİ (12.10.2026) üçün. Sahib verilən mail-in sahibi deyilsə null qaytarılır.
    /// </summary>
    Task<(string FaylYolu, string FaylAdi, string ContentType)?> QosmaYukleAsync(int qosmaId, int sahibUserId);
}
