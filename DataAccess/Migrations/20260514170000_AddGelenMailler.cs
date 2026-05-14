using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddGelenMailler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GelenMailler')
                BEGIN
                    CREATE TABLE [GelenMailler] (
                        [Id]                    INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [MessageId]             NVARCHAR(500)   NOT NULL,
                        [KimdenAd]              NVARCHAR(200)   NOT NULL DEFAULT '',
                        [KimdenEmail]           NVARCHAR(320)   NOT NULL,
                        [Movzu]                 NVARCHAR(1000)  NOT NULL DEFAULT '',
                        [MetinHtml]             NVARCHAR(MAX)   NOT NULL DEFAULT '',
                        [MetinDuz]              NVARCHAR(MAX)   NOT NULL DEFAULT '',
                        [AlinmaTarixi]          DATETIME2       NOT NULL,
                        [Oxunub]                BIT             NOT NULL DEFAULT 0,
                        [OxunmaTarixi]          DATETIME2       NULL,
                        [AIXulase]              NVARCHAR(MAX)   NULL,
                        [AITahlilTarixi]        DATETIME2       NULL,
                        [CavabVerildi]          BIT             NOT NULL DEFAULT 0,
                        [SenedId]               INT             NULL,
                        [TapalanIsciId]         INT             NULL,
                        [TapalanIsciTarafindan] INT             NULL,
                        [TapalanTarix]          DATETIME2       NULL,
                        [TapalanQeyd]           NVARCHAR(1000)  NULL,
                        [YaradilmaTarixi]       DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YenilenmeTarixi]       DATETIME2       NULL,
                        [SilinmeTarixi]         DATETIME2       NULL,
                        [YaradanIcraciId]       INT             NULL,
                        [YenileyenIcraciId]     INT             NULL,
                        [SilenIcraciId]         INT             NULL,
                        [Silinib]               BIT             NOT NULL DEFAULT 0,

                        CONSTRAINT [FK_GelenMailler_Isciler_TapalanIsciId]
                            FOREIGN KEY ([TapalanIsciId]) REFERENCES [Isciler]([Id]) ON DELETE SET NULL
                    );

                    CREATE UNIQUE INDEX [IX_GelenMailler_MessageId] ON [GelenMailler] ([MessageId]);
                    CREATE INDEX [IX_GelenMailler_AlinmaTarixi] ON [GelenMailler] ([AlinmaTarixi] DESC);
                    CREATE INDEX [IX_GelenMailler_Oxunub] ON [GelenMailler] ([Oxunub]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GelenMailQosmalar')
                BEGIN
                    CREATE TABLE [GelenMailQosmalar] (
                        [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [GelenMailId]       INT             NOT NULL,
                        [FaylAdi]           NVARCHAR(500)   NOT NULL,
                        [ContentType]       NVARCHAR(200)   NOT NULL DEFAULT '',
                        [OlcuBayt]          BIGINT          NOT NULL DEFAULT 0,
                        [FaylYolu]          NVARCHAR(1000)  NOT NULL DEFAULT '',
                        [CixarilmisMetin]   NVARCHAR(MAX)   NULL,
                        [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YenilenmeTarixi]   DATETIME2       NULL,
                        [SilinmeTarixi]     DATETIME2       NULL,
                        [YaradanIcraciId]   INT             NULL,
                        [YenileyenIcraciId] INT             NULL,
                        [SilenIcraciId]     INT             NULL,
                        [Silinib]           BIT             NOT NULL DEFAULT 0,

                        CONSTRAINT [FK_GelenMailQosmalar_GelenMailler]
                            FOREIGN KEY ([GelenMailId]) REFERENCES [GelenMailler]([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_GelenMailQosmalar_GelenMailId] ON [GelenMailQosmalar] ([GelenMailId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('GelenMailQosmalar') IS NOT NULL DROP TABLE [GelenMailQosmalar];");
            migrationBuilder.Sql("IF OBJECT_ID('GelenMailler') IS NOT NULL DROP TABLE [GelenMailler];");
        }
    }
}
