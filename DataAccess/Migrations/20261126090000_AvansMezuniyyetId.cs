using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Avans → Mezuniyyet bağlantısı (26.11.2026).
    ///
    /// NİYƏ: qabaqcadan ödənilmiş məzuniyyət bir neçə ayı əhatə edəndə, ikinci
    /// (və sonrakı) ayların ƏVƏZLƏŞMƏ avansını (indiyədək mühasibin əl ilə
    /// yazdığı sətri) artıq sistem MezuniyyetAvansService ilə avtomatik yaradır.
    /// Bu sütun həmin yaradılan Avans sətrini mənbə Mezuniyyətə bağlayır ki,
    /// (a) işçinin öz avans müraciəti onu "bu ay götürülüb" kimi bloklamasın,
    /// (b) mühasibat ixracında adi avansdan AYRI hesaba yazılsın.
    ///
    /// MÖVCUD DATAYA TOXUNMUR: yalnız bir nullable sütun + FK + indeks əlavə
    /// olunur; bütün köhnə Avans sətirlərində `NULL` qalır, davranış dəyişmir.
    ///
    /// FK `NO ACTION/RESTRICT`-dir (EvezediciTesdiqler→Mezuniyyetler ilə EYNİ
    /// presedent) — Mezuniyyet sətri silinəndə (əslində heç vaxt hard-delete
    /// olunmur) ona bağlı ƏVƏZLƏŞMƏ avansının səssizcə "yetim" qalması
    /// istənmir.
    ///
    /// ⚠️ Bu layihədə migration-lar ƏL İLƏ yazılır, `.Designer.cs` yoxdur,
    /// `AppDbContextModelSnapshot.cs` YENİLƏNMİR (son bir neçə onlarla
    /// migration-un hamısında olduğu kimi) və `InsertData`/`UpdateData`
    /// İŞLƏDİLMİR — sırf sxem dəyişikliyidir, data yoxdur.
    ///
    /// ⚠️ AŞAĞIDAKI İKİ ATRİBUT MƏCBURİDİR — olmasa EF faylı migration saymır,
    /// `GetPendingMigrations()` boş qayıdır, HEÇ BİR XƏTA çıxmır, sütun isə
    /// yaranmır (CLAUDE.md-də dəfələrlə sənədləşmiş tələ).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261126090000_AvansMezuniyyetId")]
    public partial class AvansMezuniyyetId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MezuniyyetId",
                table: "Avanslar",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Avanslar_MezuniyyetId",
                table: "Avanslar",
                column: "MezuniyyetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Avanslar_Mezuniyyetler_MezuniyyetId",
                table: "Avanslar",
                column: "MezuniyyetId",
                principalTable: "Mezuniyyetler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <summary>
        /// Geri qaytarma — sütun silinir.
        ///
        /// ⚠️ DAĞIDICIDIR: hansı Avans sətrinin ƏVƏZLƏŞMƏ olduğu bilgisi İTİR
        /// (sətirlərin özü silinmir, sadəcə mənbə əlaqəsi qırılır). Yalnız
        /// həqiqətən lazımdırsa işlət.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avanslar_Mezuniyyetler_MezuniyyetId",
                table: "Avanslar");
            migrationBuilder.DropIndex(
                name: "IX_Avanslar_MezuniyyetId",
                table: "Avanslar");
            migrationBuilder.DropColumn(
                name: "MezuniyyetId",
                table: "Avanslar");
        }
    }
}
