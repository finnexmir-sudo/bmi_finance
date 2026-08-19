-- ═══════════════════════════════════════════════════════════════════
-- AVTOPARK — «Invalid object name 'Masinlar'» diaqnostikası
-- 19.08.2026
--
-- YALNIZ SELECT — heç nə dəyişmir.
-- Nəticəni Claude-a göndərin.
-- ═══════════════════════════════════════════════════════════════════

-- 1) Hansı migration-lar bazada QEYDƏ ALINIB?
--    Son 6 sətrə baxın. «20260819000000_Avtopark» varmı?
SELECT TOP 6 MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId DESC;

-- 2) Avtopark cədvəllərindən hansı yaranıb? (boş = heç biri)
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Masinlar','MasinMuracietler','MasinMuddetNovleri',
                     'MasinMuddetler','AvtoparkXeberdarliqAlicilari')
ORDER BY TABLE_NAME;

-- 3) Ondan əvvəlki migration tətbiq olunubmu?
--    (GedenHevale.KocurmeId — 18.08 migration-u)
SELECT CASE WHEN EXISTS (
         SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
         WHERE TABLE_NAME = 'GedenHevale' AND COLUMN_NAME = 'KocurmeId')
       THEN 'VAR' ELSE 'YOXDUR' END AS GedenHevale_KocurmeId;

-- 4) FK-ların istinad etdiyi cədvəllər doğru adlanırmı?
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Departament','Isciler')
ORDER BY TABLE_NAME;
