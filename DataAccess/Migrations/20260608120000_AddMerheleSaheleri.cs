using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260608120000_AddMerheleSaheleri")]
    public partial class AddMerheleSaheleri : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME = 'MehkemeMerheleri' AND COLUMN_NAME = 'Hakim')
                BEGIN
                    ALTER TABLE [MehkemeMerheleri] ADD [Hakim] NVARCHAR(200) NULL;
                END
            ");

            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME = 'MehkemeMerheleri' AND COLUMN_NAME = 'IcraciMemur')
                BEGIN
                    ALTER TABLE [MehkemeMerheleri] ADD [IcraciMemur] NVARCHAR(200) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'MehkemeMerheleri' AND COLUMN_NAME = 'IcraciMemur')
                    ALTER TABLE [MehkemeMerheleri] DROP COLUMN [IcraciMemur];

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'MehkemeMerheleri' AND COLUMN_NAME = 'Hakim')
                    ALTER TABLE [MehkemeMerheleri] DROP COLUMN [Hakim];
            ");
        }
    }
}
