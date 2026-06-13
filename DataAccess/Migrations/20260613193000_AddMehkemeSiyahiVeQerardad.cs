using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260613193000_AddMehkemeSiyahiVeQerardad")]
    public partial class AddMehkemeSiyahiVeQerardad : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            // MehkemeIsleri — kompozit açarın 2-ci hissəsi (subschkre / K.S.)
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME = 'MehkemeIsleri' AND COLUMN_NAME = 'KreditSubHesab')
                BEGIN
                    ALTER TABLE [MehkemeIsleri] ADD [KreditSubHesab] NVARCHAR(10) NULL;
                END
            ");

            // MehkemeIsleri — proqramda redaktə olunan məhkəmə qərardadı
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME = 'MehkemeIsleri' AND COLUMN_NAME = 'Qerardad')
                BEGIN
                    ALTER TABLE [MehkemeIsleri] ADD [Qerardad] NVARCHAR(MAX) NULL;
                END
            ");

            // SistemAyarlari — tam siyahı Oracle sorğusu (parametrsiz)
            m.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_NAME  = 'SistemAyarlari'
                                 AND COLUMN_NAME = 'PidMehkemeSiyahiSorguId')
                BEGIN
                    ALTER TABLE [SistemAyarlari] ADD [PidMehkemeSiyahiSorguId] INT NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'MehkemeIsleri' AND COLUMN_NAME = 'Qerardad')
                    ALTER TABLE [MehkemeIsleri] DROP COLUMN [Qerardad];

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'MehkemeIsleri' AND COLUMN_NAME = 'KreditSubHesab')
                    ALTER TABLE [MehkemeIsleri] DROP COLUMN [KreditSubHesab];

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'SistemAyarlari' AND COLUMN_NAME = 'PidMehkemeSiyahiSorguId')
                    ALTER TABLE [SistemAyarlari] DROP COLUMN [PidMehkemeSiyahiSorguId];
            ");
        }
    }
}
