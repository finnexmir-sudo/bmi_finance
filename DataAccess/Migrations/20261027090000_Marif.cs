using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// "Daxili Maarifləndirmə" — HR/Admin qaydadan AI ilə sual hazırlayır, təsdiqləyir,
    /// seçdiyi işçilərə göndərir (27.10.2026, pilot). İdarəetmə tam Admin əlindədir —
    /// avtomatik cədvəl/aralıqlı təkrar bu mərhələdə YOXDUR.
    ///
    /// Seed YOXDUR — boş başlayır.
    ///
    /// ⚠️ `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md) — migration-lar əl ilə yazılır,
    /// `.Designer.cs` yoxdur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027090000_Marif")]
    public partial class Marif : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarifSualSetleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Basliq = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HRQanunFaylId = table.Column<int>(type: "int", nullable: true),
                    HRDaxiliQaydaId = table.Column<int>(type: "int", nullable: true),
                    MenbeMetn = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_MarifSualSetleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarifSualSetleri_HRQanunFayllar_HRQanunFaylId",
                        column: x => x.HRQanunFaylId,
                        principalTable: "HRQanunFayllar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MarifSualSetleri_HRDaxiliQaydalar_HRDaxiliQaydaId",
                        column: x => x.HRDaxiliQaydaId,
                        principalTable: "HRDaxiliQaydalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MarifSuallar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SualSetiId = table.Column<int>(type: "int", nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    SualMetni = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    VariantA = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VariantB = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VariantC = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VariantD = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DuzgunVariant = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    DuzgunCavabMetni = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IzahMetni = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_MarifSuallar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarifSuallar_MarifSualSetleri_SualSetiId",
                        column: x => x.SualSetiId,
                        principalTable: "MarifSualSetleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarifTesyinatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SualSetiId = table.Column<int>(type: "int", nullable: false),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    GonderilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TamamlandiMi = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TamamlanmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_MarifTesyinatlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarifTesyinatlar_MarifSualSetleri_SualSetiId",
                        column: x => x.SualSetiId,
                        principalTable: "MarifSualSetleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarifTesyinatlar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarifCavablar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TesyinatId = table.Column<int>(type: "int", nullable: false),
                    SualId = table.Column<int>(type: "int", nullable: false),
                    IsciCavabi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DuzgunduMu = table.Column<bool>(type: "bit", nullable: true),
                    CavabTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_MarifCavablar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarifCavablar_MarifTesyinatlar_TesyinatId",
                        column: x => x.TesyinatId,
                        principalTable: "MarifTesyinatlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarifCavablar_MarifSuallar_SualId",
                        column: x => x.SualId,
                        principalTable: "MarifSuallar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarifSualSetleri_HRQanunFaylId",
                table: "MarifSualSetleri",
                column: "HRQanunFaylId");

            migrationBuilder.CreateIndex(
                name: "IX_MarifSualSetleri_HRDaxiliQaydaId",
                table: "MarifSualSetleri",
                column: "HRDaxiliQaydaId");

            migrationBuilder.CreateIndex(
                name: "IX_MarifSuallar_SualSetiId",
                table: "MarifSuallar",
                column: "SualSetiId");

            migrationBuilder.CreateIndex(
                name: "IX_MarifTesyinatlar_SualSetiId",
                table: "MarifTesyinatlar",
                column: "SualSetiId");

            migrationBuilder.CreateIndex(
                name: "IX_MarifTesyinatlar_IsciId",
                table: "MarifTesyinatlar",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_MarifCavablar_TesyinatId",
                table: "MarifCavablar",
                column: "TesyinatId");

            migrationBuilder.CreateIndex(
                name: "IX_MarifCavablar_SualId",
                table: "MarifCavablar",
                column: "SualId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MarifCavablar");
            migrationBuilder.DropTable(name: "MarifTesyinatlar");
            migrationBuilder.DropTable(name: "MarifSuallar");
            migrationBuilder.DropTable(name: "MarifSualSetleri");
        }
    }
}
