using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Marif;

namespace FinNex.Application.Interfaces.Marif
{
    /// <summary>
    /// Daxili Maarifləndirmə — qayda üzrə sual dəstinin yaradılması, göndərilməsi
    /// və işçi tərəfindən cavablanması (27.10.2026, pilot). Sual MƏTNİNİN özünü
    /// AI-dən <see cref="FinNex.Application.Interfaces.AI.IMarifAiService"/> qaytarır —
    /// bu servis yalnız yadda saxlama/göndərmə/cavablama axınını idarə edir.
    /// </summary>
    public interface IMarifService
    {
        // ── Admin tərəfi ─────────────────────────────────────────────────
        // NOT: YaradanIcraciId/YenileyenIcraciId/SilenIcraciId AppDbContext-in
        // SaveChangesAsync-i tərəfindən HttpContext-dəki cari istifadəçiyə görə
        // ŞƏRTSİZ doldurulur (bax AppDbContext.cs ~1996) — bu metodlar ayrıca
        // userId qəbul etmir.
        Task<Result<int>> SetYaratAsync(MarifSualSetiYaratDto dto);
        Task<IList<MarifSualSetiListDto>> HamisiniGetirAsync();
        Task<MarifSualSetiDetailDto?> DetailGetirAsync(int setId);

        /// <summary>Qaralama statusundakı sətri redaktə edib yenidən yadda saxlayır.</summary>
        Task<Result> SetYenileAsync(int setId, MarifSualSetiYaratDto dto);

        /// <summary>Seçilmiş işçilərə göndərir — artıq göndərilmiş işçilər təkrar əlavə olunmur.</summary>
        Task<Result> GonderAsync(MarifGonderDto dto);

        /// <summary>Yalnız Qaralama statusunda olan (hələ göndərilməmiş) sət silinə bilər.</summary>
        Task<Result> SetSilAsync(int setId);

        // ── İşçi tərəfi ──────────────────────────────────────────────────
        Task<IList<MarifIsciTesyinatListDto>> IsciTesyinatlariAsync(int isciId);
        Task<MarifIsciSualSetiDto?> IsciSualSetiGetirAsync(int tesyinatId, int isciId);
        Task<Result<MarifCavabNeticeDto>> CavabVerAsync(MarifCavabVerDto dto, int isciId);
    }
}
