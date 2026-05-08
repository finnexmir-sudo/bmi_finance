using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260508120000_AddHybridIcazeJetonModel")]
    public partial class AddHybridIcazeJetonModel : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            // Icaze: jetonla ödənən saat (0 = adi icazə)
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Icazeler' AND COLUMN_NAME = 'JetonOdenenSaat')
    ALTER TABLE Icazeler ADD JetonOdenenSaat DECIMAL(18,2) NOT NULL CONSTRAINT DF_Icazeler_JetonOdenenSaat DEFAULT 0;
");

            // JetonRedimTelebieri: icazə sorğusu üçün tarix/saat
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'IcazeTarixi')
    ALTER TABLE JetonRedimTelebieri ADD IcazeTarixi DATETIME2 NULL;
");
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'BaslamaSaati')
    ALTER TABLE JetonRedimTelebieri ADD BaslamaSaati TIME(7) NULL;
");
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'BitisSaati')
    ALTER TABLE JetonRedimTelebieri ADD BitisSaati TIME(7) NULL;
");

            // JetonRedimTelebieri: rəhbər təsdiq mərhələsi
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberTesdiq')
    ALTER TABLE JetonRedimTelebieri ADD RehberTesdiq BIT NULL;
");
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberUserId')
    ALTER TABLE JetonRedimTelebieri ADD RehberUserId INT NULL;
");
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberTesdiqTarixi')
    ALTER TABLE JetonRedimTelebieri ADD RehberTesdiqTarixi DATETIME2 NULL;
");
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberQeyd')
    ALTER TABLE JetonRedimTelebieri ADD RehberQeyd NVARCHAR(MAX) NULL;
");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("ALTER TABLE Icazeler DROP CONSTRAINT IF EXISTS DF_Icazeler_JetonOdenenSaat;");
            m.Sql("ALTER TABLE Icazeler DROP COLUMN IF EXISTS JetonOdenenSaat;");

            m.Sql("ALTER TABLE JetonRedimTelebieri DROP COLUMN IF EXISTS IcazeTarixi;");
            m.Sql("ALTER TABLE JetonRedimTelebieri DROP COLUMN IF EXISTS BaslamaSaati;");
            m.Sql("ALTER TABLE JetonRedimTelebieri DROP COLUMN IF EXISTS BitisSaati;");
            m.Sql("ALTER TABLE JetonRedimTelebieri DROP COLUMN IF EXISTS RehberTesdiq;");
            m.Sql("ALTER TABLE JetonRedimTelebieri DROP COLUMN IF EXISTS RehberUserId;");
            m.Sql("ALTER TABLE JetonRedimTelebieri DROP COLUMN IF EXISTS RehberTesdiqTarixi;");
            m.Sql("ALTER TABLE JetonRedimTelebieri DROP COLUMN IF EXISTS RehberQeyd;");
        }
    }
}
