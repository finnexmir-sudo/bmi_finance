using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260602180000_AddIcazeIdToIsciJetonu")]
    public partial class AddIcazeIdToIsciJetonu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'IcazeId'
                      AND Object_ID = Object_ID(N'IsciJetonlari')
                )
                BEGIN
                    ALTER TABLE [dbo].[IsciJetonlari]
                    ADD [IcazeId] INT NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'IcazeId'
                      AND Object_ID = Object_ID(N'IsciJetonlari')
                )
                BEGIN
                    ALTER TABLE [dbo].[IsciJetonlari]
                    DROP COLUMN [IcazeId];
                END
            ");
        }
    }
}
