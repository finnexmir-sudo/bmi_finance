# Avtopark Modulu — Layihə Planı

**Status:** təqdimat üçün layihə · cavab gözlənilən 5 sual var
**Tarix:** 19.08.2026

---

## 1. Nə istənilir

İdarənin xidməti maşınları üçün iki şey:

1. **Giriş-çıxışa nəzarət** — kim maşını götürdü, nə vaxt getdi, nə vaxt qayıtdı
2. **Vaxt izləmə** — sığortanın bitməsi, texniki baxış, yağ dəyişmə və s.

---

## 2. İş axını (istifadəçi ilə razılaşdırılıb)

```
1. İŞÇİ     → maşını seçir, müraciət göndərir (tarix, saat, məqsəd)
2. RƏHBƏR   → təsdiq / imtina
3. KASSA    → açarı verir  →  «ÇIXDI»  düyməsi   →  bildiriş: işçi + rəhbər
4. KASSA    → açarı alır   →  «GƏLDİ»  düyməsi   →  bildiriş: işçi + rəhbər
```

Açarlar fiziki olaraq kassada saxlanılır — sistem yalnız həmin anı qeyd edir.
Yəni «çıxdı/gəldi» düyməsi **açarın əldən-ələ keçdiyi an** basılır, bu da
jurnalın həqiqətə uyğunluğunu təmin edir.

### Statuslar

| Status | Mənası | Kim dəyişir |
|---|---|---|
| `Gozlemede` | rəhbər təsdiqini gözləyir | işçi (yaradır) |
| `Tesdiqlenib` | təsdiq olunub, açar gözləyir | rəhbər |
| `Cixib` | **maşın çöldədir** | kassa |
| `Qayidib` | bağlandı | kassa |
| `ImtinaEdildi` | rəhbər imtina etdi | rəhbər |
| `LegvEdildi` | işçi özü ləğv etdi | işçi (yalnız çıxışdan əvvəl) |

---

## 3. Cədvəllər

### 3.1 `Masin` — maşın kartı

| Sahə | İzah |
|---|---|
| `DovletNomresi` | 10-AA-123 — **unikal** |
| `Marka`, `Model`, `BuraxilisIli`, `Reng` | |
| `Ban` / `VIN` | texpasportdan |
| `Novu` | minik / mikroavtobus / yük |
| `DepartamentId` | hansı şöbəyə aiddir (nullable — ümumi ola bilər) |
| `TehkimSurucuId` | təhkim olunmuş sürücü (nullable) |
| `Status` | Aktiv / Təmirdə / İstifadədən çıxıb |
| `CariKm` | son bilinən spidometr (nullable — **bax §5.2**) |
| `Qeyd` | |

### 3.2 `MasinMuraciet` — müraciət + çıxış/qayıdış jurnalı

**Bir cədvəldə saxlanılır.** Müraciət və faktiki çıxış eyni hadisənin iki
mərhələsidir; ayrı cədvəl saxlasaq «hansı çıxış hansı müraciətə aiddir»
bağlantısını əl ilə qurmaq lazım gələrdi və uyğunsuzluq riski yaranardı.

| Sahə | Kim doldurur |
|---|---|
| `MasinId`, `IsciId` | işçi |
| `PlanBaslama`, `PlanBitme` | işçi — nə vaxt lazımdır |
| `Meqsed`, `Marsrut` | işçi |
| `Status` | axın |
| `RehberId`, `RehberTesdiqTarixi`, `ImtinaSebebi` | rəhbər |
| `CixisTarixi`, `CixisQeydEdenId` | **kassa** — «Çıxdı» anı |
| `QayidisTarixi`, `QayidisQeydEdenId` | **kassa** — «Gəldi» anı |
| `CixisKm`, `QayidisKm` | nullable — **bax §5.2** |

### 3.3 `MasinMuddet` — sığorta, baxış, yağ…

| Sahə | İzah |
|---|---|
| `MasinId`, `NovId` | növ ayrıca cədvəldədir (aşağı bax) |
| `SonTarix` | tarixə görə bitənlər (nullable) |
| `SonKm` | kilometrə görə bitənlər (nullable) |
| `XeberdarliqGun` | neçə gün əvvəl xəbər verilsin (default 30) |
| `XeberdarliqKm` | neçə km qalmış (default 500) |
| `Mebleg`, `SenedFaylYolu` | polis/qəbz — DMS-ə |
| `Aktivdir` | yenilənəndə köhnə sətir **passivləşir, silinmir** |

**Növlər üçün ayrıca cədvəl (`MasinMuddetNovu`)** — enum yox. Səbəb: hər idarədə
siyahı fərqlidir və yeni növ («təkər», «əyləc», «yanğınsöndürən») əlavə etmək
üçün kod dəyişikliyi tələb olunmamalıdır. Admin özü idarə edir.

**İKİ FƏRQLİ MÜDDƏT NÖVÜ VAR — qarışdırılmamalıdır:**

| Növ | Nəyə görə bitir |
|---|---|
| İcbari sığorta, Kasko, Texniki baxış, İcazə | **tarixə** görə |
| Yağ dəyişmə, filtr, təkər | **kilometrə** görə |
| Bəziləri hər ikisinə | «10 000 km **və ya** 6 ay — hansı əvvəl gəlsə» |

Ona görə bir sətirdə həm `SonTarix`, həm `SonKm` var və hər ikisi boş qala bilər.

---

## 4. Qoruyucular — səssiz səhvləri qabaqlayan qaydalar

Bunlar FinNex-də artıq yaşanmış səhvlərdən çıxarılıb; təkrarlanmasın deyə
əvvəlcədən qoyulur.

**4.1 Açıq çıxış ikinci dəfə açıla bilməz.** Maşın «Çıxıb» statusundadırsa,
həmin maşına yeni «Çıxdı» yazıla bilməz. Əks halda jurnal iki paralel çıxış
göstərər və heç bir xəta çıxmaz.

**4.2 Təsdiqlənmiş müraciətlər üst-üstə düşməməlidir.** Eyni maşın, kəsişən
saat aralığı — ikinci müraciət təsdiqlənə bilməz. Xəta mətni **toqquşan
müraciətin** sahibini və saatlarını yazsın ki, işçi səbəbi ekranda görsün.

**4.3 Qayıdış çıxışdan əvvəl ola bilməz.** Sadə görünür, amma yazılmasa saat
səhv yazılanda jurnalda mənfi müddət yaranır.

**4.4 Qayıtmayanlar paneli.** Gün sonunda hələ «Çıxıb» statusunda qalan
sətirlər ayrıca siyahıda görünsün — kimin üstündə maşın qalıb, dərhal bilinsin.

**4.5 Rol prioriteti.** Müraciət edən özü rəhbərdirsə kim təsdiqləyir?
FinNex-də bu tələ artıq bir dəfə yaşanıb (işçidə HR + Rəhbər rolu birlikdə idi,
ekran səhv addım göstərirdi). Qayda **əvvəlcədən** yazılmalıdır — **bax §5.5**.

---

## 5. CAVAB GÖZLƏYƏN SUALLAR

### 5.1 «Kassa» rolu

«Çıxdı/Gəldi» düymələrini kim basacaq?

- (a) Yeni **`Kassa`** rolu yaradılsın — yalnız açar verən şəxs
- (b) Mövcud rollardan biri (Admin / Təsərrüfat)

*Təklifim: (a).* Ayrıca rol olsa jurnalda «kim verdi» dəqiq qalır və səlahiyyət
lazımsız genişlənmir.

### 5.2 Spidometr (kilometr) yazılacaqmı?

Çıxış və qayıdışda spidometr göstəricisi yazılsın?

- **Yazılsa:** yağ dəyişmə avtomatik izlənir («yağa 850 km qalıb»), yürüş
  hesabatı çıxır, sürücü üzrə istifadə görünür.
- **Yazılmasa:** yağ dəyişməni yalnız tarixlə izləyəcəyik (zəif), km-ə bağlı
  müddətlər əl ilə yenilənəcək.

*Təklifim:* qərar gec verilsə də **sahələr indidən qoyulsun** (boş qala bilər).
Sonradan qərar gələndə yalnız formaya bir input əlavə olunur — cədvəl
dəyişikliyi lazım gəlmir.

### 5.3 Xatırlatma kimə və nə vaxt?

Sığortaya 30 gün qalmış — **kimə** bildiriş getsin?

- təsərrüfat müdiri? · rəhbər? · təhkim olunmuş sürücü?
- neçə gün əvvəl (30 / 15 / 7 — bir neçə mərhələ ola bilər)?

### 5.4 Xərclər bu modulda izlənəcəkmi?

Yanacaq, təmir, sığorta ödənişi — «hansı maşın ayda nə qədərə başa gəlir»
hesabatı istənilirmi?

FinNex-də **`Xerc` modulu artıq var** (kateqoriya, təsdiq axını, qəbz faylı).
Ora `MasinId` sahəsi əlavə etmək kifayətdir — yenidən qurmağa ehtiyac yoxdur.

*Bu, 4-cü mərhələdir; ilk üç mərhələni bloklamır.*

### 5.5 Müraciət edən özü rəhbərdirsə?

- (a) Onun müraciəti təsdiq tələb etmir — birbaşa təsdiqlənir
- (b) Başqa bir rəhbər/admin təsdiqləyir

*Təklifim: (a)* — icazə modulunda eyni qayda işləyir.

---

## 6. Ekranlar

| Ekran | Kim görür |
|---|---|
| **Maşınlar** (siyahı + kart) | Admin / Təsərrüfat |
| **Maşın müraciəti** (yeni + mənim müraciətlərim) | bütün işçilər |
| **Təsdiq paneli** — maşın müraciətləri | rəhbər |
| **Açar jurnalı** — təsdiqlənmişlər + «Çıxdı/Gəldi» düymələri | kassa |
| **Açıq çıxışlar** (qayıtmayanlar) | kassa + rəhbər |
| **Müddətlər** — sığorta/baxış/yağ + yaxınlaşanlar | Admin / Təsərrüfat |

Müraciət ekranı mövcud **«Müraciətlər»** portalına əlavə oluna bilər — işçi
məzuniyyət/icazə ilə eyni yerdən maşın da istəyər.

---

## 7. Mərhələlər

| # | Nə qurulur | Nəticə |
|---|---|---|
| **1** | `Masin` kartı + CRUD ekranı + `Kassa` rolu | maşınlar sistemdədir |
| **2** | Müraciət → təsdiq → çıxış/qayıdış + 4 bildiriş | **əsas tələb işləyir** |
| **3** | `MasinMuddet` + xatırlatma | sığorta/baxış unudulmur |
| **4** | Xərc bağı + hesabatlar | maşın üzrə maya dəyəri |

Hər mərhələ ayrıca işlək olur — 2-ci mərhələdən sonra sistem real istifadəyə
verilə bilər, 3-cü gözlənilmədən.

---

## 8. Təkrar işlədiləcək hazır hissələr

Sıfırdan qurulmayacaq:

| Hazır olan | Nəyə yarayır |
|---|---|
| `XatirlatmaBackgroundService` (saatda bir, 4 xatırlatma növü) | müddət xəbərdarlığı üçün **yeni servis lazım deyil** — 6-cı bənd əlavə olunur |
| `Bildiris` + `BildirisRouter` | 4 yeni bildiriş növü mövcud zəng ikonuna düşür |
| `Xerc` modulu | 4-cü mərhələdə yenidən qurmadan bağlanır |
| DMS (`C:\FinNex_DMS`) | sığorta polisi, texpasport faylları |
| Təsdiq paneli, müraciət portalı | maşın müraciəti ora əlavə olunur |
| `Isci`, `Departament` | sürücü və şöbə bağı |

Bu, həm iş həcmini azaldır, həm də modulun sistemin qalan hissəsi ilə eyni
görünüb eyni işləməsini təmin edir.
