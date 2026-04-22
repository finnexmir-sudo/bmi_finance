using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.Interfaces.Kredit
{
    /// <summary>
    /// Kredit müraciətinin həyat dövrünü idarə edir:
    ///   Yeni → Yoxlanılır → KomitəyəGöndərildi → Təsdiqlənib/RəddEdilib
    /// Müraciət yaratma, status dəyişiklikləri, MKR/AsanFinance nəticəsini
    /// yazma və siyahı/filtr əməliyyatları buradadır.
    /// </summary>
    public interface IKreditMuracietService
    {
        // ── Oxuma ────────────────────────────────────────────
        Task<KreditMuraciet?> IdIleGetirAsync(int id, bool includeAll = true);
        Task<IList<KreditMuraciet>> SiyahiAsync(KreditMuracietStatus? status = null,
                                                  KreditMuracietMenbe? menbe = null,
                                                  int? baxanIsciId = null);

        /// <summary>
        /// Verilən FIN üzrə eyni şəxsin əvvəlki müraciətlərini qaytarır
        /// (cari müraciət istisna). Boş FIN olsa — boş siyahı.
        /// </summary>
        Task<IList<KreditMuraciet>> FinUzreTarixceAsync(string? fin, int? istisnaMuracietId = null);

        /// <summary>
        /// Mail-dən gələn müraciətin MessageId-si artıq DB-də varsa true qaytarır.
        /// </summary>
        Task<bool> MailMessageIdVarsa(string messageId);

        // ── Yaratma ──────────────────────────────────────────
        Task<KreditMuraciet> YaratAsync(KreditMuraciet entity, int? yaradanIcraciId);

        // ── Status keçidləri ─────────────────────────────────
        /// <summary>
        /// "Yoxlanılır" statusuna keçir + baxan işçini təyin edir + qeyd yazır.
        /// </summary>
        Task BaxilmagaGoturAsync(int muracietId, int baxanIsciId, string? qeyd);

        /// <summary>
        /// MKR nəticəsini yazır (bütün müraciət üçün bir dəfə saxlanır).
        /// </summary>
        Task MkrNeticesiYazAsync(int muracietId, string netice, int baxanIsciId);

        /// <summary>
        /// AsanFinance nəticəsini yazır.
        /// </summary>
        Task AsanFinanceNeticesiYazAsync(int muracietId, string netice, int baxanIsciId);

        /// <summary>
        /// "KomitəyəGöndərildi" statusuna keçir. İlkin baxış tamamlanmalıdır.
        /// </summary>
        Task KomiteyeGonderAsync(int muracietId, int gonderenIsciId, string? qeyd);

        /// <summary>
        /// Müraciəti yeniləyir (müştəri məlumatları). Status toxunulmur.
        /// </summary>
        Task YenileAsync(KreditMuraciet entity, int? yenileyenIcraciId);

        /// <summary>
        /// Müraciəti yumşaq sil.
        /// </summary>
        Task SilAsync(int muracietId, int? silenIcraciId);
    }
}
