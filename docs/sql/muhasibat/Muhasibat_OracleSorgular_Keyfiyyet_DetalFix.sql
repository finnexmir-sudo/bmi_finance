/* ============================================================================
   MÜHASİBAT — Kredit keyfiyyəti girov join düzəlişi + drill-down sütunları (UPDATE)
   ----------------------------------------------------------------------------
   İki düzəliş (mövcud sorğuları UPDATE edir — INSERT script IF NOT EXISTS-dir):

   1) GİROV JOİN — girovun_bazar_deyeri-də `subschkre` girovun SIRA nömrəsidir
      (kreditin subschkre-si deyil), ona görə licschkre+subschkre join əksəriyyəti
      tutmurdu (girovlu cəmi 9 çıxırdı, 108 olmalı). İndi YALNIZ licschkre üzrə join.
      Həm "baza" (girovlu/girovsuz/girov_cem), həm "detal".

   2) DETAL — girov + restrukt sütunları əlavə (Girovlu/Girovsuz/Restrukt drill-down).

   Oracle YALNIZ SELECT. Azərbaycan hərfi YOXDUR.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

/* ── 1. Baza — girov join licschkre-only ────────────────────────────────── */
UPDATE OracleSorgular
SET    SorguMetni = N'select
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
  and al.licschkre = g.licschkre(+)'
WHERE  SorguAdi = N'Muhasibat — Kredit keyfiyyet baza' AND ISNULL(Silinib,0)=0;

/* ── 2. Detal — girov + restrukt sütunları, licschkre-only join ─────────── */
UPDATE OracleSorgular
SET    SorguMetni = N'select
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
  and al.licschkre = g.licschkre(+)'
WHERE  SorguAdi = N'Muhasibat — Kredit keyfiyyet detal' AND ISNULL(Silinib,0)=0;

COMMIT TRAN;
PRINT N'Kredit keyfiyyet baza + detal yeniləndi.';

SELECT SorguAdi,
       CASE WHEN SorguMetni LIKE '%group by licschkre)%' THEN 'YENİ (licschkre join)' ELSE 'KÖHNƏ' END AS veziyyet
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Kredit keyfiyyet baza', N'Muhasibat — Kredit keyfiyyet detal')
  AND  ISNULL(Silinib,0)=0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
