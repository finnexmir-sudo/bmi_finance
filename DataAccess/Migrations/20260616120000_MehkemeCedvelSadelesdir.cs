using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260616120000_MehkemeCedvelSadelesdir")]
    public partial class MehkemeCedvelSadelesdir : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            // Məhkəmə İşləri sadələşdirildi: girov növü + iş nömrəsi + hakim sütunları.
            // (Köhnə sütunlar — Status, KreditNovu, KreditHesabi, Subkod, MehkemeSenedi,
            //  QetnameTarixi, Qeyd — silinmir; sadəcə istifadədən çıxır.)
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeCedveller' AND COLUMN_NAME='GirovunNovu')
                    ALTER TABLE [MehkemeCedveller] ADD [GirovunNovu] NVARCHAR(MAX) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeCedveller' AND COLUMN_NAME='MehkemeIsNomresi')
                    ALTER TABLE [MehkemeCedveller] ADD [MehkemeIsNomresi] NVARCHAR(MAX) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeCedveller' AND COLUMN_NAME='Hakim')
                    ALTER TABLE [MehkemeCedveller] ADD [Hakim] NVARCHAR(MAX) NULL;
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeCedveller' AND COLUMN_NAME='GirovunNovu') ALTER TABLE [MehkemeCedveller] DROP COLUMN [GirovunNovu];
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeCedveller' AND COLUMN_NAME='MehkemeIsNomresi') ALTER TABLE [MehkemeCedveller] DROP COLUMN [MehkemeIsNomresi];
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MehkemeCedveller' AND COLUMN_NAME='Hakim') ALTER TABLE [MehkemeCedveller] DROP COLUMN [Hakim];
            ");
        }
    }
}
