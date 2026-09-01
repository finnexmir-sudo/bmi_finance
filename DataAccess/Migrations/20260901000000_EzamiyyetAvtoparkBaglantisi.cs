using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Ezamiyyət ↔ Avtopark bağlantısı (01.09.2026).
    ///
    /// NİYƏ: işçi ezamiyyətə maşınla gedəndə eyni məlumatı iki dəfə yazırdı —
    /// bir dəfə ezamiyyət müraciətində, bir dəfə Avtoparkda. İstifadəçi qərarı:
    /// «bir forma, bir təsdiq» — maşın ezamiyyət formasında seçilir, rəhbər
    /// ezamiyyəti təsdiqləyəndə maşın müraciəti AVTOMATİK yaranır və artıq
    /// təsdiqlənmiş olur (birbaşa kassaya düşür).
    ///
    /// İKİ SÜTUN, İKİ FƏRQLİ MƏNA — qarışdırma:
    ///   EzamiyyetMuracietler.MasinId          → işçinin İSTƏDİYİ maşın (təsdiqə qədər)
    ///   MasinMuracietler.EzamiyyetMuracietId  → bu maşın müraciəti hansı ezamiyyətdəndir
    ///
    /// MÖVCUD DATAYA TOXUNMUR: nə INSERT, nə UPDATE, nə DELETE. Yalnız iki
    /// nullable sütun + FK + indeks əlavə olunur; bütün köhnə sətirlərdə
    /// hər ikisi `NULL` qalır və heç bir davranış dəyişmir.
    ///
    /// FK-lar `NO ACTION`-dır: `MasinMuracietler`-də onsuz da 5 restrict FK var
    /// (4-ü `Isciler`-ə), kaskad yol açılsa SQL Server «multiple cascade paths»
    /// ilə migration-u bütöv sındırardı.
    ///
    /// ⚠️ Bu layihədə migration-lar ƏL İLƏ yazılır və `.Designer.cs` yoxdur —
    /// ona görə `InsertData`/`UpdateData`/`DeleteData` İŞLƏDİLMİR (CLAUDE.md).
    /// Burada onlara ehtiyac da yoxdur: sırf sxem dəyişikliyidir.
    /// </summary>
    public partial class EzamiyyetAvtoparkBaglantisi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1) Ezamiyyət → istənilən maşın ────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "MasinId",
                table: "EzamiyyetMuracietler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EzamiyyetMuracietler_MasinId",
                table: "EzamiyyetMuracietler",
                column: "MasinId");

            migrationBuilder.AddForeignKey(
                name: "FK_EzamiyyetMuracietler_Masinlar_MasinId",
                table: "EzamiyyetMuracietler",
                column: "MasinId",
                principalTable: "Masinlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            // ── 2) Maşın müraciəti → mənbə ezamiyyət ──────────────────────
            migrationBuilder.AddColumn<int>(
                name: "EzamiyyetMuracietId",
                table: "MasinMuracietler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasinMuracietler_EzamiyyetMuracietId",
                table: "MasinMuracietler",
                column: "EzamiyyetMuracietId");

            migrationBuilder.AddForeignKey(
                name: "FK_MasinMuracietler_EzamiyyetMuracietler_EzamiyyetMuracietId",
                table: "MasinMuracietler",
                column: "EzamiyyetMuracietId",
                principalTable: "EzamiyyetMuracietler",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <summary>
        /// Geri qaytarma — sütunlar silinir.
        ///
        /// ⚠️ DAĞIDICIDIR: ezamiyyətlə maşın müraciəti arasındakı bağ İTİR.
        /// Maşın müraciətlərinin özü qalır (sətirlər silinmir), sadəcə hansı
        /// ezamiyyətdən gəldiyi bilinməz olur. Ezamiyyətdəki maşın seçimi də
        /// silinir. Bərpa yolu yoxdur — yalnız həqiqətən lazımdırsa işlət.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MasinMuracietler_EzamiyyetMuracietler_EzamiyyetMuracietId",
                table: "MasinMuracietler");
            migrationBuilder.DropIndex(
                name: "IX_MasinMuracietler_EzamiyyetMuracietId",
                table: "MasinMuracietler");
            migrationBuilder.DropColumn(
                name: "EzamiyyetMuracietId",
                table: "MasinMuracietler");

            migrationBuilder.DropForeignKey(
                name: "FK_EzamiyyetMuracietler_Masinlar_MasinId",
                table: "EzamiyyetMuracietler");
            migrationBuilder.DropIndex(
                name: "IX_EzamiyyetMuracietler_MasinId",
                table: "EzamiyyetMuracietler");
            migrationBuilder.DropColumn(
                name: "MasinId",
                table: "EzamiyyetMuracietler");
        }
    }
}
