/* ============================================================================
   ÖS-6.1 — "Bir kreditin 4 hesabı" iddiasının doğrulanması (YALNIZ SELECT)
   18.11.2026. "Hesab sayı" sütununun bir KREDİTİ 1 yox, neçə licsch sətri kimi
   göstərdiyini yoxlamaq üçün.

   ⚠️ İLK CƏHDDƏ regnom_kod=000012 (sira_sayi=137) seçilmişdi — bu, real
   müştəri DEYİL (çox aşağı "0000..." kodu → çox güman daxili/sistem/suspense
   hesabdır). Aşağıdakı A2 bu kodları ÇIXARIB kiçik (2-8) say aralığında
   NORMAL müştəri axtarır.
   ========================================================================== */

-- A2) Texniki regnomları (000-ilə başlayan) çıxarıb, KİÇİK sira_sayi (2-8)
-- olan REAL müştəriləri tap:
select substr(l.licsch,10,6) regnom_kod, count(*) sira_sayi
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and l.date_open_licsch <= sysdate
  and (l.date_close_licsch is null or l.date_close_licsch > sysdate)
  and substr(l.licsch,10,6) not like '0000%'
group by substr(l.licsch,10,6)
having count(*) between 2 and 8
order by sira_sayi
fetch first 30 rows only;

-- B2) A2-dən tapılan BİR regnom_kod-u BURAYA HƏRFİ YAZ (dırnaqla, && İŞLƏTMƏ —
-- əvvəlki cəhddə substitution dəyişəni əvəzlənməyib, 0 sətir qaytarıb):
select l.licsch, l.name_licsch, substr(l.licsch,1,5) bolme,
       substr(l.licsch,5,1) kredit_flag, substr(l.licsch,3,1) rezerv_flag,
       l.date_open_licsch, l.saldo_ish_nacval
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and substr(l.licsch,10,6) = '000000'   -- ⚠️ A2-dən tapılan REAL regnom-u bura yaz
order by bolme;

-- C) Depozitdə eyni yoxlama (Qrup 411/412/391) — tətbiqin özü də bu bankda
-- hazırda 0 depozit göstərir, ona görə bu sorğu 0 qaytarsa TƏBİİDİR, bug DEYİL.
select substr(l.licsch,10,6) regnom_kod, count(*) sira_sayi
from odb.licsch l
where substr(l.licsch,1,3) in ('411','412','391')
  and l.date_open_licsch <= sysdate
  and (l.date_close_licsch is null or l.date_close_licsch > sysdate)
group by substr(l.licsch,10,6)
order by sira_sayi desc
fetch first 20 rows only;
