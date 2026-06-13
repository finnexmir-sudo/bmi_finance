using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMaasNovuGelirBayraqlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DsmfyeCelb",
                table: "MaasNovleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GuzestHeddineDaxil",
                table: "MaasNovleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IssizliyeCelb",
                table: "MaasNovleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ItsseCelb",
                table: "MaasNovleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManualGelir",
                table: "MaasNovleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MezuniyyetOrtalamasinaDaxil",
                table: "MaasNovleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProvodkaHesabAcari",
                table: "MaasNovleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VergiyeCelb",
                table: "MaasNovleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(631) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(636) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(641) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(641) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(641) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(641) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(645) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(650) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(650) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(650) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(650) });

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "DsmfyeCelb", "GuzestHeddineDaxil", "IssizliyeCelb", "ItsseCelb", "ManualGelir", "MezuniyyetOrtalamasinaDaxil", "ProvodkaHesabAcari", "VergiyeCelb", "YaradilmaTarixi" },
                values: new object[] { true, true, true, true, false, true, null, true, new DateTime(2026, 6, 12, 14, 23, 59, 56, DateTimeKind.Local).AddTicks(655) });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DsmfyeCelb",
                table: "MaasNovleri");

            migrationBuilder.DropColumn(
                name: "GuzestHeddineDaxil",
                table: "MaasNovleri");

            migrationBuilder.DropColumn(
                name: "IssizliyeCelb",
                table: "MaasNovleri");

            migrationBuilder.DropColumn(
                name: "ItsseCelb",
                table: "MaasNovleri");

            migrationBuilder.DropColumn(
                name: "ManualGelir",
                table: "MaasNovleri");

            migrationBuilder.DropColumn(
                name: "MezuniyyetOrtalamasinaDaxil",
                table: "MaasNovleri");

            migrationBuilder.DropColumn(
                name: "ProvodkaHesabAcari",
                table: "MaasNovleri");

            migrationBuilder.DropColumn(
                name: "VergiyeCelb",
                table: "MaasNovleri");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2987));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2987));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2987));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2992));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2992));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2992));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2992));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2992));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2997));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2997));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2997));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2997));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2997));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2997));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3002));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3002));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3026));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3031));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3036));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3036));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3036));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3041));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3060));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3065));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3070));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3070));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3070));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3084));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3089));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3089));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3089));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3094));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3094));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(3099));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2929));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2934));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2939));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2944));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 6, 12, 8, 46, 46, 619, DateTimeKind.Local).AddTicks(2944));
        }
    }
}
