using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Paylaşımlar — şəkil üçün mətndən AYRI, xüsusi "Üz qabığı şəkli" sahəsi
    /// (28.10.2026, istifadəçi tələbi: "şəkil üçün ayrıca əlavə, mətn üçün ayrı
    /// olsun"). Yalnız sütun əlavəsidir, `InsertData` YOXDUR.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261028100000_PaylasimUzQabigiSekil")]
    public partial class PaylasimUzQabigiSekilMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UzQabigiSekilUrl",
                table: "Melumatlar",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UzQabigiSekilUrl",
                table: "Melumatlar");
        }
    }
}
