-- ============================================================================
-- GOAML — "UPDATE NEW" kaskadının TAM (kəsilməmiş) mətni + istinad cədvəlləri
-- (93-cü faylın NÖVBƏTİ addımı — oradakı 208 sətirlik ÖNİZLƏMƏDƏ ~49 fərqli
--  sql_id tapıldı, amma HƏR BİRİ 300 simvolda KƏSİLİB. Bu TƏHLÜKƏLİDİR: iki
--  fərqli sətir eyni TIP=8 yazır, amma biri "alan HÜQUQİ şəxsdir" (TO_YURIK=1),
--  o biri "alan FİZİKİ şəxsdir" (TO_PRED=1 OR TO_FIZIK=1) şərti daşıyır — bu,
--  ya kəsilmiş hissədə gizli əlavə şərt var deməkdir, ya da hər ikisi düzdür
--  AMMA fərqli DEBET/KREDIT kombinasiyasına aiddir. TAM mətn olmadan bunu
--  AYIRD ETMƏK mümkün deyil.)
-- ============================================================================
-- NECƏ İŞLƏT:
--   1. AYSEL-i aç, 93-cü faylın 0-cı addımı ilə öz SID-ini tap.
--   2. 1-ci sorğunu (bu fayl) işlət — bu, 93-cü faylın tapdığı BÜTÜN sql_id-lərin
--      (open_cursor-dan, SƏNİN SID-inlə) TAM, kəsilməmiş mətnini verir.
--      Nəticəni EXPORT ET (PL/SQL Developer-də grid üzərində sağ klik →
--      "Export Data" → .txt/.csv, "Max column width" seçimini BÖYÜK (məs. 4000)
--      qoy ki, uzun UPDATE mətnləri kəsilməsin) və mənə göndər.
--   3. 2-ci sorğu ilə həmin açıq kursorların SAYINI yoxla — 93-cü faylda 208
--      sətirdə ~49 fərqli sql_id var idi; bu say 49-dan ÇOXDURSA, deməli
--      daha çox sətir var (əvvəlki "150+" rəqəmi ilə uyğunlaşa bilər) — bu
--      halda bütün sql_id-lərin SİYAHISINI (2-ci sorğunun NƏTİCƏSİNİ) də göndər.
--   4. 3-cü blokdakı istinad cədvəllərinin (BALSCHT_FIZ/BALSCHT_UR/
--      MMX_ID_SERIAL/TYPES_OF_DOCUMENTS/GOAML_NAME_FIL) HAMISINI işlədib
--      nəticələri göndər — bunlar KİÇİK cədvəllərdir (adətən 10-50 sətir),
--      TAM SELECT * kifayətdir.
--   5. BIC_CODES böyük ola bilər (bank SWIFT kodları siyahısı) — 4-cü sorğu
--      ilə yalnız STRUKTURUNU (sütun adları) VƏ sətir sayını göstər, TAM
--      datanı göndərmə (lazım olsa ayrıca, konkret BIC kodu üçün sorğulayarıq).
-- ============================================================================

-- ── 1) BÜTÜN görülən sql_id-lərin TAM mətni (AYSEL-in SID-i ilə) ───────────
-- DÜZƏLİŞ #1: `GROUP BY`-da CLOB (sql_fulltext) sütunu YOXDUR — Oracle
-- ORA-00932 ("inconsistent datatypes") verir. Dublikatları (open_cursor-un
-- özü yaradır) `sql_id`-ni ƏVVƏLCƏ alt-sorğuda süzüb, sonra v$sql-dən
-- BİR DƏFƏ oxumaqla aradan qaldırırıq — GROUP BY heç lazım deyil.
-- DÜZƏLİŞ #2: PL/SQL Developer-in "Export Results" funksiyası CLOB sütununu
-- GRİDDƏ GÖRÜNƏN "<CLOB>" yerişarəsi kimi ixrac edir, ƏSL MƏTNİ YOX. Bunu
-- `DBMS_LOB.SUBSTR(..., 4000, 1)` ilə ADİ VARCHAR2-yə çeviririk — nəticə indi
-- GÖRÜNƏN, İXRAC OLUNA BİLƏN mətndir (4000 bayta qədər — bizim UPDATE
-- sətirlərinin böyük əksəriyyəti bundan qısadır; daha uzun çıxsa, həmin
-- KONKRET sql_id üçün ayrıca, SUBSTR-in 2-ci parametrini dəyişib (məs.
-- `DBMS_LOB.SUBSTR(s.sql_fulltext, 4000, 4001)`) QALAN hissəni ayrıca al).
select s.sql_id,
       s.executions,
       dbms_lob.substr(s.sql_fulltext, 4000, 1) as sql_text_tam
from   v$sql s
where  s.sql_id in (
         select distinct oc.sql_id
         from   v$open_cursor oc
         where  oc.sid = <BURAYA_SID_YAZ>
       )
  and  upper(s.sql_text) like 'UPDATE%GOAML_CLIENTS%'
order by s.sql_id;

-- ============================================================================

-- ── 2) Neçə FƏRQLİ sql_id var? (93-cü faylda tapılan 49 ilə tutuşdurmaq üçün)
select count(distinct s.sql_id) as ferqli_sql_sayi
from   v$open_cursor oc
join   v$sql s on s.sql_id = oc.sql_id
where  oc.sid = <BURAYA_SID_YAZ>
  and  upper(s.sql_text) like 'UPDATE%GOAML_CLIENTS%';

-- ============================================================================

-- ── 3) Kiçik istinad cədvəlləri — TAM SELECT * (bunlar olmadan TIP/ID-serial
--      qaydalarının heç birini kodda tətbiq edə bilmərik) ──────────────────
select * from odb.balscht_fiz order by balscht;
select * from odb.balscht_ur  order by balscht;
select * from odb.mmx_id_serial order by id_serial;
select * from odb.types_of_documents order by isare;
select * from odb.goaml_name_fil order by filialnumber;

-- ============================================================================

-- ── 4) BIC_CODES — YALNIZ struktur+say, TAM data YOX (çox böyük ola bilər) ──
select column_name, data_type, data_length
from   all_tab_columns
where  owner = 'ODB' and table_name = 'BIC_CODES'
order by column_id;

select count(*) as bic_codes_setir_sayi from odb.bic_codes;

-- ============================================================================
-- ── 5) ATTRIB cədvəlinin (SWIFT branch yoxlaması, sql_id=6q2w8y0a31pr3-də
--      görünür) hansı sxemdə olduğunu tap — "ODB." prefiksi yox idi, ehtimal
--      cari istifadəçinin öz sxemasındadır ──────────────────────────────────
select owner, table_name
from   all_tables
where  table_name = 'ATTRIB';
