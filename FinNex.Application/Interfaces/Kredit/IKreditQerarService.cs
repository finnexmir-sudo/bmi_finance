using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.Interfaces.Kredit
{
    /// <summary>
    /// Komitə qərarlarını idarə edir. Komitə yığıncaqdan sonra qərar protokolu
    /// Word-də imzalanır — bu servis həmin qərar məlumatını və skanlanmış
    /// faylı sistemə daxil edir, imzalayan üzvləri qeyd edir, nəticə olaraq
    /// müraciətin statusunu yeniləyir.
    /// </summary>
    public interface IKreditQerarService
    {
        Task<KreditQerar?> MuracietUzreGetirAsync(int muracietId);
        Task<IList<KreditQerar>> HamisiniGetirAsync(KreditQerarTipi? yalnizBu = null);

        /// <summary>
        /// Komitə qərarını sistemə daxil edir. İmzalayan üzvlər (KomiteUzvu.Id siyahısı)
        /// mütləq aktiv üzvlər olmalıdır. Qərardan sonra müraciət statusu avtomatik
        /// Tesdiqlenib / ReddEdilib olaraq yenilənir.
        /// </summary>
        Task<KreditQerar> QebulEtAsync(KreditQerar qerar, IList<int> imzalayanKomiteUzvuIds,
                                         int daxilEdenIsciId);

        /// <summary>
        /// Mövcud qərarı düzəltmək (yalnız sistem səhvi halında).
        /// </summary>
        Task YenileAsync(KreditQerar qerar, IList<int>? yeniImzalayanlar, int? yenileyenIcraciId);
    }
}
