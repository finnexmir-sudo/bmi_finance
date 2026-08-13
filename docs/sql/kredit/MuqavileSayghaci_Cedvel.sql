/* ============================================================================
   MuqavileSayghaci — müqavilə nömrə sayğacı cədvəli
   ----------------------------------------------------------------------------
   NORMAL YOL: Package Manager Console-da migration yaratmaq —
       Add-Migration MuqavileSayghaci
       Update-Database
   Entity və DbContext konfiqurasiyası hazırdır, migration cədvəli özü yaradır.

   BU SKRİPT yalnız migration işlətmək mümkün olmayanda (məs. birbaşa serverdə)
   lazımdır. İdempotentdir — cədvəl varsa toxunmur.

   ── STRUKTUR ─────────────────────────────────────────────────────────────
   Bir sətir = bir (Novu, Il) sayğacı. BMI-dəki geniş sütun düzülüşü (bir il =
   bir sətir, hər sayğac ayrı sütun) QƏSDƏN təkrarlanmır: yeni sayğac növü
   burada sadəcə yeni sətirdir, migration tələb etmir.

   SonNomre = SON VERİLMİŞ nömrə. Növbəti = SonNomre + 1.
   Novu dəyərləri: 1 KrZaminlik, 2 KrMenzil, 3 KrZaminler, 4 KrSerencam,
                   5 KrAvtomobil, 6 Depozit, 7 KrKart, 8 KartZamin, 9 KrQizil
   ============================================================================ */
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MuqavileSayghaci', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MuqavileSayghaci
    (
        Id                 INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_MuqavileSayghaci PRIMARY KEY,
        Novu               INT           NOT NULL,
        Il                 INT           NOT NULL,
        SonNomre           INT           NOT NULL,

        /* BaseEntity sahələri */
        YaradilmaTarixi    DATETIME2(7)  NOT NULL,
        YaradanIcraciId    INT           NULL,
        YenileyenIcraciId  INT           NULL,
        SilenIcraciId      INT           NULL,
        YenilenmeTarixi    DATETIME2(7)  NULL,
        Silinib            BIT           NOT NULL CONSTRAINT DF_MuqavileSayghaci_Silinib DEFAULT (0),
        SilinmeTarixi      DATETIME2(7)  NULL
    );

    /* Bir il/növ üçün YALNIZ BİR sayğac — iki sətir olsa iki müqavilə eyni nömrəni alardı */
    CREATE UNIQUE INDEX IX_MuqavileSayghaci_Novu_Il
        ON dbo.MuqavileSayghaci (Novu, Il);

    PRINT N'MuqavileSayghaci yaradıldı.';
END
ELSE
    PRINT N'MuqavileSayghaci artıq mövcuddur — toxunulmadı.';

/* ── Yoxlama ─────────────────────────────────────────────────────────────── */
SELECT Novu, Il, SonNomre, SonNomre + 1 AS Novbeti, YaradilmaTarixi
FROM dbo.MuqavileSayghaci
WHERE ISNULL(Silinib, 0) = 0
ORDER BY Il DESC, Novu;
