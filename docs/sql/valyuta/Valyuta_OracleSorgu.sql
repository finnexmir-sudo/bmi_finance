/* ============================================================================
   VALYUTA SİYAHISI — Oracle sorğusu (həvalə formalarındakı açılan siyahı üçün)
   ----------------------------------------------------------------------------
   Bir sorğu əlavə edir (idempotent — varsa təkrar yaratmır):

     VALYUTA_SIYAHISI — kurval cədvəlindən kod + ad

   ── NİYƏ ORACLE-DAN OXUNUR ───────────────────────────────────────────────
   Valyuta siyahısı 6 sətirdir və mənbəyi BMI-nin əsas bankçılıq sistemidir.
   FinNex-ə köçürsək, BMI yeni valyuta əlavə edəndə bizdə görünməzdi. Ona görə
   canlı oxunur — cədvəl yaradılmır, idxal yoxdur.

   ── SÜTUNLAR ─────────────────────────────────────────────────────────────
   SOKNAMEVALUT — kod  ("00", "01", "02"…)  → formada SAXLANILAN dəyər
   NAMEVALUTI   — ad   ("ABŞ DOLLARI"…)     → yalnız göstərmə üçün

   `kurval`-da USD/EUR kimi beynəlxalq qısaltma YOXDUR — ona görə həvalə
   qeydində valyuta KOD kimi saxlanılır (13.08.2026 qərarı).

   Sorğu tapılmasa və ya Oracle əlçatmaz olsa, `ValyutaService` sabit ehtiyat
   siyahı ilə işləyir — forma sıradan çıxmır.

   Oracle YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

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
               WHERE SorguAdi = N'VALYUTA_SIYAHISI' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'VALYUTA_SIYAHISI',
    N'Valyuta siyahısı (kurval) — həvalə formalarındakı açılan siyahı. Sütunlar: SOKNAMEVALUT (kod, saxlanılan dəyər), NAMEVALUTI (ad, göstərmə).',
    N'SELECT soknamevalut, namevaluti FROM kurval ORDER BY soknamevalut',
    1, 0, @DepId, SYSDATETIME(), 0);

COMMIT TRAN;

/* ── Yoxlama ─────────────────────────────────────────────────────────────── */
SELECT SorguAdi, Aktiv, DepartamentId, LEN(SorguMetni) AS MetnUzunluq
FROM OracleSorgular
WHERE SorguAdi = N'VALYUTA_SIYAHISI' AND ISNULL(Silinib,0) = 0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SELECT ERROR_NUMBER() AS XetaNo, ERROR_MESSAGE() AS Xeta;
END CATCH
