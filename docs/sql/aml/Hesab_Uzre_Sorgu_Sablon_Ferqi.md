# AML → Hesab üzrə sorğu — köhnə/yeni Excel şablonu fərqi

**Tarix:** 19.08.2026 · **Status:** analiz hazırdır, SQL gözlənilir

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

## 4. Cavab gözləyən suallar

1. **Data hansı sətirdən başlayır?** Köhnədə `startRow = 12`. Yenidə 12-ci sətir
   **analitik qeydləri** ilə doludur. Data 13-dən başlayır, yoxsa 12-ci sətir
   silinməlidir?
2. **`H` — «Alt növü»** nədir? Hansı sahədən gəlir, hansı dəyərləri alır?
3. **Bankın VÖEN-i / BİC kodu** (P, Q, AD, AE) hansı cədvəldən? BMI-də bank
   məlumatı `odb.mfo`-dadırmı?
4. **`AQ` / `AR` ödəmə (məxaric) tərəfi** qəsdən boşdur, yoxsa doldurulmalıdır?
5. **Şapka `D2` / `G2`** (bank adı + VÖEN) artıq şablonda sabit deyil — kod
   yazmalıdır, yoxsa yenə şablona yazılacaq?
6. **Vərəq adı:** köhnə kod `worksheet.Name = "Hesab çıxarışı"` qoyurdu.
   Yenidə vərəq `Sheet2` adlanır — eyni qayda saxlanılsınmı?

---

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
