using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPidCedveller : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PidMehkemeIsleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sira = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BorcluAd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KreditNovu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KreditHesabi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subkod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MehkemeyeVerilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MehkemeSenedi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QetnameTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MusteriId = table.Column<int>(type: "int", nullable: true),
                    KreditId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_PidMehkemeIsleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PidMehkemeIclaslari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PidMehkemeIsiId = table.Column<int>(type: "int", nullable: false),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Saat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Netice = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PidMehkemeIclaslari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PidMehkemeIclaslari_PidMehkemeIsleri_PidMehkemeIsiId",
                        column: x => x.PidMehkemeIsiId,
                        principalTable: "PidMehkemeIsleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7409));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7409));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7414));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7414));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7414));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7414));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7419));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7419));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7419));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7419));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7424));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7424));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7424));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7424));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7424));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7428));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7458));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7462));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7462));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7467));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7467));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7472));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7491));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7501));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7501));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7506));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7506));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7511));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7535));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7535));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7540));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7545));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7545));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7550));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7346));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7351));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7356));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7361));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 27, 59, 412, DateTimeKind.Local).AddTicks(7361));

            migrationBuilder.CreateIndex(
                name: "IX_PidMehkemeIclaslari_PidMehkemeIsiId",
                table: "PidMehkemeIclaslari",
                column: "PidMehkemeIsiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PidMehkemeIclaslari");

            migrationBuilder.DropTable(
                name: "PidMehkemeIsleri");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3611));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3616));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3616));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3616));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3621));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3621));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3621));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3621));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3626));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3626));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3626));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3626));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3630));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3664));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3664));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3664));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3703));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3708));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3708));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3718));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3718));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3718));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3747));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3756));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3761));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3761));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3766));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3766));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3771));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3771));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3776));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3781));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3781));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3786));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3543));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3548));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3553));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3562));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 11, 14, 48, 25, 356, DateTimeKind.Local).AddTicks(3567));
        }
    }
}
