using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260602190000_AddXerclenmeTarixixToIsciJetonu")]
    public partial class AddXerclenmeTarixixToIsciJetonu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'XerclenmeTarixi'
                      AND Object_ID = Object_ID(N'IsciJetonlari')
                )
                BEGIN
                    ALTER TABLE [dbo].[IsciJetonlari]
                    ADD [XerclenmeTarixi] DATETIME2 NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'XerclenmeTarixi'
                      AND Object_ID = Object_ID(N'IsciJetonlari')
                )
                BEGIN
                    ALTER TABLE [dbo].[IsciJetonlari]
                    DROP COLUMN [XerclenmeTarixi];
                END
            ");
        }
    }
}
