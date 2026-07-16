/* ============================================================================
   MÜHASİBAT — Balans sorğusuna KREDİT FAİZ bayrağı (kredit_novu)
   ----------------------------------------------------------------------------
   Bir dəfə işlət. "Muhasibat — Balans qaliqlari" sorğusuna 'kredit_novu' sütunu
   əlavə edir: hesab arh_licschkre-nin FAİZ sütunlarında (licschpkre / licschppkre)
   varsa 'F', əks halda 'E'. Servis 20-23 kredit hesablarını buna görə ayırır:
   faiz → "Hesablanmış faizlər və digər aktivlər" (öz təbii yeri), əsas → "Müştərilərə kreditlər".
   REPLACE ilə cərrahi, idempotent.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni,
       'ch.name_licsch AS ad',
       'ch.name_licsch AS ad,
       CASE WHEN ar.licsch IN (
              select lk.licschpkre  from arh_licschkre lk where lk.date_oper = TO_DATE(''{TARIX}'',''dd/mm/yyyy'')
              union
              select lk.licschppkre from arh_licschkre lk where lk.date_oper = TO_DATE(''{TARIX}'',''dd/mm/yyyy'')
            ) THEN ''F'' ELSE ''E'' END AS kredit_novu')
WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari'
  AND  SorguMetni NOT LIKE '%kredit_novu%';

COMMIT TRAN;
PRINT N'kredit_novu bayrağı əlavə olundu (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%kredit_novu%' THEN 'kredit_novu VAR' ELSE 'YOX' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Balans qaliqlari';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
