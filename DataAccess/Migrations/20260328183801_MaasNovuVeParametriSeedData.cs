using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MaasNovuVeParametriSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IsciTeyinatlari_Departament_DepartamentId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropForeignKey(
                name: "FK_IsciTeyinatlari_Vezifeler_VezifeId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropIndex(
                name: "IX_IsciTeyinatlari_DepartamentId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropIndex(
                name: "IX_IsciTeyinatlari_VezifeId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropColumn(
                name: "DepartamentId1",
                table: "IsciTeyinatlari");

            migrationBuilder.DropColumn(
                name: "VezifeId1",
                table: "IsciTeyinatlari");

            migrationBuilder.InsertData(
                table: "MaasNovleri",
                columns: new[] { "Id", "Ad", "Aktivdir", "SilenIcraciId", "Silinib", "SilinmeTarixi", "Tip", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[,]
                {
                    { 1, "Əsas Əməkhaqqı", true, null, false, null, 1, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7362), null, null },
                    { 2, "Bonus/Mükafat", true, null, false, null, 1, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7377), null, null },
                    { 3, "Məzuniyyət Ödənişi", true, null, false, null, 1, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7378), null, null },
                    { 4, "Davamiyyət Kəsintisi", true, null, false, null, 2, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7380), null, null },
                    { 5, "Gecikdirmə Cəriməsi", true, null, false, null, 2, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7381), null, null },
                    { 6, "Gəlir Vergisi", true, null, false, null, 2, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7382), null, null },
                    { 7, "DSMF (İşçi)", true, null, false, null, 2, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7383), null, null },
                    { 8, "İşsizlik Sığortası (İşçi)", true, null, false, null, 2, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7385), null, null },
                    { 9, "İTSS", true, null, false, null, 2, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7386), null, null },
                    { 10, "DSMF (İşəgötürən)", true, null, false, null, 3, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7387), null, null },
                    { 11, "İşsizlik Sığortası (İşəgötürən)", true, null, false, null, 3, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7388), null, null }
                });

            migrationBuilder.InsertData(
                table: "MaasParametrleri",
                columns: new[] { "Id", "Aciqlama", "Aktivdir", "BaslamaTarixi", "BitmeTarixi", "Deyer", "Nov", "SilenIcraciId", "Silinib", "SilinmeTarixi", "Tip", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[,]
                {
                    { 1, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 14m, 1, null, false, null, 1, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7582), null, null },
                    { 2, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 3m, 2, null, false, null, 1, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7589), null, null },
                    { 3, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 0.5m, 3, null, false, null, 1, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7592), null, null },
                    { 4, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2m, 4, null, false, null, 1, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7595), null, null },
                    { 5, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 345m, 5, null, false, null, 2, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7596), null, null },
                    { 6, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 200m, 6, null, false, null, 2, null, new DateTime(2026, 3, 28, 22, 38, 0, 907, DateTimeKind.Local).AddTicks(7598), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "DepartamentId1",
                table: "IsciTeyinatlari",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VezifeId1",
                table: "IsciTeyinatlari",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsciTeyinatlari_DepartamentId1",
                table: "IsciTeyinatlari",
                column: "DepartamentId1");

            migrationBuilder.CreateIndex(
                name: "IX_IsciTeyinatlari_VezifeId1",
                table: "IsciTeyinatlari",
                column: "VezifeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_IsciTeyinatlari_Departament_DepartamentId1",
                table: "IsciTeyinatlari",
                column: "DepartamentId1",
                principalTable: "Departament",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IsciTeyinatlari_Vezifeler_VezifeId1",
                table: "IsciTeyinatlari",
                column: "VezifeId1",
                principalTable: "Vezifeler",
                principalColumn: "Id");
        }
    }
}
