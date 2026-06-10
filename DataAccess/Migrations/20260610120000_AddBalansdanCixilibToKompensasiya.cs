using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// MezuniyyetKompensasiyalari cədvəlinə BalansdanCixilib sütunu əlavə edir.
    /// Yaradılan kimi kompensasiya günləri məzuniyyət balansından çıxılıbsa true.
    /// Ləğv zamanı yalnız bu true olan qeydlər balansa geri qaytarılır.
    /// İdempotentdir — sütun artıq varsa heç nə etmir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610120000_AddBalansdanCixilibToKompensasiya")]
    public partial class AddBalansdanCixilibToKompensasiya : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME = 'MezuniyyetKompensasiyalari'
                                 AND COLUMN_NAME = 'BalansdanCixilib')
                BEGIN
                    ALTER TABLE [dbo].[MezuniyyetKompensasiyalari]
                        ADD [BalansdanCixilib] BIT NOT NULL DEFAULT 0;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'MezuniyyetKompensasiyalari'
                             AND COLUMN_NAME = 'BalansdanCixilib')
                BEGIN
                    ALTER TABLE [dbo].[MezuniyyetKompensasiyalari] DROP COLUMN [BalansdanCixilib];
                END
            ");
        }
    }
}
