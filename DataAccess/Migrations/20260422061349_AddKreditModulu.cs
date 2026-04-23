using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddKreditModulu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AsanFinanceBaxanIsciId",
                table: "KreditMuracietler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AsanFinanceBaxilmaTarixi",
                table: "KreditMuracietler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AsanFinanceNeticesi",
                table: "KreditMuracietler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IlkinBaxisQeyd",
                table: "KreditMuracietler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MkrBaxanIsciId",
                table: "KreditMuracietler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MkrBaxilmaTarixi",
                table: "KreditMuracietler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MkrNeticesi",
                table: "KreditMuracietler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KomiteUzvleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    AktivdirFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktivdirTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KomiteUzvleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KomiteUzvleri_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KreditBaxanIsciler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    AktivdirFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktivdirTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KreditBaxanIsciler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KreditBaxanIsciler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KreditQerarlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KreditMuracietId = table.Column<int>(type: "int", nullable: false),
                    Qerar = table.Column<int>(type: "int", nullable: false),
                    ProtokolNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QerarTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProtokolFaylAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProtokolFaylYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TesdiqMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TesdiqMuddet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaizDerecesi = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Teminat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DaxilEdenIsciId = table.Column<int>(type: "int", nullable: false),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KreditQerarlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KreditQerarlar_Isciler_DaxilEdenIsciId",
                        column: x => x.DaxilEdenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KreditQerarlar_KreditMuracietler_KreditMuracietId",
                        column: x => x.KreditMuracietId,
                        principalTable: "KreditMuracietler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KreditRandevular",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KreditMuracietId = table.Column<int>(type: "int", nullable: false),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MuddetDeqiqe = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GelmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TeyinEdenIsciId = table.Column<int>(type: "int", nullable: false),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KreditRandevular", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KreditRandevular_Isciler_TeyinEdenIsciId",
                        column: x => x.TeyinEdenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KreditRandevular_KreditMuracietler_KreditMuracietId",
                        column: x => x.KreditMuracietId,
                        principalTable: "KreditMuracietler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KreditSmsLoglar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KreditMuracietId = table.Column<int>(type: "int", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sablon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GonderilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GatewayCavabi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Xeta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GonderenIsciId = table.Column<int>(type: "int", nullable: false),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KreditSmsLoglar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KreditSmsLoglar_Isciler_GonderenIsciId",
                        column: x => x.GonderenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KreditSmsLoglar_KreditMuracietler_KreditMuracietId",
                        column: x => x.KreditMuracietId,
                        principalTable: "KreditMuracietler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KreditZaminler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KreditMuracietId = table.Column<int>(type: "int", nullable: false),
                    AdSoyadAtaAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FIN = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsYeri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmekHaqqi = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Unvan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KreditZaminler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KreditZaminler_KreditMuracietler_KreditMuracietId",
                        column: x => x.KreditMuracietId,
                        principalTable: "KreditMuracietler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KreditQerarImzalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KreditQerarId = table.Column<int>(type: "int", nullable: false),
                    KomiteUzvuId = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KreditQerarImzalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KreditQerarImzalar_KomiteUzvleri_KomiteUzvuId",
                        column: x => x.KomiteUzvuId,
                        principalTable: "KomiteUzvleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KreditQerarImzalar_KreditQerarlar_KreditQerarId",
                        column: x => x.KreditQerarId,
                        principalTable: "KreditQerarlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_KreditMuracietler_AsanFinanceBaxanIsciId",
                table: "KreditMuracietler",
                column: "AsanFinanceBaxanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditMuracietler_MkrBaxanIsciId",
                table: "KreditMuracietler",
                column: "MkrBaxanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KomiteUzvleri_IsciId",
                table: "KomiteUzvleri",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditBaxanIsciler_IsciId",
                table: "KreditBaxanIsciler",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditQerarImzalar_KomiteUzvuId",
                table: "KreditQerarImzalar",
                column: "KomiteUzvuId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditQerarImzalar_KreditQerarId",
                table: "KreditQerarImzalar",
                column: "KreditQerarId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditQerarlar_DaxilEdenIsciId",
                table: "KreditQerarlar",
                column: "DaxilEdenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditQerarlar_KreditMuracietId",
                table: "KreditQerarlar",
                column: "KreditMuracietId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KreditRandevular_KreditMuracietId",
                table: "KreditRandevular",
                column: "KreditMuracietId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KreditRandevular_Tarix",
                table: "KreditRandevular",
                column: "Tarix");

            migrationBuilder.CreateIndex(
                name: "IX_KreditRandevular_TeyinEdenIsciId",
                table: "KreditRandevular",
                column: "TeyinEdenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditSmsLoglar_GonderenIsciId",
                table: "KreditSmsLoglar",
                column: "GonderenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditSmsLoglar_KreditMuracietId",
                table: "KreditSmsLoglar",
                column: "KreditMuracietId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditZaminler_FIN",
                table: "KreditZaminler",
                column: "FIN");

            migrationBuilder.CreateIndex(
                name: "IX_KreditZaminler_KreditMuracietId",
                table: "KreditZaminler",
                column: "KreditMuracietId");

            migrationBuilder.AddForeignKey(
                name: "FK_KreditMuracietler_Isciler_AsanFinanceBaxanIsciId",
                table: "KreditMuracietler",
                column: "AsanFinanceBaxanIsciId",
                principalTable: "Isciler",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KreditMuracietler_Isciler_MkrBaxanIsciId",
                table: "KreditMuracietler",
                column: "MkrBaxanIsciId",
                principalTable: "Isciler",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KreditMuracietler_Isciler_AsanFinanceBaxanIsciId",
                table: "KreditMuracietler");

            migrationBuilder.DropForeignKey(
                name: "FK_KreditMuracietler_Isciler_MkrBaxanIsciId",
                table: "KreditMuracietler");

            migrationBuilder.DropTable(
                name: "KreditBaxanIsciler");

            migrationBuilder.DropTable(
                name: "KreditQerarImzalar");

            migrationBuilder.DropTable(
                name: "KreditRandevular");

            migrationBuilder.DropTable(
                name: "KreditSmsLoglar");

            migrationBuilder.DropTable(
                name: "KreditZaminler");

            migrationBuilder.DropTable(
                name: "KomiteUzvleri");

            migrationBuilder.DropTable(
                name: "KreditQerarlar");

            migrationBuilder.DropIndex(
                name: "IX_KreditMuracietler_AsanFinanceBaxanIsciId",
                table: "KreditMuracietler");

            migrationBuilder.DropIndex(
                name: "IX_KreditMuracietler_MkrBaxanIsciId",
                table: "KreditMuracietler");

            migrationBuilder.DropColumn(
                name: "AsanFinanceBaxanIsciId",
                table: "KreditMuracietler");

            migrationBuilder.DropColumn(
                name: "AsanFinanceBaxilmaTarixi",
                table: "KreditMuracietler");

            migrationBuilder.DropColumn(
                name: "AsanFinanceNeticesi",
                table: "KreditMuracietler");

            migrationBuilder.DropColumn(
                name: "IlkinBaxisQeyd",
                table: "KreditMuracietler");

            migrationBuilder.DropColumn(
                name: "MkrBaxanIsciId",
                table: "KreditMuracietler");

            migrationBuilder.DropColumn(
                name: "MkrBaxilmaTarixi",
                table: "KreditMuracietler");

            migrationBuilder.DropColumn(
                name: "MkrNeticesi",
                table: "KreditMuracietler");

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4160));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170));

            migrationBuilder.UpdateData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4175));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4194));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4199));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4204));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4204));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4209));

            migrationBuilder.UpdateData(
                table: "MaasParametrleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4209));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4233));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4238));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4238));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4238));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4243));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4243));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4243));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4248));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4248));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4272));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4277));

            migrationBuilder.UpdateData(
                table: "VergiPilleleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4277));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 1,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4010));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 2,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4025));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 3,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4025));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 4,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4029));

            migrationBuilder.UpdateData(
                table: "XercKateqoriyalari",
                keyColumn: "Id",
                keyValue: 5,
                column: "YaradilmaTarixi",
                value: new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4029));
        }
    }
}
