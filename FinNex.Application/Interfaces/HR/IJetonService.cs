using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Jeton;

public interface IJetonService
{
    // Jeton kataloqu
    Task<IList<JetonTeyinatiListDto>> JetonTeyinatlariGetirAsync();

    // HR: yeni jeton növü yarat
    Task<Result> JetonTeyinatiYaratAsync(JetonTeyinatiCreateDto dto);

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

    // Rəhbər: redim sorğusunu təsdiqlə (HR təsdiqindən əvvəl)
    Task<Result> RedimRehberTesdiqleAsync(int redimId, int rehberUserId);

    // Rəhbər: redim sorğusunu rədd et
    Task<Result> RedimRehberReddEtAsync(int redimId, string qeyd, int rehberUserId);

    // HR: redim sorğusunu təsdiqlə (yalnız Rəhbər təsdiqindən sonra)
    Task<Result> RedimTelebiTesdiqleAsync(int redimId, int tesdiqleyenUserId);

    // HR: redim sorğusunu rədd et
    Task<Result> RedimTelebiReddEtAsync(int redimId, string qeyd, int userId);

    // Gözləyən redim sorğuları (rola görə filtrlənir):
    //   Rehber → öz departamentinin RehberTesdiq=null olan sorğuları
    //   HR/Admin → RehberTesdiq=true olan sorğular (rəhbər artıq təsdiqləyib)
    Task<IList<JetonRedimTelebiListDto>> GozleyenRedimlerGetirAsync(int? rehberDepartamentId = null, bool rehberView = false);

    // İcazə üçün FIFO ilə jeton xərclə (Variant 1 — Icaze formu yolu).
    // İşçinin aktiv jetonlarından ən köhnədən başlayaraq tələb olunan saat qədər seçir,
    // həmin jetonları "İstifadə olunub" edir. Tam xərclənir — qismən deyil.
    Task<Result<decimal>> IcazeUcunFifoJetonXercleAsync(int isciId, decimal teleblesaat, int? icazeId = null);

    // İşçinin redim tarixçəsi
    Task<IList<JetonRedimTelebiListDto>> IsciRedimTarixcesiGetirAsync(int isciId);

    // HR: bütün redim tarixçəsi (audit üçün)
    Task<IList<JetonRedimTelebiListDto>> ButunRedimlerTarixcesiAsync();
}
