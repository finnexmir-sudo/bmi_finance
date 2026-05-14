using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260514120000_ExpandFealiyyetJurnaliAciqlamaColumn")]
    public partial class ExpandFealiyyetJurnaliAciqlamaColumn : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            // Acıqlama sütununun ölçüsünü NVARCHAR(MAX)-a genişləndir
            // (sahə dəyişiklik detalları üçün 500 simvol kifayət etmir)
            m.Sql("ALTER TABLE [FealiyyetJurnali] ALTER COLUMN [Acıqlama] NVARCHAR(MAX) NULL;");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("ALTER TABLE [FealiyyetJurnali] ALTER COLUMN [Acıqlama] NVARCHAR(500) NULL;");
        }
    }
}
