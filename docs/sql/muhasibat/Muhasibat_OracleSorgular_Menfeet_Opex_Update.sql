/* ============================================================================
   MÜHASİBAT — Mənfəət/Zərər (P&L): Əməliyyat xərci (opex, sinif 9) əlavəsi
   ----------------------------------------------------------------------------
   Muhasibat_OracleSorgular_Menfeet.sql seed-i IF NOT EXISTS olduğu üçün MÖVCUD
   bazalardakı iki sorğunu YENİLƏMİR. Bu skript onların SorguMetni-ni yeniləyir:

     1) "Muhasibat — Menfeet zerer"  — aqreqatı sinif2 yerinə TAM hesab üzrə qaytarır
                                        və sinif 9 (opex) əlavə edir.
     2) "Muhasibat — Menfeet detal"  — drill-down filtrinə sinif 9 əlavə edir.

   NƏTİCƏ: dashboard xərc tərəfinə əməliyyat xərcləri (maaş, amortizasiya, sığorta,
   enerji, rabitə, ...) düşür → "Ehtiyatdan əvvəl mənfəət" düzəlir (aşağı düşür),
   "Xalis ehtiyat" (törədilən) real xalis provisiona yaxınlaşır.

   Bu YALNIZ SQL Server (OracleSorgular) UPDATE-idir — Oracle bazasına toxunmur.
   Oracle sorğuları hələ də YALNIZ SELECT-dir (daha çox hesab çəkir, o qədər).
   Təhlükəsizdir: yalnız iki sətrin SorguMetni dəyişir, geri qaytarıla bilər.
   ============================================================================ */
SET NOCOUNT ON;

-- ── ƏVVƏL: cari SorguMetni-ni göstər (migration-dan əvvəl yoxlama) ──────────
SELECT SorguAdi, LEN(SorguMetni) AS UzunlukEvvel, SorguMetni AS MetniEvvel
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Menfeet zerer', N'Muhasibat — Menfeet detal')
  AND  ISNULL(Silinib,0) = 0;

BEGIN TRY
BEGIN TRAN;

/* ── 1. Aqreqat — hesab-səviyyə + sinif 9 ───────────────────────────────── */
UPDATE OracleSorgular
SET    Mahiyyet   = N'Mənfəət/Zərər (P&L) — sinif 6/7/8/9 dövriyyə, hesab üzrə (opex daxil)',
       SorguMetni = N'select
  s.licsch hesab,
  round(sum(s.oboroti_debet_nacval),2)  debet,
  round(sum(s.oboroti_kredit_nacval),2) kredit
from odb.arh_saldo_ls s
where s.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
  and substr(s.licsch,1,1) in (''6'',''7'',''8'',''9'')
group by s.licsch'
WHERE  SorguAdi = N'Muhasibat — Menfeet zerer' AND ISNULL(Silinib,0) = 0;

/* ── 2. Detal — drill-down filtrinə sinif 9 ─────────────────────────────── */
UPDATE OracleSorgular
SET    SorguMetni = N'select
  s.licsch hesab, min(l.name_licsch) ad,
  substr(s.licsch,1,2) sinif2,
  round(sum(s.oboroti_debet_nacval),2)  debet,
  round(sum(s.oboroti_kredit_nacval),2) kredit
from odb.arh_saldo_ls s, licsch l
where s.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
  and l.licsch = s.licsch
  and substr(s.licsch,1,1) in (''6'',''7'',''8'',''9'')
group by s.licsch, substr(s.licsch,1,2)'
WHERE  SorguAdi = N'Muhasibat — Menfeet detal' AND ISNULL(Silinib,0) = 0;

COMMIT TRAN;
PRINT N'Mənfəət/Zərər opex (sinif 9) əlavəsi tətbiq olundu.';

-- ── SONRA: yenilənmiş SorguMetni-ni göstər ──────────────────────────────────
SELECT SorguAdi, LEN(SorguMetni) AS UzunlukSonra, SorguMetni AS MetniSonra
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Menfeet zerer', N'Muhasibat — Menfeet detal')
  AND  ISNULL(Silinib,0) = 0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
