using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260607100000_AddMehkemeIsiModulu")]
    public partial class AddMehkemeIsiModulu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                               WHERE TABLE_NAME = 'MehkemeIsleri')
                BEGIN
                    CREATE TABLE [MehkemeIsleri] (
                        [Id]                  INT IDENTITY(1,1) PRIMARY KEY,
                        [QeydiyyatNomresi]    NVARCHAR(20)  NOT NULL,
                        [BorcluAd]            NVARCHAR(200) NOT NULL,
                        [EsasBorc]            DECIMAL(18,2) NULL,
                        [MehkemeXerci]        DECIMAL(18,2) NULL,
                        [Nov]                 INT           NOT NULL DEFAULT 4,
                        [Status]              INT           NOT NULL DEFAULT 1,
                        [BaslamaTarixi]       DATE          NULL,
                        [Qeyd]                NVARCHAR(MAX) NULL,
                        [YaradilmaTarixi]     DATETIME2     NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]     INT           NULL,
                        [YenileyenIcraciId]   INT           NULL,
                        [SilenIcraciId]       INT           NULL,
                        [YenilenmeTarixi]     DATETIME2     NULL,
                        [Silinib]             BIT           NOT NULL DEFAULT 0,
                        [SilinmeTarixi]       DATETIME2     NULL
                    );
                END
            ");

            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                               WHERE TABLE_NAME = 'MehkemeMerheleri')
                BEGIN
                    CREATE TABLE [MehkemeMerheleri] (
                        [Id]                  INT IDENTITY(1,1) PRIMARY KEY,
                        [MehkemeIsiId]        INT           NOT NULL,
                        [MerheleTipi]         INT           NOT NULL DEFAULT 6,
                        [Tarix]               DATE          NOT NULL,
                        [Qeyd]                NVARCHAR(MAX) NULL,
                        [SenedYolu]           NVARCHAR(500) NULL,
                        [YaradilmaTarixi]     DATETIME2     NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]     INT           NULL,
                        [YenileyenIcraciId]   INT           NULL,
                        [SilenIcraciId]       INT           NULL,
                        [YenilenmeTarixi]     DATETIME2     NULL,
                        [Silinib]             BIT           NOT NULL DEFAULT 0,
                        [SilinmeTarixi]       DATETIME2     NULL,
                        CONSTRAINT [FK_MehkemeMerheleri_MehkemeIsleri]
                            FOREIGN KEY ([MehkemeIsiId]) REFERENCES [MehkemeIsleri]([Id])
                    );
                END
            ");

            // SistemAyar — PidMehkemeSorguId sütunu
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME  = 'SistemAyarlari'
                                 AND COLUMN_NAME = 'PidMehkemeSorguId')
                BEGIN
                    ALTER TABLE [SistemAyarlari] ADD [PidMehkemeSorguId] INT NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MehkemeMerheleri')
                    DROP TABLE [MehkemeMerheleri];
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MehkemeIsleri')
                    DROP TABLE [MehkemeIsleri];
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'SistemAyarlari' AND COLUMN_NAME = 'PidMehkemeSorguId')
                    ALTER TABLE [SistemAyarlari] DROP COLUMN [PidMehkemeSorguId];
            ");
        }
    }
}
