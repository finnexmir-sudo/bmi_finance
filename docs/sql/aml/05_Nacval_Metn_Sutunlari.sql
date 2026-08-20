/* ============================================================================
   AML — «Öz xeyrinə» (AQ/AR): mətn SƏNƏD cədvəlindədir, `arh_dd`-də YOX
   ----------------------------------------------------------------------------
   04-cü skriptin nəticəsi (20.08.2026):

   · `arh_dd.primechanie` — 9093/9094 DOLU, amma məzmunu DAXİLİ MÜHASİBAT
     QEYDİDİR: «Emanat id:47285 fn:0 kre:(0)…», «Mübadilə kassa təminatı»,
     «IPS əməliyyatları üzrə limitin təmin edilməsi».
     Müştərinin ÖDƏNİŞ TƏYİNATI orada saxlanmır → «VÖEN» axtarışı 0 verdi.

   · `doc_vnesh_nacval`-da isə BEŞ mətn sahəsi var və heç biri sorğuda
     oxunmur: PRIMECHANIE(560), REMITTENS_INF(560), COMMENTS(840),
     D3_DESTINATION(512), D4_BUDGET_LEVEL(512).

   · 1604964601 → `inn_debet`, yəni AZAR-BARAKAT MMC **ÖDƏYƏNDİR**
     (alanlar: DSMF, İcbari Tibbi Sığorta, Məşğulluq Agentliyi).
     `D4_BUDGET_LEVEL` sahəsinin mövcudluğu bunun BÜDCƏ ödənişi olduğunu
     göstərir — gömrük/rüsum ödənişləri məhz belədir.

   BU SKRİPT SUALA CAVAB VERİR: «VÖEN-AVANS: … - … (AD)» mətni bu beş
   sahədən HANSINDADIR?

   ⚠️ `&voen` = ekrandakı VÖEN (nümunə: 1604964601).
   YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */


/* ── 1) BEŞ MƏTN SAHƏSİ YAN-YANA — eyni 11 sətir üçün ────────────────────
   Hansı sütunda «VÖEN-…» mətninin olduğunu GÖZLƏ görmək üçün.
   Boş sütunlar dərhal seçiləcək. */
select v.date_oper, v.nomer_docum,
       odb.func_utf8_to_latin(v.name_debet)                  odeyen,
       odb.func_utf8_to_latin(v.name_credit)                 alan,
       odb.func_utf8_to_latin(v.primechanie)                 c1_primechanie,
       odb.func_utf8_to_latin(v.remittens_inf)               c2_remittens,
       odb.func_utf8_to_latin(v.comments)                    c3_comments,
       odb.func_utf8_to_latin(v.d3_destination)              c4_d3_destination,
       odb.func_utf8_to_latin(v.d4_budget_level)             c5_d4_budget
  from odb.doc_vnesh_nacval v
 where v.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                       and to_date('31/07/2026','dd/mm/yyyy')
   and (to_char(v.inn_credit) = '&voen' or to_char(v.inn_debet) = '&voen');


/* ── 2) HANSI SAHƏ NƏ QƏDƏR DOLUDUR? — bütün iyul üzrə ───────────────────
   Yuxarıdakı 11 sətir təsadüfi ola bilər. Bu sorğu hər sahənin ümumi
   doluluğunu ölçür — hansına güvənmək olar. */
select count(*)                          setir,
       count(v.primechanie)              d_primechanie,
       count(v.remittens_inf)            d_remittens,
       count(v.comments)                 d_comments,
       count(v.d3_destination)           d_d3,
       count(v.d4_budget_level)          d_d4
  from odb.doc_vnesh_nacval v
 where v.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                       and to_date('31/07/2026','dd/mm/yyyy');


/* ── 3) «VÖEN» ŞABLONU — BEŞ SAHƏNİN HƏR BİRİNDƏ AYRICA ──────────────────
   Hansı sahədə neçə dəfə rast gəlinir. Ən böyük rəqəm = əsl mənbə.
   `func_utf8_to_latin` ilə çevrilir, çünki `Ö` xam halda iki bayt ola bilər. */
select 'primechanie'   sahe, count(*) say from odb.doc_vnesh_nacval v
 where v.date_oper between to_date('01/07/2026','dd/mm/yyyy') and to_date('31/07/2026','dd/mm/yyyy')
   and regexp_like(odb.func_utf8_to_latin(v.primechanie),   'V[^0-9]{0,3}EN')
union all
select 'remittens_inf', count(*) from odb.doc_vnesh_nacval v
 where v.date_oper between to_date('01/07/2026','dd/mm/yyyy') and to_date('31/07/2026','dd/mm/yyyy')
   and regexp_like(odb.func_utf8_to_latin(v.remittens_inf), 'V[^0-9]{0,3}EN')
union all
select 'comments', count(*) from odb.doc_vnesh_nacval v
 where v.date_oper between to_date('01/07/2026','dd/mm/yyyy') and to_date('31/07/2026','dd/mm/yyyy')
   and regexp_like(odb.func_utf8_to_latin(v.comments),      'V[^0-9]{0,3}EN')
union all
select 'd3_destination', count(*) from odb.doc_vnesh_nacval v
 where v.date_oper between to_date('01/07/2026','dd/mm/yyyy') and to_date('31/07/2026','dd/mm/yyyy')
   and regexp_like(odb.func_utf8_to_latin(v.d3_destination),'V[^0-9]{0,3}EN')
union all
select 'd4_budget_level', count(*) from odb.doc_vnesh_nacval v
 where v.date_oper between to_date('01/07/2026','dd/mm/yyyy') and to_date('31/07/2026','dd/mm/yyyy')
   and regexp_like(odb.func_utf8_to_latin(v.d4_budget_level),'V[^0-9]{0,3}EN');


/* ── 4) MƏDAXİL SƏNƏDİNDƏ DƏ EYNİ SAHƏLƏR VARMI? ─────────────────────────
   `doc_vnesh_postupl` — milli valyutada mədaxil. Ödəmə ilə mədaxilin sahə
   dəsti fərqlidirsə, ayrıştırma hər qolda AYRICA yazılmalıdır. */
select column_name, data_type, data_length
  from all_tab_columns
 where owner = 'ODB' and table_name = 'DOC_VNESH_POSTUPL'
   and data_type in ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')
 order by column_id;


/* ============================================================================
   NƏTİCƏNİ NECƏ OXUMAQ

   · 3-cü sorğuda BİR sahə açıq-aşkar qabaqdadırsa → mənbə odur;
     1-ci sorğunun həmin sütununa baxıb şablonu ona görə qururuq.

   · Bütün saylar 0-dırsa → «VÖEN-…» mətni Oracle-da yoxdur; ekranda
     görünən sətir PROGRESS-in öz formatlaşdırması ola bilər.

   · 4-cü sorğu ödəmə cədvəlindən FƏRQLİ sahə adları verirsə → sorğunun
     hər qolunda ayrı sütun adı işlədilməlidir (indi hər iki qolda
     `cast(null …)` yazılıb).

   ⚠️ Bu beş sahə hazırda sorğuda ÜMUMİYYƏTLƏ oxunmur. AP («Təyinat»)
   sütunu `arh_dd.primechanie`-dən gəlir — yəni BMI-nin özündə də belədir
   (`frmhesabsorgu.cs:544` — `t.primechanie emel`), qəsdən dəyişilməyib.
   Əsl ödəniş təyinatını AP-yə salmaq AYRI bir qərardır.
   ============================================================================ */
