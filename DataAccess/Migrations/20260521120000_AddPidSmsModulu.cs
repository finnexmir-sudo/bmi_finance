using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521120000_AddPidSmsModulu")]
    public partial class AddPidSmsModulu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PidSmsSablonlar')
                BEGIN
                    CREATE TABLE [PidSmsSablonlar] (
                        [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [Ad]                NVARCHAR(200)   NOT NULL DEFAULT '',
                        [Metn]              NVARCHAR(1000)  NOT NULL DEFAULT '',
                        [Aciqlama]          NVARCHAR(500)   NULL,
                        [Aktiv]             BIT             NOT NULL DEFAULT 1,
                        [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YenilenmeTarixi]   DATETIME2       NULL,
                        [SilinmeTarixi]     DATETIME2       NULL,
                        [YaradanIcraciId]   INT             NULL,
                        [YenileyenIcraciId] INT             NULL,
                        [SilenIcraciId]     INT             NULL,
                        [Silinib]           BIT             NOT NULL DEFAULT 0
                    );

                    CREATE INDEX [IX_PidSmsSablonlar_Aktiv] ON [PidSmsSablonlar] ([Aktiv]);
                END
            ");

            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PidSmsLoglar')
                BEGIN
                    CREATE TABLE [PidSmsLoglar] (
                        [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [Telefon]           NVARCHAR(20)    NOT NULL DEFAULT '',
                        [Metn]              NVARCHAR(1000)  NOT NULL DEFAULT '',
                        [SablonId]          INT             NULL,
                        [Status]            INT             NOT NULL DEFAULT 0,
                        [GonderilmeTarixi]  DATETIME2       NULL,
                        [GoyercinId]        NVARCHAR(100)   NULL,
                        [GatewayCavabi]     NVARCHAR(2000)  NULL,
                        [Xeta]              NVARCHAR(1000)  NULL,
                        [GonderenIsciId]    INT             NOT NULL,
                        [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YenilenmeTarixi]   DATETIME2       NULL,
                        [SilinmeTarixi]     DATETIME2       NULL,
                        [YaradanIcraciId]   INT             NULL,
                        [YenileyenIcraciId] INT             NULL,
                        [SilenIcraciId]     INT             NULL,
                        [Silinib]           BIT             NOT NULL DEFAULT 0,
                        CONSTRAINT [FK_PidSmsLoglar_PidSmsSablonlar_SablonId]
                            FOREIGN KEY ([SablonId]) REFERENCES [PidSmsSablonlar] ([Id]) ON DELETE SET NULL,
                        CONSTRAINT [FK_PidSmsLoglar_Isciler_GonderenIsciId]
                            FOREIGN KEY ([GonderenIsciId]) REFERENCES [Isciler] ([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_PidSmsLoglar_YaradilmaTarixi] ON [PidSmsLoglar] ([YaradilmaTarixi] DESC);
                    CREATE INDEX [IX_PidSmsLoglar_SablonId]        ON [PidSmsLoglar] ([SablonId]);
                    CREATE INDEX [IX_PidSmsLoglar_GonderenIsciId]  ON [PidSmsLoglar] ([GonderenIsciId]);
                    CREATE INDEX [IX_PidSmsLoglar_GoyercinId]      ON [PidSmsLoglar] ([GoyercinId]);
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DROP TABLE IF EXISTS [PidSmsLoglar];");
            m.Sql("DROP TABLE IF EXISTS [PidSmsSablonlar];");
        }
    }
}
