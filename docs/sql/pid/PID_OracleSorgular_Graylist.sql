/* ============================================================================
   PID — Gray list ödənişləri Oracle sorğusu (Ödənişə Nəzarət, Gray tab)
   ----------------------------------------------------------------------------
   Bir sorğu əlavə edir (idempotent — varsa təkrar yaratmır):
     "PID Odenis Nezaret Graylist" — arh_dd-dən graylist ödəniş əməliyyatları.

   Hesab cütü SABİTDİR:
     debet  = 45019000000000400014  (təmin olunan vəsait / yığım transit)
     kredit = 89150000010000700000  (balansdankənar graylist)

   Sorğu XAM sətirləri qaytarır (tarix, məbləğ, təyinat) — müştəri adı təyinatın
   ilk mötərizəsindən SERVİSDƏ (OdenisNezaretiService.GraySiyahiAsync) çıxarılır,
   qruplaşma da orada olur. Ona görə burada aqreqasiya YOXDUR.

   Servis sorğunu ada görə tapır: adında "Odenis" + "Nezaret" + "Gray"
   (normalizasiya: azərbaycan hərfləri ASCII-yə, boşluqlar silinir).
   Adi siyahının axtarışı "gray"-i İSTİSNA edir — qarışmır.

   Oracle YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

/* PID sorğuları hansı departamentdədirsə, oradan götür; tapılmasa ilk uyğun */
SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  (SorguAdi LIKE N'%PID%' OR SorguAdi LIKE N'%Nezaret%' OR SorguAdi LIKE N'%Nəzarət%')
  AND  ISNULL(Silinib,0)=0
ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler WHERE ISNULL(Silinib,0)=0 ORDER BY Id;

IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'PID Odenis Nezaret Graylist' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'PID Odenis Nezaret Graylist',
    N'Gray list ödənişləri — arh_dd (debet 45019…400014 → kredit 89150…700000). Müştəri təyinatdan çıxarılır.',
    N'SELECT a.date_oper AS tarix,
       a.summa_v_nacval AS mebleg,
       a.primechanie    AS teyinat
  FROM odb.arh_dd a
 WHERE a.debet  = ''45019000000000400014''
   AND a.kredit = ''89150000010000700000''
   AND NVL(a.summa_v_nacval, 0) <> 0
 ORDER BY a.date_oper DESC',
    1, 0, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'PID Graylist sorğusu əlavə olundu.';

SELECT SorguAdi, DepartamentId, Aktiv FROM OracleSorgular
WHERE  SorguAdi = N'PID Odenis Nezaret Graylist';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
