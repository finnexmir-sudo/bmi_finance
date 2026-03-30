using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMezuniyyetNavigationProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_HrId",
                table: "Mezuniyyetler",
                column: "HrId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_RehberId",
                table: "Mezuniyyetler",
                column: "RehberId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_SobeReisiId",
                table: "Mezuniyyetler",
                column: "SobeReisiId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mezuniyyetler_Isciler_HrId",
                table: "Mezuniyyetler",
                column: "HrId",
                principalTable: "Isciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mezuniyyetler_Isciler_RehberId",
                table: "Mezuniyyetler",
                column: "RehberId",
                principalTable: "Isciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mezuniyyetler_Isciler_SobeReisiId",
                table: "Mezuniyyetler",
                column: "SobeReisiId",
                principalTable: "Isciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mezuniyyetler_Isciler_HrId",
                table: "Mezuniyyetler");

            migrationBuilder.DropForeignKey(
                name: "FK_Mezuniyyetler_Isciler_RehberId",
                table: "Mezuniyyetler");

            migrationBuilder.DropForeignKey(
                name: "FK_Mezuniyyetler_Isciler_SobeReisiId",
                table: "Mezuniyyetler");

            migrationBuilder.DropIndex(
                name: "IX_Mezuniyyetler_HrId",
                table: "Mezuniyyetler");

            migrationBuilder.DropIndex(
                name: "IX_Mezuniyyetler_RehberId",
                table: "Mezuniyyetler");

            migrationBuilder.DropIndex(
                name: "IX_Mezuniyyetler_SobeReisiId",
                table: "Mezuniyyetler");
        }
    }
}
