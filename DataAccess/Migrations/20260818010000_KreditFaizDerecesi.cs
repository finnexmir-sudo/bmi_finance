using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// VM 98.2.1 — bazar faiz dərəcəsi tarixçəsi (18.08.2026).
    ///
    /// İşçi kreditinin faizi bazar dərəcəsindən aşağı olduqda fərq hesabi gəlir
    /// sayılır. Bazar dərəcəsini mühasib əl ilə yazır; dəyişəndə YENİ SƏTİR
    /// əlavə olunur ki, keçmiş dövrlər öz vaxtındakı dərəcə ilə hesablansın.
    ///
    /// Yeni cədvəl — mövcud dataya toxunmur.
    /// Startup-da db.Database.Migrate() ilə avtomatik tətbiq olunur.
    ///
    /// SİNİF ADI QƏSDƏN «...Cedveli»-dir: entity də `KreditFaizDerecesi` adlanır,
    /// eyni adı işlətsək ad kölgələnməsi riski yaranır (CLAUDE.md — CS0118).
    /// Migration ID isə fayl adı ilə eyni qalır.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260818010000_KreditFaizDerecesi")]
    public partial class KreditFaizDerecesiCedveli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KreditFaizDereceleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValyutaKodu = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Derece = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_KreditFaizDereceleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KreditFaizDereceleri_ValyutaKodu_Tarix",
                table: "KreditFaizDereceleri",
                columns: new[] { "ValyutaKodu", "Tarix" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "KreditFaizDereceleri");
        }
    }
}
