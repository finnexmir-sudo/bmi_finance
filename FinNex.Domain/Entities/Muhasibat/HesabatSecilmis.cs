using FinNex.Domain;

namespace FinNex.Domain.Entities.Muhasibat
{
    /// <summary>
    /// İşçinin "Maliyyə Hesabatları" səhifəsində ★ ilə seçdiyi hesabatlar (29.10.2026 —
    /// "Hesabatlar" Balans İcmalının tab-dropdown-undan ayrıca, sidebar-da öz səhifəsinə
    /// köçürüldü). `HesabatKod` sabit, koddakı kataloqun açarıdır
    /// (Muhasibat/HesabatController-dəki statik siyahı) — DB-yə kataloq sətri yazılmır,
    /// "Kassa valyutaları"/"Qara Jeton" kimi tək-yerdən-idarə statik siyahı prinsipi.
    ///
    /// Unikal indeks (AppUserId, HesabatKod) `Silinib`-i FİLTRLƏMİR — `MailIzlemeUnvani`
    /// ilə EYNİ qayda (CLAUDE.md: "Yumşaq Silinmiş Sətir + Unikal İndeks"): toggle
    /// "aktiv → yumşaq silinmiş (dirilt) → yeni yarat" sırası ilə gedir
    /// (bax HesabatSecilmisService.SecVeyaCixarAsync).
    /// </summary>
    public class HesabatSecilmis : BaseEntity
    {
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        public string HesabatKod { get; set; } = null!;
    }
}
