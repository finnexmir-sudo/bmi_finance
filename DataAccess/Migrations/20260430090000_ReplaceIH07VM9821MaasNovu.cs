using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// "Fərqli Gəlir" (Id=14) əvəzinə iki yeni MaasNovu əlavə edir:
    ///   Id=15 — 18.02.2016 tarixli IH-07 saylı əmrlə əlavə təminat
    ///   Id=16 — VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan gəlirlər
    /// Hər ikisi Tip=Gelir(1). İdempotent: artıq mövcuddursa keçir.
    /// </summary>
    public partial class ReplaceIH07VM9821MaasNovu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Köhnə "Fərqli Gəlir" girişini deaktiv et (mövcud MaasDetay qeydlərini qorumaq üçün silmirik)
            migrationBuilder.Sql(@"
                UPDATE MaasNovleri SET Aktivdir = 0 WHERE Id = 14 AND Ad = N'Fərqli Gəlir';
            ");

            // IH-07 Əlavə Təminat
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Id = 15)
                BEGIN
                    SET IDENTITY_INSERT MaasNovleri ON;
                    INSERT INTO MaasNovleri (Id, Ad, Tip, Aktivdir, Silinib, YaradilmaTarixi)
                    VALUES (15, N'IH-07 Əlavə Təminat', 1, 1, 0, GETDATE());
                    SET IDENTITY_INSERT MaasNovleri OFF;
                END
            ");

            // VM 98.2.1 Gəlirləri
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Id = 16)
                BEGIN
                    SET IDENTITY_INSERT MaasNovleri ON;
                    INSERT INTO MaasNovleri (Id, Ad, Tip, Aktivdir, Silinib, YaradilmaTarixi)
                    VALUES (16, N'VM 98.2.1 Gəlirləri', 1, 1, 0, GETDATE());
                    SET IDENTITY_INSERT MaasNovleri OFF;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE MaasNovleri SET Aktivdir = 1 WHERE Id = 14 AND Ad = N'Fərqli Gəlir';
                DELETE FROM MaasNovleri WHERE Id IN (15, 16);
            ");
        }
    }
}
