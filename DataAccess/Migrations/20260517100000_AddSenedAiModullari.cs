using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSenedAiModullari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SenedAnalizler')
                BEGIN
                    CREATE TABLE [SenedAnalizler] (
                        [Id]                    INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [AppUserId]             INT             NOT NULL,
                        [OriginalFileName]      NVARCHAR(500)   NOT NULL DEFAULT '',
                        [OriginalText]          NVARCHAR(MAX)   NOT NULL DEFAULT '',
                        [RiskLevel]             INT             NOT NULL DEFAULT 1,
                        [RiskyClausesJson]      NVARCHAR(MAX)   NOT NULL DEFAULT '[]',
                        [YaradilmaTarixi]       DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YenilenmeTarixi]       DATETIME2       NULL,
                        [SilinmeTarixi]         DATETIME2       NULL,
                        [YaradanIcraciId]       INT             NULL,
                        [YenileyenIcraciId]     INT             NULL,
                        [SilenIcraciId]         INT             NULL,
                        [Silinib]               BIT             NOT NULL DEFAULT 0,

                        CONSTRAINT [FK_SenedAnalizler_AspNetUsers_AppUserId]
                            FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_SenedAnalizler_AppUserId] ON [SenedAnalizler] ([AppUserId]);
                    CREATE INDEX [IX_SenedAnalizler_YaradilmaTarixi] ON [SenedAnalizler] ([YaradilmaTarixi] DESC);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SenedKonstruktorlar')
                BEGIN
                    CREATE TABLE [SenedKonstruktorlar] (
                        [Id]                    INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [AppUserId]             INT             NOT NULL,
                        [SenedNovu]             NVARCHAR(500)   NOT NULL DEFAULT '',
                        [ParametrlerJson]       NVARCHAR(MAX)   NOT NULL DEFAULT '{}',
                        [GeneratedContent]      NVARCHAR(MAX)   NOT NULL DEFAULT '',
                        [YaradilmaTarixi]       DATETIME2       NOT NULL DEFAULT GETDATE(),
                        [YenilenmeTarixi]       DATETIME2       NULL,
                        [SilinmeTarixi]         DATETIME2       NULL,
                        [YaradanIcraciId]       INT             NULL,
                        [YenileyenIcraciId]     INT             NULL,
                        [SilenIcraciId]         INT             NULL,
                        [Silinib]               BIT             NOT NULL DEFAULT 0,

                        CONSTRAINT [FK_SenedKonstruktorlar_AspNetUsers_AppUserId]
                            FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_SenedKonstruktorlar_AppUserId] ON [SenedKonstruktorlar] ([AppUserId]);
                    CREATE INDEX [IX_SenedKonstruktorlar_YaradilmaTarixi] ON [SenedKonstruktorlar] ([YaradilmaTarixi] DESC);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [SenedKonstruktorlar];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [SenedAnalizler];");
        }
    }
}
