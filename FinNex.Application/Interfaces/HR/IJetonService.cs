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

    // Standart iş saatı (günlük token üçün modalda avtomatik tam gün doldurmaq) — ("09:00","17:45")
    Task<(string Giris, string Cixis)> StandartIsSaatiGetirAsync();

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

    // İşçi: öz sorğusunu təsdiqdən əvvəl (Gozlenilir) ləğv et — jetonlar geri qayıdır
    Task<Result> RedimTelebiLegvEtAsync(int redimId, int isciId);

    // Gözləyən redim sorğuları (rola görə filtrlənir):
    //   Rehber → öz departamentinin RehberTesdiq=null olan sorğuları
    //   HR/Admin → RehberTesdiq=true olan sorğular (rəhbər artıq təsdiqləyib)
    // asRehber: rəhbər mərhələsi (RehberTesdiq==null); asHrAdmin: HR final mərhələsi (RehberTesdiq==true).
    // İkisi də true olduqda hər iki mərhələ qaytarılır (rəhbər+admin eyni şəxs ola bilər).
    Task<IList<JetonRedimTelebiListDto>> GozleyenRedimlerGetirAsync(int? rehberDepartamentId, bool asRehber, bool asHrAdmin);

    // İcazə üçün FIFO ilə jeton xərclə (Variant 1 — Icaze formu yolu).
    // İşçinin aktiv jetonlarından ən köhnədən başlayaraq tələb olunan saat qədər seçir,
    // həmin jetonları "İstifadə olunub" edir. Tam xərclənir — qismən deyil.
    Task<Result<decimal>> IcazeUcunFifoJetonXercleAsync(int isciId, decimal teleblesaat, int? icazeId = null);

    // İcazə/məzuniyyətə jeton əvəzləşdirmə (FIFO tutulma) üçün GÖRÜNƏN redim qeydi yaradır —
    // Xərcləmə Tarixçəsində adi redim kimi görünsün (Status=Təsdiqləndi). Məzuniyyətdə saat
    // aralığı olmadığı üçün baslama/bitis null ötürülür.
    Task<Result> JetonEvezlesdirmeQeydiAsync(int isciId, decimal jetonSaat, System.DateTime tarix, System.TimeSpan? baslama, System.TimeSpan? bitis, string qeyd);

    // İcazə ləğvində jetonu geri qaytarır (reverse-FIFO). Dəyişiklikləri stage edir
    // (save etmir) — çağıran tək tranzaksiyada saxlamalıdır.
    Task<Result> IcazeJetonuGeriQaytarAsync(int isciId, decimal saat);

    // İşçinin redim tarixçəsi
    Task<IList<JetonRedimTelebiListDto>> IsciRedimTarixcesiGetirAsync(int isciId);

    // HR: bütün redim tarixçəsi (audit üçün)
    Task<IList<JetonRedimTelebiListDto>> ButunRedimlerTarixcesiAsync();
}
