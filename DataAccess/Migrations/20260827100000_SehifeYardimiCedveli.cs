using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Səhifə təlimatları — «?» düyməsinin mətn mənbəyi (27.08.2026).
    ///
    /// NİYƏ: «bu səhifə necə işləyir?» sualı hər dəfə admin-ə gəlirdi.
    /// İzah səhifənin öz üstündə olsun; mətn BAZADA saxlanılır ki, admin
    /// onu deploy gözləmədən redaktə edə bilsin.
    ///
    /// MÖVCUD HEÇ NƏYƏ TOXUNMUR — yalnız yeni cədvəl yaradılır. Nə maaş,
    /// nə məzuniyyət, nə də başqa modulun datası dəyişmir.
    ///
    /// ⚠️ `InsertData` İSTİFADƏ EDİLMİR — bu layihədə migration-lar əl ilə
    /// yazılır və `.Designer.cs` faylı olmur; `InsertData` TargetModel-ə baxdığı
    /// üçün SQL yaradılan mərhələdə sınır və `CreateTable` də icra olunmur
    /// (19.08.2026 Avtopark hadisəsi). Başlanğıc sətirlər lazım olsa
    /// `migrationBuilder.Sql(@"INSERT …")` ilə, Azərbaycan hərfləri üçün N'…'.
    ///
    /// Startup-da `db.Database.Migrate()` ilə avtomatik tətbiq olunur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260827100000_SehifeYardimiCedveli")]
    public partial class SehifeYardimiCedveli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SehifeYardimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Acar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Basliq = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Modul = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Xulase = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    // Təlimat HTML-dir və uzun ola bilər — limit QOYULMUR.
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hazirlanir = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    YalnizAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BaxisSayi = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_SehifeYardimlari", x => x.Id);
                });

            // Açar marşrutdan qurulur ({area}/{controller}/{action}) və hər səhifə
            // üçün BİR sətir olmalıdır — unikal indeks bunu bazada təmin edir.
            migrationBuilder.CreateIndex(
                name: "IX_SehifeYardimlari_Acar",
                table: "SehifeYardimlari",
                column: "Acar",
                unique: true);

            // Slug çatda paylaşılan qısa ünvandır (/Yardim/mezuniyyet-muracieti) —
            // iki səhifə eyni ünvanı daşıya bilməz.
            migrationBuilder.CreateIndex(
                name: "IX_SehifeYardimlari_Slug",
                table: "SehifeYardimlari",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cədvəl tamamilə yenidir — silinməsi başqa modula toxunmur.
            // DİQQƏT: yazılmış bütün təlimat mətnləri İTİR.
            migrationBuilder.DropTable(name: "SehifeYardimlari");
        }
    }
}
