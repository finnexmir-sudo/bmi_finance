/* ============================================================================
   MAIL — «mail_istifade» icazəsi · 01.09.2026
   ----------------------------------------------------------------------------
   NƏ EDİR: `Permissions` cədvəlinə BİR sətir əlavə edir (idempotent).
   Heç bir istifadəçiyə icazə VERMİR — onu Admin panel-dən siz verirsiniz.

   NİYƏ: mail funksiyası (Gələn Maillər + Profildəki «Mail Ayarları») əvvəl
   YALNIZ `Rehber` roluna bağlı idi. Yəni bir işçiyə öz poçtunu sistemdən
   oxutmaq üçün ona TAM RƏHBƏR rolu vermək lazım gəlirdi — o isə təsdiq
   panellərini, işçi izləməni və dashboard-ları da açır.

   İndi ayrıca icazə var:
     · Admin        — həmişə girir (icazə lazım deyil);
     · Rəhbər rolu  — QƏSDƏN saxlanılıb, mövcud rəhbərlər heç nə itirmir;
     · Digər işçi   — bu icazə verilirsə YALNIZ mail açılır.

   POÇT QUTUSU ŞƏXSİDİR (`GelenMail.SahibUserId`) — icazə verilən işçi ÖZ
   məktublarını görür, rəhbərinkiləri YOX. Sinxronizasiya işçinin öz SMTP/IMAP
   məlumatı ilə gedir; onu da işçi Profil səhifəsində özü yazır (şifrə
   şifrələnmiş saxlanılır, admin də görmür).

   Kod C#-da da yazılıb: FinNex.UI/Filters/MailIcazesiAttribute.cs
   → `IcazeKodu = "mail_istifade"`. Buradakı `Kod` sütunu ONUNLA HƏRFƏN EYNİ
   olmalıdır; bir hərf fərq olsa icazə heç kimə işləməz və HEÇ BİR XƏTA ÇIXMAZ.

   İCAZƏNİ VERMƏK: script işlədikdən sonra
     Admin panel → İstifadəçi İcazələri → (işçi) → «Mail istifadəsi» → Allowed
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

/* ── Nə dəyişəcək — ƏVVƏLCƏ BUNA BAX ────────────────────────────────────── */
SELECT Id, Kod, Ad FROM Permissions WHERE Kod = N'mail_istifade';
-- Boş qayıdırsa → aşağıdakı INSERT işləyəcək.
-- Sətir varsa  → heç nə dəyişmir (idempotent, təkrar işlətmək təhlükəsizdir).

IF NOT EXISTS (SELECT 1 FROM Permissions
               WHERE Kod = N'mail_istifade' AND ISNULL(Silinib, 0) = 0)
INSERT INTO Permissions (Kod, Ad, Aciqlama, YaradilmaTarixi, Silinib)
VALUES (
    N'mail_istifade',
    N'Mail istifadəsi',
    N'Gələn Maillər + Profildə Mail Ayarları. İşçi YALNIZ öz poçtunu görür. Admin və Rəhbər onsuz da girir.',
    SYSDATETIME(), 0);

COMMIT TRAN;

/* ── Yoxlama ─────────────────────────────────────────────────────────────── */
SELECT Id, Kod, Ad, Aciqlama FROM Permissions WHERE Kod = N'mail_istifade';

/* Kimə verilib? (icazə verildikdən sonra yoxlamaq üçün) */
SELECT u.UserName, p.Kod, up.Allowed
  FROM UserPermissions up
  JOIN Permissions p ON p.Id = up.PermissionId
  JOIN AspNetUsers u ON u.Id = up.UserId
 WHERE p.Kod = N'mail_istifade' AND ISNULL(up.Silinib, 0) = 0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
    THROW;
END CATCH

/* ============================================================================
   GƏLƏCƏK ÜÇÜN — RƏHBƏR ROLUNU TAMAMİLƏ KƏSMƏK İSTƏSƏNİZ

   Hazırda şərt «Admin VƏ YA Rəhbər rolu VƏ YA icazə»dir. Rolu kəsmək üçün
   `MailIcazesiAttribute`-dəki `ElaveRol` sətri silinməlidir — AMMA əvvəlcə
   cari rəhbərlərə bu icazə verilməlidir, yoxsa onlar maili DƏRHAL itirər.

   Hazır SQL (işlətməzdən əvvəl SELECT ilə kimin siyahıya düşdüyünü görün):

   -- SELECT u.UserName FROM AspNetUsers u
   --   JOIN AspNetUserRoles ur ON ur.UserId = u.Id
   --   JOIN AspNetRoles r ON r.Id = ur.RoleId
   --  WHERE r.Name = N'Rehber';
   ============================================================================ */
