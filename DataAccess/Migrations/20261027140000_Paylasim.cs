using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// "Paylaşımlar" — Admin-in xaricdən aldığı mənalı məlumatı rich-text (şəkilli)
    /// formatda işçilərlə bölüşdüyü bilik lenti (27.10.2026, pilot). Elan-dan AYRI
    /// modul — Elan əməliyyat xəbərdarlığıdır, bu sadəcə oxuma məqsədlidir.
    ///
    /// Seed YOXDUR — boş başlayır.
    ///
    /// ⚠️ `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md) — migration-lar əl ilə yazılır,
    /// `.Designer.cs` yoxdur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027140000_Paylasim")]
    public partial class Paylasim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Melumatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Basliq = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MetnHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GonderenIsciId = table.Column<int>(type: "int", nullable: false),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_Melumatlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Melumatlar_Isciler_GonderenIsciId",
                        column: x => x.GonderenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Melumatlar_GonderenIsciId",
                table: "Melumatlar",
                column: "GonderenIsciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Melumatlar");
        }
    }
}
