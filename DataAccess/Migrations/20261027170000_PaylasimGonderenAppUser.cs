using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Paylaşımlar — "Göndərən" artıq Isci YOX, AppUser-dir (27.10.2026, düzəliş).
    ///
    /// Kök səbəb: Admin hesabının `AppUser.IsciId`-si boşdur (işçi profilinə bağlı
    /// deyil) — `Melumat.GonderenIsciId` NOT NULL FK olduğu üçün Admin HEÇ BİR
    /// paylaşım yarada bilmirdi ("İşçi profiliniz tapılmadı", hər publish cəhdində).
    /// AppUser isə hər autentifikasiya olunmuş sessiyada mütləq mövcuddur.
    ///
    /// `Melumatlar` cədvəli bu an boşdur (ilk paylaşım hələ yaradıla bilmədiyi üçün) —
    /// data itkisi riski yoxdur, sütun sadəcə əvəz olunur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027170000_PaylasimGonderenAppUser")]
    public partial class PaylasimGonderenAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Melumatlar_Isciler_GonderenIsciId",
                table: "Melumatlar");

            migrationBuilder.DropIndex(
                name: "IX_Melumatlar_GonderenIsciId",
                table: "Melumatlar");

            migrationBuilder.DropColumn(
                name: "GonderenIsciId",
                table: "Melumatlar");

            migrationBuilder.AddColumn<int>(
                name: "GonderenAppUserId",
                table: "Melumatlar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Melumatlar_GonderenAppUserId",
                table: "Melumatlar",
                column: "GonderenAppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Melumatlar_AspNetUsers_GonderenAppUserId",
                table: "Melumatlar",
                column: "GonderenAppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Melumatlar_AspNetUsers_GonderenAppUserId",
                table: "Melumatlar");

            migrationBuilder.DropIndex(
                name: "IX_Melumatlar_GonderenAppUserId",
                table: "Melumatlar");

            migrationBuilder.DropColumn(
                name: "GonderenAppUserId",
                table: "Melumatlar");

            migrationBuilder.AddColumn<int>(
                name: "GonderenIsciId",
                table: "Melumatlar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Melumatlar_GonderenIsciId",
                table: "Melumatlar",
                column: "GonderenIsciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Melumatlar_Isciler_GonderenIsciId",
                table: "Melumatlar",
                column: "GonderenIsciId",
                principalTable: "Isciler",
                principalColumn: "Id");
        }
    }
}
