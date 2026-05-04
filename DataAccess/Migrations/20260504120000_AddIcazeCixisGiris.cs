using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260504120000_AddIcazeCixisGiris")]
    public partial class AddIcazeCixisGiris : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IcazeCixisGirisler cədvəli
            migrationBuilder.CreateTable(
                name: "IcazeCixisGirisler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IcazeId = table.Column<int>(type: "int", nullable: false),
                    CixisVaxt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QayidisVaxt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Birdefelik = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IcazeCixisGirisler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IcazeCixisGirisler_Icazeler_IcazeId",
                        column: x => x.IcazeId,
                        principalTable: "Icazeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IcazeCixisGirisler_IcazeId",
                table: "IcazeCixisGirisler",
                column: "IcazeId",
                unique: true);

            // Icazeler cədvəlinə Birdefelik sütunu
            migrationBuilder.AddColumn<bool>(
                name: "Birdefelik",
                table: "Icazeler",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "IcazeCixisGirisler");

            migrationBuilder.DropColumn(
                name: "Birdefelik",
                table: "Icazeler");
        }
    }
}
