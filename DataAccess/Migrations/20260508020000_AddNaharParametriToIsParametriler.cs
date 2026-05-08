using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260508020000_AddNaharParametriToIsParametriler")]
    public partial class AddNaharParametriToIsParametriler : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsParametriler' AND COLUMN_NAME = 'NaharBaslamaSaati')
    ALTER TABLE IsParametriler ADD NaharBaslamaSaati TIME NOT NULL DEFAULT '13:00:00';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IsParametriler' AND COLUMN_NAME = 'NaharMuddetDeqiqe')
    ALTER TABLE IsParametriler ADD NaharMuddetDeqiqe INT NOT NULL DEFAULT 45;
");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql(@"
ALTER TABLE IsParametriler DROP COLUMN NaharBaslamaSaati;
ALTER TABLE IsParametriler DROP COLUMN NaharMuddetDeqiqe;
");
        }
    }
}
