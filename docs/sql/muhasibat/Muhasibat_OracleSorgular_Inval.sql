/* ============================================================================
   MÜHASİBAT — Balans sorğusuna ÖZ VALYUTASI qalığı (qaliq_inval)
   ----------------------------------------------------------------------------
   Bir dəfə işlət. "Muhasibat — Balans qaliqlari" sorğusuna 'qaliq_inval' sütunu
   (ar.saldo_ish_inval) əlavə edir. Bu sütun hesabın ÖZ VALYUTASINDAKI qalığıdır.
   DİQQƏT: saldo_ish_inval AZN (val_kod '00') hesablarda 0-dır (yalnız xarici
   valyutada dolu). Servis (DetalAsync) AZN-də manat qalığını göstərir, xarici
   valyutada isə bu sütunu — beləcə drill-down modal-da "Öz valyutası" + "Manat
   qarşılığı" yan-yana görünür. REPLACE ilə cərrahi, idempotent.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni,
       'ar.saldo_ish_nacval AS qaliq,',
       'ar.saldo_ish_nacval AS qaliq,
       ar.saldo_ish_inval AS qaliq_inval,')
WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari'
  AND  SorguMetni NOT LIKE '%qaliq_inval%';

COMMIT TRAN;
PRINT N'qaliq_inval sütunu əlavə olundu (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%qaliq_inval%' THEN 'qaliq_inval VAR' ELSE 'YOX' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Balans qaliqlari';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
