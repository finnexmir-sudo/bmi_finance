using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260506120000_AddSirketBudcesi")]
    public partial class AddSirketBudcesi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.tables WHERE name = 'SirketBudceleri'
                )
                BEGIN
                    CREATE TABLE SirketBudceleri (
                        Id          INT IDENTITY(1,1) PRIMARY KEY,
                        Il          INT            NOT NULL,
                        Mebleg      DECIMAL(18,2)  NOT NULL DEFAULT 0,
                        Qeyd        NVARCHAR(500)  NULL,
                        Silinib     BIT            NOT NULL DEFAULT 0,
                        YaradilmaTarixi DATETIME2  NOT NULL DEFAULT GETDATE()
                    );

                    CREATE UNIQUE INDEX UX_SirketBudceleri_Il
                        ON SirketBudceleri(Il)
                        WHERE Silinib = 0;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS SirketBudceleri;");
        }
    }
}
