/* ============================================================================
   MÜHASİBAT — Depozit sorğusuna SAHİBKAR seqmenti (predprinimatel)
   ----------------------------------------------------------------------------
   Bir dəfə işlət. "Muhasibat — Depozit hesablari" sorğusunda müştərinin
   regnom.predprinimatel bayrağı 1-dirsə tip='sahibkar' olur (əks halda
   mövcud 'huquqi'/'fiziki' qalır). Hər iki branch onsuz da regnom (r) join edir.

   Nəticə (16/07/2026 test): predprinimatel=1 → 12 müştəri, 101 528.30 (hamısı 41
   fiziki hesablarda). Fiziki 555 461 → 453 933, Sahibkar 101 528; cəm dəyişmir.

   REPLACE ilə cərrahi, idempotent (guard: predprinimatel hələ yoxdursa dəyişir).
   Yalnız SQL — kod tərəfi (DepozitAsync, DTO, view) ayrıca commit-də hazırdır.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = REPLACE(REPLACE(SorguMetni,
       '''huquqi'' tip',
       'CASE WHEN r.predprinimatel = 1 THEN ''sahibkar'' ELSE ''huquqi'' END tip'),
       '''fiziki'' tip',
       'CASE WHEN r.predprinimatel = 1 THEN ''sahibkar'' ELSE ''fiziki'' END tip')
WHERE  SorguAdi = N'Muhasibat — Depozit hesablari'
  AND  SorguMetni NOT LIKE '%predprinimatel%';

COMMIT TRAN;
PRINT N'Sahibkar seqmenti əlavə olundu (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%predprinimatel%' THEN 'SAHİBKAR VAR' ELSE 'YOX' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Depozit hesablari';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
