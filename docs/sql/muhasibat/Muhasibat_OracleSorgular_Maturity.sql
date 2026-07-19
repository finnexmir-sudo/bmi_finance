/* ============================================================================
   MÜHASİBAT — Kredit Pul Axını (Maturity Ladder) Oracle sorğuları
   ----------------------------------------------------------------------------
   İki sorğu (idempotent):
     1) "Muhasibat — Maturity ladder"   — graphpogkre üzrə gələcək ödənişlər
        (əsas=summa_pog_kre, faiz=summa_pog_pro) date_pog müddət qutularında:
        e1/f1=0-1ay · e2/f2=1-3ay · e3/f3=3-6ay · e4/f4=6-12ay · e5/f5=1-2il · e6/f6=2il+
     2) "Muhasibat — Maturity kontekst" — tələbli depozit bazası (35-49) + HQLA
        (15770/11710), ≤TARIX son iş gününə.

   Qeyd: bu bankda depozitlərin vaxt strukturu yoxdur (müddətsiz/tələbli), ona görə
   yalnız AKTİV tərəf pul axını verilir (tam GAP deyil).
   Oracle YALNIZ SELECT. Azərbaycan hərfi YOXDUR. {TARIX} servis tərəfindən əvəz olunur.
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

/* ── 1. Maturity ladder — graphpogkre gələcək ödənişlər, qutular ─────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Maturity ladder' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Maturity ladder', N'Kredit pul axını — graphpogkre gələcək ödənişlər müddət qutularında', N'select
  round(sum(case when g.date_pog >  to_date(''{TARIX}'',''dd/mm/yyyy'')                 and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),1)  then g.summa_pog_kre else 0 end),2) e1,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),1)   and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),3)  then g.summa_pog_kre else 0 end),2) e2,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),3)   and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),6)  then g.summa_pog_kre else 0 end),2) e3,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),6)   and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),12) then g.summa_pog_kre else 0 end),2) e4,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),12)  and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),24) then g.summa_pog_kre else 0 end),2) e5,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),24)                                                                       then g.summa_pog_kre else 0 end),2) e6,
  round(sum(case when g.date_pog >  to_date(''{TARIX}'',''dd/mm/yyyy'')                 and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),1)  then g.summa_pog_pro else 0 end),2) f1,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),1)   and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),3)  then g.summa_pog_pro else 0 end),2) f2,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),3)   and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),6)  then g.summa_pog_pro else 0 end),2) f3,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),6)   and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),12) then g.summa_pog_pro else 0 end),2) f4,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),12)  and g.date_pog <= add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),24) then g.summa_pog_pro else 0 end),2) f5,
  round(sum(case when g.date_pog >  add_months(to_date(''{TARIX}'',''dd/mm/yyyy''),24)                                                                       then g.summa_pog_pro else 0 end),2) f6
from graphpogkre g
where g.date_pog > to_date(''{TARIX}'',''dd/mm/yyyy'')', 1, @DepId, GETDATE(), 0);

/* ── 2. Kontekst — tələbli depozit bazası + HQLA ────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Maturity kontekst' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Maturity kontekst', N'Tələbli depozit bazası (35-49) + likvid aktivlər (LikvidQrup ilə eyni)', N'select
  round(sum(case when substr(s.licsch,1,2) in (''35'',''36'',''38'',''39'',''40'',''41'',''49'') then abs(s.saldo_ish_nacval) else 0 end),2) depozit,
  round(sum(case when s.saldo_ish_nacval > 0 and (
                 substr(s.licsch,1,3) = ''100''
              or substr(s.licsch,1,5) in (''11010'',''11020'',''11110'',''11710'')
              or substr(s.licsch,1,5) in (''14010'',''14012'',''14014'',''14030'',''14032'',''14034'')
              or substr(s.licsch,1,5) in (''15020'',''15025'',''15770'')
            ) then s.saldo_ish_nacval else 0 end),2) hqla
from odb.arh_saldo_ls s
where s.date_oper = (select max(date_oper) from odb.arh_saldo_ls
                     where date_oper <= to_date(''{TARIX}'',''dd/mm/yyyy''))', 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Maturity ladder sorğuları əlavə olundu.';

SELECT SorguAdi, DepartamentId, Aktiv FROM OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Maturity ladder', N'Muhasibat — Maturity kontekst');

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
