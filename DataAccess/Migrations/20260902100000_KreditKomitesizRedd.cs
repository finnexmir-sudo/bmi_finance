using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Kredit müraciəti — KOMİTƏSİZ RƏDD (02.09.2026).
    ///
    /// NİYƏ: BMI-dəki iş prinsipində müraciət hər zaman komitəyə getmirdi —
    /// baxan işçi «komitəyə getmədən etiraz olubsa» onu elə orada yazırdı.
    /// FinNex-də bu yol yox idi: `ReddEdilib` statusunu YALNIZ
    /// `KreditQerarService.QebulEtAsync` yaza bilirdi və o, protokol nömrəsi +
    /// ən azı bir aktiv komitə üzvünün imzasını tələb edir.
    ///
    /// NƏ ƏLAVƏ OLUNUR:
    ///   1. `KreditReddSebebleri` — səbəb açar cədvəli (7 standart sətir);
    ///   2. `KreditMuracietler`-ə 4 sütun: səbəb, əlavə qeyd, tarix, rəddi yazan işçi.
    ///
    /// MÖVCUD DATAYA TOXUNMUR: sütunların hamısı `nullable`, köhnə sətirlərdə
    /// `NULL` qalır və heç bir davranış dəyişmir. Cədvəl yenidir.
    ///
    /// FK-lar `NO ACTION`-dır: `Isciler` silinsə müraciətin rədd tarixçəsi
    /// qalmalıdır; `KreditMuracietler`-də onsuz da 3 restrict FK `Isciler`-ə
    /// gedir, kaskad yol açılsa SQL Server «multiple cascade paths» ilə
    /// migration-u bütöv sındırardı.
    ///
    /// ⚠️ SEED `migrationBuilder.Sql` İLƏDİR, `InsertData` İLƏ YOX. Bu layihədə
    /// migration-lar əl ilə yazılır və `.Designer.cs` olmur; `InsertData` sütun
    /// tiplərini `TargetModel`-dən oxuyur, model boş olduğuna görə migration
    /// SQL YARADILAN mərhələdə bütöv sınar — cədvəl də yaranmaz (CLAUDE.md).
    /// Azərbaycan hərfləri üçün `N'…'`.
    ///
    /// ⚠️ AŞAĞIDAKI İKİ ATRİBUT MƏCBURİDİR — yoxdursa EF faylı migration saymır,
    /// heç bir xəta çıxmır, sütunlar isə yaranmır və səhifə «Invalid column name»
    /// verir (01.09.2026, real hadisə).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260902100000_KreditKomitesizRedd")]
    public partial class KreditKomitesizRedd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1) Səbəb açar cədvəli ────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "KreditReddSebebleri",
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
                    table.PrimaryKey("PK_KreditReddSebebleri", x => x.Id);
                });

            // ── 2) Müraciətə rədd sahələri ───────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "ReddSebebiId",
                table: "KreditMuracietler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReddQeyd",
                table: "KreditMuracietler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReddTarixi",
                table: "KreditMuracietler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReddEdenIsciId",
                table: "KreditMuracietler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KreditMuracietler_ReddSebebiId",
                table: "KreditMuracietler",
                column: "ReddSebebiId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditMuracietler_ReddEdenIsciId",
                table: "KreditMuracietler",
                column: "ReddEdenIsciId");

            migrationBuilder.AddForeignKey(
                name: "FK_KreditMuracietler_KreditReddSebebleri_ReddSebebiId",
                table: "KreditMuracietler",
                column: "ReddSebebiId",
                principalTable: "KreditReddSebebleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_KreditMuracietler_Isciler_ReddEdenIsciId",
                table: "KreditMuracietler",
                column: "ReddEdenIsciId",
                principalTable: "Isciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            // ── 3) Standart səbəblər ─────────────────────────────────────
            // Başlanğıc siyahıdır — Admin → «Kredit rədd səbəbləri» səhifəsindən
            // artırıla bilər. `NOT EXISTS` ilə idempotentdir.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM KreditReddSebebleri)
INSERT INTO KreditReddSebebleri (Ad, Sira, Aktivdir, YaradilmaTarixi, Silinib)
VALUES
    (N'MKR mənfi — kredit tarixçəsi',      10, 1, SYSDATETIME(), 0),
    (N'Gəlir kifayət deyil',               20, 1, SYSDATETIME(), 0),
    (N'Sənədlər natamam',                  30, 1, SYSDATETIME(), 0),
    (N'Girov / təminat uyğun deyil',       40, 1, SYSDATETIME(), 0),
    (N'Şərtlərə uyğun gəlmir',             50, 1, SYSDATETIME(), 0),
    (N'Müştəri özü imtina etdi',           60, 1, SYSDATETIME(), 0),
    (N'Digər',                             99, 1, SYSDATETIME(), 0);
");
        }

        /// <summary>
        /// Geri qaytarma.
        ///
        /// ⚠️ DAĞIDICIDIR: komitəsiz rədd edilmiş müraciətlərin SƏBƏBİ, QEYDİ,
        /// TARİXİ və KİMİN etdiyi TAMAMİLƏ İTİR. Statusun özü (`ReddEdilib`)
        /// qalır, amma «niyə rədd edildi» sualının cavabı olmur. Bərpa yolu
        /// yoxdur — yalnız həqiqətən lazımdırsa işlət.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KreditMuracietler_Isciler_ReddEdenIsciId",
                table: "KreditMuracietler");
            migrationBuilder.DropForeignKey(
                name: "FK_KreditMuracietler_KreditReddSebebleri_ReddSebebiId",
                table: "KreditMuracietler");

            migrationBuilder.DropIndex(
                name: "IX_KreditMuracietler_ReddEdenIsciId",
                table: "KreditMuracietler");
            migrationBuilder.DropIndex(
                name: "IX_KreditMuracietler_ReddSebebiId",
                table: "KreditMuracietler");

            migrationBuilder.DropColumn(name: "ReddEdenIsciId", table: "KreditMuracietler");
            migrationBuilder.DropColumn(name: "ReddTarixi", table: "KreditMuracietler");
            migrationBuilder.DropColumn(name: "ReddQeyd", table: "KreditMuracietler");
            migrationBuilder.DropColumn(name: "ReddSebebiId", table: "KreditMuracietler");

            migrationBuilder.DropTable(name: "KreditReddSebebleri");
        }
    }
}
