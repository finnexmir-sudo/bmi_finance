using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    public partial class AddJetonIleOdendiToMezuniyyet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Mezuniyyetler'
                      AND COLUMN_NAME = 'JetonIleOdendi')
                BEGIN
                    ALTER TABLE Mezuniyyetler ADD JetonIleOdendi BIT NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Mezuniyyetler'
                      AND COLUMN_NAME = 'IstifadeOlunanJetonSaat')
                BEGIN
                    ALTER TABLE Mezuniyyetler ADD IstifadeOlunanJetonSaat DECIMAL(18,2) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Mezuniyyetler' AND COLUMN_NAME = 'JetonIleOdendi')
                    ALTER TABLE Mezuniyyetler DROP COLUMN JetonIleOdendi;
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Mezuniyyetler' AND COLUMN_NAME = 'IstifadeOlunanJetonSaat')
                    ALTER TABLE Mezuniyyetler DROP COLUMN IstifadeOlunanJetonSaat;
            ");
        }
    }
}
