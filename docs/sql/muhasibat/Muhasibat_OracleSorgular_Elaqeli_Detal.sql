/* ============================================================================
   MÜHASİBAT — Əlaqəli tərəf DRILL-DOWN sorğusu (OracleSorgular)
   ----------------------------------------------------------------------------
   Bir dəfə işlət. "Muhasibat — Elaqeli detal" sorğusunu qoyur/yeniləyir.
   Filtr aqreqat "Muhasibat — Elaqeli teref" ilə EYNİdir (imza sahibi olan
   hüquqi şəxslərin 3x/4x hesabları), amma:
     • hər sətrin ROL-unu göstərir: "Şirkət" / "İmza sahibi: <bağlı şirkət>"
     • sətirlər QRUPLA sıralanır — şirkət, sonra onun əlaqəli şəxsləri.
   İdempotentdir (varsa UPDATE, yoxdursa INSERT).
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

DECLARE @Sql NVARCHAR(MAX) = N'select ar.licsch hesab,
       l.name_licsch ad,
       substr(ar.licsch,6,2) valyuta,
       round(-ar.saldo_ish_nacval,2) mebleg,
       case
         when exists (select 1 from imza_huquqi_olan_shexsler i where i.customer_regnom = l.registrac_nomer)
           then ''Şirkət''
         else ''İmza sahibi: '' ||
              nvl((select substr(listagg(cr.name_regnom, ''; '') within group (order by cr.name_regnom),1,120)
                     from imza_huquqi_olan_shexsler i2 join regnom cr on cr.regnom = i2.customer_regnom
                    where i2.regnom = l.registrac_nomer), ''?'')
       end elave
from   odb.arh_saldo_ls ar
join   licsch l on l.licsch = ar.licsch
where  ar.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and  substr(ar.licsch,1,1) in (''3'',''4'')
  and  (l.registrac_nomer in (select customer_regnom from imza_huquqi_olan_shexsler)
        or l.registrac_nomer in (select regnom from imza_huquqi_olan_shexsler))
  and  ar.saldo_ish_nacval <> 0
order  by (case
             when exists (select 1 from imza_huquqi_olan_shexsler i where i.customer_regnom = l.registrac_nomer)
               then l.registrac_nomer
             else nvl((select min(i2.customer_regnom) from imza_huquqi_olan_shexsler i2 where i2.regnom = l.registrac_nomer), l.registrac_nomer)
           end),
           (case when exists (select 1 from imza_huquqi_olan_shexsler i where i.customer_regnom = l.registrac_nomer) then 0 else 1 end),
           mebleg desc';

UPDATE OracleSorgular
SET    SorguMetni = @Sql, Aktiv = 1, Silinib = 0
WHERE  SorguAdi = N'Muhasibat — Elaqeli detal';

IF @@ROWCOUNT = 0
    INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (N'Muhasibat — Elaqeli detal', N'Əlaqəli tərəf — hesab-səviyyə detalı, ROL + qrup (drill-down)',
            @Sql, 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Elaqeli detal sorğusu (ROL + qrup) hazırdır.';

SELECT SorguAdi FROM OracleSorgular WHERE SorguAdi LIKE N'Muhasibat — Elaqeli%' ORDER BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
