using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260608150000_AddGorushCihazTracking")]
    public partial class AddGorushCihazTracking : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME = 'GorushIshtirakcilar'
                                 AND COLUMN_NAME = 'CihazCixisVaxti')
                BEGIN
                    ALTER TABLE [GorushIshtirakcilar]
                        ADD [CihazCixisVaxti]  DATETIME2 NULL,
                            [CihazQayidisVaxti] DATETIME2 NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'GorushIshtirakcilar'
                             AND COLUMN_NAME = 'CihazCixisVaxti')
                BEGIN
                    ALTER TABLE [GorushIshtirakcilar]
                        DROP COLUMN [CihazCixisVaxti];
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'GorushIshtirakcilar'
                             AND COLUMN_NAME = 'CihazQayidisVaxti')
                BEGIN
                    ALTER TABLE [GorushIshtirakcilar]
                        DROP COLUMN [CihazQayidisVaxti];
                END
            ");
        }
    }
}
