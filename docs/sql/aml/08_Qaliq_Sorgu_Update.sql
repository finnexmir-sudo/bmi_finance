/* ═══════════════════════════════════════════════════════════════════════════
   AML_HESAB_SORGU_QALIQ — sorğu mətninin YENİLƏNMƏSİ · 31.08.2026
   ───────────────────────────────────────────────────────────────────────────
   Bu skript SQL SERVER-də (FinNex_Maliyye_Db) işlədilir — `OracleSorgular`
   bizim öz cədvəlimizdir. Oracle-a heç bir yazı YOXDUR.

   ⚠️ ƏVVƏLCƏ 07_Qaliq_Yeni_Sorgu_Yoxlama.sql-i Oracle-da işlədin və
      rəqəmlərin uyğun gəldiyini görün. Yalnız ondan sonra buranı işlədin.

   90_AML_OracleSorgular.sql yalnız «yoxdursa əlavə et» edir — mövcud sətri
   yeniləmir. Ona görə bu ayrıca UPDATE skripti lazımdır.

   Skript ardıcıllığı: ƏVVƏL göstər → UPDATE → SONRA göstər (CLAUDE.md qaydası).
   ═══════════════════════════════════════════════════════════════════════════ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.OracleSorgular', N'U') IS NULL
BEGIN
    RAISERROR(N'OracleSorgular cədvəli tapılmadı — səhv bazadasınız.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.OracleSorgular
               WHERE SorguAdi = N'AML_HESAB_SORGU_QALIQ' AND ISNULL(Silinib,0) = 0)
BEGIN
    RAISERROR(N'AML_HESAB_SORGU_QALIQ tapılmadı — əvvəlcə 90_AML_OracleSorgular.sql işlədin.', 16, 1);
    RETURN;
END

/* ── 1) ƏVVƏLKİ HAL (nə dəyişəcək) ──────────────────────────────────────── */
PRINT N'--- ƏVVƏL ---';
SELECT Id, SorguAdi, Aktiv, LEN(SorguMetni) AS Uzunluq, SorguMetni
  FROM dbo.OracleSorgular
 WHERE SorguAdi = N'AML_HESAB_SORGU_QALIQ' AND ISNULL(Silinib,0) = 0;

BEGIN TRAN;

UPDATE dbo.OracleSorgular
   SET Mahiyyet = N'AML — Hesab üzrə sorğu şapkası: hesabın adı (odb.accounts.name_latin) + giriş/son qalıq. Tokenlər: {HESAB}, {TARIX1}, {TARIX2}. Sütunlar: NAME_LATIN, GIR_QALIQ, SON_QALIQ. 31.08.2026: üç hissə müstəqil skalyar alt-sorğuya ayrıldı (daxili join biri boş olanda üçünü də itirirdi); hər iki qalıq DƏQİQ tarix əvəzinə həmin tarixə QƏDƏR sonuncu günün saldo_ish qalığıdır (bugünkü/həftəsonu qalığı hələ yazılmır). Tərif istifadəçinindir: giriş = TARIX1-dəki son qalıq, son = TARIX2-dəki son qalıq.',
       SorguMetni = N'select
  (select max(ac.name_latin)
     from odb.accounts ac
    where ac.licsch = ''{HESAB}'')                                          name_latin,

  (select max(case when substr(t.licsch,6,2) = ''00''
                   then abs(t.saldo_ish_nacval)
                   else abs(t.saldo_ish_inval) end)
            keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = ''{HESAB}''
      and t.date_oper <= to_date(''{TARIX1}'',''dd/mm/yyyy''))              gir_qaliq,

  (select max(case when substr(t.licsch,6,2) = ''00''
                   then abs(t.saldo_ish_nacval)
                   else abs(t.saldo_ish_inval) end)
            keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = ''{HESAB}''
      and t.date_oper <= to_date(''{TARIX2}'',''dd/mm/yyyy''))              son_qaliq
  from dual',
       Aktiv = 1
 WHERE SorguAdi = N'AML_HESAB_SORGU_QALIQ' AND ISNULL(Silinib,0) = 0;

PRINT N'Yenilənən sətir sayı: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' (1 olmalıdır)';

COMMIT TRAN;

/* ── 2) SONRAKI HAL ─────────────────────────────────────────────────────── */
PRINT N'--- SONRA ---';
SELECT Id, SorguAdi, Aktiv, LEN(SorguMetni) AS Uzunluq, SorguMetni
  FROM dbo.OracleSorgular
 WHERE SorguAdi = N'AML_HESAB_SORGU_QALIQ' AND ISNULL(Silinib,0) = 0;

/* ── 3) SONRA NƏ ETMƏLİ ─────────────────────────────────────────────────────
   Servis sorğuları sorğu boyu keşləyir (`_cache`), sessiya arası saxlamır —
   yəni SƏHİFƏNİ YENİDƏN AÇMAQ kifayətdir, tətbiqi restart etmək lazım deyil.

   Yoxlama: Risk → AML → Hesab üzrə sorğu
       Hesab  41010000000008700000
       Dövr   28.08.2026 – 31.08.2026
   Gözlənilən: Giriş qalığı 1 272,13 · Son qalıq 1 272,13 · Hesabın adı dolu.

   ⚠️ İKİSİ EYNİ ÇIXIR — SƏHV DEYİL. Tərifə görə giriş = 28/08-in son qalığı;
   31/08-ə qədər sonrakı əməliyyat günü yoxdur (29/30 həftəsonu, 31 hələ
   bağlanmayıb), ona görə son qalıq da elə 28/08-in qalığıdır. Dövrü daha
   geniş götürdükdə (məs. 20.08–28.08) ikisi fərqlənəcək.
   ═══════════════════════════════════════════════════════════════════════════ */
