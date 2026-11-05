namespace FinNex.Domain;

/// <summary>
/// Sistem icazə kodları — `Permissions.Kod` sütunu ilə HƏRFƏN eyni (01.09.2026).
///
/// NİYƏ BURADA: eyni kod həm `FinNex.UI` (atribut, view şərti), həm
/// `FinNex.Application` (fon servisləri) tərəfindən lazım olur. Application
/// layihəsi UI-a istinad edə bilmir, ona görə sabit ORTAQ layihədədir
/// (`FinNex.Domain`) — `RoleNames` ilə eyni məntiq.
///
/// ⚠️ İki yerdə mətn kimi yazılsa, biri dəyişəndə o biri səssizcə köhnə qalar
/// və icazə HEÇ BİR XƏTA VERMƏDƏN işləməz (CLAUDE.md). Yeni icazə kodu
/// əlavə edəndə onu bura yaz, hər yerdən buradan oxu.
///
/// Bazaya salan skriptlər: `docs/sql/mail/`, `docs/sql/avtopark/`.
/// </summary>
public static class IcazeKodlari
{
    /// <summary>
    /// Mail istifadəsi — «Gələn Maillər» + Profildə «Mail Ayarları».
    /// Admin və Rəhbər rolu onsuz da girir; bu, ƏLAVƏ işçilər üçündür.
    /// </summary>
    public const string MailIstifade = "mail_istifade";

    /// <summary>Avtopark idarəetməsi — Maşınlar və Müddətlər səhifələri.</summary>
    public const string AvtoparkIdare = "avtopark_idare";

    /// <summary>
    /// GOAML modulu (05.11.2026) — Risk/AML şübhəli əməliyyat hesabatı. Bilərəkdən
    /// Risk-in öz (hazırda icazəsiz) sahəsindən AYRI — Risk gələcəkdə fərqli
    /// qaydaya keçsə belə, GOAML buna bağlı qalmasın. Yalnız Admin avtomatik
    /// keçir (`ElaveRol` YOXDUR) — qalan hər kəs, Rəhbər daxil, açıq icazə alır.
    /// </summary>
    public const string GoamlBax = "goaml_bax";
}
