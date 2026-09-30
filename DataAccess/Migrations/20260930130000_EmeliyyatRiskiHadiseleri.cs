using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// MB Qərar 04/1 ("Banklarda əməliyyat risklərinin idarə edilməsi Qaydası"),
    /// Əlavə 4 — əməliyyat riski hadisələri jurnalı (30.09.2026).
    ///
    /// ⚠️ `InsertData` İŞLƏDİLMİR (CLAUDE.md) — bu layihədə migration-lar əl
    /// ilə yazılır, `.Designer.cs` yoxdur; seed sətri `migrationBuilder.Sql`
    /// ilə raw INSERT-dir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260930130000_EmeliyyatRiskiHadiseleri")]
    public partial class EmeliyyatRiskiHadiseleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmeliyyatRiskiParametrleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TedbirZererHeddi = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 1000m),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmeliyyatRiskiParametrleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmeliyyatRiskiHadiseleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QeydiyyatKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StrukturBolme = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MelumatiVerenSexs = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HadiseninBasVerdiyiTarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HadiseninMueyyenlesdirilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MueyyenlesdirenIsciId = table.Column<int>(type: "int", nullable: true),
                    Tesvir = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Sebeb = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TezlikDerecesi = table.Column<int>(type: "int", nullable: false),
                    TesirDerecesi = table.Column<int>(type: "int", nullable: false),
                    BiznesSahesi = table.Column<int>(type: "int", nullable: false),
                    BankMehsulu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RiskKateqoriyasi1 = table.Column<int>(type: "int", nullable: false),
                    RiskKateqoriyasi2 = table.Column<int>(type: "int", nullable: false),
                    RiskHadisesiNumune = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ZererTesirKateqoriyasi = table.Column<int>(type: "int", nullable: false),
                    UmumiZererMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PotensialZererMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BerpaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BerpaOlunanMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SigortaIleBerpaOlunanHisse = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TedbirlerinTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TedbirlerinTesviri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TedbirlereMesulBolme = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TedbirlerinIcraStatusu = table.Column<int>(type: "int", nullable: true),
                    // Qeyd yaradılan andakı hədd — donmuş dəyər, redaktədə dəyişmir
                    // (bax entity-dəki qeyd: Pul Köçürməsi UsdEkvivalent ilə EYNİ prinsip).
                    TedbirZererHeddiYaradilmaAninda = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    SonDeyisiklikTesviri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmeliyyatRiskiHadiseleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmeliyyatRiskiHadisesiTarixce",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HadiseId = table.Column<int>(type: "int", nullable: false),
                    DeyisiklikTesviri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EvvelkiDeyerlerJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmeliyyatRiskiHadisesiTarixce", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmeliyyatRiskiHadisesiTarixce_EmeliyyatRiskiHadiseleri_HadiseId",
                        column: x => x.HadiseId,
                        principalTable: "EmeliyyatRiskiHadiseleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmeliyyatRiskiHadisesiTarixce_HadiseId",
                table: "EmeliyyatRiskiHadisesiTarixce",
                column: "HadiseId");

            // Seed — Faza 1 default hədd. Risk departamenti "Parametrlər"
            // səhifəsindən sonradan dəyişə bilər (bank özü təyin edir, MB
            // Qərar 04/1-in 100 000 AZN hesabat həddi ilə QARIŞDIRILMASIN).
            migrationBuilder.Sql(@"
                INSERT INTO [EmeliyyatRiskiParametrleri]
                    ([TedbirZererHeddi],[YaradilmaTarixi],[Silinib])
                VALUES
                    (1000, GETDATE(), 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EmeliyyatRiskiHadisesiTarixce");
            migrationBuilder.DropTable(name: "EmeliyyatRiskiHadiseleri");
            migrationBuilder.DropTable(name: "EmeliyyatRiskiParametrleri");
        }
    }
}
