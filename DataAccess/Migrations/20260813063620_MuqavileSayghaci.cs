using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MuqavileSayghaci : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DİQQƏT: bu migration YALNIZ MuqavileSayghaci cədvəlini yaradır.
            // Add-Migration snapshot ilə baza arasındakı köhnə fərqi (XaricMektub,
            // DaxilMektub, GedenHevale, GelenHevale, Kocurme, TelebeKocurme,
            // IsciUsaqlari + 11 sütun + 8 indeks) da bura yığmışdı — onlar bazada
            // ARTIQ MÖVCUDDUR (əl ilə SQL ilə yaradılmışdı, 13.08.2026 yoxlanıldı).
            // Silinməsəydi CREATE TABLE xəta verər və bütün migration geri alınardı.
            // Snapshot toxunulmayıb — model artıq düzgündür, sonrakı migration-lar
            // təmiz fərq verəcək.
            migrationBuilder.CreateTable(
                name: "MuqavileSayghaci",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Novu = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    SonNomre = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MuqavileSayghaci", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuqavileSayghaci_Novu_Il",
                table: "MuqavileSayghaci",
                columns: new[] { "Novu", "Il" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MuqavileSayghaci");
        }

        
    }
}
