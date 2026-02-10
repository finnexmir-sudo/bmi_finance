using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTapsirigiYeniSaheler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OduyenBankId",
                table: "OdenisTapsiriqlari",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AlanBankId",
                table: "OdenisTapsiriqlari",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MeblegYazi",
                table: "OdenisTapsiriqlari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ElaveInformasiya",
                table: "OdenisTapsiriqlari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudceTesnifatininKodu",
                table: "OdenisTapsiriqlari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudceSeviyyesininKodu",
                table: "OdenisTapsiriqlari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OdenisTapsiriqlari_OduyenBankId",
                table: "OdenisTapsiriqlari",
                column: "OduyenBankId");

            migrationBuilder.CreateIndex(
                name: "IX_OdenisTapsiriqlari_AlanBankId",
                table: "OdenisTapsiriqlari",
                column: "AlanBankId");

            migrationBuilder.AddForeignKey(
                name: "FK_OdenisTapsiriqlari_Banklar_OduyenBankId",
                table: "OdenisTapsiriqlari",
                column: "OduyenBankId",
                principalTable: "Banklar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OdenisTapsiriqlari_Banklar_AlanBankId",
                table: "OdenisTapsiriqlari",
                column: "AlanBankId",
                principalTable: "Banklar",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OdenisTapsiriqlari_Banklar_OduyenBankId",
                table: "OdenisTapsiriqlari");

            migrationBuilder.DropForeignKey(
                name: "FK_OdenisTapsiriqlari_Banklar_AlanBankId",
                table: "OdenisTapsiriqlari");

            migrationBuilder.DropIndex(
                name: "IX_OdenisTapsiriqlari_OduyenBankId",
                table: "OdenisTapsiriqlari");

            migrationBuilder.DropIndex(
                name: "IX_OdenisTapsiriqlari_AlanBankId",
                table: "OdenisTapsiriqlari");

            migrationBuilder.DropColumn(
                name: "OduyenBankId",
                table: "OdenisTapsiriqlari");

            migrationBuilder.DropColumn(
                name: "AlanBankId",
                table: "OdenisTapsiriqlari");

            migrationBuilder.DropColumn(
                name: "MeblegYazi",
                table: "OdenisTapsiriqlari");

            migrationBuilder.DropColumn(
                name: "ElaveInformasiya",
                table: "OdenisTapsiriqlari");

            migrationBuilder.DropColumn(
                name: "BudceTesnifatininKodu",
                table: "OdenisTapsiriqlari");

            migrationBuilder.DropColumn(
                name: "BudceSeviyyesininKodu",
                table: "OdenisTapsiriqlari");
        }
    }
}
