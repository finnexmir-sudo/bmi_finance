using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260514140000_AddTeklifler")]
    public partial class AddTeklifler : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Teklifler')
BEGIN
    CREATE TABLE Teklifler (
        Id                  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IsciId              INT NOT NULL,
        Nov                 INT NOT NULL DEFAULT 0,
        Prioritet           INT NOT NULL DEFAULT 1,
        Status              INT NOT NULL DEFAULT 0,
        Bashliq             NVARCHAR(200) NOT NULL,
        Mezmun              NVARCHAR(MAX) NOT NULL,
        Cavab               NVARCHAR(MAX) NULL,
        CavabVerenIsciId    INT NULL,
        CavabTarixi         DATETIME2 NULL,
        YaradilmaTarixi     DATETIME2 NOT NULL DEFAULT GETDATE(),
        YaradanIcraciId     INT NULL,
        YenileyenIcraciId   INT NULL,
        SilenIcraciId       INT NULL,
        YenilenmeTarixi     DATETIME2 NULL,
        Silinib             BIT NOT NULL DEFAULT 0,
        SilinmeTarixi       DATETIME2 NULL,
        CONSTRAINT FK_Teklifler_Isciler_IsciId
            FOREIGN KEY (IsciId) REFERENCES Isciler(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Teklifler_Isciler_CavabVeren
            FOREIGN KEY (CavabVerenIsciId) REFERENCES Isciler(Id)
    );

    CREATE INDEX IX_Teklifler_IsciId ON Teklifler(IsciId);
    CREATE INDEX IX_Teklifler_Status ON Teklifler(Status);
    CREATE INDEX IX_Teklifler_Tarix  ON Teklifler(YaradilmaTarixi DESC);
END
");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DROP TABLE IF EXISTS Teklifler;");
        }
    }
}
