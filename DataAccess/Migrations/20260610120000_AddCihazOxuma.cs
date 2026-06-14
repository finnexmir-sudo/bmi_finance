using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610120000_AddCihazOxuma")]
    public partial class AddCihazOxuma : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.tables WHERE name = 'CihazOxumalar')
                BEGIN
                    CREATE TABLE [CihazOxumalar] (
                        [Id]                 INT IDENTITY(1,1) NOT NULL,
                        [IsciId]             INT       NOT NULL,
                        [Vaxt]               DATETIME2 NOT NULL,
                        [Nov]                INT       NOT NULL DEFAULT(0),
                        [Girisdir]           BIT       NOT NULL DEFAULT(0),
                        [YaradilmaTarixi]    DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
                        [YaradanIcraciId]    INT       NULL,
                        [YenileyenIcraciId]  INT       NULL,
                        [SilenIcraciId]      INT       NULL,
                        [YenilenmeTarixi]    DATETIME2 NULL,
                        [Silinib]            BIT       NOT NULL DEFAULT(0),
                        [SilinmeTarixi]      DATETIME2 NULL,
                        CONSTRAINT [PK_CihazOxumalar] PRIMARY KEY ([Id])
                    );

                    CREATE INDEX [IX_CihazOxumalar_IsciId_Vaxt]
                        ON [CihazOxumalar] ([IsciId], [Vaxt]);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CihazOxumalar')
                    DROP TABLE [CihazOxumalar];
            ");
        }
    }
}
