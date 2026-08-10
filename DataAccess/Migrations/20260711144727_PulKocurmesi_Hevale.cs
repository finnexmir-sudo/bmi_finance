using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PulKocurmesi_Hevale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GedenHevale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HEV_NOM = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HES_NOM = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SAA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TIP_RES = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    MEBLEG = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    VAL_TIP = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TARIX = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MEN_OLKE = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CONTRAC_NOM = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    DECLAR_NOM = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ARAYIS = table.Column<short>(type: "smallint", nullable: true),
                    OLKE = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    HEV_TIP = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    GON_TIP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AL_BANK = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ICRA = table.Column<short>(type: "smallint", nullable: true),
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
                    table.PrimaryKey("PK_GedenHevale", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GelenHevale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HEV_NOM = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HES_NOM = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SAA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TIP_RES = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    MEBLEG = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    VAL_TIP = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TARIX = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MEN_OLKE = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    HEV_TIP = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    DEC = table.Column<long>(type: "bigint", nullable: true),
                    DEC_NOM = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    GEL_OLKE = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GON_TIP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AL_BANK = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ICRA = table.Column<short>(type: "smallint", nullable: true),
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
                    table.PrimaryKey("PK_GelenHevale", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GedenHevale_TARIX",
                table: "GedenHevale",
                column: "TARIX");

            migrationBuilder.CreateIndex(
                name: "IX_GelenHevale_TARIX",
                table: "GelenHevale",
                column: "TARIX");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GedenHevale");

            migrationBuilder.DropTable(
                name: "GelenHevale");
        }
    }
}
