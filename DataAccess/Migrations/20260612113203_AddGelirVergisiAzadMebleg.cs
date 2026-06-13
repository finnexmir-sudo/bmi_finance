using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddGelirVergisiAzadMebleg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GelirVergisiAzadMebleg",
                table: "MaasNovleri",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(734) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(738) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(738) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(738) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(738) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(743) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(743) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(743) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(743) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(743) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(748) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(748) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(748) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(748) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(748) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "GelirVergisiAzadMebleg", "YaradilmaTarixi" },
                values: new object[] { 0m, new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(753) });

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(782));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(787));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(787));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(792));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(792));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(797));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(840));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(845));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(850));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(850));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(855));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(855));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(855));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(860));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(860));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(864));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(869));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(869));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(661));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(666));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(671));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(685));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 15, 32, 2, 214, DateTimeKind.Local).AddTicks(685));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GelirVergisiAzadMebleg",
                table: "MaasNovleri");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(631));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(636));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(641));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(641));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(641));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(641));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(650));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(650));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(650));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(650));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(655));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(689));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(699));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(699));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(699));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(704));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(704));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(733));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(738));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(742));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(742));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(747));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(747));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(752));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(752));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(757));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(762));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(767));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(767));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(480));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(490));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(504));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(514));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(514));
        }
    }
}
