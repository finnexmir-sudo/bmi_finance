using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Avans.OdenisTarixi (26.11.2026) — "Təsdiqləndi" ilə "Pul hazırdır"
    /// anlarını ayırmaq üçün.
    ///
    /// NİYƏ: mühasib müraciəti təsdiqləyəndən (Status=Tesdiqlenib) sonra
    /// işçiyə DƏRHAL "ödənildi" bildirişi gedirdi — amma real pul ancaq
    /// mühasib BMI-də pravodkanı vurub "ulduzları" götürəndən SONRA çıxarıla
    /// bilir. Bu sütun + "Bildiriş göndər" düyməsi (AvansController.
    /// BildirisGonder/BildirisGonderToplu) bu ikinci, HƏQİQİ anı qeyd edir;
    /// bax CLAUDE.md.
    ///
    /// MÖVCUD DATAYA TOXUNMUR: yalnız bir nullable sütun əlavə olunur,
    /// bütün köhnə sətirlərdə NULL qalır.
    ///
    /// ⚠️ Bu layihədə migration-lar ƏL İLƏ yazılır, `.Designer.cs` yoxdur,
    /// `InsertData`/`UpdateData` İŞLƏDİLMİR. Aşağıdakı iki atribut MƏCBURİDİR
    /// — olmasa EF faylı migration saymır, sütun yaranmır, heç bir xəta
    /// çıxmır (CLAUDE.md-də dəfələrlə sənədləşmiş tələ).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261126100000_AvansOdenisTarixi")]
    public partial class AvansOdenisTarixi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OdenisTarixi",
                table: "Avanslar",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OdenisTarixi",
                table: "Avanslar");
        }
    }
}
