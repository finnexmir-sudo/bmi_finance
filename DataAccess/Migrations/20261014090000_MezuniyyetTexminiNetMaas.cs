using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Məzuniyyət müraciəti yaradılanda göstərilən təxmini net maaş artıq
    /// dondurulub saxlanır (14.10.2026).
    ///
    /// İstifadəçinin öz sözü ilə sual: "işçi mezuniyyet yazanda ay sonu
    /// alacağı maaşı göstərir, amma sonra onu heç yerdə görə bilmirəm".
    /// Araşdırma göstərdi ki, "MezuniyyetController.Preview" (umumiNet)
    /// real hesablama mühərrikini (MaasHesablamaService) işlədir, amma
    /// cavab heç cədvələ yazılmır — bir dəfə ekranda görünüb yox olur.
    ///
    /// Həll: müraciət YARADILAN ANDA (Create POST) preview-un son
    /// göstərdiyi "umumiNet" dəyəri bu sütuna yazılır — "Pul Köçürməsi"nin
    /// `UsdEkvivalent`-i, Qara Jetonun `MenfiMiqdar`-ı ilə EYNİ "əməliyyat
    /// anında dondur" prinsipi. İşçinin öz Müraciət Detayı səhifəsində
    /// "Təxmini (müraciət zamanı)" etiketi ilə göstərilir.
    ///
    /// ⚠️ Real ödənişlə (`OdenenMebleg`, HR təsdiqində YENİDƏN hesablanır)
    /// eyni olacağına ZƏMANƏT YOXDUR — iki ayrı hesablama anıdır, arada
    /// korreksiya/başqa məzuniyyət/qazanc tarixçəsi dəyişə bilər.
    ///
    /// ⚠️ GERİYƏ TƏTBİQ OLUNMUR — köhnə (bu migrasiyadan əvvəl yaradılmış)
    /// müraciətlərdə sütun NULL qalır, çünki o anın preview cavabı heç yerdə
    /// saxlanılmayıb, retroaktiv bərpa mümkün deyil.
    ///
    /// `InsertData` İŞLƏDİLMİR, `.Designer.cs` yoxdur (CLAUDE.md) — hər iki
    /// atribut sinfin ÖZÜNDƏDİR, Designer olmadığı üçün məcburidir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261014090000_MezuniyyetTexminiNetMaas")]
    public partial class MezuniyyetTexminiNetMaas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TexminiNetMaas", table: "Mezuniyyetler",
                type: "decimal(18,2)", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TexminiNetMaas", table: "Mezuniyyetler");
        }
    }
}
