/* ============================================================================
   HR — Vezifeler.YonlukHal sütunu (məzuniyyət əmrləri üçün)
   ----------------------------------------------------------------------------
   Vəzifə adının yönlük halı ("rəis" → "rəisinə"). BOŞ olduqda sistem avtomatik
   şəkilçi qoşur; yalnız avtomatik forma səhv çıxan vəzifələr üçün doldurulur.
   İdempotentdir — sütun varsa heç nə etmir.
   ============================================================================ */
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Vezifeler' AND COLUMN_NAME = 'YonlukHal')
BEGIN
    ALTER TABLE Vezifeler ADD YonlukHal NVARCHAR(150) NULL;
    PRINT N'Vezifeler.YonlukHal əlavə olundu.';
END
ELSE
    PRINT N'Vezifeler.YonlukHal artıq mövcuddur.';
