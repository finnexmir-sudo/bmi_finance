using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DaxilMektub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DaxilMektub",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NOM = table.Column<int>(type: "int", nullable: true),
                    NOM1 = table.Column<int>(type: "int", nullable: true),
                    DAX_TARIX = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IDARE_ADI = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GON_TARIX = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DAX_NOM = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    MEK_UNVAN = table.Column<int>(type: "int", nullable: true),
                    IL = table.Column<int>(type: "int", nullable: true),
                    MEZMUN = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FaylYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_DaxilMektub", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DaxilMektub_IL_NOM1",
                table: "DaxilMektub",
                columns: new[] { "IL", "NOM1" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DaxilMektub");
        }
    }
}
