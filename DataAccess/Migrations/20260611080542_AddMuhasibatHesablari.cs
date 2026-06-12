using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMuhasibatHesablari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MuhasibatHesablari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Acar = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HesabNomresi = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_MuhasibatHesablari", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2319));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2319));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2319));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2319));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2324));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2324));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2324));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2328));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2333));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2333));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2338));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2338));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2338));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2338));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2338));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2338));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2362));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2367));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2372));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2392));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2396));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2396));

            migrationBuilder.InsertData(
                table: "MuhasibatHesablari",
                columns: new[] { "Id", "Acar", "Ad", "Aktiv", "HesabNomresi", "SilenIcraciId", "Silinib", "SilinmeTarixi", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[] { 1, "AvansDebet", "Avans — Debet hesabı (proводka)", true, "25052000010000300000", null, false, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null });

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2421));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2430));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2430));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2435));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2435));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2440));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2440));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2440));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2445));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2450));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2450));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2450));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2270));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2275));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2275));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2275));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 12, 5, 41, 839, DateTimeKind.Local).AddTicks(2280));

            migrationBuilder.CreateIndex(
                name: "IX_MuhasibatHesablari_Acar",
                table: "MuhasibatHesablari",
                column: "Acar",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MuhasibatHesablari");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9934));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9934));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9939));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9939));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9939));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9939));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9939));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9944));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9949));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9954));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9954));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9954));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9959));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9959));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9959));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9959));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9983));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9988));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9988));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9993));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9993));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(2));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(22));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(27));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(31));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(31));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(31));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(36));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(36));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(41));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(41));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(46));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(46));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 133, DateTimeKind.Local).AddTicks(51));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9862));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9867));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9867));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9867));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 10, 42, 39, 132, DateTimeKind.Local).AddTicks(9891));
        }
    }
}
