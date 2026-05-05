using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Isciler cədvəlinə EvvelkiStajPeriodlari (nvarchar(max), nullable) əlavə edir.
    /// JSON formatında işçinin əvvəlki iş yerlərindəki dövrlər saxlanılır:
    ///   [{"s":"2010-01-15","e":"2015-06-30"}, ...]
    ///
    /// Cari ümumi staj hesablaması:
    ///   total = Σ(period_dövrü) + bu_bankda_staj (IsheQebulTarixi → bu gün)
    ///
    /// İdempotentdir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260505140000_AddIsciEvvelkiStajPeriodlari")]
    public partial class AddIsciEvvelkiStajPeriodlari : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'EvvelkiStajPeriodlari' AND Object_ID = Object_ID('Isciler')
                )
                BEGIN
                    ALTER TABLE Isciler ADD EvvelkiStajPeriodlari nvarchar(max) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'EvvelkiStajPeriodlari' AND Object_ID = Object_ID('Isciler')
                )
                BEGIN
                    ALTER TABLE Isciler DROP COLUMN EvvelkiStajPeriodlari;
                END
            ");
        }
    }
}
