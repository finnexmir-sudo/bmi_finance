using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Avtopark;

namespace FinNex.Application.Interfaces.Avtopark
{
    /// <summary>
    /// Maşın müddətləri (sığorta, texniki baxış, yağ dəyişmə…) və xəbərdarlıq
    /// alıcıları. 19.08.2026 qərarı ilə izləmə YALNIZ TARİXƏ görədir.
    /// </summary>
    public interface IMasinMuddetService
    {
        // ── Növlər (lookup) ───────────────────────────────────────────────
        Task<IList<MasinMuddetNovuDto>> NovleriGetirAsync(bool yalnizAktiv = false);
        Task<Result<int>> NovYaratAsync(MasinMuddetNovuDto dto, int userId);
        Task<Result> NovYenileAsync(MasinMuddetNovuDto dto, int userId);
        Task<Result> NovSilAsync(int id, int userId);

        // ── Müddət qeydləri ───────────────────────────────────────────────
        /// <param name="masinId">Boş — bütün maşınlar.</param>
        /// <param name="yalnizAktiv">true — yalnız cari sətirlər (köhnəlmiş tarixçə gizlənir).</param>
        Task<IList<MasinMuddetDto>> HamisiniGetirAsync(int? masinId = null, bool yalnizAktiv = true);

        /// <summary>Bitməsinə <paramref name="gun"/> gündən az qalanlar + keçmişlər.</summary>
        Task<IList<MasinMuddetDto>> YaxinlasanlarAsync(int gun = 30);

        Task<MasinMuddetDto?> GetirAsync(int id);

        Task<Result<int>> YaratAsync(MasinMuddetCreateDto dto, int userId);
        Task<Result> YenileAsync(MasinMuddetCreateDto dto, int userId);
        Task<Result> SilAsync(int id, int userId);

        /// <summary>
        /// Müddəti uzadır: köhnə sətir passivləşir (silinmir), yenisi yaranır.
        /// Tarixçə qalsın deyə redaktədən ayrı metoddur.
        /// </summary>
        Task<Result<int>> UzatAsync(int kohneId, DateTime yeniSonTarix, decimal? mebleg, string? qeyd, int userId);

        // ── Xəbərdarlıq alıcıları (admin idarə edir) ──────────────────────
        Task<IList<AvtoparkAliciDto>> AlicilarAsync(bool yalnizAktiv = false);
        Task<Result> AliciElaveEtAsync(int isciId, int userId);
        Task<Result> AliciSilAsync(int id, int userId);
        Task<Result> AliciAktivlikDeyisAsync(int id, bool aktivdir, int userId);
    }
}
