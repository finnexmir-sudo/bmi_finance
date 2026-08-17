-- ══════════════════════════════════════════════════════════════════════
-- Erkən çıxış icazəsi — əl ilə qeyd
--   İşçi:        IsciId = 10
--   Tarix:       17.08.2026
--   İcazə verən: IsciId = 3 (rəhbər)
--   Qeyd vaxtı:  16:02 (işçi 16:03-də çıxıb — icazə çıxışdan əvvəl verilib)
--
-- SƏBƏB: rəhbər icazəni şifahi verib, sistemdəki düymə isə həmin gün
-- işləmirdi (JS-in oxuduğu `isciId` sahəsi serverdən gəlmirdi → POST
-- isciId=0 göndərirdi). Kod düzəldilib; bu skript yalnız KEÇMİŞ günü
-- bərpa etmək üçündür.
--
-- QEYD: `YaradilmaTarixi` hesablamaya GİRMİR — kod icazəni yalnız
-- (IsciId, Tarix.Date) cütü ilə axtarır (DavamiyyetController:147).
-- 16:02 audit üçündür, nəticəyə təsiri yoxdur.
--
-- `YaradanIcraciId` = AppUser id-dir (İsci id DEYİL). Bu sətri proqram
-- deyil, əl ilə SSMS yazdığı üçün NULL qalır — uydurma istifadəçi
-- yazmaq audit izini yalan göstərərdi.
-- ══════════════════════════════════════════════════════════════════════

USE FinNex_Maliyye_Db;
GO

-- ── 1) ƏVVƏLCƏ YOXLA: kimdir və artıq qeyd varmı ──────────────────────
SELECT Id, Ad, Soyad, Status
FROM   Isciler
WHERE  Id = 10;

SELECT *
FROM   ErkenCixisIcazeler
WHERE  IsciId = 10 AND CAST(Tarix AS date) = '2026-08-17';
GO

-- ── 2) YAZ (idempotent — təkrar işlətsəniz ikinci sətir yaranmır) ─────
IF NOT EXISTS (SELECT 1 FROM ErkenCixisIcazeler
               WHERE IsciId = 10
                 AND CAST(Tarix AS date) = '2026-08-17'
                 AND Silinib = 0)
BEGIN
    INSERT INTO ErkenCixisIcazeler
        (IsciId, Tarix, IcazeVerenIsciId, YaradilmaTarixi, YaradanIcraciId, Silinib)
    VALUES
        (10, '2026-08-17', 3, '2026-08-17T16:02:00', NULL, 0);

    PRINT N'✔ Erkən çıxış icazəsi yazıldı (IsciId=10, 17.08.2026).';
END
ELSE
    PRINT N'• Bu gün üçün icazə artıq var — heç nə yazılmadı.';
GO

-- ── 3) NƏTİCƏ ─────────────────────────────────────────────────────────
SELECT Id, IsciId, Tarix, IcazeVerenIsciId, YaradilmaTarixi, Silinib
FROM   ErkenCixisIcazeler
WHERE  IsciId = 10 AND CAST(Tarix AS date) = '2026-08-17';
GO
