using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    public partial class AddFealiyyetJurnali : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FealiyyetJurnali')
                BEGIN
                    CREATE TABLE FealiyyetJurnali (
                        Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        UserId     INT NULL,
                        Emeliyyat  NVARCHAR(1) NOT NULL,
                        CedvelAdi  NVARCHAR(100) NOT NULL,
                        CedvelFarsi NVARCHAR(100) NOT NULL,
                        RecordId   INT NOT NULL DEFAULT 0,
                        Acıqlama   NVARCHAR(500) NULL,
                        Tarix      DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_FealiyyetJurnali_Users FOREIGN KEY (UserId)
                            REFERENCES AspNetUsers(Id) ON DELETE SET NULL
                    );
                    CREATE INDEX IX_FealiyyetJurnali_UserId  ON FealiyyetJurnali(UserId);
                    CREATE INDEX IX_FealiyyetJurnali_Tarix   ON FealiyyetJurnali(Tarix DESC);
                    CREATE INDEX IX_FealiyyetJurnali_Cedvel  ON FealiyyetJurnali(CedvelAdi);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS FealiyyetJurnali;");
        }
    }
}
