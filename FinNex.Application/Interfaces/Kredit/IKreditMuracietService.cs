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
        /// KOMİTƏSİZ RƏDD — baxan işçi müraciəti komitəyə göndərmədən bağlayır
        /// (02.09.2026, BMI iş prinsipi). Status → <c>ReddEdilib</c>.
        ///
        /// Səbəb MƏCBURİDİR (<c>KreditReddSebebi</c> açar cədvəlindən, aktiv
        /// olmalıdır); <paramref name="qeyd"/> sərbəst əlavə izahdır, məcburi deyil.
        ///
        /// ⚠️ `KreditQerar` sətri YARADILMIR — komitə jurnalı təmiz qalır.
        /// «Komitəsiz rədd» şərti elə budur: <c>Status == ReddEdilib &amp;&amp; Qerar == null</c>.
        ///
        /// Yalnız <c>Yeni</c> və <c>Yoxlanılır</c> statuslarında işləyir.
        /// Komitəyə göndərilmiş müraciəti işçi geri rədd EDƏ BİLMƏZ — o mərhələdə
        /// qərar komitənindir.
        /// </summary>
        Task KomitesizReddEtAsync(int muracietId, int reddSebebiId, string? qeyd, int reddEdenIsciId);

        /// <summary>
        /// Komitəsiz rəddi geri qaytarır — status rəddən əvvəlki mərhələyə düşür
        /// (<c>BaxanIsciId</c> varsa <c>Yoxlanılır</c>, yoxsa <c>Yeni</c>) və rədd
        /// sahələri təmizlənir.
        ///
        /// İCAZƏ: yalnız rəddi YAZAN işçi, ya da Admin
        /// (<paramref name="adminMi"/>). İstifadəçi qərarı (02.09.2026).
        ///
        /// ⚠️ KOMİTƏ QƏRARINI GERİ QAYTARMIR — müraciətin `Qerar`-ı varsa metod
        /// istisna atır. Komitənin protokolla verdiyi qərarı bir işçinin düyməsi
        /// ilə ləğv etmək olmaz.
        /// </summary>
        Task ReddiGeriQaytarAsync(int muracietId, int isciId, bool adminMi);

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
