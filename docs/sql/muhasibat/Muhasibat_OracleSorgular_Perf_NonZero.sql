/* ============================================================================
   MÜHASİBAT — Balans sorğusuna saldo_ish_nacval <> 0 filtri (SÜRƏT)
   ----------------------------------------------------------------------------
   PROBLEM: Bağlı hesab filtri (date_close_licsch) götürüləndən sonra sorğu
   MİNLƏRLƏ sıfır qalıqlı (əsasən bağlı) hesabı da qaytarırdı — 15/07/2026-da
   ~24 800 sətir, 9+ saniyə. C# onsuz da qaliq=0 olanları atırdı, amma bu qədər
   sətri Oracle-dan gətirmək Balans İcmalı səhifəsini lənglədirdi.

   HƏLL: sıfır qalığı DB səviyyəsində filtrlə. Bu:
     - sıfır qalıqlı hesabları (bağlı olsun/olmasın) atır → az sətir, sürətli
     - qalığı OLAN hesabı (bağlı olsa belə) saxlayır → balans dəqiq qalır (49.92)
     - nəticə C# ilə eynidir (qaliq=0 onsuz da atılırdı)

   Anchor "ch.licsch = ar.licsch" (join şərti) — sorğuda bir dəfə var.
   REPLACE ilə cərrahi, idempotent. Yalnız SQL — kod/rebuild lazım deyil.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni,
       'ch.licsch = ar.licsch',
       'ch.licsch = ar.licsch AND ar.saldo_ish_nacval <> 0')
WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari'
  AND  SorguMetni NOT LIKE '%saldo_ish_nacval <> 0%';

COMMIT TRAN;
PRINT N'saldo_ish_nacval <> 0 filtri əlavə olundu (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%saldo_ish_nacval <> 0%' THEN 'FILTR VAR (sürətli)' ELSE 'YOX' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Balans qaliqlari';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
