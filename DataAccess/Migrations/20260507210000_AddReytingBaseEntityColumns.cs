using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507210000_AddReytingBaseEntityColumns")]
    public partial class AddReytingBaseEntityColumns : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            // ReytingKateqoriyalari — missing BaseEntity audit columns
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'YaradanIcraciId')
    ALTER TABLE ReytingKateqoriyalari ADD YaradanIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'YenileyenIcraciId')
    ALTER TABLE ReytingKateqoriyalari ADD YenileyenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'SilenIcraciId')
    ALTER TABLE ReytingKateqoriyalari ADD SilenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'YenilenmeTarixi')
    ALTER TABLE ReytingKateqoriyalari ADD YenilenmeTarixi DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'SilinmeTarixi')
    ALTER TABLE ReytingKateqoriyalari ADD SilinmeTarixi DATETIME2 NULL;
");

            // ReytingParametrleri — missing BaseEntity audit columns
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'YaradanIcraciId')
    ALTER TABLE ReytingParametrleri ADD YaradanIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'YenileyenIcraciId')
    ALTER TABLE ReytingParametrleri ADD YenileyenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'SilenIcraciId')
    ALTER TABLE ReytingParametrleri ADD SilenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'YenilenmeTarixi')
    ALTER TABLE ReytingParametrleri ADD YenilenmeTarixi DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'SilinmeTarixi')
    ALTER TABLE ReytingParametrleri ADD SilinmeTarixi DATETIME2 NULL;
");

            // IsciReytinqleri — missing BaseEntity audit columns
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'YaradanIcraciId')
    ALTER TABLE IsciReytinqleri ADD YaradanIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'YenileyenIcraciId')
    ALTER TABLE IsciReytinqleri ADD YenileyenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'SilenIcraciId')
    ALTER TABLE IsciReytinqleri ADD SilenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'YenilenmeTarixi')
    ALTER TABLE IsciReytinqleri ADD YenilenmeTarixi DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'SilinmeTarixi')
    ALTER TABLE IsciReytinqleri ADD SilinmeTarixi DATETIME2 NULL;
");

            // ManualReytingQeydleri — missing BaseEntity audit columns
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'YaradanIcraciId')
    ALTER TABLE ManualReytingQeydleri ADD YaradanIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'YenileyenIcraciId')
    ALTER TABLE ManualReytingQeydleri ADD YenileyenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'SilenIcraciId')
    ALTER TABLE ManualReytingQeydleri ADD SilenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'YenilenmeTarixi')
    ALTER TABLE ManualReytingQeydleri ADD YenilenmeTarixi DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'SilinmeTarixi')
    ALTER TABLE ManualReytingQeydleri ADD SilinmeTarixi DATETIME2 NULL;
");
        }

        protected override void Down(MigrationBuilder m)
        {
            // These nullable audit columns are safe to drop
            m.Sql(@"
ALTER TABLE ReytingKateqoriyalari DROP COLUMN IF EXISTS YaradanIcraciId;
ALTER TABLE ReytingKateqoriyalari DROP COLUMN IF EXISTS YenileyenIcraciId;
ALTER TABLE ReytingKateqoriyalari DROP COLUMN IF EXISTS SilenIcraciId;
ALTER TABLE ReytingKateqoriyalari DROP COLUMN IF EXISTS YenilenmeTarixi;
ALTER TABLE ReytingKateqoriyalari DROP COLUMN IF EXISTS SilinmeTarixi;

ALTER TABLE ReytingParametrleri DROP COLUMN IF EXISTS YaradanIcraciId;
ALTER TABLE ReytingParametrleri DROP COLUMN IF EXISTS YenileyenIcraciId;
ALTER TABLE ReytingParametrleri DROP COLUMN IF EXISTS SilenIcraciId;
ALTER TABLE ReytingParametrleri DROP COLUMN IF EXISTS YenilenmeTarixi;
ALTER TABLE ReytingParametrleri DROP COLUMN IF EXISTS SilinmeTarixi;

ALTER TABLE IsciReytinqleri DROP COLUMN IF EXISTS YaradanIcraciId;
ALTER TABLE IsciReytinqleri DROP COLUMN IF EXISTS YenileyenIcraciId;
ALTER TABLE IsciReytinqleri DROP COLUMN IF EXISTS SilenIcraciId;
ALTER TABLE IsciReytinqleri DROP COLUMN IF EXISTS YenilenmeTarixi;
ALTER TABLE IsciReytinqleri DROP COLUMN IF EXISTS SilinmeTarixi;

ALTER TABLE ManualReytingQeydleri DROP COLUMN IF EXISTS YaradanIcraciId;
ALTER TABLE ManualReytingQeydleri DROP COLUMN IF EXISTS YenileyenIcraciId;
ALTER TABLE ManualReytingQeydleri DROP COLUMN IF EXISTS SilenIcraciId;
ALTER TABLE ManualReytingQeydleri DROP COLUMN IF EXISTS YenilenmeTarixi;
ALTER TABLE ManualReytingQeydleri DROP COLUMN IF EXISTS SilinmeTarixi;
");
        }
    }
}
