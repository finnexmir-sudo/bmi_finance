/* ============================================================================
   ÖS-6.1 — "kredit açıqdır" arh_licschkre.date_close ÜZRƏ olmalıdır, licsch-in
   ÖZÜNÜN date_close_licsch-i YOX (YALNIZ SELECT). 18.11.2026.
   ========================================================================== */

-- A) arh_licschkre-nin strukturu — hansı sütunlar var, date_close/date_oper
-- dəqiq necə yazılıb:
select * from odb.arh_licschkre where rownum <= 3;

-- B) EYNİ loan (BAYDAROV V.Ü., əsas borc sətri = licsch flag=0) üçün —
-- licsch-in date_close_licsch-i NULL idi (B2-də görüldü). Bəs arh_licschkre
-- bu kreditin müqaviləsini AÇIQ sayır, yoxsa BAĞLI?
select al.licschkre, al.subschkre, al.date_oper, al.date_close,
       al.summa, al.summa_19
from odb.arh_licschkre al
where al.licschkre = '21210000001307400000'
order by al.date_oper desc
fetch first 10 rows only;

-- C) Dövrün sonuna (31-10-2026) HƏR AÇIQ kreditin (arh_licschkre, date_close
-- üzrə) sayı — bu, ekranda G3=643 ilə TUTUŞDURULMALI olan DOĞRU rəqəm:
select count(distinct al.licschkre) g3_arh_licschkre_uzre
from odb.arh_licschkre al
where al.date_oper = to_date('31-10-2026','dd-mm-yyyy')
  and (al.date_close is null or al.date_close > to_date('31-10-2026','dd-mm-yyyy'));
