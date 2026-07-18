/* ============================================================================
   MÜHASİBAT — Depozit sorğusuna ÖZ VALYUTASI qalığı (qaliq_inval)
   ----------------------------------------------------------------------------
   Bir dəfə işlət. "Muhasibat — Depozit hesablari" sorğusunun HƏR İKİ branch-inə
   '-l.saldo_ish_inval qaliq_inval' əlavə edir (deposit qaliq mənfi saxlanır,
   ona görə '-' ilə). Drill-down modalında "Öz valyutası" + "Manat qarşılığı"
   yan-yana görünsün deyə. AZN hesablarda saldo_ish_inval 0-dır — servis onda
   manat qalığını göstərir.

   REPLACE ilə cərrahi (anchor '-l.saldo_ish_nacval qaliq' hər iki branch-də var),
   idempotent. Yalnız SQL — kod tərəfi (DetalAsync) ayrıca commit-də hazırdır.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni,
       '-l.saldo_ish_nacval qaliq',
       '-l.saldo_ish_nacval qaliq, -l.saldo_ish_inval qaliq_inval')
WHERE  SorguAdi = N'Muhasibat — Depozit hesablari'
  AND  SorguMetni NOT LIKE '%qaliq_inval%';

COMMIT TRAN;
PRINT N'Depozit sorğusuna qaliq_inval əlavə olundu (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%qaliq_inval%' THEN 'qaliq_inval VAR' ELSE 'YOX' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Depozit hesablari';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
