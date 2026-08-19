-- ═══════════════════════════════════════════════════════════════════
-- AVTOPARK — 5 cədvəlin əl ilə yaradılması
-- 19.08.2026 · v2 (hər blok ayrı batch)
--
-- NƏ VAXT İŞLƏDİLİR: startup migration-u tətbiq olunmayıbsa
-- («Invalid object name 'Masinlar'» / «'MasinMuracietler'» xətası).
--
-- ⚠️ v1-DƏN FƏRQ: əvvəlki nüsxə hamısını TƏK tranzaksiyaya salırdı və `GO`
-- ayırıcısı yox idi. Bir blok sınanda SSMS-də hansı blokda dayandığı aydın
-- görünmürdü. İndi hər cədvəl AYRI batch-dədir:
--   • uğurlu bloklar yerində qalır,
--   • sınan blokun xətası «Messages» tabında ÖZ adı ilə görünür,
--   • skript təkrar işlədilə bilər (hamısı `IF NOT EXISTS` altındadır).
--
-- NƏ DƏYİŞİR: yalnız CREATE TABLE / CREATE INDEX + boş cədvələ INSERT.
-- NƏYƏ TOXUNMUR: mövcud heç bir cədvəl, sətir, sütun.
--
-- İŞLƏTMƏ QAYDASI: faylı bütöv aç, F5 (Execute). Sonda «Messages» tabına
-- BAX — hər blok üçün bir sətir yazılır. Xəta varsa mətnini göndər.
-- ═══════════════════════════════════════════════════════════════════

-- ── 1) Masinlar ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Masinlar')
BEGIN
    CREATE TABLE [Masinlar] (
        [Id]                INT            NOT NULL IDENTITY(1,1),
        [DovletNomresi]     NVARCHAR(20)   NOT NULL,
        [Marka]             NVARCHAR(50)   NULL,
        [Model]             NVARCHAR(50)   NULL,
        [BuraxilisIli]      INT            NULL,
        [Reng]              NVARCHAR(30)   NULL,
        [Ban]               NVARCHAR(50)   NULL,
        [Vin]               NVARCHAR(50)   NULL,
        [Novu]              NVARCHAR(50)   NULL,
        [DepartamentId]     INT            NULL,
        [TehkimSurucuId]    INT            NULL,
        [Status]            INT            NOT NULL,
        [CariKm]            INT            NULL,
        [Qeyd]              NVARCHAR(500)  NULL,
        [YaradilmaTarixi]   DATETIME2      NOT NULL,
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL,
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_Masinlar] PRIMARY KEY ([Id]),
        -- Cədvəl adı «Departament»-dir (çoxluq DEYİL) — AppDbContext-də eyni
        -- entity üçün iki DbSet var, ona görə EF adı entity-dən götürür.
        CONSTRAINT [FK_Masinlar_Departament_DepartamentId]
            FOREIGN KEY ([DepartamentId]) REFERENCES [Departament]([Id]),
        CONSTRAINT [FK_Masinlar_Isciler_TehkimSurucuId]
            FOREIGN KEY ([TehkimSurucuId]) REFERENCES [Isciler]([Id])
    );
    CREATE INDEX [IX_Masinlar_DovletNomresi]  ON [Masinlar]([DovletNomresi]);
    CREATE INDEX [IX_Masinlar_DepartamentId]  ON [Masinlar]([DepartamentId]);
    CREATE INDEX [IX_Masinlar_TehkimSurucuId] ON [Masinlar]([TehkimSurucuId]);
    PRINT '1/6  Masinlar — YARADILDI';
END
ELSE PRINT '1/6  Masinlar — artiq var, toxunulmadi';
GO

-- ── 2) MasinMuracietler ────────────────────────────────────────────
-- DİQQƏT: İşçiyə DÖRD ayrı FK var (müraciətçi, rəhbər, çıxışı qeyd edən,
-- qayıdışı qeyd edən). Heç birində ON DELETE CASCADE YOXDUR — olsaydı
-- SQL Server «may cause cycles or multiple cascade paths» (xəta 1785) ilə
-- cədvəli yaratmazdı. Layihədə silinmə onsuz da yumşaqdır (`Silinib`).
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasinMuracietler')
BEGIN
    CREATE TABLE [MasinMuracietler] (
        [Id]                 INT            NOT NULL IDENTITY(1,1),
        [MasinId]            INT            NOT NULL,
        [IsciId]             INT            NOT NULL,
        [PlanBaslama]        DATETIME2      NOT NULL,
        [PlanBitme]          DATETIME2      NOT NULL,
        [Meqsed]             NVARCHAR(300)  NOT NULL,
        [Marsrut]            NVARCHAR(300)  NULL,
        [Status]             INT            NOT NULL,
        [RehberId]           INT            NULL,
        [RehberTesdiqTarixi] DATETIME2      NULL,
        [ImtinaSebebi]       NVARCHAR(500)  NULL,
        [CixisTarixi]        DATETIME2      NULL,
        [CixisQeydEdenId]    INT            NULL,
        [QayidisTarixi]      DATETIME2      NULL,
        [QayidisQeydEdenId]  INT            NULL,
        [CixisKm]            INT            NULL,
        [QayidisKm]          INT            NULL,
        [YaradilmaTarixi]    DATETIME2      NOT NULL,
        [YaradanIcraciId]    INT            NULL,
        [YenileyenIcraciId]  INT            NULL,
        [SilenIcraciId]      INT            NULL,
        [YenilenmeTarixi]    DATETIME2      NULL,
        [Silinib]            BIT            NOT NULL,
        [SilinmeTarixi]      DATETIME2      NULL,
        CONSTRAINT [PK_MasinMuracietler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MasinMuracietler_Masinlar_MasinId]
            FOREIGN KEY ([MasinId]) REFERENCES [Masinlar]([Id]),
        CONSTRAINT [FK_MasinMuracietler_Isciler_IsciId]
            FOREIGN KEY ([IsciId]) REFERENCES [Isciler]([Id]),
        CONSTRAINT [FK_MasinMuracietler_Isciler_RehberId]
            FOREIGN KEY ([RehberId]) REFERENCES [Isciler]([Id]),
        CONSTRAINT [FK_MasinMuracietler_Isciler_CixisQeydEdenId]
            FOREIGN KEY ([CixisQeydEdenId]) REFERENCES [Isciler]([Id]),
        CONSTRAINT [FK_MasinMuracietler_Isciler_QayidisQeydEdenId]
            FOREIGN KEY ([QayidisQeydEdenId]) REFERENCES [Isciler]([Id])
    );
    CREATE INDEX [IX_MasinMuracietler_MasinId_Status_PlanBaslama]
        ON [MasinMuracietler]([MasinId], [Status], [PlanBaslama]);
    CREATE INDEX [IX_MasinMuracietler_IsciId_Status]
        ON [MasinMuracietler]([IsciId], [Status]);
    CREATE INDEX [IX_MasinMuracietler_RehberId]          ON [MasinMuracietler]([RehberId]);
    CREATE INDEX [IX_MasinMuracietler_CixisQeydEdenId]   ON [MasinMuracietler]([CixisQeydEdenId]);
    CREATE INDEX [IX_MasinMuracietler_QayidisQeydEdenId] ON [MasinMuracietler]([QayidisQeydEdenId]);
    PRINT '2/6  MasinMuracietler — YARADILDI';
END
ELSE PRINT '2/6  MasinMuracietler — artiq var, toxunulmadi';
GO

-- ── 3) MasinMuddetNovleri ──────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasinMuddetNovleri')
BEGIN
    CREATE TABLE [MasinMuddetNovleri] (
        [Id]                INT           NOT NULL IDENTITY(1,1),
        [Ad]                NVARCHAR(80)  NOT NULL,
        [XeberdarliqGun]    INT           NOT NULL,
        [Aktivdir]          BIT           NOT NULL,
        [Sira]              INT           NOT NULL,
        [YaradilmaTarixi]   DATETIME2     NOT NULL,
        [YaradanIcraciId]   INT           NULL,
        [YenileyenIcraciId] INT           NULL,
        [SilenIcraciId]     INT           NULL,
        [YenilenmeTarixi]   DATETIME2     NULL,
        [Silinib]           BIT           NOT NULL,
        [SilinmeTarixi]     DATETIME2     NULL,
        CONSTRAINT [PK_MasinMuddetNovleri] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_MasinMuddetNovleri_Ad] ON [MasinMuddetNovleri]([Ad]);
    PRINT '3/6  MasinMuddetNovleri — YARADILDI';
END
ELSE PRINT '3/6  MasinMuddetNovleri — artiq var, toxunulmadi';
GO

-- ── 4) MasinMuddetler ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasinMuddetler')
BEGIN
    CREATE TABLE [MasinMuddetler] (
        [Id]                    INT            NOT NULL IDENTITY(1,1),
        [MasinId]               INT            NOT NULL,
        [NovId]                 INT            NOT NULL,
        [SonTarix]              DATETIME2      NOT NULL,
        [XeberdarliqGun]        INT            NOT NULL,
        [Mebleg]                DECIMAL(18,2)  NULL,
        [SenedFaylYolu]         NVARCHAR(300)  NULL,
        [SenedFaylAdi]          NVARCHAR(200)  NULL,
        [Qeyd]                  NVARCHAR(500)  NULL,
        [Aktivdir]              BIT            NOT NULL,
        [XeberdarliqGonderilib] BIT            NOT NULL,
        [XeberdarliqTarixi]     DATETIME2      NULL,
        [SonKm]                 INT            NULL,
        [YaradilmaTarixi]       DATETIME2      NOT NULL,
        [YaradanIcraciId]       INT            NULL,
        [YenileyenIcraciId]     INT            NULL,
        [SilenIcraciId]         INT            NULL,
        [YenilenmeTarixi]       DATETIME2      NULL,
        [Silinib]               BIT            NOT NULL,
        [SilinmeTarixi]         DATETIME2      NULL,
        CONSTRAINT [PK_MasinMuddetler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MasinMuddetler_Masinlar_MasinId]
            FOREIGN KEY ([MasinId]) REFERENCES [Masinlar]([Id]),
        CONSTRAINT [FK_MasinMuddetler_MasinMuddetNovleri_NovId]
            FOREIGN KEY ([NovId]) REFERENCES [MasinMuddetNovleri]([Id])
    );
    CREATE INDEX [IX_MasinMuddetler_Aktivdir_XeberdarliqGonderilib_SonTarix]
        ON [MasinMuddetler]([Aktivdir], [XeberdarliqGonderilib], [SonTarix]);
    CREATE INDEX [IX_MasinMuddetler_MasinId] ON [MasinMuddetler]([MasinId]);
    CREATE INDEX [IX_MasinMuddetler_NovId]   ON [MasinMuddetler]([NovId]);
    PRINT '4/6  MasinMuddetler — YARADILDI';
END
ELSE PRINT '4/6  MasinMuddetler — artiq var, toxunulmadi';
GO

-- ── 5) AvtoparkXeberdarliqAlicilari ────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AvtoparkXeberdarliqAlicilari')
BEGIN
    CREATE TABLE [AvtoparkXeberdarliqAlicilari] (
        [Id]                INT        NOT NULL IDENTITY(1,1),
        [IsciId]            INT        NOT NULL,
        [Aktivdir]          BIT        NOT NULL,
        [YaradilmaTarixi]   DATETIME2  NOT NULL,
        [YaradanIcraciId]   INT        NULL,
        [YenileyenIcraciId] INT        NULL,
        [SilenIcraciId]     INT        NULL,
        [YenilenmeTarixi]   DATETIME2  NULL,
        [Silinib]           BIT        NOT NULL,
        [SilinmeTarixi]     DATETIME2  NULL,
        CONSTRAINT [PK_AvtoparkXeberdarliqAlicilari] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AvtoparkXeberdarliqAlicilari_Isciler_IsciId]
            FOREIGN KEY ([IsciId]) REFERENCES [Isciler]([Id])
    );
    CREATE INDEX [IX_AvtoparkXeberdarliqAlicilari_IsciId]
        ON [AvtoparkXeberdarliqAlicilari]([IsciId]);
    PRINT '5/6  AvtoparkXeberdarliqAlicilari — YARADILDI';
END
ELSE PRINT '5/6  AvtoparkXeberdarliqAlicilari — artiq var, toxunulmadi';
GO

-- ── 6) Standart müddət növləri + migration qeydi ───────────────────
IF NOT EXISTS (SELECT 1 FROM [MasinMuddetNovleri])
BEGIN
    INSERT INTO [MasinMuddetNovleri]
        ([Ad], [XeberdarliqGun], [Aktivdir], [Sira], [YaradilmaTarixi], [Silinib])
    VALUES
        (N'İcbari sığorta', 30, 1, 1, '2026-08-19', 0),
        (N'Kasko',          30, 1, 2, '2026-08-19', 0),
        (N'Texniki baxış',  30, 1, 3, '2026-08-19', 0),
        (N'Yağ dəyişmə',    14, 1, 4, '2026-08-19', 0),
        (N'Yanğınsöndürən', 30, 1, 5, '2026-08-19', 0);
    PRINT '6/6  Standart novler yazildi (5 setir)';
END
ELSE PRINT '6/6  Novler onsuz da var, toxunulmadi';

-- EF-ə «bu migration artıq tətbiq olunub» de. Bu sətir olmasa növbəti
-- startup-da `Migrate()` cədvəlləri TƏKRAR yaratmağa çalışar və
-- «There is already an object named 'Masinlar'» xətası verər.
--
-- ⚠️ YALNIZ BÜTÜN 5 CƏDVƏL YARANDIQDA yazılır — natamam vəziyyətdə
-- yazsaq, migration «tətbiq olunub» sayılar və qalan cədvəllər HEÇ VAXT
-- yaranmaz.
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260819000000_Avtopark')
   AND (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_NAME IN ('Masinlar','MasinMuracietler','MasinMuddetNovleri',
                             'MasinMuddetler','AvtoparkXeberdarliqAlicilari')) = 5
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260819000000_Avtopark', '8.0.23');
    PRINT 'Migration tarixcesine yazildi.';
END
GO

-- ── YEKUN YOXLAMA ──────────────────────────────────────────────────
SELECT TABLE_NAME AS YaradilmisCedvel
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Masinlar','MasinMuracietler','MasinMuddetNovleri',
                     'MasinMuddetler','AvtoparkXeberdarliqAlicilari')
ORDER BY TABLE_NAME;   -- 5 sətir gözlənilir

SELECT COUNT(*) AS NovSayi FROM [MasinMuddetNovleri];   -- 5 gözlənilir
