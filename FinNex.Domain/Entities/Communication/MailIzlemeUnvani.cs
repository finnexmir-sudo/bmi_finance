namespace FinNex.Domain.Entities.Communication;

// İşçinin "izlədiyi" mail ünvanları (12.10.2026, istifadəçi tələbi: "istədiyi
// mail ünvanlarını əlavə etsin, seçimi olsun ki yalnız bu maildən gələn və
// gedənləri görsün"). Qutu şəxsi olduğu kimi (GelenMail.SahibUserId) bu siyahı
// da şəxsidir — hər işçi yalnız ÖZ siyahısını görür/idarə edir.
public class MailIzlemeUnvani : BaseEntity
{
    public int SahibUserId { get; set; }

    // Həmişə kiçik hərflə, boşluqsuz saxlanılır (normallaşdırma servisdə) —
    // Pul Köçürməsi modulundakı `FinTemizle` ilə EYNİ prinsip: yazan və axtaran
    // fərqli formatda saxlasa uyğunluq səssizcə pozular.
    public string Email { get; set; } = null!;

    public string? Ad { get; set; }   // ixtiyari etiket ("Kontragent X" və s.)
}
