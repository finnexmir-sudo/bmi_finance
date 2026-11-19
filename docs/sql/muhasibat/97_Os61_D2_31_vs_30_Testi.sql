/* ============================================================================
   ÖS-6.1 — "Kredit hesabları" (Kod 3): YENİ (tək sütunlu) kod deploy olundu,
   AMMA tətbiq 231 göstərdi (sizin manual 30-10-2026 testiniz 289 idi).

   Fərq: tətbiqin D2-si AYIN TƏQVİM SON GÜNÜDÜR (31-10-2026, ŞƏNBƏ), sizin
   manual testiniz isə BİRBAŞA 30-10-2026 (Excel-in tarixi) idi. Bu skript
   həmin fərqi (a) D2=31-10 ilə max-date yanaşması, (b) bizim YENİ arh_agg
   CTE-sinin (pəncərə+dense_rank) D2=31-10 ilə verdiyi nəticə arasında
   TƏK-TƏK yoxlayır.

   ⚠️ YALNIZ-OXU, Oracle-da (PL/SQL Developer-də) işlədin. Hər bloku AYRI-AYRI.
   ========================================================================== */

-- ===========================================================================
-- (1) D2=31-10-2026-dan ƏVVƏL/BƏRABƏR olan ƏN SON arxiv tarixi nədir?
--     Gözlənilən: 30-10-2026 (cümə). Başqa tarix çıxsa, kök səbəb elə BUDUR.
-- ===========================================================================
select max(date_oper) en_son_arxiv_tarixi
  from odb.arh_licschkre
 where date_oper <= to_date('31-10-2026','dd-mm-yyyy');

-- ===========================================================================
-- (2) HƏMİN ən son tarixdə (yuxarı (1)-in nəticəsi, əl ilə DƏYİŞMƏDƏN,
--     alt-sorğu kimi) sadə exact-match sayı — sizin 30-10 testinizin EYNİSİ,
--     yalnız tarixi DİNAMİK (max) tapır. Gözlənilən: 289-a yaxın.
-- ===========================================================================
select count(distinct a.licschkre) son_tarixde_acik_sayi
  from odb.arh_licschkre a
 where a.date_oper = (select max(date_oper) from odb.arh_licschkre where date_oper <= to_date('31-10-2026','dd-mm-yyyy'))
   and a.date_close is null;

-- ===========================================================================
-- (3) Yuxarıdakı (2) ilə odb.licsch-in raw filtrini BİRLƏŞDİRİB say — bizim
--     643 namizədin bu "son tarix"ə görə təmiz alt-dəsti. Gözlənilən: 289-a
--     yaxın (sizin 278+11 testinizin D2=31-10 ilə təkrarı).
-- ===========================================================================
with raw_aday as (
    select l.licsch
      from odb.licsch l
     where substr(l.licsch,1,2) in ('20','21')
       and substr(l.licsch,5,1) in ('0','5')
       and substr(l.licsch,3,1) <> '9'
       and l.date_open_licsch is not null
       and l.date_open_licsch <= to_date('31-10-2026','dd-mm-yyyy')
       and (l.date_close_licsch is null or l.date_close_licsch > to_date('31-10-2026','dd-mm-yyyy'))
)
select count(*) raw_ve_son_tarix_kesisimi
  from raw_aday r
  join odb.arh_licschkre a
    on a.licschkre = r.licsch
   and a.date_oper = (select max(date_oper) from odb.arh_licschkre where date_oper <= to_date('31-10-2026','dd-mm-yyyy'))
 where a.date_close is null;

-- ===========================================================================
-- (4) BİZİM YENİ arh_agg MƏNTİQİMİZ (pəncərə D2-31..D2 + dense_rank), AMMA
--     TƏK-TƏK, raw_aday-a BAĞLAMADAN — "pəncərə+dense_rank" (2)-dəki sadə
--     exact-match ilə EYNİ nəticəni verirmi? Gözlənilən: (2) ilə EYNİ, 289.
--     Fərqli çıxarsa, kök səbəb "dense_rank"ın ÖZÜNDƏDİR (tie-break, və ya
--     D2-31..D2 pəncərəsində max(date_oper) 30-10-dan FƏRQLİ bir tarix tapır).
-- ===========================================================================
with arh_agg_yeni as (
    select a.licschkre acc,
           max(a.date_close) keep (dense_rank first order by a.date_oper desc) date_close
      from odb.arh_licschkre a
     where a.date_oper >  to_date('31-10-2026','dd-mm-yyyy') - 31
       and a.date_oper <= to_date('31-10-2026','dd-mm-yyyy')
       and a.licschkre is not null
     group by a.licschkre
)
select count(*) arh_agg_acik_sayi
  from arh_agg_yeni
 where date_close is null or date_close > to_date('31-10-2026','dd-mm-yyyy');

-- ===========================================================================
-- (5) (4)-ü raw_aday-a BAĞLAYIB say — bu, DƏQİQ bizim tətbiqin HESABLADIĞI
--     "231"-i yenidən qurur (əgər (4) 289-dan FƏRQLİDİRSƏ, bu da 231-ə yaxın
--     çıxacaq — əgər (4) 289-dursa, bu da 289-a yaxın çıxmalıdır, 231 ayrı bir
--     yerdə — raw_aday özündə — itir deməkdir).
-- ===========================================================================
with raw_aday as (
    select l.licsch
      from odb.licsch l
     where substr(l.licsch,1,2) in ('20','21')
       and substr(l.licsch,5,1) in ('0','5')
       and substr(l.licsch,3,1) <> '9'
       and l.date_open_licsch is not null
       and l.date_open_licsch <= to_date('31-10-2026','dd-mm-yyyy')
       and (l.date_close_licsch is null or l.date_close_licsch > to_date('31-10-2026','dd-mm-yyyy'))
),
arh_agg_yeni as (
    select a.licschkre acc,
           max(a.date_close) keep (dense_rank first order by a.date_oper desc) date_close
      from odb.arh_licschkre a
     where a.date_oper >  to_date('31-10-2026','dd-mm-yyyy') - 31
       and a.date_oper <= to_date('31-10-2026','dd-mm-yyyy')
       and a.licschkre is not null
     group by a.licschkre
)
select count(*) tetbiqin_hesabladigi_231
  from raw_aday r
  join arh_agg_yeni a on a.acc = r.licsch
 where a.date_close is null or a.date_close > to_date('31-10-2026','dd-mm-yyyy');
