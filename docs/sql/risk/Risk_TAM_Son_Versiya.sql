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
       müştəri hesabı  : substr(l.licsch,1,1) in ('2','3','4')
                         (2=kredit, 3/4=cari hesab; bank/texniki deyil.
                          Cari bağlı olsa da açıq krediti olan müştəri AKTİVdir.)
       real müştəri    : r.yurik=1 or r.fizik=1 or r.predprinimatel=1

   ┌─ ƏLAVƏ ETMƏ SİYAHISI ─────────────────────────────────────────────┐
   │ DASHBOARD (1–8) — hamısını əlavə et                                │
   │   1. [KPI] Aktiv müştərilər        5. [PIE] Müştəri tipi           │
   │   2. [KPI] Qeyri-rezidentlər       6. [BAR] Ölkə üzrə müştərilər   │
   │   3. [KPI] İnsayder / əlaqəli      7. [LINE] Son 12 ay açılan hes. │
   │   4. [KPI] Açıq müştəri hesabları  8. [PIE] Risk səviyyəsi         │
   │ HESABATLAR (9–18) — parametrli/detallı, istəyə görə               │
   │   16. Vintage (verilmə ili → default)  17. Müddət (maturity)      │
   │   18. Vintage — kreditlər (il üzrə, #16 detalı — müştərilər)      │
   │   19. Ödəniş təqvimi (srokpogprockre) — aylıq ödənişlər  (YOXLA)   │
   └───────────────────────────────────────────────────────────────────┘

   Cədvəllər: regnom, licsch, countrycode, riskler, arh_dd, fiziki_shexs,
              huquqi_shexs, imza_huquqi_olan_shexsler
   ============================================================================ */



/* ####################  DASHBOARD — KPI KARTLARI (1–4)  #################### */


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
  and  substr(l.licsch,1,1) in ('2','3','4')
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
  and  substr(l.licsch,1,1) in ('2','3','4')
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
  and  substr(l.licsch,1,1) in ('2','3','4')
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
  and  substr(l.licsch,1,1) in ('2','3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
order by l.date_open_licsch desc;



/* ####################  DASHBOARD — QRAFİKLƏR (5–8)  ###################### */


/* ── 5 ─────────────────────────────────────────────────────────────────────
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
  and  substr(l.licsch,1,1) in ('2','3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
group by case when r.yurik = 1 then 'Hüquqi'
              when r.predprinimatel = 1 then 'Sahibkar'
              when r.fizik = 1 then 'Fiziki' end
order by say desc;


/* ── 6 ─────────────────────────────────────────────────────────────────────
   Ad      : Ölkə üzrə müştərilər
   Mahiyyət: [BAR] aktiv müştəri, ölkə üzrə
--------------------------------------------------------------------------- */
select case when l.countrycode = 'GEO' then 'Gürcüstan' else c.name end olke,
       count(distinct r.regnom) say
from   regnom r, licsch l, countrycode c
where  r.regnom = l.registrac_nomer
  and  l.countrycode = c.code
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('2','3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
group by case when l.countrycode = 'GEO' then 'Gürcüstan' else c.name end
order by say desc;


/* ── 7 ─────────────────────────────────────────────────────────────────────
   Ad      : Son 12 ay açılan hesablar
   Mahiyyət: [LINE] real müştəri hesabı, aylıq
--------------------------------------------------------------------------- */
select to_char(l.date_open_licsch, 'YYYY-MM') ay, count(distinct l.licsch) say
from   regnom r, licsch l
where  r.regnom = l.registrac_nomer
  and  l.date_open_licsch >= add_months(trunc(sysdate, 'MM'), -11)
  and  substr(l.licsch,1,1) in ('2','3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
group by to_char(l.date_open_licsch, 'YYYY-MM')
order by ay;


/* ── 8 ─────────────────────────────────────────────────────────────────────
   Ad      : Risk səviyyəsi bölgüsü
   Mahiyyət: [PIE] aktiv müştəri, risk reytinqi
   Qeyd    : '%%%%%%' = boş risk → 'boş' göstərilir
--------------------------------------------------------------------------- */
select case when rs.name = '%%%%%%%%%%%%%%%%' then 'boş' else rs.name end risk,
       count(distinct r.regnom) say
from   regnom r, licsch l, riskler rs
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('2','3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
  and  r.riskler = rs.code
group by case when rs.name = '%%%%%%%%%%%%%%%%' then 'boş' else rs.name end
order by say desc;



/* ####################  HESABATLAR — parametrli/detallı (9–15)  ######### */
/* Bunlar tag-siz əlavə olunur (aşağıda hesabat kartı kimi görünür).        */
/* Parametr token-i olanlar açılanda tarix/ədəd xanası göstərir.            */


/* ── 9 ─────────────────────────────────────────────────────────────────────
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


/* ── 10 ────────────────────────────────────────────────────────────────────
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


/* ── 11 ────────────────────────────────────────────────────────────────────
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


/* ── 12 ────────────────────────────────────────────────────────────────────
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


/* ── 13 ────────────────────────────────────────────────────────────────────
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


/* ── 14 ────────────────────────────────────────────────────────────────────
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


/* ── 15 ────────────────────────────────────────────────────────────────────
   Ad      : Ölkə üzrə müştərilər (siyahı)
   Mahiyyət: Ölkə üzrə ad-ad detallı siyahı (6-cı BAR qrafiki ilə eyni süzgəc)
   Parametr: yoxdur
   Mənbə   : olke_kodlari_uzre_adlari.sql
   QEYD    : 6-cı qrafiklə EYNİ real-aktiv-müştəri süzgəci qoyuldu — beləliklə
             siyahı və say uyğun gəlir. Əvvəl süzgəc olmadığı üçün bankın öz
             filialı ("BMİ - HAMBURG BRANCH" / Almaniya) kimi qeyri-müştərilər
             də siyahıya düşürdü.
--------------------------------------------------------------------------- */
select distinct r.regnom qeyd_no, r.name_regnom ad,
       case when l.countrycode='GEO' then 'Gürcüstan' else c.name end olke
from   regnom r, licsch l, countrycode c
where  r.regnom = l.registrac_nomer
  and  l.date_close_licsch is null
  and  substr(l.licsch,1,1) in ('2','3','4')
  and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
  and  l.countrycode = c.code
order by olke, r.name_regnom;


/* ####################  KREDİT PORTFELİ / DİNAMİKA (16–17)  ############# */
/* Cədvəl: odb.licschkre (CARI kredit hesabları — arxiv arh_licschkre YOX,
   snapshot uyğunluğu problemi olmasın deyə). Gecikmə günü (DPD) isə
   view_nacpogprokre_all-dan BU GÜNƏ bağlanır: x.date_oper = to_date(sysdate):
       odb.tar_ferq360(x.date_oper, nvl(x.lastoverduedate, x.date_oper))
   (day_uderproc = ödəniş günü, DPD DEYİL!). Birləşmə: licschpkre + subschkre.
   Sütunlar: date_open=verilmə, date_planclose=son ödəmə, summakre=verilmiş,
   summa=cari qalıq. Müştəri: substr(licschkre,10,6) = r.regnom.
   MANATA ÇEVİRMƏ (bazada xam rəqəm valyutadadır — hazır metod):
       məbləğ * round(odb.func_get_kurval(substr(licschkre,6,2), tarix), 6)
       (substr(...,6,2)=valyuta kodu; AZN üçün kurs=1). ×100 YOX — o köhnə IT qalığı idi.
   DİQQƏT: default həddi (90 gün) bazanda yoxlanmalıdır. */


/* ── 16 ────────────────────────────────────────────────────────────────────
   Ad      : Vintage (verilmə ili üzrə default)
   Mahiyyət: Hansı ildə verilən kreditlər daha çox gecikir (90+ gün)
   Parametr: yoxdur      Vəziyyət: ✅ bazada yoxlanıldı (0.12 san).

   NECƏ İŞLƏYİR:
     1) licschkre (cari AÇIQ kreditlər) + view_nacpogprokre_all (gecikmə) —
        licschpkre+subschkre üzrə birləşir; view BU GÜNƏ (x.date_oper=to_date(sysdate)).
     2) Filtr: yalnız açıq kredit (date_close is null) + düzgün hesab (length=20).
     3) MANAT: hər kredit summakre × func_get_kurval(valyuta_kodu, bugün) = meb_azn.
     4) GECİKMƏ (DPD): tar_ferq360(bugün, nvl(son_gecikmə_tarixi, bugün)) = gec_gun.
     5) Qruplaşma: verilmə İLİ (date_open ili).
     6) Hər il üçün: say, cəmi məbləğ(manat), 90+ gün gecikən say (default_say),
        default_faizi = 90+ gün gecikən məbləğ ÷ ümumi məbləğ × 100.
     → Nəticə: hansı ilin kreditləri indi daha çox defolt olub (vintage əyrisi).

   QEYD: həftəsonu/bayramda to_date(sysdate) snapshot tapmasa [ALT] blokuna bax.

   ★ KLİK DRILL-DOWN (il-ə klik → müştəri siyahısı #18):
     #16-nın Mahiyyət sahəsinin SONUNA bu direktivi əlavə et:
        {DRILL:Vintage kreditlər|il|verilme_ili}
     Format: {DRILL:<hədəf hesabatın adı>|<parametr>|<sütun adı>}
     Yəni: cədvəldə sətrə klikləyəndə həmin sətrin "verilme_ili" dəyəri #18-ə
     "il" parametri kimi ötürülür və #18 avtomatik açılır.
     ({DRILL:...} istifadəçiyə görünmür — Mahiyyətdən avtomatik çıxarılır.)
     Tam Mahiyyət nümunəsi:
        Hansı ildə verilən kreditlər daha çox gecikir (90+ gün) {DRILL:Vintage kreditlər|il|verilme_ili}
--------------------------------------------------------------------------- */
with kr as (
  select extract(year from lk.date_open)                                              il,
         lk.summakre * round(odb.func_get_kurval(substr(lk.licschkre,6,2), to_date(sysdate)), 6)  meb_azn,
         odb.tar_ferq360(x.date_oper, nvl(x.lastoverduedate, x.date_oper))             gec_gun
  from   odb.licschkre lk, view_nacpogprokre_all x
  where  lk.licschpkre = x.licschpkre
    and  lk.subschkre  = x.subschkre
    and  x.date_oper   = to_date(sysdate)
    and  lk.date_close is null
    and  length(lk.licschkre) = 20
)
select il                                                    verilme_ili,
       count(*)                                              kredit_sayi,
       round(sum(meb_azn), 2)                                verilmis_mebleg,
       sum(case when gec_gun >= 90 then 1 else 0 end)        default_say,
       round(100 * sum(case when gec_gun >= 90 then meb_azn else 0 end)
             / nullif(sum(meb_azn), 0), 2)                   default_faizi
from   kr
group by il
order by il;

/* [ALT] Həftəsonu/bayramda da işləsin deyə — son iş günü snapshot-u (calendar).
   Yuxarıdakı boş qaytarsa to_date(sysdate)-i bununla əvəz et (2 yerdə):
       (select max(c.date_oper) from calendar c
         where (c.space_or_star is null or c.space_or_star<>'*') and c.date_oper<=sysdate)
   — həm x.date_oper filtrində, həm func_get_kurval-ın tarix arqumentində. */


/* ── 17 ────────────────────────────────────────────────────────────────────
   Ad      : Müddət uyğunsuzluğu (maturity)
   Mahiyyət: Yaxın vaxtda ödəniş vaxtı çatan böyük kreditlər
   Parametr: Son tarix (nə vaxta qədər) + Hədd (min tam qalıq, manat)
   Sütunlar: ESAS_QALIQ  = əsas borc qalığı (summa)
             VK_QALIQ    = vaxtı keçmiş / problemli borc qalığı (summa_19)
             TAM_QALIQ   = esas + vk = ümumi borc (real risk məbləği)
             GECIKME_GUN = DPD (view_nacpogprokre_all + tar_ferq360)
             VEZIYYET    = normal (<90 gün) / DEFAULT (90+ gün — icra/problemli)
   MƏNTİQ  : İcradakı / uzun gecikmiş kreditlər hesabatda QALIR, amma VEZIYYET
             sütunu ilə etiketlənir. Belə kreditin date_planclose-u praktikada
             saxtadır (cədvəl üzrə ödənmir, məhkəmə/icra ilə yığılır) — DEFAULT
             etiketi risk zabitinə bunu bildirir; likvidlik gözləntisinə salma.
   QEYD    : summa & summa_19 hər ikisi odb.licschkre sütunudur. GECIKME_GUN üçün
             view_nacpogprokre_all snapshot join-i əlavə edildi (#16/#18 ilə eyni).
             Hədd filtri TAM_QALIQ-ə görə işləyir (ümumi borca baxır).
--------------------------------------------------------------------------- */
with kr as (
  select lk.licschkre                                                                    hesab,
         r.name_regnom                                                                   musteri,
         round(lk.summa    * round(odb.func_get_kurval(substr(lk.licschkre,6,2), to_date(sysdate)), 6), 2)  esas_qaliq,
         round(lk.summa_19 * round(odb.func_get_kurval(substr(lk.licschkre,6,2), to_date(sysdate)), 6), 2)  vk_qaliq,
         round((lk.summa + lk.summa_19) * round(odb.func_get_kurval(substr(lk.licschkre,6,2), to_date(sysdate)), 6), 2)  tam_qaliq,
         lk.date_planclose                                                               son_odeme,
         round(lk.date_planclose - trunc(sysdate))                                       qalan_gun,
         odb.tar_ferq360(x.date_oper, nvl(x.lastoverduedate, x.date_oper))               gecikme_gun,
         case when odb.tar_ferq360(x.date_oper, nvl(x.lastoverduedate, x.date_oper)) >= 90
              then 'DEFAULT' else 'normal' end                                           veziyyet,
         lk.procstavkre                                                                  faiz
  from   odb.licschkre lk, view_nacpogprokre_all x, regnom r
  where  lk.licschpkre = x.licschpkre
    and  lk.subschkre  = x.subschkre
    and  x.date_oper   = to_date(sysdate)
    and  substr(lk.licschkre, 10, 6) = r.regnom
    and  length(lk.licschkre) = 20
    and  lk.date_close is null
    and  lk.summa > 0
    and  lk.date_planclose between trunc(sysdate) and {SONTARIX}
)
select * from kr
where  tam_qaliq >= {HEDD}
order by tam_qaliq desc;


/* ── 18 ────────────────────────────────────────────────────────────────────
   Ad      : Vintage kreditlər
   Mahiyyət: Seçilmiş ildə verilən kreditlərin müştəri siyahısı (#16 detalı)
   Parametr: İl
   İzah    : #16 Vintage cədvəlində hansı ilin yüksək default-u varsa, həmin ili
             burda yaz — o ilin bütün kreditlərini/müştərilərini görürsən (kim,
             nə qədər, neçə gün gecikib, DEFAULT-durmu). #16 il-ə "klik" əvəzi.
--------------------------------------------------------------------------- */
select r.name_regnom                                                                     musteri,
       lk.licschkre                                                                      kredit_hesabi,
       round(lk.summakre * round(odb.func_get_kurval(substr(lk.licschkre,6,2), to_date(sysdate)), 6), 2)  mebleg_azn,
       to_char(lk.date_open, 'DD.MM.YYYY')                                               verilme,
       odb.tar_ferq360(x.date_oper, nvl(x.lastoverduedate, x.date_oper))                 gecikme_gun,
       case when odb.tar_ferq360(x.date_oper, nvl(x.lastoverduedate, x.date_oper)) >= 90
            then 'DEFAULT' else 'normal' end                                             veziyyet,
       lk.procstavkre                                                                    faiz
from   odb.licschkre lk, view_nacpogprokre_all x, regnom r
where  lk.licschpkre = x.licschpkre
  and  lk.subschkre  = x.subschkre
  and  x.date_oper   = to_date(sysdate)
  and  substr(lk.licschkre, 10, 6) = r.regnom
  and  lk.date_close is null
  and  length(lk.licschkre) = 20
  and  extract(year from lk.date_open) = {IL}
order by gecikme_gun desc;


/* ── 19 ────────────────────────────────────────────────────────────────────
   Ad      : Ödəniş təqvimi (aylıq ödənişlər)
   Mahiyyət: Seçilmiş tarixə qədər ödəniş vaxtı çatan planlaşdırılmış ödənişlər
   Parametr: Son tarix (nə vaxta qədər)
   Mənbə   : odb.srokpogprockre — kredit ödəniş qrafiki (plan cədvəli)

   ⚠️ YOXLAMA LAZIMDIR — srokpogprockre sütunlarının adları/mənası mənə dəqiq
      məlum deyil (item_01..item_13). Aşağıdakı hesabatı işlətməzdən əvvəl bir
      dəfə bu KƏŞF sorğusunu işlət və sütunları mənə göstər:

        select * from odb.srokpogprockre
         where licschpkre = <yoxlanacaq kreditin licschpkre-si>
         order by 1;

      Adətən bu cədvəldə: ödəniş tarixi, əsas hissə (osnovnoy), faiz hissəsi
      (procent), ümumi ödəniş sütunları olur. Hansı item_NN nəyə uyğundur —
      təsdiqlə, aşağıdakı SELECT-i ona görə dəqiqləşdirim. İndilik struktur
      SKELETdir, real sütun adları ilə əvəz olunmalıdır.
--------------------------------------------------------------------------- */
-- SKELET (sütun adları təsdiqlənəndən sonra dəqiqləşdiriləcək):
-- select lk.licschkre                                    hesab,
--        r.name_regnom                                   musteri,
--        s.<odeme_tarixi_sutunu>                         odeme_tarixi,
--        round(s.<esas_hisse>  * round(odb.func_get_kurval(substr(lk.licschkre,6,2), s.<odeme_tarixi_sutunu>), 6), 2)  esas,
--        round(s.<faiz_hisse>  * round(odb.func_get_kurval(substr(lk.licschkre,6,2), s.<odeme_tarixi_sutunu>), 6), 2)  faiz,
--        round((s.<esas_hisse> + s.<faiz_hisse>) * round(odb.func_get_kurval(substr(lk.licschkre,6,2), s.<odeme_tarixi_sutunu>), 6), 2)  ayliq_odenis
-- from   odb.srokpogprockre s, odb.licschkre lk, regnom r
-- where  s.licschpkre = lk.licschpkre
--   and  s.subschkre  = lk.subschkre
--   and  substr(lk.licschkre, 10, 6) = r.regnom
--   and  length(lk.licschkre) = 20
--   and  lk.date_close is null
--   and  s.<odeme_tarixi_sutunu> between trunc(sysdate) and {SONTARIX}
-- order by s.<odeme_tarixi_sutunu>, tam_qaliq desc;
