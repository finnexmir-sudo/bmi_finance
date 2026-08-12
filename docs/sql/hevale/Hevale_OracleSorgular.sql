/* ============================================================================
   HƏVALƏ İDXALI — Oracle sorğuları (BMI → FinNex köçürməsi üçün)
   ----------------------------------------------------------------------------
   Dörd sorğu əlavə edir (idempotent — varsa təkrar yaratmır):

     HEVALE_IDXAL_GEDEN_SAYLAR     — il üzrə sətir sayı (vəziyyət ekranı)
     HEVALE_IDXAL_GEDEN_SETIRLER   — bir ilin sətirləri (idxal)
     HEVALE_IDXAL_GELEN_SAYLAR     — il üzrə sətir sayı
     HEVALE_IDXAL_GELEN_SETIRLER   — bir ilin sətirləri (idxal)

   ── MƏKTUBDAN ƏSAS FƏRQ: İL SÜTUNU YOXDUR ───────────────────────────────
   odb.geden_hevale / odb.gelen_hevale cədvəllərində `IL` sütunu YOXDUR.
   İl `TARIX`-dən çıxarılır: EXTRACT(YEAR FROM tarix). Tarixi boş sətirlər
   ayrıca "— (tarixsiz)" qrupudur və `tarix IS NULL` ilə köçürülür.

   ── DİQQƏT: TOKEN ────────────────────────────────────────────────────────
   Sətir sorğularında {IL_SERTI} tokeni VAR. Servis onu icradan əvvəl
   "EXTRACT(YEAR FROM tarix) = 2024" və ya "tarix IS NULL" ilə əvəz edir.
   Tokeni SİLMƏ və adını dəyişmə, yoxsa hər "Köçür" düyməsi bütün illəri
   gətirməyə çalışar.

   ── DİQQƏT: SÜTUN ADLARI ─────────────────────────────────────────────────
   İdxal nəticəni sütun ADINA görə oxuyur (HEV_NOM, SAA, MEBLEG …). Sorğunu
   redaktə edərkən sütunu silmə/adını dəyişmə. Etsən, idxal başlamır və açıq
   xəta verir ("sütun çatışmır") — bazaya heç nə yazılmır.

   ── NİYƏ "DEC" DIRNAQ İÇİNDƏDİR ──────────────────────────────────────────
   Gələn həvalədə `DEC` adlı sütun var — bu, SQL-in ehtiyat sözüdür (DECIMAL
   qısaltması). Dırnaqsız yazılsa sorğu ORA-00936 verə bilər. Dırnaq içində
   Oracle onu böyük hərfli sütun adı kimi oxuyur (cədvəldə də belədir).

   ── ADLAR NİYƏ ASCII-DİR ─────────────────────────────────────────────────
   SSMS-də Azərbaycan hərfləri bəzən pozulur (M?h?mm?d) və LIKE/= müqayisəsi
   sükutla sınır. Ad tam ASCII olduğu üçün bu risk yoxdur.

   Oracle YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

/* Məktub idxal sorğuları hansı departamentdədirsə oradan; yoxsa ilk aktiv */
SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi LIKE N'MEKTUB_IDXAL%' AND ISNULL(Silinib, 0) = 0
ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler WHERE ISNULL(Silinib, 0) = 0 ORDER BY Id;

IF @DepId IS NULL
BEGIN
    RAISERROR (N'Departament tapılmadı — sorğular əlavə edilmədi.', 16, 1);
    ROLLBACK TRAN;
    RETURN;
END

/* ── 1) GEDƏN — il üzrə saylar ──────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'HEVALE_IDXAL_GEDEN_SAYLAR' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'HEVALE_IDXAL_GEDEN_SAYLAR',
    N'Gedən həvalə — il üzrə sətir sayı (idxal vəziyyət ekranı). İl TARIX-dən çıxarılır. Sütunlar: IL, SAY.',
    N'SELECT EXTRACT(YEAR FROM tarix) il, COUNT(*) say
        FROM odb.geden_hevale
       GROUP BY EXTRACT(YEAR FROM tarix)',
    1, 0, @DepId, SYSDATETIME(), 0);

/* ── 2) GEDƏN — bir ilin sətirləri ──────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'HEVALE_IDXAL_GEDEN_SETIRLER' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'HEVALE_IDXAL_GEDEN_SETIRLER',
    N'Gedən həvalə — bir ilin sətirləri (idxal). {IL_SERTI} tokeni servisdə əvəz olunur. Sütunlar: HEV_NOM, HES_NOM, SAA, TIP_RES, MEBLEG, VAL_TIP, TARIX, MEN_OLKE, CONTRAC_NOM, DECLAR_NOM, ARAYIS, OLKE, HEV_TIP, GON_TIP, AL_BANK, ICRA.',
    N'SELECT hev_nom, hes_nom, saa, tip_res, mebleg, val_tip, tarix, men_olke,
             contrac_nom, declar_nom, arayis, olke, hev_tip, gon_tip, al_bank, icra
        FROM odb.geden_hevale
       WHERE {IL_SERTI}',
    1, 0, @DepId, SYSDATETIME(), 0);

/* ── 3) GƏLƏN — il üzrə saylar ──────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'HEVALE_IDXAL_GELEN_SAYLAR' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'HEVALE_IDXAL_GELEN_SAYLAR',
    N'Gələn həvalə — il üzrə sətir sayı (idxal vəziyyət ekranı). İl TARIX-dən çıxarılır. Sütunlar: IL, SAY.',
    N'SELECT EXTRACT(YEAR FROM tarix) il, COUNT(*) say
        FROM odb.gelen_hevale
       GROUP BY EXTRACT(YEAR FROM tarix)',
    1, 0, @DepId, SYSDATETIME(), 0);

/* ── 4) GƏLƏN — bir ilin sətirləri ──────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'HEVALE_IDXAL_GELEN_SETIRLER' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'HEVALE_IDXAL_GELEN_SETIRLER',
    N'Gələn həvalə — bir ilin sətirləri (idxal). {IL_SERTI} tokeni servisdə əvəz olunur. Sütunlar: HEV_NOM, HES_NOM, SAA, TIP_RES, MEBLEG, VAL_TIP, TARIX, MEN_OLKE, HEV_TIP, DEC, DEC_NOM, GEL_OLKE, GON_TIP, AL_BANK, ICRA.',
    N'SELECT hev_nom, hes_nom, saa, tip_res, mebleg, val_tip, tarix, men_olke,
             hev_tip, "DEC", dec_nom, gel_olke, gon_tip, al_bank, icra
        FROM odb.gelen_hevale
       WHERE {IL_SERTI}',
    1, 0, @DepId, SYSDATETIME(), 0);

COMMIT TRAN;

/* ── Yoxlama — dördü də görünməlidir ─────────────────────────────────────── */
SELECT SorguAdi, Aktiv, DepartamentId, LEN(SorguMetni) AS MetnUzunluq,
       CASE WHEN SorguMetni LIKE N'%{IL_SERTI}%' THEN N'var' ELSE N'—' END AS IlTokeni
FROM OracleSorgular
WHERE SorguAdi LIKE N'HEVALE_IDXAL_%' AND ISNULL(Silinib,0) = 0
ORDER BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SELECT ERROR_NUMBER() AS XetaNo, ERROR_MESSAGE() AS Xeta;
END CATCH
