/* ============================================================================
   MÜHASİBAT — Mənfəət/Zərər (P&L) Oracle sorğuları
   ----------------------------------------------------------------------------
   İki sorğu əlavə edir (idempotent — varsa təkrar yaratmır):
     1) "Muhasibat — Menfeet zerer"  — kateqoriya (sinif2) üzrə dövriyyə (aqreqat)
     2) "Muhasibat — Menfeet detal"  — hesab-səviyyə dövriyyə (drill-down)

   Struktur (kəşf edildi — arh_saldo_ls dövriyyəsi):
     GƏLİR  = sinif 6 + 7  → kredit dövriyyəsi
              60/61/63/64/65 faiz gəliri · 66/68 valyuta-ticarət · 67 komissiya · 70/72 digər
     XƏRC   = sinif 8 + 9   → debet dövriyyəsi
              84 faiz xərci · 86/88 valyuta-ticarət zərəri · 87 komissiya · 89 ehtiyat
              sinif 9 = əməliyyat xərci (opex): 9002 əmək haqqı · 9004 amortizasiya ·
              9005 təmir · 90xx sığorta/kredit/mühafizə/enerji/rabitə/hərrac/hüquq · 91xx
     Opex TAM hesab üzrə təsnif olunur (C# OpexTesnif) — 9007/9008 prefiksi bölünür.
   Hesablar hər gün mənfəətə bağlanır (debet=kredit), ona görə servis
   gəlir üçün KREDİT, xərc üçün DEBET dövriyyəsini götürür (MenfeetTesnif).
   Doğrulama: (gəlir − xərc) ≈ 50130 "cari ilin mənfəəti".

   Oracle YALNIZ SELECT (CLAUDE.md). Azərbaycan hərfi YOXDUR (charset təhlükəsiz).
   {BAS}/{SON} servis tərəfindən dd/MM/yyyy ilə əvəz olunur.
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

/* ── 1. Aqreqat — hesab-səviyyə dövriyyə (C# tam hesaba görə təsnif edir) ──── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Menfeet zerer' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Menfeet zerer', N'Mənfəət/Zərər (P&L) — sinif 6/7/8/9 dövriyyə, hesab üzrə (opex daxil)', N'select
  s.licsch hesab,
  round(sum(s.oboroti_debet_nacval),2)  debet,
  round(sum(s.oboroti_kredit_nacval),2) kredit
from odb.arh_saldo_ls s
where s.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
  and substr(s.licsch,1,1) in (''6'',''7'',''8'',''9'')
group by s.licsch', 1, @DepId, GETDATE(), 0);

/* ── 2. Detal — hesab-səviyyə dövriyyə (drill-down) ─────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Menfeet detal' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Menfeet detal', N'Mənfəət/Zərər — hesab-səviyyə dövriyyə (drill-down)', N'select
  s.licsch hesab, min(l.name_licsch) ad,
  substr(s.licsch,1,2) sinif2,
  round(sum(s.oboroti_debet_nacval),2)  debet,
  round(sum(s.oboroti_kredit_nacval),2) kredit
from odb.arh_saldo_ls s, licsch l
where s.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
  and l.licsch = s.licsch
  and substr(s.licsch,1,1) in (''6'',''7'',''8'',''9'')
group by s.licsch, substr(s.licsch,1,2)', 1, @DepId, GETDATE(), 0);

/* ── 3. Baza — işləyən aktiv (NIM məxrəci) + 50130 mənfəəti (yoxlama) ─────
   200k-sətirli tam balansı çəkmək əvəzinə yalnız 2 rəqəm qaytarır (SÜRƏT).
   date_oper = ≤SON SON İŞ GÜNÜ (şənbə/bazar boş olsa da düzgün gün tapılır). */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Menfeet baza' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Muhasibat — Menfeet baza', N'P&L bazası — işləyən aktiv + 50130 (son iş günü ≤ SON)', N'select
  round(sum(case when substr(s.licsch,1,2) in (''11'',''12'',''13'',''14'',''15'',''20'',''21'',''22'',''23'')
                  and s.saldo_ish_nacval > 0 then s.saldo_ish_nacval else 0 end),2) isleyen,
  round(sum(case when substr(s.licsch,1,5)=''50130'' then -s.saldo_ish_nacval else 0 end),2) menfeet
from odb.arh_saldo_ls s
where s.date_oper = (select max(date_oper) from odb.arh_saldo_ls
                     where date_oper <= to_date(''{SON}'',''dd/mm/yyyy''))', 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Mənfəət/Zərər sorğuları əlavə olundu.';

SELECT SorguAdi, DepartamentId, Aktiv FROM OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Menfeet zerer', N'Muhasibat — Menfeet detal', N'Muhasibat — Menfeet baza');

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
