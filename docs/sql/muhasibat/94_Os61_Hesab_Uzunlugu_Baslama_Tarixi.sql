/* ============================================================================
   ÖS-6.1 — HESAB NÖMRƏSİNİN UZUNLUĞU (13 → 20) VƏ SÜRƏT ÜÇÜN BAŞLAMA TARİXİ
   (Oracle, YALNIZ-OXU — SELECT). 18.11.2026.

   Şikayət: "men sql i bazada isledirem ,cox sayir,orda kohneden olan
   hesablarida getirir,evvel hesablar 13 herf idi indi 20 dir,ve basqa
   seyler. biz baslama tarixini duz tapsaq daha tez bas verer sorgu."

   Səbəb ehtimalı: `Muhasibat_OracleSorgular_OS6.sql`-in əsas sorğusu
   (`from odb.licsch l where l.date_open_licsch <= {D2}`) TARİXCƏ BÜTÜN
   `licsch` cədvəlini (ən köhnə qeydlərdən bu günə qədər) oxuyur. Bütün
   SUBSTR formulları (klass/qrup/bölmə/valyuta/regnom_kod) 20-simvollu
   hesab formatına görə yazılıb (CLAUDE.md-dəki MID()↔SUBSTR() xəritəsi)
   — köhnə 13-simvollu hesablarda bu SUBSTR mövqeləri YA BOŞ, YA YANLIŞ
   dəyər qaytarır (məs. SUBSTR(hesab,10,6) 13-simvollu sətirdə yalnız
   4 simvol tuta bilər) → "başqa şeylər" (qarışıq/səhv klass-qrup) elə
   buradan gəlir.

   Bu skript İKİ sualı cavablandırır:
   (A) 13→20 keçidi NƏ VAXT olub — "başlama tarixi" üçün namizəd.
   (B) Bu tarixdən ƏVVƏL/SONRA uzunluq QARIŞIQ qalırmı (yəni YALNIZ tarixlə
       filtr etmək TƏHLÜKƏSİZDİRMİ, yoxsa LENGTH(licsch)=20 şərti DƏ
       məcburi qalır).

   PL/SQL Developer-də, FOXPRO@bmi bağlantısı ilə, HƏR bloku AYRI-AYRI
   işlət, nəticəni bura/mənə göndər — mən bundan sonra əsl OS6.sql
   sorğusuna (VƏ 90_Os61_Dogrulama_Diaqnostika.sql-ə) filtri əlavə edəcəm.
   ========================================================================== */


/* ───────────────── A) Uzunluq census — sayı + tarix aralığı ───────────────
   Gözlənilən: iki qrup (13 və 20), arada bəlkə az sayda keçid/qeyri-standart
   uzunluq. Hər sətirdə min/max date_open_licsch — "nə vaxt başlayıb, nə vaxt
   qurtarıb" sualına birbaşa cavab. */

select length(l.licsch)            uzunluq,
       count(*)                    sayi,
       min(l.date_open_licsch)     en_erken_acilma,
       max(l.date_open_licsch)     en_son_acilma,
       sum(case when l.date_close_licsch is null then 1 else 0 end) baglanmamisdir
from odb.licsch l
group by length(l.licsch)
order by 1;


/* ───────────────── B) Keçid sərhədi — qarışıq var mı? ──────────────────────
   "en_son_acilma" (13-simvollu üçün, A-dan) ilə "en_erken_acilma" (20-simvollu
   üçün, A-dan) arasında ÜST-ÜSTƏ DÜŞMƏ varsa, YALNIZ tarixlə filtr etmək
   RİSKLİDİR — aşağıdakı iki sorğu bunu birbaşa göstərir (hər ikisi 0 sətir
   qaytarmalıdır ki, tarix filtri TƏK BAŞINA TƏHLÜKƏSİZ olsun). */

-- B1: 20-simvollu namizəd başlama tarixindən ƏVVƏL açılan 20-simvollu hesab varmı?
--     (varsa, bu hesablar tarix-filtri ilə SƏHVƏN atılacaq)
select count(*) erken_20_simvollu
from odb.licsch l
where length(l.licsch) = 20
  and l.date_open_licsch < (select min(l2.date_open_licsch) from odb.licsch l2 where length(l2.licsch) = 20);

-- B2: 20-simvollu namizəd başlama tarixindən SONRA açılan 13-simvollu hesab varmı?
--     (varsa, bu köhnə-formatlı hesablar tarix-filtri ilə SƏHVƏN DAXİL olacaq —
--     LENGTH(licsch)=20 şərti DƏ saxlanmalıdır, tarix filtri YALNIZ sürət üçündür)
select count(*) gec_13_simvollu
from odb.licsch l
where length(l.licsch) = 13
  and l.date_open_licsch >= (select min(l2.date_open_licsch) from odb.licsch l2 where length(l2.licsch) = 20);


/* ───────────────── C) İndeks yoxlaması — tarix filtri HƏQİQƏTƏN sürətlənərmi? ─
   date_open_licsch-ə indeks yoxdursa, filtr əlavə etmək Oracle-u FULL TABLE
   SCAN-dan xilas ETMƏZ (yenə hər sətiri oxuyub şərtə baxacaq) — yalnız
   NƏTİCƏ sətirlərini azaldar, SCAN-ın ÖZÜNÜ YOX. */

select ic.index_name, ic.column_name, ic.column_position
from all_ind_columns ic
join all_indexes ix on ix.index_name = ic.index_name and ix.owner = ic.index_owner
where ic.table_name = 'LICSCH'
  and ic.table_owner = 'ODB'
order by ic.index_name, ic.column_position;


/* ───────────────── D) Namizəd filtrin REAL effekti — sətir sayı müqayisəsi ──
   Mövcud sorğunun indi oxuduğu sətir sayı (filtrsiz, bütün tarix) ilə
   namizəd tarix+uzunluq filtri ilə qalan sətir sayını tutuşdur — fərq nə
   qədər böyükdürsə, sürət artımı bir o qədər böyük olacaq. */

select
  (select count(*) from odb.licsch l where l.date_open_licsch is not null) hazirki_setir_sayi,
  (select count(*) from odb.licsch l
    where l.date_open_licsch is not null
      and length(l.licsch) = 20
      and l.date_open_licsch >= (select min(l2.date_open_licsch) from odb.licsch l2 where length(l2.licsch) = 20)
  ) namized_filtrli_setir_sayi
from dual;
