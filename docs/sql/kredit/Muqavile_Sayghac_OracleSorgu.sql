/* ============================================================================
   MÜQAVİLƏ SAYĞACLARI — Oracle sorğusu (BMI → FinNex köçürməsi üçün)
   ----------------------------------------------------------------------------
   Bir sorğu əlavə edir (idempotent — varsa təkrar yaratmır):

     KREDIT_SAYGAC_ORACLE — odb.muqavile_nomreleri-nin bütün sətirləri

   ── SÜTUN ADLARI ─────────────────────────────────────────────────────────
   Köçürmə nəticəni sütun ADINA görə oxuyur (IL, KR_ZAMINLIK, KR_MENZIL …).
   IL sütunu olmasa köçürmə BAŞLAMIR və açıq xəta verir. Digər sütunlardan biri
   yoxdursa həmin sayğac sadəcə ekranda görünmür — qalanları işləyir.

   ── SEMANTİKA (ÇOX VACİB) ────────────────────────────────────────────────
   BMI-də `KR_ZAMINLIK`, `KR_MENZIL` və digər müqavilə sayğacları NÖVBƏTİ
   nömrəni saxlayır (kod dəyəri olduğu kimi işlədir, sonra +1 yazır).
   `KR_ZAMINLER` isə SONUNCU verilmiş nömrəni saxlayır (`kr_zaminler + i`).

   FinNex-də hamısı SONUNCUDUR (MuqavileSayghaci.SonNomre), ona görə köçürmədə
   "növbəti saxlayan" sayğaclardan 1 çıxılır. Köçürmə ekranı hər sətirdə həm xam
   Oracle dəyərini, həm çevrilmiş SonNomre-ni, həm də veriləcək NÖVBƏTİ nömrəni
   göstərir — yazmazdan əvvəl gözlə tutuşdur.

   ── ADLAR NİYƏ ASCII-DİR ─────────────────────────────────────────────────
   SSMS-də Azərbaycan hərfləri bəzən pozulur və LIKE/= müqayisəsi sükutla sınır.

   Oracle YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

/* Məktub/həvalə idxal sorğuları hansı departamentdədirsə oradan; yoxsa ilk aktiv */
SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  (SorguAdi LIKE N'MEKTUB_IDXAL%' OR SorguAdi LIKE N'HEVALE_IDXAL%')
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
               WHERE SorguAdi = N'KREDIT_SAYGAC_ORACLE' AND ISNULL(Silinib,0) = 0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'KREDIT_SAYGAC_ORACLE',
    N'Müqavilə nömrə sayğacları (odb.muqavile_nomreleri) — FinNex-ə köçürmə üçün. Sütunlar: IL, KR_SERENCAM, KR_ZAMINLIK, KR_MENZIL, KR_AVTOMOBIL, KR_ZAMINLER, DEPOZIT, KR_KART, KART_ZAMIN, KR_QIZIL.',
    N'SELECT il, kr_serencam, kr_zaminlik, kr_menzil, kr_avtomobil, kr_zaminler,
             depozit, kr_kart, kart_zamin, kr_qizil
        FROM odb.muqavile_nomreleri
       ORDER BY il',
    1, 0, @DepId, SYSDATETIME(), 0);

COMMIT TRAN;

/* ── Yoxlama ─────────────────────────────────────────────────────────────── */
SELECT SorguAdi, Aktiv, DepartamentId, LEN(SorguMetni) AS MetnUzunluq
FROM OracleSorgular
WHERE SorguAdi = N'KREDIT_SAYGAC_ORACLE' AND ISNULL(Silinib,0) = 0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SELECT ERROR_NUMBER() AS XetaNo, ERROR_MESSAGE() AS Xeta;
END CATCH
