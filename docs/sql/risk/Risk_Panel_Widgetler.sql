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
   Ağır (arh_dd tam skan) sorğuları widget etmə — səhifəni yavaşladar; onları
   parametrli hesabat kimi saxla (Risk_Sorgular.sql).

   Cədvəllər: regnom, licsch, countrycode, riskler, fiziki_shexs
   ============================================================================ */


/* ========================  KPI KARTLARI  ================================== */

/* Ad: Ümumi müştərilər     | Mahiyyət: [KPI] qeydiyyatda olan */
select count(*) from regnom;

/* Ad: Qeyri-rezidentlər    | Mahiyyət: [KPI] nerezident müştəri */
select count(*) from regnom where nerezident = 1;

/* Ad: İnsayder / əlaqəli   | Mahiyyət: [KPI] insider və ya əlaqəli tərəf */
select count(*) from regnom where insider = 1 or svazanniy = 1;

/* Ad: Açıq hesablar        | Mahiyyət: [KPI] bağlanmamış hesab */
select count(*) from licsch where date_close_licsch is null;


/* ========================  QRAFİKLƏR  ===================================== */

/* Ad: Müştəri tipi         | Mahiyyət: [PIE] fiziki / hüquqi / sahibkar */
select case when yurik = 1 then 'Hüquqi'
            when predprinimatel = 1 then 'Sahibkar'
            else 'Fiziki' end tip,
       count(*) say
from   regnom
group by case when yurik = 1 then 'Hüquqi'
              when predprinimatel = 1 then 'Sahibkar'
              else 'Fiziki' end
order by say desc;

/* Ad: Ölkə üzrə müştərilər | Mahiyyət: [BAR] aktiv hesab üzrə (top) */
/* Mənbə: olke_kodlari_uzre_adlari.sql */
select case when l.countrycode = 'GEO' then 'Gürcüstan' else c.name end olke,
       count(distinct l.registrac_nomer) say
from   licsch l, countrycode c
where  l.countrycode = c.code
  and  l.date_close_licsch is null
group by case when l.countrycode = 'GEO' then 'Gürcüstan' else c.name end
order by say desc;

/* Ad: Son 12 ay açılan hesablar | Mahiyyət: [LINE] aylıq */
select to_char(date_open_licsch, 'YYYY-MM') ay, count(*) say
from   licsch
where  date_open_licsch >= add_months(trunc(sysdate, 'MM'), -11)
group by to_char(date_open_licsch, 'YYYY-MM')
order by ay;

/* Ad: Risk səviyyəsi bölgüsü | Mahiyyət: [PIE] risk reytinqi üzrə */
/* DİQQƏT: riskler join-i sənin bazanda yoxlanmalıdır (rs.code=r.riskler).
   Səhv çıxsa mətnini göndər, dəqiqləşdirək. */
select rs.name risk, count(distinct r.regnom) say
from   regnom r, riskler rs
where  r.riskler = rs.code
group by rs.name
order by say desc;
