# AML → Hesab üzrə sorğu — köhnə/yeni Excel şablonu fərqi

**Tarix:** 19.08.2026 · **Status:** analiz + qərarlar hazırdır, SQL gözlənilir

| | Fayl | Vərəq | Ölçü |
|---|---|---|---|
| Köhnə | `AML_Hesab.xlsx` (BMI: `Fayllar\AML\Exceller\`) | `Sheet1` | 37 sütun (A…AK) |
| Yeni | `vahidhesabchixarishi1.xlsx` | `Sheet2` | **47 sütun** (A…AU) |

Mənbə kod: `BMI/BMI/AML/frmhesabsorgu.cs` → `exceleat2()`.

---

## 1. Şapka (başlıq hissəsi) fərqləri

| Xana | Köhnə | Yeni |
|---|---|---|
| `D1` / VOEN | `D1:E1` = «Adı», `F1:G1` = «VOEN-i» | `D1:F1` = «Adı», **`G1:H1`** = «VOEN-i» |
| `D2` / VOEN dəyəri | Şablona **sabit yazılıb**: «Bank Melli Iran Bakı filialı» / `1300036291` | `D2:F2` və `G2:H2` **boşdur** → kod doldurmalıdır |
| `D5` / `E5` | başlama / bitmə tarixi | eyni |
| `D6` / `E6` | başlanğıc / son saldo | eyni |
| `D7` | çıxarış verilən hesab | eyni |
| **`A8:C8`** | — | **YENİ: «Hesabın valyutası»** → dəyər `D8`-ə yazılmalıdır |

> ⚠️ Koddakı `valkod` dəyişəni (hesab №-nin 6–7-ci simvolundan AZN/USD/EURO/RUBL/IRR/BƏƏ
> hesablayır) köhnə versiyada **hesablanır, amma heç yerə yazılmır** — ölü koddur.
> Yeni şablonda onun yeri var (`A8`), yəni nəhayət işlənəcək.

---

## 2. Sütun-sütun müqayisə

### 2.1 Baş hissə — 4 → 8 sütun

| Köhnə | Yeni | Başlıq | Vəziyyət |
|---|---|---|---|
| A | A | İcraya qəbul | eyni |
| B | B | Faktiki icra | eyni |
| C | C | ~~Əməliyyatın nömrəsi~~ → **Daxili istinad** | **ad dəyişib** |
| — | **D** | **Xarici istinad** | 🆕 |
| — | **E** | **Əməliyyatın növü** (nağd / qeyri-nağd) | 🆕 |
| D | F | Çatdırılma kanalı | yerini dəyişdi |
| — | **G** | **Ödəniş sisteminin növü** | 🆕 |
| — | **H** | **Alt növü** | 🆕 |

### 2.2 GÖNDƏRƏN TƏRƏF — 12 → 14 sütun

| Köhnə | Yeni | Başlıq | Vəziyyət |
|---|---|---|---|
| E | I | Ad | |
| F | J | VÖEN | |
| G | K | FİN | |
| H | L | Hesabın növü | |
| I | M | Hesab nömrəsi | |
| J | N | Bankın adı | |
| K | O | Bank filialının adı | |
| — | **P** | **Bankın VÖEN-i** | 🆕 |
| — | **Q** | **Bankın BİC kodu** | 🆕 |
| P | R | Müxbir bankın BİC kodu | **sona idi, indi ortada** |
| L | S | Ölkə | |
| M | T | Hesabın valyuta kodu | |
| N | U | Hesabın bağlı olduğu PAN və ya FİN | |
| O | V | MCC kod | |

### 2.3 ALAN TƏRƏF — 12 → 14 sütun (göndərənlə eyni quruluş)

| Köhnə | Yeni | Başlıq |
|---|---|---|
| Q | W | Ad |
| R | X | VÖEN |
| S | Y | FİN |
| T | Z | Hesabın növü |
| U | AA | Hesab nömrəsi |
| V | AB | Bankın adı |
| W | AC | Bank filialının adı |
| — | **AD** | **Bankın VÖEN-i** 🆕 |
| — | **AE** | **Bankın BİC kodu** 🆕 |
| AB | AF | Müxbir bankın BİC kodu |
| X | AG | Ölkə |
| Y | AH | Hesabın valyuta kodu |
| Z | AI | Hesabın bağlı olduğu PAN və ya FİN |
| AA | AJ | MCC kod |

### 2.4 Əməliyyat + Bank — 9 → 11 sütun

| Köhnə | Yeni | Başlıq | Vəziyyət |
|---|---|---|---|
| AC | AK | Mədaxil | |
| AD | AL | Məxaric | |
| AE | AM | Valyuta kodu | |
| AF | AN | Mədaxil (AZN ilə) | |
| AG | AO | Məxaric (AZN ilə) | |
| AH | AP | Əməliyyat (təyinat) | |
| — | **AQ** | **Xeyrinə ödənilən şəxsin/müəssisənin adı** | 🆕 |
| — | **AR** | **FİN/VÖEN** | 🆕 |
| AI | AS | Kommunal ödəniş kodu, mobil nömrə və s. | |
| AJ | AT | DT (hesablar planı) | |
| AK | AU | KT (hesablar planı) | |

---

## 3. Yeni sütunlar və mənbələri

Yeni şablonun **12-ci sətrində** analitikin qeydləri var — mənbə birbaşa göstərilib:

| Sütun | Başlıq | Mənbə (şablonun 12-ci sətrindən) |
|---|---|---|
| **C** | Daxili istinad | `arxiv / balans sənədləri / RC` |
| **D** | Xarici istinad | `Ödənişlər/(Milli valyutada ödəmələr · Milli valyutada mədaxil · Xarici valyutada ödəmələr · Xarici valyutada mədaxil) / NAME.ID` |
| **E** | Əməliyyatın növü | **hesablanan**: DT/KT hissəsi kassa **və ya** `25019` → **nağd**; deyilsə → **qeyri-nağd** |
| **G** | Ödəniş sisteminin növü | `DOC_VNESH_NACVAL.PLAT_SYSTEM` (milli ödəmə)<br>`DOC_VNESH_POSTULP.PLAT_SYSTEM` (milli mədaxil)<br>`DOC_VNESH_INVAL.PLAT_SYSTEM` (xarici ödəmə)<br>`DOC_VNESH_SWIFT.PLAT_SYSTEM` (xarici mədaxil) |
| **H** | Alt növü | qeyd yoxdur — **dəqiqləşdirilməlidir** |
| **P / AD** | Bankın VÖEN-i | qeyd yoxdur — **dəqiqləşdirilməlidir** |
| **Q / AE** | Bankın BİC kodu | qeyd yoxdur — **dəqiqləşdirilməlidir** |
| **AQ** | Xeyrinə ödənilən şəxsin/müəssisənin adı | milli mədaxil: `DOC_VNESH_POST1.KREDIT_NAME`<br>xarici mədaxil: `DOC_VNESH_SWIFT1.BENEFICIARY_BANK_BIC` |
| **AR** | FİN/VÖEN | milli mədaxil: `DOC_VNESH_POST1.KREDIT_INN`<br>digərləri: qeyd yoxdur |

> `AQ`/`AR` üçün qeyddə **yalnız mədaxil** sətirləri doldurulub; ödəmə (məxaric)
> tərəfi boş buraxılıb. Bu, qəsdəndirmi yoxsa unudulub — soruşulmalıdır.

---

## 4. QƏRARLAR (19.08.2026 — istifadəçi cavabladı)

| # | Sual | Cavab |
|---|---|---|
| 1 | Data hansı sətirdən? | **12-ci sətirdən.** Yenidəki 12-ci sətir qeydləri yalnız «necə yanaşacağıq» izahıdır — testdir, şablondan silinəcək. `startRow = 12` **dəyişmir**. |
| 2 | `H` «Alt növü» | **Boş qalacaq.** |
| 3 | Bankın VÖEN-i / BİC kodu | **`odb.muxbir_hesab`** cədvəlindən — aşağıda §4.1. |
| 4 | `AQ` / `AR` ödəmə tərəfi | **Olduğu kimi** — yalnız şablondakı qeyddə göstərilən hallar doldurulur, qalanı boş. |
| 5 | Şapka `D2` / `G2` (bank adı + VÖEN) | **Kod yazacaq** (şablonda sabit deyil). |
| 6 | Vərəq adı | **`Hesab çıxarışı`** — köhnə qayda saxlanılır. |
| 7 | `C` Daxili istinad | **`arh_dd.RECNUM`.** ⚠️ BMI-nin `axtar()` sorğusu **onsuz da** `t.recnum san_nom` seçir — dəyişiklik lazım deyil. (Köhnə `gonder()` metodu `nomer_docum` işlədirdi, amma o metod çağırılmır.) |
| 8 | `D` Xarici istinad | `doc_vnesh_*.ID` — `vd` alt sorğusuna `v.id` əlavə edildi. **`arh_dd.ID_VD` İŞLƏDİLMİR** (18.08.2026 sorğusunda 20 sətrin hamısında boş idi); bağlantı BMI-nin mövcud kompozit açarı ilə qalır: `date_oper + debet/kredit + summa_v_nacval`. |
| 9 | Bankın VÖEN-i / BİC-i — daxili sətir | Daxili əməliyyatda (`id_vd is null`) bank onsuz da **bizik**, ona görə sabit yazılır: VÖEN **`1300036291`**, BİC **`MELIAZ22`** (şapkadakı `D2`/`G2` ilə eyni mənbə). Xarici sətirdə `muxbir_hesab`-dan tapılır. |

### 4.2 Nağd / qeyri-nağd (E sütunu) — kassa hesabları

«Kassa» = BMI → Hesablar ekranındakı **ilk 6 hesab**:

| Hesab | Valyuta |
|---|---|
| `10010000000000100000` | AZN |
| `10020010000000100000` | USD |
| `10020020000000100000` | EUR |
| `10020030000000100000` | RUB |
| `10020040000000100000` | İRR |
| `10020050000000100000` | AED |

Qayda: DT **və ya** KT tərəfi bu 6 hesabdan biridirsə, yaxud `25019…` ilə başlayırsa
→ **Nağd**; əks halda → **Qeyri-nağd**.

> Şərt `substr(...,1,5) in ('10010','10020')` kimi **yazılmadı** — o, kassadan başqa
> `100xx`/`10020xx` hesablarını da tutardı. İstifadəçi «ilk 6 hesab» dedi, ona görə
> tam bərabərlik siyahısı işlədilir.

---

## 7. Hazır sorğular — İKİ VARİANT (formadakı radio düymə)

BMI formasında «Sorğu:» seçimi var və **iki tam ayrı SQL** işə salır. FinNex-də
də iki sorğu saxlanmalıdır — birləşdirmək olmaz, nəticələr fərqlidir:

| Radio | BMI metodu | Fayl |
|---|---|---|
| **Fiziki şəxs** | `axtar()` | `docs/sql/aml/Hesab_Uzre_Sorgu_Yeni.sql` |
| **Sahibkar / hüquqi şəxs VÖEN** | `axtarhuquqi()` | `docs/sql/aml/Hesab_Uzre_Sorgu_Yeni_Huquqi.sql` |

Hər ikisi BMI sorğusunun üzərinə **eyni 10 yeni sütunu** əlavə edir; yalnız
başdakı `prm` bloku dəyişdirilir (hesab № + iki tarix).

### 7.1 İki variantın fərqləri (QƏSDƏN saxlanılıb)

| # | Fiziki (`axtar`) | Hüquqi (`axtarhuquqi`) |
|---|---|---|
| 1 | Göndərənin VÖEN-i (J) **boş** `'   '` | `f.inn_regnom` |
| 2 | Alanın VÖEN-i (X) boş / `h.inn_regnom` | `f.inn_regnom` / `h.inn_regnom` |
| 3 | Qarşı tərəf şərti **KREDİT**-ə baxır (`substr(t.kredit,1,1)<>'4'`) | **DEBET**-ə baxır (`substr(t.debet,1,3) in (100,150)`) |
| 4 | Mədaxil qolunda `(100,150,350)` | `(100,150)` — **350 yoxdur** |
| 5 | Alan bankı `vd.ben_bank` / `'diger'` | `vd.filial_name` |
| 6 | Mədaxildə göndərənin adı `vd.ben_ad` | `vd.emit_name` |
| 7 | `postupl` (mədaxil qolu): `substr(v.kredit,9,20)` + `lpad(...,28)` | `odb.right(v.kredit,20)` + **lpad YOXDUR** |
| 8 | `swift` (mədaxil qolu): `regnom` **6** simvol | `regnom` **5** simvol |
| 9 | `nacval` (mədaxil qolu): `odb.mfo` join yoxdur | eyni — yoxdur |
| 10 | `icra_tarix` = `to_char(...)` mətn | `t.date_oper` tarix |

Bunlar BMI-də illərdir belədir; «eyniləşdirmək» nəticəni dəyişər. Toxunmadan
köçürülüb.

**HƏLƏ İCRA EDİLMƏYİB.** Aşağıdakı sütun adları istifadəçinin qeydi/ekran
görüntüsü əsasında qəbul edilib və bazada yoxlanmalıdır — biri yoxdursa
ORA-00904 verəcək:

| Cədvəl | Gözlənilən sütun |
|---|---|
| `doc_vnesh_inval` | `ID`, `PLAT_SYSTEM` |
| `doc_vnesh_nacval` | `ID`, `PLAT_SYSTEM` |
| `doc_vnesh_postupl` | `ID`, `PLAT_SYSTEM`, `KREDIT_INN` |
| `doc_vnesh_swift` | `ID`, `PLAT_SYSTEM` |
| `muxbir_hesab` | `VOEN`, `SWIFT_KODU`, `KOD` |

### ⚠️ ORA-00932 — UNION-da NUMBER/CHAR toqquşması (19.08.2026, ilk icrada çıxdı)

`muxbir_hesab.VOEN` (və `doc_vnesh_*.ID`) **NUMBER**-dır. Yeni sütunların bir
qolunda sabit mətn (`'1300036291'`), o biri qolunda NUMBER sütunu yazılmışdı →
`UNION ALL` tipi birinci qoldan götürür və ikincidə
**`ORA-00932: inconsistent datatypes: expected NUMBER got CHAR`** verir.

Xəta **sətir nömrəsi göstərmir** və sorğunun harasında olduğunu demir — 4 qollu
`vd` alt sorğusunda hansı sütun olduğunu tapmaq üçün qolları bir-bir tutuşdurmaq
lazım gəlir.

**Qayda:** UNION-un hər qolunda eyni sütun **eyni tipdə** olmalıdır. Yeni sütun
əlavə edəndə, bir qolda sabit mətn yazırsansa, o biri qollarda da `to_char(...)`
ilə mətnə çevir. Bu sorğuda `vd_id`, `plat_system`, `gon_bank_voen`,
`alan_bank_voen`, `gon_bank_bic2`, `alan_bank_bic2`, `xeyrine_fin` — hamısı
`to_char` ilə mətnə salınıb (Excel-ə onsuz da mətn kimi düşür).

`muxbir_hesab` **join ilə yox, skalyar alt sorğu ilə** oxunur
(`(select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = …)`) —
belədə §-dəki «üçə qatlama» tələsi ümumiyyətlə yarana bilmir, `distinct`
düzgün işləyib-işləmədiyini yoxlamağa ehtiyac qalmır.

Sütun sayı: hər iki UNION qolunda **50** (47 Excel sütunu + 3 köməkçi:
`dbtam`, `krtam`, `id_vd` — bunlar Excel-ə yazılmır, C#-da çatdırılma kanalı
üçün lazımdır).

### 4.1 `odb.muxbir_hesab` — bank məlumatının mənbəyi

Bazadan təsdiqləndi (19.08.2026):

```sql
select * from muxbir_hesab r where r.swift_kodu = 'BRESAZ22';
```

| TESHKILATIN_ADI | KOD | MUXBIR_HESAB | VALYUTA_KODU | SWIFT_KODU | VOEN |
|---|---|---|---|---|---|
| Bank Respublika ASC | 505668 | AZ80NABZ0135010000000014944 | AZN | BRESAZ22 | 9900001901 |
| Bank Respublika ASC | 505668 | AZ02NABZ0135020000000014840 | USD | BRESAZ22 | 9900001901 |
| Bank Respublika ASC | 505668 | AZ28NABZ0135020000000014954 | EUR | BRESAZ22 | 9900001901 |

**Bağlantı iki cür mümkündür** — `doc_vnesh_postupl`-dakı sahələrlə:

| Şablon sütunu | Mənbə |
|---|---|
| Bankın adı | `muxbir_hesab.teshkilatin_adi` |
| **Bankın VÖEN-i** (P / AD) | `muxbir_hesab.voen` |
| **Bankın BİC kodu** (Q / AE) | `doc_vnesh_postupl.bic_debet` / `bic_kredit` (= `muxbir_hesab.swift_kodu`) |
| bağlantı açarı | `muxbir_hesab.kod = mfo_debet` **və ya** `muxbir_hesab.swift_kodu = bic_debet` |

Nümunə sətir (`doc_vnesh_postupl`, 18-08-2026):
`MFO_DEBET = 505668`, `BIC_DEBET = 'BRESAZ22'` → hər ikisi eyni banka aparır.

---

> ## ⚠️ TƏLƏ — `muxbir_hesab` SƏTİRLƏRİ ÜÇƏ QATLAYIR
>
> `muxbir_hesab`-da **hər valyuta üçün ayrıca sətir** var (yuxarıdakı nümunədə
> AZN/USD/EUR — 3 sətir). Adi `join` ilə bağlasaq **hər əməliyyat sətri 3 dəfə**
> təkrarlanar: 100 əməliyyatlıq çıxarış 300 sətir olar.
>
> Bank **adı və VÖEN-i valyutadan asılı deyil** — üç sətirdə də eynidir.
> Ona görə bağlantı **təkrarsız** olmalıdır:
>
> ```sql
> -- ✅ DÜZGÜN — valyutaya görə təkrarı əvvəlcədən yığ
> left join (select distinct kod, swift_kodu, teshkilatin_adi, voen
>              from odb.muxbir_hesab) mh
>        on mh.swift_kodu = t.bic_debet
> ```
>
> ```sql
> -- ❌ SƏHV — hər sətir 3 dəfə çıxar
> left join odb.muxbir_hesab mh on mh.swift_kodu = t.bic_debet
> ```
>
> Yoxlama: sorğunu yazandan sonra `count(*)`-ı `muxbir_hesab` join-suz nəticə
> ilə tutuşdur — bərabər olmalıdır. Bərabər deyilsə `distinct` işləmir və ya
> açar unikal deyil.
>
> **NİYƏ BU QEYD BURADADIR:** eyni tip səhv layihədə artıq yaşanıb — say və
> siyahı bir-birindən ayrı düşəndə heç bir xəta çıxmır, sadəcə rəqəm yalan olur
> (CLAUDE.md — «Kredit Hesabatları» və «Davamiyyət KPI» bölmələri).

## 5. Koddakı çevirmələr — SQL-dən GƏLMİR, C#-da hesablanır

Bunlar köhnə versiyada Excel-ə yazılmazdan **əvvəl** tətbiq olunur
(`frmhesabsorgu.cs`). Yeni versiyada da lazımdırsa, SQL-ə köçürmək və ya
kodda saxlamaq qərarı verilməlidir:

**«Çatdırılma kanalı»** (köhnə D, yeni F) — müxbir hesab koduna görə:

| Şərt (hesab kodu) | Yazılan dəyər |
|---|---|
| `25010000000000300000` | ATM |
| `25010000000000300000` / `25020010000000300002` / `25020020000000300002` | POS terminal |
| `25019…` | Ödəniş terminalı |
| `25019000000000300006` | Ödəniş terminalı (+ ölkə = AZE) |
| `25052000040000300000` | İŞÇİLƏRƏ AVANS MÜKAFAT |
| `11010000020000200000` / `…030000200000` / `…050000200000` | Digər maliyyə institutu vasitəsi |
| ilk 5 simvol: `35025` / `35020` / `15025` / `15020` | Digər maliyyə institutu vasitəsi |
| ilk 5 simvol: `10010` / `10020` | CAS |

**Qarşı tərəfin adı / FİN / VÖEN** — ayrıca sorğu ilə tapılır:

```sql
select t.licsch, t.name_licsch, r.pincode,
       case when r.fizik = 1 then '' else t.inn_licsch end voen
  from odb.licsch t, odb.regnom r
 where substr(t.licsch, 11, 5) = substr(r.regnom, 2.5)
   and t.date_close_licsch is null
 order by t.licsch
```

Nəticə C#-da sətir-sətir uyğunlaşdırılıb 17/19-cu sütunlara yazılır.

**Valyuta kodu** — hesab №-nin 6–7-ci simvolundan:
`00`→AZN, `01`→USD, `02`→EURO, `03`→RUBL, `04`→IRR, `05`→BƏƏ

---

## 6. Formanın giriş parametrləri (dəyişməyib)

| Sahə | Nə |
|---|---|
| Hesab № | `textBox1` |
| Başlama / bitmə tarixi | `textBox2` / `textBox3` |
| Sorğu növü | `radioButton1` = **Fiziki şəxs**, `radioButton2` = **Sahibkar/hüquqi şəxs VÖEN** |

İki radio düymə **iki fərqli SQL variantı** işə salır (`frmhesabsorgu.cs:335` və `:351`).
