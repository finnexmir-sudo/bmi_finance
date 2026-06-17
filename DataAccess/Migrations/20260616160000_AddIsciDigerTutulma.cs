using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İşçidən digər tutulma (sığorta və s.) cədvəli. Maaşa təsir etmir —
    /// yalnız provodkada aylıq pay net ödənişdən çıxılır.
    /// İdempotentdir — cədvəl artıq mövcuddursa heç nə etmir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260616160000_AddIsciDigerTutulma")]
    public partial class AddIsciDigerTutulma : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IsciDigerTutulmalari')
                BEGIN
                    CREATE TABLE [dbo].[IsciDigerTutulmalari] (
                        [Id]                  INT            NOT NULL IDENTITY(1,1),
                        [IsciId]              INT            NOT NULL,
                        [Mebleg]              DECIMAL(18,2)  NOT NULL DEFAULT 0,
                        [MuddetAy]            INT            NOT NULL DEFAULT 12,
                        [BaslamaIl]           INT            NOT NULL DEFAULT 0,
                        [BaslamaAy]           INT            NOT NULL DEFAULT 0,
                        [AyliqMebleg]         DECIMAL(18,2)  NOT NULL DEFAULT 0,
                        [Aciqlama]            NVARCHAR(MAX)  NULL,
                        [Aktiv]               BIT            NOT NULL DEFAULT 1,
                        [YaradilmaTarixi]     DATETIME2      NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]     INT            NULL,
                        [YenileyenIcraciId]   INT            NULL,
                        [SilenIcraciId]       INT            NULL,
                        [YenilenmeTarixi]     DATETIME2      NULL,
                        [Silinib]             BIT            NOT NULL DEFAULT 0,
                        [SilinmeTarixi]       DATETIME2      NULL,
                        CONSTRAINT [PK_IsciDigerTutulmalari] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DROP TABLE IF EXISTS [dbo].[IsciDigerTutulmalari];");
        }
    }
}
