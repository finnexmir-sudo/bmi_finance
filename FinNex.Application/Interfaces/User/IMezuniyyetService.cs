using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces
{
    public interface IMezuniyyetService
        : IServiceAsync<Mezuniyyet, MezuniyyetDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>
    {
        // Mövcud metodlar
        Task<Result<IList<MezuniyyetListDto>>> GetListAsync();
        Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd, int sobeReisiId);
        Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId);
        /// <summary>
        /// HR təsdiq. Opsional olaraq manual gün sayı və düzəliş səbəbi ilə.
        /// gunSayiManual null olarsa avtomatik hesablanmış IsGunlerininSayi
        /// istifadə olunur. Dəyər varsa balans kəsimi və davamiyyət qeydləri
        /// bu rəqəmə görə icra olunur.
        /// </summary>
        Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd, int hrId,
                                     int? gunSayiManual = null, string? duzelisSebebi = null);

        // YENİ: İşçi paneli üçün əlavə edildi
        Task<Result<IList<MezuniyyetListDto>>> GetIsciMezuniyyetleriAsync(int isciId);
        Task<Result> LegvEtAsync(int id, int isciId);

        // Təsdiq paneli üçün
        Task<Result<IList<MezuniyyetListDto>>> GetGozlemededeAsync();
        Task<Result<IList<MezuniyyetListDto>>> GetRehberTesdiqindeAsync();
        Task<Result<IList<MezuniyyetListDto>>> GetHrTesdiqindeAsync();
        // HR izləmə üçün — SobeReisi və Rəhbər təsdiqində dayanan müraciətlər
        Task<Result<IList<MezuniyyetListDto>>> GetProsesdeOlanlarAsync();

        /// <summary>
        /// HR tarixçəsi — təsdiqlənmiş və imtina edilmiş bütün müraciətlər
        /// (opsional axtarış: işçi adı/soyadı/FIN).
        /// </summary>
        Task<Result<IList<MezuniyyetListDto>>> GetTarixceAsync(string? axtaris = null);

        /// <summary>
        /// Hazırda məzuniyyətdə olan (Təsdiqlənib + BaslamaTarixi ≤ bugün ≤ BitmeTarixi)
        /// və/və ya yaxın günlərdə başlayacaq işçilərin izləmə siyahısı.
        /// </summary>
        /// <param name="qabaqcaGun">Bugündən sonra neçə gün irəli (default 30) — "yaxın 30 gündə başlayacaqlar" üçün</param>
        Task<Result<IList<MezuniyyetListDto>>> GetAktivVeYaxinlardakilarAsync(int qabaqcaGun = 30);

        Task<Result<IList<MezuniyyetListDto>>> GetSobeyeGoreMezuniyyetlerAsync(int departamentId, int sobeReisiIsciId);
        Task<Result<IList<MezuniyyetListDto>>> GetFiltrliAsync( DateTime? baslaTarixFrom,DateTime? baslaTarixTo,int? departamentId,int? status,string? axtaris);

        // Təsdiq zamanı məlumatlandırma — paralel məzuniyyətlər + əvəzedici konflikti
        Task<Result<IList<MezuniyyetOverlapDto>>> GetOverlapMezuniyyetlerAsync(int mezuniyyetId, StrukturRolTipi? viewerRol = null);
        Task<Result<IList<EvezediciKonfliktDto>>> GetEvezediciKonfliktiAsync(int mezuniyyetId);

        // HR tərəfindən keçmiş tarixlər üçün məzuniyyətin geriyə qeyd edilməsi.
        // Təsdiq axınını atlayır, avtomatik təsdiqlənib statusunda yaranır,
        // davamiyyətdə "Qayib" qeydlərini "İcazəli"-yə çevirir.
        Task<Result<MezuniyyetDto>> GeriyeQeydEtAsync(GeriyeMezuniyyetCreateDto dto, int hrIsciId);

        /// <summary>
        /// Dövlət vəzifəsi korreksiyası (Əmək Məcəlləsi Maddə 173).
        /// İşçi əmək məzuniyyətində olarkən hərbi çağırış / məhkəmə şahidliyi kimi
        /// dövlət vəzifəsi yerinə yetirirsə:
        ///   a) həmin günlər illik balansa geri qaytarılır (+KorreksiyaGunSayi)
        ///   b) DovletVezifelerininIcrasi növündə yeni məzuniyyət qeydi yaranır
        ///   c) Davamiyyət qeydlərində həmin günlər OdenisDovletVezifesi → MaasdanKes=false
        /// </summary>
        Task<Result<MezuniyyetDto>> KorreksiyaEtAsync(
            MezuniyyetKorreksiyaDto dto, int hrIsciId, string senedSaxlama);

        /// <summary>
        /// Mövcud dövlət vəzifəsi korreksiya qeydine sonradan sənəd əlavə edir.
        /// senedYollari: "|" ilə ayrılmış yeni fayl yolları.
        /// Mövcud sənədlər silinmir — üstünə əlavə olunur.
        /// </summary>
        Task<Result> SenedElavetEtAsync(int mezuniyyetId, string senedYollari);
    }
}