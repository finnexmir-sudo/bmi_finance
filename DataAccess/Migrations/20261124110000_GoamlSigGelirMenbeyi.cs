using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// GOAML — "Hesab imza hüququ olan şəxs" (SIG) blokuna "Gəlir mənbəyi"
    /// sahəsi (24.11.2026). Eyni cədvəl/tab (`odb.imza_huquqi_olan_shexsler.
    /// GELIR_MENBEYI`) SigVezife/SigIsYeriAdi/SigAyliqGelir ilə — istifadəçi
    /// Oracle Forms "Properties" ekranını göstərdi (Name=GELIR_MENBEYI).
    /// ⚠️ `GoamlEmeliyyat.GelirMenbeyi` (mövcud, "Əməliyyatın əlavə sahələri"
    /// — GOAML_CLIENTS-in raw mirror sütunu) İLƏ QARIŞDIRMA — o, BAŞQA, ƏVVƏLKİ
    /// bir sahədir, bu YENİ sahə (`SigGelirMenbeyi`) ilə heç bir əlaqəsi yoxdur.
    /// Yalnız `AddColumn` — `InsertData` yoxdur, bu layihənin "Əl ilə Yazılan
    /// Migration" qaydasına uyğundur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261124110000_GoamlSigGelirMenbeyi")]
    public partial class GoamlSigGelirMenbeyi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SigGelirMenbeyi",
                table: "GoamlEmeliyyatlar",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SigGelirMenbeyi",
                table: "GoamlEmeliyyatlar");
        }
    }
}
