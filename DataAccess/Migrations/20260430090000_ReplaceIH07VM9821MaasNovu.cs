using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İki yeni MaasNovu əlavə edir (Id=17, Id=18):
    ///   Id=17 — 18.02.2016 tarixli IH-07 saylı əmrlə əlavə təminat
    ///   Id=18 — VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan gəlirlər
    /// Hər ikisi Tip=Gelir(1). İdempotent: ad üzrə yoxlanılır, təkrarlanmaz.
    /// </summary>
    public partial class ReplaceIH07VM9821MaasNovu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IH-07 Əlavə Təminat (Id=17)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Ad = N'IH-07 Əlavə Təminat')
                BEGIN
                    SET IDENTITY_INSERT MaasNovleri ON;
                    INSERT INTO MaasNovleri (Id, Ad, Tip, Aktivdir, Silinib, YaradilmaTarixi)
                    VALUES (17, N'IH-07 Əlavə Təminat', 1, 1, 0, GETDATE());
                    SET IDENTITY_INSERT MaasNovleri OFF;
                END
            ");

            // VM 98.2.1 Gəlirləri (Id=18)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Ad = N'VM 98.2.1 Gəlirləri')
                BEGIN
                    SET IDENTITY_INSERT MaasNovleri ON;
                    INSERT INTO MaasNovleri (Id, Ad, Tip, Aktivdir, Silinib, YaradilmaTarixi)
                    VALUES (18, N'VM 98.2.1 Gəlirləri', 1, 1, 0, GETDATE());
                    SET IDENTITY_INSERT MaasNovleri OFF;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM MaasNovleri WHERE Ad IN (N'IH-07 Əlavə Təminat', N'VM 98.2.1 Gəlirləri');
            ");
        }
    }
}
