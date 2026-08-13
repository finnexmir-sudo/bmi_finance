/* ============================================================================
   BİLDİRİŞ QALIQLARININ TƏMİZLƏNMƏSİ (məzuniyyət)
   ----------------------------------------------------------------------------
   13.08.2026 hadisəsi: rəhbər eyni məzuniyyət müraciətini "iki dəfə" gördü.
   İki səbəb vardı, hər ikisi kodda bağlandı:

     1) BildirisRouter bildirişləri PARALEL yazırdı (Task.WhenAll) — ortaq
        DbContext thread-safe olmadığı üçün sətir təkrarlana bilirdi;
     2) Müraciət ləğv ediləndə (yumşaq silmə) ona bağlı "təsdiq et" bildirişləri
        yerində qalırdı.

   Bu skript KEÇMİŞDƏ yaranmış iki növ qalığı təmizləyir:
     A. silinmiş məzuniyyətə bağlı, hələ OXUNMAMIŞ "təsdiq et" bildirişləri;
     B. eyni alıcıya, eyni müraciət üçün yazılmış DUBLİKAT bildirişlər
        (ən köhnəsi saxlanılır, təkrarları silinir).

   ── TOXUNULMAYANLAR (QƏSDƏN) ─────────────────────────────────────────────
   • Nov <> 1 (MezuniyyetMuraciet) olan bildirişlər — təsdiq/imtina/ödəniş
     xəbərləridir. "HR ləğv etdi" və "ödənişi icra etməyin" bildirişləri məhz
     məzuniyyət silinəndən SONRA yaradılır; onları silmək məlumat itkisi olardı.
   • OXUNMUŞ bildirişlər — baş vermiş hadisənin tarixçəsidir.

   ADDIM 1-i işlət, nəticəyə bax; razısansa ADDIM 2-ni işlət.
   Nov dəyəri: 1 = MezuniyyetMuraciet (BildirisNovu enum-u).
   ============================================================================ */
SET NOCOUNT ON;

/* ══ ADDIM 1 — NƏ SİLİNƏCƏK (yalnız göstərir, HEÇ NƏ dəyişmir) ══════════ */

PRINT N'--- A: silinmiş məzuniyyətə bağlı, oxunmamış "təsdiq et" bildirişləri ---';
SELECT  b.Id AS BildirisId, b.IsciId AS Alici,
        i.Ad + ' ' + i.Soyad AS AliciAd,
        b.MezuniyyetId, b.Bashliq,
        CONVERT(varchar(30), b.YaradilmaTarixi, 121) AS Yaradilib,
        m.Silinib AS MezuniyyetSilinib
FROM    Bildirisler b
JOIN    Mezuniyyetler m ON m.Id = b.MezuniyyetId
LEFT JOIN Isciler i     ON i.Id = b.IsciId
WHERE   b.Nov = 1
  AND   ISNULL(b.Silinib, 0) = 0
  AND   ISNULL(b.Oxunub,  0) = 0
  AND   m.Silinib = 1
ORDER BY b.Id;

PRINT N'--- B: dublikat bildirişlər (ən köhnəsi saxlanılır) ---';
WITH d AS (
    SELECT  b.*,
            ROW_NUMBER() OVER (
                PARTITION BY b.IsciId, b.Nov, b.Bashliq, b.Metn,
                             b.MezuniyyetId, b.IcazeId, b.MesajId
                ORDER BY b.Id) AS Sira
    FROM    Bildirisler b
    WHERE   b.MezuniyyetId IS NOT NULL
      AND   ISNULL(b.Silinib, 0) = 0
)
SELECT  d.Id AS BildirisId, d.IsciId AS Alici,
        i.Ad + ' ' + i.Soyad AS AliciAd,
        d.MezuniyyetId, d.Nov, d.Bashliq, d.Oxunub,
        CONVERT(varchar(30), d.YaradilmaTarixi, 121) AS Yaradilib,
        d.Sira
FROM    d
LEFT JOIN Isciler i ON i.Id = d.IsciId
WHERE   d.Sira > 1
ORDER BY d.MezuniyyetId DESC, d.IsciId, d.Id;


/* ══ ADDIM 2 — SİLMƏ (yumşaq silmə; ADDIM 1-i yoxlayandan SONRA işlət) ══
   Aşağıdakı bloku seçib işlət. Tranzaksiyadadır — say gözlədiyindən çox
   çıxsa ROLLBACK et.

BEGIN TRAN;

    -- A: silinmiş məzuniyyətin oxunmamış "təsdiq et" bildirişləri
    UPDATE  b
    SET     b.Silinib = 1,
            b.SilinmeTarixi = SYSDATETIME()
    FROM    Bildirisler b
    JOIN    Mezuniyyetler m ON m.Id = b.MezuniyyetId
    WHERE   b.Nov = 1
      AND   ISNULL(b.Silinib, 0) = 0
      AND   ISNULL(b.Oxunub,  0) = 0
      AND   m.Silinib = 1;

    PRINT N'A bloku — silinən sətir sayı:';
    PRINT @@ROWCOUNT;

    -- B: dublikatlar (ən köhnəsi qalır)
    WITH d AS (
        SELECT  b.Id,
                ROW_NUMBER() OVER (
                    PARTITION BY b.IsciId, b.Nov, b.Bashliq, b.Metn,
                                 b.MezuniyyetId, b.IcazeId, b.MesajId
                    ORDER BY b.Id) AS Sira
        FROM    Bildirisler b
        WHERE   b.MezuniyyetId IS NOT NULL
          AND   ISNULL(b.Silinib, 0) = 0
    )
    UPDATE  b
    SET     b.Silinib = 1,
            b.SilinmeTarixi = SYSDATETIME()
    FROM    Bildirisler b
    JOIN    d ON d.Id = b.Id
    WHERE   d.Sira > 1;

    PRINT N'B bloku — silinən sətir sayı:';
    PRINT @@ROWCOUNT;

-- Saylar gözlədiyin kimidirsə:
COMMIT TRAN;
-- Deyilsə:
-- ROLLBACK TRAN;
*/
