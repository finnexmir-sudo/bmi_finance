/* ============================================================================
   MÜHASİBAT — Əlaqəli tərəf DRILL-DOWN sorğusu (OracleSorgular)
   ----------------------------------------------------------------------------
   Bir dəfə işlət. "Muhasibat — Elaqeli detal" sorğusunu qoyur/yeniləyir.
   Filtr aqreqat "Muhasibat — Elaqeli teref" ilə EYNİdir. Hər sətrin yanında
   onun ƏLAQƏLİ TƏRƏFinin adını göstərir — imza_huquqi_olan_shexsler cədvəlində
   HƏR İKİ istiqamətə baxaraq (regnom ↔ customer_regnom). Sətirlər əlaqəli
   qrup üzrə sıralanır (əlaqəli tərəflər yan-yana).
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
       (select substr(listagg(nm, ''; '') within group (order by nm),1,150)
          from (select nm from (
                  select distinct r.name_regnom nm
                    from imza_huquqi_olan_shexsler i join regnom r on r.regnom = i.customer_regnom
                   where i.regnom = l.registrac_nomer
                  union
                  select distinct r.name_regnom nm
                    from imza_huquqi_olan_shexsler i join regnom r on r.regnom = i.regnom
                   where i.customer_regnom = l.registrac_nomer
                ) where rownum <= 10)) elave,
       (select min(g) from (
          select l.registrac_nomer g from dual
          union select i.customer_regnom from imza_huquqi_olan_shexsler i where i.regnom = l.registrac_nomer
          union select i.regnom          from imza_huquqi_olan_shexsler i where i.customer_regnom = l.registrac_nomer
        )) grup_kod
from   odb.arh_saldo_ls ar
join   licsch l on l.licsch = ar.licsch
where  ar.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and  substr(ar.licsch,1,1) in (''3'',''4'')
  and  (l.registrac_nomer in (select customer_regnom from imza_huquqi_olan_shexsler)
        or l.registrac_nomer in (select regnom from imza_huquqi_olan_shexsler))
  and  ar.saldo_ish_nacval <> 0
order  by grup_kod, mebleg desc';

UPDATE OracleSorgular
SET    SorguMetni = @Sql, Aktiv = 1, Silinib = 0
WHERE  SorguAdi = N'Muhasibat — Elaqeli detal';

IF @@ROWCOUNT = 0
    INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (N'Muhasibat — Elaqeli detal', N'Əlaqəli tərəf — hesab-səviyyə, əlaqəli tərəf adı + qrup (drill-down)',
            @Sql, 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Elaqeli detal sorğusu (iki istiqamətli əlaqə + qrup) hazırdır.';

SELECT SorguAdi FROM OracleSorgular WHERE SorguAdi LIKE N'Muhasibat — Elaqeli%' ORDER BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
