using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521160000_AddOracleSorgu")]
    public partial class AddOracleSorgu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OracleSorgular')
                BEGIN
                    CREATE TABLE [OracleSorgular] (
                        [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [SorguAdi]          NVARCHAR(200)   NOT NULL DEFAULT '',
                        [Mahiyyet]          NVARCHAR(500)   NULL,
                        [SorguMetni]        NVARCHAR(MAX)   NOT NULL DEFAULT '',
                        [Aktiv]             BIT             NOT NULL DEFAULT 1,
                        [DepartamentId]     INT             NOT NULL,
                        [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YenilenmeTarixi]   DATETIME2       NULL,
                        [SilinmeTarixi]     DATETIME2       NULL,
                        [YaradanIcraciId]   INT             NULL,
                        [YenileyenIcraciId] INT             NULL,
                        [SilenIcraciId]     INT             NULL,
                        [Silinib]           BIT             NOT NULL DEFAULT 0,
                        CONSTRAINT [FK_OracleSorgular_Departamentler] FOREIGN KEY ([DepartamentId])
                            REFERENCES [Departamentler] ([Id])
                    );

                    CREATE INDEX [IX_OracleSorgular_DepartamentId] ON [OracleSorgular] ([DepartamentId]);
                    CREATE INDEX [IX_OracleSorgular_Aktiv]         ON [OracleSorgular] ([Aktiv]);
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DROP TABLE IF EXISTS [OracleSorgular];");
        }
    }
}
