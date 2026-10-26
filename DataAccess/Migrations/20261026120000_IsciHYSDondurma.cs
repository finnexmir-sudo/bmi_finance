using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İşçi HYS-inin tək aylıq "dondurulması" (26.10.2026, istifadəçi tələbi).
    ///
    /// Bir IsciHYS qeydi üçün konkret (Il, Ay) sətri yazılır — maaş/vergi
    /// hesablaması həmin ayda bu HYS-i cəmə qatmır. IsciHYS-in özünün
    /// BaslamaTarixi/BitmeTarixi-nə TOXUNULMUR, sliding-pause üsulu əvəzinə.
    ///
    /// ⚠️ `InsertData` İŞLƏDİLMİR, `.Designer.cs` yoxdur (CLAUDE.md) — hər iki
    /// atribut sinfin ÖZÜNDƏDİR, Designer olmadığı üçün məcburidir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261026120000_IsciHYSDondurma")]
    public partial class IsciHYSDondurma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IsciHYSDondurmalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciHYSId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    Sebeb = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_IsciHYSDondurmalari", x => x.Id);
                });

            // Oxuma sorğularının hamısı (Il, Ay) üzrə filtrlənir — bax
            // IsciHYSDondurmaService.DondurulmusIdlerAsync.
            migrationBuilder.CreateIndex(
                name: "IX_IsciHYSDondurmalari_Il_Ay",
                table: "IsciHYSDondurmalari",
                columns: new[] { "Il", "Ay" });

            // Eyni (IsciHYSId, Il, Ay) iki dəfə dondurulmasın — servis səviyyəsində
            // yoxlanılır, indeks yalnız axtarışı sürətləndirir (unikal deyil, çünki
            // silinmiş köhnə sətirlər qala bilər).
            migrationBuilder.CreateIndex(
                name: "IX_IsciHYSDondurmalari_IsciHYSId",
                table: "IsciHYSDondurmalari",
                column: "IsciHYSId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "IsciHYSDondurmalari");
        }
    }
}
