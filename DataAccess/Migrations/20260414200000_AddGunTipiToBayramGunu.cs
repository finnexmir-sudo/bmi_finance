using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// BayramGunu cədvəlinə Tip sütunu əlavə edilir.
    /// Default: Bayram (1) — istirahət günü.
    /// Alternativ: IsGunu (2) — şənbə/bazarı iş günü kimi işarələmək üçün.
    /// </summary>
    public partial class AddGunTipiToBayramGunu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tip",
                table: "BayramGunleri",
                type: "int",
                nullable: false,
                defaultValue: 1); // Bayram
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tip",
                table: "BayramGunleri");
        }
    }
}
