# Avtopark Modulu — Layihə Planı

**Status:** ✅ 1–3-cü MƏRHƏLƏ QURULDU (19.08.2026) · 4-cü mərhələ (xərclər) açıqdır
**Tarix:** 19.08.2026

> ⚠️ **KOD BUILD EDİLMƏYİB.** Bu mühitdə `dotnet` yoxdur — sintaksis əl ilə
> yoxlanılıb (mötərizə balansı, using-lər, ad toqquşması), amma `dotnet build`
> ilə **0 xəta** isbat OLUNMAYIB. İlk işə salmada build nəticəsini yoxlayın.

---

## 1. Nə istənilir

İdarənin xidməti maşınları üçün iki şey:

1. **Giriş-çıxışa nəzarət** — kim maşını götürdü, nə vaxt getdi, nə vaxt qayıtdı
2. **Vaxt izləmə** — sığortanın bitməsi, texniki baxış, yağ dəyişmə və s.

---

## 2. İş axını (istifadəçi ilə razılaşdırılıb)

```
ADİ İŞÇİ:
  1. İŞÇİ    → maşını seçir, müraciət göndərir (tarix, saat, məqsəd)
  2. RƏHBƏR  → təsdiq / imtina
  3. KASSA   → açarı verir  →  «ÇIXDI»  →  bildiriş: işçi + rəhbər
  4. KASSA   → açarı alır   →  «GƏLDİ»  →  bildiriş: işçi + rəhbər

MÜRACİƏTÇİ ÖZÜ RƏHBƏRDİRSƏ:
  1. RƏHBƏR  → müraciət göndərir  →  2-ci addım ATLANIR
  2. KASSA   → «ÇIXDI» / «GƏLDİ»  →  bildiriş: işçi (= rəhbər)
```

Açarlar fiziki olaraq kassada saxlanılır — sistem yalnız həmin anı qeyd edir.
Yəni «çıxdı/gəldi» düyməsi **açarın əldən-ələ keçdiyi an** basılır, bu da
jurnalın həqiqətə uyğunluğunu təmin edir.

### Statuslar

| Status | Mənası | Kim dəyişir |
|---|---|---|
| `Gozlemede` | rəhbər təsdiqini gözləyir | işçi (yaradır) |
| `Tesdiqlenib` | təsdiq olunub, açar gözləyir | rəhbər — **və ya müraciətçi özü rəhbərdirsə sistem** |
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

### 5.1 «Kassa» rolu — ✅ CAVABLANDI

**Yeni `Kassa` rolu əlavə edilir.** 19.08.2026 dəqiqləşməsi: düymələri
**`Kassa` + `Rehber` (+ `Admin`)** görür — «kassa tərəfini kassa dep-də
işləyənlər görəcək + əlavə rəhbərdə də olsun gediş-qayıdış düymə vurmaq üçün».
Kassa işçisi olmayanda axın dayanmasın. Jurnalda «açarı kim verdi»
(`CixisQeydEdenId` / `QayidisQeydEdenId`) dəqiq qalır.

Rol `IdentitySeed`-də yaradılır, amma **heç kimə avtomatik verilmir** —
kimin kassada işlədiyini Admin → İstifadəçi idarəetməsi ekranında təyin edir.

### 5.2 Spidometr — ✅ CAVABLANDI (19.08.2026): İSTİFADƏ OLUNMUR

İstifadəçi qərarı: **«km ilə yağ dəyişmə olmayacaq, hələlik ildə bir dəfə qoyarıq».**
Yəni müddət izləməsi **tamamilə tarixə görədir** — yağ dəyişmə də daxil
(standart növ siyahısında «Yağ dəyişmə» 14 günlük xəbərdarlıqla, illik yenilənən).

**Qərar:** forma və ekranda spidometr **OLMAYACAQ**, amma `CixisKm` / `QayidisKm`
sahələri cədvəldə **boş qalacaq şəkildə saxlanılır**. Sonradan lazım olsa yalnız
formaya bir input əlavə olunur — cədvəl dəyişikliyi və deploy riski olmur.

> ⚠️ **NƏTİCƏSİNİ BİLMƏK VACİBDİR:** ilkin tələbdə «**yağ dəyişmə**» vardı.
> Spidometr olmadan yağ dəyişməni **yalnız tarixlə** izləyə bilərik («hər 6 ayda
> bir»), faktiki yürüşlə yox. Real qayda «hər 10 000 km» olduğu üçün bu, zəif
> izləmədir — maşın az işlədilibsə vaxtından əvvəl, çox işlədilibsə gec xəbər verər.
>
> Rəhbər bunu bilərək qərar versin: **km-ə görə izləmə istənilirsə spidometr
> lazımdır**, başqa yolu yoxdur.

### 5.3 Xatırlatma kimə? — ✅ CAVABLANDI (19.08.2026)

İstifadəçi qərarı: **«texniki baxış və yağ dəyişmə və s. məlumatının gedəcəyi
işçiləri mən admin tərəfdə icazəni verim».**

Alıcılar **rola görə hesablanmır** — Admin `AvtoparkXeberdarliqAlicilari`
cədvəlində işçiləri bir-bir seçir (ekran: Avtopark → Müddətlər → Xəbərdarlıq
alıcıları). Rola fallback **qəsdən yoxdur**: gizli alıcı «kim xəbər aldı?»
sualını cavabsız qoyar. Siyahı boş olarsa xəbərdarlıq heç kimə getmir və bu,
həm ekranda sarı xəbərdarlıqla, həm də logda açıq göstərilir.

**Nə vaxt:** hər müddət sətrinin öz `XeberdarliqGun`-u var (növün defaultu ilə
dolur, əl ilə dəyişdirilə bilir). Bir dəfə göndərilir; **vaxtı keçmişlərdə
həftədə bir təkrarlanır** — yoxsa uzadılmayan sığorta bir bildirişdən sonra
unudulardı.

### 5.4 Xərclər bu modulda izlənəcəkmi?

Yanacaq, təmir, sığorta ödənişi — «hansı maşın ayda nə qədərə başa gəlir»
hesabatı istənilirmi?

FinNex-də **`Xerc` modulu artıq var** (kateqoriya, təsdiq axını, qəbz faylı).
Ora `MasinId` sahəsi əlavə etmək kifayətdir — yenidən qurmağa ehtiyac yoxdur.

*Bu, 4-cü mərhələdir; ilk üç mərhələni bloklamır.*

### 5.5 Müraciət edən özü rəhbərdirsə — ✅ CAVABLANDI

**Birbaşa kassaya gedir.** Rəhbər addımı atlanır, müraciət yaradılan anda
`Tesdiqlenib` statusunda olur və kassanın siyahısında görünür.

> ⚠️ **BU, TƏLƏLİ YERDİR.** Bu gün (19.08.2026) FinNex-də məhz belə bir səhv
> tapıldı: işçidə iki rol birlikdə idi, servis bir addımı atlayırdı, ekran isə
> başqa addımı gizlədirdi — nəticədə işçi öz müraciətinin harada olduğunu səhv
> görürdü.
>
> Ona görə burada qayda **bir yerdə** yazılacaq (servisdə) və ekran həmin
> mənbədən oxuyacaq — şərt markup içində təkrar qurulmayacaq.

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

| # | Nə qurulur | Nəticə | Açıq suala bağlıdırmı |
|---|---|---|---|
| **1** | `Masin` kartı + CRUD ekranı + `Kassa` rolu | maşınlar sistemdədir | **YOX — başlana bilər** |
| **2** | Müraciət → təsdiq → çıxış/qayıdış + 4 bildiriş | **əsas tələb işləyir** | **YOX — başlana bilər** |
| **3** | `MasinMuddet` + xatırlatma | sığorta/baxış unudulmur | bəli — §5.3 (kimə xəbər) |
| **4** | Xərc bağı + hesabatlar | maşın üzrə maya dəyəri | bəli — §5.4 |

**1-ci və 2-ci mərhələ açıq sualların heç birinə bağlı deyil** — cavablanmış üç
qərar (Kassa rolu, rəhbər axını, spidometrsiz) onları tam təyin edir.

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

---

## 9. QURULAN KOD (19.08.2026)

### 9.1 Cədvəllər — migration `20260819000000_Avtopark`

5 **yeni** cədvəl, mövcud dataya **toxunmur**:

| Cədvəl | Nə saxlayır |
|---|---|
| `Masinlar` | maşın kartı |
| `MasinMuracietler` | müraciət + açar (çıxış/qayıdış) jurnalı — bir cədvəldə |
| `MasinMuddetNovleri` | sığorta/baxış/yağ… növləri (5 sətir ilkin doldurulur) |
| `MasinMuddetler` | müddət qeydləri |
| `AvtoparkXeberdarliqAlicilari` | xəbərdarlığı kim alsın |

**SQL öncəsi yoxlama** (CLAUDE.md — «nə dəyişəcəyini göstər»): heç bir mövcud
cədvəl dəyişmir, heç bir sətir yenilənmir/silinmir. Yalnız `CREATE TABLE` ×5,
`CREATE INDEX` ×12 və `MasinMuddetNovleri`-yə 5 `INSERT`. Tətbiqdən sonra:

```sql
SELECT COUNT(*) FROM MasinMuddetNovleri;   -- 5 gözlənilir
SELECT COUNT(*) FROM Masinlar;             -- 0
```

**Bütün FK-lar `Restrict`-dir.** `MasinMuracietler`-də İşçiyə **dörd** ayrı FK var
(müraciətçi, rəhbər, çıxışı qeyd edən, qayıdışı qeyd edən) — Cascade qalsaydı
SQL Server «multiple cascade paths» ilə cədvəli **yaratmazdı**.

⚠️ `AppDbContextModelSnapshot.cs` **yenilənmədi** — layihədə son iki migration
(`GedenHevale_KocurmeId`, `KreditFaizDerecesi`) da snapshot-a düşməyib.
`db.Database.Migrate()` snapshot-a baxmır; snapshot yalnız `Add-Migration`
skaffoldu üçün lazımdır. Qayda dəyişsə hamısı birlikdə bərpa olunmalıdır.

### 9.2 Marşrut — TƏK MƏNBƏ

`MasinMuracietService.IlkinStatus(bool muracietSahibiRehberdirmi)` —
**qaydanın yeganə yeri**. Ekran `RehberAddimiVar(...)` ilə həmin metoddan oxuyur;
şərt view/DTO içində təkrar qurulmur. CLAUDE.md-dəki «Rol Prioriteti» tələsi
(servis bir addımı atlayır, ekran başqasını gizlədir) məhz belə bağlanır.

`MuracietSahibiRehberdirmi` **formadan qəbul edilmir** — controller
`User.IsInRole` ilə doldurur. Formadan gəlsəydi istifadəçi POST-u dəyişib öz
müraciətini avtomatik təsdiqlədə bilərdi.

### 9.3 Qoruyucular — hansı yerdə

| # | Qayda | Harada |
|---|---|---|
| 4.1 | Bir maşının eyni anda iki açıq çıxışı ola bilməz | `CixdiAsync` |
| 4.2 | Təsdiqlənmiş müraciətlər üst-üstə düşməməlidir | `YaratAsync` **və** `TesdiqEtAsync` |
| 4.3 | Qayıdış çıxışdan əvvəl ola bilməz | `GeldiAsync` |
| 4.4 | Qayıtmayanlar paneli | `Tesdiq/AcigCixislar` + Kassa ekranı |
| 4.5 | Rol prioriteti | `IlkinStatus` (tək mənbə) |
| — | Təmirdə/çıxmış maşına müraciət yazılmır | `YaratAsync` + siyahı filtri |
| — | Çöldə olan maşın «Təmirdə» edilə bilməz | `MasinService.YenileAsync` |
| — | Diri müraciəti olan maşın silinmir | `MasinService.SilAsync` |

**Üst-üstə düşmə yoxlaması iki yerdədir və hər ikisi lazımdır.** Yalnız
yaratmada olsaydı: iki nəfər eyni maşına eyni saata müraciət yazır (ikisi də
«Gözlemede» — blok yoxdur, seçim rəhbərindir), rəhbər hər ikisini təsdiqləyir →
iki paralel təsdiq, heç bir xəta çıxmır.

«Gözləmədə» olanlar **qəsdən bloklamır** — yoxsa birinci müraciət ikincini heç
kimin qərarı olmadan susdurardı.

### 9.4 Bildirişlər

`BildirisNovu` 49–54 əlavə olundu: `MasinMuraciet`, `MasinTesdiq`, `MasinImtina`,
`MasinCixis`, `MasinQayidis`, `MasinMuddetXeberdarliq`.

Hamısı **ardıcıl** yazılır (`Task.WhenAll` YOX) — `BildirisService` sorğunun ortaq
`DbContext`-ini işlədir, o isə thread-safe deyil (CLAUDE.md — paralel yazı və ölü
bildiriş). Bildiriş xətası əsas əməliyyatı pozmur, amma **izsiz də qalmır**
(`GuvenliBildirisAsync` → `ILogger`, boş `catch` yoxdur).

Çıxış/qayıdışda işçiyə **və** təsdiqləyən rəhbərə gedir; rəhbər öz müraciətini
yazıbsa (`RehberId == IsciId`) ikinci bildiriş göndərilmir.

### 9.5 Müddət xəbərdarlığı — fon xidməti

`XatirlatmaBackgroundService`-ə **7-ci bənd** əlavə olundu (yeni servis
yaradılmadı). Saatda bir işə düşür.

`MasinMuddet.XeberdarliqGonderilib` bayrağı olmasa eyni xəbərdarlıq **gündə 24
dəfə** yazılardı — `BildirisService`-in 15 saniyəlik dublikat qoruması bu qədər
uzun aralığı tutmur.

Bayraq **alıcı siyahısı boş olanda YAZILMIR** — yazılsaydı alıcı sonradan əlavə
ediləndə xəbərdarlıq heç vaxt getməzdi.

`YenileAsync`-də son tarix dəyişirsə bayraq **sıfırlanır** — yoxsa uzadılmış
müddət üçün yeni xəbərdarlıq heç vaxt getməzdi.

### 9.6 Ekranlar

| Ünvan | Kim görür |
|---|---|
| `/Avtopark/Muraciet` | bütün işçilər — öz müraciətləri |
| `/Avtopark/Tesdiq` · `/Avtopark/Tesdiq/AcigCixislar` | Rəhbər, Admin |
| `/Avtopark/Kassa` · `/Avtopark/Kassa/Jurnal` | Kassa, Rəhbər, Admin |
| `/Avtopark/Masin` (+ Detail/Create/Edit) | Admin |
| `/Avtopark/Muddet` (+ Yaxinlasanlar/Novler/Alicilar) | Admin |

Yuxarı menyuda **«Avtopark»** tab-ı — «Ümumi xidmətlər» kimi departament DEYİL,
maşından bütün şöbələr istifadə edir, ona görə «Departamentlər» dropdown-una
qoyulmadı.

Sol menyudakı linklərin şərtləri controller-lərin `[Authorize]` şərti ilə
**eynidir** — linki görüb 403 almaq istifadəçini çaşdırır.

### 9.7 Fayl saxlama

Sığorta polisi / qəbz `C:\FinNex_DMS\avtopark\` qovluğuna yazılır, bazada
**yalnız nisbi yol** (`avtopark/{guid}.pdf`). `wwwroot`-a yazılmır.
Uzantı ağ siyahısı (`.pdf .jpg .jpeg .png .doc .docx .xls .xlsx`) + 10 MB limit.

Redaktədə **fayl seçilməyibsə mövcud sənədə toxunulmur** — şərtsiz yazsaq sənəd
səssizcə silinərdi (CLAUDE.md — şərtli sahə + default parametr = səssiz data itkisi).

### 9.8 Ekran/mesaj qərarları

- Forma xətaları `TempData` ilə **YOX**, `ModelState` ilə göstərilir: layout
  `TempData`-nı `.fn-alert` kimi render edir və `user-area.js` onu **4 saniyəyə
  silir** — istifadəçi səbəbi oxumağa macal tapmır.
- Validasiya xülasəsi **şərtli** render olunur (`!ModelState.IsValid`) — yoxsa
  xətasız halda boş qırmızı qutu görünərdi (layihədə `validation-summary-valid`
  CSS qaydası yoxdur).
- Status `<select>`-ində `value` **enum ADIdır**, rəqəm yox: `asp-for` model
  dəyərini enum adı kimi render edir və `selected`-i həmin mətnə görə qoyur.
  Rəqəm yazılsaydı redaktədə cari status seçilməmiş görünərdi.

---

## 10. YOXLANMALI (istifadəçi tərəfində)

CLAUDE.md — bazaya yazma və workflow statusları istifadəçi yoxlaması tələb edir.

1. **Build** — `dotnet build` ilə 0 xəta (bu mühitdə yoxlanmayıb).
2. **Migration** — startup logunda `[Migration] ... uğurla tətbiq olundu`;
   `SELECT COUNT(*) FROM MasinMuddetNovleri` → 5.
3. **Kassa rolu** — Admin → İstifadəçi idarəetməsi: `Kassa` rolu siyahıda
   görünürmü, kassa işçisinə verilirmi.
4. **Axın** — adi işçi müraciət → rəhbər təsdiq → kassa «Çıxdı» → «Gəldi».
   Hər addımda bildiriş gəldimi.
5. **Rəhbər öz müraciəti** — addım atlanıb birbaşa kassaya düşürmü.
6. **Üst-üstə düşmə** — eyni maşına eyni saat: ikinci müraciət blok olurmu,
   xəta mətnində kimin götürdüyü yazılırmı.
7. **İkinci çıxış** — çöldə olan maşına ikinci «Çıxdı» blok olurmu.
8. **Müddət** — sığorta qeydi + uzatma (köhnəsi tarixçəyə keçirmi).
9. **Xəbərdarlıq** — alıcı əlavə edilib, son tarixi yaxın qeyd yaradılsın;
   bir saat sonra zəng ikonunda bildiriş görünməlidir.

---

## 11. SADƏLƏŞDİRMƏ (19.08.2026, istifadəçi qərarı)

İstifadəçi: **«bu çox mürəkkəb, bizdə 4-5 maşın var, işçiyə qəliz gəlməz?»**
Qərar: **«sadələşdirək, bizə lazım olan prinsip qalsın»**.

⚠️ **HEÇ BİR FUNKSİYA SİLİNMƏDİ** (CLAUDE.md qaydası). Yalnız gündəlik
işlənməyənlər gözdən yığışdırıldı və öz səhifələrinin içindən açılır.

### 11.1 Sidebar: 8 → 5 sətir

Çıxarılan linklər və indi haradan açılır:

| Çıxarılan link | İndi haradadır |
|---|---|
| Çöldə olanlar | Təsdiq panelində düymə |
| Jurnal tarixçəsi | Açar jurnalında «Tarixçə» düyməsi |
| Yaxınlaşanlar | Müddətlərdə düymə (+ Maşınlar səhifəsində) |
| Növlər | Müddətlərdə düymə |
| Xəbərdarlıq alıcıları | Müddətlərdə düymə |

Aktivlik indi **ACTION-a görə yox, CONTROLLER-ə görədir** — «Çöldə olanlar»a
keçəndə menyuda «Təsdiq paneli» seçili qalır. Əvvəlki variantda heç nə seçili
qalmazdı və istifadəçi «hardayam?» sualı ilə üzləşərdi.

**Yeni link əlavə etməzdən əvvəl:** gündəlik işlənmirsə menyuda yeri yoxdur.

### 11.2 Maşın forması: 12 sahə → 4 görünən + açılan bölmə

| Görünən | Əlavə məlumat (açılan) |
|---|---|
| Dövlət nömrəsi, Marka, Model, Status | Buraxılış ili, Rəng, Növü, Ban, VIN, Departament, Təhkim sürücü, Qeyd |

**Redaktədə bölmə AVTOMATİK açılır** əgər əlavə sahələrdən biri doludursa
(`elaveDolu`). Bu olmasa mövcud VIN/rəng/sürücü gizli qalar və istifadəçi
«yazmamışam» sanıb yenidən doldurar — yaxud doldurulmuş dəyəri görmədən saxlayar.

### 11.3 İşçinin gördüyü — dəyişmədi, onsuz da minimal idi

İşçidə sidebar-da **tək sətir** var («Müraciətlərim»). Müraciət formasında
4 sahə, ikisi hazır dolu gəlir (növbəti saat + 2 saat), «Marşrut» istəyə bağlı.
Bu, mövcud icazə müraciətindən azdır.

Gündəlik yük: işçi 1 forma → rəhbər 1 klik → kassa 2 klik (çıxdı/gəldi).
