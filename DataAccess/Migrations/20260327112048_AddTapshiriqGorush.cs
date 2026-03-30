using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTapshiriqGorush : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gorushler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bashliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Agenda = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeshkilatciIsciId = table.Column<int>(type: "int", nullable: false),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaslamaSaati = table.Column<TimeSpan>(type: "time", nullable: false),
                    BitisSaati = table.Column<TimeSpan>(type: "time", nullable: false),
                    Yer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnlineLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Qeydler = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Gorushler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gorushler_Isciler_TeshkilatciIsciId",
                        column: x => x.TeshkilatciIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tapshiriqlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bashliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tesvir = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YaradanIsciId = table.Column<int>(type: "int", nullable: false),
                    TeyinOlunanIsciId = table.Column<int>(type: "int", nullable: false),
                    SonTarix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Prioritet = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TamamlanmaFaizi = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Tapshiriqlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tapshiriqlar_Isciler_TeyinOlunanIsciId",
                        column: x => x.TeyinOlunanIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tapshiriqlar_Isciler_YaradanIsciId",
                        column: x => x.YaradanIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GorushIshtirakcilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GorushId = table.Column<int>(type: "int", nullable: false),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_GorushIshtirakcilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GorushIshtirakcilar_Gorushler_GorushId",
                        column: x => x.GorushId,
                        principalTable: "Gorushler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GorushIshtirakcilar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TapshiriqSherhler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TapshiriqId = table.Column<int>(type: "int", nullable: false),
                    MuellifIsciId = table.Column<int>(type: "int", nullable: false),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_TapshiriqSherhler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TapshiriqSherhler_Isciler_MuellifIsciId",
                        column: x => x.MuellifIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TapshiriqSherhler_Tapshiriqlar_TapshiriqId",
                        column: x => x.TapshiriqId,
                        principalTable: "Tapshiriqlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GorushIshtirakcilar_GorushId",
                table: "GorushIshtirakcilar",
                column: "GorushId");

            migrationBuilder.CreateIndex(
                name: "IX_GorushIshtirakcilar_IsciId",
                table: "GorushIshtirakcilar",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Gorushler_TeshkilatciIsciId",
                table: "Gorushler",
                column: "TeshkilatciIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Tapshiriqlar_TeyinOlunanIsciId",
                table: "Tapshiriqlar",
                column: "TeyinOlunanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Tapshiriqlar_YaradanIsciId",
                table: "Tapshiriqlar",
                column: "YaradanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_TapshiriqSherhler_MuellifIsciId",
                table: "TapshiriqSherhler",
                column: "MuellifIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_TapshiriqSherhler_TapshiriqId",
                table: "TapshiriqSherhler",
                column: "TapshiriqId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GorushIshtirakcilar");

            migrationBuilder.DropTable(
                name: "TapshiriqSherhler");

            migrationBuilder.DropTable(
                name: "Gorushler");

            migrationBuilder.DropTable(
                name: "Tapshiriqlar");
        }
    }
}
