using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    public partial class AddIsGunuBitdiElan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[IsGunuBitdiElanlar];");
        }
    }
}
