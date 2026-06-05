using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260603100000_AddEzamiyyetTables")]
    public partial class AddEzamiyyetTables : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EzamiyyetMekanlar')
                BEGIN
                    CREATE TABLE [dbo].[EzamiyyetMekanlar] (
                        [Id]                INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Ad]                NVARCHAR(200) NOT NULL,
                        [Aktiv]             BIT           NOT NULL DEFAULT 1,
                        [YaradilmaTarixi]   DATETIME2     NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]   INT           NULL,
                        [YenileyenIcraciId] INT           NULL,
                        [SilenIcraciId]     INT           NULL,
                        [YenilenmeTarixi]   DATETIME2     NULL,
                        [Silinib]           BIT           NOT NULL DEFAULT 0,
                        [SilinmeTarixi]     DATETIME2     NULL
                    );
                END
            ");

            m.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EzamiyyetMuracietler')
                BEGIN
                    CREATE TABLE [dbo].[EzamiyyetMuracietler] (
                        [Id]                  INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [IsciId]              INT           NOT NULL,
                        [Baslig]              NVARCHAR(300) NOT NULL,
                        [MekanId]             INT           NOT NULL,
                        [BaslamaTarixi]       DATETIME2     NOT NULL,
                        [BitmeTarixi]         DATETIME2     NOT NULL,
                        [BaslamaSaati]        TIME          NULL,
                        [BitisSaati]          TIME          NULL,
                        [SenedYolu]           NVARCHAR(500) NULL,
                        [SenedAd]             NVARCHAR(300) NULL,
                        [Qeyd]                NVARCHAR(1000) NULL,
                        [Status]              INT           NOT NULL DEFAULT 1,
                        [RehberTesdiq]        BIT           NULL,
                        [RehberId]            INT           NULL,
                        [RehberTesdiqTarixi]  DATETIME2     NULL,
                        [RehberQeydi]         NVARCHAR(500) NULL,
                        [YaradilmaTarixi]     DATETIME2     NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]     INT           NULL,
                        [YenileyenIcraciId]   INT           NULL,
                        [SilenIcraciId]       INT           NULL,
                        [YenilenmeTarixi]     DATETIME2     NULL,
                        [Silinib]             BIT           NOT NULL DEFAULT 0,
                        [SilinmeTarixi]       DATETIME2     NULL,
                        CONSTRAINT [FK_EzamMur_Isci]    FOREIGN KEY ([IsciId])   REFERENCES [Isciler]([Id]),
                        CONSTRAINT [FK_EzamMur_Mekan]   FOREIGN KEY ([MekanId])  REFERENCES [EzamiyyetMekanlar]([Id]),
                        CONSTRAINT [FK_EzamMur_Rehber]  FOREIGN KEY ([RehberId]) REFERENCES [Isciler]([Id])
                    );
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("IF EXISTS (SELECT * FROM sys.tables WHERE name = 'EzamiyyetMuracietler') DROP TABLE [dbo].[EzamiyyetMuracietler];");
            m.Sql("IF EXISTS (SELECT * FROM sys.tables WHERE name = 'EzamiyyetMekanlar')    DROP TABLE [dbo].[EzamiyyetMekanlar];");
        }
    }
}
