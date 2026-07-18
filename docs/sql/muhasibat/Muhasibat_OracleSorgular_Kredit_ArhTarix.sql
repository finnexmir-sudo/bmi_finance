/* ============================================================================
   MÜHASİBAT — Kredit portfeli sorğusunu TARİXLİ (arh_licschkre) et
   ----------------------------------------------------------------------------
   Əvvəl sorğu canlı odb.licschkre + view_nacpogprokre_all (sysdate) işlədirdi —
   yalnız CARİ vəziyyət, tarixçə yox (bu gün data yoxdursa 0 çıxır).
   İndi arh_licschkre (tarixli snapshot) + {TARIX} parametri:
     - tip      = al.tipkredita
     - teyinat  = al.index_otrasli   (sektor indeksi üzrə bölgü)
     - valyuta  = SUBSTR(al.licschkre,6,2)
     - kurs     = func_get_kurval(..., al.date_oper)
     - esas     = al.summa,  vk = al.summa_19
     - gec_gun  = 0  (DPD/NPL üçün tarixli gecikmə mənbəyi ayrıca əlavə olunacaq)

   Servis sorğuda {TARIX}-i seçilmiş hesabat tarixi ilə əvəz edir.
   Tam UPDATE (kökündən dəyişiklik olduğu üçün REPLACE yox).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = 'SELECT al.tipkredita tip,
       al.index_otrasli teyinat,
       SUBSTR(al.licschkre,6,2) valyuta,
       ROUND(odb.func_get_kurval(SUBSTR(al.licschkre,6,2), al.date_oper), 6) kurs,
       al.summa esas,
       al.summa_19 vk,
       0 gec_gun
FROM   arh_licschkre al
WHERE  al.date_oper = TO_DATE(''{TARIX}'',''dd/mm/yyyy'')
  AND  (al.date_close IS NULL OR al.date_close > TO_DATE(''{TARIX}'',''dd/mm/yyyy''))
  AND  LENGTH(al.licschkre) = 20',
       Mahiyyet = N'Kredit portfeli (arh_licschkre, tarix üzrə — tip/təyinat=index_otrasli/valyuta)',
       Aktiv = 1, Silinib = 0
WHERE  SorguAdi = N'Muhasibat — Kredit portfeli';

COMMIT TRAN;
PRINT N'Kredit portfeli sorğusu arh_licschkre (tarixli) versiyaya keçdi (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%arh_licschkre%' THEN 'ARH (tarixli)' ELSE 'KÖHNƏ' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Kredit portfeli';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
