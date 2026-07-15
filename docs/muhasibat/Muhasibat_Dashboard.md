# Mühasibat Dashboard — Balans İcmalı (v1)

Bankın mühasibat/maliyyə paneli. İlk tab: **Balans İcmalı**. Çox şöbə (Mühasibat,
Audit, Risk) icazə ilə görə bilir. Mənbə: Oracle (BMI), yalnız SELECT.

---

## 1. Nə göstərir

| Blok | Məzmun |
|------|--------|
| **KPI kartları** | Ümumi Aktiv · Ümumi Öhdəlik · Kapital · Kapital/Aktiv əmsalı (%) |
| **Balans yoxlaması** | Aktiv = Öhdəlik + Kapital — fərq göstərilir (yaşıl = düz) |
| **Aktivlərin strukturu** | Donut + faiz zolaqları (kassa, AMB, kreditlər, …) |
| **Öhdəliklərin strukturu** | Donut + faiz zolaqları (depozitlər, cəlb olunmuş, …) |
| **Valyuta strukturu** | Aktivlərin AZN/USD/EUR/… bölgüsü |
| **Tarix seçici** | İstənilən keçmiş tarixə balans |

---

## 2. Balans necə hesablanır

**Mənbə sorğu** — bir tarixə bütün açıq hesabların qalığı:

```sql
SELECT ar.licsch AS hesab,
       CASE WHEN SUBSTR(ar.licsch,0,3) IN ('159','209','219','239','259')
            THEN SUBSTR(ar.licsch,16,2) ELSE SUBSTR(ar.licsch,6,2) END AS valyuta,
       ar.saldo_ish_nacval AS qaliq
FROM   odb.arh_saldo_ls ar, licsch ch
WHERE  ar.date_oper = TO_DATE('{TARIX}','dd/mm/yyyy')
  AND  ch.licsch = ar.licsch
  AND  (ch.date_close_licsch IS NULL OR ar.date_oper <= ch.date_close_licsch);
```

> `saldo_ish_nacval` **artıq AZN-dədir** (nacval = milli valyuta) — kurs çevirməsi yox.
> Passiv hesablar mənfi saxlanır, servisdə `-` ilə müsbətə çevrilir.

**Təsnifat** — hesab kodunun ilk rəqəmi üzrə (hər hesab bir dəfə sayılır):

| İlk rəqəm | Kateqoriya | Qruplar (ilk 2 rəqəm) |
|-----------|-----------|------------------------|
| **1, 2** | Aktiv | 10=Kassa · 11=AMB/müxbir · 12–14=Banklararası · 15=Likvid/digər · 20–23=Kreditlər · 24–26=Faizlər/digər · 27–28=Əsas vəsaitlər |
| **3, 4** | Öhdəlik | 35–36=Bank/maliyyə · 38–39=Cəlb olunmuş · 40=Hüquqi depozit · 41=Fiziki depozit · digər |
| **44, 45, 5x** | Kapital | Kapital və ehtiyatlar |
| digər | Təsnifsiz | Balans yoxlaması üçün ayrıca göstərilir |

**Totallar:** Aktiv = Σ aktiv qruplar · Öhdəlik = −Σ öhdəlik · Kapital = −Σ kapital.

---

## 3. Fayllar

| Qat | Fayl |
|-----|------|
| DTO | `FinNex.Application/DTOs/Muhasibat/MuhasibatBalansDto.cs` |
| İnterfeys | `FinNex.Application/Interfaces/Muhasibat/IMuhasibatService.cs` |
| Servis | `FinNex.Application/Services/Muhasibat/MuhasibatService.cs` |
| Controller | `FinNex.UI/Areas/Muhasibat/Controllers/DashboardController.cs` |
| View | `FinNex.UI/Areas/Muhasibat/Views/Dashboard/Index.cshtml` |
| DI | `FinNex.Application/AddApplicationServices.cs` (Risk-in yanında) |

**URL:** `/Muhasibat/Dashboard`

---

## 4. Giriş (icazə əsaslı)

- **Admin** və **Muhasib** rolu → avtomatik görür.
- **Digər şöbələr (Audit, Risk, …)** → `muhasibat_dashboard_bax` icazəsi ilə.

**Audit/Risk-ə vermək:**
1. Admin → **Permissions** → yeni icazə: kod = `muhasibat_dashboard_bax`
2. Admin → **UserPermissions** → həmin istifadəçiyə ver (Allowed = ✓)

Yeni rol yaratmağa ehtiyac yoxdur — icazə istənilən istifadəçiyə verilə bilər.

---

## 5. Yoxlama (vacib)

> ⚠️ Kod **build edilməyib** (mühitdə dotnet SDK yox) — üç qat əl ilə yoxlanıb.
> VS-də build et; xəta olsa bildir.

1. Pull → build → işə sal.
2. `/Muhasibat/Dashboard` aç → tarix seç → Göstər.
3. **Ümumi Aktiv / Öhdəlik**-i eyni tarixin real **Daily Report**-u ilə tutuşdur.
   - Yaxındırsa → təsnifat düzdür.
   - Fərq böyükdürsə → "Təsnifsiz" sətrinə bax, mapping dəqiqləşdiriləcək.

---

## 6. Məhdudiyyətlər (dürüst qeyd)

- **Dashboard səviyyəli** balansdır (ilk rəqəm təsnifatı) — tənzimləyici AMB
  Daily Report-un Row 6–70 sətir-sətir detalı **deyil**. Rəqəmlər ±yaxın olmalıdır.
- **Cari il mənfəəti yoxdur** — mənbə desktopda da hesablanmır; ayrıca P&L
  (gəlir/xərc) hesablarından qurulmalıdır.

---

## 7. Növbəti addımlar

1. ✅ Balansı təsdiqlə (rəqəmlər düzdürmü) — lazımsa mapping dəqiqləşdirilir.
2. Dünənlə müqayisə (gündəlik dəyişmə oxları) + Excel ixrac.
3. Növbəti tab: **Likvidlik** (LCR, ani likvidlik, maturity ladder) — yaxud
   Depozitlər / Kredit portfeli.

> Tam hesabat xəritəsi: `docs/sql/muhasibat/Muhasibat_Hesabatlar_Xerite.md`
