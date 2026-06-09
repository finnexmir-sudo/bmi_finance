using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260609090000_AddErkenCixisIcaze")]
    public partial class AddErkenCixisIcaze : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ErkenCixisIcazeler')
                BEGIN
                    CREATE TABLE [ErkenCixisIcazeler] (
                        [Id]                    INT IDENTITY(1,1) NOT NULL,
                        [IsciId]                INT NOT NULL,
                        [Tarix]                 DATE NOT NULL,
                        [IcazeVerenIsciId]      INT NOT NULL,
                        [YaradilmaTarixi]       DATETIME2 NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]       INT NULL,
                        [YenileyenIcraciId]     INT NULL,
                        [SilenIcraciId]         INT NULL,
                        [YenilenmeTarixi]       DATETIME2 NULL,
                        [Silinib]               BIT NOT NULL DEFAULT 0,
                        [SilinmeTarixi]         DATETIME2 NULL,
                        CONSTRAINT [PK_ErkenCixisIcazeler] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ErkenCixisIcazeler_Isciler_IsciId]
                            FOREIGN KEY ([IsciId]) REFERENCES [Isciler] ([Id])
                    );
                    CREATE INDEX [IX_ErkenCixisIcazeler_IsciId_Tarix]
                        ON [ErkenCixisIcazeler] ([IsciId], [Tarix]);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [ErkenCixisIcazeler];");
        }
    }
}
