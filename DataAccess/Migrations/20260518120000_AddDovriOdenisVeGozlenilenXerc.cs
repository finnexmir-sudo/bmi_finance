using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    public partial class AddDovriOdenisVeGozlenilenXerc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DovriOdenisler')
BEGIN
    CREATE TABLE [DovriOdenisler] (
        [Id]                    INT            NOT NULL IDENTITY(1,1),
        [Ad]                    NVARCHAR(200)  NOT NULL DEFAULT '',
        [KateqoriyaId]          INT            NOT NULL,
        [DepartamentId]         INT            NOT NULL,
        [Mebleg]                DECIMAL(18,2)  NOT NULL DEFAULT 0,
        [Dovruluq]              INT            NOT NULL DEFAULT 2,
        [BaslamaTarixi]         DATETIME2      NOT NULL,
        [BitmeTarixi]           DATETIME2      NULL,
        [NovbatiOdenisTarixi]   DATETIME2      NOT NULL,
        [Aktiv]                 BIT            NOT NULL DEFAULT 1,
        [Qeyd]                  NVARCHAR(500)  NULL,
        [YaradilmaTarixi]       DATETIME2      NOT NULL DEFAULT GETDATE(),
        [YaradanIcraciId]       INT            NULL,
        [YenileyenIcraciId]     INT            NULL,
        [SilenIcraciId]         INT            NULL,
        [YenilenmeTarixi]       DATETIME2      NULL,
        [Silinib]               BIT            NOT NULL DEFAULT 0,
        [SilinmeTarixi]         DATETIME2      NULL,
        CONSTRAINT [PK_DovriOdenisler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DovriOdenisler_XercKateqoriyalari_KateqoriyaId]
            FOREIGN KEY ([KateqoriyaId]) REFERENCES [XercKateqoriyalari]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DovriOdenisler_Departments_DepartamentId]
            FOREIGN KEY ([DepartamentId]) REFERENCES [Departments]([Id]) ON DELETE NO ACTION
    );
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GozlenilenXercler')
BEGIN
    CREATE TABLE [GozlenilenXercler] (
        [Id]                INT            NOT NULL IDENTITY(1,1),
        [Ad]                NVARCHAR(200)  NOT NULL DEFAULT '',
        [Tesvir]            NVARCHAR(1000) NULL,
        [KateqoriyaId]      INT            NOT NULL,
        [DepartamentId]     INT            NULL,
        [TəxminiMebleg]     DECIMAL(18,2)  NOT NULL DEFAULT 0,
        [GozlenilenTarix]   DATETIME2      NOT NULL,
        [Prioritet]         INT            NOT NULL DEFAULT 2,
        [Status]            INT            NOT NULL DEFAULT 0,
        [XercId]            INT            NULL,
        [Qeyd]              NVARCHAR(500)  NULL,
        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT GETDATE(),
        [YaradanIcraciId]   INT            NULL,
        [YenileyenIcraciId] INT            NULL,
        [SilenIcraciId]     INT            NULL,
        [YenilenmeTarixi]   DATETIME2      NULL,
        [Silinib]           BIT            NOT NULL DEFAULT 0,
        [SilinmeTarixi]     DATETIME2      NULL,
        CONSTRAINT [PK_GozlenilenXercler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GozlenilenXercler_XercKateqoriyalari_KateqoriyaId]
            FOREIGN KEY ([KateqoriyaId]) REFERENCES [XercKateqoriyalari]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GozlenilenXercler_Departments_DepartamentId]
            FOREIGN KEY ([DepartamentId]) REFERENCES [Departments]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GozlenilenXercler_Xercler_XercId]
            FOREIGN KEY ([XercId]) REFERENCES [Xercler]([Id]) ON DELETE NO ACTION
    );
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GozlenilenXercler') DROP TABLE [GozlenilenXercler];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DovriOdenisler') DROP TABLE [DovriOdenisler];");
        }
    }
}
