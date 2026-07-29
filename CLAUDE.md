# BMI Finance — Claude Qaydaları

## Sessiya Başlanğıcı — MƏCBURI YOXLAMA

Hər yeni sessiya açılanda, hər hansı iş başlamadan əvvəl **mütləq** aşağıdakı əmri işlət:

```bash
git fetch --all && git log --oneline --remotes ^main --no-walk=unsorted 2>/dev/null | head -20
```

Əgər nəticə boş deyilsə — yəni `main`-ə merge olunmamış remote branch-lar varsa — istifadəçiyə bildir və nə etmək lazım olduğunu soruş. Yeni iş başlatma.

Sessiyanın **sonunda** (işlər tamamlananda) mütləq:
1. Feature branch-ı `main`-ə merge et
2. Hər iki branch-ı `push` et
3. `git log --oneline origin/claude/* ^main` ilə yoxla — nəticə boş olmalıdır

## Ümumi Prinsiplər

### Kod yazmadan əvvəl
- İlgili bütün faylları oxu. Bir faylda düzəliş həmişə digər faylları da təsir edir.
- Dəyişikliyin bütün nəticələrini düşün — yalnız birbaşa yox, dolayı təsirləri də.
- Əgər tam əmin deyilsənsə, kodu yazma — istifadəçiyə sual ver.

### Kod yazarkən
- Hər dəyişikliyin real sistemdə necə işləyəcəyini izlə.
- "Elə bilirəm işləyir" yox — ya isbat et, ya da açıq qeyd et ki, yoxlama lazımdır.
- Maliyyəyə, maaşa, balansa toxunan hər dəyişiklik xüsusi diqqət tələb edir — iki dəfə düşün.

### Kod yazdıqdan sonra
- Dəyişikliyin əvvəl ehtimal etdiyin kimi işlədiyini yoxla.
- Yalnız sintaktik düzgünlük yox — real məntiq düzgünlüyünü yoxla.
- Test nəticəsini istifadəçiyə bildirməzdən əvvəl özün əmin ol.

## Maliyyə və Maaş Hesablamaları

- Payroll hesablamaları tarix aralığı əsasında işləyir — `IsGunlerininSayi` sıfırlamaq kifayət deyil, query-dən filtrlənməlidir.
- Məzuniyyət dəyişikliklərindən sonra `TopluHesabla` səhifəsini real data ilə yoxla.
- İkiqat sayılma riskini hər zaman nəzərə al (korreksiya + orijinal).
- SQL migration vermədən əvvəl `SELECT` ilə nə dəyişəcəyini göstər.

## EF Core — Filtered Include + Tracking Tələsi (KRİTİK)

Tracking ilə işləyən sorğuda `Include(x => x.Nav.Where(...))` (filtered include)
istifadə edirsənsə və **eyni `DbContext`-də** sonradan həmin entity tipini başqa
bir tracking sorğusu ilə yükləyirsənsə — EF Core "relationship fixup" həmin
əlavə sətirləri birinci sorğunun naviqasiya kolleksiyasına **avtomatik yapışdırır**
və filtered Include effektsiz qalır.

Real nümunə (MezuniyyetBalans): `isciler` sorğusu yalnız cari ilin balansını
yükləmək üçün `Include(...Where(b => b.Il == cariIl))` istifadə edirdi, amma
sonrakı `butunBalanslar` sorğusu (tracking) bütün illəri yüklədi. Nəticədə
işçinin əvvəlki illərinin balansı naviqasiyaya düşdü və view onu cari il kimi
göstərdi — cari il balansı olmayan işçidə əvvəlki ilin günlərini "2026" kimi
göstərdi.

**Qaydalar:**
- Yalnız oxumaq üçün olan sorğularda həmişə `.AsNoTracking()` istifadə et.
- Eyni context-də eyni entity tipini iki dəfə yükləyirsənsə, ən azı sonrakı
  sorğuda `.AsNoTracking()` qoy ki, fixup baş verməsin.
- View/servisdə naviqasiyadan oxuyanda filtri **bir daha** tətbiq et
  (məs. `b.Il == secilmisIl`) — yalnız Include filtrinə güvənmə.

## Metod İmzası Dəyişikliyi — İnterfeys + İmplementasiya + Çağırış (KRİTİK)

Bir servis metodunun imzasına parametr əlavə edəndə **üç yeri eyni anda** yenilə:

1. **İnterfeys** (`I<Modul>Service.cs`)
2. **İmplementasiya** (`<Modul>Service.cs`)
3. **Bütün çağırış yerləri** (Controller-lər və başqa servislər)

Real nümunə (RehberTesdiqAsync): `birdefelik` parametri implementasiyaya
(`IcazeService`) və controller-ə (`TesdiqController`) əlavə edildi, amma
**interfeysə əlavə edilmədi**. C#-da default dəyəri olsa belə, 7 parametrli
metod 6 parametrli interfeys üzvünü implementasiya **etmir** — ayrı imzadır.
Nəticədə `FinNex.Application` build olmadı (CS0535 + CS1501), bu da `FinNex.UI`-ı
**köhnə DLL-ə** bağladı və əlaqəsiz görünən kaskad xəta verdi (CS1061
`FaktikiSaat` tapılmır — halbuki DTO-da var idi).

**Qaydalar:**
- İmza dəyişikliyindən sonra "işləyir" demə — `dotnet build` ilə **0 xəta**
  olduğunu isbat et. Build mümkün deyilsə, üç qatın imzasını əl ilə tutuşdur
  və yoxlanmadığını açıq qeyd et.
- Bir layihə build olmayanda asılı layihələrdəki xətalar yalançı istiqamətə
  yönəldə bilər — **kök səbəb həmişə build olmayan layihədədir**, oradan başla.

## Məzuniyyət Təsdiq Axını — İki Yerdə Dublikat Routing (KRİTİK)

Məzuniyyət müraciətinin **ilkin təsdiqçisini** (şöbə rəisi / rəhbər) təyin edən
routing məntiqi **İKİ ayrı yerdə** var:

1. `MezuniyyetService.YaratAsync` — işçi əvəzedici **seçməyəndə** birbaşa işləyir.
2. `EvezediciTesdiqService.QebulEtAsync` — işçi əvəzedici **seçəndə**, əvəzedici
   qəbul edəndən sonra müraciəti növbəti mərhələyə keçirən yer.

Routing qaydasını dəyişəndə (məs. "şöbə rəisi məzuniyyətdədirsə addımı atla,
Rəhbərə keç") **HƏR İKİ yeri eyni anda yenilə**.

Real nümunə (2026-07): "şöbə rəisi məzuniyyətdədirsə keç" yoxlaması yalnız
`YaratAsync`-də var idi. `EvezediciTesdiqService` isə şöbə rəisinin yalnız
**MÖVCUDLUĞUNU** yoxlayırdı (məzuniyyətdə olub-olmadığını yox). Nəticədə
**əvəzedici seçən** işçinin müraciəti, əvəzedici qəbul edəndən sonra
məzuniyyətdə olan şöbə rəisinə ilişib qaldı. Əsas yolda yoxlama düz idi — səhv
yalnız əvəzedici yolunda görünürdü, ona görə diaqnoz çətinləşdi.

**Qayda:** Bir müraciətin təsdiq axınının birdən çox giriş nöqtəsi (birbaşa /
əvəzedici / birbaşa qeyd) varsa, status/routing qaydasını dəyişəndə hamısını
tutuşdur — biri köhnə məntiqlə qalarsa, xəta yalnız o yolda təzahür edər.

## İşçi Siyahıları — Sıralama və Filtr Qaydası (KRİTİK)

İşçi siyahısı göstərən **hər** səhifədə eyni qayda tətbiq olunmalıdır — mənbə
kanonik nümunə: `IsciSiralamaService`.

- **Sıralama**: həmişə `OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)`.
  "İşçi Sıralaması" səhifəsində HR drag-and-drop ilə `Sira`-nı təyin edir — bütün
  siyahılar həmin sıraya tabe olmalıdır (ad/soyad əlifbası yalnız eyni `Sira`-da).
- **Aktiv filtr**: aktiv işçi siyahılarında `x.Status == IsciStatus.Aktiv && !x.Silinib`.
  (Passiv/məzuniyyətdə/işdən çıxmış işçilər aktiv siyahıda görünmür.)
- Yeni işçi siyahısı yazanda bu iki qaydanı **əl ilə əlavə etmə** — mövcud
  `IsciSiralamaService` / `IsciService.HamisiniGetirAsync` sıralamasını təkrarla.

## Kredit Hesabatları — Açıq/Bağlı (date_close) vs Qalıq (KRİTİK)

Kredit siyahılarında/saylarında filtr **`date_close IS NULL` (açıq müqavilə)**
üzrə olmalıdır — **qalığa (`summa+summa_19 = 0`) görə YOX**. Kreditin əsas qalığı
0 olsa belə, müqavilə **açıqdırsa** hesabatda görünməli və sayılmalıdır (balansdankənar
`b/k`-da və ya faizdə qalığı ola bilər). Aqreqat `count(*)` (açıq üzrə) ilə drill-down
siyahısı **eyni prinsiplə** getməlidir — biri qalığı 0 olanı atsa, say ≠ siyahı olur.

Real nümunə (2026-07, Kredit Keyfiyyəti): aqreqat "GİROVSUZ · 3" göstərdi, drill-down
isə 1 sətir (2 kreditin əsas qalığı 0 idi). Səhv həll: `qaliq<>0` filtri (say düşdü).
Düzgün həll: hər ikisi `date_close` (açıq) üzrə — qalığı 0 olan açıq kredit də görünür,
say tutuşur. `arh_licschkre` sorğularında `(date_close is null or date_close > TARIX)`
kifayətdir; drill-down servisində `if (qaliq == 0) continue` **qoyma**.

## Davamiyyət — İcazəli KPI vs Drill-down Siyahısı (KRİTİK)

Davamiyyət səhifəsində "İcazəli" KPI kartı **İKİ qrupdan** ibarətdir:
1. `icazeliIndi` — cihazda **faktiki çıxıb** icazədə olanlar (`umumi`-də qeydi var).
2. `icazeGozleyen` — təsdiqlənmiş icazəsi olan, amma həmin gün **cihaz qeydi OLMAYAN**
   işçilər (məs. işə gəlməyib, adından login olub icazə yazılıb və təsdiqlənib).

Kart sayı hər iki qrupu toplayır, amma drill-down siyahısı yalnız `umumi`-dən qurulurdusa,
ikinci qrup siyahıya **düşmür** → **kart 1, siyahı boş** (2026-07, real hadisə).

**Qayda:** İcazəli filtri (`status=4`) seçiləndə siyahıya `icazeGozleyen` işçilər üçün
**sintetik sətir** əlavə et (ad/departament `Isci`-dən, giriş/çıxış null, status İcazəli).
Say və siyahı **eyni `icazeGozleyenIds` mənbəsindən** getməlidir — biri toplayıb, o biri
atsa, say ≠ siyahı olur (eyni prinsip kredit hesabatlarındakı `date_close` tələsi kimidir).

## Balans — Bağlı Hesab (date_close_licsch) Filtri (KRİTİK)

Oracle GL balans sorğularında hesab adı/dep_tip üçün `licsch` cədvəlinə join edilir.
Bu join-a **`(ch.date_close_licsch IS NULL OR ar.date_oper <= ch.date_close_licsch)`**
şərti əlavə etmək TƏHLÜKƏLİDİR: `arh_saldo_ls`-də sətri (qalığı) olan bağlı hesab
**real aktivdir** — GL-də qalıq varsa, pul oradadır. Bu filtr onu balansdan atır və
balansı pozur.

Real nümunə (2026-07, 15/07/2026): filtr bağlı, amma qalığı olan **1 kredit-faiz
hesabını (49.92 AZN)** aktivlərdən atırdı. Nəticədə "Kredit üzrə faizlər" 188 522.52
əvəzinə 188 472.60 görünürdü **və** balans yoxlaması `Aktiv − (Öhdəlik+Kapital) =
−49.92` verirdi (bağlanmırdı). Atılan məbləğ (49.92) düz balans fərqinə (−49.92)
bərabər idi — filtri götürəndə həm faiz düzəldi, həm balans dəqiq bağlandı.

**Qaydalar:**
- Balans (Aktiv=Öhdəlik+Kapital) sorğularında `date_close_licsch` ilə **filtrləmə**.
  `saldo_ish_nacval <> 0` özü kifayətdir — qalığı olmayan artıq düşür.
- Bir balans sətrinin rəqəmi report ilə tutuşmursa, əvvəlcə **balans yoxlaması
  fərqini** yoxla: sətir fərqi çox vaxt həmin fərqə bərabərdir və kök səbəb ümumi
  bir filtrdir.
- Diaqnostik SELECT verəndə dashboard-un **bütün** filtrlərini təkrarla — natamam
  diaqnostik yanlış "düz/səhv" nəticəsinə aparır.

## Xəta Etirafı

- Səhv aşkar olarsa dərhal bildirr — gizlətmə, bəhanə axtarma.
- Nə səhv olduğunu, niyə olduğunu, necə düzəldildiyini izah et.
- Eyni tip səhvin bir daha olmaması üçün bu fayla əlavə et.

## İstifadəçi Yoxlaması Tələb Olunan Hallar

Aşağıdakı dəyişikliklərdən sonra mütləq istifadəçi yoxlamasını gözlə:
- Maaş hesablama məntiqi
- Məzuniyyət balansı
- Verilənlər bazasına yazma (INSERT/UPDATE/DELETE)
- Workflow statusları (təsdiq/imtina axını)
- Vergi hesablamaları

## Arxitektura (Clean Architecture / Service Layer)

Layihə tədricən Clean Architecture-ə keçirilir. Yeni kod yazarkən və mövcud kodu dəyişdirərkən aşağıdakı qaydalara riayət et:

### Controller qaydaları
- Controller-lər **yalnız** `IService` interfeysini inject edir, `IUnitOfWork` və ya `DbContext` inject etmir
- Controller metodları **yalnız** DTO qəbul edir və DTO qaytarır — Entity birbaşa View-a və ya JSON-a verilmir
- Biznes məntiqi (hesablama, status dəyişikliyi, bildiriş göndərmə) Controller-də yazılmır

### Service Layer qaydaları
- Hər modul üçün `FinNex.Application/Services/<Modul>/I<Modul>Service.cs` interfeysi olur
- Implementasiya `FinNex.Application/Services/<Modul>/<Modul>Service.cs`-də yazılır
- Servis `IUnitOfWork` istifadə edir, `DbContext`-ə birbaşa müraciət etmir

### DTO qaydaları
- DTO-lar `FinNex.Application/DTOs/<Modul>/` qovluğunda saxlanılır
- `<Ad>Dto` — oxumaq üçün (GET cavabları)
- `<Ad>CreateDto` / `<Ad>UpdateDto` — yazmaq üçün (POST/PUT)
- Entity property-ləri DTO-ya manual map olunur (AutoMapper yoxdur)

### Refaktor strategiyası
- Köhnə Controller-lər **hissə-hissə** dəyişdirilir — birdən hamısı deyil
- Hər dəfə bir Controller seçilir: Servis → DTO → Controller → Test → Commit
- Köhnə işləyən kod, yeni kod hazır olana qədər toxunulmaz qalır

## Oracle Verilənlər Bazası — KRİTİK QAYDA

Layihədə ikinci bir verilənlər bazası mövcuddur: **Oracle (BMI)**

- Bağlantı: `DATA SOURCE=BMI;USER ID=FOXPRO;Password=...` (secrets.json-da saxlanır)
- Bu baza **yalnız oxumaq** üçündür — **YALNIZ SELECT** icazəlidir
- **INSERT, UPDATE, DELETE, DDL — QƏTI QADAĞANDIR**
- Oracle-a yazma əməliyyatı heç bir halda edilməməlidir — nə birbaşa, nə dolayı
- Bütün Oracle sorğuları `IOracleService` vasitəsilə icra olunur
- Oracle sorguları `OracleSorgular` cədvəlində saxlanır (SQL Server-də), oradan oxunur

### İSTİSNA — Kredit müqavilə nömrələri (yalnız 2 cədvəl)

Kredit müqaviləsi modulu üçün **yalnız aşağıdakı iki cədvələ** yazı (INSERT/UPDATE)
icazəlidir. Səbəb: BMI (köhnə desktop) və FinNex bir müddət paralel işləyəcək və
müqavilə nömrələri **eyni Oracle sayğacından** verilməlidir ki, nömrələr toqquşmasın.

- `odb.muqavile_nomreleri` — müqavilə nömrə sayğacları (UPDATE, seed üçün INSERT)
- `odb.xaric_mektub` — girova düşmə (BTİ) məktub jurnalı (INSERT)

Qaydalar:
- Bu yazılar **yalnız** `IKreditMuqavileNomreService`-də olur — başqa yerdə Oracle yazısı QADAĞANDIR.
- `IOracleService` hələ də **yalnız SELECT**-dir, dəyişdirilmir.
- Bütün yazılar parametrli (`OracleCommand` bind) və atomik (`SELECT ... FOR UPDATE`) olmalıdır.
- `KreditMuqavile:NomreYaz = false` (default) olduqda servis **heç nə yazmır** (preview);
  yalnız real yoxlamadan sonra `true` edilir.
- **Bu iki cədvəldən başqa Oracle-a heç bir yazı əlavə edilə bilməz.**

## Texnoloji stack
- ASP.NET Core MVC, Areas: HR / User / Admin
- EF Core, IUnitOfWork + IRepositoryAsync pattern
- SQL Server (əsas baza — yazma/oxuma)
- Oracle (BMI) — **yalnız oxuma**, `IOracleService` vasitəsilə
- Identity (AppUser, int PK)
- Azərbaycan dili — bütün UI mətnləri Azərbaycan dilindədir

## Fayl Yükləmə — SƏNƏD SAXLAMA QAYDASI

### ✅ ƏSAS QAYDA — KƏSİN RİAYƏT EDİLMƏLİDİR

**Bütün yüklənən fayllar `C:\FinNex_DMS\` qovluğuna yazılmalıdır.**

- `wwwroot`-a fayl **YAZILMAZ** — publish edildikdə silinir, bu dəyişdiriləcək
- Hər modul öz alt qovluğuna yazır
- Konfiqurasiya mənbəyi: `appsettings.json → DocumentStorage:RootPath`

### Düzgün istifadə nümunəsi

```csharp
// ✅ DÜZGÜN — həmişə belə yaz
var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
var dir = Path.Combine(dmsRoot, "modul-adi");
Directory.CreateDirectory(dir);
var fileName = $"{Guid.NewGuid()}{ext}";
await using var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
await file.CopyToAsync(fs);

// ❌ SƏHV — wwwroot istifadə etmə
var dir = Path.Combine(_env.WebRootPath, "uploads", "modul");
```

### Qovluq strukturu — `C:\FinNex_DMS\`

| Qovluq | Modul | Status |
|--------|-------|--------|
| `dovlet-vezife\` | Məzuniyyət — dövlət vəzifəsi sənədləri | ✅ Düzgün |
| `senedler\yyyy\MM\` | Sənəd dövriyyəsi (SenedService) | ✅ Düzgün |
| `elanlar\` | Elan şəkilləri/sənədləri | ⚠️ Hələ wwwroot-da |
| `fakturalar\` | Xərc fakturaları (HR) | ⚠️ Hələ wwwroot-da |
| `xercler\` | Xərc sənədləri (User) | ⚠️ Hələ wwwroot-da |
| `kredit-qerarlar\` | Kredit komitə qərarları | ⚠️ Hələ wwwroot-da |
| `chat\` | Chat qoşmaları | ⚠️ Hələ wwwroot-da |
| `hr-qanun\` | HR məsləhətçi qanun faylları | ⚠️ Hələ wwwroot-da |
| `hr-qaydalar\` | HR məsləhətçi qaydalar | ⚠️ Hələ wwwroot-da |

### ⚠️ İslahedilməli fayllar (wwwroot → FinNex_DMS)

Aşağıdakı controller-lər hələ `wwwroot`-a yazır — dəyişdirilməlidir:

1. `ElanController.cs` → `wwwroot/uploads/elan/` → `FinNex_DMS/elanlar/`
2. `XercController.cs` (HR) → `wwwroot/uploads/fakturalar/` → `FinNex_DMS/fakturalar/`
3. `XercController.cs` (User) → `wwwroot/uploads/xercler/` → `FinNex_DMS/xercler/`
4. `KreditMuracietController.cs` → `wwwroot/Files/Kredit/Qerarlar/` → `FinNex_DMS/kredit-qerarlar/`
5. `ChatController.cs` → `wwwroot/uploads/chat/` → `FinNex_DMS/chat/`
6. `HRMeslehetciController.cs` → `wwwroot/uploads/hr-qanun/` + `hr-qaydalar/` → `FinNex_DMS/hr-qanun/` + `FinNex_DMS/hr-qaydalar/`

### Yeni modul yazarkən

Yeni bir sahədə fayl yükləmə lazım olarsa:
1. `DocumentStorage:RootPath` konfiqurasiyasından oxu
2. `FinNex_DMS\{yeni-modul-adi}\` alt qovluğu yarat
3. `Directory.CreateDirectory(dir)` ilə qovluğu avtomatik yarat
4. Faylı yaz, DB-yə **yalnız nisbi yolu** saxla (məs: `dovlet-vezife/abc123.pdf`)
5. Faylı serve etmək üçün `Program.cs`-dəki `/dms` static file middleware-i istifadə et
