using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Heyat Yigim Sigortasi (HYS) — isci uzre teyin olunmus ayliq HYS odenisileri.
    /// Hesablamada HYS meblegi vergi+DSMF bazasindan cixilir, ITSS/Issizlik bazasinda qalir.
    /// Isegoturen 15% elave odeyir. Bazaya dusen gelir = maas + isegoturen HYS payi.
    ///
    /// MaasParametrNovu-ya iki yeni sira elave olunur:
    ///   10 = HysIsegoturenFaizi   (defolt 15%)
    ///   11 = HysMaxMaasFaizi      (defolt 50%)
    /// </summary>
    public partial class AddIsciHYS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IsciHYSler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_IsciHYSler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciHYSler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IsciHYSler_IsciId",
                table: "IsciHYSler",
                column: "IsciId");

            // HYS parametrleri seed — MaasParametrleri cedveline elave et
            // 10 = HysIsegoturenFaizi (15%), 11 = HysMaxMaasFaizi (50%)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM MaasParametrleri WHERE Nov = 10 AND Silinib = 0)
                    INSERT INTO MaasParametrleri (Nov, Tip, Deyer, Aktivdir, BaslamaTarixi, YaradilmaTarixi, Silinib)
                    VALUES (10, 1, 15, 1, '2026-01-01', GETUTCDATE(), 0);

                IF NOT EXISTS (SELECT 1 FROM MaasParametrleri WHERE Nov = 11 AND Silinib = 0)
                    INSERT INTO MaasParametrleri (Nov, Tip, Deyer, Aktivdir, BaslamaTarixi, YaradilmaTarixi, Silinib)
                    VALUES (11, 1, 50, 1, '2026-01-01', GETUTCDATE(), 0);
            ");

            // MaasNovleri seed — HYS ucun iki yeni nov:
            //   "HYS (Isci)"         — Tutulma tipi (iscinin brut-undan cixilir, NET-i azaldir)
            //   "HYS (Isegoturen)"   — IsegoturenXerci tipi (sirketin 15%-i, informativ)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Ad = N'HYS (İşçi)')
                    INSERT INTO MaasNovleri (Ad, Tip, Aktivdir, YaradilmaTarixi, Silinib)
                    VALUES (N'HYS (İşçi)', 2, 1, GETUTCDATE(), 0);

                IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Ad = N'HYS (İşəgötürən)')
                    INSERT INTO MaasNovleri (Ad, Tip, Aktivdir, YaradilmaTarixi, Silinib)
                    VALUES (N'HYS (İşəgötürən)', 3, 1, GETUTCDATE(), 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IsciHYSler");

            // HYS parametrlerini sil
            migrationBuilder.Sql("DELETE FROM MaasParametrleri WHERE Nov IN (10, 11)");
        }
    }
}
