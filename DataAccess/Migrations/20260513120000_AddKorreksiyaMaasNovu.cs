using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <summary>
    /// MaasNovleri cədvəlinə əvvəlki ay korreksiyası üçün iki yeni növ əlavə edir:
    ///   Id=15: "Əvvəlki Ay Artıq Ödəniş Kəsintisi"  (Tip=Tutulma=2)
    ///   Id=16: "Əvvəlki Ay Kompensasiyası"           (Tip=Gelir=1)
    /// İdempotent: artıq mövcuddursa heç nə etmir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260513120000_AddKorreksiyaMaasNovu")]
    public partial class AddKorreksiyaMaasNovu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT MaasNovleri ON;

                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Id = 15)
                    INSERT INTO MaasNovleri (Id, Ad, Tip, Aktivdir, Silinib, YaradilmaTarixi)
                    VALUES (15, N'Əvvəlki Ay Artıq Ödəniş Kəsintisi', 2, 1, 0, GETDATE());

                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Id = 16)
                    INSERT INTO MaasNovleri (Id, Ad, Tip, Aktivdir, Silinib, YaradilmaTarixi)
                    VALUES (16, N'Əvvəlki Ay Kompensasiyası', 1, 1, 0, GETDATE());

                SET IDENTITY_INSERT MaasNovleri OFF;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM MaasNovleri WHERE Id IN (15, 16);
            ");
        }
    }
}
