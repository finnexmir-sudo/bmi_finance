using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// SirketBudceleri cədvəlinə BaseEntity-dən gələn əksik sütunları əlavə edir.
    /// İdempotentdir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260506130000_FixSirketBudcesiColumns")]
    public partial class FixSirketBudcesiColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Cədvəl mövcuddursa əskik sütunları əlavə et
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SirketBudceleri')
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'YaradanIcraciId'   AND Object_ID = Object_ID('SirketBudceleri'))
                        ALTER TABLE SirketBudceleri ADD YaradanIcraciId   INT NULL;

                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'YenileyenIcraciId' AND Object_ID = Object_ID('SirketBudceleri'))
                        ALTER TABLE SirketBudceleri ADD YenileyenIcraciId INT NULL;

                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'SilenIcraciId'     AND Object_ID = Object_ID('SirketBudceleri'))
                        ALTER TABLE SirketBudceleri ADD SilenIcraciId     INT NULL;

                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'YenilenmeTarixi'   AND Object_ID = Object_ID('SirketBudceleri'))
                        ALTER TABLE SirketBudceleri ADD YenilenmeTarixi   DATETIME2 NULL;

                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'SilinmeTarixi'     AND Object_ID = Object_ID('SirketBudceleri'))
                        ALTER TABLE SirketBudceleri ADD SilinmeTarixi     DATETIME2 NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
