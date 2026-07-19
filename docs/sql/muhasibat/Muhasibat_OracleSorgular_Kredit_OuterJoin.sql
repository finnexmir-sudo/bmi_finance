/* ============================================================================
   MÜHASİBAT — Kredit portfeli: DPD join OUTER (bütün AÇIQ kreditlər) — UPDATE
   ----------------------------------------------------------------------------
   Problem: sorğu view_nacpogprokre_all-a INNER join edirdi — DPD sətri olmayan
   (məs. qalığı 0, amma AÇIQ) kreditlər siyahıdan düşürdü. Nəticədə "aktiv
   müqavilə sayı" 268 çıxırdı, halbuki açıq müqavilə 317-dir (Keyfiyyət ilə).

   Həll: x (view_nacpogprokre_all) OUTER join olur — açıq bütün kreditlər gəlir,
   DPD sətri olmayanda gec_gun = 0. Beləliklə say bütün AÇIQ müqavilələr üzrədir
   (date_close null), qalığı 0 olsa belə. Qalıq/NPL/aging MƏBLƏĞLƏRİ dəyişmir
   (0-qalıqlı 0 verir), yalnız SAY 268→317 olur.

   Əlavə: NVL(...,''(təyinatsız)'') Azərbaycan literalı SQL-dən çıxarıldı
   (Oracle charset ''ə''-nı pozur) — teyinat null gəlir, servis C#-da düzgün
   ''(təyinatsız)'' qoyur.
   Oracle YALNIZ SELECT. {TARIX} servis tərəfindən əvəz olunur.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = 'SELECT al.tipkredita tip,
       io.name_index_otrasli teyinat,
       SUBSTR(al.licschkre,6,2) valyuta,
       ROUND(odb.func_get_kurval(SUBSTR(al.licschkre,6,2), al.date_oper), 6) kurs,
       al.summa esas,
       al.summa_19 vk,
       CASE WHEN x.licschpkre IS NULL THEN 0
            ELSE odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate, x.date_oper)) END gec_gun,
       al.licschkre muqavile
FROM   arh_licschkre al, index_otrasli io, view_nacpogprokre_all x
WHERE  al.index_otrasli = io.index_otrasli(+)
  AND  al.date_oper = TO_DATE(''{TARIX}'',''dd/mm/yyyy'')
  AND  (al.date_close IS NULL OR al.date_close > TO_DATE(''{TARIX}'',''dd/mm/yyyy''))
  AND  LENGTH(al.licschkre) = 20
  AND  al.licschpkre = x.licschpkre(+) AND al.subschkre = x.subschkre(+)
  AND  x.date_oper(+) = al.date_oper',
       Mahiyyet = N'Kredit portfeli (arh_licschkre, açıq üzrə — DPD outer join, say=açıq müqavilə)'
WHERE  SorguAdi = N'Muhasibat — Kredit portfeli' AND ISNULL(Silinib,0)=0;

COMMIT TRAN;
PRINT N'Kredit portfeli sorğusu OUTER join (açıq üzrə) versiyaya keçdi (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%x.licschpkre(+)%' THEN 'YENİ (outer, açıq üzrə)' ELSE 'KÖHNƏ (inner)' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Kredit portfeli' AND ISNULL(Silinib,0)=0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
