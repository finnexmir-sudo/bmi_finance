# Maaş Hesablama Qaydaları

> Bu sənəd FinNex Maaş modulunun bütün hesablama məntiqlərini bir yerdə cəmləyir.
> Hər dəyişiklik bu sənəddə də yenilənməlidir.

## 1. Vacib məntiqi terminlər

| Termin | İzah |
|--------|------|
| **CariMaas** | İşçinin qüvvədə olan əsas (oklad) maaşı |
| **EsasBrut** | Bu ayın iş günü əsasında hesablanmış əmək haqqı brütü (qayıb, məzuniyyət, xəstəlik kəsintiləri tətbiq olunmuş) |
| **AyIsGunu** | Ayın iş günü sayı (BayramGunu cədvəlinə əsasən, həftəsonu çıxılıb) |
| **HaqiqiIsGunu** | Məzuniyyət dövründə FAKTİKİ iş günü (həftəsonu çıxılıb) |
| **IsGun** | Məzuniyyət dövründə non-bayram günləri (həftəsonu DAXİLDİR) |
| **HYS** | Həyat Yığım Sığortası — könüllü əlavə pensiya sxemi |

## 2. Brüt hesablama

```
İşlənmiş günlər üçün maaş = CariMaas / AyIsGunu × (AyIsGunu − HaqiqiIsGunu)
                          = CariMaas / AyIsGunu × İşlənmişGün
```

**Qayda:** Məzuniyyət/xəstəlik kəsintisi `HaqiqiIsGunu` əsasındadır (faktiki itirilən iş günü).

```
EsasBrut = CariMaas
         − QayıbKəsinti              (MaasdanKes=true olan qayıblar)
         − MezuniyyetKəsinti         (CariMaas/AyIsGunu × HaqiqiVacationDays)
         + MezuniyyetOdenişi         (AySonuOdenis halında)
         − XəstəlikKəsinti           (CariMaas/AyIsGunu × XestelikGun)
         + XəstəlikŞirkətOdenişi     (xəstəlik vərəqəsindən şirkət payı)
         + Bonus + FərqliGəlir − Cərimə
```

`MezuniyyetAvansBrutu` (`OdenenMeblegBrut`) — ayrıca saxlanılır, EsasBrut-a daxil OLMAYIB,
amma standart güzəşt yoxlamasında və birləşdirilmiş vergi hesablamasında istifadə olunur.

## 3. Vergi bazaları (HYS təsiri)

**Qayda (cari kod):**

| Baza | Düstur | Şamil olunur |
|------|--------|--------------|
| **VergiBazasi** | EsasBrut − HYS | Gəlir vergisi |
| **DsmfBazasi** | EsasBrut − HYS − Xəstəlik | DSMF (işçi + işəgötürən) |
| **ItssBazasi** | EsasBrut + HYSişv − Xəstəlik | İTSS, İşsizlik (işçi + işəgötürən) |

**HYS məntiqi:**
- İşçi HYS payı: Gəlir vergisi və DSMF-dən AZAD, İTSS/İşsizlikdən cəlb olunur
- İşəgötürən HYS payı: Gəlir vergisi və DSMF-dən AZAD, İTSS/İşsizlikdən cəlb OLUNUR
  (işçinin qazancına əlavə kimi sayılır)
- Xəstəlik vərəqəsi şirkət ödənişi yalnız gəlir vergisinə cəlb olunur,
  DSMF/İşsizlik/İTSS bazalarından çıxılır

## 4. Standart güzəşt (200 AZN)

```
BrutGuzestYoxlama = BrutMaas + MezuniyyetAvansBrutu
StandartGuzest = (BrutGuzestYoxlama ≤ FirstBracketMax) ? VergiGuzestiMeblegi : 0
```

Burada `BrutMaas = EsasBrut + HYSişv` (FerdiHesablaAsync-də işəgötürən HYS daxildir).

**Niyə birləşmiş?** Aylıq cəmi gəlir 2500-i keçirsə, standart güzəşt itir.
Bu, məzuniyyət avansı və maaşı bir tax dövrü kimi qiymətləndirir.

## 5. Vergilər (işçi tərəfi)

| Vergi | Bazasi | Hesablama |
|-------|--------|-----------|
| Gəlir Vergisi | VergiBazasi − StandartGuzest − İşçiGüzəşti | Pillə əsaslı |
| DSMF (işçi) | DsmfBazasi | Pillə əsaslı |
| İşsizlik (işçi) | ItssBazasi | 0.5% (pillə varsa pillə) |
| İTSS (işçi) | ItssBazasi | Pillə əsaslı |

**Pillə formulu:**

```
Pilleli(mebleg) = SabitMebleg + (mebleg − AsagiHedd) × (Faiz / 100)
```

## 6. Şirkət tərəfi (işəgötürən xərcləri)

| Növ | Bazası | İzah |
|-----|--------|------|
| DSMF işv. | DsmfBazasi | Eyni baza, fərqli pillə (Nov=7) |
| İşsizlik işv. | ItssBazasi | 0.5% (eyni baza) |
| İTSS işv. | ItssBazasi | Pillə əsaslı (Nov=9) |
| HYS işv. | HYS məbləği × HysIsvFaiz% | (param: HysIsegoturenFaizi) |

### 6.1 DSMF işəgötürən pilləsi (Mühasibin Excel düsturu ilə eyni)

```
Excel: =ROUND(IF(X<200, X*22%,
              IF((X-200)<=8000, (X-200)*0.15+44,
                                (X-8000)*0.11+1214)), 2)
```

| İnterval (X = DsmfBazasi) | Düstur |
|---------------------------|--------|
| 0 − 200 | `X × 22%` |
| 200 − 8.200 | `44 + (X − 200) × 15%` |
| 8.200+ | `1.214 + (X − 8.000) × 11%` |

**Nümunə (Məhəmməd, May 2026):**
- DsmfBazasi = 1,029.41 − 318.10 (HYS) − 0 (xəstəlik) = **711.31 ₼**
- İkinci pillə: 44 + (711.31 − 200) × 15% = 44 + 76.70 = **120.70 ₼** ✓

### 6.2 İTSS işəgötürən pilləsi

| İnterval (X = ItssBazasi) | Düstur |
|---------------------------|--------|
| 0 − 2.500 | `X × 2%` |
| 2.500+ | `50 + (X − 2.500) × 0.5%` |

### 6.3 İşsizlik işəgötürən

Flat 0.5% (ItssBazasi-dən).

## 7. Məzuniyyət ödəniş hesablaması

```
gunlukMaas    = CariMaas / AyIsGunu
gunlukMezPul  = (son 12 ay təshih edilmiş cəm) / 12 / 30.4
gunlukDerece  = MAX(gunlukMaas, gunlukMezPul)
Ödəniş        = gunlukDerece × HaqiqiIsGunu
```

**Niyə HaqiqiIsGunu?** Əsas maaşdan kəsinti də iş günü əsasındadır
(esasMaas/AyIsGunu × HaqiqiIsGunu); ödənişin də eyni bazada olması iki
hesablama arasında uyğunsuzluğu (həftəsonu padding) aradan qaldırır.

## 8. Qabaqcadan ödəniş (Avans) məzuniyyəti

**Sxem:**
1. HR təsdiqlədikdə: `OdenenMebleg = hesab.CemiOdenis` (brüt) saxlanılır
2. Mühasib `Planla` səhifəsində NET məbləği daxil edir → `OdenenMebleg` NET olur,
   `OdenenMeblegBrut` brüt olur
3. Ay sonu maaşı hesablananda:
   - Aylıq cəmi gəlir = İşlənmişMaaş + AvansBrut
   - Vergilər aylıq cəmi gəlirə tətbiq olunur
   - Avans NET artıq ödənilib → maaşda yalnız qalan NET ödənilir

**Birləşmiş vergi hesablaması (TopluHesabla preview):**

```
salaryNet  = combinedNet − mavNet  (faktiki ödənilmiş)
combinedNet = (EsasBrut + MavBrut) − cTaxes − HYS
cTaxes      = Vergilər birləşmiş bazada (EsasBrut + MavBrut)
```

**Növ üzrə bölgü (rescaled split):**

`TutulmalariHesablaAsync` `im` üçün standart güzəşti `im+hysIsv ≤ 2500` olduqda
tətbiq edir, amma birləşmiş baza üçün etmir. Bu uyğunsuzluğu aradan qaldırmaq
üçün avans vergi bölgüləri yenidən miqyaslandırılır:

```
mavTaxImplied = mavBrut − mavNet     (faktiki ödənilmiş vergi)
scale         = mavTaxImplied / sum(server-side mav vergiləri)
mav[növ] *= scale
salary[növ] = combined[növ] − mav[növ]
```

## 9. TopluHesabla görünüş qaydaları

**İşçi tərəfi (TUTULMALAR) kartında:**
- Cəmi tutulma = 4 vergi (Gəlir V. + DSMF + İşsizlik + İTSS) + Avans
- HYS AYRI kartda göstərilir (kartla cəmləmir)
- Məz. avansı vergisi info kimi əlavə olunur (artıq ödənilib)

**Yoxlama:**
```
Maaş kartı 4 vergi cəmi + Məz.avansı 4 vergi cəmi = Preview "Cəmi tutulmalar"
NET MAAŞ = EsasBrut − salary 4 taxes − HYSişçi − Avans
        (HYSişv süni olaraq brut-da əks olunsa belə, nağd alınmır)
```

## 10. Müraciət preview ilə Mühasib hesablamasının uyğunluğu

**HƏR DƏFƏ eyni rəqəm olmalıdır:**
- `User/Mezuniyyet/Create` preview-undakı "Qabaqcadan məzuniyyət ödənişi (net)"
- `HR/MezuniyyetOdenis/Detail`-də göstərilən net
- TopluHesabla-da NET (ödənilib)
- DB-də `OdenenMebleg`

**Hər ikisi `ig = AyIsGun − HaqiqiIsGun` istifadə etməlidir** (IsGun yox).

## 11. Açıq suallar (yoxlanmalı)

- [x] **DSMF işv pilləsi:** Mühasibin Excel düsturu ilə tam üst-üstə düşür
      (0-200 22%, 200-8200 44+15%, 8200+ 1214+11%). Bax bölmə 6.1.
- [ ] **DSMF bazasından HYS işçi payı çıxılır** — bu cari koddur. Mühasibin
      Excel düsturundakı `X = S26+O26+N26-M26-T26-L26-K26-U26` ifadəsində
      bu sütunlar yoxlanılmalıdır ki, kod ilə eyni baza istifadə edildiyi
      təsdiq olunsun. Hələlik nümunə üst-üstə düşür (711.31 → 120.70).
- [ ] **Şirkət xərcləri TopluHesabla-da yalnız maaş hissəsi** göstərilir —
      avans üçün ayrıca uçot olunmalıdır (cari kod belədir). Tələb varsa
      birləşmiş aylıq cəm də göstərilə bilər.
