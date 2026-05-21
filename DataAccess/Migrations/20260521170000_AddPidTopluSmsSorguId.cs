using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521170000_AddPidTopluSmsSorguId")]
    public partial class AddPidTopluSmsSorguId : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SistemAyarlari')
                BEGIN
                    CREATE TABLE [SistemAyarlari] (
                        [Id]                        INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [KreditImapServer]           NVARCHAR(200)   NOT NULL DEFAULT 'imap.titan.email',
                        [KreditImapPort]             INT             NOT NULL DEFAULT 993,
                        [KreditImapEmail]            NVARCHAR(200)   NOT NULL DEFAULT '',
                        [KreditImapPassword]         NVARCHAR(500)   NOT NULL DEFAULT '',
                        [PidTopluSmsOracleSorguId]   INT             NULL
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                                   WHERE TABLE_NAME = 'SistemAyarlari'
                                     AND COLUMN_NAME = 'PidTopluSmsOracleSorguId')
                    BEGIN
                        ALTER TABLE [SistemAyarlari] ADD [PidTopluSmsOracleSorguId] INT NULL;
                    END
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'SistemAyarlari'
                             AND COLUMN_NAME = 'PidTopluSmsOracleSorguId')
                BEGIN
                    ALTER TABLE [SistemAyarlari] DROP COLUMN [PidTopluSmsOracleSorguId];
                END
            ");
        }
    }
}
