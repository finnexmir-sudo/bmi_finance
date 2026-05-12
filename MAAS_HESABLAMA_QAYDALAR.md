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

- [ ] DSMF bazasından HYS çıxılır — qanunla doğrudurmu? (cari kod belə işləyir)
- [ ] Şirkət xərcləri TopluHesabla-da yalnız maaş hissəsi göstərilir, avans
      üçün ayrıca uçot olunmalıdır
- [ ] Pillə strukturu (DSMF işv): 0-200 sabit 0 faiz 22%? 200-2500 sabit 44
      faiz 15%? — DB-də yoxlanmalı
