using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Paylaşımlar — hədəf işçi seçimi + "son seçilmişlər" yaddaşı (27.10.2026).
    ///
    /// `MelumatHedefler` boşdursa post "Hamısına" sayılır (geriyə uyğun — mövcud
    /// bütün paylaşımlar hamıya görünməyə davam edir). `PaylasimSonHedefKitlesi`
    /// hər admin üçün son istifadə olunan seçimi saxlayır (sırf UX keşi).
    ///
    /// Seed YOXDUR. `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027180000_PaylasimHedef")]
    public partial class PaylasimHedef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MelumatHedefler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MelumatId = table.Column<int>(type: "int", nullable: false),
                    IsciId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MelumatHedefler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MelumatHedefler_Melumatlar_MelumatId",
                        column: x => x.MelumatId,
                        principalTable: "Melumatlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MelumatHedefler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MelumatHedefler_MelumatId",
                table: "MelumatHedefler",
                column: "MelumatId");

            migrationBuilder.CreateIndex(
                name: "IX_MelumatHedefler_IsciId",
                table: "MelumatHedefler",
                column: "IsciId");

            migrationBuilder.CreateTable(
                name: "PaylasimSonHedefKitlesi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminAppUserId = table.Column<int>(type: "int", nullable: false),
                    IsciIdlerCsv = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_PaylasimSonHedefKitlesi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaylasimSonHedefKitlesi_AspNetUsers_AdminAppUserId",
                        column: x => x.AdminAppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaylasimSonHedefKitlesi_AdminAppUserId",
                table: "PaylasimSonHedefKitlesi",
                column: "AdminAppUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PaylasimSonHedefKitlesi");
            migrationBuilder.DropTable(name: "MelumatHedefler");
        }
    }
}
