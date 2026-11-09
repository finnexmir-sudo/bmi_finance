-- ═══════════════════════════════════════════════════════════════════════
--  GOAML — Hesab sahibinin FİZİKİ KYC-sini (Ad/Soyad/Doğum tarixi/Ünvan/
--  Telefon/Sənəd) hesab nömrəsinə görə tapmaq — DİAQNOSTİKA, YALNIZ SELECT
--
--  Niyə lazımdır: "Nümayəndənin" (CON) və "Hesab imza hüququ olan şəxsin"
--  (SIG) blokları DÖVLƏT hesabatına (goAML) gedən REAL şəxsiyyət məlumatıdır.
--  Bu, mühasibat rəqəmi deyil — səhv/uydurma sütun adı ilə yanlış şəxsin
--  adını/FİN-ini yazmaq qanuni nəticəli bir səhv olardı. Ona görə Oracle
--  sxemini TƏXMİN ETMİRİK (CLAUDE.md-nin "Oracle Rəqəmi"/"Mənbələrdə HANSI
--  AÇAR VAR" qaydaları) — əvvəlcə REAL sütunlara baxılır.
--
--  Artıq BİLDİYİMİZ (kod bazasında sübut olunmuş, bu sorğuda TƏKRARLANIR):
--    - hesab → regnom:  substr(hesab, 10, 6) = regnom.regnom
--    - regnom.pincode      = FİN
--    - regnom.inn_regnom   = VÖEN
--    - regnom.name_regnom  = Ad (çox güman "SOYAD AD ATA_ADI" formatında)
--    - regnom.pasport_date_close = vəsiqənin bitmə tarixi (HR modulu bunu
--      onsuz da canlı işlədir — docs/sql/hr/Vesiqe_Bitme_OracleSorgu.sql)
--    - odb.licsch_all_2.name_licsch/inn_licsch = hesab sahibinin adı/VÖEN-i
--      (filial+hesab nömrəsinə görə, GoamlService.LicschAdiVeVoeniAsync-də
--      onsuz da işlənir)
--
--  BİLMƏDİYİMİZ (bu sorğu bunun üçündür): doğum tarixi, doğum yeri,
--  vətəndaşlıq, telefon, ünvan/şəhər, sənəd növü/seriya-nömrəsi/verilmə-
--  bitmə tarixi, verən orqan — bunlar HARADAN gəlir? `regnom`-un ÖZÜNDƏ
--  daha çox sütun varmı, yoxsa ayrıca cədvəl (DOCFIO, V_FIZIKI və s.)
--  lazımdır?
--
--  İCRA ADDIMLARI (PL/SQL Developer-də, hər hissəni AYRI-AYRI işlədin):
-- ═══════════════════════════════════════════════════════════════════════


-- ── A) REGNOM-un ÖZÜNÜN TAM sütun siyahısı — görək orda nə qədər var ──────
select column_name, data_type, data_length
from all_tab_columns
where owner = 'ODB' and table_name = 'REGNOM'
order by column_id;


-- ── B) LICSCH_ALL_2-nin tam sütun siyahısı (name_licsch/inn_licsch-dən
--      başqa daha nə var — telefon/ünvan ola bilərmi?) ────────────────────
select column_name, data_type, data_length
from all_tab_columns
where owner = 'ODB' and table_name = 'LICSCH_ALL_2'
order by column_id;


-- ── C) DOCFIO-nun tam sütun siyahısı (CLAUDE.md-də "ssn"/"pasport"/
--      "passport_id" adları keçir, amma tam sxem görülməyib) ─────────────
select column_name, data_type, data_length
from all_tab_columns
where owner = 'ODB' and table_name = 'DOCFIO'
order by column_id;


-- ── D) Adında "FIZIK"/"KYC"/"CLIENT"/"MUSTERI"/"ANKETA" keçən BÜTÜN
--      cədvəl/view-ları tap — tam şəxsi profil (doğum tarixi, ünvan,
--      telefon, sənəd) ayrıca bir yerdə saxlanıla bilər ──────────────────
select object_name, object_type
from all_objects
where owner = 'ODB'
  and (object_name like '%FIZIK%' or object_name like '%KYC%'
    or object_name like '%CLIENT%' or object_name like '%MUSTERI%'
    or object_name like '%ANKET%')
  and object_type in ('TABLE', 'VIEW')
order by object_name;


-- ── E) REAL NÜMUNƏ — test əməliyyatının ÖZ hesabları üçün nə qədər
--      doldurulub görək. ⚠️ Aşağıdakı iki hesab nömrəsini (DEBET/KREDIT)
--      öz test qeydinizin dəqiq nömrələri ilə ƏVƏZ EDİN (GOAML ekranında,
--      yaxud axtarış siyahısında göründüyü kimi). ──────────────────────
select r.pincode, r.inn_regnom, r.name_regnom, r.pasport_date_close
from odb.regnom r
where r.regnom = substr('2501900000000300007', 10, 6)   -- DEBET
   or r.regnom = substr('40060000001984300000', 10, 6);  -- KREDIT

select l.name_licsch, l.inn_licsch
from odb.licsch_all_2 l
where l.licsch = '2501900000000300007'
   or l.licsch = '40060000001984300000';


-- ── F) C-dən çıxan TABLE/VIEW adlarından İSTƏNİLƏN birini tapsanız, onun
--      BÜTÜN sütunlarını (A/B/C kimi) VƏ eyni iki hesab üçün real sətrini
--      AYRICA göndərin — bu fayl bir dəfə işlənib NƏTİCƏ Claude-a göndərilir,
--      kod YALNIZ bundan sonra yazılır.
