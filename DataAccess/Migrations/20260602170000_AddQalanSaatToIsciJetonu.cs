using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260602170000_AddQalanSaatToIsciJetonu")]
    public partial class AddQalanSaatToIsciJetonu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'QalanSaat'
                      AND Object_ID = Object_ID(N'IsciJetonlari')
                )
                BEGIN
                    ALTER TABLE [dbo].[IsciJetonlari]
                    ADD [QalanSaat] DECIMAL(18,4) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'QalanSaat'
                      AND Object_ID = Object_ID(N'IsciJetonlari')
                )
                BEGIN
                    ALTER TABLE [dbo].[IsciJetonlari]
                    DROP COLUMN [QalanSaat];
                END
            ");
        }
    }
}
