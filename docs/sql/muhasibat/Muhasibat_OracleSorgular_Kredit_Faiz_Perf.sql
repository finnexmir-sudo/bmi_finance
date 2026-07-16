/* ============================================================================
   MÜHASİBAT — kredit_novu yarımsorğusunun SÜRƏTLƏNDİRİLMƏSİ
   ----------------------------------------------------------------------------
   PROBLEM: "Muhasibat — Balans qaliqlari" sorğusundakı kredit_novu CASE-i
   HƏR SƏTİR üçün ağır "ar.licsch IN (select ... union select ... from
   arh_licschkre)" yoxlamasını işlədirdi. Halbuki bu bayraq yalnız 20-23
   (müştəri kreditləri) hesabları üçün oxunur — digər minlərlə hesab (kassa,
   müxbir, depozit, kapital...) boş yerə həmin skandan keçirdi → sorğu ləngləyirdi.

   HƏLL: CASE-ə əvvəldən UCUZ substr yoxlaması qoyuruq. SQL CASE qısa-qapanır:
   hesab 20-23 deyilsə, ağır IN heç işə düşmür. Nəticə eynidir (kredit_novu
   onsuz da yalnız 20-23 üçün istifadə olunur), amma çox daha sürətli.

   REPLACE ilə cərrahi, idempotent (guard: yalnız qorunmamış forma varsa dəyişir).
   Yalnız SQL — kod/rebuild lazım deyil.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni,
       'CASE WHEN ar.licsch IN (',
       'CASE WHEN SUBSTR(ar.licsch,1,2) IN (''20'',''21'',''22'',''23'') AND ar.licsch IN (')
WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari'
  AND  SorguMetni LIKE '%CASE WHEN ar.licsch IN (%';   -- yalnız qorunmamış forma

COMMIT TRAN;
PRINT N'kredit_novu yarımsorğusu 20-23 ilə qorundu (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%SUBSTR(ar.licsch,1,2) IN (''20'',''21'',''22'',''23'') AND ar.licsch IN%'
            THEN 'QORUNUB (sürətli)' ELSE 'HƏLƏ KÖHNƏ' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Balans qaliqlari';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
