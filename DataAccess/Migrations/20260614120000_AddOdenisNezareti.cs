using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260614120000_AddOdenisNezareti")]
    public partial class AddOdenisNezareti : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OdenisNezaretleri')
                BEGIN
                    CREATE TABLE [OdenisNezaretleri] (
                        [Id]                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [BalansNovu]        INT NOT NULL DEFAULT 0,
                        [MusteriAdi]        NVARCHAR(MAX) NOT NULL,
                        [HesabNomresi]      NVARCHAR(MAX) NULL,
                        [Teyinat]           NVARCHAR(MAX) NULL,
                        [SonOdenisTarixi]   DATETIME2 NULL,
                        [OdenisVeziyyeti]   NVARCHAR(MAX) NULL,
                        [Qeyd]              NVARCHAR(MAX) NULL,
                        [YaradilmaTarixi]   DATETIME2 NOT NULL,
                        [YaradanIcraciId]   INT NULL,
                        [YenileyenIcraciId] INT NULL,
                        [SilenIcraciId]     INT NULL,
                        [YenilenmeTarixi]   DATETIME2 NULL,
                        [Silinib]           BIT NOT NULL DEFAULT 0,
                        [SilinmeTarixi]     DATETIME2 NULL
                    );
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OdenisNezaretleri')
                    DROP TABLE [OdenisNezaretleri];
            ");
        }
    }
}
