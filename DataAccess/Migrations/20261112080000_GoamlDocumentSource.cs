using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// GOAML — Export to JSON (#15) üçün namizəddən köçürülən "Document Source"
    /// sahəsi (12.11.2026). Yalnız `AddColumn` — `InsertData` yoxdur, bu layihənin
    /// "Əl ilə Yazılan Migration" qaydasına uyğundur. `GoamlEmeliyyatStatusu.
    /// IxracEdilib` dəyəri isə yeni sütun TƏLƏB ETMİR — mövcud `Status` sütunu
    /// (int) onsuz da bu yeni dəyəri daşıya bilər, migration lazım deyil.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261112080000_GoamlDocumentSource")]
    public partial class GoamlDocumentSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentSource",
                table: "GoamlEmeliyyatlar",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentSource",
                table: "GoamlEmeliyyatlar");
        }
    }
}
