using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610090000_AddEzamiyyetGeriDonusQeydi")]
    public partial class AddEzamiyyetGeriDonusQeydi : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME  = 'EzamiyyetMuracietler'
                      AND COLUMN_NAME = 'GeriDonusQeydi')
                BEGIN
                    ALTER TABLE [EzamiyyetMuracietler]
                    ADD [GeriDonusQeydi] NVARCHAR(2000) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME  = 'EzamiyyetMuracietler'
                      AND COLUMN_NAME = 'GeriDonusQeydi')
                BEGIN
                    ALTER TABLE [EzamiyyetMuracietler]
                    DROP COLUMN [GeriDonusQeydi];
                END
            ");
        }
    }
}
