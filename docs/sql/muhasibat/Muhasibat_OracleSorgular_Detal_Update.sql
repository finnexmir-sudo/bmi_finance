/* ============================================================================
   MÜHASİBAT DASHBOARD — Drill-down (detal) üçün OracleSorgular YENİLƏMƏ
   ----------------------------------------------------------------------------
   Bir dəfə işlət. Mövcud 4 sorğuya identifikasiya sütunu əlavə edir (cərrahi
   REPLACE — sorğu məntiqi dəyişmir) və 1 yeni "Rezident detal" sorğusu qoyur.
   Hər addım idempotentdir (təkrar işlətsən dublikat/ikiqat sütun yaratmır).

   Bu script OLMADAN da drill-down işləyir — sadəcə hesab ADI/müqavilə nömrəsi
   görünmür (yalnız kod). Bu script-dən sonra detal tam olur.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

/* ── 1. Balans: hesab adı (ch.name_licsch) ───────────────────────────────── */
UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni, 'END AS dep_tip', 'END AS dep_tip, ch.name_licsch AS ad')
WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari'
  AND  SorguMetni NOT LIKE '%name_licsch AS ad%';

/* ── 2. Depozit: hesab kodu (l.licsch) — hər iki UNION budağına ──────────── */
UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni, '-l.saldo_ish_nacval qaliq', '-l.saldo_ish_nacval qaliq, l.licsch hesab')
WHERE  SorguAdi = N'Muhasibat — Depozit hesablari'
  AND  SorguMetni NOT LIKE '%l.licsch hesab%';

/* ── 3. Kredit: müqavilə hesabı (lk.licschkre) ──────────────────────────── */
UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni, ' gec_gun', ' gec_gun, lk.licschkre muqavile')
WHERE  SorguAdi = N'Muhasibat — Kredit portfeli'
  AND  SorguMetni NOT LIKE '%licschkre muqavile%';

/* ── 4. Valyuta: əməliyyat tarixi (d.date_oper) — hər iki budağa ─────────── */
UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni, 'd.summa_v_inval*d.kurs_valuti azn', 'd.summa_v_inval*d.kurs_valuti azn, d.date_oper tarix')
WHERE  SorguAdi = N'Muhasibat — Valyuta emeliyyatlari'
  AND  SorguMetni NOT LIKE '%d.date_oper tarix%';

/* ── 5. Rezident detal — hesab-səviyyə (aqreqat sorğu ilə eyni case) ─────── */
DECLARE @DepId INT;
SELECT @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi = N'Muhasibat — Rezident' AND ISNULL(Silinib,0)=0;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler
    WHERE (Ad LIKE N'%ühasib%' OR Ad LIKE N'%aliyy%') AND ISNULL(Silinib,0)=0 ORDER BY Id;

IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Rezident detal' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Rezident detal', N'Rezident/qeyri-rezident — hesab-səviyyə detalı (drill-down)', N'select
  case when (substr(s.licsch,0,3)=''409''
             and substr(regexp_substr(l.name_licsch,''\(([^()]*)\)\s*$'',1,1,null,1),5,1)=''5'')
            or substr(s.licsch,0,5)=''45029''
       then ''qr'' else ''r'' end tip,
  s.licsch hesab, l.name_licsch ad,
  round(abs(s.saldo_ish_nacval),2) mebleg
from odb.arh_saldo_ls s, licsch l
where l.licsch = s.licsch
  and s.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (l.date_close_licsch is null or l.date_close_licsch >= to_date(''{TARIX}'',''dd/mm/yyyy''))
  and abs(s.saldo_ish_nacval) <> 0', 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Drill-down sütunları və Rezident detal sorğusu hazırdır.';

SELECT SorguAdi,
       CASE WHEN SorguMetni LIKE '%name_licsch AS ad%'   THEN 'ad+'   ELSE '' END
     + CASE WHEN SorguMetni LIKE '%l.licsch hesab%'       THEN 'hesab+' ELSE '' END
     + CASE WHEN SorguMetni LIKE '%licschkre muqavile%'   THEN 'muqavile+' ELSE '' END
     + CASE WHEN SorguMetni LIKE '%d.date_oper tarix%'    THEN 'tarix+' ELSE '' END AS Elave
FROM   OracleSorgular
WHERE  SorguAdi LIKE N'Muhasibat — %' ORDER BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
