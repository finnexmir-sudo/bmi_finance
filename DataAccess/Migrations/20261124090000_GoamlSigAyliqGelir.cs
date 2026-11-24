using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// GOAML — "Hesab imza hüququ olan şəxs" (SIG) blokuna "Aylıq gəlir"
    /// sahəsi (24.11.2026). İstifadəçi AYSEL-in Oracle Forms "Properties of
    /// item AYLIQ_GELIR" ekran-görüntüsünü göndərdi: Canvas=HUQUQI_SHEXS,
    /// Tab Page/Master Mirror Item=IMZA_HUQUQI_OLAN_SHEXSLER.AYLIQ_GELIR,
    /// Insert/Update Allowed=TRUE — SigVezife/SigIsYeriAdi ilə EYNİ cədvəl/
    /// tab, operatorun əl ilə doldura bildiyi bir sahə. Yalnız `AddColumn` —
    /// `InsertData` yoxdur, bu layihənin "Əl ilə Yazılan Migration" qaydasına
    /// uyğundur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261124090000_GoamlSigAyliqGelir")]
    public partial class GoamlSigAyliqGelir : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SigAyliqGelir",
                table: "GoamlEmeliyyatlar",
                type: "decimal(18,2)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SigAyliqGelir",
                table: "GoamlEmeliyyatlar");
        }
    }
}
