/* ============================================================================
   AVTOPARK — «avtopark_idare» icazəsi
   ----------------------------------------------------------------------------
   NƏ EDİR: `Permissions` cədvəlinə bir sətir əlavə edir (idempotent).

   NİYƏ: Avtopark → Maşınlar və Müddətlər səhifələri əvvəl yalnız `Admin` rolu
   ilə açılırdı. Təsərrüfat işçisinə maşın kartını açmaq üçün TAM ADMİN vermək
   lazım gəlirdi — o isə maaşdan sistem ayarlarına qədər hər şeyi açır.

   İndi ayrıca icazə var:
     · Admin — həmişə girir (icazə lazım deyil);
     · Digər işçi — Admin panel → Users → «İcazələr» bölməsindən bu icazə
       verilir və YALNIZ Avtopark idarəetməsi açılır.

   Kod C#-da da yazılıb: FinNex.UI/Areas/Avtopark/AvtoparkIdareIcazesiAttribute.cs
   → `Kod = "avtopark_idare"`. Buradakı `Kod` sütunu ONUNLA HƏRFƏN EYNİ olmalıdır;
   fərq olsa icazə heç kimə işləməz və heç bir xəta çıxmaz.

   İCAZƏNİ VERMƏK: script işlədikdən sonra
     Admin panel → Users → (işçi) → İcazələr → «Avtopark idarəetməsi» → Allowed
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

/* ── Nə dəyişəcək — ƏVVƏLCƏ BUNA BAX ────────────────────────────────────── */
SELECT Id, Kod, Ad FROM Permissions WHERE Kod = N'avtopark_idare';
-- Boş qayıdırsa → aşağıdakı INSERT işləyəcək.
-- Sətir varsa  → heç nə dəyişmir (idempotent).

IF NOT EXISTS (SELECT 1 FROM Permissions
               WHERE Kod = N'avtopark_idare' AND ISNULL(Silinib, 0) = 0)
INSERT INTO Permissions (Kod, Ad, Aciqlama, YaradilmaTarixi, Silinib)
VALUES (
    N'avtopark_idare',
    N'Avtopark idarəetməsi',
    N'Avtopark → Maşınlar və Müddətlər səhifələrini açır (maşın kartı, sığorta/texniki baxış müddətləri, xəbərdarlıq alıcıları). Admin bu icazə olmadan da girir.',
    SYSDATETIME(), 0);

COMMIT TRAN;

/* ── Yoxlama ─────────────────────────────────────────────────────────────── */
SELECT Id, Kod, Ad, Aciqlama FROM Permissions WHERE Kod = N'avtopark_idare';

/* Kimə verilib? (icazə verildikdən sonra yoxlamaq üçün) */
SELECT u.UserName, p.Kod, up.Allowed
  FROM UserPermissions up
  JOIN Permissions p ON p.Id = up.PermissionId
  JOIN AspNetUsers u ON u.Id = up.UserId
 WHERE p.Kod = N'avtopark_idare' AND ISNULL(up.Silinib, 0) = 0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
    THROW;
END CATCH
