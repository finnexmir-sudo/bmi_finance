using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Gedən həvalə — «Müqavilə №» və «Bəyannamə №» sütunları 15 → 50 (27.08.2026).
    ///
    /// NİYƏ: 15 simvol BMI-nin köhnə `odb.geden_hevale` ölçüsüdür və real müqavilə
    /// nömrələri üçün azdır. İstifadəçi qərarı: «15 simvol azdır axı, müqavilə №
    /// uzundur». Bəyannamə № də eyni kateqoriyadır — 15-də qalsa sabah eyni xəta
    /// onda təkrarlanardı, ona görə birlikdə genişləndirilir.
    ///
    /// NƏ ÜÇÜN LAZIM OLDU: 27.08.2026-da VM-də «Müqavilə №» xanasına 15-dən uzun
    /// mətn yazıldı və SQL «String or binary data would be truncated» ilə BÜTÜN
    /// INSERT-i rədd etdi (log-20260827, 13:50). İstifadəçi yalnız ümumi
    /// «xəta baş verdi» səhifəsi görürdü — sütunun adı heç yerdə görünmürdü.
    ///
    /// MÖVCUD DATAYA TOXUNMUR: `nvarchar` sütununu GENİŞLƏTMƏK SQL Server-də
    /// yalnız metadata əməliyyatıdır — sətirlər yenidən yazılmır, heç bir dəyər
    /// dəyişmir və ya kəsilmir. Cədvəl kilidlənməsi də qısamüddətlidir.
    ///
    /// ⚠️ GERİ QAYTARMA (`Down`) DATA İTİRƏ BİLƏR: sütun yenidən 15-ə daraldılanda
    /// ondan uzun dəyərlər SQL tərəfindən qəbul EDİLMƏZ və `ALTER` sınar. `Down`
    /// bunu özü həll edir — əvvəlcə uzun dəyərləri 15 simvola kəsir. Yəni geri
    /// qayıtmaq DAĞIDICIDIR; yalnız həqiqətən lazım olanda işlədilməlidir.
    ///
    /// Startup-da `db.Database.Migrate()` ilə avtomatik tətbiq olunur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260827000000_HevaleMuqavileNomreUzunlugu")]
    public partial class HevaleMuqavileNomreUzunlugu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CONTRAC_NOM",
                table: "GedenHevale",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DECLAR_NOM",
                table: "GedenHevale",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Daraltmadan ƏVVƏL uzun dəyərləri kəs — yoxsa ALTER sınır.
            // DİQQƏT: bu, DATA İTKİSİDİR (15-dən sonrakı simvollar gedir).
            migrationBuilder.Sql(@"
UPDATE [GedenHevale]
   SET [CONTRAC_NOM] = LEFT([CONTRAC_NOM], 15)
 WHERE [CONTRAC_NOM] IS NOT NULL AND LEN([CONTRAC_NOM]) > 15;

UPDATE [GedenHevale]
   SET [DECLAR_NOM] = LEFT([DECLAR_NOM], 15)
 WHERE [DECLAR_NOM] IS NOT NULL AND LEN([DECLAR_NOM]) > 15;
");

            migrationBuilder.AlterColumn<string>(
                name: "CONTRAC_NOM",
                table: "GedenHevale",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DECLAR_NOM",
                table: "GedenHevale",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
