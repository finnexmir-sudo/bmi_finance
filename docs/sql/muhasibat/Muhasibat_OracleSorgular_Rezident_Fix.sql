/* ============================================================================
   MÜHASİBAT — Rezident / Qeyri-rezident təsnifatının DÜZƏLİŞİ
   ----------------------------------------------------------------------------
   Köhnə qayda yalnız  409* + (adın son mötərizəsində 5-ci simvol '5')  və ya
   45029*  yoxlayırdı — demək olar bütün qeyri-rezidentləri qaçırırdı
   (nəticə: qeyri-rezident cəmi 628 AZN / 24 hesab, real deyil).

   YENİ qayda (accounting Excel-i əsasında — reziden_ve_qrezident_hesablar):
     Qeyri-rezident (''qr'') SAYILIR, əgər:
       (a) HÜQUQİ paylaşılan sxemlər — sxem 409 ilə başlayır VƏ 20-rəqəmli
           hesabın SON rəqəmi ''1''-dir  (məs. 40932...400000 = rez,
           40932...400001 = qeyri-rez), YAXUD
       (b) Sxem (ilk 5 rəqəm) sabit qeyri-rezident siyahısındadır:
           40065, 41015, 41025, 41045, 41931, 41941, 41943.
     Əks halda rezident (''r'').

   Qeyd: (b) siyahısı əslində "41xxx sxeminin 5-ci rəqəmi TƏK (odd) →
   qeyri-rezident" qaydasına bərabərdir (40065 də tək) — amma sabit siyahı
   kimi saxlanılır (istifadəçi seçimi).

   SCOPE — bölgü YALNIZ müştəri hesablarına aiddir:
   substr(s.licsch,1,2) in ('40','41')  (40=hüquqi, 41=fiziki müştəri hesabları).
   Bankın öz kassa/aktiv/kapital hesabları (10*, 11*, 14*, 15*, 27*, 28*, 30*,
   39*, 49*, 50* və s.) rezident/qeyri-rezident bölgüsünə DAXİL EDİLMİR —
   əks halda "Rezident" bütün balans hesablarını udur (proqram diaqnostikdən
   fərqli çıxırdı, səbəb bu idi).

   Tam UPDATE (REPLACE deyil — case ifadəsi bütövlükdə əvəz olunur).
   QEYD: bu fayl idempotentdir — əvvəlki (scope-suz) versiyanı işlətmisinizsə,
   bu yenilənmiş faylı BİR DƏ işlədin ki, 40/41 scope-u tətbiq olunsun.
   ============================================================================ */

/* ----------------------------------------------------------------------------
   ADDIM 0 (İSTƏYƏ BAĞLI) — ORACLE-da yoxlama SELECT-i.
   Migrasiyadan ƏVVƏL Oracle client-də işlət: yeni təsnifatın Excel ilə
   uyğunluğunu sxem + son rəqəm səviyyəsində gör. (Yalnız SELECT — Oracle-a
   heç nə yazmır.) {TARIX}-i dashboard tarixi ilə əvəz et, məs. 16/07/2026.

   select
     substr(s.licsch,1,5) shema,
     substr(rtrim(s.licsch),-1,1) son_reqem,
     case when (substr(s.licsch,1,3)='409' and substr(rtrim(s.licsch),-1,1)='1')
               or substr(s.licsch,1,5) in ('40065','41015','41025','41045','41931','41941','41943')
          then 'qr' else 'r' end tip,
     count(*) say,
     round(sum(abs(s.saldo_ish_nacval)),2) mebleg
   from odb.arh_saldo_ls s, licsch l
   where l.licsch = s.licsch
     and s.date_oper = to_date('16/07/2026','dd/mm/yyyy')
     and (l.date_close_licsch is null or l.date_close_licsch >= to_date('16/07/2026','dd/mm/yyyy'))
     and (substr(s.licsch,1,2) in ('40','41'))
   group by substr(s.licsch,1,5), substr(rtrim(s.licsch),-1,1),
     case when (substr(s.licsch,1,3)='409' and substr(rtrim(s.licsch),-1,1)='1')
               or substr(s.licsch,1,5) in ('40065','41015','41025','41045','41931','41941','41943')
          then 'qr' else 'r' end
   order by shema, son_reqem;
   ---------------------------------------------------------------------------- */

SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

/* ── 1. Aqreqat: "Muhasibat — Rezident" ──────────────────────────────────── */
UPDATE OracleSorgular
SET    SorguMetni = N'select
  case when (substr(s.licsch,1,3)=''409'' and substr(rtrim(s.licsch),-1,1)=''1'')
            or substr(s.licsch,1,5) in (''40065'',''41015'',''41025'',''41045'',''41931'',''41941'',''41943'')
       then ''qr'' else ''r'' end tip,
  round(sum(abs(s.saldo_ish_nacval)),2) mebleg,
  count(*) say
from odb.arh_saldo_ls s, licsch l
where l.licsch = s.licsch
  and s.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (l.date_close_licsch is null or l.date_close_licsch >= to_date(''{TARIX}'',''dd/mm/yyyy''))
  and substr(s.licsch,1,2) in (''40'',''41'')
group by case when (substr(s.licsch,1,3)=''409'' and substr(rtrim(s.licsch),-1,1)=''1'')
            or substr(s.licsch,1,5) in (''40065'',''41015'',''41025'',''41045'',''41931'',''41941'',''41943'')
       then ''qr'' else ''r'' end',
       Mahiyyet = N'Rezident/qeyri-rezident bölgüsü (müştəri hesabları 40/41; ABS qalıq) — 409 son rəqəm + qeyri-rez sxem siyahısı'
WHERE  SorguAdi = N'Muhasibat — Rezident' AND ISNULL(Silinib,0)=0;

/* ── 2. Detal (drill-down): "Muhasibat — Rezident detal" — EYNİ case ─────── */
UPDATE OracleSorgular
SET    SorguMetni = N'select
  case when (substr(s.licsch,1,3)=''409'' and substr(rtrim(s.licsch),-1,1)=''1'')
            or substr(s.licsch,1,5) in (''40065'',''41015'',''41025'',''41045'',''41931'',''41941'',''41943'')
       then ''qr'' else ''r'' end tip,
  s.licsch hesab, l.name_licsch ad,
  round(abs(s.saldo_ish_nacval),2) mebleg
from odb.arh_saldo_ls s, licsch l
where l.licsch = s.licsch
  and s.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (l.date_close_licsch is null or l.date_close_licsch >= to_date(''{TARIX}'',''dd/mm/yyyy''))
  and substr(s.licsch,1,2) in (''40'',''41'')
  and abs(s.saldo_ish_nacval) <> 0'
WHERE  SorguAdi = N'Muhasibat — Rezident detal' AND ISNULL(Silinib,0)=0;

COMMIT TRAN;
PRINT N'Rezident təsnifatı yeniləndi (409 son rəqəm + qeyri-rez sxem siyahısı).';

/* Yoxlama — hər iki sorğu yeni məntiqdədir? */
SELECT SorguAdi,
       CASE WHEN SorguMetni LIKE '%40065%' THEN 'YENİ' ELSE 'KÖHNƏ' END AS veziyyet
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Rezident', N'Muhasibat — Rezident detal')
  AND  ISNULL(Silinib,0)=0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
