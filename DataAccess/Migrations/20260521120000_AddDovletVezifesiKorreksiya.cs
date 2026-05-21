using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521120000_AddDovletVezifesiKorreksiya")]
    public partial class AddDovletVezifesiKorreksiya : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            // ── 1. Mezuniyyetler cədvəlinə yeni sütunlar ─────────────────
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME='Mezuniyyetler' AND COLUMN_NAME='KorreksiyaOlunanMezuniyyetId'
                )
                BEGIN
                    ALTER TABLE [Mezuniyyetler]
                        ADD [KorreksiyaOlunanMezuniyyetId] INT           NULL,
                            [SenedYolu]                    NVARCHAR(500) NULL,
                            [KorreksiyaSebebi]              NVARCHAR(MAX) NULL;

                    ALTER TABLE [Mezuniyyetler]
                        ADD CONSTRAINT [FK_Mezuniyyetler_Mezuniyyetler_Korreksiya]
                        FOREIGN KEY ([KorreksiyaOlunanMezuniyyetId])
                        REFERENCES [Mezuniyyetler]([Id]);
                END
            ");

            // ── 2. DavamiyyetStatus = 7 (OdenisDovletVezifesi) ──────────
            // Enum dəyəri kodda tərəfindən idarə olunur; DB-də sadəcə INT
            // sütunu var. Mövcud check constraint varsa genişlət.
            m.Sql(@"
                -- Mövcud check constraint-i sil (varsa), yeni dəyərə icazə ver
                DECLARE @con NVARCHAR(200);
                SELECT @con = name
                FROM   sys.check_constraints
                WHERE  parent_object_id = OBJECT_ID('Davamiyyetler')
                  AND  definition LIKE '%Status%';
                IF @con IS NOT NULL
                    EXEC('ALTER TABLE [Davamiyyetler] DROP CONSTRAINT [' + @con + ']');
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME='Mezuniyyetler' AND COLUMN_NAME='KorreksiyaOlunanMezuniyyetId'
                )
                BEGIN
                    ALTER TABLE [Mezuniyyetler]
                        DROP CONSTRAINT IF EXISTS [FK_Mezuniyyetler_Mezuniyyetler_Korreksiya];
                    ALTER TABLE [Mezuniyyetler]
                        DROP COLUMN [KorreksiyaOlunanMezuniyyetId],
                                    [SenedYolu],
                                    [KorreksiyaSebebi];
                END
            ");
        }
    }
}
