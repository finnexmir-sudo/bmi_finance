using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddZaminYoxlamalari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AsanFinanceBaxanIsciId",
                table: "KreditZaminler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AsanFinanceBaxilmaTarixi",
                table: "KreditZaminler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AsanFinanceNeticesi",
                table: "KreditZaminler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MkrBaxanIsciId",
                table: "KreditZaminler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MkrBaxilmaTarixi",
                table: "KreditZaminler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MkrNeticesi",
                table: "KreditZaminler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4298));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4298));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4303));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4303));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4303));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4303));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4303));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4307));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4307));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4307));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4307));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4307));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4307));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4337));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4346));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4346));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4346));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4351));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4370));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4395));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4400));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4404));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4404));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4409));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4409));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4409));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4414));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4414));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4419));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4419));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4419));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4143));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4157));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4157));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4157));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 11, 12, 10, 922, DateTimeKind.Local).AddTicks(4162));

            migrationBuilder.CreateIndex(
                name: "IX_KreditZaminler_AsanFinanceBaxanIsciId",
                table: "KreditZaminler",
                column: "AsanFinanceBaxanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditZaminler_MkrBaxanIsciId",
                table: "KreditZaminler",
                column: "MkrBaxanIsciId");

            migrationBuilder.AddForeignKey(
                name: "FK_KreditZaminler_Isciler_AsanFinanceBaxanIsciId",
                table: "KreditZaminler",
                column: "AsanFinanceBaxanIsciId",
                principalTable: "Isciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KreditZaminler_Isciler_MkrBaxanIsciId",
                table: "KreditZaminler",
                column: "MkrBaxanIsciId",
                principalTable: "Isciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KreditZaminler_Isciler_AsanFinanceBaxanIsciId",
                table: "KreditZaminler");

            migrationBuilder.DropForeignKey(
                name: "FK_KreditZaminler_Isciler_MkrBaxanIsciId",
                table: "KreditZaminler");

            migrationBuilder.DropIndex(
                name: "IX_KreditZaminler_AsanFinanceBaxanIsciId",
                table: "KreditZaminler");

            migrationBuilder.DropIndex(
                name: "IX_KreditZaminler_MkrBaxanIsciId",
                table: "KreditZaminler");

            migrationBuilder.DropColumn(
                name: "AsanFinanceBaxanIsciId",
                table: "KreditZaminler");

            migrationBuilder.DropColumn(
                name: "AsanFinanceBaxilmaTarixi",
                table: "KreditZaminler");

            migrationBuilder.DropColumn(
                name: "AsanFinanceNeticesi",
                table: "KreditZaminler");

            migrationBuilder.DropColumn(
                name: "MkrBaxanIsciId",
                table: "KreditZaminler");

            migrationBuilder.DropColumn(
                name: "MkrBaxilmaTarixi",
                table: "KreditZaminler");

            migrationBuilder.DropColumn(
                name: "MkrNeticesi",
                table: "KreditZaminler");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4896));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4901));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4901));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4901));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4901));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4906));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4906));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4906));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4906));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4906));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4911));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4911));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4911));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4940));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4969));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4974));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4974));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4974));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4974));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5003));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5008));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5013));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5013));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5013));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5022));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5032));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5037));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5037));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5047));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5047));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(5047));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4736));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4751));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4751));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4751));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 22, 10, 13, 48, 728, DateTimeKind.Local).AddTicks(4756));
        }
    }
}
