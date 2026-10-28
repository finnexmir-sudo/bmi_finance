using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Kassa — günlük kurs beyannaməsi TƏSDİQLƏNDİKDƏ bildiriş alacaq işçilərin
    /// sabit siyahısı (28.10.2026, istifadəçi tələbi: "kurslari rehber
    /// tesdiqledikden sonra secdiyim iscilere bildiris gelmeyini isteyirem").
    /// `KassaTesdiqEdiciler` cədvəli ilə STRUKTUR olaraq eynidir, amma AYRI
    /// konsepti ifadə edir — bu siyahıdakı işçilər TƏSDİQ ETMİR, yalnız
    /// nəticədən XƏBƏRDAR OLUR.
    ///
    /// Seed YOXDUR — siyahı boş başlayır, Admin əl ilə doldurur.
    ///
    /// ⚠️ `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md) — bu layihədə migration-lar
    /// əl ilə yazılır, `.Designer.cs` yoxdur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261028090000_KassaBildirisAlani")]
    public partial class KassaBildirisAlani : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KassaBildirisAlanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    AktivdirFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktivdirTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_KassaBildirisAlanlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KassaBildirisAlanlari_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KassaBildirisAlanlari_IsciId",
                table: "KassaBildirisAlanlari",
                column: "IsciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "KassaBildirisAlanlari");
        }
    }
}
