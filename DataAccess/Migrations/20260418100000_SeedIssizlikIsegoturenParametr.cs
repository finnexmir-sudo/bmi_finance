using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İşəgötürən İşsizlik Sığortası flat parametrini seed edir (0.5%).
    /// MaasParametri səhifəsində "KONFİQURASİYA YOXDUR" yazırdı — indi redaktə olunacaq.
    /// </summary>
    public partial class SeedIssizlikIsegoturenParametr : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // İşəgötürən İşsizlik Sığortası (Nov=8) — flat 0.5%
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM MaasParametrleri WHERE Nov = 8 AND Silinib = 0 AND Aktivdir = 1)
                    INSERT INTO MaasParametrleri (Nov, Tip, Deyer, Aktivdir, BaslamaTarixi, YaradilmaTarixi, Silinib)
                    VALUES (8, 1, 0.5, 1, '2026-01-01', GETUTCDATE(), 0);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM MaasParametrleri WHERE Nov = 8 AND Deyer = 0.5");
        }
    }
}
