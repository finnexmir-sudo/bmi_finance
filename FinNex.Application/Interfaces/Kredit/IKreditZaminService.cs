using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.Interfaces.Kredit
{
    /// <summary>
    /// Zaminlərlə iş. Bir müraciətə bir neçə zamin əlavə oluna bilər. Zamin
    /// əlavə edildikdə eyni FIN-li zaminin əvvəlki baxışları axtarılır —
    /// tarixçə xəbərdarlığı göstərmək üçün.
    /// </summary>
    public interface IKreditZaminService
    {
        Task<IList<KreditZamin>> MuracietZaminleriniGetirAsync(int muracietId);

        /// <summary>
        /// Verilən FIN üzrə başqa müraciətlərdəki zamin qeydlərini qaytarır
        /// (cari müraciət istisna). Boş FIN olsa — boş siyahı.
        /// </summary>
        Task<IList<KreditZamin>> FinUzreTarixceAsync(string fin, int? istisnaMuracietId = null);

        Task<KreditZamin> YaratAsync(KreditZamin entity, int? yaradanIcraciId);
        Task SilAsync(int zaminId, int? silenIcraciId);

        /// <summary>
        /// Zamin üçün MKR nəticəsini yazır (manual — gələcəkdə API ilə əvəz olunacaq).
        /// </summary>
        Task MkrNeticesiYazAsync(int zaminId, string netice, int baxanIsciId);

        /// <summary>
        /// Zamin üçün AsanFinance nəticəsini yazır.
        /// </summary>
        Task AsanFinanceNeticesiYazAsync(int zaminId, string netice, int baxanIsciId);
    }
}
