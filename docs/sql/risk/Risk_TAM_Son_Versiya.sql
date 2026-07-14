/* ============================================================================
   RISK DEPARTAMENTİ — TAM SON VERSİYA (admin panelə əlavə etmək üçün)
   ----------------------------------------------------------------------------
   Hər sorğu üçün admin panel → Oracle Sorğular → Yeni sorğu:
       Ad          = başlıq (aşağıda göstərilib)
       Departament = Risk
       Mahiyyət    = aşağıda göstərilən mətn (TAG-lə birlikdə, dəyişdirmə)
       SQL mətni   = həmin blokun SELECT-i (";" işarəsi olmadan yapışdır)

   TAG (Mahiyyətin əvvəlində) → widget tipi:
       [KPI]  = rəqəm kartı        (klikləyəndə siyahı açılır)
       [PIE]  = dairə qrafiki
       [BAR]  = sütun qrafiki
       [LINE] = xətt (trend) qrafiki
       tag yoxdursa → adi hesabat kartı (parametrli olanlar)

   PARAMETR token-ləri (Risk səhifəsi avtomatik tarix/ədəd xanası göstərir):
       {BASTARIX} {SONTARIX} {TARIX} = tarix
       {HEDD} = məbləğ həddi      {IL} = il

   AKTİV MÜŞTƏRİ təyini (bütün widget-lərdə):
       açıq hesab      : l.date_close_licsch is null
       müştəri hesabı  : substr(l.licsch,1,1) in ('3','4')   (bank/texniki deyil)
       real müştəri    : r.yurik=1 or r.fizik=1 or r.predprinimatel=1

   ┌─ ƏLAVƏ ETMƏ SİYAHISI ─────────────────────────────────────────────┐
   │ DASHBOARD (1–9) — hamısını əlavə et                                │
   │   1. [KPI] Aktiv müştərilər        6. [PIE] Müştəri tipi           │
   │   2. [KPI] Qeyri-rezidentlər       7. [BAR] Ölkə üzrə müştərilər   │
   │   3. [KPI] İnsayder / əlaqəli      8. [LINE] Son 12 ay açılan hes. │
   │   4. [KPI] Açıq müştəri hesabları  9. [PIE] Risk səviyyəsi         │
   │   5. [KPI] Ümumi müştərilər                                        │
   │ HESABATLAR (10–16) — parametrli/detallı, istəyə görə              │
   └───────────────────────────────────────────────────────────────────┘

   Cədvəllər: regnom, licsch, countrycode, riskler, arh_dd, fiziki_shexs,
              huquqi_shexs, imza_huquqi_olan_shexsler
   ============================================================================ */



/* ####################  DASHBOARD — KPI KARTLARI (1–5)  #################### */


/* ── 1 ─────────────────────────────────────────────────────────────────────
   Ad      : Aktiv müştərilər
   Mahiyyət: [KPI] açıq hesablı real müştəri
--------------------------------------------------------------------------- */
select distinct r.regnom qeyd_no, r.name_regnom musteri, r.inn_regnom voen,
       r.pincode fin, r.passport pasport,
       case when r.yurik=1 then 'Hüquqi' when r.predprinimatel=1 then 'Sahibkar'
            when r.fizik=1 then 'Fiziki' end tip
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
order by r.name_regnom;


/* ── 2 ─────────────────────────────────────────────────────────────────────
   Ad      : Qeyri-rezidentlər
   Mahiyyət: [KPI] aktiv müştəri, nerezident
--------------------------------------------------------------------------- */
select distinct r.regnom qeyd_no, r.name_regnom musteri, r.inn_regnom voen,
       r.pincode fin, r.passport pasport, r.telefon telefon
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
  and  r.nerezident = 1
order by r.name_regnom;


/* ── 3 ─────────────────────────────────────────────────────────────────────
   Ad      : İnsayder / əlaqəli
   Mahiyyət: [KPI] aktiv müştəri, insider/əlaqəli
--------------------------------------------------------------------------- */
select distinct r.regnom qeyd_no, r.name_regnom musteri, r.inn_regnom voen, r.passport pasport,
       case when r.insider=1 then 'insayder' end insayder,
       case when r.svazanniy=1 then 'əlaqəli' end elaqeli
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
  and  (r.insider = 1 or r.svazanniy = 1)
order by r.name_regnom;


/* ── 4 ─────────────────────────────────────────────────────────────────────
   Ad      : Açıq müştəri hesabları
   Mahiyyət: [KPI] real müştəriyə aid açıq hesab
--------------------------------------------------------------------------- */
select l.licsch hesab, r.name_regnom musteri, r.inn_regnom voen, r.pincode fin,
       l.name_licsch hesab_adi, l.date_open_licsch acilma
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
order by l.date_open_licsch desc;


/* ── 5 ─────────────────────────────────────────────────────────────────────
   Ad      : Ümumi müştərilər
   Mahiyyət: [KPI] qeydiyyatda olan (cəmi rəqəm)
--------------------------------------------------------------------------- */
select count(*) from regnom;



/* ####################  DASHBOARD — QRAFİKLƏR (6–9)  ###################### */


/* ── 6 ─────────────────────────────────────────────────────────────────────
   Ad      : Müştəri tipi
   Mahiyyət: [PIE] aktiv — fiziki/hüquqi/sahibkar
--------------------------------------------------------------------------- */
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


/* ── 7 ─────────────────────────────────────────────────────────────────────
   Ad      : Ölkə üzrə müştərilər
   Mahiyyət: [BAR] aktiv müştəri, ölkə üzrə
--------------------------------------------------------------------------- */
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


/* ── 8 ─────────────────────────────────────────────────────────────────────
   Ad      : Son 12 ay açılan hesablar
   Mahiyyət: [LINE] real müştəri hesabı, aylıq
--------------------------------------------------------------------------- */
select to_char(l.date_open_licsch, 'YYYY-MM') ay, count(distinct l.licsch) say
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_open_licsch >= add_months(trunc(sysdate, 'MM'), -11)
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
group by to_char(l.date_open_licsch, 'YYYY-MM')
order by ay;


/* ── 9 ─────────────────────────────────────────────────────────────────────
   Ad      : Risk səviyyəsi bölgüsü
   Mahiyyət: [PIE] aktiv müştəri, risk reytinqi
   Qeyd    : '%%%%%%' = boş risk → 'boş' göstərilir
--------------------------------------------------------------------------- */
select case when rs.name = '%%%%%%%%%%%%%%%%' then 'boş' else rs.name end risk,
       count(distinct r.regnom) say
from   regnom r, licsch l, riskler rs
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
  and  r.riskler = rs.code
group by case when rs.name = '%%%%%%%%%%%%%%%%' then 'boş' else rs.name end
order by say desc;



/* ####################  HESABATLAR — parametrli/detallı (10–16)  ######### */
/* Bunlar tag-siz əlavə olunur (aşağıda hesabat kartı kimi görünür).        */
/* Parametr token-i olanlar açılanda tarix/ədəd xanası göstərir.            */


/* ── 10 ────────────────────────────────────────────────────────────────────
   Ad      : Böyük əməliyyatlar
   Mahiyyət: Müştəri üzrə gün aralığında hədddən böyük mədaxil
   Parametr: Başlanğıc + Son tarix + Hədd
   Mənbə   : AML_limit_sorgu.sql
--------------------------------------------------------------------------- */
select d.date_oper tar, l.registrac_nomer q_no, r.name_regnom adi,
       r.passport pas, r.pincode fin, r.adress unvan,
       case when r.m=1 then 'kişi' when r.f=1 then 'qadın' end cins,
       sum(d.summa_v_nacval) mebleg, count(*) emel_sayi
from   arh_dd d, regnom r, licsch l
where  d.date_oper between {BASTARIX} and {SONTARIX}
  and  substr(d.kredit,1,2) in (38,39,40,41)
  and  d.kredit = l.licsch and l.registrac_nomer = r.regnom
group by d.date_oper, l.registrac_nomer, r.name_regnom, r.passport, r.pincode, r.adress, r.m, r.f
having sum(d.summa_v_nacval) > {HEDD}
order by mebleg desc;


/* ── 11 ────────────────────────────────────────────────────────────────────
   Ad      : Qeyri-rezident hesablar (tarixdən)
   Mahiyyət: Qeyri-rezidentlərin seçilmiş tarixdən açdığı hesablar
   Parametr: Başlanğıc tarix
   Mənbə   : AML/qeyri_rezidentler acilmis hesablar secilmis tarixden.sql
--------------------------------------------------------------------------- */
select distinct r.regnom qeyd_no, r.name_regnom ad, f.vetendashligi vetendasligi,
       f.vezifesi vezife, r.telefon telefon
from   regnom r, licsch l, fiziki_shexs f
where  r.regnom = f.regnom
  and  r.regnom = l.registrac_nomer
  and  r.nerezident = 1
  and  substr(l.licsch,1,1) = 4
  and  l.date_open_licsch >= {BASTARIX}
order by r.name_regnom;


/* ── 12 ────────────────────────────────────────────────────────────────────
   Ad      : Yüksək risk + KYC yeniləmə
   Mahiyyət: Risk səviyyəsinə görə KYC yenilənməli müştərilər
   Parametr: Tarix
   Mənbə   : AML_riskler.sql  ('15-04-2024' → {TARIX})
--------------------------------------------------------------------------- */
SELECT distinct m.qeyd, m.ad, m.risk,
       CASE WHEN m.risk = 'AŞAĞI'  AND {TARIX}-ac_tar >= 1095 THEN 'yenilənməli'
            WHEN m.risk = 'ORTA'   AND {TARIX}-ac_tar >= 730  THEN 'yenilənməli'
            WHEN m.risk = 'YÜKSƏK' AND {TARIX}-ac_tar >= 365  THEN 'yenilənməli' END AS status,
       m.ac_tar, m.duzelis, t.tar
FROM licsch l,
     (SELECT r.regnom AS qeyd, r.name_regnom AS ad,
             CASE WHEN rs.name = '%%%%%%%%%%%%%%%%' THEN 'boş' ELSE rs.name END AS risk, rs.name,
             MAX(l.date_open_licsch) AS ac_tar, r.note AS duzelis
      FROM regnom r, licsch l, riskler rs
      WHERE l.registrac_nomer = r.regnom AND rs.code = 1 AND rs.code = r.riskler
        AND l.date_close_licsch IS NULL AND SUBSTR(l.licsch,1,5) <> '99999'
      GROUP BY r.regnom, r.name_regnom,
               CASE WHEN rs.name = '%%%%%%%%%%%%%%%%' THEN 'boş' ELSE rs.name END, rs.name, r.note) m,
     (SELECT distinct r.regnom qeyd,
             case when r.fizik=1 or r.svazanniy=1 then f.elave_melumatlarin_tarixi else h.customerrenewaldate end tar
      FROM regnom r, fiziki_shexs f, huquqi_shexs h, licsch l
      WHERE ((r.regnom=f.regnom) or (r.regnom=h.regnom)) and l.registrac_nomer=r.regnom
        and l.date_close_licsch is null) t
WHERE m.qeyd = t.qeyd
  and (( substr(l.licsch,1,1) in (2,3,4))
       and (l.registrac_nomer in (select distinct registrac_nomer from licsch, balschkli b
             where substr(licsch,1,5) = b.balsch and registrac_nomer = l.registrac_nomer)))
  and m.qeyd = l.registrac_nomer and l.date_close_licsch is null
order by m.qeyd asc;


/* ── 13 ────────────────────────────────────────────────────────────────────
   Ad      : Benefisiar mülkiyyətçilər
   Mahiyyət: Təsisçi payı ≥ 10% olan şəxslər
   Parametr: yoxdur
   Mənbə   : AML_Benefisiar_hesabat.sql
--------------------------------------------------------------------------- */
select h.qeydiyyat_tarixi q_tar, i.soyadi soyad, i.adi ad, i.ata_adi ata, i.fin fin,
       i.vetendashligi vetendasliq, i.olke olke, i.doguldugu_tarix d_tar,
       REGEXP_SUBSTR(i.seriyasi_ve_nomresi, '^[A-Z]+') seriya,
       REGEXP_SUBSTR(i.seriyasi_ve_nomresi, '[0-9]+') seriya_no,
       i.verilme_tarixi ver_tar, i.tesischinin_payi pay, i.regnom qeyd_no
from   imza_huquqi_olan_shexsler i, huquqi_shexs h
where  i.tesischinin_payi >= 10
  and  i.regnom = h.regnom
order by i.tesischinin_payi desc;


/* ── 14 ────────────────────────────────────────────────────────────────────
   Ad      : 12 ay aktiv VÖEN-lər
   Mahiyyət: İl ərzində hər ay ən azı 2 əməliyyatı olan VÖEN-lər
   Parametr: İl
   Mənbə   : Butun_ve_aktiv_voenler...sql
--------------------------------------------------------------------------- */
SELECT DISTINCT r.inn_regnom AS voen, r.name_regnom AS ad, r.biznesin_meyarlari AS novu
FROM regnom r, arh_dd dd, licsch l
WHERE to_char(dd.date_oper,'yyyy') = {IL}
  AND l.registrac_nomer = r.regnom
  AND l.licsch IN (dd.debet, dd.kredit)
  AND (r.yurik=1 OR r.predprinimatel=1)
  AND dd.vid_operacii < 97
  AND (substr(dd.debet,1,1) != 4 AND substr(dd.kredit,1,1) != 1)
  AND substr(dd.debet,1,2) != 20
GROUP BY r.inn_regnom, r.name_regnom, r.biznesin_meyarlari
HAVING COUNT(DISTINCT EXTRACT(MONTH FROM dd.date_oper)) = 12
ORDER BY voen;


/* ── 15 ────────────────────────────────────────────────────────────────────
   Ad      : Terminal əməliyyatları
   Mahiyyət: İl üzrə terminal mədaxil əməliyyatları (yekun)
   Parametr: İl
   Mənbə   : Odenis_Terminal_emeliyyat_sorgusu.sql
--------------------------------------------------------------------------- */
select count(distinct substr(dd.kredit,10,6)) musteri_say,
       count(dd.kredit) emel_say,
       sum(dd.summa_v_nacval) mebleg
from   arh_dd dd
where  EXTRACT(YEAR FROM dd.date_oper) = {IL}
  and  dd.debet in ('25019000000000300006','25019000000000300007')
  and  substr(dd.kredit,1,1) in (3,4);


/* ── 16 ────────────────────────────────────────────────────────────────────
   Ad      : Ölkə üzrə müştərilər (siyahı)
   Mahiyyət: Ölkə üzrə ad-ad detallı siyahı (7-ci qrafikdən fərqli)
   Parametr: Tarix
   Mənbə   : olke_kodlari_uzre_adlari.sql
--------------------------------------------------------------------------- */
select distinct l.registrac_nomer qeyd_no, r.name_regnom ad,
       case when l.countrycode='GEO' then 'Gürcüstan' else c.name end olke
from   licsch l, countrycode c, regnom r
where  l.registrac_nomer = r.regnom
  and  (l.date_close_licsch is null or l.date_close_licsch > {TARIX})
  and  l.countrycode = c.code
order by olke, r.name_regnom;
