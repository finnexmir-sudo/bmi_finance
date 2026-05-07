using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507200000_AddReytingModulu")]
    public partial class AddReytingModulu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ReytingKateqoriyalari')
BEGIN
    CREATE TABLE ReytingKateqoriyalari (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        Ad                  NVARCHAR(50)      NOT NULL,
        RengKodu            NVARCHAR(20)      NULL,
        MinXal              INT               NOT NULL DEFAULT 0,
        MaxXal              INT               NOT NULL DEFAULT 9999,
        PulAmsali           DECIMAL(5,2)      NOT NULL DEFAULT 1.00,
        SaatAmsali          DECIMAL(5,2)      NOT NULL DEFAULT 1.00,
        Sira                INT               NOT NULL DEFAULT 99,
        Aktivdir            BIT               NOT NULL DEFAULT 1,
        YaradilmaTarixi     DATETIME2         NOT NULL DEFAULT GETDATE(),
        YaradanIcraciId     INT               NULL,
        YenileyenIcraciId   INT               NULL,
        SilenIcraciId       INT               NULL,
        YenilenmeTarixi     DATETIME2         NULL,
        Silinib             BIT               NOT NULL DEFAULT 0,
        SilinmeTarixi       DATETIME2         NULL
    );
END
");

            // Patch tables that were already created without the BaseEntity audit columns
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'YaradanIcraciId')
    ALTER TABLE ReytingKateqoriyalari ADD YaradanIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'YenileyenIcraciId')
    ALTER TABLE ReytingKateqoriyalari ADD YenileyenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'SilenIcraciId')
    ALTER TABLE ReytingKateqoriyalari ADD SilenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'YenilenmeTarixi')
    ALTER TABLE ReytingKateqoriyalari ADD YenilenmeTarixi DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingKateqoriyalari' AND COLUMN_NAME = 'SilinmeTarixi')
    ALTER TABLE ReytingKateqoriyalari ADD SilinmeTarixi DATETIME2 NULL;
");

            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ReytingParametrleri')
BEGIN
    CREATE TABLE ReytingParametrleri (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        HadiseNovu          INT               NOT NULL,
        Ad                  NVARCHAR(150)     NOT NULL,
        XalDeyeri           INT               NOT NULL DEFAULT 0,
        Aktivdir            BIT               NOT NULL DEFAULT 1,
        YaradilmaTarixi     DATETIME2         NOT NULL DEFAULT GETDATE(),
        YaradanIcraciId     INT               NULL,
        YenileyenIcraciId   INT               NULL,
        SilenIcraciId       INT               NULL,
        YenilenmeTarixi     DATETIME2         NULL,
        Silinib             BIT               NOT NULL DEFAULT 0,
        SilinmeTarixi       DATETIME2         NULL
    );
END
");

            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'YaradanIcraciId')
    ALTER TABLE ReytingParametrleri ADD YaradanIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'YenileyenIcraciId')
    ALTER TABLE ReytingParametrleri ADD YenileyenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'SilenIcraciId')
    ALTER TABLE ReytingParametrleri ADD SilenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'YenilenmeTarixi')
    ALTER TABLE ReytingParametrleri ADD YenilenmeTarixi DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ReytingParametrleri' AND COLUMN_NAME = 'SilinmeTarixi')
    ALTER TABLE ReytingParametrleri ADD SilinmeTarixi DATETIME2 NULL;
");

            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IsciReytinqleri')
BEGIN
    CREATE TABLE IsciReytinqleri (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        IsciId              INT               NOT NULL,
        Il                  INT               NOT NULL,
        Ay                  INT               NOT NULL,
        AvtomatikXal        INT               NOT NULL DEFAULT 0,
        ManualXal           INT               NOT NULL DEFAULT 0,
        CemiXal             INT               NOT NULL DEFAULT 0,
        KateqoriyaId        INT               NULL,
        HesablamaTarixi     DATETIME2         NOT NULL DEFAULT GETDATE(),
        YaradilmaTarixi     DATETIME2         NOT NULL DEFAULT GETDATE(),
        YaradanIcraciId     INT               NULL,
        YenileyenIcraciId   INT               NULL,
        SilenIcraciId       INT               NULL,
        YenilenmeTarixi     DATETIME2         NULL,
        Silinib             BIT               NOT NULL DEFAULT 0,
        SilinmeTarixi       DATETIME2         NULL,
        CONSTRAINT FK_IsciReytinqleri_Isciler
            FOREIGN KEY (IsciId) REFERENCES Isciler(Id),
        CONSTRAINT FK_IsciReytinqleri_Kateqoriya
            FOREIGN KEY (KateqoriyaId) REFERENCES ReytingKateqoriyalari(Id),
        CONSTRAINT UQ_IsciReytinq_IsciIlAy
            UNIQUE (IsciId, Il, Ay)
    );
END
");

            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'YaradanIcraciId')
    ALTER TABLE IsciReytinqleri ADD YaradanIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'YenileyenIcraciId')
    ALTER TABLE IsciReytinqleri ADD YenileyenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'SilenIcraciId')
    ALTER TABLE IsciReytinqleri ADD SilenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'YenilenmeTarixi')
    ALTER TABLE IsciReytinqleri ADD YenilenmeTarixi DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsciReytinqleri' AND COLUMN_NAME = 'SilinmeTarixi')
    ALTER TABLE IsciReytinqleri ADD SilinmeTarixi DATETIME2 NULL;
");

            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ManualReytingQeydleri')
BEGIN
    CREATE TABLE ManualReytingQeydleri (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        IsciReytinqiId      INT               NOT NULL,
        Xal                 INT               NOT NULL DEFAULT 0,
        Sebeb               NVARCHAR(500)     NOT NULL,
        ElavEdenUserId      INT               NOT NULL DEFAULT 0,
        Tarix               DATETIME2         NOT NULL DEFAULT GETDATE(),
        YaradilmaTarixi     DATETIME2         NOT NULL DEFAULT GETDATE(),
        YaradanIcraciId     INT               NULL,
        YenileyenIcraciId   INT               NULL,
        SilenIcraciId       INT               NULL,
        YenilenmeTarixi     DATETIME2         NULL,
        Silinib             BIT               NOT NULL DEFAULT 0,
        SilinmeTarixi       DATETIME2         NULL,
        CONSTRAINT FK_ManualReytingQeydleri_IsciReytinqleri
            FOREIGN KEY (IsciReytinqiId) REFERENCES IsciReytinqleri(Id) ON DELETE CASCADE
    );
END
");

            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'YaradanIcraciId')
    ALTER TABLE ManualReytingQeydleri ADD YaradanIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'YenileyenIcraciId')
    ALTER TABLE ManualReytingQeydleri ADD YenileyenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'SilenIcraciId')
    ALTER TABLE ManualReytingQeydleri ADD SilenIcraciId INT NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'YenilenmeTarixi')
    ALTER TABLE ManualReytingQeydleri ADD YenilenmeTarixi DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ManualReytingQeydleri' AND COLUMN_NAME = 'SilinmeTarixi')
    ALTER TABLE ManualReytingQeydleri ADD SilinmeTarixi DATETIME2 NULL;
");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DROP TABLE IF EXISTS ManualReytingQeydleri");
            m.Sql("DROP TABLE IF EXISTS IsciReytinqleri");
            m.Sql("DROP TABLE IF EXISTS ReytingParametrleri");
            m.Sql("DROP TABLE IF EXISTS ReytingKateqoriyalari");
        }
    }
}
