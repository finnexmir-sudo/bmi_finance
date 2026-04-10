-- ============================================================
-- BMI Finance - VM Database Setup Script
-- Creates all potentially missing tables from YeniModullar onwards
-- Safe to run multiple times (IF NOT EXISTS wrappers)
-- Generated from AppDbContextModelSnapshot.cs + Entity classes
-- ============================================================

SET NOCOUNT ON;
GO

PRINT '=== BMI Finance VM Setup Script ==='
PRINT 'Start time: ' + CONVERT(VARCHAR, GETDATE(), 120)
GO

-- ============================================================
-- 1. ChatMesajlar
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMesajlar')
BEGIN
    CREATE TABLE [ChatMesajlar] (
        [Id]                  INT            IDENTITY(1,1) NOT NULL,
        [GonderenIsciId]      INT            NOT NULL,
        [AlanIsciId]          INT            NOT NULL,
        [Metn]                NVARCHAR(MAX)  NOT NULL,
        [Oxunub]              BIT            NOT NULL DEFAULT(0),
        [GonderilmeTarixi]    DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [OxunmaTarixi]        DATETIME2      NULL,
        [TopluMesajGrupId]    UNIQUEIDENTIFIER NULL,
        [FaylAdi]             NVARCHAR(MAX)  NULL,
        [FaylYolu]            NVARCHAR(MAX)  NULL,
        [FaylOlcusu]          BIGINT         NULL,
        [FaylTipi]            NVARCHAR(MAX)  NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]     DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]     INT            NULL,
        [YenileyenIcraciId]   INT            NULL,
        [SilenIcraciId]       INT            NULL,
        [YenilenmeTarixi]     DATETIME2      NULL,
        [Silinib]             BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]       DATETIME2      NULL,
        CONSTRAINT [PK_ChatMesajlar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMesajlar_Isciler_GonderenIsciId] FOREIGN KEY ([GonderenIsciId]) REFERENCES [Isciler]([Id]),
        CONSTRAINT [FK_ChatMesajlar_Isciler_AlanIsciId] FOREIGN KEY ([AlanIsciId]) REFERENCES [Isciler]([Id])
    );
    CREATE INDEX [IX_ChatMesajlar_GonderenIsciId] ON [ChatMesajlar] ([GonderenIsciId]);
    CREATE INDEX [IX_ChatMesajlar_AlanIsciId] ON [ChatMesajlar] ([AlanIsciId]);
    PRINT 'Created table: ChatMesajlar'
END
ELSE
    PRINT 'Table already exists: ChatMesajlar'
GO

-- ============================================================
-- 2. Elanlar
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Elanlar')
BEGIN
    CREATE TABLE [Elanlar] (
        [Id]                  INT            IDENTITY(1,1) NOT NULL,
        [Bashliq]             NVARCHAR(MAX)  NOT NULL,
        [Metn]                NVARCHAR(MAX)  NOT NULL,
        [GonderenIsciId]      INT            NOT NULL,
        [Vacibdir]            BIT            NOT NULL DEFAULT(0),
        [BitirmeTarixi]       DATETIME2      NULL,
        [Aktivdir]            BIT            NOT NULL DEFAULT(1),
        [SekilYolu]           NVARCHAR(MAX)  NULL,
        [FaylAdi]             NVARCHAR(MAX)  NULL,
        [FaylYolu]            NVARCHAR(MAX)  NULL,
        [FaylTipi]            NVARCHAR(MAX)  NULL,
        [FaylOlcusu]          BIGINT         NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]     DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]     INT            NULL,
        [YenileyenIcraciId]   INT            NULL,
        [SilenIcraciId]       INT            NULL,
        [YenilenmeTarixi]     DATETIME2      NULL,
        [Silinib]             BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]       DATETIME2      NULL,
        CONSTRAINT [PK_Elanlar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Elanlar_Isciler_GonderenIsciId] FOREIGN KEY ([GonderenIsciId]) REFERENCES [Isciler]([Id])
    );
    CREATE INDEX [IX_Elanlar_GonderenIsciId] ON [Elanlar] ([GonderenIsciId]);
    PRINT 'Created table: Elanlar'
END
ELSE
    PRINT 'Table already exists: Elanlar'
GO

-- ============================================================
-- 3. PerformansQiymetlendirmeler
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PerformansQiymetlendirmeler')
BEGIN
    CREATE TABLE [PerformansQiymetlendirmeler] (
        [Id]                         INT            IDENTITY(1,1) NOT NULL,
        [IsciId]                     INT            NOT NULL,
        [QiymetlendirenIsciId]       INT            NOT NULL,
        [DovrTipi]                   INT            NOT NULL,
        [Il]                         INT            NOT NULL,
        [Rubu]                       INT            NULL,
        [Status]                     INT            NOT NULL DEFAULT(0),
        [IsciOrtalamaQiymet]         DECIMAL(5,2)   NOT NULL DEFAULT(0),
        [MudirOrtalamaQiymet]        DECIMAL(5,2)   NOT NULL DEFAULT(0),
        [YekunQiymet]                DECIMAL(5,2)   NOT NULL DEFAULT(0),
        [IsciSherhi]                 NVARCHAR(MAX)  NULL,
        [MudirSherhi]                NVARCHAR(MAX)  NULL,
        [InkisafPlani]               NVARCHAR(MAX)  NULL,
        [IsciQiymetlendirmeTarixi]   DATETIME2      NULL,
        [MudirQiymetlendirmeTarixi]  DATETIME2      NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]            DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]            INT            NULL,
        [YenileyenIcraciId]          INT            NULL,
        [SilenIcraciId]              INT            NULL,
        [YenilenmeTarixi]            DATETIME2      NULL,
        [Silinib]                    BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]              DATETIME2      NULL,
        CONSTRAINT [PK_PerformansQiymetlendirmeler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PerformansQiymetlendirmeler_Isciler_IsciId] FOREIGN KEY ([IsciId]) REFERENCES [Isciler]([Id]),
        CONSTRAINT [FK_PerformansQiymetlendirmeler_Isciler_QiymetlendirenIsciId] FOREIGN KEY ([QiymetlendirenIsciId]) REFERENCES [Isciler]([Id])
    );
    CREATE INDEX [IX_PerformansQiymetlendirmeler_IsciId] ON [PerformansQiymetlendirmeler] ([IsciId]);
    CREATE INDEX [IX_PerformansQiymetlendirmeler_QiymetlendirenIsciId] ON [PerformansQiymetlendirmeler] ([QiymetlendirenIsciId]);
    PRINT 'Created table: PerformansQiymetlendirmeler'
END
ELSE
    PRINT 'Table already exists: PerformansQiymetlendirmeler'
GO

-- ============================================================
-- 4. PerformansKriteriyalar
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PerformansKriteriyalar')
BEGIN
    CREATE TABLE [PerformansKriteriyalar] (
        [Id]              INT            IDENTITY(1,1) NOT NULL,
        [PerformansId]    INT            NOT NULL,
        [KriteriyaAdi]    NVARCHAR(MAX)  NOT NULL,
        [Ceki]            DECIMAL(5,2)   NOT NULL DEFAULT(0),
        [IsciQiymeti]     DECIMAL(5,2)   NULL,
        [MudirQiymeti]    DECIMAL(5,2)   NULL,
        [IsciSherhi]      NVARCHAR(MAX)  NULL,
        [MudirSherhi]     NVARCHAR(MAX)  NULL,
        -- BaseEntity fields
        [YaradilmaTarixi] DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId] INT            NULL,
        [YenileyenIcraciId] INT          NULL,
        [SilenIcraciId]   INT            NULL,
        [YenilenmeTarixi] DATETIME2      NULL,
        [Silinib]         BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]   DATETIME2      NULL,
        CONSTRAINT [PK_PerformansKriteriyalar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PerformansKriteriyalar_PerformansQiymetlendirmeler_PerformansId] FOREIGN KEY ([PerformansId]) REFERENCES [PerformansQiymetlendirmeler]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_PerformansKriteriyalar_PerformansId] ON [PerformansKriteriyalar] ([PerformansId]);
    PRINT 'Created table: PerformansKriteriyalar'
END
ELSE
    PRINT 'Table already exists: PerformansKriteriyalar'
GO

-- ============================================================
-- 5. Telimler
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Telimler')
BEGIN
    CREATE TABLE [Telimler] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Ad]                NVARCHAR(MAX)  NOT NULL,
        [Tesvir]            NVARCHAR(MAX)  NULL,
        [Tesviqci]          NVARCHAR(MAX)  NULL,
        [Mekan]             NVARCHAR(MAX)  NULL,
        [BaslamaTarixi]     DATETIME2      NOT NULL,
        [BitmeTarixi]       DATETIME2      NULL,
        [MuddetSaat]        INT            NOT NULL DEFAULT(0),
        [Status]            INT            NOT NULL DEFAULT(0),
        [DaxiliTelimdir]    BIT            NOT NULL DEFAULT(1),
        [Xerc]              DECIMAL(18,2)  NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_Telimler] PRIMARY KEY ([Id])
    );
    PRINT 'Created table: Telimler'
END
ELSE
    PRINT 'Table already exists: Telimler'
GO

-- ============================================================
-- 6. TelimIshtiraklar
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TelimIshtiraklar')
BEGIN
    CREATE TABLE [TelimIshtiraklar] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [TelimId]           INT            NOT NULL,
        [IsciId]            INT            NOT NULL,
        [Istirakdir]        BIT            NOT NULL DEFAULT(0),
        [Qiymet]            DECIMAL(5,2)   NULL,
        [Qeyd]              NVARCHAR(MAX)  NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_TelimIshtiraklar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TelimIshtiraklar_Telimler_TelimId] FOREIGN KEY ([TelimId]) REFERENCES [Telimler]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TelimIshtiraklar_Isciler_IsciId] FOREIGN KEY ([IsciId]) REFERENCES [Isciler]([Id])
    );
    CREATE INDEX [IX_TelimIshtiraklar_TelimId] ON [TelimIshtiraklar] ([TelimId]);
    CREATE INDEX [IX_TelimIshtiraklar_IsciId] ON [TelimIshtiraklar] ([IsciId]);
    PRINT 'Created table: TelimIshtiraklar'
END
ELSE
    PRINT 'Table already exists: TelimIshtiraklar'
GO

-- ============================================================
-- 7. Sertifikatlar
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sertifikatlar')
BEGIN
    CREATE TABLE [Sertifikatlar] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [IsciId]            INT            NOT NULL,
        [Ad]                NVARCHAR(MAX)  NOT NULL,
        [VerenQurum]        NVARCHAR(MAX)  NULL,
        [VerilmeTarixi]     DATETIME2      NOT NULL,
        [BitirmeTarixi]     DATETIME2      NULL,
        [FaylYolu]          NVARCHAR(MAX)  NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_Sertifikatlar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sertifikatlar_Isciler_IsciId] FOREIGN KEY ([IsciId]) REFERENCES [Isciler]([Id])
    );
    CREATE INDEX [IX_Sertifikatlar_IsciId] ON [Sertifikatlar] ([IsciId]);
    PRINT 'Created table: Sertifikatlar'
END
ELSE
    PRINT 'Table already exists: Sertifikatlar'
GO

-- ============================================================
-- 8. XercKateqoriyalari (with seed data)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'XercKateqoriyalari')
BEGIN
    CREATE TABLE [XercKateqoriyalari] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Ad]                NVARCHAR(450)  NOT NULL,
        [Ikon]              NVARCHAR(MAX)  NULL,
        [Aktivdir]          BIT            NOT NULL DEFAULT(1),
        -- BaseEntity fields
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_XercKateqoriyalari] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_XercKateqoriyalari_Ad] ON [XercKateqoriyalari] ([Ad]);

    -- Seed data for XercKateqoriyalari
    SET IDENTITY_INSERT [XercKateqoriyalari] ON;
    INSERT INTO [XercKateqoriyalari] ([Id], [Ad], [Ikon], [Aktivdir], [Silinib], [YaradilmaTarixi])
    VALUES
        (1, N'Taksi',             N'bi-taxi-front',   1, 0, GETDATE()),
        (2, N'Yem' + NCHAR(601) + N'k',  N'bi-cup-hot',      1, 0, GETDATE()),
        (3, N'Ofis l' + NCHAR(601) + N'vazımatı', N'bi-printer', 1, 0, GETDATE()),
        (4, N'S' + NCHAR(601) + N'f' + NCHAR(601) + N'r x' + NCHAR(601) + N'rcl' + NCHAR(601) + N'ri', N'bi-airplane', 1, 0, GETDATE()),
        (5, N'Dig' + NCHAR(601) + N'r',  N'bi-three-dots',   1, 0, GETDATE());
    SET IDENTITY_INSERT [XercKateqoriyalari] OFF;

    PRINT 'Created table: XercKateqoriyalari (with seed data)'
END
ELSE
    PRINT 'Table already exists: XercKateqoriyalari'
GO

-- ============================================================
-- 9. Xercler
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Xercler')
BEGIN
    CREATE TABLE [Xercler] (
        [Id]                  INT            IDENTITY(1,1) NOT NULL,
        [IsciId]              INT            NOT NULL,
        [KateqoriyaId]        INT            NOT NULL,
        [Tesvir]              NVARCHAR(MAX)  NOT NULL,
        [Mebleg]              DECIMAL(18,2)  NOT NULL,
        [XercTarixi]          DATETIME2      NOT NULL,
        [QebzFaylYolu]        NVARCHAR(MAX)  NULL,
        [Status]              INT            NOT NULL DEFAULT(0),
        [TesdiqleyenIsciId]   INT            NULL,
        [TesdiqTarixi]        DATETIME2      NULL,
        [ImtinaSebebi]        NVARCHAR(MAX)  NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]     DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]     INT            NULL,
        [YenileyenIcraciId]   INT            NULL,
        [SilenIcraciId]       INT            NULL,
        [YenilenmeTarixi]     DATETIME2      NULL,
        [Silinib]             BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]       DATETIME2      NULL,
        CONSTRAINT [PK_Xercler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Xercler_Isciler_IsciId] FOREIGN KEY ([IsciId]) REFERENCES [Isciler]([Id]),
        CONSTRAINT [FK_Xercler_XercKateqoriyalari_KateqoriyaId] FOREIGN KEY ([KateqoriyaId]) REFERENCES [XercKateqoriyalari]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Xercler_Isciler_TesdiqleyenIsciId] FOREIGN KEY ([TesdiqleyenIsciId]) REFERENCES [Isciler]([Id])
    );
    CREATE INDEX [IX_Xercler_IsciId] ON [Xercler] ([IsciId]);
    CREATE INDEX [IX_Xercler_KateqoriyaId] ON [Xercler] ([KateqoriyaId]);
    CREATE INDEX [IX_Xercler_TesdiqleyenIsciId] ON [Xercler] ([TesdiqleyenIsciId]);
    PRINT 'Created table: Xercler'
END
ELSE
    PRINT 'Table already exists: Xercler'
GO

-- ============================================================
-- 10. Budceler
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Budceler')
BEGIN
    CREATE TABLE [Budceler] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [DepartamentId]     INT            NOT NULL,
        [Il]                INT            NOT NULL,
        [Ay]                INT            NOT NULL,
        [PlanMebleg]        DECIMAL(18,2)  NOT NULL DEFAULT(0),
        [FaktikiMebleg]     DECIMAL(18,2)  NOT NULL DEFAULT(0),
        [Qeyd]              NVARCHAR(MAX)  NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_Budceler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Budceler_Departament_DepartamentId] FOREIGN KEY ([DepartamentId]) REFERENCES [Departament]([Id])
    );
    CREATE UNIQUE INDEX [IX_Budceler_DepartamentId_Il_Ay] ON [Budceler] ([DepartamentId], [Il], [Ay]);
    PRINT 'Created table: Budceler'
END
ELSE
    PRINT 'Table already exists: Budceler'
GO

-- ============================================================
-- 11. Bildirisler
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Bildirisler')
BEGIN
    CREATE TABLE [Bildirisler] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Bashliq]           NVARCHAR(MAX)  NOT NULL,
        [Metn]              NVARCHAR(MAX)  NOT NULL,
        [Nov]               INT            NOT NULL,
        [IsciId]            INT            NOT NULL,
        [Oxunub]            BIT            NOT NULL DEFAULT(0),
        [OxunmaTarixi]      DATETIME2      NULL,
        [RedirectUrl]       NVARCHAR(MAX)  NULL,
        [MesajId]           INT            NULL,
        [IcazeId]           INT            NULL,
        [MezuniyyetId]      INT            NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_Bildirisler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bildirisler_Isciler_IsciId] FOREIGN KEY ([IsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Bildirisler_IsciId] ON [Bildirisler] ([IsciId]);
    PRINT 'Created table: Bildirisler'
END
ELSE
    PRINT 'Table already exists: Bildirisler'
GO

-- ============================================================
-- 12. Mesajlar
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Mesajlar')
BEGIN
    CREATE TABLE [Mesajlar] (
        [Id]                    INT            IDENTITY(1,1) NOT NULL,
        [GonderenIsciId]        INT            NOT NULL,
        [AlanIsciId]            INT            NOT NULL,
        [Movzu]                 NVARCHAR(MAX)  NOT NULL,
        [Metn]                  NVARCHAR(MAX)  NOT NULL,
        [Oxunub]                BIT            NOT NULL DEFAULT(0),
        [OxunmaTarixi]          DATETIME2      NULL,
        [CavabVerdigiMesajId]   INT            NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]       DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]       INT            NULL,
        [YenileyenIcraciId]     INT            NULL,
        [SilenIcraciId]         INT            NULL,
        [YenilenmeTarixi]       DATETIME2      NULL,
        [Silinib]               BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]         DATETIME2      NULL,
        CONSTRAINT [PK_Mesajlar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Mesajlar_Isciler_GonderenIsciId] FOREIGN KEY ([GonderenIsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Mesajlar_Isciler_AlanIsciId] FOREIGN KEY ([AlanIsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Mesajlar_Mesajlar_CavabVerdigiMesajId] FOREIGN KEY ([CavabVerdigiMesajId]) REFERENCES [Mesajlar]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Mesajlar_GonderenIsciId] ON [Mesajlar] ([GonderenIsciId]);
    CREATE INDEX [IX_Mesajlar_AlanIsciId] ON [Mesajlar] ([AlanIsciId]);
    CREATE INDEX [IX_Mesajlar_CavabVerdigiMesajId] ON [Mesajlar] ([CavabVerdigiMesajId]);
    PRINT 'Created table: Mesajlar'
END
ELSE
    PRINT 'Table already exists: Mesajlar'
GO

-- ============================================================
-- 13. Tapshiriqlar
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tapshiriqlar')
BEGIN
    CREATE TABLE [Tapshiriqlar] (
        [Id]                  INT            IDENTITY(1,1) NOT NULL,
        [Bashliq]             NVARCHAR(MAX)  NOT NULL,
        [Tesvir]              NVARCHAR(MAX)  NULL,
        [Qeyd]                NVARCHAR(MAX)  NULL,
        [Status]              INT            NOT NULL DEFAULT(0),
        [Prioritet]           INT            NOT NULL DEFAULT(0),
        [TamamlanmaFaizi]     INT            NOT NULL DEFAULT(0),
        [SonTarix]            DATETIME2      NULL,
        [YaradanIsciId]       INT            NOT NULL,
        [TeyinOlunanIsciId]   INT            NOT NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]     DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]     INT            NULL,
        [YenileyenIcraciId]   INT            NULL,
        [SilenIcraciId]       INT            NULL,
        [YenilenmeTarixi]     DATETIME2      NULL,
        [Silinib]             BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]       DATETIME2      NULL,
        CONSTRAINT [PK_Tapshiriqlar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tapshiriqlar_Isciler_YaradanIsciId] FOREIGN KEY ([YaradanIsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tapshiriqlar_Isciler_TeyinOlunanIsciId] FOREIGN KEY ([TeyinOlunanIsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Tapshiriqlar_YaradanIsciId] ON [Tapshiriqlar] ([YaradanIsciId]);
    CREATE INDEX [IX_Tapshiriqlar_TeyinOlunanIsciId] ON [Tapshiriqlar] ([TeyinOlunanIsciId]);
    PRINT 'Created table: Tapshiriqlar'
END
ELSE
    PRINT 'Table already exists: Tapshiriqlar'
GO

-- ============================================================
-- 14. TapshiriqSherhler
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TapshiriqSherhler')
BEGIN
    CREATE TABLE [TapshiriqSherhler] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [TapshiriqId]       INT            NOT NULL,
        [MuellifIsciId]     INT            NOT NULL,
        [Metn]              NVARCHAR(MAX)  NOT NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_TapshiriqSherhler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TapshiriqSherhler_Tapshiriqlar_TapshiriqId] FOREIGN KEY ([TapshiriqId]) REFERENCES [Tapshiriqlar]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TapshiriqSherhler_Isciler_MuellifIsciId] FOREIGN KEY ([MuellifIsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_TapshiriqSherhler_TapshiriqId] ON [TapshiriqSherhler] ([TapshiriqId]);
    CREATE INDEX [IX_TapshiriqSherhler_MuellifIsciId] ON [TapshiriqSherhler] ([MuellifIsciId]);
    PRINT 'Created table: TapshiriqSherhler'
END
ELSE
    PRINT 'Table already exists: TapshiriqSherhler'
GO

-- ============================================================
-- 15. Gorushler
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Gorushler')
BEGIN
    CREATE TABLE [Gorushler] (
        [Id]                  INT            IDENTITY(1,1) NOT NULL,
        [Bashliq]             NVARCHAR(MAX)  NOT NULL,
        [Tarix]               DATETIME2      NOT NULL,
        [BaslamaSaati]        TIME           NOT NULL,
        [BitisSaati]          TIME           NOT NULL,
        [Yer]                 NVARCHAR(MAX)  NULL,
        [OnlineLink]          NVARCHAR(MAX)  NULL,
        [Agenda]              NVARCHAR(MAX)  NULL,
        [Qeydler]             NVARCHAR(MAX)  NULL,
        [Nov]                 INT            NOT NULL DEFAULT(0),
        [Status]              INT            NOT NULL DEFAULT(0),
        [TeshkilatciIsciId]   INT            NOT NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]     DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]     INT            NULL,
        [YenileyenIcraciId]   INT            NULL,
        [SilenIcraciId]       INT            NULL,
        [YenilenmeTarixi]     DATETIME2      NULL,
        [Silinib]             BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]       DATETIME2      NULL,
        CONSTRAINT [PK_Gorushler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Gorushler_Isciler_TeshkilatciIsciId] FOREIGN KEY ([TeshkilatciIsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Gorushler_TeshkilatciIsciId] ON [Gorushler] ([TeshkilatciIsciId]);
    PRINT 'Created table: Gorushler'
END
ELSE
    PRINT 'Table already exists: Gorushler'
GO

-- ============================================================
-- 16. GorushIshtirakcilar
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GorushIshtirakcilar')
BEGIN
    CREATE TABLE [GorushIshtirakcilar] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [GorushId]          INT            NOT NULL,
        [IsciId]            INT            NOT NULL,
        [Status]            INT            NOT NULL DEFAULT(0),
        [Qeyd]              NVARCHAR(MAX)  NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_GorushIshtirakcilar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GorushIshtirakcilar_Gorushler_GorushId] FOREIGN KEY ([GorushId]) REFERENCES [Gorushler]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GorushIshtirakcilar_Isciler_IsciId] FOREIGN KEY ([IsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_GorushIshtirakcilar_GorushId] ON [GorushIshtirakcilar] ([GorushId]);
    CREATE INDEX [IX_GorushIshtirakcilar_IsciId] ON [GorushIshtirakcilar] ([IsciId]);
    PRINT 'Created table: GorushIshtirakcilar'
END
ELSE
    PRINT 'Table already exists: GorushIshtirakcilar'
GO

-- ============================================================
-- 17. Xatirlatmalar
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Xatirlatmalar')
BEGIN
    CREATE TABLE [Xatirlatmalar] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Bashliq]           NVARCHAR(MAX)  NOT NULL,
        [Qeyd]              NVARCHAR(MAX)  NULL,
        [XatirlatmaTarixi]  DATETIME2      NOT NULL,
        [Nov]               INT            NOT NULL DEFAULT(0),
        [IsciId]            INT            NOT NULL,
        [EntityId]          INT            NULL,
        [EntityTipi]        INT            NULL,
        [GonderiilibMi]     BIT            NOT NULL DEFAULT(0),
        [OxunubMu]          BIT            NOT NULL DEFAULT(0),
        -- BaseEntity fields
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_Xatirlatmalar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Xatirlatmalar_Isciler_IsciId] FOREIGN KEY ([IsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Xatirlatmalar_IsciId] ON [Xatirlatmalar] ([IsciId]);
    PRINT 'Created table: Xatirlatmalar'
END
ELSE
    PRINT 'Table already exists: Xatirlatmalar'
GO

-- ============================================================
-- 18. EvezediciTesdiqler
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvezediciTesdiqler')
BEGIN
    CREATE TABLE [EvezediciTesdiqler] (
        [Id]                  INT            IDENTITY(1,1) NOT NULL,
        [MezuniyyetId]        INT            NOT NULL,
        [EvezediciIsciId]     INT            NOT NULL,
        [Status]              INT            NOT NULL DEFAULT(0),
        [Qeyd]                NVARCHAR(MAX)  NULL,
        [CavabTarixi]         DATETIME2      NULL,
        -- BaseEntity fields
        [YaradilmaTarixi]     DATETIME2      NOT NULL DEFAULT(GETDATE()),
        [YaradanIcraciId]     INT            NULL,
        [YenileyenIcraciId]   INT            NULL,
        [SilenIcraciId]       INT            NULL,
        [YenilenmeTarixi]     DATETIME2      NULL,
        [Silinib]             BIT            NOT NULL DEFAULT(0),
        [SilinmeTarixi]       DATETIME2      NULL,
        CONSTRAINT [PK_EvezediciTesdiqler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EvezediciTesdiqler_Isciler_EvezediciIsciId] FOREIGN KEY ([EvezediciIsciId]) REFERENCES [Isciler]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvezediciTesdiqler_Mezuniyyetler_MezuniyyetId] FOREIGN KEY ([MezuniyyetId]) REFERENCES [Mezuniyyetler]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_EvezediciTesdiqler_EvezediciIsciId] ON [EvezediciTesdiqler] ([EvezediciIsciId]);
    CREATE INDEX [IX_EvezediciTesdiqler_MezuniyyetId] ON [EvezediciTesdiqler] ([MezuniyyetId]);
    PRINT 'Created table: EvezediciTesdiqler'
END
ELSE
    PRINT 'Table already exists: EvezediciTesdiqler'
GO

-- ============================================================
-- Mark pending migrations as applied in __EFMigrationsHistory
-- ============================================================
PRINT ''
PRINT '=== Marking migrations as applied ==='

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260409120033_YeniModullar')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260409120033_YeniModullar', '8.0.23');
    PRINT 'Marked as applied: 20260409120033_YeniModullar'
END
ELSE
    PRINT 'Already applied: 20260409120033_YeniModullar'

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260410005721_AddChatBulkAndReadReceipt')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260410005721_AddChatBulkAndReadReceipt', '8.0.23');
    PRINT 'Marked as applied: 20260410005721_AddChatBulkAndReadReceipt'
END
ELSE
    PRINT 'Already applied: 20260410005721_AddChatBulkAndReadReceipt'

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260410009592_ChatElanEmoji')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260410009592_ChatElanEmoji', '8.0.23');
    PRINT 'Marked as applied: 20260410009592_ChatElanEmoji'
END
ELSE
    PRINT 'Already applied: 20260410009592_ChatElanEmoji'

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260410101008_ElanYeniex')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260410101008_ElanYeniex', '8.0.23');
    PRINT 'Marked as applied: 20260410101008_ElanYeniex'
END
ELSE
    PRINT 'Already applied: 20260410101008_ElanYeniex'
GO

PRINT ''
PRINT '=== VM Setup Script Complete ==='
PRINT 'End time: ' + CONVERT(VARCHAR, GETDATE(), 120)
GO
