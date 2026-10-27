using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Paylasim;

namespace FinNex.Application.Interfaces.Paylasim
{
    /// <summary>
    /// "Paylaşımlar" — Admin-in xaricdən aldığı mənalı məlumatı rich-text (şəkilli)
    /// formatda işçilərlə bölüşdüyü bilik lenti (27.10.2026, pilot). Elan-dan AYRI
    /// — Elan əməliyyat xəbərdarlığıdır, bu sadəcə oxuma məqsədlidir.
    /// </summary>
    public interface IPaylasimService
    {
        /// <summary>Admin idarəetmə siyahısı — aktiv/deaktiv hamısı, ən yenidən.</summary>
        Task<List<PaylasimListDto>> HamisiniGetirAsync();

        /// <summary>İşçi lenti — yalnız aktiv, ən yenidən.</summary>
        Task<List<PaylasimListDto>> AktivLentAsync();

        Task<PaylasimListDto?> DetailGetirAsync(int id);

        /// <param name="appUserId">Göndərənin AppUser.Id-si (Isci DEYİL — admin hesabının işçi profili olmaya bilər).</param>
        Task<Result<int>> YaratAsync(PaylasimYaratDto dto, int appUserId);

        Task<Result> YenileAsync(int id, PaylasimYaratDto dto);

        /// <summary>Göstər/gizlət — silmədən lentdən çıxarır (geri qaytarıla bilər).</summary>
        Task<Result> AktivlikDeyisAsync(int id);

        Task<Result> SilAsync(int id);
    }
}
