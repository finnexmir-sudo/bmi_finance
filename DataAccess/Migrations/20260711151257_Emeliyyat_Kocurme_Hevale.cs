using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Emeliyyat_Kocurme_Hevale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kocurme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Novu = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    HevaleNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GonderenAd = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    GonderenSoyad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    GonderenAtaAd = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    GonderenPassport = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    GonderenTelefon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AlanAd = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AlanSoyad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AlanAtaAd = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AlanPassport = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AlanTelefon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RialCbar = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValyutaCbar = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    IranRial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MedaxilValyuta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    KocurulenValyuta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Secim = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BankAd = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Filial = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AlanHesab = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Elave = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Meqsed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Kocurme", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kocurme_Novu_Tarix",
                table: "Kocurme",
                columns: new[] { "Novu", "Tarix" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kocurme");
        }
    }
}
