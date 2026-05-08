using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    public partial class AddNaharNezereAlinmasinToIcaze : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Icazeler'
                      AND COLUMN_NAME = 'NaharNezereAlinmasin'
                )
                BEGIN
                    ALTER TABLE Icazeler ADD NaharNezereAlinmasin BIT NOT NULL DEFAULT 0
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Icazeler'
                      AND COLUMN_NAME = 'NaharNezereAlinmasin'
                )
                BEGIN
                    ALTER TABLE Icazeler DROP COLUMN NaharNezereAlinmasin
                END
            ");
        }
    }
}
