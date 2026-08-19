-- ══════════════════════════════════════════════════════════════════════════
-- PLAT_SYSTEM — namizəd lüğət cədvəllərinin içi
--
-- 1-ci addımın nəticəsi:
--   · FK YOXDUR (tapılan 2 sətir USER_SEND_PLAT / USER_VER_PLAT-dır — əlaqəsiz)
--   · `ODB.MFO`-da PLAT_SYSTEM sütunu VAR → kod BANKA bağlıdır
--   · Kiçik namizədlər: AML_SETUP_BANK_PLATFORMS (4 sətir),
--                       XONKS_PLAT_KOD (11 sətir)
--
-- Aşağıdakı 4 sorğunu işlət və nəticəni göndər. Adı saxlayan cədvəl
-- bilinən kimi əsas sorğuya bir skalyar alt sorğu əlavə edirəm.
-- ══════════════════════════════════════════════════════════════════════════


-- ── 1) Namizəd №1 — cəmi 4 sətir ────────────────────────────────────────
select * from odb.aml_setup_bank_platforms;


-- ── 2) Namizəd №2 — cəmi 11 sətir ───────────────────────────────────────
select * from odb.xonks_plat_kod;


-- ── 3) MFO-da hansı kodlar var və neçə bank? ────────────────────────────
-- Sənəd cədvəlindəki kod (məs. 6) burada da varsa, deməli mənbə MFO-dur
-- və ad ehtimalla yuxarıdakı iki lüğətdən birindədir.
select m.plat_system, count(*) bank_sayi,
       min(m.bank_large_name) nümunə_bank
  from odb.mfo m
 group by m.plat_system
 order by 1;


-- ── 4) Sənəd cədvəllərində real dəyər sahəsi ────────────────────────────
select 'NACVAL'  menbe, plat_system, count(*) say from odb.doc_vnesh_nacval  group by plat_system
union all
select 'POSTUPL' menbe, plat_system, count(*)     from odb.doc_vnesh_postupl group by plat_system
union all
select 'INVAL'   menbe, plat_system, count(*)     from odb.doc_vnesh_inval   group by plat_system
union all
select 'SWIFT'   menbe, plat_system, count(*)     from odb.doc_vnesh_swift   group by plat_system
 order by 1, 2;
