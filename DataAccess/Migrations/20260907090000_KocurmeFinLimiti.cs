using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// 20 000 USD aylıq köçürmə limiti (07.09.2026).
    ///
    /// · `Kocurme` — GonderenFin, UsdEkvivalent, UsdKursu, SenedNovuId, LimitQeydi
    /// · `KocurmeSenedNovleri` — əsaslandırma sənədlərinin açar siyahısı
    ///
    /// ⚠️ `InsertData` İŞLƏDİLMİR — bu layihədə migration-lar əl ilə yazılır və
    /// `.Designer.cs` olmur; `InsertData` TargetModel-ə baxdığı üçün migration-u
    /// BÜTÖV sındırır (CLAUDE.md). Burada seed onsuz da yoxdur: sənəd növləri
    /// siyahısı boş başlayır, operator formadan əlavə edir.
    ///
    /// ⚠️ Hər iki atribut sinfin ÖZÜNDƏDİR — Designer olmadığı üçün məcburidir.
    /// Olmasa EF faylı MIGRATION SAYMIR və heç bir xəta vermədən keçir
    /// (sonra səhifə «Invalid column name 'GonderenFin'» verir).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260907090000_KocurmeFinLimiti")]
    public partial class KocurmeFinLimiti : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Sənəd növləri açar cədvəli ─────────────────────────────
            migrationBuilder.CreateTable(
                name: "KocurmeSenedNovleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KocurmeSenedNovleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KocurmeSenedNovleri_Aktivdir_Sira",
                table: "KocurmeSenedNovleri",
                columns: new[] { "Aktivdir", "Sira" });

            // ── 2. Kocurme sütunları ──────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "GonderenFin", table: "Kocurme",
                type: "nvarchar(10)", maxLength: 10, nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UsdEkvivalent", table: "Kocurme",
                type: "decimal(18,2)", precision: 18, scale: 2, nullable: true);

            // MB kursu — 10 onluq (RialCbar/ValyutaCbar ilə eyni qayda).
            migrationBuilder.AddColumn<decimal>(
                name: "UsdKursu", table: "Kocurme",
                type: "decimal(18,10)", precision: 18, scale: 10, nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SenedNovuId", table: "Kocurme",
                type: "int", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LimitQeydi", table: "Kocurme",
                type: "nvarchar(500)", maxLength: 500, nullable: true);

            // Aylıq cəm sorğusu həmişə bu üçlüklə filtrlənir.
            migrationBuilder.CreateIndex(
                name: "IX_Kocurme_Novu_GonderenFin_Tarix",
                table: "Kocurme",
                columns: new[] { "Novu", "GonderenFin", "Tarix" });

            // NoAction — sənəd növü silinmir, deaktiv edilir. Cascade olsaydı
            // növün silinməsi köçürmə qeydlərini də aparardı.
            migrationBuilder.AddForeignKey(
                name: "FK_Kocurme_KocurmeSenedNovleri_SenedNovuId",
                table: "Kocurme",
                column: "SenedNovuId",
                principalTable: "KocurmeSenedNovleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kocurme_KocurmeSenedNovleri_SenedNovuId", table: "Kocurme");

            migrationBuilder.DropIndex(
                name: "IX_Kocurme_Novu_GonderenFin_Tarix", table: "Kocurme");

            migrationBuilder.DropColumn(name: "LimitQeydi", table: "Kocurme");
            migrationBuilder.DropColumn(name: "SenedNovuId", table: "Kocurme");
            migrationBuilder.DropColumn(name: "UsdKursu", table: "Kocurme");
            migrationBuilder.DropColumn(name: "UsdEkvivalent", table: "Kocurme");
            migrationBuilder.DropColumn(name: "GonderenFin", table: "Kocurme");

            migrationBuilder.DropTable(name: "KocurmeSenedNovleri");
        }
    }
}
