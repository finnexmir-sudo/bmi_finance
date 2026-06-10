using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İstifadə edilməmiş əmək məzuniyyəti günlərinə görə kompensasiya cədvəli.
    /// İdempotentdir — artıq mövcuddursa heç nə etmir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260524130000_AddMezuniyyetKompensasiyasi")]
    public partial class AddMezuniyyetKompensasiyasi : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MezuniyyetKompensasiyalari')
                BEGIN
                    CREATE TABLE [dbo].[MezuniyyetKompensasiyalari] (
                        [Id]                              INT            NOT NULL IDENTITY(1,1),
                        [IsciId]                          INT            NOT NULL,
                        [AyrilmaTarixi]                   DATETIME2      NOT NULL,
                        [SonuncuMezuniyyetBitmeTarixi]    DATETIME2      NULL,
                        [SonuncuMezuniyyetId]             INT            NULL,
                        [KecenGunSayi]                    INT            NOT NULL DEFAULT 0,
                        [KecmisQaligGun]                  DECIMAL(10,2)  NOT NULL DEFAULT 0,
                        [CariIlProrateGun]                DECIMAL(10,2)  NOT NULL DEFAULT 0,
                        [CemiKompensasiyaGun]             DECIMAL(10,2)  NOT NULL DEFAULT 0,
                        [Son12AyDuzelmisQazanc]           DECIMAL(18,2)  NOT NULL DEFAULT 0,
                        [GunlukMezPul]                    DECIMAL(18,4)  NOT NULL DEFAULT 0,
                        [GunlukMaas]                      DECIMAL(18,4)  NOT NULL DEFAULT 0,
                        [GunlukRate]                      DECIMAL(18,4)  NOT NULL DEFAULT 0,
                        [CemiMebleg]                      DECIMAL(18,2)  NOT NULL DEFAULT 0,
                        [HesablananIl]                    INT            NOT NULL,
                        [HesablananAy]                    INT            NOT NULL,
                        [MaasId]                          INT            NULL,
                        [Status]                          INT            NOT NULL DEFAULT 1,
                        [Qeyd]                            NVARCHAR(500)  NULL,
                        [HesablayanIsciId]                INT            NOT NULL DEFAULT 0,
                        [YaradilmaTarixi]                 DATETIME2      NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]                 INT            NULL,
                        [YenileyenIcraciId]               INT            NULL,
                        [SilenIcraciId]                   INT            NULL,
                        [YenilenmeTarixi]                 DATETIME2      NULL,
                        [Silinib]                         BIT            NOT NULL DEFAULT 0,
                        [SilinmeTarixi]                   DATETIME2      NULL,
                        CONSTRAINT [PK_MezuniyyetKompensasiyalari] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_MezuniyyetKompensasiyalari_Isciler]
                            FOREIGN KEY ([IsciId]) REFERENCES [dbo].[Isciler]([Id])
                    );

                    -- Hər (İşçi, İl, Ay) üçün yalnız bir aktiv kompensasiya qeydi
                    CREATE UNIQUE INDEX [IX_MezKomp_Isci_Il_Ay]
                        ON [dbo].[MezuniyyetKompensasiyalari] ([IsciId], [HesablananIl], [HesablananAy])
                        WHERE [Silinib] = 0 AND [Status] != 99;

                    CREATE INDEX [IX_MezKomp_Status]
                        ON [dbo].[MezuniyyetKompensasiyalari] ([Status]);
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DROP TABLE IF EXISTS [dbo].[MezuniyyetKompensasiyalari];");
        }
    }
}
