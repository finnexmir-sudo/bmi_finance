using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Avans (emek haqqi avansi) sistemi.
    /// Isci her ay 1 defe avans iste biler (maasin max 30%-i).
    /// Muhasib tesdiq edir, ay sonu maas hesablananda NET-den cixilir.
    /// </summary>
    public partial class AddAvans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Avanslar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Sebeb = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MuhasibId = table.Column<int>(type: "int", nullable: true),
                    TesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImtinaSebebi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avanslar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Avanslar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Avanslar_Isciler_MuhasibId",
                        column: x => x.MuhasibId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Avanslar_IsciId",
                table: "Avanslar",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Avanslar_MuhasibId",
                table: "Avanslar",
                column: "MuhasibId");

            migrationBuilder.CreateIndex(
                name: "IX_Avanslar_IsciId_Il_Ay",
                table: "Avanslar",
                columns: new[] { "IsciId", "Il", "Ay" });

            // Avans parametrleri seed
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM MaasParametrleri WHERE Nov = 12 AND Silinib = 0)
                    INSERT INTO MaasParametrleri (Nov, Tip, Deyer, Aktivdir, BaslamaTarixi, YaradilmaTarixi, Silinib)
                    VALUES (12, 1, 30, 1, '2026-01-01', GETUTCDATE(), 0);

                IF NOT EXISTS (SELECT 1 FROM MaasParametrleri WHERE Nov = 13 AND Silinib = 0)
                    INSERT INTO MaasParametrleri (Nov, Tip, Deyer, Aktivdir, BaslamaTarixi, YaradilmaTarixi, Silinib)
                    VALUES (13, 2, 15, 1, '2026-01-01', GETUTCDATE(), 0);

                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Ad = N'Avans Kəsintisi')
                    INSERT INTO MaasNovleri (Ad, Tip, Aktivdir, YaradilmaTarixi, Silinib)
                    VALUES (N'Avans Kəsintisi', 2, 1, GETUTCDATE(), 0);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Avanslar");
            migrationBuilder.Sql("DELETE FROM MaasParametrleri WHERE Nov IN (12, 13)");
            migrationBuilder.Sql("DELETE FROM MaasNovleri WHERE Ad = N'Avans Kəsintisi'");
        }
    }
}
