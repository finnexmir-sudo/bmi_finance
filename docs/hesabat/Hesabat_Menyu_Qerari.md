# Hesabatların Yerləşdirilməsi — QƏRAR

**Tarix:** 19.08.2026 · **Status:** ✅ QƏRAR VERİLDİ (istifadəçi seçdi) · kod hələ yazılmayıb

---

## Sual

BMI-dəki hesabatları FinNex-ə köçürərkən onlara giriş necə olsun?

- **(A)** Hər departamentin öz səhifəsinin içində «Hesabatlar» bölməsi
- **(B)** Yuxarı menyuda ayrıca «Hesabatlar» tab-ı → içində departament adları →
  onların içində hesabatların adları

## Qərar: **(B)** — ayrıca «Hesabatlar» tab-ı

---

## Niyə

BMI-də hesabatlar departament menyularının **içinə** paylanıb və nəticə budur:

| BMI menyusu | Hesabat/sorğu sayı |
|---|---|
| Maliyyə DP → Hesabatlar | 11 |
| Sorgular (mühasibat) | ~12 |
| AML | ~9 |
| Kredit DP | ~6 |
| PİD | ~6 |
| Ümumi sorğular | ~5 |
| **Cəmi** | **~49** |

49 hesabat 6 ayrı menyuya səpələnib. «Hansı menyuda idi?» real problemdir:
*Head office monthly report* PİD-in altındadır, *Baş İdarə aylıq hesabat* isə
AML-in — istifadəçi üçün ikisi də «aylıq hesabat»dır.

(A) variantı eyni səhvi WinForms əvəzinə brauzerdə təkrarlayardı.

---

## Quruluş

**Departament → Dövrilik → Hesabat.** Dövrilik qatı BMI-nin
`Maliyyə DP → Hesabatlar → Günlük / Aylıq / Rüblük` quruluşundan götürülüb və
doğrudur: mühasib səhər «bu gün nə göndərməliyəm?» deyə düşünür, «bu hansı
departamentin hesabatıdır» deyə yox.

```
Hesabatlar  (yuxarı tab)
├── Maliyyə
│   ├── Günlük  → Daily Report · Daily comments · Depozitlər · Daily report yeni
│   ├── Aylıq   → LCR · NXVS · ADD1 · İran · Əlaqəli depozitlər · Rezident/q-rezident
│   └── Rüblük  → XBBIS
├── Kredit · AML · PİD · HR · Mühasibatlıq
```

**İki giriş nöqtəsi, BİR mühərrik:** departament səhifəsində «Hesabatlar →»
qısayolu qalır, amma öz siyahısını QURMUR — eyni səhifəyə süzgəclə keçir
(`/Hesabat?departament=kredit`). İki ayrı siyahı saxlansa biri mütləq köhnə qalar.

---

## BMI hesabatlarının əsl forması (KRİTİK)

Bunlar **ekran cədvəli deyil**. Hər 10 forma EPPlus ilə **hazır Excel şablonunu**
doldurub fayl yaradır — çünki fayl olduğu kimi **AMB-yə gedir**, forma AMB-nindir.

| Hesabat | Şablon | Qeyd |
|---|---|---|
| Daily Report | `Daily_report.xlsm` | makrolu |
| Daily comments | `Daily comments.xlsx` | eyni forma, 2 tarixli rejim |
| LCR | `LCR_.xlsx` | vərəqlər: `L1`, `L2`, `L3 (A)`, `L3 (B)` |
| XBBIS | `XBBIS_.xlsm` | makrolu |

Şablonlar: `…\Fayllar\Muhasibat\Exceller\`, nəticə: `…\Yaradilmis exceller\`
(ad təkrarlanarsa ` - 1`, ` - 2`).

Ekranda cəmi bir-iki tarix xanası və bir düymə var:

| Forma | Giriş parametri |
|---|---|
| ADD1 | 1 tarix |
| Daily Report | 1 tarix · Daily comments-də 2 (dünən + bugün) |
| Report comments Yeni | 2 tarix |
| Dep, Əlaqəli dep | 2 tarix |
| LCR | hesabat tarixi + son altı ay |
| NXVS | giriş–çıxış tarixi |
| XBBIS | dövr əvvəli–sonu |
| İran | dövr əvvəli–sonu + əvvəlki ay |
| Rezident/q-rezident | təqvim (bir gün) |

Yəni **hesabat = parametr forması + Oracle SELECT-lər + Excel şablonunun doldurulması.**

---

## Köçürəndə düzəldiləcək

Kodda hesab kodları **sabit yazılıb** və əl ilə dəyişdirilir — `LCR.cs`-də şərh:
*«04-08-2025 ci ilde Vuqar deyisdirdi, c21 ve c22 setr elave olundu ora atildi»*.
FinNex-də kod→xana uyğunluğu **cədvələ** çıxarılmalıdır ki, qayda dəyişəndə
proqramçı lazım olmasın.

Hesabatın özü də cədvəldən yığılsın (BMI-də hər biri `Form1.Designer.cs`-də ayrıca
`ToolStripMenuItem`-dir — yeni hesabat = kod dəyişikliyi + yeni build):

| Sahə | Nümunə |
|---|---|
| `DepartamentId` | Maliyyə |
| `Dovrilik` | Günlük / Aylıq / Rüblük / İstənilən vaxt |
| `Ad` | LCR |
| `ParametrNovu` | 1 tarix / 2 tarix / dövr |
| `SablonFayl` | `LCR_.xlsx` |
| `Rol` | kim görür |

---

## ⚠️ Təsdiq gözləyən — mövcud «Hesabatlar» tab-ı

`User/HesabatController` adı ARTIQ TUTUB, amma içi yalnız HR-dır (məzuniyyət +
icazə siyahısı, filtrlə). Yeni hub-ın **HR bölməsinə köçməlidir**.

Bu **işlək funksiyadır** — CLAUDE.md-yə görə açıq icazə olmadan yeri dəyişilmir.
İki variant istifadəçiyə verildi, cavab hələ yoxdur:

- **(a)** olduğu kimi köçsün, ad «HR → Məzuniyyət və icazə hesabatı» olsun (tövsiyə olunan)
- **(b)** yerində qalsın, yeni hub başqa ad alsın (məs. «DP Hesabatları»)

---

## Mənbələr

- BMI menyu quruluşu: `BMI/BMI/Form1.Designer.cs:869-1004`
- Formalar: `BMI/BMI/Muhasibat/` (10 fayl)
- SELECT-lərin xəritəsi: `docs/sql/muhasibat/Muhasibat_Hesabatlar_Xerite.md`
  (⚠️ orada məqsəd «dashboard-a KPI çıxarmaq» kimi yazılıb — bu, **başqa işdir**;
  köçürmə isə eyni Excel faylını FinNex-dən almaqdır)
