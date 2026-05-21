using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260518110000_AddHRQanunFayl")]
    public partial class AddHRQanunFayl : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HRQanunFayllar')
                BEGIN
                    CREATE TABLE [HRQanunFayllar] (
                        [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [Ad]                NVARCHAR(300)   NOT NULL DEFAULT '',
                        [Kateqoriya]        NVARCHAR(50)    NOT NULL DEFAULT '',
                        [FaylYolu]          NVARCHAR(500)   NOT NULL DEFAULT '',
                        [MetnContent]       NVARCHAR(MAX)   NOT NULL DEFAULT '',
                        [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YenilenmeTarixi]   DATETIME2       NULL,
                        [SilinmeTarixi]     DATETIME2       NULL,
                        [YaradanIcraciId]   INT             NULL,
                        [YenileyenIcraciId] INT             NULL,
                        [SilenIcraciId]     INT             NULL,
                        [Silinib]           BIT             NOT NULL DEFAULT 0
                    );

                    CREATE INDEX [IX_HRQanunFayllar_Kateqoriya]      ON [HRQanunFayllar] ([Kateqoriya]);
                    CREATE INDEX [IX_HRQanunFayllar_YaradilmaTarixi] ON [HRQanunFayllar] ([YaradilmaTarixi] DESC);
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DROP TABLE IF EXISTS [HRQanunFayllar];");
        }
    }
}
