using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// HesabatSablonu entity-də 'Kateqoriya' enum sahəsi 'KateqoriyaId' FK ilə
    /// əvəz olundu (commit 505ecb5), lakin o zaman migration yaradılmamışdı.
    /// Nəticədə köhnə DB-lərdə 'Invalid column name KateqoriyaId' SQL xətası
    /// yaranır. Bu migration həmin boşluğu doldurur — idempotentdir, yəni
    /// yeni DB-lərdə (artıq KateqoriyaId olan) heç nə etmir.
    /// </summary>
    public partial class FixHesabatSablonuKateqoriyaFk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) HesabatKateqoriyalari cədvəlini yarat (əgər yoxdursa)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HesabatKateqoriyalari')
                BEGIN
                    CREATE TABLE HesabatKateqoriyalari (
                        Id              int IDENTITY(1,1) NOT NULL,
                        Ad              nvarchar(max) NOT NULL,
                        Aktivdir        bit NOT NULL DEFAULT(1),
                        YaradilmaTarixi datetime2 NOT NULL DEFAULT(GETUTCDATE()),
                        YaradanIcraciId int NULL,
                        YenileyenIcraciId int NULL,
                        SilenIcraciId   int NULL,
                        YenilenmeTarixi datetime2 NULL,
                        Silinib         bit NOT NULL DEFAULT(0),
                        SilinmeTarixi   datetime2 NULL,
                        CONSTRAINT PK_HesabatKateqoriyalari PRIMARY KEY (Id)
                    );
                END
            ");

            // 2) Default kateqoriya — varsa atla, yoxdursa yarat
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM HesabatKateqoriyalari WHERE Ad = N'Ümumi' AND Silinib = 0)
                BEGIN
                    INSERT INTO HesabatKateqoriyalari (Ad, Aktivdir, YaradilmaTarixi, Silinib)
                    VALUES (N'Ümumi', 1, GETUTCDATE(), 0);
                END
            ");

            // 3) HesabatSablonlari-a KateqoriyaId sütunu əlavə et (yoxdursa)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'KateqoriyaId' AND Object_ID = Object_ID('HesabatSablonlari')
                )
                BEGIN
                    -- Defaultu: 'Ümumi' kateqoriyasının Id-si
                    DECLARE @defaultKatId int = (SELECT TOP 1 Id FROM HesabatKateqoriyalari WHERE Ad = N'Ümumi' AND Silinib = 0);
                    IF @defaultKatId IS NULL SET @defaultKatId = 0;

                    DECLARE @sql nvarchar(max) = N'ALTER TABLE HesabatSablonlari ADD KateqoriyaId int NOT NULL CONSTRAINT DF_HesabatSablonlari_KateqoriyaId DEFAULT(' + CAST(@defaultKatId AS nvarchar(10)) + N')';
                    EXEC sp_executesql @sql;

                    -- Defaultu sonradan çıxarırıq ki, yeni yazılanlar əllə verilsin
                    ALTER TABLE HesabatSablonlari DROP CONSTRAINT DF_HesabatSablonlari_KateqoriyaId;
                END
            ");

            // 4) Köhnə 'Kateqoriya' enum sütununu sil (əgər qalıbsa)
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'Kateqoriya' AND Object_ID = Object_ID('HesabatSablonlari')
                )
                BEGIN
                    ALTER TABLE HesabatSablonlari DROP COLUMN Kateqoriya;
                END
            ");

            // 5) FK konstraynt — yoxdursa yarat
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_HesabatSablonlari_HesabatKateqoriyalari_KateqoriyaId'
                )
                BEGIN
                    ALTER TABLE HesabatSablonlari
                    ADD CONSTRAINT FK_HesabatSablonlari_HesabatKateqoriyalari_KateqoriyaId
                        FOREIGN KEY (KateqoriyaId) REFERENCES HesabatKateqoriyalari(Id)
                        ON DELETE NO ACTION;
                END
            ");

            // 6) İndex — yoxdursa yarat
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_HesabatSablonlari_KateqoriyaId'
                      AND object_id = OBJECT_ID('HesabatSablonlari')
                )
                BEGIN
                    CREATE INDEX IX_HesabatSablonlari_KateqoriyaId
                    ON HesabatSablonlari (KateqoriyaId);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_HesabatSablonlari_HesabatKateqoriyalari_KateqoriyaId'
                )
                BEGIN
                    ALTER TABLE HesabatSablonlari
                    DROP CONSTRAINT FK_HesabatSablonlari_HesabatKateqoriyalari_KateqoriyaId;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_HesabatSablonlari_KateqoriyaId'
                      AND object_id = OBJECT_ID('HesabatSablonlari')
                )
                BEGIN
                    DROP INDEX IX_HesabatSablonlari_KateqoriyaId ON HesabatSablonlari;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'KateqoriyaId' AND Object_ID = Object_ID('HesabatSablonlari')
                )
                BEGIN
                    ALTER TABLE HesabatSablonlari DROP COLUMN KateqoriyaId;
                END
            ");
        }
    }
}
