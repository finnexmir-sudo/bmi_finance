-- ═══════════════════════════════════════════════════════════════════
-- AVTOPARK — vəziyyət yoxlaması (yalnız SELECT, heç nə dəyişmir)
-- 19.08.2026
--
-- KÖK SƏBƏB TAPILDI VƏ KODDA DÜZƏLDİLDİ (bax: CLAUDE.md — «Əl ilə Yazılan
-- Migration — InsertData İŞLƏMİR»). Bu fayl yalnız yoxlama üçün qalır.
-- ═══════════════════════════════════════════════════════════════════

-- 1) Migration tarixçəyə düşübmü? «20260819000000_Avtopark» olmalıdır.
SELECT TOP 6 MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId DESC;

-- 2) 5 cədvəlin hamısı varmı?
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Masinlar','MasinMuracietler','MasinMuddetNovleri',
                     'MasinMuddetler','AvtoparkXeberdarliqAlicilari')
ORDER BY TABLE_NAME;   -- 5 sətir gözlənilir

-- 3) Standart müddət növləri yazılıbmı?
SELECT Id, Ad, XeberdarliqGun, Sira FROM MasinMuddetNovleri ORDER BY Sira;
-- 5 sətir: İcbari sığorta, Kasko, Texniki baxış, Yağ dəyişmə, Yanğınsöndürən
