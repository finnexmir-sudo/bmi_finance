/* ============================================================================
   ÖS-6.1 — "Kredit hesabları" (Kod 3), dövrün sonuna: 232 (tətbiq) vs 293
   (mühasibin "OS old 2026-10.xls"-i, 30-10-2026 tarixli snapshot).

   ⚠️ BU, OracleSorgular-a YAZILMIR — birbaşa PL/SQL Developer-də, FOXPRO
   hesabı ilə, BÖLMƏ-BÖLMƏ işlədilən, YALNIZ-OXU bir DİAQNOSTİKA skriptidir.
   Oracle-a HEÇ NƏ yazılmır.

   Məqsəd: 232 (bizim tətbiqin 31-günlük arxiv-pəncərəsi ilə hesabladığı
   "aktiv kredit hesabı" sayı) ilə 293 (mühasibin Excel-dəki sayı) arasındakı
   FƏRQİ YARADAN KONKRET hesabları (licsch nömrəsi, adı, regnom) tapmaq.

   Fərziyyə: fərq (293-232≈61) 31-günlük pəncərədən kənarda qalan, AMMA
   HƏQİQƏTƏN açıq olan kreditlərdir — yəni arxivdə (`odb.arh_licschkre`)
   TAM tarixçə üzrə TAPILIR (və son bağlanma tarixi boş/gələcəkdir), AMMA
   son 31 gündə HEÇ bir snapshot yazılmayıb (arxiv o hesab üçün bu dövrdə
   "sakit" qalıb — bəlkə arxiv yalnız HƏRƏKƏTİ OLAN hesablara yazır, "hər
   açıq hesaba hər iş günü" fərziyyəsi bu kredit tipləri üçün doğru deyil).

   Dəyişdirmə: {D2}-ni öz tarixinizlə (mühasibin istifadə etdiyi tarixlə,
   BU HALDA 30-10-2026) əvəz edin — aşağıdakı hər bloku AYRI-AYRI işlədin.
   ========================================================================== */

-- ===========================================================================
-- A) RAW CANDIDATE — "kredit hesabı" (Klass 20/21), inzibati olaraq D2-yə görə
--    hələ açıq (licsch.date_close_licsch) sayı. Bu, Kod 3-ün "HESAB SAYI"
--    (G) sütununun TƏK BAŞINA, arxivsiz versiyasıdır — gözlənilən ~643.
-- ===========================================================================
select count(*) raw_aday_sayi
  from odb.licsch l
 where substr(l.licsch,1,2) in ('20','21')
   and substr(l.licsch,5,1) in ('0','5')
   and substr(l.licsch,3,1) <> '9'
   and l.date_open_licsch is not null
   and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
   and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'));

-- ===========================================================================
-- B) ARXİVDƏ TAM TARİXÇƏ ÜZRƏ (pəncərəsiz) aktiv sayı — gözlənilən ~293
--    (mühasibin Excel-inə ən yaxın olmalıdır).
-- ===========================================================================
with
raw_aday as (
    select l.licsch, l.name_licsch adi, substr(l.licsch,10,6) regnom_kod,
           l.inn_licsch voen, l.saldo_ish_nacval qaliq_nacval, l.saldo_ish_inval qaliq_inval
      from odb.licsch l
     where substr(l.licsch,1,2) in ('20','21')
       and substr(l.licsch,5,1) in ('0','5')
       and substr(l.licsch,3,1) <> '9'
       and l.date_open_licsch is not null
       and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
       and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'))
),
arh_norm_tam as (
    select licschkre acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licschpkre acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licsch_19 acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licschppkre acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper <= to_date('30-10-2026','dd-mm-yyyy')
),
arh_agg_tam as (
    select acc,
           max(date_oper) keep (dense_rank first order by date_oper desc, nvl(subschkre,0) asc) son_date_oper,
           max(date_close) keep (dense_rank first order by date_oper desc, nvl(subschkre,0) asc) date_close
      from arh_norm_tam where acc is not null group by acc
)
select count(*) tam_tarixce_aktiv_sayi
  from raw_aday r
  join arh_agg_tam a on a.acc = r.licsch
 where a.date_close is null or a.date_close > to_date('30-10-2026','dd-mm-yyyy');

-- ===========================================================================
-- C) ARXİVDƏ YALNIZ SON 31 GÜN PƏNCƏRƏSİNDƏ (bizim CARİ tətbiq məntiqi) —
--    gözlənilən ~232 (tətbiqin göstərdiyi rəqəmlə EYNİ çıxmalıdır).
-- ===========================================================================
with
raw_aday as (
    select l.licsch
      from odb.licsch l
     where substr(l.licsch,1,2) in ('20','21')
       and substr(l.licsch,5,1) in ('0','5')
       and substr(l.licsch,3,1) <> '9'
       and l.date_open_licsch is not null
       and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
       and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'))
),
arh_norm_31 as (
    select licschkre acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper > to_date('30-10-2026','dd-mm-yyyy') - 31
       and date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licschpkre acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper > to_date('30-10-2026','dd-mm-yyyy') - 31
       and date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licsch_19 acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper > to_date('30-10-2026','dd-mm-yyyy') - 31
       and date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licschppkre acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper > to_date('30-10-2026','dd-mm-yyyy') - 31
       and date_oper <= to_date('30-10-2026','dd-mm-yyyy')
),
arh_agg_31 as (
    select acc,
           max(date_close) keep (dense_rank first order by date_oper desc, nvl(subschkre,0) asc) date_close
      from arh_norm_31 where acc is not null group by acc
)
select count(*) otuzbir_gun_aktiv_sayi
  from raw_aday r
  join arh_agg_31 a on a.acc = r.licsch
 where a.date_close is null or a.date_close > to_date('30-10-2026','dd-mm-yyyy');

-- ===========================================================================
-- D) ƏSL CAVAB — FƏRQ SİYAHISI: "tam tarixçədə aktiv, AMMA son 31 gündə
--    arxivə HEÇ düşməmiş" hesablar. Bunlar "bizdə olmayanlar"dır — nə qədər
--    köhnə olduqlarını (GUN_FERQI) da göstərir ki, pəncərəni nə qədər
--    genişləndirmək lazım olduğu görünsün.
-- ===========================================================================
with
raw_aday as (
    select l.licsch, l.name_licsch adi, substr(l.licsch,10,6) regnom_kod,
           l.inn_licsch voen, l.saldo_ish_nacval qaliq_nacval, l.saldo_ish_inval qaliq_inval,
           l.date_open_licsch, l.date_close_licsch
      from odb.licsch l
     where substr(l.licsch,1,2) in ('20','21')
       and substr(l.licsch,5,1) in ('0','5')
       and substr(l.licsch,3,1) <> '9'
       and l.date_open_licsch is not null
       and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
       and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'))
),
arh_norm_tam as (
    select licschkre acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licschpkre acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licsch_19 acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licschppkre acc, date_oper, date_close, subschkre from odb.arh_licschkre
     where date_oper <= to_date('30-10-2026','dd-mm-yyyy')
),
arh_agg_tam as (
    select acc,
           max(date_oper) keep (dense_rank first order by date_oper desc, nvl(subschkre,0) asc) son_date_oper,
           max(date_close) keep (dense_rank first order by date_oper desc, nvl(subschkre,0) asc) date_close
      from arh_norm_tam where acc is not null group by acc
),
arh_norm_31 as (
    select licschkre acc from odb.arh_licschkre
     where date_oper > to_date('30-10-2026','dd-mm-yyyy') - 31 and date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licschpkre acc from odb.arh_licschkre
     where date_oper > to_date('30-10-2026','dd-mm-yyyy') - 31 and date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licsch_19 acc from odb.arh_licschkre
     where date_oper > to_date('30-10-2026','dd-mm-yyyy') - 31 and date_oper <= to_date('30-10-2026','dd-mm-yyyy')
    union all
    select licschppkre acc from odb.arh_licschkre
     where date_oper > to_date('30-10-2026','dd-mm-yyyy') - 31 and date_oper <= to_date('30-10-2026','dd-mm-yyyy')
)
select
    r.licsch, r.adi, r.regnom_kod, r.voen,
    r.date_open_licsch, r.date_close_licsch,
    a.son_date_oper                                   arxivde_son_gorundu,
    to_date('30-10-2026','dd-mm-yyyy') - a.son_date_oper  gun_ferqi,
    a.date_close                                      arxivin_bagladigi_tarix,
    nvl(r.qaliq_nacval, r.qaliq_inval)                qaliq
from raw_aday r
join arh_agg_tam a on a.acc = r.licsch
where (a.date_close is null or a.date_close > to_date('30-10-2026','dd-mm-yyyy'))   -- tam tarixçəyə görə AKTİV
  and not exists (select 1 from arh_norm_31 w where w.acc = r.licsch)               -- AMMA son 31 gündə arxivdə YOXDUR
order by gun_ferqi desc;
