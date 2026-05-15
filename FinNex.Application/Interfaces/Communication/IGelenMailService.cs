using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication;

public interface IGelenMailService
{
    Task<List<GelenMailListDto>> GetListAsync(bool? oxunmamis = null, int? tapalanIsciId = null, int page = 1, int pageSize = 50, string? axtaris = null, DateTime? tarixden = null, DateTime? tarixe = null, bool? tapshirildi = null, bool? qosmali = null, string? deadlineFilter = null);
    Task<GelenMailDetailDto?> GetDetailAsync(int id);
    Task OxunduIsareEtAsync(int id);
    Task<bool> TapaAsync(int mailId, List<int> isciIds, string? qeyd, int rehberUserId);
    Task<int?> SenedeCevir(int mailId, int qosmaId, int yaradanUserId, int departmentId);
    Task<int> GetOxunmamisSayAsync();
    Task SaveAINeticAsync(int id, AIMailTahlilNetic netic);
    Task SilAsync(int id);
    Task<List<GelenMailTapshiriqDto>> GetMailTapshiriqlariAsync(int isciId);
    Task<List<MailTapshirilanDto>> GetRehberTapshirilanMaillerAsync(int rehberUserId);
}
