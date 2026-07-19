/* ============================================================================
   MÜHASİBAT — Kredit keyfiyyəti GİROV bölməsi arh_licschkre-ə keçirildi
   ----------------------------------------------------------------------------
   Əvvəl girov girovun_bazar_deyeri cədvəlindən gəlirdi (etibarsız, join səhv).
   İndi DÜZGÜN mənbə — arh_licschkre:
     - növ  = al.tipzaloga  → tipzal.name (lüğət). Təminatsız = tipzaloga 8.
     - dəyər = nvl(nullif(summa_pereocen_zaloga,0), summa_zaloga)
               (yenidən qiymətləndirmə üstün, yoxdursa ilkin).

   Bu script:
     1) YENİ "Muhasibat — Kredit girov"        — növ üzrə struktur (say/qalıq/girov).
     2) UPDATE "Muhasibat — Kredit keyfiyyet baza" — yalnız restrukt (girov çıxıldı).
     3) UPDATE "Muhasibat — Kredit keyfiyyet detal" — tipzaloga+ad+girov sütunları.
   Oracle YALNIZ SELECT. Azərbaycan hərfi YOXDUR. {TARIX} servis tərəfindən əvəz olunur.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;
SELECT @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi = N'Muhasibat — Kredit keyfiyyet' AND ISNULL(Silinib,0)=0;
IF @DepId IS NULL
    SELECT @DepId = DepartamentId FROM OracleSorgular
    WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari' AND ISNULL(Silinib,0)=0;

/* ── 1. YENİ — girov strukturu (növ üzrə) ───────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Kredit girov' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Kredit girov', N'Girov strukturu — tipzaloga (tipzal.name) + summa_zaloga/pereocen', N'select
  z.name ad, al.tipzaloga kod,
  count(*) say,
  round(sum((al.summa+al.summa_19)*round(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6)),2) qaliq,
  round(sum(nvl(nullif(al.summa_pereocen_zaloga,0), al.summa_zaloga)),2) girov
from arh_licschkre al, tipzal z
where al.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (al.date_close is null or al.date_close > to_date(''{TARIX}'',''dd/mm/yyyy''))
  and length(al.licschkre) = 20
  and (al.summa+al.summa_19) <> 0
  and al.tipzaloga = z.code(+)
group by z.name, al.tipzaloga', 1, @DepId, GETDATE(), 0);

/* ── 2. UPDATE — baza yalnız restrukt ───────────────────────────────────── */
UPDATE OracleSorgular
SET    SorguMetni = N'select
  sum(case when al.date_restructure is not null then 1 else 0 end) restrukt_say,
  round(sum(case when al.date_restructure is not null then (al.summa+al.summa_19)*round(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6) else 0 end),2) restrukt_qaliq
from arh_licschkre al
where al.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (al.date_close is null or al.date_close > to_date(''{TARIX}'',''dd/mm/yyyy''))
  and length(al.licschkre) = 20
  and (al.summa+al.summa_19) <> 0',
       Mahiyyet = N'Kredit keyfiyyəti — restrukt (girov ayrıca sorğuya keçdi)'
WHERE  SorguAdi = N'Muhasibat — Kredit keyfiyyet baza' AND ISNULL(Silinib,0)=0;

/* ── 3. UPDATE — detal (tipzaloga+ad+girov) ─────────────────────────────── */
UPDATE OracleSorgular
SET    SorguMetni = N'select
  al.licschkre muqavile, al.tipkredita tip,
  greatest(nvl(al.procstavrez,0),nvl(al.procstavrez_19,0)) rez,
  round((al.summa+al.summa_19)*round(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6),2) qaliq,
  al.tipzaloga kod, z.name ad,
  round(nvl(nullif(al.summa_pereocen_zaloga,0), al.summa_zaloga),2) girov,
  case when al.date_restructure is not null then 1 else 0 end restrukt
from arh_licschkre al, tipzal z
where al.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (al.date_close is null or al.date_close > to_date(''{TARIX}'',''dd/mm/yyyy''))
  and length(al.licschkre) = 20
  and (al.summa+al.summa_19) <> 0
  and al.tipzaloga = z.code(+)'
WHERE  SorguAdi = N'Muhasibat — Kredit keyfiyyet detal' AND ISNULL(Silinib,0)=0;

COMMIT TRAN;
PRINT N'Kredit keyfiyyəti girov bölməsi arh_licschkre-ə keçdi.';

SELECT SorguAdi, Aktiv FROM OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Kredit girov', N'Muhasibat — Kredit keyfiyyet baza', N'Muhasibat — Kredit keyfiyyet detal')
  AND  ISNULL(Silinib,0)=0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
