using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Kassa — Valyuta Mübadiləsi. BMI-nin "frmExchange" / "frmExchangeTesdiq"
    /// modulunun FinNex-ə köçürülməsi (09.10.2026). Oracle-a (BMI) YAZI qadağan
    /// olduğu üçün (CLAUDE.md) bu modul tamamilə öz SQL Server bazamızda işləyir —
    /// Oracle-dakı `bmi_kassa_kurs` cədvəli ilə heç bir əlaqəsi yoxdur.
    ///
    /// Köhnə (BMI) dizaynından fərqli olaraq status/təsdiq AYRI "beyannamə"
    /// sətrinə bağlıdır (qlobal "bütün gözləyənlər" UPDATE-i yoxdur) — BMI-dəki
    /// məlum bugun (approve/reject tarixə/qrupa bağlı deyildi) qarşısı budur.
    ///
    /// Seed YOXDUR — hər iki siyahı (günlük kurslar, təsdiqedicilər) boş başlayır.
    ///
    /// ⚠️ `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md) — bu layihədə migration-lar
    /// əl ilə yazılır, `.Designer.cs` yoxdur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261009070000_Kassa")]
    public partial class Kassa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KassaKursBeyannameleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IcraciIsciId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TesdiqEdenIsciId = table.Column<int>(type: "int", nullable: true),
                    TesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImtinaSebebi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_KassaKursBeyannameleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KassaKursBeyannameleri_Isciler_IcraciIsciId",
                        column: x => x.IcraciIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KassaKursBeyannameleri_Isciler_TesdiqEdenIsciId",
                        column: x => x.TesdiqEdenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KassaKursSetirleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeyannameId = table.Column<int>(type: "int", nullable: false),
                    Valyuta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NagdAlis = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    NagdSatis = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    QeyriNagdAlis = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    QeyriNagdSatis = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
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
                    table.PrimaryKey("PK_KassaKursSetirleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KassaKursSetirleri_KassaKursBeyannameleri_BeyannameId",
                        column: x => x.BeyannameId,
                        principalTable: "KassaKursBeyannameleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KassaTesdiqEdiciler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    AktivdirFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktivdirTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_KassaTesdiqEdiciler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KassaTesdiqEdiciler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KassaKursBeyannameleri_IcraciIsciId",
                table: "KassaKursBeyannameleri",
                column: "IcraciIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KassaKursBeyannameleri_TesdiqEdenIsciId",
                table: "KassaKursBeyannameleri",
                column: "TesdiqEdenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KassaKursBeyannameleri_Tarix",
                table: "KassaKursBeyannameleri",
                column: "Tarix");

            migrationBuilder.CreateIndex(
                name: "IX_KassaKursSetirleri_BeyannameId",
                table: "KassaKursSetirleri",
                column: "BeyannameId");

            migrationBuilder.CreateIndex(
                name: "IX_KassaTesdiqEdiciler_IsciId",
                table: "KassaTesdiqEdiciler",
                column: "IsciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "KassaKursSetirleri");
            migrationBuilder.DropTable(name: "KassaKursBeyannameleri");
            migrationBuilder.DropTable(name: "KassaTesdiqEdiciler");
        }
    }
}
