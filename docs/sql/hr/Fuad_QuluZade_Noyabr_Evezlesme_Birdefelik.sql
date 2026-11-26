-- ============================================================================
-- Fuad Qulu Zadə — Noyabr ayı əvəzləşmə avansı (BİR DƏFƏLİK, ƏL İLƏ DÜZƏLİŞ)
-- 26.11.2026
--
-- NİYƏ ƏL İLƏ: 26.11.2026-da qurulan avtomatik əvəzləşmə mexanizmi
-- (MezuniyyetAvansService.EvezlesmeAvanslariniHazirlaAsync) YALNIZ
-- Mezuniyyet.OdenisTipi = QabaqcadanOdenis (enum=2) olan qeydlərə işləyir.
--
-- Fuad-ın problemli məzuniyyəti (Id=1150, 16.10.2026–03.11.2026) bu şərti
-- ÖDƏMİR — SQL ilə yoxlanıldı, OdenisTipi = 1 (AySonuOdenis) çıxdı:
--
--   SELECT Id, OdenisTipi, OdenisStatus, Silinib, HrTesdiqTarixi
--   FROM Mezuniyyetler WHERE Id = 1150;
--   -- OdenisTipi=1, OdenisStatus=0, Silinib=0, HrTesdiqTarixi=28.09.2026
--
-- Yəni "Əvəzləşmə avanslarını indi yarat" düyməsi bu qeydə HEÇ VAXT
-- çatmır — nə Məzuniyyət Ödənişləri səhifəsi onu göstərir (o səhifə də
-- eyni OdenisTipi=QabaqcadanOdenis şərtini tələb edir), nə servis onu
-- qəbul edir (öz şərti: "Bu müraciət qabaqcadan ödənişə təyin olunmayıb").
--
-- ⚠️ AÇIQ QALAN SUAL (bu skriptdə HƏLL EDİLMİR): mühasibin təsvir etdiyi
-- real hərəkət ("10-cu ayda verib, 11-ci aya avans yazıb") AySonuOdenis
-- tipli, bir neçə aya düşən məzuniyyətdə DƏ baş verə bilər — demək
-- avtomatik mexanizmin OdenisTipi=QabaqcadanOdenis şərti, görünür, real
-- ehtiyacdan DAHA DAR tutulub. Bu, ayrıca qərar tələb edir (gating şərtini
-- genişləndirmək, yoxsa Fuad-ın OdenisTipi-ni dəyişmək) — bura TOXUNULMADI,
-- çünki maaş/vergi məntiqinə toxunan hər dəyişiklik əlavə təsdiq tələb edir.
--
-- BU SKRİPT YALNIZ bir dəfəlik, Fuad-ın noyabr fərqini (242,20 ₼,
-- mühasibin Excel-i ilə təsdiqlənib) MÖVCUD "Avans" axınına (adi avans
-- siyahısı → maaş hesablamasında tutulma) yerləşdirir. Sebeb mətni və
-- sütun dəyərləri MezuniyyetAvansService-in avtomatik yaratdığı sətirlə
-- BİRƏ-BİR eynidir ki, ekranda/ixracda fərqli görünməsin.
--
-- TƏKRAR İŞLƏDİLƏ BİLƏR — "IF NOT EXISTS" ilə qorunur.
-- ============================================================================

SET NOCOUNT ON;

-- 1) ƏVVƏLCƏ YOXLA — bu qeyd artıq varmı? (Status=1 Gözləmədə, 2 Təsdiqlənib,
--    3 Rədd, 4 Ödənilib)
SELECT a.Id, a.IsciId, a.Il, a.Ay, a.Mebleg, a.Status, a.MezuniyyetId, a.Sebeb
FROM Avanslar a
WHERE a.MezuniyyetId = 1150 AND a.Il = 2026 AND a.Ay = 11 AND a.Silinib = 0;

-- 2) QEYD YOXDURSA — yarat (MezuniyyetAvansService-in formatı ilə EYNİ)
IF NOT EXISTS (
    SELECT 1 FROM Avanslar
    WHERE MezuniyyetId = 1150 AND Il = 2026 AND Ay = 11 AND Silinib = 0
)
BEGIN
    INSERT INTO Avanslar
        (IsciId, Il, Ay, Mebleg, Sebeb, Status, MuhasibId, TesdiqTarixi,
         MezuniyyetId, YaradilmaTarixi, Silinib)
    SELECT
        m.IsciId,
        2026, 11,
        242.20,
        N'Qabaqcadan ödənilmiş məzuniyyətin (16.10.2026–03.11.2026) 11.2026 ayına ' +
        N'düşən əvəzləşməsi (əl ilə, bir dəfəlik — bax Fuad_QuluZade_Noyabr_' +
        N'Evezlesme_Birdefelik.sql).',
        2,          -- AvansStatus.Tesdiqlenib
        NULL,       -- MuhasibId — konkret mühasib bilinmir, boş qalır
        GETDATE(),
        1150,
        GETDATE(),
        0
    FROM Mezuniyyetler m
    WHERE m.Id = 1150;
END;

-- 3) NƏTİCƏ
SELECT a.Id, a.IsciId, i.Ad, i.Soyad, a.Il, a.Ay, a.Mebleg, a.Status,
       a.MezuniyyetId, a.TesdiqTarixi
FROM Avanslar a
JOIN Isciler i ON i.Id = a.IsciId
WHERE a.MezuniyyetId = 1150;
