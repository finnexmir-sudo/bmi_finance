-- «Pul köçürməsi → Gedən həvalə» bağı üçün sütun.
-- Normalda startup-da avtomatik tətbiq olunur (Program.cs → db.Database.Migrate(),
-- migration: 20260818000000_GedenHevale_KocurmeId). Konsolda
-- «[Migration XƏTA]» görsəniz — bu skripti əl ilə işlədin.
-- Data itkisi yoxdur: yalnız nullable sütun + indeks.

USE FinNex_Maliyye_Db;
GO

IF COL_LENGTH('dbo.GedenHevale', 'KocurmeId') IS NULL
BEGIN
    ALTER TABLE dbo.GedenHevale ADD KocurmeId int NULL;
    PRINT N'✔ KocurmeId sütunu əlavə edildi.';
END
ELSE
    PRINT N'• KocurmeId onsuz da var.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_GedenHevale_KocurmeId'
                 AND object_id = OBJECT_ID('dbo.GedenHevale'))
BEGIN
    CREATE INDEX IX_GedenHevale_KocurmeId ON dbo.GedenHevale(KocurmeId);
    PRINT N'✔ IX_GedenHevale_KocurmeId yaradıldı.';
END
GO

-- Migration-ı tətbiq olunmuş kimi qeyd et ki, startup onu təkrar işlətməsin
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory
               WHERE MigrationId = '20260818000000_GedenHevale_KocurmeId')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260818000000_GedenHevale_KocurmeId', '8.0.23');
GO
