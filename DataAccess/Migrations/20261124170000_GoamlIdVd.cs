using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// GOAML — DOC_DNA_ALL_RISK.ID_VD sahəsi (24.11.2026). Namizədin SWIFT/xarici
    /// invaluta mənşəli olduğunu göstərən açar — Sender-in bank adının
    /// (`sender_account_fin_inst_name`) real goAML-dəki mənbəyi. İstifadəçi
    /// Oracle Forms "Properties" (SENDER_BANK_NAME → DOC_VNESH_SWIFT1.
    /// SENDER_BANK_NAME) göstərdi, canlı Oracle sorğusu ilə join açarı
    /// (`odb.doc_vnesh_swift.id = id_vd`) TƏSDİQLƏNDİ.
    /// Yalnız `AddColumn` — `InsertData` yoxdur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261124170000_GoamlIdVd")]
    public partial class GoamlIdVd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "IdVd",
                table: "GoamlEmeliyyatlar",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdVd",
                table: "GoamlEmeliyyatlar");
        }
    }
}
