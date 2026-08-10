using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Emeliyyat_Hevale_Kocurme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelebeKocurme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HevaleNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Adi = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Passport = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BmiFilial = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RefNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UniAd = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AlanBank = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TelebeKursu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    XH = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    Kurs = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    Komissiya = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Hes35025 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Hes45023 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Hes45011 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Hes67013 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Icra = table.Column<short>(type: "smallint", nullable: true),
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
                    table.PrimaryKey("PK_TelebeKocurme", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelebeKocurme_Tarix",
                table: "TelebeKocurme",
                column: "Tarix");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelebeKocurme");
        }
    }
}
