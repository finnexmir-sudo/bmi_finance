/* ============================================================================
   MƏKTUB İDXALI — Oracle sorğuları (BMI → FinNex köçürməsi üçün)
   ----------------------------------------------------------------------------
   Dörd sorğu əlavə edir (idempotent — varsa təkrar yaratmır):

     MEKTUB_IDXAL_XARIC_SAYLAR     — il üzrə sətir sayı (vəziyyət ekranı)
     MEKTUB_IDXAL_XARIC_SETIRLER   — bir ilin sətirləri (idxal)
     MEKTUB_IDXAL_DAXIL_SAYLAR     — il üzrə sətir sayı
     MEKTUB_IDXAL_DAXIL_SETIRLER   — bir ilin sətirləri (idxal)

   ── DİQQƏT: TOKEN ────────────────────────────────────────────────────────
   Sətir sorğularında {IL_SERTI} tokeni VAR. Servis onu icradan əvvəl
   "il = 2024" və ya "il IS NULL" ilə əvəz edir. Tokeni SİLMƏ və adını dəyişmə,
   yoxsa hər "Köçür" düyməsi bütün illəri gətirməyə çalışar.

   ── DİQQƏT: SÜTUN ADLARI ─────────────────────────────────────────────────
   İdxal nəticəni sütun ADINA görə oxuyur (KOD, QEY_NOM, GON_YER …). Sorğunu
   redaktə edərkən sütunu silmə/adını dəyişmə. Etsən, idxal başlamır və açıq
   xəta verir ("sütun çatışmır") — bazaya heç nə yazılmır.

   ── ADLAR NİYƏ ASCII-DİR ─────────────────────────────────────────────────
   SSMS-də Azərbaycan hərfləri bəzən pozulur (M?h?mm?d) və LIKE/= müqayisəsi
   sükutla sınır. Ad tam ASCII olduğu üçün bu risk yoxdur.

   MEZMUN (LONG RAW) sorğulara QƏSDƏN daxil deyil — 54 101 sətrin yalnız
   264-ündə var, ayrı keçidlə gətiriləcək.

   Oracle YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

/* Sənəd dövriyyəsi/məktub sorğuları hansı departamentdədirsə oradan; yoxsa ilk aktiv */
SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi LIKE N'MEKTUB%' AND ISNULL(Silinib, 0) = 0
ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler WHERE ISNULL(Silinib, 0) = 0 ORDER BY Id;

IF @DepId IS NULL
BEGIN
    RAISERROR (N'Departament tapılmadı — sorğular əlavə edilmədi.', 16, 1);
    ROLLBACK TRAN;
    RETURN;
END

/* ── 1) XARİC — il üzrə saylar ──────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'MEKTUB_IDXAL_XARIC_SAYLAR' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'MEKTUB_IDXAL_XARIC_SAYLAR',
    N'Xaric məktub — il üzrə sətir sayı (idxal vəziyyət ekranı). Sütunlar: IL, SAY.',
    N'SELECT il, COUNT(*) say FROM odb.xaric_mektub GROUP BY il',
    1, 0, @DepId, SYSDATETIME(), 0);

/* ── 2) XARİC — bir ilin sətirləri ──────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'MEKTUB_IDXAL_XARIC_SETIRLER' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'MEKTUB_IDXAL_XARIC_SETIRLER',
    N'Xaric məktub — bir ilin sətirləri (idxal). {IL_SERTI} tokeni servisdə əvəz olunur. Sütunlar: KOD, QEY_NOM, GON_YER, TARIX, QISA_MEZ, ICRACI, DUBL, MEKTUB_METN, IL.',
    N'SELECT kod, qey_nom, gon_yer, tarix, qisa_mez, icraci, dubl, mektub_metn, il
        FROM odb.xaric_mektub
       WHERE {IL_SERTI}',
    1, 0, @DepId, SYSDATETIME(), 0);

/* ── 3) DAXİL — il üzrə saylar ──────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'MEKTUB_IDXAL_DAXIL_SAYLAR' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'MEKTUB_IDXAL_DAXIL_SAYLAR',
    N'Daxil məktub — il üzrə sətir sayı (idxal vəziyyət ekranı). Sütunlar: IL, SAY.',
    N'SELECT il, COUNT(*) say FROM odb.daxil_mektub GROUP BY il',
    1, 0, @DepId, SYSDATETIME(), 0);

/* ── 4) DAXİL — bir ilin sətirləri ──────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'MEKTUB_IDXAL_DAXIL_SETIRLER' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'MEKTUB_IDXAL_DAXIL_SETIRLER',
    N'Daxil məktub — bir ilin sətirləri (idxal). MEZMUN (LONG RAW) QƏSDƏN yoxdur. {IL_SERTI} tokeni servisdə əvəz olunur. Sütunlar: NOM, NOM1, DAX_TARIX, IDARE_ADI, GON_TARIX, DAX_NOM, MEK_UNVAN, IL.',
    N'SELECT nom, nom1, dax_tarix, idare_adi, gon_tarix, dax_nom, mek_unvan, il
        FROM odb.daxil_mektub
       WHERE {IL_SERTI}',
    1, 0, @DepId, SYSDATETIME(), 0);

COMMIT TRAN;

/* ── Yoxlama — dördü də görünməlidir ─────────────────────────────────────── */
SELECT SorguAdi, Aktiv, DepartamentId, LEN(SorguMetni) AS MetnUzunluq,
       CASE WHEN SorguMetni LIKE N'%{IL_SERTI}%' THEN N'var' ELSE N'—' END AS IlTokeni
FROM OracleSorgular
WHERE SorguAdi LIKE N'MEKTUB_IDXAL_%' AND ISNULL(Silinib,0) = 0
ORDER BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SELECT ERROR_NUMBER() AS XetaNo, ERROR_MESSAGE() AS Xeta;
END CATCH
