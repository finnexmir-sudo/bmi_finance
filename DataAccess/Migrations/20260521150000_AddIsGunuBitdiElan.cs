using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521150000_AddIsGunuBitdiElan")]
    public partial class AddIsGunuBitdiElan : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IsGunuBitdiElanlar')
                BEGIN
                    CREATE TABLE [dbo].[IsGunuBitdiElanlar] (
                        [Id]                  INT            NOT NULL IDENTITY(1,1),
                        [Tarix]               DATETIME2      NOT NULL,
                        [BitisVaxti]          TIME(7)        NOT NULL,
                        [YaradilmaTarixi]     DATETIME2      NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]     INT            NULL,
                        [YenileyenIcraciId]   INT            NULL,
                        [SilenIcraciId]       INT            NULL,
                        [YenilenmeTarixi]     DATETIME2      NULL,
                        [Silinib]             BIT            NOT NULL DEFAULT 0,
                        [SilinmeTarixi]       DATETIME2      NULL,
                        CONSTRAINT [PK_IsGunuBitdiElanlar] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DROP TABLE IF EXISTS [dbo].[IsGunuBitdiElanlar];");
        }
    }
}
