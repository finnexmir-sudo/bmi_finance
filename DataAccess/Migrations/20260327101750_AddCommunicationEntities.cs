using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bildirisler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    Bashliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Oxunub = table.Column<bool>(type: "bit", nullable: false),
                    OxunmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MezuniyyetId = table.Column<int>(type: "int", nullable: true),
                    IcazeId = table.Column<int>(type: "int", nullable: true),
                    MesajId = table.Column<int>(type: "int", nullable: true),
                    RedirectUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Bildirisler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildirisler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvezediciTesdiqler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MezuniyyetId = table.Column<int>(type: "int", nullable: false),
                    EvezediciIsciId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CavabTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_EvezediciTesdiqler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvezediciTesdiqler_Isciler_EvezediciIsciId",
                        column: x => x.EvezediciIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvezediciTesdiqler_Mezuniyyetler_MezuniyyetId",
                        column: x => x.MezuniyyetId,
                        principalTable: "Mezuniyyetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mesajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderenIsciId = table.Column<int>(type: "int", nullable: false),
                    AlanIsciId = table.Column<int>(type: "int", nullable: false),
                    Movzu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Oxunub = table.Column<bool>(type: "bit", nullable: false),
                    OxunmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CavabVerdigiMesajId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Mesajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Isciler_AlanIsciId",
                        column: x => x.AlanIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Isciler_GonderenIsciId",
                        column: x => x.GonderenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Mesajlar_CavabVerdigiMesajId",
                        column: x => x.CavabVerdigiMesajId,
                        principalTable: "Mesajlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bildirisler_IsciId",
                table: "Bildirisler",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_EvezediciTesdiqler_EvezediciIsciId",
                table: "EvezediciTesdiqler",
                column: "EvezediciIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_EvezediciTesdiqler_MezuniyyetId",
                table: "EvezediciTesdiqler",
                column: "MezuniyyetId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_AlanIsciId",
                table: "Mesajlar",
                column: "AlanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_CavabVerdigiMesajId",
                table: "Mesajlar",
                column: "CavabVerdigiMesajId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_GonderenIsciId",
                table: "Mesajlar",
                column: "GonderenIsciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bildirisler");

            migrationBuilder.DropTable(
                name: "EvezediciTesdiqler");

            migrationBuilder.DropTable(
                name: "Mesajlar");
        }
    }
}
