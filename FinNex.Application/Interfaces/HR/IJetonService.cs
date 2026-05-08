using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Jeton;

public interface IJetonService
{
    // Jeton kataloqu
    Task<IList<JetonTeyinatiListDto>> JetonTeyinatlariGetirAsync();

    // HR: jeton kataloqunu yenilə (saat dəyəri, ad, status və s.)
    Task<Result> JetonTeyinatiYenileAsync(JetonTeyinatiUpdateDto dto);

    // HR: işçiyə jeton vermək
    Task<Result> JetonVerAsync(IsciJetonuCreateDto dto, int verenUserId);

    // HR: işçinin jetonunu geri almaq / ləğv etmək
    Task<Result> JetonLegvetAsync(int isciJetonuId, string sebeb);

    // İşçinin aktiv jetonlarını gətir
    Task<IList<IsciJetonuListDto>> IsciAktivJetonlariniGetirAsync(int isciId);

    // Bütün əməliyyat tarixçəsi (HR üçün, isteğe bağlı işçi filteri)
    Task<IList<IsciJetonuListDto>> JetonEmeliyyatlariGetirAsync(int? isciId = null);

    // İşçinin aktiv Qara jetonu var mı?
    Task<bool> AktivQaraJetonuVarmiAsync(int isciId);

    // İşçinin aktiv müsbət saat balansı
    Task<decimal> AktivSaatBalansiAsync(int isciId);

    // İşçi: redim sorğusu göndər
    Task<Result> RedimTelebiYaratAsync(int isciId, JetonRedimTelebiCreateDto dto);

    // HR: redim sorğusunu təsdiqlə
    Task<Result> RedimTelebiTesdiqleAsync(int redimId, int tesdiqleyenUserId);

    // HR: redim sorğusunu rədd et
    Task<Result> RedimTelebiReddEtAsync(int redimId, string qeyd, int userId);

    // HR: gözləyən redim sorğuları
    Task<IList<JetonRedimTelebiListDto>> GozleyenRedimlerGetirAsync();

    // İşçinin redim tarixçəsi
    Task<IList<JetonRedimTelebiListDto>> IsciRedimTarixcesiGetirAsync(int isciId);

    // HR: bütün redim tarixçəsi (audit üçün)
    Task<IList<JetonRedimTelebiListDto>> ButunRedimlerTarixcesiAsync();
}
