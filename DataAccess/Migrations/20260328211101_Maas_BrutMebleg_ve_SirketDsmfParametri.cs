using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Maas_BrutMebleg_ve_SirketDsmfParametri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BrutMebleg",
                table: "Maaslar",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6729));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6750));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6751));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6752));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6754));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6755));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6756));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6757));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6759));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6760));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6761));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6978));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6986));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6988));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6991));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6992));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 29, 1, 11, 0, 487, DateTimeKind.Local).AddTicks(6994));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrutMebleg",
                table: "Maaslar");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7362));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7377));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7378));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7380));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7381));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7382));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7383));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7385));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7386));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7387));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7388));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7582));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7589));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7592));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7595));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7596));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7598));
        }
    }
}
