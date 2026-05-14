using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication;

public interface IGelenMailService
{
    Task<List<GelenMailListDto>> GetListAsync(bool? oxunmamis = null, int? tapalanIsciId = null, int page = 1, int pageSize = 50);
    Task<GelenMailDetailDto?> GetDetailAsync(int id);
    Task OxunduIsareEtAsync(int id);
    Task<bool> TapaAsync(int mailId, int isciId, string? qeyd, int rehberUserId);
    Task<int?> SenedeCevir(int mailId, int qosmaId, int yaradanUserId, string saxlamaKlasoru);
    Task<int> GetOxunmamisSayAsync();
    Task SaveAIXulaseAsync(int id, string xulase);
}
