using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// «Pul köçürməsi → Gedən həvalə» bağı (18.08.2026).
    ///
    /// Həvalə № -ni verən ƏSAS jurnal <c>GedenHevale</c>-dir; Əməliyyat modulundakı
    /// Pul köçürməsi həmin jurnala sətir yazır və eyni nömrəni özündə də saxlayır.
    /// Bu sütun köçürmə ilə jurnal sətrini AÇIQ bağlayır ki, redaktə/silmə düzgün
    /// sətrə düşsün.
    ///
    /// Nömrə (HEV_NOM) ilə bağlamaq OLMAZDI: mövcud datada nömrə hələ unikal deyil
    /// (test «26-T-1» ↔ real BMI idxalı «26-T-1»), nömrə ilə bağlasaq test qeydinin
    /// silinməsi real jurnal sətrini silərdi.
    ///
    /// Startup-da db.Database.Migrate() ilə avtomatik tətbiq olunur.
    /// Data itkisi yoxdur — yalnız nullable sütun + indeks əlavə olunur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260818000000_GedenHevale_KocurmeId")]
    public partial class GedenHevale_KocurmeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KocurmeId",
                table: "GedenHevale",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GedenHevale_KocurmeId",
                table: "GedenHevale",
                column: "KocurmeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_GedenHevale_KocurmeId", table: "GedenHevale");
            migrationBuilder.DropColumn(name: "KocurmeId", table: "GedenHevale");
        }
    }
}
