using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Sənəd (Senedler) cədvəlinə istifadəçinin daxil etdiyi sənəd tarixi əlavə edilir.
    /// Mövcud sətirlər üçün default olaraq YaradilmaTarixi qiyməti istifadə olunur.
    /// </summary>
    public partial class AddSenedTarixi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SenedTarixi",
                table: "Senedler",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            // Mövcud sənədlər üçün SenedTarixi-ni YaradilmaTarixi ilə doldur
            migrationBuilder.Sql(@"
                UPDATE Senedler
                SET SenedTarixi = YaradilmaTarixi
                WHERE SenedTarixi IS NOT NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenedTarixi",
                table: "Senedler");
        }
    }
}
