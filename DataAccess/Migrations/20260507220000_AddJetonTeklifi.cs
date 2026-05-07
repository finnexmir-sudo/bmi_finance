using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507220000_AddJetonTeklifi")]
    public partial class AddJetonTeklifi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JetonTeklifleri')
                BEGIN
                    CREATE TABLE [JetonTeklifleri] (
                        [Id]                INT IDENTITY(1,1) NOT NULL,
                        [IsciId]            INT NOT NULL,
                        [TeklifNovu]        INT NOT NULL,
                        [Metn]              NVARCHAR(500) NOT NULL,
                        [ElaveMelumat]      NVARCHAR(1000) NULL,
                        [Status]            INT NOT NULL DEFAULT 1,
                        [IslemEdenUserId]   INT NULL,
                        [IslemTarixi]       DATETIME2 NULL,
                        [YaradilmaTarixi]   DATETIME2 NOT NULL DEFAULT GETDATE(),
                        [YaradanIcraciId]   INT NULL,
                        [YenileyenIcraciId] INT NULL,
                        [SilenIcraciId]     INT NULL,
                        [YenilenmeTarixi]   DATETIME2 NULL,
                        [Silinib]           BIT NOT NULL DEFAULT 0,
                        [SilinmeTarixi]     DATETIME2 NULL,
                        CONSTRAINT [PK_JetonTeklifleri] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_JetonTeklifleri_Isciler_IsciId]
                            FOREIGN KEY ([IsciId]) REFERENCES [Isciler] ([Id]) ON DELETE NO ACTION
                    );
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'JetonTeklifleri')
                BEGIN
                    DROP TABLE [JetonTeklifleri];
                END
            ");
        }
    }
}
