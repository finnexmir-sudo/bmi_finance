using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610120000_AddMezuniyyetJetonFields")]
    public partial class AddMezuniyyetJetonFields : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME  = 'Mezuniyyetler'
                      AND COLUMN_NAME = 'JetonleOdend')
                BEGIN
                    ALTER TABLE [Mezuniyyetler]
                    ADD [JetonleOdend] BIT NOT NULL DEFAULT 0;
                END
            ");

            m.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME  = 'Mezuniyyetler'
                      AND COLUMN_NAME = 'IstifadeOlunanJetonSaat')
                BEGIN
                    ALTER TABLE [Mezuniyyetler]
                    ADD [IstifadeOlunanJetonSaat] DECIMAL(18,2) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME  = 'Mezuniyyetler'
                      AND COLUMN_NAME = 'IstifadeOlunanJetonSaat')
                BEGIN
                    ALTER TABLE [Mezuniyyetler]
                    DROP COLUMN [IstifadeOlunanJetonSaat];
                END
            ");

            m.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME  = 'Mezuniyyetler'
                      AND COLUMN_NAME = 'JetonleOdend')
                BEGIN
                    ALTER TABLE [Mezuniyyetler]
                    DROP COLUMN [JetonleOdend];
                END
            ");
        }
    }
}
