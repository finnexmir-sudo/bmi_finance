using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddGuzestNovuVeIsciMezuniyyetFaktlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EngelliUsaqVar",
                table: "Isciler",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TekValideyn",
                table: "Isciler",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UsaqSayi",
                table: "Isciler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Novu",
                table: "Guzestler",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EngelliUsaqVar",
                table: "Isciler");

            migrationBuilder.DropColumn(
                name: "TekValideyn",
                table: "Isciler");

            migrationBuilder.DropColumn(
                name: "UsaqSayi",
                table: "Isciler");

            migrationBuilder.DropColumn(
                name: "Novu",
                table: "Guzestler");
        }
    }
}
