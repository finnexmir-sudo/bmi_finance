/* ============================================================================
   HR — Plan üzrə avtomatik bağlama sütunları (gecə servisi üçün)
   ----------------------------------------------------------------------------
   İşçi cihaza vurmadan gedəndə gün bağlananda icazə/ezamiyyət faktiki vaxtları
   müraciətdəki PLAN üzrə avtomatik yazılır; bu bayraqlar həmin qeydləri real
   cihaz datasından fərqləndirir (audit). İdempotentdir.
   ============================================================================ */
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='IcazeCixisGirisler' AND COLUMN_NAME='PlanUzreAvtomatik')
    ALTER TABLE IcazeCixisGirisler ADD PlanUzreAvtomatik BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='EzamiyyetMuracietler' AND COLUMN_NAME='CihazVaxtPlanUzre')
    ALTER TABLE EzamiyyetMuracietler ADD CihazVaxtPlanUzre BIT NOT NULL DEFAULT 0;

PRINT N'Plan uzre baglanma sutunlari hazirdir.';
