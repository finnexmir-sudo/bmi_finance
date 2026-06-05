using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260604100000_AddPidOdenisGunuSorguId")]
    public partial class AddPidOdenisGunuSorguId : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME  = 'SistemAyarlari'
                                 AND COLUMN_NAME = 'PidOdenisGunuSorguId')
                BEGIN
                    ALTER TABLE [SistemAyarlari] ADD [PidOdenisGunuSorguId] INT NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME  = 'SistemAyarlari'
                             AND COLUMN_NAME = 'PidOdenisGunuSorguId')
                BEGIN
                    ALTER TABLE [SistemAyarlari] DROP COLUMN [PidOdenisGunuSorguId];
                END
            ");
        }
    }
}
