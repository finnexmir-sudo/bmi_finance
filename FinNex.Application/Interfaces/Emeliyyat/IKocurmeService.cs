using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Emeliyyat;

namespace FinNex.Application.Interfaces.Emeliyyat;

public interface IKocurmeService
{
    // Novu: "Pul" / "Telebe"
    Task<IList<KocurmeListDto>> HamisiniGetirAsync(string novu, int? il = null);

    // Növbəti Həvalə № ({il}-T-{sıra}) — yadda saxlanmadan (form preview)
    Task<string> NovbetiHevaleNoAsync(string novu);

    // Mövcud köçürməni təkrar üçün: bütün məlumat dolu, yeni Həvalə № + bugünkü tarix
    Task<KocurmeCreateDto?> TekrarMelumatiAsync(int id, string novu);

    // Yeni köçürmə — Həvalə № il üzrə avtomatik (növə görə prefiks). Qaytarır: yeni qeydin Id-si.
    Task<Result<int>> YaratAsync(string novu, KocurmeCreateDto dto, int yaradanUserId);

    // Qeyd + hesablanmış debet/kredit voucher (BMI cevirme məntiqi)
    Task<KocurmeDetalDto?> DetalAsync(int id, string novu);

    // Form dəyərlərindən canlı voucher (yadda saxlanmadan preview)
    IList<MuhasibatSetirDto> VoucherHesabla(KocurmeFormDto dto, string? hevaleNo);

    Task<KocurmeEditDto?> RedakteMelumatiAsync(int id, string novu);
    Task<Result> YenileAsync(string novu, KocurmeEditDto dto, int userId, bool isAdmin);
    Task<Result> SilAsync(int id, int userId, bool isAdmin);

    // ══ 20 000 USD AYLIQ LİMİTİ (07.09.2026) ═══════════════════════════════
    // Məntiq `KocurmeLimit.cs`-dədir (servisin `partial` hissəsi).

    /// <summary>
    /// Bir FİN üzrə bir təqvim ayının cəmi — siyahı səhifəsindəki axtarış üçün.
    /// <paramref name="xaricId"/> redaktədə qeydin özünü cəmdən çıxarır.
    /// </summary>
    Task<FinLimitDto> FinAyliqCemAsync(string? fin, int il, int ay, int? xaricId = null);

    /// <summary>
    /// Formada CANLI yoxlama — məbləğ/FİN/tarix dəyişdikcə çağırılır.
    /// Yadda saxlama yolu da EYNİ bu metodu işlədir ki, ekranda göstərilən
    /// ilə tətbiq olunan qayda fərqlənməsin.
    /// </summary>
    Task<FinLimitYoxlamaDto> LimitYoxlaAsync(
        string novu, string? fin, decimal? mebleg, string? medaxilValyuta,
        DateTime? tarix, int? xaricId = null, CancellationToken ct = default);
}
