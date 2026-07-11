using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Emeliyyat;

namespace FinNex.Application.Interfaces.Emeliyyat;

public interface IKocurmeService
{
    // Novu: "Pul" / "Telebe"
    Task<IList<KocurmeListDto>> HamisiniGetirAsync(string novu, int? il = null);

    // Yeni köçürmə — Həvalə № il üzrə avtomatik (növə görə prefiks). Qaytarır: yeni Həvalə №.
    Task<Result<string>> YaratAsync(string novu, KocurmeCreateDto dto, int yaradanUserId);

    Task<KocurmeEditDto?> RedakteMelumatiAsync(int id, string novu);
    Task<Result> YenileAsync(string novu, KocurmeEditDto dto, int userId, bool isAdmin);
    Task<Result> SilAsync(int id, int userId, bool isAdmin);
}
