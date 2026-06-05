using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260605100000_AddPidZaminlerSorguId")]
    public partial class AddPidZaminlerSorguId : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME  = 'SistemAyarlari'
                                 AND COLUMN_NAME = 'PidZaminlerSorguId')
                BEGIN
                    ALTER TABLE [SistemAyarlari] ADD [PidZaminlerSorguId] INT NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME  = 'SistemAyarlari'
                             AND COLUMN_NAME = 'PidZaminlerSorguId')
                BEGIN
                    ALTER TABLE [SistemAyarlari] DROP COLUMN [PidZaminlerSorguId];
                END
            ");
        }
    }
}
