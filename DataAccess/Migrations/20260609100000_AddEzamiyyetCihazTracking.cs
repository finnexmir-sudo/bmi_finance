using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260609100000_AddEzamiyyetCihazTracking")]
    public partial class AddEzamiyyetCihazTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('EzamiyyetMuracietler')
                      AND name = 'CihazCixisVaxti')
                BEGIN
                    ALTER TABLE [EzamiyyetMuracietler]
                        ADD [CihazCixisVaxti]   DATETIME2 NULL,
                            [CihazQayidisVaxti] DATETIME2 NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('EzamiyyetMuracietler')
                      AND name = 'CihazCixisVaxti')
                BEGIN
                    ALTER TABLE [EzamiyyetMuracietler]
                        DROP COLUMN [CihazCixisVaxti],
                                    [CihazQayidisVaxti];
                END
            ");
        }
    }
}
