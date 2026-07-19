/* ============================================================================
   MÜHASİBAT — Kredit keyfiyyəti: yalnız qalığı olan kreditləri say (UPDATE)
   ----------------------------------------------------------------------------
   Problem: aqreqatdakı `say` (count) qalığı 0 olan kreditləri də sayırdı
   (məs. GİROVSUZ · 3, amma drill-down 1 sətir — 2 kreditin qalığı 0-dır).
   Həll: bütün keyfiyyət sorğularına `(al.summa+al.summa_19) <> 0` filtri
   əlavə olunur — say/qalıq/drill-down üst-üstə düşür. Qalıq/ehtiyat cəmi
   dəyişmir (0-qalıqlı onsuz da 0 verir), yalnız SAY düzəlir.

   REPLACE ilə `length(al.licschkre) = 20` sətrindən sonra filtri əlavə edir
   (idempotent — təkrar işlətsən ikinci dəfə əlavə etmir).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni,
         'and length(al.licschkre) = 20',
         'and length(al.licschkre) = 20
  and (al.summa+al.summa_19) <> 0')
WHERE  SorguAdi IN (N'Muhasibat — Kredit keyfiyyet',
                    N'Muhasibat — Kredit girov',
                    N'Muhasibat — Kredit keyfiyyet baza',
                    N'Muhasibat — Kredit keyfiyyet detal')
  AND  SorguMetni LIKE '%length(al.licschkre) = 20%'
  AND  SorguMetni NOT LIKE '%(al.summa+al.summa_19) <> 0%'
  AND  ISNULL(Silinib,0)=0;

COMMIT TRAN;
PRINT N'Kredit keyfiyyəti sorğularına qalıq<>0 filtri əlavə olundu (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT SorguAdi,
       CASE WHEN SorguMetni LIKE '%(al.summa+al.summa_19) <> 0%' THEN 'YENİ (qalıq<>0)' ELSE 'KÖHNƏ' END AS veziyyet
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Kredit keyfiyyet', N'Muhasibat — Kredit girov',
                    N'Muhasibat — Kredit keyfiyyet baza', N'Muhasibat — Kredit keyfiyyet detal')
  AND  ISNULL(Silinib,0)=0
ORDER BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
