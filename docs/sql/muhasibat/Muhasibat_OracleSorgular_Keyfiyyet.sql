/* ============================================================================
   MÜHASİBAT — Kredit Keyfiyyəti & Ehtiyat Oracle sorğuları
   ----------------------------------------------------------------------------
   Üç sorğu (idempotent):
     1) "Muhasibat — Kredit keyfiyyet"       — ehtiyat dərəcəsi üzrə təsnifat
        (procstavrez/procstavrez_19, %), kateqoriya 1-5: say/qalıq/ehtiyat.
        Kat: 1 Standart(≤5%) 2 Nəzarət(≤20%) 3 Qeyri-standart(≤50%) 4 Şübhəli(<100%) 5 Ümidsiz(100%).
        Ehtiyat = (əsas×procstavrez + VK×procstavrez_19)/100 × valyuta kursu.
     2) "Muhasibat — Kredit keyfiyyet baza"  — restrukt (date_restructure) +
        girov/LTV (girovun_bazar_deyeri, licschkre+subschkre outer join).
     3) "Muhasibat — Kredit keyfiyyet detal" — hesab-səviyyə (drill-down).

   Test (16/07/2026): 317 müqavilə, portfel 3 302 471, ehtiyat 825 546 (25%),
   restrukt 29, girov 9.7M (108 kredit).
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

/* ── 1. Təsnifat (ehtiyat dərəcəsi üzrə) ─────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Kredit keyfiyyet' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Kredit keyfiyyet', N'Kredit təsnifatı — ehtiyat dərəcəsi üzrə (say/qalıq/ehtiyat)', N'select
  case when greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) <= 5  then 1
       when greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) <= 20 then 2
       when greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) <= 50 then 3
       when greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) < 100 then 4
       else 5 end kat,
  count(*) say,
  round(sum((al.summa+al.summa_19)*round(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6)),2) qaliq,
  round(sum((al.summa*nvl(al.procstavrez,0)+al.summa_19*nvl(al.procstavrez_19,0))/100
            *round(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6)),2) ehtiyat
from arh_licschkre al
where al.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (al.date_close is null or al.date_close > to_date(''{TARIX}'',''dd/mm/yyyy''))
  and length(al.licschkre) = 20
group by case when greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) <= 5  then 1
              when greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) <= 20 then 2
              when greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) <= 50 then 3
              when greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) < 100 then 4
              else 5 end', 1, @DepId, GETDATE(), 0);

/* ── 2. Baza — restrukt + girov/LTV ─────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Kredit keyfiyyet baza' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Kredit keyfiyyet baza', N'Restrukt + girov/LTV (girovun_bazar_deyeri)', N'select
  sum(case when al.date_restructure is not null then 1 else 0 end) restrukt_say,
  round(sum(case when al.date_restructure is not null then (al.summa+al.summa_19)*round(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6) else 0 end),2) restrukt_qaliq,
  sum(case when g.girov is not null then 1 else 0 end) girovlu_say,
  round(sum(case when g.girov is not null then (al.summa+al.summa_19)*round(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6) else 0 end),2) girovlu_qaliq,
  sum(case when g.girov is null then 1 else 0 end) girovsuz_say,
  round(sum(case when g.girov is null then (al.summa+al.summa_19)*round(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6) else 0 end),2) girovsuz_qaliq,
  round(sum(nvl(g.girov,0)),2) girov_cem
from arh_licschkre al,
     (select licschkre, sum(girovun_bazar_deyeri) girov from girovun_bazar_deyeri group by licschkre) g
where al.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (al.date_close is null or al.date_close > to_date(''{TARIX}'',''dd/mm/yyyy''))
  and length(al.licschkre) = 20
  and al.licschkre = g.licschkre(+)', 1, @DepId, GETDATE(), 0);

/* ── 3. Detal (drill-down) ──────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Kredit keyfiyyet detal' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Kredit keyfiyyet detal', N'Kredit keyfiyyəti — müqavilə-səviyyə (drill-down: kateqoriya/girov/restrukt)', N'select
  al.licschkre muqavile, al.tipkredita tip,
  greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) rez,
  round((al.summa+al.summa_19)*round(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6),2) qaliq,
  round(nvl(g.girov,0),2) girov,
  case when al.date_restructure is not null then 1 else 0 end restrukt
from arh_licschkre al,
     (select licschkre, sum(girovun_bazar_deyeri) girov from girovun_bazar_deyeri group by licschkre) g
where al.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (al.date_close is null or al.date_close > to_date(''{TARIX}'',''dd/mm/yyyy''))
  and length(al.licschkre) = 20
  and al.licschkre = g.licschkre(+)', 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Kredit keyfiyyəti sorğuları əlavə olundu.';

SELECT SorguAdi, DepartamentId, Aktiv FROM OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Kredit keyfiyyet', N'Muhasibat — Kredit keyfiyyet baza', N'Muhasibat — Kredit keyfiyyet detal');

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
