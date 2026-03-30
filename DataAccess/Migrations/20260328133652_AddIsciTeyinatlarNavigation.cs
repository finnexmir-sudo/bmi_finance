using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsciTeyinatlarNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartamentId1",
                table: "IsciTeyinatlari",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VezifeId1",
                table: "IsciTeyinatlari",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsciTeyinatlari_DepartamentId1",
                table: "IsciTeyinatlari",
                column: "DepartamentId1");

            migrationBuilder.CreateIndex(
                name: "IX_IsciTeyinatlari_VezifeId1",
                table: "IsciTeyinatlari",
                column: "VezifeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_IsciTeyinatlari_Departament_DepartamentId1",
                table: "IsciTeyinatlari",
                column: "DepartamentId1",
                principalTable: "Departament",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IsciTeyinatlari_Vezifeler_VezifeId1",
                table: "IsciTeyinatlari",
                column: "VezifeId1",
                principalTable: "Vezifeler",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IsciTeyinatlari_Departament_DepartamentId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropForeignKey(
                name: "FK_IsciTeyinatlari_Vezifeler_VezifeId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropIndex(
                name: "IX_IsciTeyinatlari_DepartamentId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropIndex(
                name: "IX_IsciTeyinatlari_VezifeId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropColumn(
                name: "DepartamentId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropColumn(
                name: "VezifeId1",
                table: "IsciTeyinatlari");
        }
    }
}
