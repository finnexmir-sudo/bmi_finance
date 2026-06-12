using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPidIcraSaheleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PidMehkemeIclaslari");

            migrationBuilder.DropTable(
                name: "PidMehkemeIsleri");

            migrationBuilder.AddColumn<string>(
                name: "AdinaSorgu",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DogumTarixi",
                table: "MehkemeIsleri",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DypSorguTarixi",
                table: "MehkemeIsleri",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmekHaqqiMelumati",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmlakaHebs",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IcraMemuru",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IcraQeyd",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IcraSonIsler",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IsYeri",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KreditHesabi",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QalanBorc",
                table: "MehkemeIsleri",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QetnameTarixi",
                table: "MehkemeIsleri",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Qeydiyyati",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonOdenisTarixi",
                table: "MehkemeIsleri",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stop",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subkod",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Zamin",
                table: "MehkemeIsleri",
                type: "nvarchar(max)",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdinaSorgu",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "DogumTarixi",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "DypSorguTarixi",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "EmekHaqqiMelumati",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "EmlakaHebs",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "IcraMemuru",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "IcraQeyd",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "IcraSonIsler",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "IsYeri",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "KreditHesabi",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "QalanBorc",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "QetnameTarixi",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "Qeydiyyati",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "SonOdenisTarixi",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "Stop",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "Subkod",
                table: "MehkemeIsleri");

            migrationBuilder.DropColumn(
                name: "Zamin",
                table: "MehkemeIsleri");

            migrationBuilder.CreateTable(
                name: "PidMehkemeIsleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorcluAd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KreditHesabi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KreditId = table.Column<int>(type: "int", nullable: true),
                    KreditNovu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MehkemeSenedi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MehkemeyeVerilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MusteriId = table.Column<int>(type: "int", nullable: true),
                    QetnameTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sira = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subkod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true)
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
                    Netice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Saat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true)
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
    }
}
