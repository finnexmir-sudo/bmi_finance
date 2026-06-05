using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260602150000_AddBirbasaOdenishliToJetonTeyinati")]
    public partial class AddBirbasaOdenishliToJetonTeyinati : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'BirbasaOdenishli'
                      AND Object_ID = Object_ID(N'JetonTeyinatlari')
                )
                BEGIN
                    ALTER TABLE [dbo].[JetonTeyinatlari]
                    ADD [BirbasaOdenishli] BIT NOT NULL DEFAULT 0;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'BirbasaOdenishli'
                      AND Object_ID = Object_ID(N'JetonTeyinatlari')
                )
                BEGIN
                    ALTER TABLE [dbo].[JetonTeyinatlari]
                    DROP COLUMN [BirbasaOdenishli];
                END
            ");
        }
    }
}
