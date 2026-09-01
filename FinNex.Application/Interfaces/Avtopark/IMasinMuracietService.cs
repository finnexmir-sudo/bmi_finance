using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Avtopark;

namespace FinNex.Application.Interfaces.Avtopark
{
    /// <summary>
    /// Maşın müraciəti — müraciət → rəhbər təsdiqi → kassa (çıxdı/gəldi).
    ///
    /// Marşrut qaydası (kim təsdiqləyir) YALNIZ implementasiyadadır
    /// (`MasinMuracietService.IlkinStatus`). Ekran həmin mənbədən oxuyur —
    /// şərti view/DTO içində təkrar qurma (CLAUDE.md «Rol Prioriteti»).
    /// </summary>
    public interface IMasinMuracietService
    {
        /// <summary>İşçinin öz müraciətləri (ən yeni əvvəl).</summary>
        Task<IList<MasinMuracietListDto>> GetIsciMuracietleriAsync(int isciId);

        /// <summary>Rəhbər təsdiqini gözləyənlər.</summary>
        Task<IList<MasinMuracietListDto>> GetTesdiqGozleyenlerAsync();

        /// <summary>
        /// Kassa ekranı — açar gözləyənlər (Tesdiqlenib) və çöldə olanlar (Cixib).
        /// </summary>
        Task<IList<MasinMuracietListDto>> GetKassaSiyahisiAsync();

        /// <summary>Qayıtmayanlar — hələ «Çıxıb» statusunda qalanlar.</summary>
        Task<IList<MasinMuracietListDto>> GetAcigCixislarAsync();

        /// <summary>Tarix aralığı üzrə jurnal (boş buraxılsa son 30 gün).</summary>
        Task<IList<MasinMuracietListDto>> GetJurnalAsync(DateTime? bas, DateTime? son, int? masinId);

        Task<MasinMuracietListDto?> GetirAsync(int id);

        Task<Result<int>> YaratAsync(MasinMuracietCreateDto dto, int userId);

        // ══ EZAMİYYƏT BAĞLANTISI (01.09.2026) ══════════════════════════════
        // İşçi ezamiyyətə maşınla gedirsə maşını Avtoparkda AYRICA yazmır —
        // ezamiyyət formasında seçir, rəhbər ezamiyyəti təsdiqləyəndə maşın
        // müraciəti burada yaranır. «Bir forma, bir təsdiq» (istifadəçi qərarı).

        /// <summary>
        /// Maşın seçimi qəbul edilə bilərmi (mövcuddur + `Aktiv`).
        /// Ezamiyyət YARADILARKƏN çağırılır ki, təsdiq anında yox, elə
        /// forma göndəriləndə xəbər verilsin.
        /// </summary>
        Task<Result> MasinSecimiYoxlaAsync(int masinId);

        /// <summary>
        /// Təsdiqlənmiş ezamiyyət üçün maşın müraciəti yaradır — DƏRHAL
        /// <c>Tesdiqlenib</c> statusunda (rəhbər onsuz da ezamiyyəti təsdiqlədi,
        /// eyni şeyi ikinci dəfə soruşmuruq) və birbaşa kassaya bildiriş gedir.
        ///
        /// ⚠️ `YaratAsync` ÇAĞIRILMIR: o, statusu müraciət sahibinin roluna görə
        /// təyin edir (`IlkinStatus`) və adi işçidə `Gozlemede` yazardı — yəni
        /// rəhbər eyni səfəri iki dəfə təsdiqləməli olardı.
        ///
        /// Çağıran TRANZAKSİYA açmalıdır — bu metod öz `YaddaSaxlaAsync`-ini
        /// çağırır, amma ezamiyyətin statusu ilə birlikdə atomik olmalıdır.
        /// </summary>
        Task<Result<int>> EzamiyyetdenYaratAsync(
            int ezamiyyetId, int masinId, int isciId,
            DateTime planBaslama, string meqsed, string? marsrut,
            int rehberIsciId, int userId);

        /// <summary>
        /// Ezamiyyət ləğv/imtina olunanda ona bağlı maşın müraciətini ləğv edir.
        ///
        /// Açar ARTIQ VERİLİBSƏ (`Cixib`) sətrə TOXUNULMUR — maşın fiziki olaraq
        /// çöldədir, onu ekran «ləğv edilmiş» sayarsa kassa jurnalı yalan danışar
        /// və maşın heç vaxt «qayıtdı» olmaz. Belə halda `false` qaytarılır ki,
        /// çağıran istifadəçiyə xəbərdarlıq göstərə bilsin.
        /// </summary>
        Task<bool> EzamiyyetLegvindeMasiniLegvEtAsync(int ezamiyyetId, int userId);

        /// <param name="rehberIsciId">Təsdiqi edən işçinin Id-si (jurnalda qalır).</param>
        Task<Result> TesdiqEtAsync(int id, int rehberIsciId, int userId);
        Task<Result> ImtinaEtAsync(int id, int rehberIsciId, string? sebeb, int userId);

        /// <summary>İşçi öz müraciətini ləğv edir — yalnız açar verilməmişdən əvvəl.</summary>
        Task<Result> LegvEtAsync(int id, int isciId, int userId);

        /// <summary>Kassa açarı verdi — «Çıxdı».</summary>
        Task<Result> CixdiAsync(int id, int qeydEdenIsciId, int userId);

        /// <summary>Kassa açarı geri aldı — «Gəldi».</summary>
        Task<Result> GeldiAsync(int id, int qeydEdenIsciId, int userId);
    }
}
