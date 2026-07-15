/* ============================================================================
   MÜHASİBAT — Əlaqəli tərəf DRILL-DOWN sorğusu (OracleSorgular)
   ----------------------------------------------------------------------------
   Bir dəfə işlət. "Muhasibat — Elaqeli detal" sorğusunu əlavə edir — əlaqəli
   tərəf panelinə klik edəndə açılan hesab-səviyyə detalı üçün.
   Filtr aqreqat "Muhasibat — Elaqeli teref" sorğusu ilə EYNİdir (imza sahibi
   olan hüquqi şəxslərin hesabları), sadəcə qruplaşdırılmır — hesab-hesab.
   İdempotentdir.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;
SELECT @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi = N'Muhasibat — Elaqeli teref' AND ISNULL(Silinib,0)=0;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler
    WHERE (Ad LIKE N'%ühasib%' OR Ad LIKE N'%aliyy%') AND ISNULL(Silinib,0)=0 ORDER BY Id;

IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Elaqeli detal' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Elaqeli detal', N'Əlaqəli tərəf — hesab-səviyyə detalı (drill-down)', N'select ar.licsch hesab, l.name_licsch ad,
       substr(ar.licsch,6,2) valyuta, round(-ar.saldo_ish_nacval,2) mebleg
from   odb.arh_saldo_ls ar, licsch l
where  ar.licsch = l.licsch
  and  ar.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and  substr(ar.licsch,1,1) in (''3'',''4'')
  and  (l.registrac_nomer in (select customer_regnom from imza_huquqi_olan_shexsler)
        or l.registrac_nomer in (select regnom from imza_huquqi_olan_shexsler))
  and  ar.saldo_ish_nacval <> 0', 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Elaqeli detal sorğusu hazırdır.';

SELECT SorguAdi FROM OracleSorgular WHERE SorguAdi LIKE N'Muhasibat — Elaqeli%' ORDER BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
