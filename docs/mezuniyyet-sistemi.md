# Məzuniyyət Sistemi — Spesifikasiya

> Bu sənəd məzuniyyət balansının avtomatlaşdırılması və ümumi "şəxsi fakt +
> məzuniyyət növü" sisteminin razılaşdırılmış **qaydalarını və qərarlarını**
> saxlayır. Kod bu sənədə əsasən yazılır. Hər sessiyada buraya baxılır.

## Məqsəd

1. Məzuniyyət balansını **avtomatik** hesablamaq (əl ilə yox).
2. Şəxsi faktları (əlillik, uşaq və s.) **bir yerdə** saxlamaq — maaş və məzuniyyət
   modulları oradan öz nəticəsini çıxarsın.
3. Yeni məzuniyyət növləri: ödənişsiz (öz hesabına), analıq, atalıq.

---

## 1. Bağlanmış qərarlar

| # | Qərar | Nəticə |
|---|---|---|
| 1 | **Əlillik mənbəyi** | Mövcud **güzəşt** təyinatından oxunur — maaşa toxunulmur. Güzəştə `Novu` markeri əlavə olunur ki, əlillik adla yox, **tiplə** tanınsın. |
| 2 | **Əsas 21/30** | **Vəzifəyə** bağlanır (`EsasMezuniyyetGunu`). İşçi öz aktiv vəzifəsindən götürür. Əlil → 42 (vəzifədən asılı deyil). |
| 3 | **Staj** | **Ümumi əmək stajı** (bütün iş yerləri). Mənbə: `EvvelkiStajPeriodlari` + `IsheQebulTarixi`. |
| 4 | **Köçürmə** | ⚠️ **AÇIQ** — maks 5 / limitsiz / kompensasiya. Addım 4-də həll olunur. |
| 5 | **Uşaq/tək-valideyn/analıq datası** | HR **əl ilə** doldurur. |
| 6 | **Analıq/ödənişsiz stajı** | Analıq → staj/balansa **sayılır**; ödənişsiz → **sayılmır**. |
| 7 | **Model** | **İş ili** — işə qəbul tarixindən növbəti ilin həmin gününə (təqvim ili yox). |

---

## 2. Əmək Məcəlləsi qaydaları

### Əsas məzuniyyət
- **M.114** — ümumi ≥ 21 təqvim günü; mütəxəssis/rəhbər → **30**.
- **M.119** — əlilliyi olan işçi → **42** (qrupdan, səbəbdən, müddətdən asılı olmayaraq).

### Əlavələr (əsasın üstünə toplanır)
- **M.116 (staj):** 5–10 il **+2**; 10–15 il **+4**; 15 ildən çox **+6**. **Ümumi staj** (bax Qərar 3).
- **M.117 (uşaq):** 14 yaşınadək 2 uşaq **+2**; 14-dək 3+ uşaq, ya 18-dək əlil uşaq **+5**. Tək ataya/övladlığa götürənə də şamil.
- **M.115 (şərait):** ≥6 (bank üçün adətən 0).

### ⚠️ KRİTİK — M.116.3
Staj (116) və şərait (115) əlavələri **118, 119, 120, 121** kateqoriyalarına **VERİLMİR**.
→ **Əlil işçi (119) staj əlavəsi almır** — yalnız **42** (+ uşaq 117, əgər varsa).
→ 118 (pedaqoji 56), 120 (elmi dərəcə), 121 (teatr/TV/kino) — bank üçün adətən aktual deyil.

### Vaxt qaydaları
- **M.133** — ilk iş ilində məzuniyyət **6 ay** işlədikdən sonra istifadə oluna bilər (qazanma yox, istifadə).
- **Mütənasib:** illik gün ÷ 12 × işlənmiş tam ay.
- **M.144** — işdən çıxanda bütün qalıq pulla ödənilir.

### Digər növlər
- **M.125 (analıq):** 126 gün (adi) / 140 gün (çətin və ya çoxdöllü doğuş). Şirkət ödəmir — DSMF müavinəti.
- **M.129 (ödənişsiz / öz hesabına):** ödənilmir; balansa dəymir; stajа sayılmır.
- **Atalıq** — sonrakı iş.

---

## 3. Düstur

```
əsas       = əlil ? 42 : vəzifə.EsasMezuniyyetGunu           // 21 / 30 / 42
staj_əlavə  = əlil ? 0  : (staj≥15 ? 6 : staj≥10 ? 4 : staj≥5 ? 2 : 0)
uşaq_əlavə  = (3+ uşaq və ya əlil uşaq) ? 5 : (2 uşaq ? 2 : 0)
illik_hüquq = əsas + staj_əlavə + uşaq_əlavə
qazanılan   = illik_hüquq ÷ 12 × işlənmiş_tam_ay             // iş ili ərzində
qalıq       = qazanılan − istifadə + köçürülən
```

İş ili = `IsheQebulTarixi` → növbəti ilin həmin günü. Balans hər işçinin öz dönümündə açılır.

---

## 4. Data mənbələri

| Fakt | Mənbə | Status |
|---|---|---|
| Əsas 21/30 | `Vezife.EsasMezuniyyetGunu` | Addım 1a (yeni) |
| Əlillik | Güzəşt təyinatı (`Guzest.Novu == Əlillik`) | Addım 1b (marker) |
| Ümumi staj | `EvvelkiStajPeriodlari` + `IsheQebulTarixi` | mövcud |
| Uşaq / əlil uşaq / tək valideyn | İşçi — yeni sahələr | Addım 1c |
| İstifadə olunan günlər | Təsdiqlənmiş `Mezuniyyet` qeydləri | mövcud |
| İş ili başlanğıcı | `IsheQebulTarixi` | mövcud |

---

## 5. Ssenari (fazalar)

### Faza 1 — Balans avtomatlaşdırması
- **Addım 0** — bu spesifikasiya.
- **Addım 1 (additive, heç nə sınmır):**
  - 1a. Vəzifəyə `EsasMezuniyyetGunu` (21/30) + form + mövcudlara ad üzrə ilkin təklif.
  - 1b. Güzəştə `Novu` markeri (əlillik tiplə tanınsın).
  - 1c. İşçiyə uşaq sayı, əlil uşaq, tək valideyn.
- **Addım 2** — hesablama mühərriki (yalnız oxuma) + yoxlama səhifəsi.
- **Addım 3** — iş ili + balans (canlı).
- **Addım 4** — il dönümü + köçürmə (Qərar 4 lazımdır).
- **Addım 5** — miqrasiya + canlıya keçid.

### Faza 2 — Ödənişsiz (öz hesabına) məzuniyyət
Davamiyyət: "Ödənişsiz" statusu (qayıb yox). Maaş: ödənişli günlərdən çıxılır. Balansa dəymir.

### Faza 3 — Analıq məzuniyyəti
126/140 gün. Davamiyyət: "Analıq" statusu. Maaş: şirkət ödəmir. Staj/balansa **sayılır**.

### Faza 4 — Atalıq məzuniyyəti (sonraya)

---

## 6. Açıq qərarlar
- **Köçürmə siyasəti** (Qərar 4): maks 5 / limitsiz / kompensasiya — Addım 4-dən əvvəl.
- Analıq/ödənişsiz günlərin stajа sayılmasının texniki detalları (Qərar 6 təsdiqi) — Faza 2/3-dən əvvəl.

---

## 7. Prinsip — mövcud işləyən heç nəyə toxunmuruq
- Güzəşt kataloğu + işçi güzəştləri **qalır**, maaş hesablaması **dəyişmir**.
- Yeni sistem **additive** qurulur; məzuniyyət əlilliyi güzəştdən **oxuyur** (ikinci flag yaratmırıq).
- Hər maliyyə addımından sonra **istifadəçi yoxlaması** gözlənilir.
