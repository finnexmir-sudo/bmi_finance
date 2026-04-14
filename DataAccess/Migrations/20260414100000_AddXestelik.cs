using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Xəstəlik bülletənləri üçün ayrıca cədvəl + ödəniş audit cədvəli.
    /// HR sənəd əsasında yazır, sistem avtomatik şirkət payını hesablayır.
    /// </summary>
    public partial class AddXestelik : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Xestelikler cədvəli ────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Xestelikler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsGunSayi = table.Column<int>(type: "int", nullable: false),
                    BulletenNomresi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MualiceMuessisesi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HrId = table.Column<int>(type: "int", nullable: true),
                    HrTesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Xestelikler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Xestelikler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Xestelikler_IsciId_BaslamaTarixi",
                table: "Xestelikler",
                columns: new[] { "IsciId", "BaslamaTarixi" });

            // ── XestelikOdenisleri cədvəli ─────────────────────────
            migrationBuilder.CreateTable(
                name: "XestelikOdenisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    XestelikId = table.Column<int>(type: "int", nullable: false),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    BirGunluk = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SirketGunSayi = table.Column<int>(type: "int", nullable: false),
                    DsmfGunSayi = table.Column<int>(type: "int", nullable: false),
                    SirketOdenis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DsmfOdenis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaasId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_XestelikOdenisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XestelikOdenisleri_Xestelikler_XestelikId",
                        column: x => x.XestelikId,
                        principalTable: "Xestelikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_XestelikOdenisleri_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_XestelikOdenisleri_Maaslar_MaasId",
                        column: x => x.MaasId,
                        principalTable: "Maaslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XestelikOdenisleri_XestelikId",
                table: "XestelikOdenisleri",
                column: "XestelikId");

            migrationBuilder.CreateIndex(
                name: "IX_XestelikOdenisleri_IsciId_Il_Ay",
                table: "XestelikOdenisleri",
                columns: new[] { "IsciId", "Il", "Ay" });

            migrationBuilder.CreateIndex(
                name: "IX_XestelikOdenisleri_MaasId",
                table: "XestelikOdenisleri",
                column: "MaasId");

            // Yeni MaasNovu: Xəstəlik Ödənişi (gəlir tipində)
            migrationBuilder.InsertData(
                table: "MaasNovleri",
                columns: new[] { "Id", "Ad", "Aktivdir", "SilenIcraciId", "Silinib", "SilinmeTarixi", "Tip", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[] { 13, "Xəstəlik Ödənişi", true, null, false, null, 1 /* Gelir */, null, new DateTime(2026, 1, 1), null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XestelikOdenisleri");

            migrationBuilder.DropTable(
                name: "Xestelikler");

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13);
        }
    }
}
