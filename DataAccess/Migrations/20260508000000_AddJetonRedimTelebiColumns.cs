using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260508000000_AddJetonRedimTelebiColumns")]
    public partial class AddJetonRedimTelebiColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'JetonRedimTelebieri')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'IcazeTarixi')
        ALTER TABLE [JetonRedimTelebieri] ADD [IcazeTarixi] DATETIME2 NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'BaslamaSaati')
        ALTER TABLE [JetonRedimTelebieri] ADD [BaslamaSaati] TIME NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'BitisSaati')
        ALTER TABLE [JetonRedimTelebieri] ADD [BitisSaati] TIME NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberTesdiq')
        ALTER TABLE [JetonRedimTelebieri] ADD [RehberTesdiq] BIT NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberUserId')
        ALTER TABLE [JetonRedimTelebieri] ADD [RehberUserId] INT NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberTesdiqTarixi')
        ALTER TABLE [JetonRedimTelebieri] ADD [RehberTesdiqTarixi] DATETIME2 NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberQeyd')
        ALTER TABLE [JetonRedimTelebieri] ADD [RehberQeyd] NVARCHAR(MAX) NULL;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'JetonRedimTelebieri')
BEGIN
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberQeyd')
        ALTER TABLE [JetonRedimTelebieri] DROP COLUMN [RehberQeyd];
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberTesdiqTarixi')
        ALTER TABLE [JetonRedimTelebieri] DROP COLUMN [RehberTesdiqTarixi];
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberUserId')
        ALTER TABLE [JetonRedimTelebieri] DROP COLUMN [RehberUserId];
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'RehberTesdiq')
        ALTER TABLE [JetonRedimTelebieri] DROP COLUMN [RehberTesdiq];
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'BitisSaati')
        ALTER TABLE [JetonRedimTelebieri] DROP COLUMN [BitisSaati];
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'BaslamaSaati')
        ALTER TABLE [JetonRedimTelebieri] DROP COLUMN [BaslamaSaati];
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JetonRedimTelebieri' AND COLUMN_NAME = 'IcazeTarixi')
        ALTER TABLE [JetonRedimTelebieri] DROP COLUMN [IcazeTarixi];
END");
        }
    }
}
