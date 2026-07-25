/* ============================================================================
   MÜHASİBAT — Kredit portfeli sorğusuna MÜŞTƏRİ ADI (regnom.name_regnom) əlavə et
   ----------------------------------------------------------------------------
   Məqsəd: kredit drill-down siyahısında (dashboard-da karta/segmentə klik) hər
   sətirdə müştərinin adı görünsün. Ad `regnom` cədvəlindədir — sütun name_regnom.

   Join açarı (BMI/FoxPro sxemi, Risk sorğuları ilə eyni):
       SUBSTR(al.licschkre, 10, 6) = r.regnom
   Yəni müştəri nömrəsi 20 simvolluq hesab nömrəsinin 10-cu mövqeyindən 6 simvoldur.

   TƏHLÜKƏSİZLİK: Bu sorğu HƏM drill-down siyahısını, HƏM DƏ aqreqat portfel/say/
   aging rəqəmlərini (317 müqavilə, ~3 302 470.53) qidalandırır. Ona görə müştəri
   adını FROM-a regnom əlavə edib join ilə deyil, KORRELYASİYALI SKALYAR ALT-SORĞU
   ilə gətiririk: alt-sorğu hər kredit sətri üçün DƏQİQ BİR dəyər qaytarır — sətri
   ÇOXALTMIR, deməli aqreqat cəmi/say heç bir halda dəyişə bilməz (regnom-da təkrar
   olsa belə MAX ilə tək dəyər). FROM/WHERE əvvəlki (sınaqdan keçmiş) sorğu ilə
   hərfbəhərf eynidir — yalnız bir SELECT sütunu artır.

   YALNIZ SELECT əlavə olunur — Oracle-a heç bir yazı yoxdur. Bu skript SQL
   Server-də OracleSorgular cədvəlinin SorguMetni sahəsini yeniləyir.

   Əvvəlki versiya: Muhasibat_OracleSorgular_Kredit_ArhTarix.sql
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
       al.licschkre muqavile,
       (SELECT MAX(r.name_regnom) FROM regnom r
         WHERE r.regnom = SUBSTR(al.licschkre,10,6)) musteri
FROM   arh_licschkre al, index_otrasli io, view_nacpogprokre_all x
WHERE  al.index_otrasli = io.index_otrasli(+)
  AND  al.date_oper = TO_DATE(''{TARIX}'',''dd/mm/yyyy'')
  AND  (al.date_close IS NULL OR al.date_close > TO_DATE(''{TARIX}'',''dd/mm/yyyy''))
  AND  LENGTH(al.licschkre) = 20
  AND  al.licschpkre = x.licschpkre(+) AND al.subschkre = x.subschkre(+)
  AND  x.date_oper(+) = al.date_oper',
       Mahiyyet = N'Kredit portfeli (arh_licschkre, tarix üzrə — tip/təyinat/valyuta/DPD + müştəri adı regnom)',
       Aktiv = 1, Silinib = 0
WHERE  SorguAdi = N'Muhasibat — Kredit portfeli';

COMMIT TRAN;
PRINT N'Kredit portfeli sorğusuna müştəri adı (regnom) əlavə edildi (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%name_regnom%' THEN 'MÜŞTƏRİ ADI VAR' ELSE 'KÖHNƏ' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Kredit portfeli';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
