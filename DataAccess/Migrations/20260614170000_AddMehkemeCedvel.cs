using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260614170000_AddMehkemeCedvel")]
    public partial class AddMehkemeCedvel : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MehkemeCedveller')
                BEGIN
                    CREATE TABLE [MehkemeCedveller] (
                        [Id]                     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Sira]                   INT NULL,
                        [Status]                 NVARCHAR(MAX) NULL,
                        [BorcluAd]               NVARCHAR(MAX) NOT NULL,
                        [KreditNovu]             NVARCHAR(MAX) NULL,
                        [KreditHesabi]           NVARCHAR(MAX) NULL,
                        [Subkod]                 NVARCHAR(MAX) NULL,
                        [MehkemeyeVerilmeTarixi] DATETIME2 NULL,
                        [MehkemeSenedi]          NVARCHAR(MAX) NULL,
                        [QetnameTarixi]          DATETIME2 NULL,
                        [Qeyd]                   NVARCHAR(MAX) NULL,
                        [YaradilmaTarixi]        DATETIME2 NOT NULL,
                        [YaradanIcraciId]        INT NULL,
                        [YenileyenIcraciId]      INT NULL,
                        [SilenIcraciId]          INT NULL,
                        [YenilenmeTarixi]        DATETIME2 NULL,
                        [Silinib]                BIT NOT NULL DEFAULT 0,
                        [SilinmeTarixi]          DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MehkemeCedvelIclaslar')
                BEGIN
                    CREATE TABLE [MehkemeCedvelIclaslar] (
                        [Id]                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [MehkemeCedvelId]   INT NOT NULL,
                        [Tarix]             DATETIME2 NULL,
                        [Saat]              NVARCHAR(MAX) NULL,
                        [Netice]            NVARCHAR(MAX) NULL,
                        [YaradilmaTarixi]   DATETIME2 NOT NULL,
                        [YaradanIcraciId]   INT NULL,
                        [YenileyenIcraciId] INT NULL,
                        [SilenIcraciId]     INT NULL,
                        [YenilenmeTarixi]   DATETIME2 NULL,
                        [Silinib]           BIT NOT NULL DEFAULT 0,
                        [SilinmeTarixi]     DATETIME2 NULL,
                        CONSTRAINT [FK_MehkemeCedvelIclaslar_MehkemeCedveller] FOREIGN KEY ([MehkemeCedvelId])
                            REFERENCES [MehkemeCedveller]([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_MehkemeCedvelIclaslar_MehkemeCedvelId] ON [MehkemeCedvelIclaslar]([MehkemeCedvelId]);
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MehkemeCedvelIclaslar') DROP TABLE [MehkemeCedvelIclaslar];
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MehkemeCedveller') DROP TABLE [MehkemeCedveller];
            ");
        }
    }
}
