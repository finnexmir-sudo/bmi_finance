using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260608160000_GorushBitisSaatiOptional")]
    public partial class GorushBitisSaatiOptional : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            // Mövcud NOT NULL sütunu NULL-a çevir
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'Gorushler'
                             AND COLUMN_NAME = 'BitisSaati'
                             AND IS_NULLABLE = 'NO')
                BEGIN
                    ALTER TABLE [Gorushler] ALTER COLUMN [BitisSaati] TIME NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            // Geri alarkən NULL dəyərləri 00:00-a set et, sonra NOT NULL yap
            m.Sql(@"
                UPDATE [Gorushler] SET [BitisSaati] = '00:00:00' WHERE [BitisSaati] IS NULL;
                ALTER TABLE [Gorushler] ALTER COLUMN [BitisSaati] TIME NOT NULL;
            ");
        }
    }
}
