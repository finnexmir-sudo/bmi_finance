/* ============================================================================
   MÜHASİBAT — Maturity "kontekst" sorğusunun HQLA-sını YENİLƏ (UPDATE)
   ----------------------------------------------------------------------------
   Əvvəlki versiya likvid tamponu yalnız 15770/11710 ilə sayırdı → HQLA=0.
   Bu UPDATE onu LikvidQrup ilə eyni geniş likvid aktiv dəstinə keçirir:
   kassa (100), müxbir (11010/11020/11110/11710), qiymətli kağız (14010-14034),
   cari likvid (15020/15025/15770). Nəticə Likvidlik tab-ı ilə uyğun (~113M).
   (INSERT script IF NOT EXISTS olduğu üçün mövcud sorğunu yeniləmir — bu lazımdır.)
   Oracle YALNIZ SELECT. Azərbaycan hərfi YOXDUR.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = N'select
  round(sum(case when substr(s.licsch,1,2) in (''35'',''36'',''38'',''39'',''40'',''41'',''49'') then abs(s.saldo_ish_nacval) else 0 end),2) depozit,
  round(sum(case when s.saldo_ish_nacval > 0 and (
                 substr(s.licsch,1,3) = ''100''
              or substr(s.licsch,1,5) in (''11010'',''11020'',''11110'',''11710'')
              or substr(s.licsch,1,5) in (''14010'',''14012'',''14014'',''14030'',''14032'',''14034'')
              or substr(s.licsch,1,5) in (''15020'',''15025'',''15770'')
            ) then s.saldo_ish_nacval else 0 end),2) hqla
from odb.arh_saldo_ls s
where s.date_oper = (select max(date_oper) from odb.arh_saldo_ls
                     where date_oper <= to_date(''{TARIX}'',''dd/mm/yyyy''))',
       Mahiyyet = N'Tələbli depozit bazası (35-49) + likvid aktivlər (LikvidQrup ilə eyni)'
WHERE  SorguAdi = N'Muhasibat — Maturity kontekst' AND ISNULL(Silinib,0)=0;

COMMIT TRAN;
PRINT N'Maturity kontekst yeniləndi (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%14010%' THEN 'YENİ (geniş likvid)' ELSE 'KÖHNƏ' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Maturity kontekst' AND ISNULL(Silinib,0)=0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
