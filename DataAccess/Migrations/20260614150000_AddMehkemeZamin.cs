using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260614150000_AddMehkemeZamin")]
    public partial class AddMehkemeZamin : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MehkemeZaminler')
                BEGIN
                    CREATE TABLE [MehkemeZaminler] (
                        [Id]                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [MehkemeIsiId]      INT NOT NULL,
                        [Ad]                NVARCHAR(MAX) NOT NULL,
                        [Fin]               NVARCHAR(MAX) NULL,
                        [DogumTarixi]       NVARCHAR(MAX) NULL,
                        [Telefon]           NVARCHAR(MAX) NULL,
                        [Unvan]             NVARCHAR(MAX) NULL,
                        [EmekHaqqiTutulma]  NVARCHAR(MAX) NULL,
                        [IsYeri]            NVARCHAR(MAX) NULL,
                        [EmlakaHebs]        NVARCHAR(MAX) NULL,
                        [Stop]              NVARCHAR(MAX) NULL,
                        [IcraMemuru]        NVARCHAR(MAX) NULL,
                        [IcraSonIsler]      NVARCHAR(MAX) NULL,
                        [DypSorgu]          NVARCHAR(MAX) NULL,
                        [AdinaSorgu]        NVARCHAR(MAX) NULL,
                        [IcraQeyd]          NVARCHAR(MAX) NULL,
                        [YaradilmaTarixi]   DATETIME2 NOT NULL,
                        [YaradanIcraciId]   INT NULL,
                        [YenileyenIcraciId] INT NULL,
                        [SilenIcraciId]     INT NULL,
                        [YenilenmeTarixi]   DATETIME2 NULL,
                        [Silinib]           BIT NOT NULL DEFAULT 0,
                        [SilinmeTarixi]     DATETIME2 NULL,
                        CONSTRAINT [FK_MehkemeZaminler_MehkemeIsleri] FOREIGN KEY ([MehkemeIsiId])
                            REFERENCES [MehkemeIsleri]([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_MehkemeZaminler_MehkemeIsiId] ON [MehkemeZaminler]([MehkemeIsiId]);
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MehkemeZaminler')
                    DROP TABLE [MehkemeZaminler];
            ");
        }
    }
}
