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

   REAL AKTİV MÜŞTƏRİ TƏYİNİ (sənin sorğularından — Meyransa/Vuqar/IRQ):
     1) açıq hesab       : l.date_close_licsch is null
     2) müştəri hesabı   : substr(l.licsch,1,1) in ('3','4')   (bank/texniki hesab yox)
     3) REAL müştəri     : r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1
        (bankın öz qeydiyyatlarında bu tip bayraqları olmur — beləliklə kənarda qalır)
   Yəni "regnom-da neçə sətir var" YOX — həm açıq hesablı, həm də müştəri tipli
   olan regnom-lar sayılır. Bank/nostro/texniki qeydlər avtomatik düşür.
   (Hesab sinifini bazana görə dəyişə bilərsən: bəzi sorğularda (2,3,4) da var.)

   Cədvəllər: regnom, licsch, countrycode, riskler
   ============================================================================ */


/* ========================  KPI KARTLARI  ================================== */

/* Ad: Aktiv müştərilər     | Mahiyyət: [KPI] açıq hesablı real müştəri */
select count(distinct r.regnom)
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1);

/* Ad: Qeyri-rezidentlər    | Mahiyyət: [KPI] aktiv müştəri, nerezident */
select count(distinct r.regnom)
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
  and  r.nerezident = 1;

/* Ad: İnsayder / əlaqəli   | Mahiyyət: [KPI] aktiv müştəri, insider/əlaqəli */
select count(distinct r.regnom)
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
  and  (r.insider = 1 or r.svazanniy = 1);

/* Ad: Açıq müştəri hesabları | Mahiyyət: [KPI] real müştəriyə aid açıq hesab */
select count(distinct l.licsch)
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1);


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

/* Ad: Ölkə üzrə müştərilər | Mahiyyət: [BAR] aktiv müştəri, ölkə üzrə (top) */
/* Mənbə: olke_kodlari_uzre_adlari.sql */
select case when l.countrycode = 'GEO' then 'Gürcüstan' else c.name end olke,
       count(distinct r.regnom) say
from   regnom r, licsch l, countrycode c
where  r.regnom = l.registrac_nomer
  and  l.countrycode = c.code
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
group by case when l.countrycode = 'GEO' then 'Gürcüstan' else c.name end
order by say desc;

/* Ad: Son 12 ay açılan hesablar | Mahiyyət: [LINE] real müştəri hesabı, aylıq */
select to_char(l.date_open_licsch, 'YYYY-MM') ay, count(distinct l.licsch) say
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_open_licsch >= add_months(trunc(sysdate, 'MM'), -11)
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
group by to_char(l.date_open_licsch, 'YYYY-MM')
order by ay;

/* Ad: Risk səviyyəsi bölgüsü | Mahiyyət: [PIE] aktiv müştəri, risk reytinqi */
/* DİQQƏT: riskler join-i sənin bazanda yoxlanmalıdır (rs.code = r.riskler).
   Səhv çıxsa mətnini göndər, dəqiqləşdirək. */
select rs.name risk, count(distinct r.regnom) say
from   regnom r, licsch l, riskler rs
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
  and  r.riskler = rs.code
group by rs.name
order by say desc;
