using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InsertIH07VM9821Seed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(403));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(403));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(408));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(408));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(408));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(408));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(408));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(413));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(413));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(413));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(413));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(413));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(413));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(418));

            migrationBuilder.InsertData(
                table: "MaasNovleri",
                columns: new[] { "Id", "Ad", "Aktivdir", "SilenIcraciId", "Silinib", "SilinmeTarixi", "Tip", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[,]
                {
                    { 17, "IH-07 Əlavə Təminat", true, null, false, null, 1, null, new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(418), null, null },
                    { 18, "VM 98.2.1 Gəlirləri", true, null, false, null, 1, null, new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(418), null, null }
                });

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(447));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(452));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(452));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(457));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(457));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(457));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(510));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(520));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(529));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(544));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(544));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(544));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(549));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(554));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(554));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(558));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(558));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(563));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(234));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(248));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(248));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(253));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 39, 55, 799, DateTimeKind.Local).AddTicks(253));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5838));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5838));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5838));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5838));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5843));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5843));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5843));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5843));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5843));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5848));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5848));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5848));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5848));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5848));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5896));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5901));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5906));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5906));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5906));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5911));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5930));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5935));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5940));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5940));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5940));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5945));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5945));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5949));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5949));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5949));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5954));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5954));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5663));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5678));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5678));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5678));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 30, 15, 24, 21, 73, DateTimeKind.Local).AddTicks(5678));
        }
    }
}
