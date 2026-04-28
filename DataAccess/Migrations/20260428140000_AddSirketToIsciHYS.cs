using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// IsciHYS cədvəlinə Sirket (nvarchar(200), nullable) sütunu əlavə edir.
    /// Bir işçi bir neçə sığorta şirkətində HYS açdıqda hər qeydi ayrıca
    /// uçotlamaq üçün lazımdır. Mövcud qeydlər NULL (köhnə adlandırma yoxdur)
    /// kimi qalır — funksionallıq pozulmur.
    /// </summary>
    public partial class AddSirketToIsciHYS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'Sirket' AND Object_ID = Object_ID('IsciHYS')
                )
                BEGIN
                    ALTER TABLE IsciHYS ADD Sirket nvarchar(200) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'Sirket' AND Object_ID = Object_ID('IsciHYS')
                )
                BEGIN
                    ALTER TABLE IsciHYS DROP COLUMN Sirket;
                END
            ");
        }
    }
}
