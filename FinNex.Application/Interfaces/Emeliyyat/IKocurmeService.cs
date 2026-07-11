using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Emeliyyat;

namespace FinNex.Application.Interfaces.Emeliyyat;

public interface IKocurmeService
{
    // Novu: "Pul" / "Telebe"
    Task<IList<KocurmeListDto>> HamisiniGetirAsync(string novu, int? il = null);

    // Növbəti Həvalə № ({il}-T-{sıra}) — yadda saxlanmadan (form preview)
    Task<string> NovbetiHevaleNoAsync(string novu);

    // Yeni köçürmə — Həvalə № il üzrə avtomatik (növə görə prefiks). Qaytarır: yeni Həvalə №.
    Task<Result<string>> YaratAsync(string novu, KocurmeCreateDto dto, int yaradanUserId);

    // Qeyd + hesablanmış debet/kredit voucher (BMI cevirme məntiqi)
    Task<KocurmeDetalDto?> DetalAsync(int id, string novu);

    // Form dəyərlərindən canlı voucher (yadda saxlanmadan preview)
    IList<MuhasibatSetirDto> VoucherHesabla(KocurmeFormDto dto, string? hevaleNo);

    Task<KocurmeEditDto?> RedakteMelumatiAsync(int id, string novu);
    Task<Result> YenileAsync(string novu, KocurmeEditDto dto, int userId, bool isAdmin);
    Task<Result> SilAsync(int id, int userId, bool isAdmin);
}
