using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    public partial class AddReytingModulu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ReytingKateqoriyalari')
BEGIN
    CREATE TABLE ReytingKateqoriyalari (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        Ad           NVARCHAR(50)      NOT NULL,
        RengKodu     NVARCHAR(20)      NULL,
        MinXal       INT               NOT NULL DEFAULT 0,
        MaxXal       INT               NOT NULL DEFAULT 9999,
        PulAmsali    DECIMAL(5,2)      NOT NULL DEFAULT 1.00,
        SaatAmsali   DECIMAL(5,2)      NOT NULL DEFAULT 1.00,
        Sira         INT               NOT NULL DEFAULT 99,
        Aktivdir     BIT               NOT NULL DEFAULT 1,
        YaradilmaTarixi DATETIME2     NOT NULL DEFAULT GETDATE(),
        Silinib      BIT               NOT NULL DEFAULT 0
    );
END
");

            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ReytingParametrleri')
BEGIN
    CREATE TABLE ReytingParametrleri (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        HadiseNovu  INT           NOT NULL,
        Ad          NVARCHAR(150) NOT NULL,
        XalDeyeri   INT           NOT NULL DEFAULT 0,
        Aktivdir    BIT           NOT NULL DEFAULT 1,
        YaradilmaTarixi DATETIME2 NOT NULL DEFAULT GETDATE(),
        Silinib     BIT           NOT NULL DEFAULT 0
    );
END
");

            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IsciReytinqleri')
BEGIN
    CREATE TABLE IsciReytinqleri (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        IsciId              INT          NOT NULL,
        Il                  INT          NOT NULL,
        Ay                  INT          NOT NULL,
        AvtomatikXal        INT          NOT NULL DEFAULT 0,
        ManualXal           INT          NOT NULL DEFAULT 0,
        CemiXal             INT          NOT NULL DEFAULT 0,
        KateqoriyaId        INT          NULL,
        HesablamaTarixi     DATETIME2    NOT NULL DEFAULT GETDATE(),
        YaradilmaTarixi     DATETIME2    NOT NULL DEFAULT GETDATE(),
        Silinib             BIT          NOT NULL DEFAULT 0,
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
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ManualReytingQeydleri')
BEGIN
    CREATE TABLE ManualReytingQeydleri (
        Id                INT IDENTITY(1,1) PRIMARY KEY,
        IsciReytinqiId    INT           NOT NULL,
        Xal               INT           NOT NULL DEFAULT 0,
        Sebeb             NVARCHAR(500) NOT NULL,
        ElavEdenUserId    INT           NOT NULL DEFAULT 0,
        Tarix             DATETIME2     NOT NULL DEFAULT GETDATE(),
        YaradilmaTarixi   DATETIME2     NOT NULL DEFAULT GETDATE(),
        Silinib           BIT           NOT NULL DEFAULT 0,
        CONSTRAINT FK_ManualReytingQeydleri_IsciReytinqleri
            FOREIGN KEY (IsciReytinqiId) REFERENCES IsciReytinqleri(Id) ON DELETE CASCADE
    );
END
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
