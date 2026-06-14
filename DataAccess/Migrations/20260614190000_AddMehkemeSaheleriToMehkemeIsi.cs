using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260614190000_AddMehkemeSaheleriToMehkemeIsi")]
    public partial class AddMehkemeSaheleriToMehkemeIsi : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='Sira')
                    ALTER TABLE [MehkemeIsleri] ADD [Sira] INT NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='MehkemeStatusMetn')
                    ALTER TABLE [MehkemeIsleri] ADD [MehkemeStatusMetn] NVARCHAR(MAX) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='KreditNovuMetn')
                    ALTER TABLE [MehkemeIsleri] ADD [KreditNovuMetn] NVARCHAR(MAX) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='MehkemeyeVerilmeTarixi')
                    ALTER TABLE [MehkemeIsleri] ADD [MehkemeyeVerilmeTarixi] DATETIME2 NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='MehkemeSenedi')
                    ALTER TABLE [MehkemeIsleri] ADD [MehkemeSenedi] NVARCHAR(MAX) NULL;
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='Sira') ALTER TABLE [MehkemeIsleri] DROP COLUMN [Sira];
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='MehkemeStatusMetn') ALTER TABLE [MehkemeIsleri] DROP COLUMN [MehkemeStatusMetn];
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='KreditNovuMetn') ALTER TABLE [MehkemeIsleri] DROP COLUMN [KreditNovuMetn];
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='MehkemeyeVerilmeTarixi') ALTER TABLE [MehkemeIsleri] DROP COLUMN [MehkemeyeVerilmeTarixi];
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeIsleri' AND COLUMN_NAME='MehkemeSenedi') ALTER TABLE [MehkemeIsleri] DROP COLUMN [MehkemeSenedi];
            ");
        }
    }
}
