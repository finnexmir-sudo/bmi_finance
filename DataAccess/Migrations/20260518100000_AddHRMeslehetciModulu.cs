using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260518100000_AddHRMeslehetciModulu")]
    public partial class AddHRMeslehetciModulu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HRSohbetler')
                BEGIN
                    CREATE TABLE [HRSohbetler] (
                        [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [AppUserId]         INT             NOT NULL,
                        [BaslanmaTarixi]    DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YenilenmeTarixi]   DATETIME2       NULL,
                        [SilinmeTarixi]     DATETIME2       NULL,
                        [YaradanIcraciId]   INT             NULL,
                        [YenileyenIcraciId] INT             NULL,
                        [SilenIcraciId]     INT             NULL,
                        [Silinib]           BIT             NOT NULL DEFAULT 0,

                        CONSTRAINT [FK_HRSohbetler_AspNetUsers_AppUserId]
                            FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_HRSohbetler_AppUserId]        ON [HRSohbetler] ([AppUserId]);
                    CREATE INDEX [IX_HRSohbetler_BaslanmaTarixi]   ON [HRSohbetler] ([BaslanmaTarixi] DESC);
                END
            ");

            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HRSohbetMesajlar')
                BEGIN
                    CREATE TABLE [HRSohbetMesajlar] (
                        [Id]        INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [SohbetId]  INT             NOT NULL,
                        [Rol]       NVARCHAR(20)    NOT NULL DEFAULT 'user',
                        [Metn]      NVARCHAR(MAX)   NOT NULL DEFAULT '',
                        [Tarix]     DATETIME2       NOT NULL DEFAULT GETDATE(),

                        CONSTRAINT [FK_HRSohbetMesajlar_HRSohbetler_SohbetId]
                            FOREIGN KEY ([SohbetId]) REFERENCES [HRSohbetler]([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_HRSohbetMesajlar_SohbetId] ON [HRSohbetMesajlar] ([SohbetId]);
                    CREATE INDEX [IX_HRSohbetMesajlar_Tarix]    ON [HRSohbetMesajlar] ([Tarix] ASC);
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DROP TABLE IF EXISTS [HRSohbetMesajlar];");
            m.Sql("DROP TABLE IF EXISTS [HRSohbetler];");
        }
    }
}
