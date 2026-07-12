/* ============================================================================
   RISK DEPARTAMENTİ — hazır Oracle sorğuları (parametrli)
   ----------------------------------------------------------------------------
   İstifadə: Admin panel → Oracle Sorğular → Yeni sorğu:
     - Ad         : aşağıdakı başlıq (məs. "Risk — Böyük əməliyyatlar")
     - Departament: Risk
     - Mahiyyət   : qısa izah
     - SQL mətni  : müvafiq blok (parametr token-ləri ilə)

   Parametr token-ləri (Risk səhifəsi avtomatik xana göstərir):
     {BASTARIX} {SONTARIX} {TARIX} → tarix (TO_DATE ilə əvəz olunur)
     {HEDD} → ədəd (məbləğ həddi)
     {IL}   → il

   Mənbə: Samir-in mövcud SQL bazası (AML_limit_sorgu, AML_riskler və s.)
   Cədvəllər: regnom, licsch, arh_dd, fiziki_shexs, huquqi_shexs,
              imza_huquqi_olan_shexsler, riskler, countrycode
   ============================================================================ */


/* === Risk — Böyük əməliyyatlar (müştəri üzrə, gün aralığı) ================= */
/* Mənbə: AML_limit_sorgu.sql */
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


/* === Risk — Qeyri-rezidentlərin açdığı hesablar ============================ */
/* Mənbə: AML/qeyri_rezidentler acilmis hesablar secilmis tarixden.sql */
select distinct r.regnom qeyd_no, r.name_regnom ad, f.vetendashligi vetendasligi,
       f.vezifesi vezife, r.telefon telefon
from   regnom r, licsch l, fiziki_shexs f
where  r.regnom = f.regnom
  and  r.regnom = l.registrac_nomer
  and  r.nerezident = 1
  and  substr(l.licsch,1,1) = 4
  and  l.date_open_licsch >= {BASTARIX}
order by r.name_regnom;


/* === Risk — İnsayder / əlaqəli tərəf müştərilər =========================== */
select r.regnom qeyd_no, r.name_regnom ad, r.inn_regnom voen, r.passport pasport,
       case when r.insider=1 then 'insayder' end insayder,
       case when r.svazanniy=1 then 'əlaqəli tərəf' end elaqeli
from   regnom r
where  r.insider = 1 or r.svazanniy = 1
order by r.name_regnom;


/* === Risk — Yüksək risk müştərilər + KYC yeniləmə vaxtı ==================== */
/* Mənbə: AML_riskler.sql — '15-04-2024' → {TARIX} */
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


/* === Risk — Benefisiar mülkiyyətçilər (təsisçi payı ≥ 10%) ================= */
/* Mənbə: AML_Benefisiar_hesabat.sql */
select h.qeydiyyat_tarixi q_tar, i.soyadi soyad, i.adi ad, i.ata_adi ata, i.fin fin,
       i.vetendashligi vetendasliq, i.olke olke, i.doguldugu_tarix d_tar,
       REGEXP_SUBSTR(i.seriyasi_ve_nomresi, '^[A-Z]+') seriya,
       REGEXP_SUBSTR(i.seriyasi_ve_nomresi, '[0-9]+') seriya_no,
       i.verilme_tarixi ver_tar, i.tesischinin_payi pay, i.regnom qeyd_no
from   imza_huquqi_olan_shexsler i, huquqi_shexs h
where  i.tesischinin_payi >= 10
  and  i.regnom = h.regnom
order by i.tesischinin_payi desc;


/* === Risk — 12 ay hər ay ən azı 2 əməliyyat olan VÖEN-lər ================= */
/* Mənbə: Butun_ve_aktiv_voenler...sql — il → {IL} */
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


/* === Risk — Terminal əməliyyatları (il üzrə) ============================== */
/* Mənbə: AML sorgular/Odenis_Terminal_emeliyyat_sorgusu.sql — 2025 → {IL} */
select count(distinct substr(dd.kredit,10,6)) musteri_say,
       count(dd.kredit) emel_say,
       sum(dd.summa_v_nacval) mebleg
from   arh_dd dd
where  EXTRACT(YEAR FROM dd.date_oper) = {IL}
  and  dd.debet in ('25019000000000300006','25019000000000300007')
  and  substr(dd.kredit,1,1) in (3,4);


/* === Risk — Ölkə üzrə müştərilər (detallı siyahı, aktiv hesab) ============ */
/* Mənbə: olke_kodlari_uzre_adlari.sql
   QEYD: ölkə üzrə SAY qrafiki (dashboard [BAR] widget) Risk_Panel_Widgetler.sql-dədir.
   Bu blok isə ad-ad DETALLI siyahıdır (kim, hansı ölkə). İkisi eyni deyil —
   birini dashboard kartı, digərini hesabat kimi saxla. */
select distinct l.registrac_nomer qeyd_no, r.name_regnom ad,
       case when l.countrycode='GEO' then 'Gürcüstan' else c.name end olke
from   licsch l, countrycode c, regnom r
where  l.registrac_nomer = r.regnom
  and  (l.date_close_licsch is null or l.date_close_licsch > {TARIX})
  and  l.countrycode = c.code
order by olke, r.name_regnom;
