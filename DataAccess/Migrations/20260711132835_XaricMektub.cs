using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class XaricMektub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XaricMektub",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KOD = table.Column<long>(type: "bigint", nullable: true),
                    QEY_NOM = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    GON_YER = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TARIX = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QISA_MEZ = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ICRACI = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DUBL = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MEKTUB_METN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IL = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_XaricMektub", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XaricMektub_IL_KOD",
                table: "XaricMektub",
                columns: new[] { "IL", "KOD" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XaricMektub");
        }
    }
}
