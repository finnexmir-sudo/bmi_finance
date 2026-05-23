using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İşçi üzrə aylıq Bonus/Overtime qeydləri üçün cədvəl + "Overtime" maaş növü.
    /// İdempotentdir — artıq mövcuddursa heç nə etmir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260522120000_AddAyliqElaveQeydi")]
    public partial class AddAyliqElaveQeydi : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            // ── Cədvəl ──────────────────────────────────────────────────
            m.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AyliqElaveQeydleri')
                BEGIN
                    CREATE TABLE [dbo].[AyliqElaveQeydleri] (
                        [Id]                  INT            NOT NULL IDENTITY(1,1),
                        [IsciId]              INT            NOT NULL,
                        [Il]                  INT            NOT NULL,
                        [Ay]                  INT            NOT NULL,
                        [Bonus]               DECIMAL(18,2)  NOT NULL DEFAULT 0,
                        [Overtime]            DECIMAL(18,2)  NOT NULL DEFAULT 0,
                        [YaradilmaTarixi]     DATETIME2      NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]     INT            NULL,
                        [YenileyenIcraciId]   INT            NULL,
                        [SilenIcraciId]       INT            NULL,
                        [YenilenmeTarixi]     DATETIME2      NULL,
                        [Silinib]             BIT            NOT NULL DEFAULT 0,
                        [SilinmeTarixi]       DATETIME2      NULL,
                        CONSTRAINT [PK_AyliqElaveQeydleri] PRIMARY KEY ([Id])
                    );

                    -- Hər (İşçi, İl, Ay) üçün yalnız bir aktiv qeyd
                    CREATE UNIQUE INDEX [IX_AyliqElaveQeydleri_Isci_Il_Ay]
                        ON [dbo].[AyliqElaveQeydleri] ([IsciId], [Il], [Ay])
                        WHERE [Silinib] = 0;
                END
            ");

            // ── "Overtime" maaş növü (Tip = Gelir = 1) ──────────────────
            m.Sql(@"
                IF NOT EXISTS (SELECT * FROM [dbo].[MaasNovleri] WHERE [Ad] = N'Overtime')
                BEGIN
                    INSERT INTO [dbo].[MaasNovleri] ([Ad], [Tip], [Aktivdir], [Silinib], [YaradilmaTarixi])
                    VALUES (N'Overtime', 1, 1, 0, GETDATE());
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DELETE FROM [dbo].[MaasNovleri] WHERE [Ad] = N'Overtime';");
            m.Sql("DROP TABLE IF EXISTS [dbo].[AyliqElaveQeydleri];");
        }
    }
}
