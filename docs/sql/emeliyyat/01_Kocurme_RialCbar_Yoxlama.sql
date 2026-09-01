/* ============================================================================
   PUL KÖÇÜRMƏSİ — «Rial (MB kursu)» yoxlaması · 01.09.2026
   ----------------------------------------------------------------------------
   BU SKRİPT HEÇ NƏ DƏYİŞMİR — YALNIZ SELECT.
   Sondakı UPDATE şablonu ŞƏRH İÇİNDƏDİR və qərar sizindir (aşağıya bax).

   NİYƏ YARANDI: `Kocurme.RialCbar` sütunu `decimal(18,4)` idi, yəni cəmi 4 onluq
   yer. İran rialının real MB kursu isə `0,000002950`-dir — 4 onluqda o, `0,0000`
   olur. Forma da `step="0.0001"` ilə bağlı idi və brauzer belə rəqəmi ümumiyyətlə
   qəbul etmirdi («The two nearest valid values are 0 and 0.0001»).

   NƏTİCƏ: kursu OLDUĞU KİMİ yazmaq mümkün deyildi. 10 000 dəfə böyüdülmüş
   `0,0295` yazılıbsa, mühasibat yazılışındakı **dilinq fərqi sətri də düz
   10 000 dəfə böyük** çıxıb. Düstur (`PulKocurmeVoucher.DilingAdd`):

       dilinq fərqi = Mebleg × IranRial × RialCbar − Mebleg × ValyutaCbar
                      └─ yalnız BU hissə RialCbar-dan asılıdır ─┘

   Nümunə (26-T-29 — Mebleg 200, IranRial 850 000, ValyutaCbar 1):
       RialCbar 0,0295      → 200×850000×0,0295      − 200 = 5 014 800,00
       RialCbar 0,00000295  → 200×850000×0,00000295  − 200 =       301,50

   ⚠️ Kursun DÜZGÜN dəyəri nədir — bunu YALNIZ mühasib deyə bilər. Bu skript
   yalnız «hansı sətirlərə baxmaq lazımdır» sualına cavab verir.
   ============================================================================ */
SET NOCOUNT ON;

/* ── 1) Bütün Rial/Rubl köçürmələri və hər birinin dilinq fərqi ───────────── */
SELECT
    k.Id,
    k.HevaleNo,
    k.Tarix,
    k.MedaxilValyuta,
    k.KocurulenValyuta,
    k.Mebleg,
    k.IranRial,
    k.RialCbar,
    k.ValyutaCbar,
    /* Serverdəki düsturun eynisi — `PulKocurmeVoucher.DilingAdd`.
       ⚠️ FLOAT-a cast MƏCBURİDİR: decimal(18,2)×decimal(18,2)×decimal(18,10)
       nəticəsi 38 rəqəm həddini aşır və SQL Server «Arithmetic overflow error
       converting expression to data type numeric» ilə sorğunu bütöv sındırır.
       Bu diaqnostikdir — float-un dəqiqliyi burada kifayətdir. */
    CemiDilinqFerqi = ROUND(
        CAST(k.Mebleg AS float) * CAST(k.IranRial AS float) * CAST(k.RialCbar AS float)
      - CAST(k.Mebleg AS float) * CAST(k.ValyutaCbar AS float), 2),
    /* Kurs 10 000 dəfə kiçildilsəydi nə olardı — MÜQAYİSƏ ÜÇÜN, tətbiq deyil */
    FerqEger10000Bolunse = ROUND(
        CAST(k.Mebleg AS float) * CAST(k.IranRial AS float) * (CAST(k.RialCbar AS float) / 10000)
      - CAST(k.Mebleg AS float) * CAST(k.ValyutaCbar AS float), 2),
    /* «1 vahid = N rial» kursu ilə MB kursunun uzlaşması.
       Bu nisbət 1-ə nə qədər yaxındırsa, dilinq fərqi bir o qədər kiçikdir. */
    Uzlasma = CASE WHEN k.ValyutaCbar > 0
                   THEN ROUND(CAST(k.IranRial AS float) * CAST(k.RialCbar AS float)
                              / CAST(k.ValyutaCbar AS float), 4) END
FROM Kocurme k
WHERE ISNULL(k.Silinib, 0) = 0
  AND k.KocurulenValyuta IN (N'Rial', N'Rubl')
ORDER BY k.Tarix DESC, k.Id DESC;

/* ── 2) Şübhəli sətirlər — dilinq fərqi köçürülən məbləğdən BÖYÜKDÜR ──────── */
/*    Sağlam köçürmədə fərq məbləğin kiçik bir hissəsi olur. Fərq məbləğin
      özündən böyükdürsə, çox güman kurs miqyası səhvdir.                     */
SELECT
    k.Id, k.HevaleNo, k.Tarix, k.Mebleg, k.IranRial, k.RialCbar,
    CemiDilinqFerqi = ROUND(
        CAST(k.Mebleg AS float) * CAST(k.IranRial AS float) * CAST(k.RialCbar AS float)
      - CAST(k.Mebleg AS float) * CAST(k.ValyutaCbar AS float), 2)
FROM Kocurme k
WHERE ISNULL(k.Silinib, 0) = 0
  AND k.KocurulenValyuta IN (N'Rial', N'Rubl')
  AND k.Mebleg > 0
  AND ABS(CAST(k.Mebleg AS float) * CAST(k.IranRial AS float) * CAST(k.RialCbar AS float)
        - CAST(k.Mebleg AS float) * CAST(k.ValyutaCbar AS float)) > CAST(k.Mebleg AS float)
ORDER BY k.Tarix DESC;

/* ── 3) Sütunun yeni dəqiqliyi tətbiq olunubmu? ───────────────────────────── */
/*    Migration `20260901120000_KocurmeKursDeqiqliyi` işləyibsə: scale = 10.   */
SELECT c.name AS Sutun, t.name AS Tip, c.precision, c.scale
FROM sys.columns c
JOIN sys.types   t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID('Kocurme')
  AND c.name IN ('RialCbar', 'ValyutaCbar', 'IranRial', 'Mebleg');

SELECT TOP 5 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;

/* ============================================================================
   KEÇMİŞ SƏTİRLƏRİ DÜZƏLTMƏK — YALNIZ MÜHASİB TƏSDİQİNDƏN SONRA

   AŞAĞIDAKI ƏMR ŞƏRH İÇİNDƏDİR. İşlətməzdən əvvəl:

     1. Yuxarıdakı 1-ci sorğunu işlədin və `Uzlasma` sütununa baxın.
     2. Mühasibdən HƏR bir sətir üçün düzgün MB kursunu təsdiqlədin.
        «Hamısı 10 000-ə bölünməlidir» AVTOMATİK doğru DEYİL — bəzi sətirlər
        başqa valyuta ilə, başqa dövrdə yazılmış ola bilər.
     3. Migration tətbiq olunmuş olmalıdır (3-cü sorğuda scale = 10), yoxsa
        yeni dəyər yenidən `0,0000`-a yuvarlaqlaşar və SƏSSİZCƏ İTƏR.
     4. Düzəlişdən sonra həmin köçürmənin Detal səhifəsini açıb dilinq fərqi
        sətrini mühasib Exceli ilə tutuşdurun.

   -- BEGIN TRAN;
   --
   -- -- ƏVVƏLCƏ: nə dəyişəcək?
   -- SELECT Id, HevaleNo, RialCbar AS Kohne, RialCbar / 10000 AS Yeni
   --   FROM Kocurme WHERE Id IN (/* ← təsdiqlənmiş Id-lər */);
   --
   -- UPDATE Kocurme
   --    SET RialCbar = RialCbar / 10000
   --  WHERE Id IN (/* ← EYNİ Id-lər */);
   --
   -- -- SONRA: yoxla
   -- SELECT Id, HevaleNo, RialCbar FROM Kocurme WHERE Id IN (/* ← EYNİ Id-lər */);
   --
   -- -- Nəticə düzgündürsə COMMIT, əks halda ROLLBACK:
   -- -- COMMIT TRAN;
   -- -- ROLLBACK TRAN;

   ⚠️ `Kocurme` sətrinin cütü `GedenHevale` jurnalındadır (`KocurmeId` ilə bağlı).
   Jurnal sətri `RialCbar`-ı SAXLAMIR — orada `MEBLEG = Mebleg × IranRial`-dır.
   Yəni bu düzəliş jurnala TOXUNMUR, yalnız mühasibat yazılışını dəyişir.
   ============================================================================ */
