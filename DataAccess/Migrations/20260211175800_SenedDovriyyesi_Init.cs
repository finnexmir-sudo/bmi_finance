using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SenedDovriyyesi_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sobeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Sobeler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tagler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Tagler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SenedNovleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SobeId = table.Column<int>(type: "int", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SenedNovleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SenedNovleri_Sobeler_SobeId",
                        column: x => x.SobeId,
                        principalTable: "Sobeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Senedler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SobeId = table.Column<int>(type: "int", nullable: false),
                    SenedNovuId = table.Column<int>(type: "int", nullable: false),
                    Basliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcarSoz = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Mexfilik = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Senedler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senedler_SenedNovleri_SenedNovuId",
                        column: x => x.SenedNovuId,
                        principalTable: "SenedNovleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Senedler_Sobeler_SobeId",
                        column: x => x.SobeId,
                        principalTable: "Sobeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenedId = table.Column<int>(type: "int", nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Senedler_SenedId",
                        column: x => x.SenedId,
                        principalTable: "Senedler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SenedAccessler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenedId = table.Column<int>(type: "int", nullable: false),
                    PrincipalType = table.Column<int>(type: "int", nullable: false),
                    PrincipalId = table.Column<int>(type: "int", nullable: false),
                    Permission = table.Column<int>(type: "int", nullable: false),
                    SenedId1 = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_SenedAccessler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SenedAccessler_Senedler_SenedId",
                        column: x => x.SenedId,
                        principalTable: "Senedler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SenedAccessler_Senedler_SenedId1",
                        column: x => x.SenedId1,
                        principalTable: "Senedler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SenedFayllar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenedId = table.Column<int>(type: "int", nullable: false),
                    VersiyaNo = table.Column<int>(type: "int", nullable: false),
                    OriginalAd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoredAd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OlcuBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Yol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AktivVersiya = table.Column<bool>(type: "bit", nullable: false),
                    StorageProvider = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_SenedFayllar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SenedFayllar_Senedler_SenedId",
                        column: x => x.SenedId,
                        principalTable: "Senedler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SenedTagMaps",
                columns: table => new
                {
                    SenedId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SenedTagMaps", x => new { x.SenedId, x.TagId });
                    table.ForeignKey(
                        name: "FK_SenedTagMaps_Senedler_SenedId",
                        column: x => x.SenedId,
                        principalTable: "Senedler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SenedTagMaps_Tagler_TagId",
                        column: x => x.TagId,
                        principalTable: "Tagler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SenedId",
                table: "AuditLogs",
                column: "SenedId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedAccessler_SenedId",
                table: "SenedAccessler",
                column: "SenedId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedAccessler_SenedId1",
                table: "SenedAccessler",
                column: "SenedId1");

            migrationBuilder.CreateIndex(
                name: "IX_SenedFayllar_SenedId_VersiyaNo",
                table: "SenedFayllar",
                columns: new[] { "SenedId", "VersiyaNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senedler_SenedNovuId",
                table: "Senedler",
                column: "SenedNovuId");

            migrationBuilder.CreateIndex(
                name: "IX_Senedler_SobeId",
                table: "Senedler",
                column: "SobeId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedNovleri_SobeId",
                table: "SenedNovleri",
                column: "SobeId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedTagMaps_TagId",
                table: "SenedTagMaps",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "SenedAccessler");

            migrationBuilder.DropTable(
                name: "SenedFayllar");

            migrationBuilder.DropTable(
                name: "SenedTagMaps");

            migrationBuilder.DropTable(
                name: "Senedler");

            migrationBuilder.DropTable(
                name: "Tagler");

            migrationBuilder.DropTable(
                name: "SenedNovleri");

            migrationBuilder.DropTable(
                name: "Sobeler");
        }
    }
}
