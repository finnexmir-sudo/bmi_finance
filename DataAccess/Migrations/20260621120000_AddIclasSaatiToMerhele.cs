using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260621120000_AddIclasSaatiToMerhele")]
    public partial class AddIclasSaatiToMerhele : Migration
    {
        // Məhkəmə mərhələsinə (iclas) saat sahəsi — Excel görüş saatları üçün.
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME = 'MehkemeMerheleri' AND COLUMN_NAME = 'Saat')
                BEGIN
                    ALTER TABLE [MehkemeMerheleri] ADD [Saat] NVARCHAR(50) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'MehkemeMerheleri' AND COLUMN_NAME = 'Saat')
                    ALTER TABLE [MehkemeMerheleri] DROP COLUMN [Saat];
            ");
        }
    }
}
