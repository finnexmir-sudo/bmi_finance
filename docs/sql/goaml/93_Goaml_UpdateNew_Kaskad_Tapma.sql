-- ============================================================================
-- GOAML — "UPDATE NEW" kaskadının DƏQIQ SQL mətnini tapmaq
-- (06.11.2026-dakı araşdırma kaskadın NƏ ETDİYİNİ ÜMUMİ izah etmişdi —
--  TIP-i hesab prefiksindən təyin edir, REPORT_CODE-u qara siyahı/PEP/
--  sanksiya cədvəllərinə baxıb qoyur, KYC-ni V_FIZIKI/HUQUQI_SHEXS_ALL/
--  LICSCH_ALL_2-dən köçürür — BU FAYL isə həmin 150+ UPDATE-in DƏQIQ
--  mətnini tapır ki, C#/SQL Server-ə SÖZBƏSÖZ köçürülə bilsin).
--
-- DİQQƏT: "Təsdiq et"+Save ilə "UPDATE NEW" İKİ AYRI addımdır (əvvəlki
-- araşdırmada təsdiqləndi — Save özü bu kaskadı İŞƏ SALMIR). Bu skript
-- YALNIZ "UPDATE NEW" düyməsinin ÖZÜNÜ hədəf alır.
-- ============================================================================
-- NECƏ İŞLƏT:
--   1. AYSEL-də bir namizədi "Təsdiq et" + Save et (artıq GOAML_CLIENTS-ə
--      yazılıb olmalıdır).
--   2. Aşağıdakı 0-cı sorğu ilə AYSEL-in öz SID-ini tap (USERNAME/MACHINE/
--      OSUSER/LOGON_TIME-a görə tanı — öz PL/SQL Developer sessiyanla
--      QARIŞDIRMA).
--   3. AYSEL-də "UPDATE NEW" düyməsinə bas, bitməsini gözlə.
--   4. DƏRHAL (keş aradan getməmiş) 1-ci sorğunu, 2-ci addımda tapdığın SID
--      ilə işlət. Nəticə bir neçə onlarla/yüzlərlə sətir ola bilər — HAMISINI
--      (ilk 300 simvollq önizləmə kifayətdir) mənə göndər.
--   5. Mən önizləmədən fərqli NÖVLƏRİ (TIP-təyini, REPORT_CODE, V_FIZIKI-dən
--      KYC, HUQUQI_SHEXS-dən KYC, LICSCH-dən KYC və s.) ayırd edib, hər
--      NÖVDƏN 1-2 nümunənin TAM mətnini istəyəcəyəm — onda 2-ci sorğunu
--      (sql_id-ni dəyişərək) işlədərsən.
--   6. 3-cü sorğu ilə reyestr cədvəllərinin (V_FIZIKI/HUQUQI_SHEXS_ALL/
--      LICSCH_ALL_2) öz sütun adlarını da göndər — mapping yazmaq üçün
--      bunlar MÜTLƏQ lazımdır.
-- ============================================================================

-- ── 0) AYSEL-in sessiya ID-sini (SID) tap ───────────────────────────────────
select sid, serial#, username, module, program, machine, osuser, logon_time, status
from   v$session
where  username is not null
order by logon_time desc;

-- ============================================================================

-- ── 1) ÖNİZLƏMƏ — "UPDATE NEW"-dən DƏRHAL SONRA, AYSEL-in SID-inin açıq
--      kursorlarından GOAML_CLIENTS-ə yazan BÜTÜN UPDATE-lərin qısa mətni ───
select oc.sid,
       s.sql_id,
       s.executions,
       substr(s.sql_text, 1, 300) as onizleme
from   v$open_cursor oc
join   v$sql s on s.sql_id = oc.sql_id
where  oc.sid = <BURAYA_0-CI_ADDIMDA_TAPDIĞIN_SID_YAZ>
  and  upper(s.sql_text) like 'UPDATE%GOAML_CLIENTS%'
order by s.sql_id;

-- Əgər 0 sətir qayıdırsa — "UPDATE NEW" bu sessiyada görünmür, ehtimal AYSEL-in
-- ARXA fon prosesi (planlaşdırılmış job, ayrı DB bağlantısı) kimi işləyir.
-- Bu halda bütün sessiyalar üzrə (SID şərtsiz) eyni sorğunu sına:
--
-- select s.sql_id, s.last_active_time, s.parsing_schema_name,
--        substr(s.sql_text, 1, 300) as onizleme
-- from   v$sql s
-- where  s.last_active_time >= sysdate - (10/1440)
--   and  upper(s.sql_text) like 'UPDATE%GOAML_CLIENTS%'
-- order by s.last_active_time desc;

-- ============================================================================

-- ── 2) TAM MƏTN — önizləmədən seçilən KONKRET sql_id üçün kəsilməmiş mətn
--      (PL/SQL Developer-də SQL_FULLTEXT xanasına iki dəfə klikləyib tam
--      mətni gör, ya da "Export Grid"lə fayla çıxar) ──────────────────────
select sql_id, sql_fulltext
from   v$sql
where  sql_id = '<ONİZLƏMƏDƏN_SEÇDİYİN_SQL_ID>';

-- ============================================================================

-- ── 3) Reyestr cədvəllərinin ÖZ sütun adları — bunlar olmadan "ad/soyad/
--      ünvan/sənəd" sahələrinin HANSI sütundan gəldiyini bilə bilmərik ────
select table_name, column_name, data_type, data_length
from   all_tab_columns
where  owner = 'ODB'
  and  table_name in ('V_FIZIKI', 'HUQUQI_SHEXS_ALL', 'LICSCH_ALL_2')
order by table_name, column_id;
