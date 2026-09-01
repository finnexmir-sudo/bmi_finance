using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Pul köçürməsi — MB kursu sütunları `decimal(18,4)` → `decimal(18,10)` (01.09.2026).
    ///
    /// NİYƏ: İran rialının Mərkəzi Bank kursu **çox kiçik rəqəmdir** — real dəyər
    /// `0,000002950`. 4 onluq yerində o, `0,0000`-a yuvarlaqlaşır, yəni SIFIR olur.
    /// Bazaya yazılan sıfırın nəticəsi isə səssizdir və dağıdıcıdır:
    ///
    ///   `PulKocurmeVoucher.Qur` → `cross = ValyutaCbar / RialCbar`
    ///   `rcbar == 0` olanda `DilingAdd()` **dərhal geri qayıdır** (sıfıra bölmə
    ///   qoruyucusu) → mühasibat yazılışından **dilinq fərqi sətri tamamilə düşür**.
    ///   Heç bir xəta çıxmır, provodka sadəcə bir sətir əskik olur.
    ///
    /// 18,10 seçimi: onluq hissə 10 rəqəm (0,000002950 rahat yerləşir), tam hissə
    /// 8 rəqəm (99 999 999). Hər ikisi MB kursudur — dəyər həmişə 1-ə yaxın və ya
    /// ondan kiçikdir, tam hissəyə 8 rəqəm artıqlaması ilə bəsdir.
    ///
    /// MÖVCUD DATAYA TOXUNMUR — nə INSERT, nə UPDATE, nə DELETE. `decimal`-ın
    /// onluq yerini ARTIRMAQ dəyəri dəyişmir: `0,0295` → `0,0295000000`, yəni
    /// riyazi olaraq eyni ədəd. Mövcud köçürmələrin provodkası dəyişmir.
    ///
    /// ⚠️ `IranRial` (18,2) QƏSDƏN TOXUNULMAYIB — o, «1 vahid = N rial» kursudur
    /// (850 000,00), yəni BÖYÜK rəqəmdir. Onu 18,10 etsək tam hissə 8 rəqəmə
    /// düşərdi; 850 000 yerləşir, amma ehtiyat marja lazımsız yerə daralardı.
    ///
    /// ⚠️ Bu layihədə migration-lar ƏL İLƏ yazılır və `.Designer.cs` yoxdur —
    /// `InsertData`/`UpdateData`/`DeleteData` İŞLƏDİLMİR (CLAUDE.md). Burada
    /// onlara ehtiyac da yoxdur: sırf sxem dəyişikliyidir.
    ///
    /// ⚠️ AŞAĞIDAKI İKİ ATRİBUT MƏCBURİDİR — yoxdursa EF faylı migration saymır,
    /// heç bir xəta çıxmır, sütun isə köhnə qalır (01.09.2026, real hadisə).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260901120000_KocurmeKursDeqiqliyi")]
    public partial class KocurmeKursDeqiqliyi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "RialCbar",
                table: "Kocurme",
                type: "decimal(18,10)",
                precision: 18,
                scale: 10,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ValyutaCbar",
                table: "Kocurme",
                type: "decimal(18,10)",
                precision: 18,
                scale: 10,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);
        }

        /// <summary>
        /// Geri qaytarma — sütunlar yenidən `decimal(18,4)`.
        ///
        /// ⚠️ DAĞIDICIDIR: 4 onluqdan artıq hissə **yuvarlaqlaşır və itir**.
        /// `0,000002950` → `0,0000`, yəni dilinq fərqi sətri yenidən yox olar.
        /// SQL Server bunu XƏTA VERMƏDƏN edir — yalnız həqiqətən lazımdırsa işlət.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ValyutaCbar",
                table: "Kocurme",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,10)",
                oldPrecision: 18,
                oldScale: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RialCbar",
                table: "Kocurme",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,10)",
                oldPrecision: 18,
                oldScale: 10,
                oldNullable: true);
        }
    }
}
