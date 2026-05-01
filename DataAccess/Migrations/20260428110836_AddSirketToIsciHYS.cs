using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSirketToIsciHYS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sirket",
                table: "IsciHYSler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9214));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9219));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9219));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9219));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9219));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9219));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9224));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9224));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9224));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9224));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9224));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9224));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9229));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9229));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9253));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9258));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9262));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9262));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9267));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9267));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9292));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9296));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9296));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9301));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9311));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9321));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9325));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9350));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9350));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9355));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9355));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9359));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9059));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9073));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9073));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9073));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 28, 15, 8, 36, 5, DateTimeKind.Local).AddTicks(9073));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sirket",
                table: "IsciHYSler");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7002));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7002));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7007));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7007));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7007));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7007));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7007));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7007));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7012));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7012));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7012));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7012));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7017));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 27, 12, 0, 0, 0, DateTimeKind.Local));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7036));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7046));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7046));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7050));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7055));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7055));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7080));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7084));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7084));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7104));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7109));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7118));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7128));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7128));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7133));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7138));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7143));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(7143));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(6827));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(6847));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(6847));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(6852));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 23, 13, 53, 15, 97, DateTimeKind.Local).AddTicks(6852));
        }
    }
}
