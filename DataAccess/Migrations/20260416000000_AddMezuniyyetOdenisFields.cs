using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Məzuniyyət cədvəlinə “Qabaqcadan ödəniş” feature üçün yeni sahələr:
    ///   OdenisTipi     — 1 = AySonuOdenis (default, mövcud davranış),
    ///                    2 = QabaqcadanOdenis
    ///   OdenisStatus   — 0 = TetbiqEdilmir (default), 1 = Gozleyir, 2 = Odenilib
    ///   OdenenMebleg   — HR təsdiq anında hesablanan məbləğ (Mühasib düzəldə bilər)
    ///   OdenilmeTarixi — Mühasib təsdiq tarixi
    ///   OdeyenMuhasibId— ödənişi təsdiqləyən Mühasibin Isci FK-sı
    /// </summary>
    public partial class AddMezuniyyetOdenisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OdenisTipi",
                table: "Mezuniyyetler",
                type: "int",
                nullable: false,
                defaultValue: 1); // AySonuOdenis

            migrationBuilder.AddColumn<int>(
                name: "OdenisStatus",
                table: "Mezuniyyetler",
                type: "int",
                nullable: false,
                defaultValue: 0); // TetbiqEdilmir

            migrationBuilder.AddColumn<decimal>(
                name: "OdenenMebleg",
                table: "Mezuniyyetler",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<System.DateTime>(
                name: "OdenilmeTarixi",
                table: "Mezuniyyetler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OdeyenMuhasibId",
                table: "Mezuniyyetler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_OdeyenMuhasibId",
                table: "Mezuniyyetler",
                column: "OdeyenMuhasibId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mezuniyyetler_Isciler_OdeyenMuhasibId",
                table: "Mezuniyyetler",
                column: "OdeyenMuhasibId",
                principalTable: "Isciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mezuniyyetler_Isciler_OdeyenMuhasibId",
                table: "Mezuniyyetler");

            migrationBuilder.DropIndex(
                name: "IX_Mezuniyyetler_OdeyenMuhasibId",
                table: "Mezuniyyetler");

            migrationBuilder.DropColumn(
                name: "OdeyenMuhasibId",
                table: "Mezuniyyetler");

            migrationBuilder.DropColumn(
                name: "OdenilmeTarixi",
                table: "Mezuniyyetler");

            migrationBuilder.DropColumn(
                name: "OdenenMebleg",
                table: "Mezuniyyetler");

            migrationBuilder.DropColumn(
                name: "OdenisStatus",
                table: "Mezuniyyetler");

            migrationBuilder.DropColumn(
                name: "OdenisTipi",
                table: "Mezuniyyetler");
        }
    }
}
