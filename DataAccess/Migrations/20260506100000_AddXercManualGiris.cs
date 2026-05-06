using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Xercler cədvəlinə üç dəyişiklik:
    ///   1. IsciId  — NOT NULL → NULL (manual giriş üçün işçi olmaya bilər)
    ///   2. DepartamentId — nullable int FK (şirkətin şöbəsi)
    ///   3. ManualGiris  — bit NOT NULL DEFAULT 0
    /// İdempotentdir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260506100000_AddXercManualGiris")]
    public partial class AddXercManualGiris : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. IsciId nullable et
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'IsciId'
                      AND Object_ID = Object_ID('Xercler')
                      AND is_nullable = 0
                )
                BEGIN
                    ALTER TABLE Xercler ALTER COLUMN IsciId int NULL;
                END

                -- 2. DepartamentId sütunu
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'DepartamentId' AND Object_ID = Object_ID('Xercler')
                )
                BEGIN
                    ALTER TABLE Xercler ADD DepartamentId int NULL;
                    ALTER TABLE Xercler
                        ADD CONSTRAINT FK_Xercler_Departament_DepartamentId
                        FOREIGN KEY (DepartamentId) REFERENCES Departament(Id);
                END

                -- 3. ManualGiris sütunu
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'ManualGiris' AND Object_ID = Object_ID('Xercler')
                )
                BEGIN
                    ALTER TABLE Xercler ADD ManualGiris bit NOT NULL DEFAULT 0;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Xercler_Departament_DepartamentId'
                )
                    ALTER TABLE Xercler DROP CONSTRAINT FK_Xercler_Departament_DepartamentId;

                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'DepartamentId' AND Object_ID = Object_ID('Xercler')
                )
                    ALTER TABLE Xercler DROP COLUMN DepartamentId;

                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'ManualGiris' AND Object_ID = Object_ID('Xercler')
                )
                    ALTER TABLE Xercler DROP COLUMN ManualGiris;

                -- IsciId-ni NOT NULL-a qaytarmaq: mümkün deyil əgər NULL dəyər varsa.
                -- Ona görə Down bu addımı buraxır.
            ");
        }
    }
}
