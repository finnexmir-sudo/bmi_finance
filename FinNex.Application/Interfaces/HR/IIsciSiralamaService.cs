using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Isci;

namespace FinNex.Application.Interfaces.HR
{
    /// <summary>
    /// İşçilərin göstərmə sırasını idarə edir. "İşçi Sıralaması" səhifəsində
    /// drag-and-drop ilə təyin olunur, hər siyahıda bu sıra istifadə edilir.
    /// </summary>
    public interface IIsciSiralamaService
    {
        /// <summary>Bütün aktiv işçilər — cari sıra ilə düzülmüş.</summary>
        Task<IList<IsciSiraDto>> GetSiyahiAsync();

        /// <summary>Yeni sıra: massivdəki indekslərə görə (0, 1, 2 ...) hər
        /// işçiyə Sira təyin olunur. Data: yenilənmiş qeyd sayı.</summary>
        Task<Result<int>> SaxlaAsync(IList<int> isciIdSirasi);
    }
}
