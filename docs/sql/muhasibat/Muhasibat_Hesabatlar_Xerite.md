# Mühasibat / Maliyyə Hesabatları — Tam Xəritə (BMI köhnə desktop → FinNex dashboard)

> Mənbə: BMI köhnə WinForms desktop, `Maliyyə DP` menyusu → `BMI/BMI/Muhasibat/` qovluğu.
> 10 hesabat formasının SELECT-ləri araşdırıldı. Məqsəd: bu hesabatlardakı **dəyərli
> maliyyə göstəricilərini** FinNex-də ayrıca **Mühasibat Dashboard**-una çıxarmaq.
> Bütün Oracle sorğuları **yalnız SELECT** (CLAUDE.md qaydası) — köçürərkən `IOracleService`
> + **parametrli bind** (mövcud kodda tarixlər string konkatenasiya ilədir, düzəldilməli).

---

## 0. Menyu → Forma → Nə verir

| Forma | Hesabat | Nə verir |
|-------|---------|----------|
| `frm_Daily_Report.cs` | Günlük Hesabat | Bankın tam **balans strukturu** (aktiv/passiv/kapital/mənfəət), mədaxil-məxaric, kredit portfeli, gecikmə. İki rejim: tək tarix / "dünən vs bugün". |
| `LCR.cs` | Likvidlik Örtük Əmsalı (Basel III) | HQLA, çıxış axınları, qiymətli kağızlar, akkreditiv/qarantiya, kredit proqnoz axını. L1–L3 vərəqləri. |
| `frm_report_comments_Yeni.cs` | AMB prudensial "Daily Comments" | **Maturity ladder** (aktiv/öhdəlik müddət üzrə), likvidlik əmsalları, NPL/gecikmə, kredit təyinatı/seqment, depozit axını. |
| `frm_Dep.cs` | Günlük Depozit | Depozit portfeli (fiziki/hüquqi), **TOP-10 depozitor**, valyuta bölgü, mədaxil/məxaric, likvid aktivlər (C5–C10). |
| `frm_elaqeli_dep_cem.cs` | Əlaqəli Tərəf Depozitləri | Əlaqəli tərəf (təsisçi+işçi+şirkət) depozit cəmi və **xüsusi çəki %** (normativ). |
| `frm_valyuta_alis_satis.cs` | Valyuta Alış-Satış (NXVS) | Valyuta alış/satış həcmi, AZN qarşılığı, əməliyyat sayı, **açıq mövqe**. |
| `frm_reziden_ve_qeyri_rezident.cs` | Rezident/Qeyri-rezident | Hesab qalıqlarının rezidentlik üzrə bölgüsü. |
| `frm_XBBIS.cs` | AMB XBBIS (Aktiv/Öhdəlik) | Aktiv/öhdəlik dövriyyələri (giriş qalığı→dövriyyə→son qalıq), **müddət cədvəli** (3/6/9/12/18/24/24+ ay), ölkə üzrə. |
| `frm_ADD1.cs` | AMB ADD1 (kredit reyestri) | **Hər kredit üzrə ~90 sütun**: borclu, faiz, FIFD, ehtiyat, girov, restrukturizasiya, gecikmə. |
| `frm_iran_hesabat.cs` | İran IFRS Qeyd 13 | Kredit/depozit/faiz/ehtiyat/balansdankənar — **3 seqment** (fiziki/işçi/sahibkar). |

---

## 1. Ortaq data təməli (bütün hesabatların özəyi)

### Cədvəllər / view-lər
| Obyekt | Rol |
|--------|-----|
| `odb.arh_saldo_ls` | **Gündəlik hesab (balans) saldoları** — ƏSAS MƏNBƏ |
| `odb.arh_saldo_vbls` | Balansdankənar hesab qalıqları (`vbs` kodları) |
| `arh_dd` / `odb.arh_dd` | Mühasibat provodkaları (debet/kredit dövriyyəsi) |
| `arh_licschkre` | Kredit müqavilələri (əsas+faiz qalığı, faiz, ehtiyat, tip, tarixlər) |
| `licsch` / `odb.licsch` | Hesablar planı (ad, reg.nömrə, bağlanma tarixi) |
| `regnom` | Müştərilər (yurik/fizik/predprinimatel, biznesin_meyarlari) |
| `view_nacpogprokre_all` | Faiz hesablama/gecikmə view (lastoverduedate, nacpro/pogpro) |
| `graphpogkre` | Kredit ödəniş qrafiki (date_pog, summa_pog_kre, summa_pog_pro) |
| `srokpogprockre` | Kredit şərtləri (item_01=status, fifd, girov likvidliyi) |
| `girovun_bazar_deyeri` | Girov bazar dəyəri |
| `odb.arh_licsch_cb` | Qiymətli kağızlar portfeli |
| `arh_licschgar` | Akkreditiv/qarantiya |
| `odb.accounts`, `countrycode`, `tipkre`, `tipzal`, `naznackredita`, `index_otrasli` | Kataloqlar |

### Funksiyalar
- `odb.func_get_kurval(valyuta, tarix)` → valyuta kursu (AZN ekvivalentinə vurmaq üçün)
- `odb.tar_ferq360(t1, t2)` → 360-günlük tarix fərqi = **gecikmə günü (DPD)**
- `odb.ish_gun_cari1(tarix)` → cari iş günü

### Saldo sahələri (arh_saldo_ls)
- `saldo_vhd_nacval` — **günün əvvəlinə** qalıq (milli val.)
- `saldo_ish_nacval` — **günün sonuna** qalıq (milli val.) — depozit/passiv hesablarda MƏNFİ saxlanır (`-` ilə müsbətə çevrilir)
- `saldo_vhd_inval` — valyuta ilə qalıq
- `oboroti_debet_nacval` / `oboroti_kredit_nacval` — debet/kredit dövriyyəsi

### Hesab kodu lüğəti (məntiq açarı)
| Kod | Məna |
|-----|------|
| `100xx` | Kassa (nağd) |
| `10020/10080/10089` | Nağd xarici valyuta |
| `10050 / 10060` | Valyuta satış / alış |
| `11010 / 11020` | AMB/NOSTRO müxbir hesablar |
| `11710 / 15770` | HQLA (yüksək keyfiyyətli likvid aktivlər) |
| `14010/14014/14030/14034` | AMB-də vəsaitlər / banklararası |
| `15020 / 15025` | Cari likvid aktivlər |
| `15025/15225/15227` | Banklara tələbli depozitlər |
| `21115..21259` | **Kreditlər** (əsas `...5`, faiz `...7`, vaxtı keçmiş `...8`, VK faiz `...9`) |
| `35,36,38,39,40,41,49` | **Depozit bazası** (portfel məxrəci) |
| `40xx / 3x` | Hüquqi şəxs depozit/cari |
| `41xxx` | Fiziki şəxs depozit/cari |
| `35025/35026/35940/35020` | Xaric edilən texniki hesablar |
| valyuta = `substr(licsch,6,2)` | 00=AZN, 01=USD, 02=EUR, 03=RUB, 04=IRR, 05=AED |
| `substr(licsch,16,2)='91'` | İşçi/əmək haqqı hesabı |
| vbs `99530/99540/99550/99531` | Balansdankənar öhdəliklər (xətt/akkreditiv/qarantiya) |
| vbs `99300/99301` | Balansdankənar (faiz) |
| vbs `99740/99742/99743/99749` | Balansdankənar (İran hesabatı) |

### Müştəri seqmentləşməsi (təkrarlanan CASE)
- `tipzaloga in (10,18)` = **işçi krediti** (güzəştli)
- `tipkredita`: 1=hüquqi, 2=fiziki, 3=sahibkar
- `regnom.fizik / yurik / predprinimatel` = fiziki / hüquqi / sahibkar bayraqları
- `biznesin_meyarlari`: 1=mikro, 2=kiçik, 3=orta, 4=iri
- `index_otrasli`: 1901/1902=ipoteka/daşınmaz, 1903/1904=təmir, 1905=avto, 1906=məişət, 1907=kart

---

## 2. Açar SELECT-lər (təkrar istifadə olunan bloklar)

> Aşağıdakılar dashboard üçün ən dəyərli, təkrar istifadə oluna bilən sorğulardır.
> Tarixlər `<...>` — FinNex-də **bind parametr** olmalıdır.

### 2.1 Balans qalıqları (Daily Report / Report comments — hər şeyin əsası)
```sql
SELECT ar.date_oper AS tarix, ar.licsch AS hesab,
  CASE WHEN SUBSTR(ar.licsch,0,3) IN ('159','209','219','239','259')
       THEN SUBSTR(ar.licsch,16,2) ELSE SUBSTR(ar.licsch,6,2) END AS valyuta,
  ar.saldo_ish_nacval AS qaliq
FROM odb.arh_saldo_ls ar, licsch ch
WHERE ar.date_oper = TO_DATE('<tarix>','dd/mm/yyyy')
  AND ch.licsch = ar.licsch
  AND (ch.date_close_licsch IS NULL OR ar.date_oper <= ch.date_close_licsch);
```
→ C# tərəfdə hesab kodu prefiksinə görə balans maddələrinə (aktiv/passiv/kapital) qruplaşdırılır.

### 2.2 Likvid aktivlər / HQLA (Report comments `likvid`)
```sql
select l.licsch, substr(l.licsch,6,2), round(sum(l.saldo_ish_nacval/1000),2)
from odb.arh_saldo_ls l
where l.date_oper = TO_DATE('<tarix>','dd/mm/yyyy')
  and substr(l.licsch,1,5) in ('15770','11710')
group by l.licsch;
```

### 2.3 Ani likvidlik / LCR komponentləri (LCR + Report comments)
- **Pay (HQLA)**: kassa (100), AMB müxbir (11010/11020), banklararası (14010+), q.kağız, 15770/11710.
- **Cari likvid aktivlərə %25 haircut**: `discountFactor = 0.75` (15020/15025).
- **Məxrəc (net cash outflow)**: depozit hesabları (35/38/39/40/41) + balansdankənar.
- **LCR faizi Exceldə hesablanır** (kod yalnız aralıq məbləğləri yığır) — dashboard-da biz özümüz hesablayacağıq: `HQLA / net_outflow`.

### 2.4 Kredit portfeli (kredit tipi üzrə — Daily Report)
```sql
SELECT al.date_oper, al.licschkre, substr(al.licschkre,6,2), tk.code, tk.name,
       al.summa, al.summa_19,
       (al.summa+al.summa_19)*ROUND(odb.func_get_kurval(substr(al.licschkre,6,2),al.date_oper),6) ekv
FROM arh_licschkre al, tipkre tk
WHERE al.tipkredita = tk.code
  AND (al.date_close is null OR al.date_close > TO_DATE('<tarix>','dd/mm/yyyy'))
  AND al.date_oper = TO_DATE('<tarix>','dd/mm/yyyy');
```

### 2.5 Kredit gecikmə/NPL (Report comments `qaliqlar_30_90`)
DPD = `odb.tar_ferq360(x.date_oper, nvl(x.lastoverduedate, x.date_oper))`; seqment (mikro/kiçik/orta/iri),
təyinat (ipoteka/avto/kart...), VK (`summa_19>0`), restrukturizasiya (`date_restructure`).

### 2.6 Maturity ladder / ödəniş cədvəli (XBBIS `cedvel`)
```sql
-- graphpogkre üzrə 3/6/9/12/18/24/24+ ay intervallarında əsas (summa_pog_kre) və faiz (summa_pog_pro):
sum(case when g.date_pog between to_date('<son>','dd/mm/yyyy')
          and add_months(to_date('<son>','dd/mm/yyyy'),3) then g.summa_pog_kre/1000 else 0 end) esas3,
-- ... 6/9/12/18/24 eyni məntiqlə ...
sum(case when g.date_pog > add_months(to_date('<son>','dd/mm/yyyy'),24) then g.summa_pog_kre/1000 else 0 end) esas25
from arh_licschkre l, graphpogkre g, licsch lc
where l.date_oper = to_date('<son>','dd/mm/yyyy') and g.licschkre=l.licschkre and g.subschkre=l.subschkre ...
```
→ Gələcək cash-flow proqnozu / likvidlik gap.

### 2.7 Depozit portfeli + TOP-10 (frm_Dep)
```sql
-- Hüquqi TOP-10 (fiziki: substr(licsch,1,2)='41'):
select b.registrac_nomer qeyd, r.name_regnom,
  sum(-round(l.saldo_ish_nacval/1000,2)) top,
  SUM(CASE WHEN SUBSTR(l.licsch,6,2)='00' THEN ROUND(-l.saldo_ish_nacval/1000,2) ELSE 0 END) azn,
  SUM(CASE WHEN SUBSTR(l.licsch,6,2)='01' THEN ROUND(-l.saldo_ish_nacval/1000,2) ELSE 0 END) usd,
  SUM(CASE WHEN SUBSTR(l.licsch,6,2) NOT IN('00','01') THEN ROUND(-l.saldo_ish_nacval/1000,2) ELSE 0 END) diger
from odb.arh_saldo_ls l, regnom r, licsch b
where l.date_oper = TO_DATE('<tarix>','dd/mm/yyyy') and b.licsch=l.licsch
  and (substr(l.licsch,1,2)='40' or substr(l.licsch,1,1)='3')
  and substr(l.licsch,1,5) not in (35020,35025,35026,35940) and substr(l.licsch,10,6)<>'000004'
  and r.regnom=b.registrac_nomer
group by b.registrac_nomer, r.name_regnom
order by sum(l.saldo_ish_nacval/1000) FETCH FIRST 10 ROWS ONLY;
```

### 2.8 Əlaqəli tərəf xüsusi çəki (frm_elaqeli_dep_cem)
Ümumi depozit portfeli (`top_qal`) = `substr(licsch,0,2) in (35,36,38,39,40,41,49)` cəmi.
Xüsusi çəki = (şirkət + təsisçi `imza_huquqi_olan_shexsler` + işçi `fiziki_shexs.ish_yerinin_adi`) / `top_qal`.

### 2.9 Valyuta alış-satış (frm_valyuta_alis_satis)
Alış = debet `10060` ← kredit `10050`; Satış = debet `10050` ← kredit `10060`.
`summa_v_inval` = valyuta məbləği, `summa_v_inval*kurs_valuti` = AZN qarşılığı. Spred = orta satış − orta alış kursu.

### 2.10 Rezident/qeyri-rezident (frm_reziden_ve_qeyri_rezident)
```sql
case when (substr(s.licsch,0,3)='409'
  and substr(REGEXP_SUBSTR(l.name_licsch,'\(([^()]*)\)\s*$',1,1,NULL,1),5,1)='5')
  or SUBSTR(s.licsch,0,5)='45029' then 'qr' else 'r' end tip
```

### 2.11 Kredit reyestri detallı (ADD1 — hər kredit üçün)
Borclu, val, faiz (`procstavkre`), FIFD (`srokpogprockre.fifd`, boşdursa IRR ilə hesablanır),
ilkin məbləğ (`summakre*kurs`), qalıq (`(summa+summa_19)*kurs`), ehtiyat (`procstavrez`/`procstavrez_19`),
gecikmə (əsas/faiz), restrukturizasiya, girov (`girovun_bazar_deyeri`, növ `tipzaloga`), aylıq annuitet.

---

## 3. Dəyərli göstəricilərin mövzu üzrə xəritəsi

| Mövzu | Göstəricilər | Mənbə forma |
|-------|--------------|-------------|
| **Balans** | Ümumi aktiv, öhdəlik, kapital, cari il mənfəəti, kapital/aktiv əmsalı | Daily Report |
| **Likvidlik** | LCR, ani likvidlik (AZN/USD/EUR), HQLA, maturity ladder, likvidlik gap | LCR, Report comments, XBBIS |
| **Depozitlər** | Portfel, fiziki/hüquqi, TOP-10, əlaqəli tərəf %, mədaxil/məxaric, valyuta | Dep, əlaqəli dep |
| **Kredit** | Portfel qalığı, seqment/təyinat, NPL aging, ehtiyat, restrukt, girov LTV, FIFD | ADD1, Report comments, İran |
| **Valyuta** | Alış/satış həcmi, spred/mənfəət, açıq mövqe | Valyuta alış-satış |
| **Rezidentlik** | Rezident/qeyri-rezident qalıq və pay | Rezident/q-rezident |
| **Balansdankənar** | Akkreditiv, qarantiya, kredit xətləri | LCR, Report comments, İran |

---

## 4. Təklif olunan Mühasibat Dashboard (bölmələr / tab-lar)

### Tab 1 — Balans İcmalı
- KPI: Ümumi Aktiv, Ümumi Öhdəlik, Kapital, Cari il mənfəəti, Kapital/Aktiv əmsalı (svetofor) — hamısı dünənlə müqayisə oxu ilə.
- Qrafik: Aktiv/Passiv strukturu (100% stacked bar), valyuta strukturu (donut).

### Tab 2 — Likvidlik
- KPI: LCR əmsalı (gauge, ≥100% hədd), ani likvidlik AZN/USD/EUR, HQLA cəmi, xalis nağd axını.
- Qrafik: **Maturity ladder** (aktiv vs öhdəlik, müddət qutuları 0-30/30-90/90-180/180-360/360+), HQLA strukturu (donut), likvid aktivlər C5–C10 (stacked bar).

### Tab 3 — Depozitlər
- KPI: Ümumi portfel, fiziki vs hüquqi, əlaqəli tərəf xüsusi çəki (svetofor — normativ), likvid/depozit nisbəti.
- Qrafik: TOP-10 depozitor (üfüqi bar), valyuta bölgü (donut), mədaxil/məxaric waterfall, əlaqəli tərəf çəkisinin trendi (xətt + hədd).

### Tab 4 — Kredit Portfeli
- KPI: Ümumi portfel qalığı, aktiv müqavilə sayı, orta faiz, orta FIFD, ümumi ehtiyat + ehtiyat/portfel %.
- Qrafik: Seqment üzrə bölgü (fiziki/işçi/sahibkar — donut), təyinat üzrə (ipoteka/avto/... — bar), **NPL aging** (0/1-30/31-90/90+ — bar), maturity ladder (cash-flow proqnozu), restrukt sayı/qalıq, girov LTV.

### Tab 5 — Valyuta
- KPI: Ümumi alış, ümumi satış (AZN), açıq mövqe, əməliyyat sayı, orta alış vs satış kursu (spred).
- Qrafik: Gündəlik alış vs satış (line, USD/EUR ayrı), valyuta üzrə müqayisə (qruplu bar), açıq mövqe waterfall.

### Tab 6 — Rezidentlik & Balansdankənar
- KPI: Rezident cəmi, qeyri-rezident cəmi, qeyri-rezident payı %.
- Qrafik: Rezident/qeyri-rezident (donut), balansdankənar öhdəliklər (akkreditiv/qarantiya/xətt — stacked bar).

### Ümumi filtrlər
- Hesabat tarixi (tək gün / dünən müqayisəsi / dövr aralığı)
- Valyuta (AZN/USD/EUR/...)
- Müştəri seqmenti (fiziki/hüquqi/sahibkar)
- **Vahid diqqəti**: bəzi hesabatlar `/1000` (min AZN), bəziləri tam manat — normallaşdırılmalı.

---

## 5. Köçürmə üçün texniki qeydlər (CLAUDE.md uyğun)
1. **Oracle yalnız SELECT** — bütün sorğular `IOracleService` vasitəsilə.
2. **Parametrli bind** — mövcud kodda tarixlər string konkatenasiya ilədir (SQL injection); FinNex-də `OracleCommand` bind.
3. **Hardcode tarix qüsurları** (köçürərkən düzəlt): `frm_XBBIS.dovriyye_artma` → `'28-06-2024'`; `frm_ADD1.gecgun6` → `'30-09-2024'`.
4. **Balans maddələrinə bölgü** hazırda C# LINQ-də hesab kodu prefiksləri ilədir — bu təsnifatı konfiqurasiya/cədvələ çıxarmaq olar (Risk dashboard-dakı OracleSorgular modeli kimi).
5. Bütün valyuta məbləğləri `func_get_kurval` ilə AZN ekvivalentinə çevrilir.
