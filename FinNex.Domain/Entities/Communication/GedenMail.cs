namespace FinNex.Domain.Entities.Communication;

// Göndərilən Mail — IMAP "Sent" qovluğunun köçürülməsi (12.10.2026).
// Yalnız GÖSTƏRMƏ üçündür: real göndərmə SMTP vasitəsilə (GelenMail/Cavab axını)
// baş verir, bura heç nə YAZMIR — bu entity sadəcə poçt qutusunun "Göndərilənlər"
// qovluğunu əks etdirir (GelenMailImapSyncer-in "Inbox" üçün etdiyinin eynisi).
public class GedenMail : BaseEntity
{
    public string MessageId { get; set; } = null!;   // IMAP Message-ID header

    // Mail sahibi — qutusu sinxron olunan istifadəçi (AppUser.Id). GelenMail ilə
    // EYNİ izolyasiya qaydası: hər kəs yalnız öz qutusunun mailini görür.
    public int? SahibUserId { get; set; }

    public string KimeAd { get; set; } = "";       // ilk alıcının adı (göstərmə üçün)
    public string KimeEmail { get; set; } = null!;  // ilk alıcının ünvanı
    public string KimeToplu { get; set; } = "";     // bütün alıcılar (To + Cc), tam siyahı
    public string Movzu { get; set; } = "";
    public string MetinHtml { get; set; } = "";
    public string MetinDuz { get; set; } = "";
    public DateTime GonderilmeTarixi { get; set; }

    public virtual ICollection<GedenMailQosma> Qosmalar { get; set; } = new List<GedenMailQosma>();
}
