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
    }
}
