/* ============================================================================
   AML — «Öz xeyrinə» (AQ/AR): mətn HANSI SÜTUNDADIR? — MƏNBƏ AXTARIŞI
   ----------------------------------------------------------------------------
   TARİXÇƏ (20.08.2026):
   İlk fərziyyə — «benefisiar `arh_dd.primechanie` içində yazılır» — ÖLÇÜLDÜ və
   TƏKZİB EDİLDİ: iyul 2026-da 9094 sətir var, «VÖEN» şablonu tutan **0** sətir.
   Deməli o mətn həmin sütunda saxlanmır. Sorğuya heç nə əlavə edilmədi —
   əvvəlcə mənbəni tapmaq lazımdır.

   BU SKRİPT SUALA CAVAB VERİR: ekrandakı
       «VÖEN-AVANS: 1604964601 - Dövlət Gömrük Komitəsi (MOUSAVIAN ...)»
   mətni hansı cədvəlin hansı sütununda yaşayır?

   ⚠️ AXTARIŞ AÇARI: aşağıda `&voen` və `&ad` yerinə ekrandakı dəyərləri yaz
   (PL/SQL Developer soruşacaq). Nümunə: voen = 1604964601, ad = MOUSAVIAN
   Tarix aralığını da həmin əməliyyatın ayına uyğunlaşdır — iyul olmaya bilər.

   YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */


/* ── 1) `ARH_DD` mətn sütunları — hansılar var? ───────────────────────────
   `primechanie` boş çıxdısa, bəlkə mətn başqa sütundadır. */
select column_name, data_type, data_length, nullable
  from all_tab_columns
 where owner = 'ODB' and table_name = 'ARH_DD'
   and data_type in ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')
 order by column_id;


/* ── 2) `primechanie` ümumiyyətlə doludurmu? ──────────────────────────────
   `dolu` sıfıra yaxındırsa sütun bu bankda işlədilmir — mətni başqa yerdə
   axtarmaq lazımdır. */
select count(*)                                              setir,
       count(t.primechanie)                                  dolu,
       count(case when length(trim(t.primechanie)) > 3
                  then 1 end)                                menali
  from odb.arh_dd t
 where t.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                       and to_date('31/07/2026','dd/mm/yyyy');


/* ── 3) `primechanie` REAL OLARAQ NECƏ GÖRÜNÜR? ──────────────────────────
   20 nümunə. Həm xam, həm `func_utf8_to_latin` ilə — hansının oxunaqlı
   olduğunu gözlə görmək üçün (BMI bəzi yerlərdə çevirir, bəzilərində yox). */
select t.primechanie                                         xam,
       odb.func_utf8_to_latin(t.primechanie)                 cevrilmis
  from odb.arh_dd t
 where t.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                       and to_date('31/07/2026','dd/mm/yyyy')
   and t.primechanie is not null
   and rownum <= 20;


/* ── 4) EKRANDAKI VÖEN-i HƏR YERDƏ AXTAR — `doc_vnesh_nacval` ────────────
   Milli valyutada ödəmə sənədi. Burada onsuz da struktur sahələr var:
   `name_credit` / `inn_credit` (alan tərəf) — sorğu onları ALAN TƏRƏF
   sütunlarına yazır. Ekrandakı VÖEN bunlardan birinə uyğun gəlirsə,
   «öz xeyrinə» ayrıca bir şey DEYİL — sadəcə alan tərəfdir. */
select v.date_oper, v.nomer_docum, v.debet, v.kredit,
       odb.func_utf8_to_latin(v.name_debet)                  odeyen,
       v.inn_debet                                           odeyen_voen,
       odb.func_utf8_to_latin(v.name_credit)                 alan,
       v.inn_credit                                          alan_voen
  from odb.doc_vnesh_nacval v
 where v.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                       and to_date('31/07/2026','dd/mm/yyyy')
   and (to_char(v.inn_credit) = '&voen' or to_char(v.inn_debet) = '&voen');


/* ── 5) `DOC_VNESH_NACVAL` mətn sütunları — təyinat harada? ──────────────
   4-cü sorğu sətri tapdısa, həmin sənədin SƏRBƏST MƏTN sahəsi hansıdır?
   Siyahıya bax: `naznach`, `soderjanie`, `primechanie` kimi ad axtarırıq. */
select column_name, data_type, data_length
  from all_tab_columns
 where owner = 'ODB' and table_name = 'DOC_VNESH_NACVAL'
   and data_type in ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')
 order by column_id;


/* ── 6) MÖTƏRİZƏDƏKİ ADI AXTAR — «öz xeyrinə» əsl namizədi ───────────────
   DİQQƏT — MƏNANIN DƏQİQLƏŞMƏSİ:
   Ekrandakı sətirdə ödəyən «AZƏR-BƏRƏKƏT» MMC, alan isə VÖEN 1604964601
   (Dövlət Gömrük Komitəsi) idi. Yəni VÖEN **alan tərəfdir** və o, sorğuda
   ONSUZ DA var. «Öz xeyrinə» olan isə MÖTƏRİZƏDƏKİ şəxsdir —
   `(MOUSAVIAN KOUHASAREH SEYED SAEİD)` — gömrük rüsumu onun adına ödənilir.

   Bu sorğu həmin adı hansı sütunun daşıdığını tapır. Nəticə boş çıxarsa
   mətn Oracle-da deyil, PROGRESS tərəfindəki başqa saxlamadadır. */
select 'arh_dd.primechanie' menbe, t.date_oper, t.primechanie metn
  from odb.arh_dd t
 where t.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                       and to_date('31/07/2026','dd/mm/yyyy')
   and (upper(t.primechanie) like '%&ad%'
        or upper(odb.func_utf8_to_latin(t.primechanie)) like '%&ad%')
   and rownum <= 50;


/* ── 7) EYNİ AXTARIŞ — cari günün sənədləri (`docdna`) ───────────────────
   `arh_dd` arxivdir; bugünkü sətirlər `docdna`-dadır. Əməliyyat yaxın
   tarixlidirsə axtarışı burada aparmaq lazımdır. */
select 'docdna.primechanie' menbe, t.date_oper, t.primechanie metn
  from odb.docdna t
 where upper(t.primechanie) like '%&ad%'
    or upper(odb.func_utf8_to_latin(t.primechanie)) like '%&ad%'
   and rownum <= 50;


/* ============================================================================
   NƏTİCƏNİ NECƏ OXUMAQ

   · 4-cü sorğu sətir verdi, 6/7 boş  → «öz xeyrinə» ayrıca məlumat DEYİL;
     ekrandakı VÖEN sadəcə ALAN TƏRƏFDİR və sorğuda artıq var (AA/AB sütunları).
     Bu halda AQ/AR sütunlarına əlavə heç nə lazım deyil.

   · 6 və ya 7-ci sorğu sətir verdi → mətn həmin sütundadır; ayrıştırma
     şablonu MÖTƏRİZƏYƏ görə qurulmalıdır (`\(([^)]+)\)`), «VÖEN» sözünə yox.

   · Hamısı boş → mətn Oracle-da saxlanmır (PROGRESS-in öz sahəsidir).
     Bu halda AQ/AR-ı doldurmaq mümkün deyil, sütun boş qalmalıdır.
   ============================================================================ */
