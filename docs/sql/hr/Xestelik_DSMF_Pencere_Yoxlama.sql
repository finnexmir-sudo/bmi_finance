-- ============================================================
-- Xəstəlik ödənişi — ekranda göstərilən DSMF cəmini DB-dən yoxla
-- ============================================================
-- Nə üçün: Ekranda "DSMF CƏMİ (SON 12 AY ...)" bloku iki rəqəm göstərir —
-- İşçidən tutulan və Şirkət ödəyən. Bu skript eyni rəqəmi birbaşa
-- IsciAyliqQazanclar cədvəlindən (kodun özünün oxuduğu mənbədən) çıxarır
-- ki, ekranla müqayisə edəsiniz.
--
-- İstifadə: @IsciAdSoyad-ı dəyişdirin, @XestelikBaslama-nı bülletənin
-- başlama tarixi ilə eyni edin, F5.
-- ============================================================

DECLARE @IsciAdSoyad NVARCHAR(200) = N'Mahmudova İlkanə';
DECLARE @XestelikBaslama DATE = '2026-10-13';

-- 1) İşçini tap
SELECT Id AS IsciId, Ad, Soyad, IsheQebulTarixi
FROM Isciler
WHERE Silinib = 0
  AND (Ad + N' ' + Soyad LIKE '%' + @IsciAdSoyad + '%'
    OR Soyad + N' ' + Ad LIKE '%' + @IsciAdSoyad + '%');

-- Aşağıdakı sorğular üçün IsciId-ni əl ilə yazın (yuxarıdakı nəticədən):
DECLARE @IsciId INT = (
    SELECT TOP 1 Id FROM Isciler
    WHERE Silinib = 0
      AND (Ad + N' ' + Soyad LIKE '%' + @IsciAdSoyad + '%'
        OR Soyad + N' ' + Ad LIKE '%' + @IsciAdSoyad + '%')
);

-- 2) KÖHNƏ MODEL — SÜRÜŞƏN 12 ay (26.10.2026-ya qədər kodda idi, artıq İŞLƏDİLMİR)
-- (ayBitis = xəstəlik ayından 1 gün əvvəl, ayBaslangic = ayBitis-dən 11 ay geri)
DECLARE @AyBitis DATE = DATEADD(DAY, -1, DATEFROMPARTS(YEAR(@XestelikBaslama), MONTH(@XestelikBaslama), 1));
DECLARE @AyBaslangic DATE = DATEADD(MONTH, -11, DATEFROMPARTS(YEAR(@AyBitis), MONTH(@AyBitis), 1));

SELECT N'SÜRÜŞƏN (KÖHNƏ model — artıq kodda yoxdur)' AS Model,
       @AyBaslangic AS Bas, @AyBitis AS Bit;

-- 3) Həmin pəncərəyə düşən bütün aylıq qeydlər — ekrandakı cəmin tərkibi
SELECT Il, Ay, DsmfIsci, DsmfIsegoturen, ElIleDaxilEdilib
FROM IsciAyliqQazanclar
WHERE IsciId = @IsciId
  AND Silinib = 0
  AND (
        (Il > YEAR(@AyBaslangic)) OR (Il = YEAR(@AyBaslangic) AND Ay >= MONTH(@AyBaslangic))
      )
  AND (
        (Il < YEAR(@AyBitis)) OR (Il = YEAR(@AyBitis) AND Ay <= MONTH(@AyBitis))
      )
ORDER BY Il, Ay;

-- 4) Cəmi — bunu ekrandakı "İşçidən tutulan" / "Şirkət ödəyən" ilə tutuşdurun
SELECT
    SUM(DsmfIsci)       AS IsciIdenTutulan_Cemi,
    SUM(DsmfIsegoturen)  AS SirketOdeyen_Cemi,
    COUNT(*)             AS QeydSayi   -- 12 olmalıdır, azdırsa xəbərdarlıq ekranda da çıxır
FROM IsciAyliqQazanclar
WHERE IsciId = @IsciId
  AND Silinib = 0
  AND (
        (Il > YEAR(@AyBaslangic)) OR (Il = YEAR(@AyBaslangic) AND Ay >= MONTH(@AyBaslangic))
      )
  AND (
        (Il < YEAR(@AyBitis)) OR (Il = YEAR(@AyBitis) AND Ay <= MONTH(@AyBitis))
      );

-- 5) Həmin pəncərəyə düşən, DAHA ƏVVƏLKİ təsdiqlənmiş xəstəliklər
-- (bunlar məxrəcdən çıxılmalıdır — ekranda "Çıxılan xəstəlik təqvim günü")
SELECT Id, BaslamaTarixi, BitmeTarixi, Status,
       DATEDIFF(DAY,
            IIF(BaslamaTarixi < @AyBaslangic, @AyBaslangic, BaslamaTarixi),
            IIF(BitmeTarixi   > @AyBitis,     @AyBitis,     BitmeTarixi)
       ) + 1 AS PencereyeDusenGun
FROM Xestelikler
WHERE IsciId = @IsciId
  AND Silinib = 0
  AND Status = 1 -- Tesdiqlenib (XestelikStatus enum: yoxlayın, adətən 1)
  AND BaslamaTarixi <= @AyBitis
  AND BitmeTarixi   >= @AyBaslangic;

-- 6) YENİ MODEL — SABİT RÜB (26.10.2026-dan kodun hazırkı pəncərəsi,
-- bax XestelikService.Son4RubDovruTap). Qanun: "əmək qabiliyyətinin
-- itirildiyi aydan əvvəlki ardıcıl 4 rüb" (189 saylı Əsasnamə).
DECLARE @RubBasAyi INT = ((MONTH(@XestelikBaslama) - 1) / 3) * 3 + 1;
DECLARE @RubBaslangici DATE = DATEFROMPARTS(YEAR(@XestelikBaslama), @RubBasAyi, 1);
DECLARE @AyBitis_Rub DATE = DATEADD(DAY, -1, @RubBaslangici);
DECLARE @AyBaslangic_Rub DATE = DATEADD(MONTH, -11, DATEFROMPARTS(YEAR(@AyBitis_Rub), MONTH(@AyBitis_Rub), 1));

SELECT N'SABİT RÜB (YENİ — kodun hazırkı pəncərəsi, 26.10.2026-dan)' AS Model,
       @AyBaslangic_Rub AS Bas, @AyBitis_Rub AS Bit,
       IIF(@AyBaslangic_Rub = @AyBaslangic AND @AyBitis_Rub = @AyBitis,
           N'EYNİDİR — bu konkret ay üçün köhnə/yeni model fərqlənmir',
           N'FƏRQLİDİR — köhnə (sürüşən) model fərqli dövr götürərdi, indi SABİT RÜB tətbiq olunur') AS NeticeMuqayise;
