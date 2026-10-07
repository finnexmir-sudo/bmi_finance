using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// "İmtiyazlarım" — HR tərəfindən işçiyə verilmiş fərdi istisna/güzəşt
    /// qeydləri (07.10.2026, istifadəçi tələbi). YALNIZ GÖSTƏRİŞDİR — heç bir
    /// hesablamaya/Davamiyyət statusuna təsir etmir (bax entity-dəki qeyd).
    ///
    /// Seed YOXDUR — siyahı boş başlayır, HR işçi profilində əlavə edir.
    ///
    /// ⚠️ `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md) — bu layihədə migration-lar
    /// əl ilə yazılır, `.Designer.cs` yoxdur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261007120000_IsciImtiyaz")]
    public partial class IsciImtiyaz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IsciImtiyazlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeftaGunu = table.Column<int>(type: "int", nullable: true),
                    BaslamaSaati = table.Column<TimeSpan>(type: "time", nullable: true),
                    BitisSaati = table.Column<TimeSpan>(type: "time", nullable: true),
                    Aciqlama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_IsciImtiyazlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciImtiyazlar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IsciImtiyazlar_IsciId",
                table: "IsciImtiyazlar",
                column: "IsciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "IsciImtiyazlar");
        }
    }
}
