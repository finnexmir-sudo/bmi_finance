/* ============================================================================
   RISK DASHBOARD — panel widget-ləri (KPI kartları + qrafiklər)
   ----------------------------------------------------------------------------
   Bu sorğular Risk səhifəsinin YUXARISINDA avtomatik görünür (klik lazım deyil).
   Fərq: widget olması üçün Mahiyyət sahəsinin ƏVVƏLİNƏ tag qoyulur.

   Admin panel → Oracle Sorğular → Yeni sorğu:
     - Ad         : başlıq (kartın/qrafikin adı)
     - Departament: Risk
     - Mahiyyət   : AŞAĞIDAKI TAG ilə başlamalıdır:
         [KPI]  → tək rəqəm kartı   (sorğu 1 sətir, sonuncu sütun = rəqəm)
         [BAR]  → sütun qrafiki     (sütun0 = etiket, sonuncu sütun = rəqəm)
         [PIE]  → dairə qrafiki     (sütun0 = etiket, sonuncu sütun = rəqəm)
         [LINE] → xətt qrafiki      (sütun0 = etiket/tarix, sonuncu = rəqəm)
       (tag-dan sonra qısa izah yaza bilərsən, məs: "[KPI] cari ilə")
     - SQL mətni  : müvafiq blok

   QAYDA: widget-lər parametrsiz olmalıdır ({BASTARIX} və s. OLMAZ) — çünki
   səhifə açılan kimi avtomatik icra olunurlar. Tarix lazımdırsa SYSDATE istifadə et.
   Ağır (arh_dd tam skan) sorğuları widget etmə — səhifəni yavaşladar.

   AKTİV MÜŞTƏRİ TƏYİNİ (Meyransa/Vuqar sorğularından götürülüb):
     - açıq hesab      : l.date_close_licsch is null
     - müştəri hesabı  : substr(l.licsch,1,1) in ('3','4')   (bank/texniki hesab yox)
     - aktiv müştəri   : ən azı bir açıq müştəri hesabı olan regnom
   Beləliklə "regnom-da neçə sətir var" YOX — real aktiv müştəri sayılır.
   (Hesab sinifini bazana görə dəyişə bilərsən: bəzi sorğularda (2,3,4) da var.)

   Cədvəllər: regnom, licsch, countrycode, riskler
   ============================================================================ */


/* ========================  KPI KARTLARI  ================================== */

/* Ad: Aktiv müştərilər     | Mahiyyət: [KPI] açıq hesabı olan */
select count(distinct l.registrac_nomer)
from   licsch l
where  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4');

/* Ad: Qeyri-rezidentlər    | Mahiyyət: [KPI] aktiv, nerezident */
select count(distinct r.regnom)
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  r.nerezident = 1;

/* Ad: İnsayder / əlaqəli   | Mahiyyət: [KPI] aktiv, insider/əlaqəli tərəf */
select count(distinct r.regnom)
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.insider = 1 or r.svazanniy = 1);

/* Ad: Açıq hesablar        | Mahiyyət: [KPI] müştəri hesabları */
select count(*)
from   licsch
where  date_close_licsch is null
  and  substr(licsch,1,1) in ('3','4');


/* ========================  QRAFİKLƏR  ===================================== */

/* Ad: Müştəri tipi         | Mahiyyət: [PIE] aktiv — fiziki/hüquqi/sahibkar */
select case when r.yurik = 1 then 'Hüquqi'
            when r.predprinimatel = 1 then 'Sahibkar'
            when r.fizik = 1 then 'Fiziki' end tip,
       count(distinct r.regnom) say
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
group by case when r.yurik = 1 then 'Hüquqi'
              when r.predprinimatel = 1 then 'Sahibkar'
              when r.fizik = 1 then 'Fiziki' end
order by say desc;

/* Ad: Ölkə üzrə müştərilər | Mahiyyət: [BAR] aktiv hesab üzrə (top) */
/* Mənbə: olke_kodlari_uzre_adlari.sql */
select case when l.countrycode = 'GEO' then 'Gürcüstan' else c.name end olke,
       count(distinct l.registrac_nomer) say
from   licsch l, countrycode c
where  l.countrycode = c.code
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
group by case when l.countrycode = 'GEO' then 'Gürcüstan' else c.name end
order by say desc;

/* Ad: Son 12 ay açılan hesablar | Mahiyyət: [LINE] müştəri hesabları, aylıq */
select to_char(date_open_licsch, 'YYYY-MM') ay, count(*) say
from   licsch
where  date_open_licsch >= add_months(trunc(sysdate, 'MM'), -11)
  and  substr(licsch,1,1) in ('3','4')
group by to_char(date_open_licsch, 'YYYY-MM')
order by ay;

/* Ad: Risk səviyyəsi bölgüsü | Mahiyyət: [PIE] aktiv müştəri, risk reytinqi */
/* DİQQƏT: riskler join-i sənin bazanda yoxlanmalıdır (rs.code = r.riskler).
   Səhv çıxsa mətnini göndər, dəqiqləşdirək. */
select rs.name risk, count(distinct r.regnom) say
from   regnom r, licsch l, riskler rs
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  r.riskler = rs.code
group by rs.name
order by say desc;
