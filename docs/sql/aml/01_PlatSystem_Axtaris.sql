-- ══════════════════════════════════════════════════════════════════════════
-- PLAT_SYSTEM — «Ödəniş sisteminin növü» ADINI hansı cədvəl saxlayır?
--
-- Sorğu G sütununa KOD (məs. 6) qaytarır; şablona AD lazımdır.
-- BMI-nin C# mənbəyində `PLAT_SYSTEM` HEÇ YERDƏ işlənmir — yəni köhnə
-- proqram bu sütunu heç vaxt göstərməyib. Ona görə lüğət cədvəlini
-- bazadan tapmaq lazımdır.
--
-- Aşağıdakı 5 sorğunu SIRA İLƏ işlət. Adətən 1 və ya 2 cavabı verir.
-- Nəticələri göndər — sorğuya join əlavə edərəm.
-- ══════════════════════════════════════════════════════════════════════════


-- ── 1) ƏN SÜRƏTLİ YOL: xarici açar (FK) varmı? ──────────────────────────
-- Varsa, valideyn cədvəlin adını BİRBAŞA yazır — başqa heç nə lazım deyil.
select c.table_name          as uşaq_cedvel,
       cc.column_name        as uşaq_sutun,
       rc.table_name         as VALIDEYN_CEDVEL,
       rcc.column_name       as valideyn_sutun,
       c.constraint_name
  from all_constraints  c
  join all_cons_columns cc  on cc.owner = c.owner
                           and cc.constraint_name = c.constraint_name
  join all_constraints  rc  on rc.owner = c.r_owner
                           and rc.constraint_name = c.r_constraint_name
  join all_cons_columns rcc on rcc.owner = rc.owner
                           and rcc.constraint_name = rc.constraint_name
 where c.owner = 'ODB'
   and c.constraint_type = 'R'
   and upper(cc.column_name) like '%PLAT%'
 order by c.table_name;


-- ── 2) Hansı cədvəllərdə ümumiyyətlə PLAT_SYSTEM sütunu var? ────────────
-- Lüğət cədvəli çox vaxt EYNİ adlı sütunu saxlayır (PLAT_SYSTEM + AD).
select owner, table_name, column_name, data_type, data_length
  from all_tab_columns
 where owner = 'ODB'
   and upper(column_name) like '%PLAT%'
 order by table_name, column_name;


-- ── 3) Adında PLAT / SIST / SYS / ODEM olan cədvəllər ───────────────────
select owner, table_name, num_rows
  from all_tables
 where owner = 'ODB'
   and (upper(table_name) like '%PLAT%'
     or upper(table_name) like '%SIST%'
     or upper(table_name) like '%SYS%'
     or upper(table_name) like '%ODEM%'
     or upper(table_name) like '%SPRAV%'   -- lüğət cədvəlləri belə adlanır
     or upper(table_name) like '%SPR!_%' escape '!')
 order by table_name;


-- ── 4) Real dəyər sahəsi — hansı kodlar ümumiyyətlə işlənir? ────────────
-- Lüğət tapılmasa belə, kodların sayı azdırsa adları əl ilə xəritələmək olar.
select 'NACVAL'  menbe, plat_system, count(*) say from odb.doc_vnesh_nacval  group by plat_system
union all
select 'POSTUPL' menbe, plat_system, count(*)     from odb.doc_vnesh_postupl group by plat_system
union all
select 'INVAL'   menbe, plat_system, count(*)     from odb.doc_vnesh_inval   group by plat_system
union all
select 'SWIFT'   menbe, plat_system, count(*)     from odb.doc_vnesh_swift   group by plat_system
 order by 1, 2;


-- ── 5) Kiçik lüğət cədvəlləri: kod + ad cütü olanlar ───────────────────
-- 4-cü sorğudakı kodlar (məs. 6) hansısa kiçik cədvəlin açarıdırsa,
-- o cədvəl adətən 20 sətirdən azdır və AD/NAME sütunu var.
select t.table_name, t.num_rows,
       listagg(c.column_name, ', ') within group (order by c.column_id) sutunlar
  from all_tables t
  join all_tab_columns c on c.owner = t.owner and c.table_name = t.table_name
 where t.owner = 'ODB'
   and (t.num_rows is null or t.num_rows <= 50)   -- statistika yığılmayıbsa num_rows NULL olur
   and exists (select 1 from all_tab_columns c2
                where c2.owner = t.owner and c2.table_name = t.table_name
                  and (upper(c2.column_name) like '%NAME%'
                    or upper(c2.column_name) like '%AD%'
                    or upper(c2.column_name) like '%NAIM%'))   -- rusca «наименование»
 group by t.table_name, t.num_rows
 order by t.num_rows;
