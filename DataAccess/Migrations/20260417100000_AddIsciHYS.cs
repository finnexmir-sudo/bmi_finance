using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Həyat Yığım Sığortası (HYS) — işçi üzrə aylıq ödəniş təyinatları.
    /// Vergi və DSMF bazasından azaddır; İTSS/İşsizlik isə tam maaş üzrədir.
    /// İşəgötürənin payı və max maaş faizi MaasParametri-dən gəlir (Nov = 10, 11).
    /// </summary>
    public partial class AddIsciHYS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IsciHYSler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    AyliqMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SigortaSirketi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PolisNomresi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IsciHYSler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciHYSler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IsciHYSler_IsciId",
                table: "IsciHYSler",
                column: "IsciId");

            // HYS parametrlərini MaasParametrleri cədvəlinə seed et
            migrationBuilder.InsertData(
                table: "MaasParametrleri",
                columns: new[] { "Nov", "Deyer", "Aciqlama", "Aktivdir", "BaslamaTarixi", "Silinib", "YaradilmaTarixi" },
                values: new object[] { 10, 15m, "İşəgötürənin HYS payı (%)", true, new DateTime(2026, 1, 1), false, DateTime.Now });

            migrationBuilder.InsertData(
                table: "MaasParametrleri",
                columns: new[] { "Nov", "Deyer", "Aciqlama", "Aktivdir", "BaslamaTarixi", "Silinib", "YaradilmaTarixi" },
                values: new object[] { 11, 50m, "HYS max maaş faizi (%)", true, new DateTime(2026, 1, 1), false, DateTime.Now });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IsciHYSler");

            migrationBuilder.DeleteData(
                table: "MaasParametrleri",
                keyColumn: "Nov",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "MaasParametrleri",
                keyColumn: "Nov",
                keyValue: 11);
        }
    }
}
