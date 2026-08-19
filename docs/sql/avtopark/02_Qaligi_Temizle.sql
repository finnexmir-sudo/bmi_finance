-- ═══════════════════════════════════════════════════════════════════
-- AVTOPARK — yarımçıq qalığı təmizlə, migration özü işləsin
-- 19.08.2026
--
-- PROBLEM: `Masinlar` cədvəli VAR, qalan 4-ü YOX, `__EFMigrationsHistory`-də
-- isə Avtopark sətri YOXDUR. Ona görə hər başlanğıcda `Migrate()` migration-u
-- əvvəldən tətbiq etməyə çalışır, ilk əmr `CREATE TABLE Masinlar` olduğu üçün
-- «There is already an object named 'Masinlar'» alır və HAMISI geri qayıdır.
-- Bu vəziyyət öz-özünə heç vaxt düzəlmir.
--
-- HƏLL: boş qalıq cədvəlləri sil → app-ı restart et → migration təmiz işləsin.
--
-- ⚠️ TƏHLÜKƏSİZLİK: cədvəl YALNIZ BOŞ olduqda silinir. Bir dənə də sətir
-- varsa skript ona TOXUNMUR və ekranda xəbərdarlıq yazır.
-- Bu cədvəllər bu gün yaranıb və içi boş olmalıdır.
--
-- ADDIM 1-i işlət, nəticəyə bax. Sətir sayı 0-dırsa ADDIM 2-ni işlət.
-- ═══════════════════════════════════════════════════════════════════

-- ═══ ADDIM 1 — VƏZİYYƏTİ GÖR (yalnız SELECT, heç nə dəyişmir) ═══════

SELECT TABLE_NAME AS MovcudCedvel
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Masinlar','MasinMuracietler','MasinMuddetNovleri',
                     'MasinMuddetler','AvtoparkXeberdarliqAlicilari')
ORDER BY TABLE_NAME;

SELECT MigrationId
FROM __EFMigrationsHistory
WHERE MigrationId LIKE '%Avtopark%';   -- boş gözlənilir

-- Hər mövcud cədvəldəki sətir sayı — HAMISI 0 OLMALIDIR
SELECT 'Masinlar' AS Cedvel, COUNT(*) AS SetirSayi FROM [Masinlar]
UNION ALL SELECT 'MasinMuracietler', COUNT(*) FROM [MasinMuracietler]
UNION ALL SELECT 'MasinMuddetNovleri', COUNT(*) FROM [MasinMuddetNovleri]
UNION ALL SELECT 'MasinMuddetler', COUNT(*) FROM [MasinMuddetler]
UNION ALL SELECT 'AvtoparkXeberdarliqAlicilari', COUNT(*) FROM [AvtoparkXeberdarliqAlicilari];
-- QEYD: mövcud olmayan cədvəl üçün bu sorğu xəta verəcək — normaldır,
-- yuxarıdakı birinci SELECT hansının olduğunu göstərir. Yalnız MÖVCUD
-- olanların sətrini əl ilə yoxlayın.

GO

-- ═══ ADDIM 2 — QALIĞI SİL (yalnız 1-ci addım 0 göstərəndə) ══════════
-- Silinmə sırası ƏHƏMİYYƏTLİDİR: asılı cədvəllər əvvəl gedir, yoxsa FK
-- silinməyə imkan vermir.

-- 2.1 MasinMuddetler (Masinlar + MasinMuddetNovleri-dən asılıdır)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasinMuddetler')
BEGIN
    IF (SELECT COUNT(*) FROM [MasinMuddetler]) = 0
    BEGIN
        DROP TABLE [MasinMuddetler];
        PRINT 'MasinMuddetler — silindi (bos idi)';
    END
    ELSE PRINT 'XEBERDARLIQ: MasinMuddetler BOS DEYIL — TOXUNULMADI';
END

-- 2.2 MasinMuddetNovleri
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasinMuddetNovleri')
BEGIN
    IF (SELECT COUNT(*) FROM [MasinMuddetNovleri]) = 0
    BEGIN
        DROP TABLE [MasinMuddetNovleri];
        PRINT 'MasinMuddetNovleri — silindi (bos idi)';
    END
    ELSE PRINT 'XEBERDARLIQ: MasinMuddetNovleri BOS DEYIL — TOXUNULMADI';
END

-- 2.3 MasinMuracietler (Masinlar-dan asılıdır)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasinMuracietler')
BEGIN
    IF (SELECT COUNT(*) FROM [MasinMuracietler]) = 0
    BEGIN
        DROP TABLE [MasinMuracietler];
        PRINT 'MasinMuracietler — silindi (bos idi)';
    END
    ELSE PRINT 'XEBERDARLIQ: MasinMuracietler BOS DEYIL — TOXUNULMADI';
END

-- 2.4 AvtoparkXeberdarliqAlicilari (heç nədən asılı deyil)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AvtoparkXeberdarliqAlicilari')
BEGIN
    IF (SELECT COUNT(*) FROM [AvtoparkXeberdarliqAlicilari]) = 0
    BEGIN
        DROP TABLE [AvtoparkXeberdarliqAlicilari];
        PRINT 'AvtoparkXeberdarliqAlicilari — silindi (bos idi)';
    END
    ELSE PRINT 'XEBERDARLIQ: AvtoparkXeberdarliqAlicilari BOS DEYIL — TOXUNULMADI';
END

-- 2.5 Masinlar — ƏN SONDA (hamısı ondan asılı idi)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Masinlar')
BEGIN
    IF (SELECT COUNT(*) FROM [Masinlar]) = 0
    BEGIN
        DROP TABLE [Masinlar];
        PRINT 'Masinlar — silindi (bos idi)';
    END
    ELSE PRINT 'XEBERDARLIQ: Masinlar BOS DEYIL — TOXUNULMADI';
END

-- Tarixçədə Avtopark sətri qalıbsa götürülür — yoxsa migration
-- «artıq tətbiq olunub» sayılar və cədvəllər HEÇ VAXT yaranmaz.
DELETE FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260819000000_Avtopark';
GO

-- ═══ ADDIM 3 — YOXLA (hamısı boş olmalıdır) ═════════════════════════
SELECT TABLE_NAME AS QalanCedvel
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Masinlar','MasinMuracietler','MasinMuddetNovleri',
                     'MasinMuddetler','AvtoparkXeberdarliqAlicilari');
-- BOŞ nəticə gözlənilir

-- ═══ ADDIM 4 — TƏTBİQİ RESTART ET ══════════════════════════════════
-- Migration indi TƏMİZ bazada işləyəcək.
--
--   • Keçsə  → `Logs\log-….txt`-də «[Migration]» sətri, cədvəllər hazır.
--   • Sınsa  → `Logs\log-….txt`-də «[Migration XƏTA]» + SQL Server-in
--              ƏSL mətni. Həmin mətni göndər — kök səbəb orada olacaq.
