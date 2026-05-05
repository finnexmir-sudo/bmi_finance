using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// IsciAyliqQazanclar cədvəlinə DSMF məbləğləri üçün 2 yeni sütun əlavə edir:
    ///   DsmfIsci        — işçidən tutulan aylıq DSMF
    ///   DsmfIsegoturen  — şirkətin işçi üçün ödədiyi aylıq DSMF
    ///
    /// Bu sütunlar xəstəlik ödənişinin yeni DSMF-əsaslı düsturu üçün lazımdır
    /// (May 2026+):
    ///   GünlükPul = ((Σ DsmfIsci + Σ DsmfIsegoturen) × Əmsal)
    ///               ÷ (son 12 ay təqvim günü − xəstəlik təqvim günü)
    ///
    /// Default 0 — köhnə qeydlər boş qalır, manual import (Excel) ilə doldurulur,
    /// yeni Maas hesablamaları avtomatik dolduracaq.
    ///
    /// İdempotentdir.
    /// </summary>
    public partial class AddDsmfToIsciAyliqQazanc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'DsmfIsci' AND Object_ID = Object_ID('IsciAyliqQazanclar')
                )
                BEGIN
                    ALTER TABLE IsciAyliqQazanclar
                    ADD DsmfIsci decimal(18,2) NOT NULL CONSTRAINT DF_IsciAyliqQazanclar_DsmfIsci DEFAULT 0;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'DsmfIsegoturen' AND Object_ID = Object_ID('IsciAyliqQazanclar')
                )
                BEGIN
                    ALTER TABLE IsciAyliqQazanclar
                    ADD DsmfIsegoturen decimal(18,2) NOT NULL CONSTRAINT DF_IsciAyliqQazanclar_DsmfIsegoturen DEFAULT 0;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'DsmfIsegoturen' AND Object_ID = Object_ID('IsciAyliqQazanclar')
                )
                BEGIN
                    ALTER TABLE IsciAyliqQazanclar
                    DROP CONSTRAINT IF EXISTS DF_IsciAyliqQazanclar_DsmfIsegoturen;
                    ALTER TABLE IsciAyliqQazanclar DROP COLUMN DsmfIsegoturen;
                END
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'DsmfIsci' AND Object_ID = Object_ID('IsciAyliqQazanclar')
                )
                BEGIN
                    ALTER TABLE IsciAyliqQazanclar
                    DROP CONSTRAINT IF EXISTS DF_IsciAyliqQazanclar_DsmfIsci;
                    ALTER TABLE IsciAyliqQazanclar DROP COLUMN DsmfIsci;
                END
            ");
        }
    }
}
