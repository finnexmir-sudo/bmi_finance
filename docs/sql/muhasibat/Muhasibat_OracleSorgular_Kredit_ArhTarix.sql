/* ============================================================================
   MÜHASİBAT — Kredit portfeli sorğusunu TARİXLİ (arh_licschkre) et
   ----------------------------------------------------------------------------
   Əvvəl sorğu canlı odb.licschkre + view_nacpogprokre_all (sysdate) işlədirdi —
   yalnız CARİ vəziyyət. İndi arh_licschkre (tarixli snapshot) + {TARIX}:
     - tip      = al.tipkredita          (1=hüquqi, 2=fiziki, 3=sahibkar)
     - teyinat  = index_otrasli cədvəlindən AD (io.name_index_otrasli),
                  kod al.index_otrasli ilə join olunur (outer — kredit itməsin)
     - valyuta  = SUBSTR(al.licschkre,6,2)
     - kurs     = func_get_kurval(..., al.date_oper)
     - esas     = al.summa,  vk = al.summa_19
     - gec_gun  = odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate, x.date_oper))
                  — view_nacpogprokre_all (x) həmin date_oper üzrə join olunur (tarixli DPD)

   Test (16/07/2026): 317 müqavilə, esas+vk ≈ 3 302 470.53.
   Servis {TARIX}-i seçilmiş hesabat tarixi ilə əvəz edir. Tam UPDATE.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

UPDATE OracleSorgular
SET    SorguMetni = 'SELECT al.tipkredita tip,
       NVL(io.name_index_otrasli, ''(təyinatsız)'') teyinat,
       SUBSTR(al.licschkre,6,2) valyuta,
       ROUND(odb.func_get_kurval(SUBSTR(al.licschkre,6,2), al.date_oper), 6) kurs,
       al.summa esas,
       al.summa_19 vk,
       odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate, x.date_oper)) gec_gun,
       al.licschkre muqavile
FROM   arh_licschkre al, index_otrasli io, view_nacpogprokre_all x
WHERE  al.index_otrasli = io.index_otrasli(+)
  AND  al.date_oper = TO_DATE(''{TARIX}'',''dd/mm/yyyy'')
  AND  (al.date_close IS NULL OR al.date_close > TO_DATE(''{TARIX}'',''dd/mm/yyyy''))
  AND  LENGTH(al.licschkre) = 20
  AND  al.licschpkre = x.licschpkre AND al.subschkre = x.subschkre
  AND  x.date_oper = al.date_oper',
       Mahiyyet = N'Kredit portfeli (arh_licschkre, tarix üzrə — tip/təyinat=index_otrasli/valyuta/DPD)',
       Aktiv = 1, Silinib = 0
WHERE  SorguAdi = N'Muhasibat — Kredit portfeli';

COMMIT TRAN;
PRINT N'Kredit portfeli sorğusu arh_licschkre (tarixli, index_otrasli adı) versiyaya keçdi (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sorğu).';

SELECT CASE WHEN SorguMetni LIKE '%name_index_otrasli%' THEN 'ARH + index_otrasli adı' ELSE 'KÖHNƏ' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Kredit portfeli';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
