using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507230000_AddIsciFeish")]
    public partial class AddIsciFeish : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Isciler' AND COLUMN_NAME = 'FesihSebebi')
    ALTER TABLE Isciler ADD FesihSebebi NVARCHAR(500) NULL;
");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("ALTER TABLE Isciler DROP COLUMN FesihSebebi");
        }
    }
}
