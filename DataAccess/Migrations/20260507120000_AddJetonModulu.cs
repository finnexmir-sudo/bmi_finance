using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507120000_AddJetonModulu")]
    public partial class AddJetonModulu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── JetonTeyinatlari ──────────────────────────────────────
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'JetonTeyinatlari')
BEGIN
    CREATE TABLE [JetonTeyinatlari] (
        [Id]               INT            NOT NULL IDENTITY(1,1),
        [Ad]               NVARCHAR(MAX)  NOT NULL,
        [Nov]              INT            NOT NULL,
        [Rengi]            INT            NOT NULL,
        [SaatDeyeri]       DECIMAL(5,2)   NOT NULL,
        [Ikon]             NVARCHAR(MAX)  NOT NULL,
        [RengKodu]         NVARCHAR(MAX)  NOT NULL,
        [Tesvir]           NVARCHAR(MAX)  NULL,
        [Aktivdir]         BIT            NOT NULL DEFAULT 1,
        [YaradilmaTarixi]  DATETIME2      NOT NULL DEFAULT GETDATE(),
        [YaradanIcraciId]  INT            NULL,
        [YenileyenIcraciId] INT           NULL,
        [SilenIcraciId]    INT            NULL,
        [YenilenmeTarixi]  DATETIME2      NULL,
        [SilinmeTarixi]    DATETIME2      NULL,
        [Silinib]          BIT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_JetonTeyinatlari] PRIMARY KEY ([Id])
    );
END");

            // ── JetonRedimTelebieri ───────────────────────────────────
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'JetonRedimTelebieri')
BEGIN
    CREATE TABLE [JetonRedimTelebieri] (
        [Id]                 INT           NOT NULL IDENTITY(1,1),
        [IsciId]             INT           NOT NULL,
        [RedimNovu]          INT           NOT NULL,
        [CemiSaat]           DECIMAL(8,2)  NOT NULL,
        [Status]             INT           NOT NULL DEFAULT 1,
        [TelabTarixi]        DATETIME2     NOT NULL DEFAULT GETDATE(),
        [NeticeTarixi]       DATETIME2     NULL,
        [TesdiqleyenUserId]  INT           NULL,
        [Qeyd]               NVARCHAR(MAX) NULL,
        [YaradilmaTarixi]    DATETIME2     NOT NULL DEFAULT GETDATE(),
        [YaradanIcraciId]    INT           NULL,
        [YenileyenIcraciId]  INT           NULL,
        [SilenIcraciId]      INT           NULL,
        [YenilenmeTarixi]    DATETIME2     NULL,
        [SilinmeTarixi]      DATETIME2     NULL,
        [Silinib]            BIT           NOT NULL DEFAULT 0,
        CONSTRAINT [PK_JetonRedimTelebieri] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JetonRedimTelebieri_Isciler_IsciId] FOREIGN KEY ([IsciId])
            REFERENCES [Isciler] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_JetonRedimTelebieri_IsciId] ON [JetonRedimTelebieri] ([IsciId]);
END");

            // ── IsciJetonlari ─────────────────────────────────────────
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IsciJetonlari')
BEGIN
    CREATE TABLE [IsciJetonlari] (
        [Id]               INT            NOT NULL IDENTITY(1,1),
        [IsciId]           INT            NOT NULL,
        [JetonTeyinatiId]  INT            NOT NULL,
        [QazanmaTarixi]    DATETIME2      NOT NULL DEFAULT GETDATE(),
        [Sebeb]            NVARCHAR(MAX)  NOT NULL,
        [VerenUserId]      INT            NOT NULL,
        [Status]           INT            NOT NULL DEFAULT 1,
        [RedimTelebiId]    INT            NULL,
        [YaradilmaTarixi]  DATETIME2      NOT NULL DEFAULT GETDATE(),
        [YaradanIcraciId]  INT            NULL,
        [YenileyenIcraciId] INT           NULL,
        [SilenIcraciId]    INT            NULL,
        [YenilenmeTarixi]  DATETIME2      NULL,
        [SilinmeTarixi]    DATETIME2      NULL,
        [Silinib]          BIT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_IsciJetonlari] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IsciJetonlari_Isciler_IsciId] FOREIGN KEY ([IsciId])
            REFERENCES [Isciler] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_IsciJetonlari_JetonTeyinatlari_JetonTeyinatiId] FOREIGN KEY ([JetonTeyinatiId])
            REFERENCES [JetonTeyinatlari] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_IsciJetonlari_JetonRedimTelebieri_RedimTelebiId] FOREIGN KEY ([RedimTelebiId])
            REFERENCES [JetonRedimTelebieri] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_IsciJetonlari_IsciId] ON [IsciJetonlari] ([IsciId]);
    CREATE INDEX [IX_IsciJetonlari_JetonTeyinatiId] ON [IsciJetonlari] ([JetonTeyinatiId]);
    CREATE INDEX [IX_IsciJetonlari_RedimTelebiId] ON [IsciJetonlari] ([RedimTelebiId]);
END");

            // ── Seed: 4 default JetonTeyinati ────────────────────────
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [JetonTeyinatlari] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [JetonTeyinatlari] ON;
    INSERT INTO [JetonTeyinatlari]
        ([Id],[Ad],[Nov],[Rengi],[SaatDeyeri],[Ikon],[RengKodu],[Tesvir],[Aktivdir],[YaradilmaTarixi],[Silinib])
    VALUES
        (1, N'Bürünc Jeton', 1, 1, 0.5,  N'bi bi-award-fill',         N'#cd7f32', N'Kiçik mükafat — 30 dəqiqə',           1, '2026-01-01', 0),
        (2, N'Gümüş Jeton',  1, 2, 1.0,  N'bi bi-award-fill',         N'#9ca3af', N'Orta mükafat — 1 saat',              1, '2026-01-01', 0),
        (3, N'Qızıl Jeton',  1, 3, 4.0,  N'bi bi-award-fill',         N'#f59e0b', N'Böyük mükafat — 4 saat',            1, '2026-01-01', 0),
        (4, N'Qara Jeton',   2, 4, 0.0,  N'bi bi-slash-circle-fill',  N'#1f2937', N'İntizam cəzası — mükafat dondurulur', 1, '2026-01-01', 0);
    SET IDENTITY_INSERT [JetonTeyinatlari] OFF;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [IsciJetonlari]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [JetonRedimTelebieri]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [JetonTeyinatlari]");
        }
    }
}
