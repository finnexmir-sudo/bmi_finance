using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260602130000_AddMuqavileYenileme")]
    public partial class AddMuqavileYenileme : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.tables
                    WHERE Name = N'MuqavileYenilemeleri' AND Type = 'U'
                )
                BEGIN
                    CREATE TABLE [dbo].[MuqavileYenilemeleri] (
                        [Id]                INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [IsciId]            INT            NOT NULL,
                        [KohneBitmeTarixi]  DATETIME2      NULL,
                        [YeniBitmeTarixi]   DATETIME2      NOT NULL,
                        [YenilemeTarixi]    DATETIME2      NOT NULL DEFAULT GETDATE(),
                        [Qeyd]              NVARCHAR(500)  NULL,
                        [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]   INT            NULL,
                        [YenileyenIcraciId] INT            NULL,
                        [SilenIcraciId]     INT            NULL,
                        [YenilenmeTarixi]   DATETIME2      NULL,
                        [Silinib]           BIT            NOT NULL DEFAULT 0,
                        [SilinmeTarixi]     DATETIME2      NULL,
                        CONSTRAINT [FK_MuqavileYenilemeleri_Isciler]
                            FOREIGN KEY ([IsciId]) REFERENCES [dbo].[Isciler]([Id])
                    );
                    CREATE INDEX [IX_MuqavileYenilemeleri_IsciId]
                        ON [dbo].[MuqavileYenilemeleri] ([IsciId]);
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.tables
                    WHERE Name = N'MuqavileYenilemeleri' AND Type = 'U'
                )
                BEGIN
                    DROP TABLE [dbo].[MuqavileYenilemeleri];
                END
            ");
        }
    }
}
