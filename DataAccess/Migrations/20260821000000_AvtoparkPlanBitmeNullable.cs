using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Avtopark — `MasinMuracietler.PlanBitme` NULL qəbul edir (21.08.2026).
    ///
    /// NİYƏ: müraciət formasından «Bitmə» sahəsi çıxarıldı. İstifadəçi qərarı:
    /// «bitmə bilinmir axı — nə vaxt qayıtdı, kassada qayıtdı yazılacaq».
    /// Qayıdışın yeganə mənbəyi `QayidisTarixi`-dir (kassa yazır).
    ///
    /// SÜTUN QƏSDƏN SİLİNMİR — 21.08.2026-dan əvvəlki qeydlərdə real
    /// planlaşdırılan bitmə vaxtı var və jurnal tarixçəsi pozulmamalıdır.
    /// Yeni sətirlərdə `NULL` qalır.
    ///
    /// MÖVCUD DATAYA TOXUNMUR: nə UPDATE var, nə DELETE. Yalnız sütunun
    /// nullability-si dəyişir (`NOT NULL` → `NULL`), bu isə mövcud dəyərləri
    /// olduğu kimi saxlayır.
    ///
    /// ⚠️ GERİ QAYTARMA (`Down`) YALNIZ BOŞ DEYİLSƏ TƏHLÜKƏSİZDİR: sütun
    /// yenidən `NOT NULL` edilməzdən əvvəl `NULL` sətirlər doldurulmalıdır,
    /// yoxsa SQL Server `ALTER COLUMN`-u rədd edər. `Down` bunu özü edir —
    /// boş qalanlara `PlanBaslama` yazır (uydurma dəyildir: o vaxt sistem
    /// bitməni onsuz da bilmirdi, ona görə çıxış anı ilə eyniləşdirilir).
    ///
    /// Startup-da `db.Database.Migrate()` ilə avtomatik tətbiq olunur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260821000000_AvtoparkPlanBitmeNullable")]
    public partial class AvtoparkPlanBitmeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PlanBitme",
                table: "MasinMuracietler",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOT NULL-a qayıtmazdan ƏVVƏL boşları doldur — yoxsa ALTER sınır.
            migrationBuilder.Sql(@"
UPDATE [MasinMuracietler]
   SET [PlanBitme] = [PlanBaslama]
 WHERE [PlanBitme] IS NULL;
");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PlanBitme",
                table: "MasinMuracietler",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
