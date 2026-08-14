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

        // ── İşçi ləğv müraciəti → HR təsdiqi ilə ləğv ──
        // İşçi təsdiqlənmiş (başlamamış + ödənilməmiş) məzuniyyəti ləğv üçün müraciət
        // göndərir (səbəblə). Ləğv ETMİR — yalnız bayraq qoyur, HR-a bildiriş gedir.
        Task<Result> LegvTelebiEtAsync(int id, int isciId, string sebeb);
        // HR ləğv müraciətini rədd edir — bayraq təmizlənir, işçiyə bildiriş.
        Task<Result> LegvTelebiRedEtAsync(int id, int hrId, string? sebeb);
        // HR üçün gözləyən ləğv müraciətləri (LegvTelebEdilib + təsdiqlənmiş).
        Task<Result<IList<MezuniyyetListDto>>> GetLegvTelebleriAsync();

        // ── HR Düzəliş (təsdiqdən sonra, başlanğıc keçməyib) ──
        // Kərpic 1: ödəniş tipini dəyiş (ay-sonu ↔ qabaqcadan).
        // Avans artıq icra olunubsa (Odenilib/PlanliOdenis) bloklanır.
        Task<Result> HrOdenisTipiDeyisAsync(int id, MezuniyyetOdenisTipi yeniTipi, int hrId);

        // Kərpic 2: HR təsdiqlənmiş məzuniyyəti ləğv edir (başlanğıc keçməyib).
        // Balans geri qaytarılır, davamiyyət təmizlənir; avans icra olunubsa bloklanır,
        // yalnız gözləyən (Gozleyir) avans sıfırlanır + Mühasibə bildiriş gedir.
        Task<Result> HrLegvEtAsync(int id, string? sebeb, int hrId);

        // Kərpic 3: HR təsdiqlənmiş məzuniyyətin tarixlərini dəyişir (başlanğıc keçməyib).
        // Köhnə günlər balansa qaytarılır, yeni günlər kəsilir; davamiyyət uzlaşdırılır
        // (üst-üstə düşən günlər saxlanır). Avans gözləyirsə məbləğ yenidən hesablanır.
        // Avans icra olunubsa və ya dövlət-vəzifə korreksiyası varsa bloklanır.
        Task<Result> HrTarixDeyisAsync(int id, DateTime yeniBaslama, DateTime yeniBitme, string? sebeb, int hrId);

        // ADMIN: səhv daxil edilmiş məzuniyyətin tarixini düzəldir — məzuniyyət artıq
        // başlayıb/keçmiş olsa da işləyir (HR versiyasındakı 'başlayıb' bloku YOXDUR).
        // Balans/davamiyyət/avans eyni atomar məntiqlə uzlaşdırılır; ödənilmiş avans bloklanır.
        Task<Result> AdminTarixDeyisAsync(int id, DateTime yeniBaslama, DateTime yeniBitme, string? sebeb, int adminId);

        // ADMIN: səhvən yazılmış məzuniyyəti tamamilə LƏĞV edir — başlayıb/keçmiş
        // olsa da (HR yolunda bu mümkün deyil: həm "başlayıb" bloku, həm də işçinin
        // ləğv müraciəti tələb olunur). Real hadisə 14.08.2026: işçi işə gəlmədi,
        // HR ona 1 gün öz hesabına yazdı, rəhbər güzəşt etdi — geri götürmək
        // mümkün olmadı.
        //
        // MAAŞ QORUYUCUSU: məzuniyyətin toxunduğu HƏR ay üçün maaş `Təsdiqləndi`
        // və ya `Ödənildi` statusundadırsa bloklanır — təsdiqlənmiş hesablamanı
        // səssizcə etibarsız etmək olmaz. Səbəb məcburidir (audit).
        Task<Result> AdminLegvEtAsync(int id, string sebeb, int adminId);

        // ADMIN: səhv NÖV seçilibsə düzəldir (məs. "öz hesabına" əvəzinə "illik").
        // Balans köhnə növdən geri qaytarılır, yeni növdən kəsilir; davamiyyət
        // statusu yeni növə uyğunlaşdırılır. Maaş qoruyucusu ləğvdəki ilə eynidir.
        //
        // YALNIZ İllik ↔ Öz hesabına arasında: Xəstəlik və Ezamiyyət ayrı
        // modullardır (Xestelik / EzamiyyetMuraciet entity-ləri) — bura çevirmək
        // həmin modulların bilmədiyi "yetim" qeyd yaradardı. Dövlət vəzifəsinin
        // isə öz korreksiya mexanizmi var.
        Task<Result> AdminNovDeyisAsync(int id, MezuniyyetNovu yeniNov, string sebeb, int adminId);

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