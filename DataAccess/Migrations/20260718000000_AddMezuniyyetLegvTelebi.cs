using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İşçinin məzuniyyət ləğv müraciəti üçün 3 sütun:
    ///   LegvTelebEdilib (bit), LegvTelebSebebi (nvarchar), LegvTelebTarixi (datetime2).
    /// Startup-da db.Database.Migrate() ilə avtomatik tətbiq olunur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260718000000_AddMezuniyyetLegvTelebi")]
    public partial class AddMezuniyyetLegvTelebi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LegvTelebEdilib",
                table: "Mezuniyyetler",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegvTelebSebebi",
                table: "Mezuniyyetler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LegvTelebTarixi",
                table: "Mezuniyyetler",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LegvTelebEdilib", table: "Mezuniyyetler");
            migrationBuilder.DropColumn(name: "LegvTelebSebebi", table: "Mezuniyyetler");
            migrationBuilder.DropColumn(name: "LegvTelebTarixi", table: "Mezuniyyetler");
        }
    }
}
