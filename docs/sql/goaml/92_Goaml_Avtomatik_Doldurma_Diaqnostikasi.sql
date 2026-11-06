-- ============================================================================
-- GOAML — AYSEL-in "Təsdiq et" / Save zamanı özü doldurduğu sahələrin
-- MƏNBƏYİNİ (hansı SELECT/cədvəldən oxuduğunu) tapmaq üçün diaqnostika
-- İKİNCİ DALĞA (06.11.2026) — birinci versiyada iki şey üzə çıxdı:
--   1) A hissəsi ORA-00904 "S"."PROGRAM": invalid identifier verdi — bu
--      MƏNİM SƏHVİM idi: `PROGRAM` V$SESSION-un sütunudur, V$SQL-də BELƏ BİR
--      SÜTUN YOXDUR (V$SQL-də yalnız `MODULE` var). Aşağıda düzəldilib.
--   2) B hissəsi (v$sql_bind_capture) 0 sətir qaytardı — Oracle HƏR bağlı
--      dəyəri SAXLAMIR, bind capture defolt olaraq seçicidir və adi (bind-
--      sensitive olmayan) sadə SELECT-lərdə çox vaxt HEÇ İŞƏ DÜŞMÜR. Bu
--      üsula GÜVƏNMƏ — B aşağıda YALNIZ son çarə kimi qalır, ƏSAS üsul İNDİ
--      C hissəsidir (AYSEL-in ÖZ sessiyasının açıq kursorları — bind
--      capture-dan asılı deyil, HƏMİŞƏ işləyir).
-- ============================================================================
-- NECƏ İŞLƏT (yenilənmiş sıra):
--   1. AYSEL AÇIQKƏN (Təsdiq et-dən ƏVVƏL) 0-cı hissəni işlət — öz PL/SQL
--      Developer sessiyanı (USERNAME='ODB', PROGRAM='plsqldev.exe') TANIYIB
--      ÇIXAR, qalan sətirlər arasından AYSEL-in öz sətrini tap (MACHINE/
--      OSUSER/LOGON_TIME-a bax) və SID-ini bir kənara yaz.
--   2. AYSEL-də bir namizəd seç, "Təsdiq et" düyməsinə bas, sonra "Save" et.
--   3. DƏRHAL C hissəsini, 1-ci addımda tapdığın SID ilə, icra et.
--   4. Nəticəni TAM — hər sətrin SQL_TEXT sütununu QISALTMADAN (hücrəyə
--      klikləyib aşağı paneldə tam mətni gör) — mənə göndər.
--   5. C boş qayıdarsa (nadir), A hissəsini (açar sözlərə görə, bütün
--      sessiyalar üzrə) sına; o da boş qayıdarsa, son çarə kimi B-ni sına.
-- ============================================================================

-- ── 0) AYSEL-in sessiya ID-sini (SID) tap ───────────────────────────────────
select sid, serial#, username, module, program, machine, osuser, logon_time, status
from   v$session
where  username is not null
order by logon_time desc;

-- ============================================================================

-- ── A) Son fəaliyyətdən bəri, BÜTÜN sessiyalar üzrə, "reyestr axtarışına
--      bənzəyən" sorğular (açar sözlərə görə süzülüb — PROGRAM sütunu
--      ÇIXARILIB, V$SQL-də yoxdur) ─────────────────────────────────────────
select s.sql_id,
       s.last_active_time,
       s.executions,
       s.parsing_schema_name,
       s.module,
       s.sql_text
from   v$sql s
where  s.last_active_time >= sysdate - (30/1440)   -- son 30 dəqiqə
  and  (
         upper(s.sql_text) like '%V_FIZIKI%'
      or upper(s.sql_text) like '%HUQUQI_SHEXS%'
      or upper(s.sql_text) like '%LICSCH_ALL%'
      or upper(s.sql_text) like '%REGNOM%'
      or upper(s.sql_text) like '%DOCFIO%'
      or upper(s.sql_text) like '%PINCODE%'
      or upper(s.sql_text) like '%GOAML_CLIENTS%'
      )
order by s.last_active_time desc;

-- ============================================================================

-- ── C) ƏN ETİBARLI ÜSUL — AYSEL-in ÖZ sessiyasının (0-cı addımda tapılan
--      SID) açıq kursorlarının tam mətni. Bind capture kimi "bəzən işləmir"
--      riski YOXDUR — bu, HƏMİN SESSİYANIN HƏQİQƏTƏN icra etdiyi sorğulardır
--      (Oracle Forms kursorları aqressiv keşlədiyi üçün adətən çoxlu sətir
--      qayıdacaq — elə ona görə bu üsul daha güvənilən nəticə verir). ───────
select oc.sid, oc.sql_id, s.last_active_time, s.sql_text
from   v$open_cursor oc
join   v$sql s on s.sql_id = oc.sql_id
where  oc.sid = <BURAYA_0-CI_ADDIMDA_TAPDIĞIN_SID_YAZ>
order by s.last_active_time desc;

-- Çox sətir çıxsa, reyestr açar sözlərinə görə də süz (A-dakı WHERE-i bura
-- "and (...)" kimi əlavə et):
-- and (upper(s.sql_text) like '%V_FIZIKI%' or upper(s.sql_text) like '%REGNOM%'
--      or upper(s.sql_text) like '%HUQUQI_SHEXS%' or upper(s.sql_text) like '%LICSCH_ALL%'
--      or upper(s.sql_text) like '%DOCFIO%' or upper(s.sql_text) like '%PINCODE%')

-- ============================================================================

-- ── B) SON ÇARƏ — DƏQIQ bu FİN/VÖEN/hesabın dəyərini bağlı parametr kimi
--      oxuyan sorğu(lar) (çox vaxt 0 sətir qayıdır, yuxarıdakı qeydə bax).
--      Dəyəri YAZDIĞIN KİMİ YOX, HƏM BÖYÜK, HƏM KİÇİK HƏRFLƏ sına — Oracle-da
--      mətn müqayisəsi HƏRFƏ DUYARLIDIR, "lezvkms" ilə "LEZVKMS" FƏRQLİ
--      sayılır. ───────────────────────────────────────────────────────────
select bc.sql_id,
       bc.position,
       bc.value_string,
       s.last_active_time,
       s.sql_text
from   v$sql_bind_capture bc
join   v$sql s on s.sql_id = bc.sql_id and s.child_number = bc.child_number
where  upper(bc.value_string) = upper('<BURAYA_FIN_VEYA_HESAB_YAZ>')
  and  s.last_active_time >= sysdate - (30/1440)
order by s.last_active_time desc;

-- ============================================================================
-- Hamısı boş qayıdarsa, ehtimal AYSEL "Təsdiq et"-i server tərəfdə YOX,
-- formanın öz client-side PL/SQL-ində (fmb-in daxilində) işlədir — bu halda
-- sorğu Oracle-un SQL keşinə HEÇ DÜŞMÜR, v$sql/v$open_cursor bunu görmür.
-- Belə olsa, AYSEL-in forma faylının (.fmb) özünə baxmaq (Forms Builder-də)
-- lazım gələcək — ayrıca araşdırma tələb edir.
-- ============================================================================
