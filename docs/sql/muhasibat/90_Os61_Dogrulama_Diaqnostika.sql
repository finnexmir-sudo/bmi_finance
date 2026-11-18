/* ============================================================================
   ÖS-6.1 — EKRANDAKI RƏQƏMLƏRİN BAZADA DOĞRULANMASI (Oracle, YALNIZ SELECT)
   18.11.2026. Oktyabr 2026 üçün ({D1}=01-10-2026, {D2}=31-10-2026) — başqa ay
   yoxlayırsansa bu iki tarixi dəyiş.

   Hər blok ekrandakı BİR xananı yenidən hesablayır — tətbiqin (MuhasibatService.
   Os61Async) işlətdiyi ŞƏRTİN EYNİSİ. Nəticəni ekranla tutuşdur:
     - uyğun gəlirsə → kod düzgün işləyir, say fərqi varsa mənbə Excel/köhnə
       hesablamada başqa bir qayda var demək (bura yaz, bax).
     - uyğun gəlmirsə → aşağıdakı şərtin özü yanlış ola bilər, hansı kodda
       olduğunu de.

   PL/SQL Developer-də, FOXPRO@bmi bağlantısı ilə, HƏR bloku AYRI-AYRI işlət
   (hamısını bir yerdə YOX — bəzi SELECT-lər ayrı COUNT verir, nəticələri
   qarışdırmamaq üçün).
   ========================================================================== */


/* ───────────────────────── KOD 2 — "depozit hesabları" ───────────────────── */

-- D2 (bu ay açılan, cəmi) — ekranda "0" olmalıdır
select count(*) d2_hesab_sayi
from odb.licsch l
where substr(l.licsch,1,3) in ('411','412')
  and l.date_open_licsch between to_date('{D1}','dd-mm-yyyy') and to_date('{D2}','dd-mm-yyyy');

-- E2 (bu ay açılan, xarici valyuta)
select count(*) e2_xarici_valyuta
from odb.licsch l
where substr(l.licsch,1,3) in ('411','412')
  and substr(l.licsch,6,2) <> '00'
  and l.date_open_licsch between to_date('{D1}','dd-mm-yyyy') and to_date('{D2}','dd-mm-yyyy');

-- F2 (bu ay açılan, sahib sayı — ad üzrə distinct)
select count(distinct l.name_licsch) f2_sahib_sayi
from odb.licsch l
where substr(l.licsch,1,3) in ('411','412')
  and l.date_open_licsch between to_date('{D1}','dd-mm-yyyy') and to_date('{D2}','dd-mm-yyyy');

-- G2 (dövrün sonuna cəmi — hesab sayı) — ekranda "0" idi
select count(*) g2_dovr_sonu
from odb.licsch l
where substr(l.licsch,1,3) in ('411','412','391')
  and l.date_open_licsch <= to_date('{D2}','dd-mm-yyyy')
  and (l.date_close_licsch is null or l.date_close_licsch > to_date('{D2}','dd-mm-yyyy'));


/* ───────────────────────── KOD 3 — "kredit hesabları" ─────────────────────── */

-- D3 (bu ay açılan, cəmi) — ekranda 8
select count(*) d3_hesab_sayi
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and l.date_open_licsch between to_date('{D1}','dd-mm-yyyy') and to_date('{D2}','dd-mm-yyyy');

-- E3 (bu ay açılan, xarici valyuta) — ekranda 0
select count(*) e3_xarici_valyuta
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and substr(l.licsch,6,2) <> '00'
  and l.date_open_licsch between to_date('{D1}','dd-mm-yyyy') and to_date('{D2}','dd-mm-yyyy');

-- F3 (bu ay açılan, sahib sayı) — ekranda 2
select count(distinct l.name_licsch) f3_sahib_sayi
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and l.date_open_licsch between to_date('{D1}','dd-mm-yyyy') and to_date('{D2}','dd-mm-yyyy');

-- G3 (dövrün sonuna cəmi — hesab sayı) — ekranda 643
select count(*) g3_dovr_sonu
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and substr(l.licsch,5,1) in ('0','5')
  and substr(l.licsch,3,1) <> '9'
  and l.date_open_licsch <= to_date('{D2}','dd-mm-yyyy')
  and (l.date_close_licsch is null or l.date_close_licsch > to_date('{D2}','dd-mm-yyyy'));

-- H3 (dövrün sonuna, aktiv — Qalıq>0) — ekranda 3. QALIQ = AZN-də abs(saldo_ish_nacval),
-- FX-də abs(saldo_ish_inval) (valyuta sütunu = substr(licsch,6,2)).
select count(*) h3_aktiv
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and substr(l.licsch,5,1) in ('0','5')
  and substr(l.licsch,3,1) <> '9'
  and l.date_open_licsch <= to_date('{D2}','dd-mm-yyyy')
  and (l.date_close_licsch is null or l.date_close_licsch > to_date('{D2}','dd-mm-yyyy'))
  and (case when substr(l.licsch,6,2) = '00' then abs(nvl(l.saldo_ish_nacval,0))
            else abs(nvl(l.saldo_ish_inval,0)) end) > 0;

-- K3 (dövrün sonuna, sahib sayı — kredit hesablarında REGNOM-a görə distinct,
-- AD-a görə YOX — bu, Excel-in "Kredit hesablari_mod" vərəqinin öz qaydasıdır)
-- ekranda 619
select count(distinct substr(l.licsch,10,6)) k3_sahib_sayi
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and substr(l.licsch,5,1) in ('0','5')
  and substr(l.licsch,3,1) <> '9'
  and l.date_open_licsch <= to_date('{D2}','dd-mm-yyyy')
  and (l.date_close_licsch is null or l.date_close_licsch > to_date('{D2}','dd-mm-yyyy'));


/* ───────────────────────── KOD 4 — "cari hesablar" ─────────────────────────── */

-- D4 (bu ay açılan, cəmi) — ekranda 9
select count(*) d4_hesab_sayi
from odb.licsch l
where substr(l.licsch,1,2) in ('38','39','40','41')
  and substr(l.licsch,1,3) not in ('411','412')
  and l.date_open_licsch between to_date('{D1}','dd-mm-yyyy') and to_date('{D2}','dd-mm-yyyy');

-- F4 (bu ay açılan, sahib sayı) — ekranda 7
select count(distinct l.name_licsch) f4_sahib_sayi
from odb.licsch l
where substr(l.licsch,1,2) in ('38','39','40','41')
  and substr(l.licsch,1,3) not in ('411','412')
  and l.date_open_licsch between to_date('{D1}','dd-mm-yyyy') and to_date('{D2}','dd-mm-yyyy');

-- G4 (dövrün sonuna cəmi) = Kod 19 (G20 fiziki + G22 hüquqi birlikdə) — ekranda 954
select
  ( select count(*) from odb.licsch l
    where substr(l.licsch,1,2) = '41'
      and substr(l.licsch,1,3) not in ('411','412')
      and substr(l.licsch,1,3) <> '391'
      and substr(l.licsch,3,1) <> '9'
      and l.date_open_licsch <= to_date('{D2}','dd-mm-yyyy')
      and (l.date_close_licsch is null or l.date_close_licsch > to_date('{D2}','dd-mm-yyyy')) )
  +
  ( select count(*) from odb.licsch l
    where substr(l.licsch,1,2) in ('38','39','40')
      and substr(l.licsch,3,1) <> '9'
      and l.date_open_licsch <= to_date('{D2}','dd-mm-yyyy')
      and (l.date_close_licsch is null or l.date_close_licsch > to_date('{D2}','dd-mm-yyyy')) )
  as g4_dovr_sonu
from dual;

-- K4 (dövrün sonuna, sahib sayı — cari hesablarda AD-a görə distinct) — ekranda 813
select count(distinct l.name_licsch) k4_sahib_sayi
from odb.licsch l
where ( (substr(l.licsch,1,2) = '41' and substr(l.licsch,1,3) not in ('411','412') and substr(l.licsch,1,3) <> '391')
        or substr(l.licsch,1,2) in ('38','39','40') )
  and substr(l.licsch,3,1) <> '9'
  and l.date_open_licsch <= to_date('{D2}','dd-mm-yyyy')
  and (l.date_close_licsch is null or l.date_close_licsch > to_date('{D2}','dd-mm-yyyy'));


/* ───────────────────────── KOD 1 — "cəmi" (yoxlama üçün, Kod2+3+4) ─────────── */
-- D1 = D2+D3+D4 (0+8+9=17 — ekranla tutuşdur), G1 = G2+G3+G4 (0+643+954=1597)
