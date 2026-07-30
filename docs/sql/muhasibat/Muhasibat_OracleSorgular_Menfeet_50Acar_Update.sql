/* ============================================================================
   MÜHASİBAT — Mənfəət/Zərər (P&L): 50-AÇAR prinsipinə keçid (arh_dd yazılışları)
   ----------------------------------------------------------------------------
   Mühasibin qaydası: gəlir/xərc yalnız MƏNFƏƏT (50) hesabı ilə üzbəüz yazılışlarla
   sayılmalıdır — hesabın ümumi dövriyyəsi (arh_saldo_ls) ilə YOX:

     GƏLİR (sinif 6/7):
        + debet 6x/7x → kredit 50   (gəlirin mənfəətə bağlanması)
        − debet 50    → kredit 6x/7x (əks yazılış / korreksiya — çıxılır)
     XƏRC (sinif 8/9):
        + debet 50    → kredit 8x/9x (xərcin mənfəətə bağlanması)
        − debet 8x/9x → kredit 50    (əks yazılış / korreksiya — çıxılır)

   İSTİSNA: sinif 89 (ehtiyat ayırmaları) bu hesablanmaya DAXİL DEYİL — UI-dakı
   "Ehtiyat ayırmaları" rəqəmi törəmədir: ehtiyatdan əvvəl mənfəət − GL (50130).

   Mənbə cədvəl: odb.arh_dd (yazılış jurnalı — debet, kredit, summa_v_nacval).
   Nəticə sütunları ƏVVƏLKİ İLƏ EYNİDİR (hesab, debet, kredit) — gəlir hesablarının
   neti KREDIT sütununda, xərc hesablarının neti DEBET sütununda qayıdır, ona görə
   MuhasibatService KODU DƏYİŞMİR (meb = gelir ? kredit : debet). Say = siyahı:
   aqreqat və detal eyni prinsiplə gedir.

   Bu YALNIZ SQL Server (OracleSorgular) UPDATE-idir — Oracle bazasına toxunmur.
   Oracle sorğuları hələ də YALNIZ SELECT-dir. Geri qaytarmaq üçün əvvəlki mətnlər
   aşağıdakı "ƏVVƏL" SELECT-in çıxışında görünür (saxla!).
   ============================================================================ */
SET NOCOUNT ON;

-- ── ƏVVƏL: cari SorguMetni-ni göstər (geri qaytarma üçün SAXLA) ─────────────
SELECT SorguAdi, LEN(SorguMetni) AS UzunlukEvvel, SorguMetni AS MetniEvvel
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Menfeet zerer', N'Muhasibat — Menfeet detal')
  AND  ISNULL(Silinib,0) = 0;

BEGIN TRY
BEGIN TRAN;

/* ── 1. Aqreqat — 50-açar, hesab üzrə net ───────────────────────────────── */
UPDATE OracleSorgular
SET    Mahiyyet   = N'Mənfəət/Zərər (P&L) — arh_dd, 50-açar: gəlir=(6/7→50)−(50→6/7), xərc=(50→8/9)−(8/9→50)',
       SorguMetni = N'select hesab,
       round(sum(deb),2) debet,
       round(sum(kre),2) kredit
from (
  select dd.debet hesab, 0 deb, dd.summa_v_nacval kre
  from   odb.arh_dd dd
  where  dd.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
    and  substr(dd.debet,1,1) in (''6'',''7'') and substr(dd.kredit,1,2) = ''50''
  union all
  select dd.kredit, 0, -dd.summa_v_nacval
  from   odb.arh_dd dd
  where  dd.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
    and  substr(dd.kredit,1,1) in (''6'',''7'') and substr(dd.debet,1,2) = ''50''
  union all
  select dd.kredit, dd.summa_v_nacval, 0
  from   odb.arh_dd dd
  where  dd.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
    and  substr(dd.kredit,1,1) in (''8'',''9'') and substr(dd.kredit,1,2) <> ''89''
    and  substr(dd.debet,1,2) = ''50''
  union all
  select dd.debet, -dd.summa_v_nacval, 0
  from   odb.arh_dd dd
  where  dd.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
    and  substr(dd.debet,1,1) in (''8'',''9'') and substr(dd.debet,1,2) <> ''89''
    and  substr(dd.kredit,1,2) = ''50''
)
group by hesab'
WHERE  SorguAdi = N'Muhasibat — Menfeet zerer' AND ISNULL(Silinib,0) = 0;

/* ── 2. Detal — eyni 50-açar prinsipi, hesab adı ilə (say = siyahı) ─────── */
UPDATE OracleSorgular
SET    Mahiyyet   = N'Mənfəət/Zərər detal — arh_dd, 50-açar (aqreqatla eyni prinsip)',
       SorguMetni = N'select t.hesab, min(l.name_licsch) ad,
       substr(t.hesab,1,2) sinif2,
       round(sum(t.deb),2) debet,
       round(sum(t.kre),2) kredit
from (
  select dd.debet hesab, 0 deb, dd.summa_v_nacval kre
  from   odb.arh_dd dd
  where  dd.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
    and  substr(dd.debet,1,1) in (''6'',''7'') and substr(dd.kredit,1,2) = ''50''
  union all
  select dd.kredit, 0, -dd.summa_v_nacval
  from   odb.arh_dd dd
  where  dd.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
    and  substr(dd.kredit,1,1) in (''6'',''7'') and substr(dd.debet,1,2) = ''50''
  union all
  select dd.kredit, dd.summa_v_nacval, 0
  from   odb.arh_dd dd
  where  dd.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
    and  substr(dd.kredit,1,1) in (''8'',''9'') and substr(dd.kredit,1,2) <> ''89''
    and  substr(dd.debet,1,2) = ''50''
  union all
  select dd.debet, -dd.summa_v_nacval, 0
  from   odb.arh_dd dd
  where  dd.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
    and  substr(dd.debet,1,1) in (''8'',''9'') and substr(dd.debet,1,2) <> ''89''
    and  substr(dd.kredit,1,2) = ''50''
) t, licsch l
where l.licsch = t.hesab
group by t.hesab, substr(t.hesab,1,2)'
WHERE  SorguAdi = N'Muhasibat — Menfeet detal' AND ISNULL(Silinib,0) = 0;

COMMIT TRAN;
PRINT N'Mənfəət/Zərər 50-açar prinsipi tətbiq olundu.';

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
