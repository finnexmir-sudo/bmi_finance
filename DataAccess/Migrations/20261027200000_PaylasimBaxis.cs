using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Paylaşımlar — "yeni paylaşım" sidebar nişanı üçün son-baxış izləyicisi
    /// (27.10.2026). Hər AppUser üçün tək sətir, `PaylasimSonHedefKitlesi` ilə EYNİ
    /// naxış. Seed YOXDUR. `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027200000_PaylasimBaxis")]
    public partial class PaylasimBaxisMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaylasimBaxisler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    SonBaxisTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_PaylasimBaxisler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaylasimBaxisler_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaylasimBaxisler_AppUserId",
                table: "PaylasimBaxisler",
                column: "AppUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PaylasimBaxisler");
        }
    }
}
