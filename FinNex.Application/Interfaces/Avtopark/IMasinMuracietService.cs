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
