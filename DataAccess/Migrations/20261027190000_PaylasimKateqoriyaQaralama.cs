using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Paylaşımlar — Kateqoriya (açar-cədvəl, bir neçə başlanğıc dəyərlə seed olunur)
    /// + Qaralama/Paylaş axını (27.10.2026). Seed `migrationBuilder.Sql(INSERT)` ilədir
    /// — `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md, Designer.cs yoxdur).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027190000_PaylasimKateqoriyaQaralama")]
    public partial class PaylasimKateqoriyaQaralama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaylasimKateqoriyalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    YaradilmaTarixi = table.Column<System.DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<System.DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SilinmeTarixi = table.Column<System.DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaylasimKateqoriyalari", x => x.Id);
                });

            migrationBuilder.Sql(@"
                INSERT INTO PaylasimKateqoriyalari (Ad, Aktivdir, YaradilmaTarixi, Silinib)
                VALUES
                    (N'Bankçılıq', 1, GETDATE(), 0),
                    (N'Maliyyə tarixi', 1, GETDATE(), 0),
                    (N'Təhlükəsizlik', 1, GETDATE(), 0),
                    (N'Digər', 1, GETDATE(), 0);
            ");

            migrationBuilder.AddColumn<int>(
                name: "KateqoriyaId",
                table: "Melumatlar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Qaralamadir",
                table: "Melumatlar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Melumatlar_KateqoriyaId",
                table: "Melumatlar",
                column: "KateqoriyaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Melumatlar_PaylasimKateqoriyalari_KateqoriyaId",
                table: "Melumatlar",
                column: "KateqoriyaId",
                principalTable: "PaylasimKateqoriyalari",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Melumatlar_PaylasimKateqoriyalari_KateqoriyaId",
                table: "Melumatlar");

            migrationBuilder.DropIndex(
                name: "IX_Melumatlar_KateqoriyaId",
                table: "Melumatlar");

            migrationBuilder.DropColumn(
                name: "KateqoriyaId",
                table: "Melumatlar");

            migrationBuilder.DropColumn(
                name: "Qaralamadir",
                table: "Melumatlar");

            migrationBuilder.DropTable(name: "PaylasimKateqoriyalari");
        }
    }
}
