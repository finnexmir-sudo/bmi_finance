using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Qara Jeton — dəyərli kəsinti mexanizmi (07.10.2026, istifadəçi qərarı).
    ///
    /// Qara Jeton verilən AN: (1) müsbət (mükafat) jeton balansından FIFO
    /// çıxılır, (2) qalıbsa işçinin cari ilin "36 Saat Hüququ" jetonunun
    /// QalanSaat-ından çıxılır, (3) yenə qalıbsa QaraJetonBorcu "Gözləyir"
    /// statusunda yazılır və növbəti müsbət jeton veriləndə ödənilir. İl
    /// sonunda ödənməmiş borc bağışlanır (Status=MuddetiBitib).
    ///
    /// "36 Saat Hüququ {il}" JetonTeyinati tipi BURADA SEED EDİLMİR —
    /// JetonService.EnsureIllikHuquqTeyinatiAsync onu lazım olduqda tətbiq
    /// kodunda özü yaradır, ona görə bu migration-da yalnız YENİ SÜTUN/CƏDVƏL
    /// var, INSERT yoxdur.
    ///
    /// ⚠️ `InsertData` İŞLƏDİLMİR, `.Designer.cs` yoxdur (CLAUDE.md) — hər iki
    /// atribut sinfin ÖZÜNDƏDİR, Designer olmadığı üçün məcburidir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261007100000_QaraJeton")]
    public partial class QaraJeton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Sistemli", table: "JetonTeyinatlari",
                type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QaraJetonId", table: "IsciJetonlari",
                type: "int", nullable: true);

            migrationBuilder.CreateTable(
                name: "QaraJetonBorclari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    QalanSaat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QaraJetonId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
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
                    table.PrimaryKey("PK_QaraJetonBorclari", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QaraJetonBorclari_IsciId_Status",
                table: "QaraJetonBorclari",
                columns: new[] { "IsciId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QaraJetonBorclari");
            migrationBuilder.DropColumn(name: "QaraJetonId", table: "IsciJetonlari");
            migrationBuilder.DropColumn(name: "Sistemli", table: "JetonTeyinatlari");
        }
    }
}
