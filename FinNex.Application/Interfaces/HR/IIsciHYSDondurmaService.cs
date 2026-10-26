using FinNex.Application.Common.Results;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    /// <summary>
    /// İşçi HYS-inin tək aylıq dondurulması — "bu ay HYS-i hesablamaya qatma".
    ///
    /// ƏN VACİB METOD: <see cref="DondurulmusIdlerAsync"/>. Maaş/vergi bazasını
    /// HYS-dən azaldan HƏR yer (FerdiHesabla, TopluHesabla önizləməsi,
    /// mühasibat provodkası, məzuniyyət ödənişi — HR və işçi tərəfi) HYS
    /// cəmini çıxarmazdan ƏVVƏL bu metodu çağırıb dondurulmuş qeydləri xaric
    /// etməlidir. Öz sorğunu yazma — TƏK mənbə budur (bax CLAUDE.md
    /// "Bir Kəmiyyətə Üç/Dörd Yazıcı" qaydası).
    /// </summary>
    public interface IIsciHYSDondurmaService
    {
        Task<Result> DondurAsync(int isciHYSId, int il, int ay, string? sebeb);

        Task<Result> LegvEtAsync(int id);

        Task<List<IsciHYSDondurma>> IsciHYSUzreGetirAsync(int isciHYSId);

        /// <summary>
        /// Verilmiş (il, ay) üçün dondurulmuş BÜTÜN IsciHYS.Id-lərin dəstini
        /// qaytarır. Konkret bir işçi/qeyd üçün deyil, qlobaldır — çağıran
        /// tərəf öz hysList-indən bu dəstdə OLAN sətirləri xaric etməlidir.
        /// </summary>
        Task<HashSet<int>> DondurulmusIdlerAsync(int il, int ay);
    }
}
