using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// MaasNovleri cədvəlinə "Fərqli Gəlir" növü (Id=14, Tip=Gelir) əlavə edir.
    /// Bonusdan ayrı uçotlanan, lakin bonus kimi vergiyə cəlb edilən gəlir növü.
    /// İdempotent: artıq mövcuddursa heç nə etmir.
    /// </summary>
    public partial class AddFerqliGelirMaasNovu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Id = 14)
                BEGIN
                    SET IDENTITY_INSERT MaasNovleri ON;
                    INSERT INTO MaasNovleri (Id, Ad, Tip, Aktivdir, Silinib, YaradilmaTarixi)
                    VALUES (14, N'Fərqli Gəlir', 1, 1, 0, GETDATE());
                    SET IDENTITY_INSERT MaasNovleri OFF;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM MaasNovleri WHERE Id = 14 AND Ad = N'Fərqli Gəlir';
            ");
        }
    }
}
