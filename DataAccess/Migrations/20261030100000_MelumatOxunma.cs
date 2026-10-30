using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Paylaşımlar — hansı işçinin hansı postu HƏQİQƏTƏN açıb oxuduğunu qeyd edir
    /// (30.10.2026, Admin-in "kim oxudu, maraq varmı" sualı üçün). `PaylasimBaxisler`-
    /// dən FƏRQLİDİR (o, "lentə son nə vaxt baxdı" — tək sətir/işçi; bu, "hansı
    /// KONKRET postu oxudu" — bir sətir/(post,işçi) cütü, unikal indekslə). Seed
    /// YOXDUR. `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261030100000_MelumatOxunma")]
    public partial class MelumatOxunmaMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MelumatOxunmalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MelumatId = table.Column<int>(type: "int", nullable: false),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    OxunmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MelumatOxunmalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MelumatOxunmalari_Melumatlar_MelumatId",
                        column: x => x.MelumatId,
                        principalTable: "Melumatlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MelumatOxunmalari_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            // Bir (Post, İşçi) cütü yalnız BİR dəfə "oxundu" sayılır — ikinci açılış
            // yeni sətir yaratmır (servis qatında "mövcuddursa yaratma" yoxlaması var,
            // bu indeks isə son sədd/qoruyucudur).
            migrationBuilder.CreateIndex(
                name: "IX_MelumatOxunmalari_MelumatId_AppUserId",
                table: "MelumatOxunmalari",
                columns: new[] { "MelumatId", "AppUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MelumatOxunmalari_AppUserId",
                table: "MelumatOxunmalari",
                column: "AppUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MelumatOxunmalari");
        }
    }
}
