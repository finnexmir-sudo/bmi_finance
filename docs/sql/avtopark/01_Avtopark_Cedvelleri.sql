-- ═══════════════════════════════════════════════════════════════════
-- AVTOPARK — 5 cədvəlin əl ilə yaradılması
-- 19.08.2026
--
-- NƏ VAXT İŞLƏDİLİR: startup migration-u tətbiq olunmayıbsa
-- («Invalid object name 'Masinlar'» xətası).
--
-- NƏ DƏYİŞİR:
--   • 5 YENİ cədvəl yaranır (hamısı `IF NOT EXISTS` ilə qorunur)
--   • `MasinMuddetNovleri`-yə 5 sətir yazılır (cədvəl boş olduqda)
--   • `__EFMigrationsHistory`-yə 1 sətir yazılır ki, EF bu migration-u
--     TƏKRAR tətbiq etməyə çalışmasın
--
-- NƏYƏ TOXUNMUR: mövcud heç bir cədvəl, sətir və ya sütun.
--   Nə UPDATE, nə DELETE, nə ALTER var — yalnız CREATE + INSERT.
--
-- TƏKRAR İŞLƏDİLƏ BİLƏR — hamısı `IF NOT EXISTS` qoruması altındadır.
-- ═══════════════════════════════════════════════════════════════════

SET XACT_ABORT ON;
BEGIN TRANSACTION;

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
END

-- ── 2) MasinMuracietler ────────────────────────────────────────────
-- DİQQƏT: İşçiyə DÖRD ayrı FK var (müraciətçi, rəhbər, çıxışı qeyd edən,
-- qayıdışı qeyd edən). ON DELETE CASCADE qoyulsa SQL Server «multiple
-- cascade paths» ilə cədvəli yaratmır. Layihədə silinmə onsuz da
-- yumşaqdır (`Silinib`), ona görə hamısı NO ACTION-dır.
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
    CREATE INDEX [IX_MasinMuracietler_RehberId]           ON [MasinMuracietler]([RehberId]);
    CREATE INDEX [IX_MasinMuracietler_CixisQeydEdenId]    ON [MasinMuracietler]([CixisQeydEdenId]);
    CREATE INDEX [IX_MasinMuracietler_QayidisQeydEdenId]  ON [MasinMuracietler]([QayidisQeydEdenId]);
END

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
END

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
END

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
END

-- ── 6) Standart müddət növləri (yalnız cədvəl BOŞ olduqda) ─────────
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
END

-- ── 7) EF-ə «bu migration artıq tətbiq olunub» de ──────────────────
-- Bu sətir olmasa növbəti startup-da `Migrate()` cədvəlləri TƏKRAR
-- yaratmağa çalışar və «There is already an object named 'Masinlar'»
-- xətası verər.
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
               WHERE [MigrationId] = '20260819000000_Avtopark')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260819000000_Avtopark', '8.0.23');
END

COMMIT TRANSACTION;

-- ── YOXLAMA ────────────────────────────────────────────────────────
SELECT COUNT(*) AS NovSayi FROM [MasinMuddetNovleri];   -- 5 gözlənilir
SELECT COUNT(*) AS MasinSayi FROM [Masinlar];            -- 0 gözlənilir
