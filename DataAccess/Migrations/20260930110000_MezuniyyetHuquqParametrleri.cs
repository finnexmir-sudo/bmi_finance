using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Əmək məzuniyyəti hüququnun qanuni ədədləri (yaş həddi, əlavə gün, staj
    /// pilləsi) — əvvəllər `MezuniyyetHuquqService`-də HARDCODE idi, indi bu
    /// TƏK sətirli cədvəldən oxunur (30.09.2026, istifadəçi tələbi: "qanun
    /// dəyişsə kod dəyişməli olmasın").
    ///
    /// Seed sətri kodun ƏVVƏLKİ hardcode dəyərləri ilə BİRƏ-BİR EYNİDİR —
    /// bu migration heç bir işçinin balansını DƏYİŞMİR, yalnız eyni rəqəmləri
    /// koddan bazaya köçürür.
    ///
    /// ⚠️ `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md) — bu layihədə migration-lar
    /// əl ilə yazılır, `.Designer.cs` yoxdur; seed sətri `migrationBuilder.Sql`
    /// ilə raw INSERT-dir (model-ə baxmır, CreateTable-ı sındırmır).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260930110000_MezuniyyetHuquqParametrleri")]
    public partial class AddMezuniyyetHuquqParametrleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MezuniyyetHuquqParametrleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EsasGunAdi = table.Column<int>(type: "int", nullable: false, defaultValue: 21),
                    EsasGunElil = table.Column<int>(type: "int", nullable: false, defaultValue: 42),
                    // decimal(5,2) — `double` YOX (bax entity-dəki qeyd: az-AZ
                    // mədəniyyətində `double` FlexibleDecimalModelBinder-dən keçmir).
                    StajHedd1Il = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 5m),
                    StajHedd1Gun = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    StajHedd2Il = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 10m),
                    StajHedd2Gun = table.Column<int>(type: "int", nullable: false, defaultValue: 4),
                    StajHedd3Il = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 15m),
                    StajHedd3Gun = table.Column<int>(type: "int", nullable: false, defaultValue: 6),
                    UsaqYasHeddi = table.Column<int>(type: "int", nullable: false, defaultValue: 14),
                    EngelliUsaqYasHeddi = table.Column<int>(type: "int", nullable: false, defaultValue: 18),
                    UsaqSayi2GunHeddi = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    UsaqGun2 = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    UsaqSayi5GunHeddi = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    UsaqGun5 = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
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
                    table.PrimaryKey("PK_MezuniyyetHuquqParametrleri", x => x.Id);
                });

            // Seed — koddakı əvvəlki hardcode dəyərlərin EYNİSİ. Servis buna
            // baxmayaraq sətir tapılmasa da C#-dəki `Defolt` statik nüsxəsinə
            // düşür (ikiqat qoruma) — amma normal halda məhz bu sətir oxunur.
            migrationBuilder.Sql(@"
                INSERT INTO [MezuniyyetHuquqParametrleri]
                    ([EsasGunAdi],[EsasGunElil],
                     [StajHedd1Il],[StajHedd1Gun],[StajHedd2Il],[StajHedd2Gun],[StajHedd3Il],[StajHedd3Gun],
                     [UsaqYasHeddi],[EngelliUsaqYasHeddi],
                     [UsaqSayi2GunHeddi],[UsaqGun2],[UsaqSayi5GunHeddi],[UsaqGun5],
                     [YaradilmaTarixi],[Silinib])
                VALUES
                    (21, 42,
                     5, 2, 10, 4, 15, 6,
                     14, 18,
                     2, 2, 3, 5,
                     GETDATE(), 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MezuniyyetHuquqParametrleri");
        }
    }
}
