/* ============================================================================
   ÖLKƏ SİYAHISI — Oracle sorğusu (kredit müqaviləsi formalarındakı açılan siyahı)
   ----------------------------------------------------------------------------
   Bir sorğu əlavə edir (idempotent — varsa təkrar yaratmır):

     OLKE_SIYAHISI — countrycode cədvəlindən kod + ad

   ── NİYƏ ORACLE-DAN OXUNUR ───────────────────────────────────────────────
   Ölkə kataloqu BMI-nindir. FinNex-ə köçürsək, BMI-də yeni ölkə əlavə olunanda
   və ya ad dəyişəndə bizdə köhnə qalardı. Valyuta (kurval) ilə eyni qayda.

   ── SÜTUNLAR ─────────────────────────────────────────────────────────────
   CODE — ISO-3 kodu ("AZE", "TUR", "IRN"…)
   NAME — Azərbaycan dilində tam ad ("Azərbaycan Respublikası")

   MÜQAVİLƏYƏ **AD** DÜŞÜR, kod yox: şablon mətni «{k_olke}nın vətəndaşı»
   şəklindədir. Kod yalnız Oracle-dan gələn dəyəri (məs. zaminin COUNTRYCODE-u)
   ada çevirmək üçün işlədilir.

   Adı boş olan sətirlər (məs. kod IIR) servisdə süzülür — formada boş sətir
   görünməsin deyə.

   Oracle YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi IN (N'VALYUTA_SIYAHISI', N'KREDIT_SAYGAC_ORACLE')
  AND  ISNULL(Silinib, 0) = 0
ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
    WHERE  (SorguAdi LIKE N'HEVALE_IDXAL%' OR SorguAdi LIKE N'MEKTUB_IDXAL%')
      AND  ISNULL(Silinib, 0) = 0
    ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler WHERE ISNULL(Silinib, 0) = 0 ORDER BY Id;

IF @DepId IS NULL
BEGIN
    RAISERROR (N'Departament tapılmadı — sorğu əlavə edilmədi.', 16, 1);
    ROLLBACK TRAN;
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'OLKE_SIYAHISI' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'OLKE_SIYAHISI',
    N'Ölkə kataloqu (countrycode) — kredit müqaviləsi formalarındakı açılan siyahı. Sütunlar: CODE (ISO-3), NAME (Azərbaycan dilində ad, müqaviləyə bu düşür).',
    N'SELECT code, name FROM countrycode ORDER BY name',
    1, 0, @DepId, SYSDATETIME(), 0);

COMMIT TRAN;

/* ── Yoxlama ─────────────────────────────────────────────────────────────── */
SELECT SorguAdi, Aktiv, DepartamentId, LEN(SorguMetni) AS MetnUzunluq
FROM OracleSorgular
WHERE SorguAdi = N'OLKE_SIYAHISI' AND ISNULL(Silinib,0) = 0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SELECT ERROR_NUMBER() AS XetaNo, ERROR_MESSAGE() AS Xeta;
END CATCH
