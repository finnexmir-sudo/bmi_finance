/* ============================================================================
   ÖS-6.1 — "Kredit hesabları" (Kod 3): 232 TAM TARİXÇƏDƏ DƏ EYNİ ÇIXDI,
   31-günlük pəncərə SƏBƏB DEYİLMİŞ.

   95_...sql-in nəticəsi: A=643, B(tam tarixçə)=232, C(31 gün)=232, D(fərq
   siyahısı)=0 sətir. Yəni pəncərənin uzunluğunun HEÇ BİR TƏSİRİ yoxdur —
   kök səbəb BAŞQA YERDƏDİR: ya arxivdə "ən son snapshot"u TAPMA məntiqimiz
   (dense_rank + subschkre tie-break) səhv sətri seçir, ya da bəzi hesablar
   arxivdə ÜMUMİYYƏTLƏ tapılmır (fərqli bir cədvəl/konvensiya işlədir).

   Bu skript 643 raw namizədi İKİ YIĞCAM ANLAYIŞLA YENİDƏN yoxlayır:

     (a) "LOOSE" — hesabın arxivdə, HƏR HANSI tarixdə, date_close=NULL olan
         BİR DƏ OLSA sətri varmı (son snapshot-u deyil, BÜTÜN tarixçəni
         yoxlayır). Əgər bu say 293-ə yaxın çıxırsa, bizim "ən son snapshot"
         seçimi (95-dəki arh_agg) səhv sətr seçir demək idi.
     (b) "HEÇ TAPILMADI" — hesab arxivdə, HƏR HANSI sütunda, BİR DƏFƏ BELƏ
         görünməyib (KreditArhTapildi=false, pəncərədən ASILI OLMAYARAQ).
         Bunlar FƏRQLİ bir arxiv/konvensiya işlədən hesablar ola bilər.

   ⚠️ YALNIZ-OXU diaqnostikadır, Oracle-a HEÇ NƏ yazılmır. Hər bloku AYRI-AYRI
   işlədin.
   ========================================================================== */

-- ===========================================================================
-- (a) LOOSE — "arxivin HƏR HANSI tarixində date_close=NULL görünüb" sayı.
--     95-in B/C-si (232) ilə BURAYA baxın: fərq varsa, "ən son snapshot"
--     seçimi bəzi HƏQİQƏTƏN açıq hesabları səhvən "bağlı" göstərir deməkdir.
-- ===========================================================================
with raw_aday as (
    select l.licsch
      from odb.licsch l
     where substr(l.licsch,1,2) in ('20','21')
       and substr(l.licsch,5,1) in ('0','5')
       and substr(l.licsch,3,1) <> '9'
       and l.date_open_licsch is not null
       and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
       and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'))
)
select count(*) loose_acik_sayi
  from raw_aday r
 where exists (
         select 1 from odb.arh_licschkre al
          where al.date_oper <= to_date('30-10-2026','dd-mm-yyyy')
            and r.licsch in (al.licschkre, al.licschpkre, al.licsch_19, al.licschppkre)
            and al.date_close is null
       );

-- ===========================================================================
-- (b) HEÇ TAPILMADI — arxivdə (TAM tarixçə, pəncərəsiz) HEÇ bir sütunda
--     BİR DƏFƏ belə görünməyən raw namizədlərin sayı VƏ siyahısı.
-- ===========================================================================
with raw_aday as (
    select l.licsch, l.name_licsch adi, substr(l.licsch,10,6) regnom_kod,
           l.date_open_licsch, l.date_close_licsch
      from odb.licsch l
     where substr(l.licsch,1,2) in ('20','21')
       and substr(l.licsch,5,1) in ('0','5')
       and substr(l.licsch,3,1) <> '9'
       and l.date_open_licsch is not null
       and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
       and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'))
)
select r.licsch, r.adi, r.regnom_kod, r.date_open_licsch, r.date_close_licsch
  from raw_aday r
 where not exists (
         select 1 from odb.arh_licschkre al
          where r.licsch in (al.licschkre, al.licschpkre, al.licsch_19, al.licschppkre)
       )
 order by r.date_open_licsch desc;

-- ===========================================================================
-- (c) TAPILIB, AMMA SON SNAPSHOT-A GÖRƏ "BAĞLI" göstərilən, AMMA tarixçəsində
--     ƏN AZI BİR DƏFƏ date_close=NULL olmuş hesablar — bunlar "LOOSE"-un (a)
--     "AKTİV" saydığı, bizim DENSE_RANK seçimimizin "BAĞLI" saydığı dəqiq
--     ziddiyyət nöqtəsidir. Siyahı + hər ikisinin (son snapshot vs tarixçədəki
--     ən son NULL tarixi) tarixini göstərir ki, hansının doğru olduğu görünsün.
-- ===========================================================================
with raw_aday as (
    select l.licsch, l.name_licsch adi, substr(l.licsch,10,6) regnom_kod
      from odb.licsch l
     where substr(l.licsch,1,2) in ('20','21')
       and substr(l.licsch,5,1) in ('0','5')
       and substr(l.licsch,3,1) <> '9'
       and l.date_open_licsch is not null
       and l.date_open_licsch <= to_date('30-10-2026','dd-mm-yyyy')
       and (l.date_close_licsch is null or l.date_close_licsch > to_date('30-10-2026','dd-mm-yyyy'))
),
arh_norm as (
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
son_snapshot as (
    select acc,
           max(date_oper) keep (dense_rank first order by date_oper desc, nvl(subschkre,0) asc) son_date_oper,
           max(date_close) keep (dense_rank first order by date_oper desc, nvl(subschkre,0) asc) son_date_close
      from arh_norm where acc is not null group by acc
),
en_son_null_tarixi as (
    select acc, max(date_oper) en_son_null_date_oper
      from arh_norm where acc is not null and date_close is null
      group by acc
)
select
    r.licsch, r.adi, r.regnom_kod,
    s.son_date_oper, s.son_date_close,
    n.en_son_null_date_oper
from raw_aday r
join son_snapshot s on s.acc = r.licsch
join en_son_null_tarixi n on n.acc = r.licsch
where (s.son_date_close is not null and s.son_date_close <= to_date('30-10-2026','dd-mm-yyyy'))
order by n.en_son_null_date_oper desc;
