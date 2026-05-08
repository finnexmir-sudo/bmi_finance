using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260508010000_AddNaharNezereAlinmasinToIcaze")]
    public partial class AddNaharNezereAlinmasinToIcaze : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Icazeler' AND COLUMN_NAME = 'NaharNezereAlinmasin')
    ALTER TABLE Icazeler ADD NaharNezereAlinmasin BIT NOT NULL DEFAULT 0;
");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("ALTER TABLE Icazeler DROP COLUMN NaharNezereAlinmasin");
        }
    }
}
