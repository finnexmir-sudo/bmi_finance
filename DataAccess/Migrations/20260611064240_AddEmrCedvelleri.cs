using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEmrCedvelleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Emrler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    Nomre = table.Column<int>(type: "int", nullable: false),
                    Suffiks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ElaqeliEntityId = table.Column<int>(type: "int", nullable: true),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenedYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenedAd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerenIsciId = table.Column<int>(type: "int", nullable: true),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emrler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmrSayghaclari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    SonNomre = table.Column<int>(type: "int", nullable: false),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmrSayghaclari", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Emrler_Nov_Il_Nomre",
                table: "Emrler",
                columns: new[] { "Nov", "Il", "Nomre" });

            migrationBuilder.CreateIndex(
                name: "IX_EmrSayghaclari_Nov_Il",
                table: "EmrSayghaclari",
                columns: new[] { "Nov", "Il" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Emrler");
            migrationBuilder.DropTable(name: "EmrSayghaclari");
        }
    }
}
