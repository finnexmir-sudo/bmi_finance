using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.Interfaces.Kredit
{
    /// <summary>
    /// Komitəsiz rədd səbəbləri açar siyahısı (02.09.2026).
    ///
    /// Səbəb siyahısı biznes qərarıdır və zamanla artır — ona görə enum yox,
    /// cədvəldir. Admin öz səhifəsindən əlavə edir, build lazım deyil.
    /// </summary>
    public interface IKreditReddSebebiService
    {
        /// <summary>Admin səhifəsi üçün — deaktivlər də daxil, sıra ilə.</summary>
        Task<IList<KreditReddSebebi>> HamisiniGetirAsync();

        /// <summary>Rədd formasındakı seçim siyahısı — YALNIZ aktivlər.</summary>
        Task<IList<KreditReddSebebi>> AktivleriGetirAsync();

        /// <summary>
        /// Yeni səbəb. Eyni adlı aktiv səbəb varsa istisna atır — siyahıda
        /// iki eyni sətir olsa hesabat ikiyə bölünər.
        /// </summary>
        Task<KreditReddSebebi> YaratAsync(string ad, int sira, int? yaradanIcraciId);

        /// <summary>Ad/sıra düzəlişi.</summary>
        Task YenileAsync(int id, string ad, int sira, int? yenileyenIcraciId);

        /// <summary>
        /// Aktiv/deaktiv keçidi. SİLMİR — keçmiş müraciətlər bu sətrə istinad
        /// edir, silinsə tarixçə «səbəbsiz» qalar.
        /// </summary>
        Task AktivliyiDeyisAsync(int id, bool aktivdir, int? yenileyenIcraciId);
    }
}
