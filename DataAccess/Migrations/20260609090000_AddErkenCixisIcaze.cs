using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    public partial class AddErkenCixisIcaze : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ErkenCixisIcazeler')
                BEGIN
                    CREATE TABLE [ErkenCixisIcazeler] (
                        [Id]                INT IDENTITY(1,1) NOT NULL,
                        [IsciId]            INT NOT NULL,
                        [Tarix]             DATE NOT NULL,
                        [IcazeVerenIsciId]  INT NOT NULL,
                        [YaradildiVaxt]     DATETIME2 NOT NULL,
                        CONSTRAINT [PK_ErkenCixisIcazeler] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ErkenCixisIcazeler_Isciler_IsciId]
                            FOREIGN KEY ([IsciId]) REFERENCES [Isciler] ([Id])
                    );
                    CREATE INDEX [IX_ErkenCixisIcazeler_IsciId_Tarix]
                        ON [ErkenCixisIcazeler] ([IsciId], [Tarix]);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [ErkenCixisIcazeler];");
        }
    }
}
