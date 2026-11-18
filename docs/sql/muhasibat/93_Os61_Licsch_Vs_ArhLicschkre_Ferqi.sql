/* ============================================================================
   ÖS-6.1 — licsch (643) vs arh_licschkre (289) fərqinin mənbəyi (YALNIZ SELECT).
   18.11.2026. SON İŞ GÜNÜ = 30-10-2026 (31-10-2026 şənbədir, snapshot yoxdur).
   ========================================================================== */

-- A) arh_licschkre-də 30-10-2026 üçün ÜMUMİ sətir sayı və Klass bölgüsü —
-- licschkre dəyərinin ilk 2 rəqəmi (substr) licsch.licsch-in Klass-ı ilə
-- eynidirmi, yoxsa fərqli nömrələmədir?
select substr(to_char(al.licschkre),1,2) klass_kimi, count(distinct al.licschkre) sayi
from odb.arh_licschkre al
where al.date_oper = to_date('30-10-2026','dd-mm-yyyy')
  and (al.date_close is null or al.date_close > to_date('30-10-2026','dd-mm-yyyy'))
group by substr(to_char(al.licschkre),1,2)
order by sayi desc;

-- B) licsch tərəfi (bizim 643-ün ÖZÜ, eyni tarix, eyni şərt) — Klass üzrə bölgü:
select l.klass, count(*) sayi
from (
  select substr(licsch,1,2) klass
  from odb.licsch l
  where substr(l.licsch,1,2) in ('20','21')
    and substr(l.licsch,5,1) in ('0','5')
    and substr(l.licsch,3,1) <> '9'
    and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
    and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'))
) l
group by l.klass;

-- C) ANTI-JOIN — licsch-də "açıq kredit" sayılan, AMMA arh_licschkre-də
-- (30-10-2026 tarixində) HEÇ BİR qarşılığı olmayan 15 nümunə (hansı Klass/Bölmədə
-- "itdiyini" görmək üçün):
select l.licsch, l.name_licsch, substr(l.licsch,1,5) bolme, substr(l.licsch,5,1) kredit_flag
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and substr(l.licsch,5,1) in ('0','5')
  and substr(l.licsch,3,1) <> '9'
  and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
  and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'))
  and not exists (
    select 1 from odb.arh_licschkre al
    where al.licschkre = l.licsch
      and al.date_oper = to_date('30-10-2026','dd-mm-yyyy')
  )
fetch first 15 rows only;

-- D) TƏRS — arh_licschkre-də (30-10-2026, açıq) olan, AMMA licsch-in bizim
-- "açıq kredit" şərtini (flag 0/5, rezerv<>9, date_close_licsch) ÖDƏMƏYƏN
-- 15 nümunə:
select al.licschkre, al.date_open, al.date_close, al.summa,
       (select l.name_licsch from odb.licsch l where l.licsch = al.licschkre) adi,
       (select substr(l.licsch,5,1) from odb.licsch l where l.licsch = al.licschkre) licsch_flag,
       (select l.date_close_licsch from odb.licsch l where l.licsch = al.licschkre) licsch_date_close
from odb.arh_licschkre al
where al.date_oper = to_date('30-10-2026','dd-mm-yyyy')
  and (al.date_close is null or al.date_close > to_date('30-10-2026','dd-mm-yyyy'))
  and not exists (
    select 1 from odb.licsch l
    where l.licsch = al.licschkre
      and substr(l.licsch,5,1) in ('0','5')
      and substr(l.licsch,3,1) <> '9'
      and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
      and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'))
  )
fetch first 15 rows only;
