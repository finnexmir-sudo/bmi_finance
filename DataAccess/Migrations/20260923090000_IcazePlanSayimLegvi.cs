using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İcazə — «plan üzrə sayım»ın HR tərəfindən ləğvi (23.09.2026).
    ///
    /// Kontekst: `IcazeCixisGiris.FaktikiSaat` cihazdan ölçülə bilmirsə (punch
    /// yoxdur, ya da qayıdış çıxışdan əvvəldir — icazə pəncərəsindən kənar bir
    /// punch-un səhvən bağlanması), balans (Dashboard, İcazə İndex) bunu PLAN
    /// qədər sayır (istifadəçi qərarı: «işçi icazə yazıb getməyibsə, bu onun
    /// problemidir, sistem plan qədər hesablasın»). Amma işçinin ÜZÜRLÜ səbəbi
    /// ola bilər — HR bu KONKRET qeydin plan-sayımını ləğv edə bilsin deyə
    /// `IcazeCixisGiris`-ə 4 sahə əlavə olunur. İcazənin ÖZÜNƏ TOXUNULMUR.
    ///
    /// ⚠️ `InsertData` İŞLƏDİLMİR — bu layihədə migration-lar əl ilə yazılır və
    /// `.Designer.cs` olmur; `InsertData` TargetModel-ə baxdığı üçün migration-u
    /// BÜTÖV sındırır (CLAUDE.md). Burada seed yoxdur, sadəcə sütun əlavəsidir.
    ///
    /// ⚠️ Hər iki atribut sinfin ÖZÜNDƏDİR — Designer olmadığı üçün məcburidir.
    /// Olmasa EF faylı MIGRATION SAYMIR, heç bir xəta vermədən keçir (sonra
    /// səhifə «Invalid column name 'PlanSayimiLegvEdildi'» verir).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260923090000_IcazePlanSayimLegvi")]
    public partial class IcazePlanSayimLegvi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PlanSayimiLegvEdildi", table: "IcazeCixisGirisler",
                type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlanSayimiLegvSebebi", table: "IcazeCixisGirisler",
                type: "nvarchar(500)", maxLength: 500, nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanSayimiLegvTarixi", table: "IcazeCixisGirisler",
                type: "datetime2", nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanSayimiLegvEdenIsciId", table: "IcazeCixisGirisler",
                type: "int", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PlanSayimiLegvEdenIsciId", table: "IcazeCixisGirisler");
            migrationBuilder.DropColumn(name: "PlanSayimiLegvTarixi", table: "IcazeCixisGirisler");
            migrationBuilder.DropColumn(name: "PlanSayimiLegvSebebi", table: "IcazeCixisGirisler");
            migrationBuilder.DropColumn(name: "PlanSayimiLegvEdildi", table: "IcazeCixisGirisler");
        }
    }
}
