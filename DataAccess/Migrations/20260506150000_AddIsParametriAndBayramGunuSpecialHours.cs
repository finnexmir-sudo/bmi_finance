using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260506150000_AddIsParametriAndBayramGunuSpecialHours")]
    public partial class AddIsParametriAndBayramGunuSpecialHours : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── IsParametrleri cədvəli ──────────────────────────────
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IsParametrleri')
BEGIN
    CREATE TABLE [IsParametrleri] (
        [Id]                    INT            NOT NULL IDENTITY(1,1),
        [StandartGirisVaxti]    TIME(7)        NOT NULL DEFAULT '09:00:00',
        [StandartCixisVaxti]    TIME(7)        NOT NULL DEFAULT '17:45:00',
        [GecikmeToleransDeqiqe] INT            NOT NULL DEFAULT 5,
        [TezCixmaToleransDeqiqe] INT           NOT NULL DEFAULT 15,
        [YaradilmaTarixi]       DATETIME2      NOT NULL DEFAULT GETDATE(),
        [YaradanIcraciId]       INT            NULL,
        [YenileyenIcraciId]     INT            NULL,
        [SilenIcraciId]         INT            NULL,
        [YenilenmeTarixi]       DATETIME2      NULL,
        [Silinib]               BIT            NOT NULL DEFAULT 0,
        [SilinmeTarixi]         DATETIME2      NULL,
        CONSTRAINT [PK_IsParametrleri] PRIMARY KEY ([Id])
    );
END
");

            // ── BayramGunleri — xüsusi saat sütunları ──────────────
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'BayramGunleri' AND COLUMN_NAME = 'XususiBaslayisVaxti'
)
BEGIN
    ALTER TABLE [BayramGunleri] ADD [XususiBaslayisVaxti] TIME(7) NULL;
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'BayramGunleri' AND COLUMN_NAME = 'XususiBitisVaxti'
)
BEGIN
    ALTER TABLE [BayramGunleri] ADD [XususiBitisVaxti] TIME(7) NULL;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IsParametrleri') DROP TABLE [IsParametrleri];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BayramGunleri' AND COLUMN_NAME = 'XususiBaslayisVaxti') ALTER TABLE [BayramGunleri] DROP COLUMN [XususiBaslayisVaxti];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BayramGunleri' AND COLUMN_NAME = 'XususiBitisVaxti') ALTER TABLE [BayramGunleri] DROP COLUMN [XususiBitisVaxti];");
        }
    }
}
