# Hesabat şablonları

Requlyativ hesabat şablonları (AMB MHBS 9 və s.) proqram tərəfindən **DMS**-dən oxunur —
gələcəkdə şöbə əsaslı çoxlu şablon üçün struktur:

```
C:\FinNex_DMS\hesabat-sablonlari\
    └── muhasibat\                 (şöbə)
        └── amb-mhbs9\             (hesabat növü)
            ├── AMB_MHBS9.xlsx                          (rəsmi AMB A1 şablonu)
            └── AMB_Metodoloji_Rehberlik_23122025.docx  (AMB Metodoloji Rəhbərliyi — 23.12.2025, № 45/2)
```

Konfiqurasiya mənbəyi: `appsettings.json → DocumentStorage:RootPath` (default `C:\FinNex_DMS`).

**AMB Metodoloji Rəhbərliyi (.docx):** IFRS 9 səhifəsindən "Metodologiya → AMB qaydasını yüklə"
düyməsi bu sənədi `Hesabat/AmbQaydaSened` action-ı ilə DMS-dən verir. Sənəd tapılmasa,
istifadəçiyə hansı yola kopyalamaq lazım olduğu bildirilir.

## Quraşdırma

Bu qovluqdakı **`AMB_MHBS9.xlsx`** faylını yuxarıdakı DMS yoluna kopyala (həm VM-də,
həm də lokal test üçün). Fayl mövcuddursa `Hesabat/AmbA1Excel` onu doldurur
(A1 vərəqi — alt-sahə sətirlərinin E-H brüt + J-M ECL mərhələ xanaları; D/I və cəmlər
şablonun öz formulları ilə). Fayl tapılmasa, proqram təzə (formatlanmamış) A1 cədvəli
generasiya edir — məlumat yenə düzgün olur, sadəcə rəsmi şablon formatı olmur.

## Qeydlər
- Şablon **min manatla** doldurulur (AZN ÷ 1000, 1 onluq).
- POCI və FVOCI (C/D alt-cədvəlləri) — bankda yoxdur, 0.
- Doldurulan yalnız A1-dir; A1.1 / A1.2 / A4 növbəti mərhələdə.
