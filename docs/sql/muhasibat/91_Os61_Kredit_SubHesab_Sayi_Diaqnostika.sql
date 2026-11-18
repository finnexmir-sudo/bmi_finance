/* ============================================================================
   ÖS-6.1 — "Bir kreditin 4 hesabı" iddiasının doğrulanması (YALNIZ SELECT)
   18.11.2026. "Hesab sayı" sütununun bir KREDİTİ 1 yox, neçə licsch sətri kimi
   göstərdiyini yoxlamaq üçün.
   ========================================================================== */

-- A) Hər müştərinin (regnom) AKTIV kredit (Klass 20/21) sətirlərinin sayı —
-- əgər "bir kreditin 4 hesabı" doğrudursa, TƏK kredit müqaviləsi olan müştərinin
-- sira_sayi=4 (depozitdə isə 2) çıxmalıdır.
select substr(l.licsch,10,6) regnom_kod, count(*) sira_sayi
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and l.date_open_licsch <= sysdate
  and (l.date_close_licsch is null or l.date_close_licsch > sysdate)
group by substr(l.licsch,10,6)
order by sira_sayi desc
fetch first 30 rows only;

-- B) Yuxarıdakı nəticədən sira_sayi=4 (və ya 8, 12 — çoxkreditli müştəri) olan
-- BİR regnom_kod-u aşağıya yaz, bütün sub-hesabları ətraflı gör:
select l.licsch, l.name_licsch, substr(l.licsch,1,5) bolme,
       substr(l.licsch,5,1) kredit_flag, substr(l.licsch,3,1) rezerv_flag,
       l.date_open_licsch, l.saldo_ish_nacval
from odb.licsch l
where substr(l.licsch,1,2) in ('20','21')
  and substr(l.licsch,10,6) = '&&REGNOM_KODU'   -- A-dan tapılan regnom-u bura yaz
order by bolme;

-- C) Depozitdə eyni yoxlama (Qrup 411/412/391), "/2" iddiası üçün:
select substr(l.licsch,10,6) regnom_kod, count(*) sira_sayi
from odb.licsch l
where substr(l.licsch,1,3) in ('411','412','391')
  and l.date_open_licsch <= sysdate
  and (l.date_close_licsch is null or l.date_close_licsch > sysdate)
group by substr(l.licsch,10,6)
order by sira_sayi desc
fetch first 20 rows only;
