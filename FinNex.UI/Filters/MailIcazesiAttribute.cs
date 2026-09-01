using FinNex.Domain;

namespace FinNex.UI.Filters;

/// <summary>
/// Mail funksiyası (Gələn Maillər + Profildəki «Mail Ayarları») üçün giriş
/// yoxlaması — 01.09.2026.
///
/// **Kim girir:** Admin · Rəhbər rolu · <see cref="IcazeKodu"/> icazəsi verilmiş
/// istifadəçi. Qalanı `403` alır.
///
/// NİYƏ ROL DEYİL: əvvəl mail YALNIZ `Rehber` roluna bağlı idi. Yəni bir işçiyə
/// öz poçtunu sistemdən oxutmaq üçün ona tam Rəhbər rolu vermək lazım gəlirdi —
/// o isə təsdiq panellərini, işçi izləməni, dashboard-ları da açır. İndi ayrıca
/// icazə var: yalnız mail açılır, qalan səlahiyyətlər dəyişmir.
///
/// RƏHBƏR ROLU QƏSDƏN SAXLANILIB (`ElaveRol`) — istifadəçi qərarı. Mövcud
/// rəhbərlər build-dən sonra heç nə itirmir; Admin yalnız ƏLAVƏ işçilərə icazə
/// verir. Rolu tamamilə kəsmək istəsək əvvəlcə cari rəhbərlərə bu icazəni
/// birdəfəlik SQL ilə vermək lazımdır.
///
/// POÇT QUTUSU ŞƏXSİDİR: `GelenMail.SahibUserId` — icazə verilən işçi ÖZ
/// məktublarını görür, rəhbərinkiləri yox. Sinxronizasiya işçinin öz SMTP/IMAP
/// məlumatı ilə gedir (`AppUser.MailSmtpEmail`), o da yalnız özündə var.
///
/// ⚠️ Bu şərtin GÖSTƏRMƏ QATINDAKI qarşılığı `_UserLayout.cshtml` → `hasMail`
/// dəyişənidir və `Profile/Index.cshtml`-dəki kart şərti. Üçü EYNİ olmalıdır,
/// yoxsa istifadəçi ya linki görüb 403 alar, ya da icazəsi olduğu halda linki
/// tapa bilməz.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MailIcazesiAttribute : IcazeAttribute
{
    /// <summary>
    /// İcazə kodu — `Permissions.Kod` sütunu ilə HƏRFƏN eynidir.
    /// Bazaya salan skript: `docs/sql/mail/01_Mail_Istifade_Icazesi.sql`.
    /// </summary>
    public const string IcazeKodu = "mail_istifade";

    public MailIcazesiAttribute() : base(IcazeKodu)
    {
        ElaveRol = RoleNames.Rehber;
    }
}
