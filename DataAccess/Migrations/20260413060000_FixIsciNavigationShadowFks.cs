using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Fluent config-da Isci ↔ IsciMaliye / Maas / IsciMaasTarixcesi
    /// navigation property-ləri göstərilməmişdi, bu səbəbdən EF shadow
    /// IsciId1 FK-lərini yaratmışdı. Nəticədə .Include(x => x.Maliye)
    /// həmişə null qaytarırdı (çünki IsciId1 heç zaman doldurulmurdu),
    /// bu da "Əsas Maaş" sütununun boş görünməsinə səbəb olurdu.
    ///
    /// Bu migration orphan IsciId1 sütunlarını və onların index/FK-lərini silir.
    /// </summary>
    public partial class FixIsciNavigationShadowFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── IsciMaliyeleri.IsciId1 ──────────────────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_IsciMaliyeleri_Isciler_IsciId1",
                table: "IsciMaliyeleri");

            migrationBuilder.DropIndex(
                name: "IX_IsciMaliyeleri_IsciId1",
                table: "IsciMaliyeleri");

            migrationBuilder.DropColumn(
                name: "IsciId1",
                table: "IsciMaliyeleri");

            // ── Maaslar.IsciId1 ─────────────────────────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_Maaslar_Isciler_IsciId1",
                table: "Maaslar");

            migrationBuilder.DropIndex(
                name: "IX_Maaslar_IsciId1",
                table: "Maaslar");

            migrationBuilder.DropColumn(
                name: "IsciId1",
                table: "Maaslar");

            // ── IsciMaasTarixceleri.IsciId1 ─────────────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_IsciMaasTarixceleri_Isciler_IsciId1",
                table: "IsciMaasTarixceleri");

            migrationBuilder.DropIndex(
                name: "IX_IsciMaasTarixceleri_IsciId1",
                table: "IsciMaasTarixceleri");

            migrationBuilder.DropColumn(
                name: "IsciId1",
                table: "IsciMaasTarixceleri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── IsciMaliyeleri.IsciId1 ──────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "IsciId1",
                table: "IsciMaliyeleri",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsciMaliyeleri_IsciId1",
                table: "IsciMaliyeleri",
                column: "IsciId1",
                unique: true,
                filter: "[IsciId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_IsciMaliyeleri_Isciler_IsciId1",
                table: "IsciMaliyeleri",
                column: "IsciId1",
                principalTable: "Isciler",
                principalColumn: "Id");

            // ── Maaslar.IsciId1 ─────────────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "IsciId1",
                table: "Maaslar",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Maaslar_IsciId1",
                table: "Maaslar",
                column: "IsciId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Maaslar_Isciler_IsciId1",
                table: "Maaslar",
                column: "IsciId1",
                principalTable: "Isciler",
                principalColumn: "Id");

            // ── IsciMaasTarixceleri.IsciId1 ─────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "IsciId1",
                table: "IsciMaasTarixceleri",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsciMaasTarixceleri_IsciId1",
                table: "IsciMaasTarixceleri",
                column: "IsciId1");

            migrationBuilder.AddForeignKey(
                name: "FK_IsciMaasTarixceleri_Isciler_IsciId1",
                table: "IsciMaasTarixceleri",
                column: "IsciId1",
                principalTable: "Isciler",
                principalColumn: "Id");
        }
    }
}
