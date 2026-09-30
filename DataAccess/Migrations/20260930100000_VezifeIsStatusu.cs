using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// "İşdə statusu" (icraçı / məsul şəxs) — Vəzifə ADINA görə, TƏKRARSIZ
    /// (30.09.2026, istifadəçi təklifi). Mühasibin şəxsi vərəq Excel-indəki
    /// eyni adlı sahə üçün; HR bunu "HR → Vəzifələr → İşdə statusları"
    /// səhifəsində bir dəfə qurur, Məzuniyyət tarixçə Excel-i (IsciExcel)
    /// bunu VəzifəAdı üzrə oxuyur.
    ///
    /// MÖVCUD HEÇ NƏYƏ TOXUNMUR — `Vezife` cədvəlinə sütun əlavə edilmir
    /// (o, departament üzrə sətirlənir, bu isə ad üzrə təkrarsız olmalıdır).
    ///
    /// ⚠️ `InsertData` İSTİFADƏ EDİLMİR (CLAUDE.md) — bu layihədə migration-lar
    /// əl ilə yazılır, `.Designer.cs` yoxdur; başlanğıcda heç bir sətir lazım
    /// deyil (siyahı boş başlayır, HR ilk dəfə açanda doldurur).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260930100000_VezifeIsStatusu")]
    public partial class VezifeIsStatusu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VezifeIsStatuslari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VezifeAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
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
                    table.PrimaryKey("PK_VezifeIsStatuslari", x => x.Id);
                });

            // Vəzifə adı TƏKRARSIZ olmalıdır — eyni ad üçün iki fərqli status
            // yazıla bilməz (istifadəçi tələbi: "təkrarsız vəzifələr gəlsin").
            migrationBuilder.CreateIndex(
                name: "IX_VezifeIsStatuslari_VezifeAdi",
                table: "VezifeIsStatuslari",
                column: "VezifeAdi",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VezifeIsStatuslari");
        }
    }
}
