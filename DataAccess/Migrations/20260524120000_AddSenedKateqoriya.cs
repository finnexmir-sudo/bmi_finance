using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Sənəd Dövriyyəsinə Umumi / Xususi kateqoriyası — default Umumi.
    /// İdempotent: sütun mövcuddursa heç nə etmir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260524120000_AddSenedKateqoriya")]
    public partial class AddSenedKateqoriya : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'Kateqoriya' AND Object_ID = Object_ID(N'dbo.Senedler')
                )
                BEGIN
                    ALTER TABLE [dbo].[Senedler] ADD [Kateqoriya] INT NOT NULL DEFAULT 1;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'Kateqoriya' AND Object_ID = Object_ID(N'dbo.Senedler')
                )
                BEGIN
                    ALTER TABLE [dbo].[Senedler] DROP COLUMN [Kateqoriya];
                END
            ");
        }
    }
}
