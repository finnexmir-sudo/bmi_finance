using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class VezifeUniquePerDepartament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vezifeler_Ad",
                table: "Vezifeler");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1047));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1056));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1071));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1071));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1076));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1076));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1076));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1076));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1076));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1081));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1081));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1178));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1182));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1187));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1187));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1187));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 3, 15, 38, 11, 502, DateTimeKind.Local).AddTicks(1192));

            migrationBuilder.CreateIndex(
                name: "IX_Vezifeler_Ad_DepartamentId",
                table: "Vezifeler",
                columns: new[] { "Ad", "DepartamentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vezifeler_Ad_DepartamentId",
                table: "Vezifeler");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5891));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5901));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5905));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5905));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5905));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5905));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5910));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5910));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5910));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5910));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(5915));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(6017));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(6022));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(6027));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(6027));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(6027));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 1, 16, 17, 21, 61, DateTimeKind.Local).AddTicks(6031));

            migrationBuilder.CreateIndex(
                name: "IX_Vezifeler_Ad",
                table: "Vezifeler",
                column: "Ad",
                unique: true);
        }
    }
}
