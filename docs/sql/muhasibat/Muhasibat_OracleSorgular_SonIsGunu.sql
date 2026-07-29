/* ============================================================================
   MÜHASİBAT — İlin son iş günü yoxlaması Oracle sorğusu
   ----------------------------------------------------------------------------
   Bir sorğu əlavə edir (idempotent — varsa təkrar yaratmır):
     "Muhasibat — Ilin son is gunu" — {TARIX} ilin son əməliyyat günüdürsə 1, yoxsa 0.

   Səbəb: il sonu son iş günündə cari il mənfəəti (50130) il-sonu bağlanışı ilə
   50120-yə köçürülür — həmin gün Balans İcmalındakı "Xalis mənfəət" KPI-si
   50130-dan oxunanda 0 çıxırdı. Servis (MuhasibatService.IlinSonIsGunuMu) bu
   sorğu 1 qaytaranda mənfəəti 50120 prefiksindən oxuyur.

   Məntiq: {TARIX}-dən sonra, amma eyni il daxilində arh_saldo_ls-də əməliyyat
   günü YOXDURSA → {TARIX} ilin son əməliyyat günüdür (son_gun=1).

   Oracle YALNIZ SELECT (CLAUDE.md). Azərbaycan hərfi YOXDUR (charset təhlükəsiz).
   {TARIX} servis tərəfindən dd/MM/yyyy ilə əvəz olunur.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;
SELECT @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari' AND ISNULL(Silinib,0)=0;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler
    WHERE (Ad LIKE N'%ühasib%' OR Ad LIKE N'%aliyy%') AND ISNULL(Silinib,0)=0 ORDER BY Id;

/* ── İlin son iş günü ─────────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Ilin son is gunu' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Ilin son is gunu', N'{TARIX} ilin son əməliyyat günüdürsə 1 (mənfəət 50120-dən oxunur), yoxsa 0', N'SELECT CASE WHEN EXISTS (SELECT 1 FROM odb.arh_saldo_ls WHERE date_oper > TO_DATE(''{TARIX}'',''dd/mm/yyyy'') AND date_oper < ADD_MONTHS(TRUNC(TO_DATE(''{TARIX}'',''dd/mm/yyyy''),''YEAR''),12)) THEN 0 ELSE 1 END AS son_gun FROM dual', 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'İlin son iş günü sorğusu əlavə olundu.';

SELECT SorguAdi, DepartamentId, Aktiv FROM OracleSorgular
WHERE  SorguAdi = N'Muhasibat — Ilin son is gunu';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
