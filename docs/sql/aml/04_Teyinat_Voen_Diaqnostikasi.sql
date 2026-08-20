/* ============================================================================
   AML — «Öz xeyrinə» (AQ/AR) sütunlarını TƏYİNAT MƏTNİNDƏN tutmaq
   ----------------------------------------------------------------------------
   NİYƏ: `doc_vnesh_postupl` / `doc_vnesh_swift` sahələri YALNIZ MƏDAXİL
   sətirlərində dolur. Daxili köçürmə və ödəmə sətirlərində benefisiarın adı
   heç bir struktur sütunda YOXDUR — amma operator onu təyinata yazır:

       VÖEN-AVANS: 1604964601 - Dövlət Gömrük Komitəsi (MOUSAVIAN ... SEYED SAEİD)

   Bu skript **YALNIZ SELECT**-dir (CLAUDE.md — Oracle-a yazı qadağandır).
   Məqsəd: sorğunu dəyişməzdən əvvəl ölçmək — şablon real datada nə qədər tutur.

   ⚠️ ARDICILLIQ: əvvəlcə 1–4-cü sorğuları işlət, nəticəyə bax, sonra
   `91_AML_Xeyrine_Teyinatdan.sql`-i SSMS-də icra et.
   ============================================================================ */


/* ── 1) ÖLÇMƏ — neçə sətirdə «VÖEN + 10 rəqəm» şablonu var? ───────────────
   `arh_dd` böyük cədvəldir — dövrü DAR saxla, yoxsa sorğu uzanar. */
select count(*)                                                       setir_sayi,
       count(regexp_substr(t.primechanie,
             'V[^0-9]{0,3}EN[^0-9]{0,20}([0-9]{10})', 1, 1, null, 1)) voen_tapildi,
       round(100 * count(regexp_substr(t.primechanie,
             'V[^0-9]{0,3}EN[^0-9]{0,20}([0-9]{10})', 1, 1, null, 1))
             / nullif(count(*),0), 1)                                 faiz
  from odb.arh_dd t
 where t.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                       and to_date('31/07/2026','dd/mm/yyyy');


/* ── 2) HANSI PREFİKSLƏR VAR? ─────────────────────────────────────────────
   Ekrandakı nümunə «VÖEN-AVANS:» idi. Başqa yazılışlar da ola bilər
   («VÖEN:», «VOEN -», «V.Ö.E.N»…). Şablonun tutduğu ilk 25 simvolu qruplayır.
   ⚠️ Burada çıxan hər forma yuxarıdakı şablona UYĞUN gəlməlidir; uyğun
   gəlməyən yazılış varsa şablonu genişləndirmək lazımdır. */
select substr(regexp_substr(t.primechanie,
              'V[^0-9]{0,3}EN[^0-9]{0,20}[0-9]{10}', 1, 1, null, 0), 1, 25) sablon,
       count(*) say
  from odb.arh_dd t
 where t.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                       and to_date('31/07/2026','dd/mm/yyyy')
   and regexp_like(t.primechanie, 'V[^0-9]{0,3}EN[^0-9]{0,20}[0-9]{10}')
 group by substr(regexp_substr(t.primechanie,
                 'V[^0-9]{0,3}EN[^0-9]{0,20}[0-9]{10}', 1, 1, null, 0), 1, 25)
 order by say desc;


/* ── 3) NƏ ÇIXARILIR? — sorğuya salınacaq ifadənin EYNİSİ ────────────────
   Sol tərəfdə orijinal təyinat, sağda çıxarılan VÖEN və ad.
   Mühasib/AML əməkdaşı ilə 10-15 sətri gözlə tutuşdurmaq üçün. */
select t.primechanie                                                    teyinat,
       regexp_substr(t.primechanie,
            'V[^0-9]{0,3}EN[^0-9]{0,20}([0-9]{10})', 1, 1, null, 1)     cixarilan_voen,
       trim(regexp_substr(
            regexp_replace(
                regexp_substr(t.primechanie,
                    'V[^0-9]{0,3}EN[^0-9]{0,20}[0-9]{10}(.*)', 1, 1, null, 1),
                '^[ .,:;-]+', ''),
            '^[^(]*'))                                                  cixarilan_ad
  from odb.arh_dd t
 where t.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                       and to_date('31/07/2026','dd/mm/yyyy')
   and regexp_like(t.primechanie, 'V[^0-9]{0,3}EN[^0-9]{0,20}[0-9]{10}')
   and rownum <= 200;


/* ── 4) YALANÇI POZİTİV YOXLAMASI ────────────────────────────────────────
   Çıxarılan VÖEN həqiqətən mövcud bir VÖEN-dirmi? `regnom`-da axtarırıq.
   `tapilmadi` sətirləri şübhəlidir — ya mətndə səhv nömrə var, ya şablon
   yanlış yeri tutub. Sıfıra yaxın olmalıdır.

   QEYD: burada `tapilmadi` çıxması AVTOMATİK səhv demək DEYİL — bankda
   hesabı olmayan qarşı tərəfin VÖEN-i `regnom`-da onsuz da olmur. */
select veziyyet, count(*) say
  from (
    select v.voen,
           case when exists (select 1 from odb.regnom r
                              where to_char(r.inn_regnom) = v.voen)
                then 'var' else 'tapilmadi' end                          veziyyet
      from (select distinct regexp_substr(t.primechanie,
                     'V[^0-9]{0,3}EN[^0-9]{0,20}([0-9]{10})', 1, 1, null, 1) voen
              from odb.arh_dd t
             where t.date_oper between to_date('01/07/2026','dd/mm/yyyy')
                                   and to_date('31/07/2026','dd/mm/yyyy')
               and regexp_like(t.primechanie,
                     'V[^0-9]{0,3}EN[^0-9]{0,20}[0-9]{10}')) v)
 group by veziyyet;
