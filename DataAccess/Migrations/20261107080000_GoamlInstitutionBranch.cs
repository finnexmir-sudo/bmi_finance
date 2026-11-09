using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// GOAML — "Update New" avtomatik doldurma mexanizmi üçün çatışmayan tək sahə
    /// (07.11.2026). Yalnız `AddColumn` — `InsertData` yoxdur, bu layihənin
    /// "Əl ilə Yazılan Migration" qaydasına uyğundur (AddColumn TargetModel-ə
    /// ehtiyac duymur, yalnız data əməliyyatları duyur).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261107080000_GoamlInstitutionBranch")]
    public partial class GoamlInstitutionBranch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstitutionNameBranchTo",
                table: "GoamlEmeliyyatlar",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstitutionNameBranchTo",
                table: "GoamlEmeliyyatlar");
        }
    }
}
