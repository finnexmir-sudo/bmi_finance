using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Qara Jeton — kəsinti miqdarı VERİLMƏ ANINDA dondurulur (07.10.2026,
    /// İKİNCİ DALĞA, KRİTİK).
    ///
    /// Real hadisə: "kəsilən saat" göstərimi `JetonTeyinati.SaatDeyeri`-ni
    /// CANLI oxuyurdu (digər jeton kartları ilə eyni qayda). Bu, Musbat
    /// jetonlar üçün düzgündür (hələ xərclənməmiş mükafatın cari dəyəri), amma
    /// Qara Jeton üçün SƏHVDİR — "kəsilən saat" KEÇMİŞ bir hadisənin həcmidir,
    /// gələcəkdə HR kataloqda həmin növün dəyərini dəyişsə (məs. 1,25-dən
    /// -30-a), artıq TƏTBİQ OLUNMUŞ köhnə Qara Jetonun kartı da səssizcə
    /// dəyişib yanlış rəqəm göstərirdi (1,25 saat həqiqətən kəsilmişdi, kart
    /// "30 saat kəsildi" yazırdı).
    ///
    /// Həll: kəsinti miqdarı `IsciJetonu.MenfiMiqdar`-da verilmə ANINDA (mütləq
    /// qiymətlə) dondurulur — "Pul Köçürməsi" limitindəki `UsdEkvivalent` və
    /// "Əməliyyat Riski"ndəki `TedbirZererHeddiYaradilmaAninda` ilə EYNİ
    /// dondurma prinsipi. Yalnız Menfi növ jetonlar üçün doldurulur, Musbat
    /// üçün NULL qalır (onlar hələ də canlı dəyərlə göstərilir).
    ///
    /// ⚠️ Köhnə (bu migrationdan ƏVVƏL verilmiş) Qara Jetonlarda bu sahə NULL
    /// qalır — onların tarixi kəsinti miqdarı retroaktiv bərpa edilə bilməz
    /// (ayrıca ledger/audit cədvəli olmadığı üçün), kod canlı dəyərə geri
    /// düşür (köhnə, dəqiq olmaya bilən davranış).
    ///
    /// ⚠️ `InsertData` İŞLƏDİLMİR, `.Designer.cs` yoxdur (CLAUDE.md) — hər iki
    /// atribut sinfin ÖZÜNDƏDİR, Designer olmadığı üçün məcburidir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261007110000_QaraJetonMenfiMiqdar")]
    public partial class QaraJetonMenfiMiqdar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MenfiMiqdar", table: "IsciJetonlari",
                type: "decimal(18,2)", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MenfiMiqdar", table: "IsciJetonlari");
        }
    }
}
