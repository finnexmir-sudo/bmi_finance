using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Qara Jetonun kaskadını (müsbət jetondan FIFO, "36 Saat Hüququ"ndan çıxılma,
    /// borc yaranması/ödənməsi) addım-addım qeyd edən jurnal — 08.10.2026, "Qara
    /// Jetonu ləğv etsəm kəsilən geri qayıdır?" tələbinə görə. Bax entity sənədi
    /// (<c>QaraJetonKesinti</c>) — ləğv ediləndə bu jurnal oxunub dəqiq geri qaytarılır.
    ///
    /// `QaraJetonBorclari.Status`-a yeni dəyər (LegvEdildi=4) əlavə olunub — bu, sadəcə
    /// enum dəyəridir, sütun tipi dəyişmir, migration lazım deyil.
    ///
    /// ⚠️ `InsertData` İŞLƏDİLMİR, `.Designer.cs` yoxdur (CLAUDE.md) — hər iki atribut
    /// sinfin ÖZÜNDƏDİR.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261008090000_QaraJetonKesintiTarixcesi")]
    public partial class QaraJetonKesintiTarixcesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QaraJetonKesintileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QaraJetonId = table.Column<int>(type: "int", nullable: false),
                    Tur = table.Column<int>(type: "int", nullable: false),
                    HedefJetonId = table.Column<int>(type: "int", nullable: true),
                    BorcId = table.Column<int>(type: "int", nullable: true),
                    Miqdar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GeriQaytarilib = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_QaraJetonKesintileri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QaraJetonKesintileri_QaraJetonId",
                table: "QaraJetonKesintileri",
                column: "QaraJetonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QaraJetonKesintileri");
        }
    }
}
