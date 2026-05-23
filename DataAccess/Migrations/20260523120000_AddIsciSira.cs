using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İşçilərə xüsusi göstərmə sırası — "İşçi Sıralaması" səhifəsində
    /// drag-and-drop ilə təyin olunur, bütün siyahılar (Maaş, Bonus/Overtime,
    /// TopluHesabla və s.) bu sıraya görə düzülür.
    /// İdempotent — sütun mövcuddursa heç nə etmir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260523120000_AddIsciSira")]
    public partial class AddIsciSira : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'Sira' AND Object_ID = Object_ID(N'dbo.Isciler')
                )
                BEGIN
                    ALTER TABLE [dbo].[Isciler] ADD [Sira] INT NOT NULL DEFAULT 0;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.columns
                    WHERE Name = N'Sira' AND Object_ID = Object_ID(N'dbo.Isciler')
                )
                BEGIN
                    ALTER TABLE [dbo].[Isciler] DROP COLUMN [Sira];
                END
            ");
        }
    }
}
