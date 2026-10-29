using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Mühasibat — "Maliyyə Hesabatları" səhifəsinin ★ Seçilmişlər cədvəli (29.10.2026).
    /// Seed YOXDUR. `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md, Designer.cs yoxdur).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261029090000_HesabatSecilmis")]
    public partial class HesabatSecilmisMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HesabatSecilmisler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    HesabatKod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    YaradilmaTarixi = table.Column<System.DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<System.DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SilinmeTarixi = table.Column<System.DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HesabatSecilmisler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HesabatSecilmisler_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "UX_HesabatSecilmisler_AppUser_Kod",
                table: "HesabatSecilmisler",
                columns: new[] { "AppUserId", "HesabatKod" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HesabatSecilmisler");
        }
    }
}
