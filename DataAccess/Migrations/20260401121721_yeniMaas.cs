using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class yeniMaas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MezuniyyetBalanslari_IsciId",
                table: "MezuniyyetBalanslari");

            migrationBuilder.DropIndex(
                name: "IX_MezuniyyetBalanslari_IsciId_Il",
                table: "MezuniyyetBalanslari");

            migrationBuilder.AddColumn<int>(
                name: "Nov",
                table: "MezuniyyetBalanslari",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
                name: "IX_MezuniyyetBalanslari_IsciId_Il_Nov",
                table: "MezuniyyetBalanslari",
                columns: new[] { "IsciId", "Il", "Nov" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MezuniyyetBalanslari_IsciId_Il_Nov",
                table: "MezuniyyetBalanslari");

            migrationBuilder.DropColumn(
                name: "Nov",
                table: "MezuniyyetBalanslari");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8197));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8211));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8211));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8211));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8211));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8211));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8216));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8216));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8216));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8216));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8216));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8342));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8347));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8347));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8352));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8352));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 3, 31, 9, 20, 0, 952, DateTimeKind.Local).AddTicks(8352));

            migrationBuilder.CreateIndex(
                name: "IX_MezuniyyetBalanslari_IsciId",
                table: "MezuniyyetBalanslari",
                column: "IsciId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MezuniyyetBalanslari_IsciId_Il",
                table: "MezuniyyetBalanslari",
                columns: new[] { "IsciId", "Il" },
                unique: true);
        }
    }
}
