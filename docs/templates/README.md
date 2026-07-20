# Hesabat şablonları

Requlyativ hesabat şablonları (AMB MHBS 9 və s.) proqram tərəfindən **DMS**-dən oxunur —
gələcəkdə şöbə əsaslı çoxlu şablon üçün struktur:

```
C:\FinNex_DMS\hesabat-sablonlari\
    └── muhasibat\                 (şöbə)
        └── amb-mhbs9\             (hesabat növü)
            └── AMB_MHBS9.xlsx     (rəsmi AMB şablonu — bu qovluqdakı nüsxə)
```

Konfiqurasiya mənbəyi: `appsettings.json → DocumentStorage:RootPath` (default `C:\FinNex_DMS`).

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
