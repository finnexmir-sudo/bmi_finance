using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <summary>
    /// SenedYolu sütununu NVARCHAR(500) → NVARCHAR(MAX) edir.
    /// Çoxlu sənəd dəstəyi üçün "|" ayırıcısı ilə uzun sətir lazım ola bilər.
    /// </summary>
    public partial class MezuniyyetSenedYoluMax : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Mezuniyyetler'
                      AND COLUMN_NAME = 'SenedYolu'
                      AND CHARACTER_MAXIMUM_LENGTH = 500
                )
                BEGIN
                    ALTER TABLE Mezuniyyetler
                    ALTER COLUMN SenedYolu NVARCHAR(MAX) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Geri qaytarma: çox uzun dəyərlər kəsilə bilər
                UPDATE Mezuniyyetler
                SET SenedYolu = LEFT(SenedYolu, 500)
                WHERE LEN(SenedYolu) > 500;

                ALTER TABLE Mezuniyyetler
                ALTER COLUMN SenedYolu NVARCHAR(500) NULL;
            ");
        }
    }
}
