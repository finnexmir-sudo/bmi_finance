/* ============================================================================
   MÜHASİBAT DASHBOARD — OracleSorgular INSERT (SQL Server)
   ----------------------------------------------------------------------------
   Bir dəfə işlət. Mühasibat/Maliyyə departamentini tapır (yoxdursa yaradır) və
   7 sorğunu OracleSorgular cədvəlinə əlavə edir (idempotent — təkrar işlətsən
   dublikat yaratmır).

   SorguAdi-lər MuhasibatService.cs-dəki const-larla EYNİ olmalıdır — dəyişmə.
   SQL-lərdə {TARIX}/{BAS}/{SON} token-ləri qalmalıdır (servis əvəz edir).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

/* Mühasibat / Maliyyə departamentini tap */
SELECT TOP 1 @DepId = Id
FROM   Departamentler
WHERE  (Ad LIKE N'%ühasib%' OR Ad LIKE N'%aliyy%')
  AND  ISNULL(Silinib,0) = 0
ORDER BY Id;

/* Yoxdursa yarat */
IF @DepId IS NULL
BEGIN
    INSERT INTO Departamentler (Ad, Aciqlama, YaradilmaTarixi, Silinib)
    VALUES (N'Mühasibatlıq', N'Maliyyə / mühasibat hesabatları', GETDATE(), 0);
    SET @DepId = SCOPE_IDENTITY();
END

PRINT N'Departament Id = ' + CAST(@DepId AS NVARCHAR(10));

/* ── 1. Balans qalıqları ─────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Balans qaliqlari' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Balans qaliqlari', N'Balans İcmalı — hesab qalıqları + dep_tip (müştəri depoziti)', N'SELECT ar.licsch AS hesab,
       CASE WHEN SUBSTR(ar.licsch,0,3) IN (''159'',''209'',''219'',''239'',''259'')
            THEN SUBSTR(ar.licsch,16,2) ELSE SUBSTR(ar.licsch,6,2) END AS valyuta,
       ar.saldo_ish_nacval AS qaliq,
       CASE
         WHEN SUBSTR(ar.licsch,1,2)=''41''
              AND SUBSTR(ar.licsch,10,6)<>''000004''
              AND EXISTS (SELECT 1 FROM regnom r WHERE r.regnom=SUBSTR(ar.licsch,10,6))
           THEN ''F''
         WHEN (SUBSTR(ar.licsch,1,2)=''40'' OR SUBSTR(ar.licsch,1,1)=''3'')
              AND SUBSTR(ar.licsch,1,5) NOT IN (''35020'',''35025'',''35026'',''35940'')
              AND SUBSTR(ar.licsch,10,6)<>''000004''
              AND EXISTS (SELECT 1 FROM regnom r WHERE r.regnom=ch.registrac_nomer)
           THEN ''H''
         ELSE ''X''
       END AS dep_tip
FROM   odb.arh_saldo_ls ar, licsch ch
WHERE  ar.date_oper = TO_DATE(''{TARIX}'',''dd/mm/yyyy'')
  AND  ch.licsch = ar.licsch
  AND  (ch.date_close_licsch IS NULL OR ar.date_oper <= ch.date_close_licsch)', 1, @DepId, GETDATE(), 0);

/* ── 2. Balans müqayisə (əvvəlki iş günü) ────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Balans muqayise' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Balans muqayise', N'Əvvəlki iş günü totalları (dünənlə müqayisə)', N'select
  round(sum(case when substr(ar.licsch,1,1) in (''1'',''2'') then ar.saldo_ish_nacval else 0 end),2) aktiv,
  round(-sum(case when substr(ar.licsch,1,1) in (''3'',''4'') and substr(ar.licsch,1,2) not in (''44'',''45'') then ar.saldo_ish_nacval else 0 end),2) ohdelik,
  round(-sum(case when substr(ar.licsch,1,2) in (''44'',''45'') or substr(ar.licsch,1,1)=''5'' then ar.saldo_ish_nacval else 0 end),2) kapital,
  round(-sum(case when substr(ar.licsch,1,5)=''50130'' then ar.saldo_ish_nacval else 0 end),2) menfeet
from odb.arh_saldo_ls ar, licsch ch
where ar.date_oper = (
    select max(c.date_oper) from odb.arh_saldo_ls c
    where c.date_oper < to_date(''{TARIX}'',''dd/mm/yyyy'')
      and c.date_oper >= to_date(''{TARIX}'',''dd/mm/yyyy'') - 10)
  and ch.licsch = ar.licsch
  and (ch.date_close_licsch is null or ar.date_oper <= ch.date_close_licsch)', 1, @DepId, GETDATE(), 0);

/* ── 3. Depozit hesabları ────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Depozit hesablari' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Depozit hesablari', N'Depozit portfeli — hüquqi (40/3x) + fiziki (41), müştəri ilə', N'SELECT ''huquqi'' tip, b.registrac_nomer qeyd, r.name_regnom musteri,
       SUBSTR(l.licsch,6,2) valyuta, -l.saldo_ish_nacval qaliq
FROM   odb.arh_saldo_ls l, licsch b, regnom r
WHERE  l.date_oper = TO_DATE(''{TARIX}'',''dd/mm/yyyy'')
  AND  b.licsch = l.licsch AND r.regnom = b.registrac_nomer
  AND  (SUBSTR(l.licsch,1,2)=''40'' OR SUBSTR(l.licsch,1,1)=''3'')
  AND  SUBSTR(l.licsch,1,5) NOT IN (''35020'',''35025'',''35026'',''35940'')
  AND  SUBSTR(l.licsch,10,6) <> ''000004''
  AND  l.saldo_ish_nacval <> 0
UNION ALL
SELECT ''fiziki'' tip, SUBSTR(l.licsch,10,6) qeyd, r.name_regnom musteri,
       SUBSTR(l.licsch,6,2) valyuta, -l.saldo_ish_nacval qaliq
FROM   odb.arh_saldo_ls l, regnom r
WHERE  l.date_oper = TO_DATE(''{TARIX}'',''dd/mm/yyyy'')
  AND  SUBSTR(l.licsch,1,2)=''41'' AND r.regnom = SUBSTR(l.licsch,10,6)
  AND  SUBSTR(l.licsch,10,6) <> ''000004''
  AND  l.saldo_ish_nacval <> 0', 1, @DepId, GETDATE(), 0);

/* ── 4. Əlaqəli tərəf ─────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Elaqeli teref' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Elaqeli teref', N'Əlaqəli tərəf depoziti (şirkət+təsisçi) / portfel', N'with elaqeli_hesab as (
  select distinct ar.licsch
  from odb.arh_saldo_ls ar, licsch l
  where ar.licsch = l.licsch and ar.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
    and substr(ar.licsch,1,1) in (''3'',''4'')
    and (l.registrac_nomer in (select customer_regnom from imza_huquqi_olan_shexsler)
         or l.registrac_nomer in (select regnom from imza_huquqi_olan_shexsler))
)
select
  (select round(-sum(k.saldo_ish_nacval),2)
     from odb.arh_saldo_ls k, licsch p
    where p.licsch = k.licsch and k.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
      and substr(k.licsch,1,2) in (''35'',''36'',''38'',''39'',''40'',''41'',''49'')
      and (p.date_close_licsch is null or k.date_oper <= p.date_close_licsch)) portfel,
  (select round(-sum(ar.saldo_ish_nacval),2)
     from odb.arh_saldo_ls ar
    where ar.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
      and ar.licsch in (select licsch from elaqeli_hesab)) elaqeli
from dual', 1, @DepId, GETDATE(), 0);

/* ── 5. Kredit portfeli ──────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Kredit portfeli' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Kredit portfeli', N'Cari kredit portfeli (tip/təyinat/valyuta/DPD)', N'SELECT lk.tipkredita tip,
       lk.index_otrasli teyinat,
       SUBSTR(lk.licschkre,6,2) valyuta,
       ROUND(odb.func_get_kurval(SUBSTR(lk.licschkre,6,2), to_date(sysdate)), 6) kurs,
       lk.summa esas,
       lk.summa_19 vk,
       odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate, x.date_oper)) gec_gun
FROM   odb.licschkre lk, view_nacpogprokre_all x
WHERE  lk.licschpkre = x.licschpkre AND lk.subschkre = x.subschkre
  AND  x.date_oper = to_date(sysdate)
  AND  lk.date_close IS NULL AND LENGTH(lk.licschkre) = 20', 1, @DepId, GETDATE(), 0);

/* ── 6. Valyuta əməliyyatları ─────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Valyuta emeliyyatlari' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Valyuta emeliyyatlari', N'Valyuta alış/satış (arh_dd) — tarix aralığı', N'SELECT ''alis'' yon, SUBSTR(d.debet,6,2) val, d.summa_v_inval hecm, d.summa_v_inval*d.kurs_valuti azn
FROM   arh_dd d
WHERE  d.date_oper BETWEEN TO_DATE(''{BAS}'',''dd/mm/yyyy'') AND TO_DATE(''{SON}'',''dd/mm/yyyy'')
  AND  SUBSTR(d.debet,1,5) = ''10060'' AND SUBSTR(d.kredit,1,5) = ''10050''
UNION ALL
SELECT ''satis'' yon, SUBSTR(d.kredit,6,2) val, d.summa_v_inval hecm, d.summa_v_inval*d.kurs_valuti azn
FROM   arh_dd d
WHERE  d.date_oper BETWEEN TO_DATE(''{BAS}'',''dd/mm/yyyy'') AND TO_DATE(''{SON}'',''dd/mm/yyyy'')
  AND  SUBSTR(d.debet,1,5) = ''10050'' AND SUBSTR(d.kredit,1,5) = ''10060''', 1, @DepId, GETDATE(), 0);

/* ── 7. Rezident / qeyri-rezident ────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Rezident' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Rezident', N'Rezident/qeyri-rezident bölgüsü (ABS qalıq)', N'select
  case when (substr(s.licsch,0,3)=''409''
             and substr(regexp_substr(l.name_licsch,''\(([^()]*)\)\s*$'',1,1,null,1),5,1)=''5'')
            or substr(s.licsch,0,5)=''45029''
       then ''qr'' else ''r'' end tip,
  round(sum(abs(s.saldo_ish_nacval)),2) mebleg,
  count(*) say
from odb.arh_saldo_ls s, licsch l
where l.licsch = s.licsch
  and s.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (l.date_close_licsch is null or l.date_close_licsch >= to_date(''{TARIX}'',''dd/mm/yyyy''))
group by case when (substr(s.licsch,0,3)=''409''
             and substr(regexp_substr(l.name_licsch,''\(([^()]*)\)\s*$'',1,1,null,1),5,1)=''5'')
            or substr(s.licsch,0,5)=''45029''
       then ''qr'' else ''r'' end', 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Mühasibat sorğuları əlavə olundu.';

SELECT SorguAdi, DepartamentId, Aktiv FROM OracleSorgular
WHERE SorguAdi LIKE N'Muhasibat — %' ORDER BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
