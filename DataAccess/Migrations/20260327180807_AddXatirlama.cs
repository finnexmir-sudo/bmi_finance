using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddXatirlama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Xatirlatmalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Bashliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XatirlatmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OxunubMu = table.Column<bool>(type: "bit", nullable: false),
                    GonderiilibMi = table.Column<bool>(type: "bit", nullable: false),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    EntityTipi = table.Column<int>(type: "int", nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Xatirlatmalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Xatirlatmalar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Xatirlatmalar_IsciId",
                table: "Xatirlatmalar",
                column: "IsciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Xatirlatmalar");
        }
    }
}
