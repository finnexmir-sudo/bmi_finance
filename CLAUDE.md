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

## Aylıq Qazanc Tarixçəsi (IsciAyliqQazanc) — Nə Düşür, Nə Düşmür (KRİTİK)

Bu cədvəl məzuniyyət ortalamasının (12 aylıq S) yeganə mənbəyidir. Yazan yer:
`MaasHesablamaService.FerdiHesabla` addım 16 → `AutoInsertFromMaasAsync`. Düstur:

```
qazanc = brutMaas + qabaqcadanTarixcePayi − mezOrtalamaXaric − xestelikSirketOdenis
```

**DAXİLDİR:**
- Əsas əməkhaqqı (davamiyyət/məzuniyyət/çıxış kəsintilərindən sonra), Overtime,
  IH-07 əlavə təminat, korreksiya gəlirləri, işəgötürən HYS payı (brutMaas tərkibində);
- "Ay sonu" məzuniyyət pulu — onsuz da brutMaas içindədir;
- **Qabaqcadan ödənilən məzuniyyət brütü** — amma **MƏZUNİYYƏT günlərinin düşdüyü aya**
  (ödənilmə ayına YOX — "qabaqcadan" pul adətən əvvəlki ayda ödənilir; çoxaylı
  məzuniyyət aylara bölünür, `qabaqcadanTarixcePayi`). Vergi bazası isə ödənilmə
  ayında qalır — bu iki attribusiya QƏSDƏN fərqlidir;
- Aylıq bonuslar — yalnız növündə `MezuniyyetOrtalamasinaDaxil=true` olanlar (default).

**DAXİL DEYİL:**
- **Xəstəlik şirkət ödənişi** — brutMaas-a daxildir, amma qazancdan ÇIXILIR
  (entity sənədi: "xəstəlik ödənişi artıq çıxılmış olmalıdır");
- Birdəfəlik ödənişlər — növündə `MezuniyyetOrtalamasinaDaxil=false` (NK Qərar 137);
- VM 98.2.1 hesabi gəlirləri (onsuz da brütə düşmür).

Real hadisə (2026-08 audit, mühasib Exceli ilə üzləşdirmə): 10 qeyd səhv çıxdı —
(a) qabaqcadan brüt ümumiyyətlə düşmürdü (İyul: 1.321,30 ≠ 2.798,55);
(b) düşəndə ödənilmə ayına düşürdü (iyun ödənişi → iyul məzuniyyəti);
(c) xəstəlik pulu daxil qalırdı (+93,21). Hamısı kodda bağlandı, keçmiş SQL ilə düzəldildi.

**Qaydalar:**
- `ElIleDaxilEdilib=1` qeydləri sistem HEÇ VAXT üstələmir — korreksiyalar belə yazılır.
  Yoxlama/düzəliş aləti: **Admin → Qazanc Matrisi** (`/HR/IsciAyliqQazanc/Matris`) —
  il üzrə işçi×12 ay, mühasib Exceli ilə müqayisə üçün; Excel çıxarışı ədədi xanalarla.
- Addım 16 düsturuna toxunanda bu siyahını tutuşdur və mühasib Exceli ilə ən azı
  bir məzuniyyətli, bir xəstəlikli ayı yoxla.

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

## Davamiyyət — Ezamiyyət Statusa TOXUNAN BÜTÜN YERLƏR (KRİTİK)

Ezamiyyət bir işçinin davamiyyət statusuna **5 ayrı yerdə** təsir edir. Ezamiyyət
qaydası dəyişəndə (yaxud "ezamiyyətli işçi səhv status alır" şikayətində) hamısını
tutuşdur — biri köhnə qalsa, xəta yalnız o yolda təzahür edir:

1. `ADMSController` — ilk punch ≥13:00 heuristikası (`bugunEzamiyyet == null` şərti);
2. `ADMSController.HesablaStatus` — giriş statusu: ezamiyyət günü **Gecikmə yazılmır**
   (giriş ≤ BitisSaati+tolerans → İşdə; ondan gec → Ezamiyyət);
3. `ADMSController` çıxış yolu — çıxış ezamiyyət BaslamaSaati ±30 dəq → status Ezamiyyət;
4. `HR/DavamiyyetController` — göstərmə qatı: bazadakı köhnə **Gecikmə** qeydi ezamiyyətlə
   örtülürsə Ezamiyyət göstərilir (KPI-lardan ƏVVƏL — say=siyahı) + `ezamiyyetGozleyen`
   sintetik sətirlər (cihaz qeydi olmayan ezamiyyətlilər);
5. `User/DavamiyyetController.HesablaAsync` — işçinin öz portalında eyni göstərmə düzəlişi.

Əlavə: `QayibMarkerBackgroundService` — qeydi olmayan ezamiyyətli gün Qayıb yox, Ezamiyyət.

**Qayıdış (11.08.2026):** saatlıq ezamiyyətdə (həm başlama, həm bitmə saatı olan) işçi
qayıdıb cihaza vuranda status `HesablaStatus` ilə yenidən hesablanır → adətən **İşdə**.
Əvvəl geri qaytaran kod yox idi: 06:58-də gələn, 10:15-də çıxıb 11:44-də qayıdan işçi
gün sonuna kimi "Ezamiyyət" qalırdı və KPI-da "Gəlib" sayılmırdı. Şərtlər dardır
(giriş < başlama saatı VƏ oxuma başlama+30 dəq-dən sonra) — **gec gələn** işçinin
Ezamiyyət statusu toxunulmur, o, gecikmə qorumasıdır. Tez çıxma qoruması itmir: status
yoxlaması ilə yanaşı müraciətin özünə baxan `ezamiyyetOrtuyur` şərti də var
(HR/DavamiyyetController:416, User/DavamiyyetController:292) — status dəyişikliyi
edəndə həmin ikiqat qorumanı yoxla.

Real hadisələr (2026-08): ezamiyyətli işçi əvvəl "Gözlənilir" görünürdü (yalnız 4/6 var
idi), sonra 14:50 qayıdışı "Çıxış" yazıldı (1 yox idi), sonra da "Gecikmə" göründü
(2 yox idi) — hər dəfə başqa yol köhnə qalmışdı.

Statusun mənbəyi **bazadakı `Davamiyyet.Status`-dur** (ADMS yazır); controller-lərdəki
düzəlişlər köhnə qeydlər üçün göstərmə qatıdır — bazanı dəyişməz. Diaqnozda əvvəl qeydin
nə vaxt yazıldığını yoxla: düzəlişdən əvvəl yazılmış qeyd yeni build-lə özbaşına dəyişməz.

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

## İcazə — Nahar Güzəşti: Güzəşt və Çıxılma AYRILMAZDIR (KRİTİK)

Qayda (10.06.2026-dan): işçi "nahara çıxmıram" seçirsə nahar fasiləsi qədər **kredit**
qazanır — icazə pəncərəsi o qədər uzun ola bilər (`maxDeq = 180 + naharDeq`), **əvəzində**
sayğacdan həmin müddət **SABİT** çıxılır (`IcazeService.NaharCixilmaSaat`). İki tərəf
bir-birini tarazlayır → **sayılan icazə heç vaxt 3 saatı keçmir**.

**Çıxılma REAL KƏSİŞMƏ (icazə ∩ nahar pəncərəsi) İLƏ HESABLANMAMALIDIR.** 24.07.2026-da
(ec0a695e) qismən nahar hallarını dəqiqləşdirmək üçün sabit çıxılma kəsişməyə keçirilmişdi.
Nəticə: nahara toxunmayan pəncərədə (məs. 14:00–17:45, nahar 13:00–13:45) kəsişmə 0 →
çıxılma 0, amma güzəşt (+45 dəq) yerində qaldı. İşçi naharda işləyir, 3s45d gedir və
sayğacdan da **3s45d** yazılır — güzəştin qarşılığı itir (illik 36 saatlıq balansdan
45 dəq artıq gedir), HR tərəfdə isə "max 3 saat" nəzarəti pozulur.

**Qayda:** güzəşt (`YaratAsync` limiti) ilə çıxılma (`NaharCixilmaSaat`) **eyni kəmiyyət**
olmalıdır. Birini dəyişirsənsə o birini də dəyiş — yoxsa məntiq sükutla sınır, heç bir
xəta verməz. Çıxılma həm PLANA, həm FAKTİKİYƏ eyni cür tətbiq olunur
(`IcazeListDto.EffektivSaat` / `EffektivFaktikiSaat`, `GetIsciIzlemeAsync`,
`GetDovriyyeAsync`, `DashboardService` illik balans, `RehberTesdiqAsync` jeton limiti) —
altısı da eyni helper-i çağırmalıdır.

## Şərtli Render Olunan Form Sahəsi + Default Parametr = Səssiz Data İtkisi (KRİTİK)

Bir checkbox/input `@if (...)` şərti ilə render olunursa və POST-u qəbul edən metod həmin
sahəni **default dəyərlə** (`bool x = false`) alıb entity-yə **şərtsiz yazırsa**, sahə
render olunmayan hallarda istifadəçinin seçimi **səssizcə silinir**. Xəta yoxdur, log yoxdur.

Real nümunə (2026-08, icazə nahar bayrağı): təsdiq səhifəsində checkbox yalnız nahar
kəsişməsi olanda göstərilirdi; kəsişməyəndə forma sahəni göndərmirdi → `RehberTesdiqAsync`
`NaharNezereAlinmasin = status && false` yazırdı → işçinin müraciətdəki seçimi bazadan itdi.
Diaqnozu çətinləşdirən: bayraq 0 olduğu üçün heç bir səhifədə "nahar seçilib" izi qalmırdı.

**Qaydalar:**
- Belə sahələri **həmişə render et** (lazım gəlsə passiv/izahlı formada), yaxud
- parametri **nullable** et (`bool?`) və `null` gələndə mövcud dəyəri **saxla**
  (`var secim = param ?? entity.Sahe;`) — `null` ("göndərilməyib") ilə `false`
  ("işarə götürülüb") fərqli mənalardır;
- checkbox-un yanına `<input type="hidden" name="eyniAd" value="false" />` qoy ki,
  işarəsiz hal da açıq şəkildə göndərilsin.

## ViewModel Non-Nullable String — Gizli Required Tələsi (KRİTİK)

.NET 8 MVC-də ViewModel-dəki **non-nullable** string (`string X = null!`) avtomatik
**Required** sayılır. Sahə formda input kimi YOXDURSA (yalnız başlıqda göstərilir),
POST-da gəlmir → ModelState hər dəfə kəsilir və form heç vaxt yadda saxlanmır.

Real nümunə (2026-07, TeyinatDeyisVM.IsciTamAd): "The IsciTamAd field is required."
xətası validasiya xülasəsində çıxırdı, amma xülasə `fn-alert` class-ında idi və
user-area.js **bütün .fn-alert-ləri 4 saniyəyə silir** → xəta görünməmiş yox olurdu,
"düyməni klikləyirəm heç nə olmur" kimi təzahür edirdi. Diaqnozu 4 mərhələ uzatdı.

**Qaydalar:**
- Display-only ViewModel sahələri **həmişə nullable** (`string?`) olsun.
- Validasiya xülasəsini `.fn-alert` class-ı ilə YAZMA — auto-hide onu silir;
  qalıcı öz class-ını işlət (nümunə: TeyinatDeyis `isci-val-summary`).
- "Submit heç nə etmir" şikayətində əvvəlcə brauzerin "Confirm Form Resubmission"
  dialoquna bax — çıxırsa POST gedir, problem serverin qaytardığı görünməz xətadadır.

## "İlin Son Günü" Aşkarlanması — Cari İl Tələsi (KRİTİK)

"TARIX ilin son əməliyyat günüdürmü?" tipli sorğu (`ildə TARIX-dən sonra gün
YOXDUR`) cari ildə **hər gün doğru çıxır** — sabahkı günlər bazada hələ mövcud
deyil. Nəticədə "il sonu" məntiqi ilin ortasında işə düşür.

Real nümunə (2026-07-29, Balans İcmalı): mənfəət 50130→50120 il-sonu keçidi
yalnız son_gun sorğusuna bağlanmışdı → iyulda "son gün" sayıldı, mənfəət boş
50120-dən oxundu və Xalis mənfəət 0 göründü (ROA/ROE də 0).

**Qayda:** Belə keçidləri tək tarix-müqayisəli sorğuya bağlama — real DATA
şərti ilə birləşdir (məs. mənfəət üçün: ay=dekabr **VƏ** 50130 qalığı=0
**VƏ** ildə sonrakı gün yoxdur). "Ən son yüklənmiş gün" ≠ "ilin son günü".

## Razor → CSS/JS Rəqəm — Mədəniyyət (az-AZ vergül) Tələsi (KRİTİK)

Server mədəniyyəti az-AZ-dır: Razor-da `@decimal` **vergüllə** render olunur
(`73,3`). Bu, insan oxuyan mətndə düzdür, amma **CSS/JS-ə gedən rəqəmdə**
etibarsızdır: `style="width:73,3%"` CSS tərəfindən atılır və zolaq **tam dolu**
görünür (2026-07, Mühasibat dashboard — bütün faiz zolaqları 100% görünürdü;
mənfi faizdə `width:-2%` də eyni nəticəni verirdi).

**Qaydalar:**
- `style`/`<script>` içinə yazılan hər rəqəmi **InvariantCulture** ilə format et.
  Mühasibat view-larında hazır helper var: `Bw(decimal)` — `min(100, |v|)` +
  invariant `"0.##"`. Yeni zolaq/width yazanda **həmişə** `width:@Bw(x)%` istifadə et.
- JS-ə data ötürəndə `JsonSerializer.Serialize` istifadə et (invariant yazır) —
  əl ilə `@decimal` interpolasiya etmə.
- İnsan oxuyan mətndə (`@x.Faiz%` etiketi) vergül qala bilər — problem yalnız
  maşın oxuyan (CSS/JS) tərəfdədir.

## Bildirişlər — Paralel Yazı və Ölü Bildiriş (KRİTİK)

Bildiriş yazan bütün yollar **ardıcıl** olmalıdır. `BildirisService` sorğunun
**ortaq `IUnitOfWork`**-unu (eyni `DbContext`) işlədir; EF Core-un `DbContext`-i
thread-safe deyil. `Task.WhenAll` ilə paralel `Add` + `SaveChanges` ya istisna
verir (və boş `catch` onu udur → bildiriş **səssizcə itir**), ya da sətri
**təkrar yazır**.

Real hadisə (13.08.2026): bir məzuniyyət müraciəti üçün rəhbərə **iki eyni
bildiriş** düşdü — 3,3 ms fərqlə. Başqa iki sətrin `YaradilmaTarixi`-si isə
tick-tick eyni idi, yəni həqiqətən paralel yazılmışdılar. Bütün cədvəldə cəmi
3 dublikat qrupu var idi — yəni qayda deyil, **yarış**; ona görə aylarla
görünmədən qalmışdı.

**Qaydalar:**
- Toplu bildirişdə `Task.WhenAll` **İSTİFADƏ ETMƏ** — `BildirisRouter.GonderAsync`
  ardıcıl `foreach` işlədir, yeni metod da onu çağırsın.
- Bildiriş xətası əsas əməliyyatı pozmamalıdır, amma **izsiz də qalmamalıdır** —
  boş `catch` yerinə `ILogger` ilə yaz.
- `BildirisService.YaratAsync` dublikat qoruması var: eyni alıcı + növ + başlıq +
  **mətn** + bağlı qeyd, son 15 saniyədə → yazılmır. Pəncərə qəsdən dardır;
  sonrakı mərhələ bildirişləri (təsdiq/imtina/ödəniş) dəqiqələr sonra gəlir və
  bloklanmır. Mətn açara **qəsdən** daxildir ki, eyni başlıqlı fərqli hadisələr
  (məs. eyni anda təyin edilən iki tapşırıq) bir-birini bloklamasın.

### Ləğv olunan qeydin bildirişləri

Məzuniyyət ləğv ediləndə **yumşaq silinir**, amma bildirişlər avtomatik getmir.
Təmizlik ləğvin **hər iki giriş nöqtəsində** var — `LegvEtAsync` (işçi) və
`HrLegvEtAsync` (HR) → `MezuniyyetBildirisleriniSilAsync`. Biri unudularsa
xəta yalnız o yolda təzahür edər.

**Yalnız `MezuniyyetMuraciet` növü silinir/süzülür** — bu, "sənə iş gəlib,
təsdiq et" bildirişidir və müraciət yoxdursa mənasızdır. `MezuniyyetImtina`
(HR ləğv etdi / Mühasibə "ödənişi icra etməyin") və `MezuniyyetTesdiq`
bildirişləri məhz məzuniyyət silinəndən **SONRA** yaradılır və `MezuniyyetId`-si
silinmiş qeydə baxır — növ şərti olmasa süzgəc onları da gizlədərdi və işçi
"məzuniyyətiniz ləğv edildi" xəbərini heç vaxt görməzdi.

Göstərmə qatındakı süzgəc (`DiriBildirislerAsync`) keçmiş qalıqlar üçün ikinci
qatdır; **siyahı və say eyni süzgəcdən keçir** (say = siyahı qaydası).

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

### İSTİSNA YOXDUR — Oracle 100% oxunur (12.08.2026-dan)

Əvvəl kredit müqaviləsi modulu üçün **iki** Oracle cədvəlinə yazı icazəli idi.
Hər ikisi FinNex-ə köçürüldü, istisna **tamamilə bağlandı**:

| Köhnə Oracle yazısı | İndi haradadır |
|---|---|
| ~~`odb.xaric_mektub`~~ (INSERT) | `XaricMektub` — `XaricMektubService.YaratAsync` |
| ~~`odb.muqavile_nomreleri`~~ (UPDATE/INSERT) | `MuqavileSayghaci` — `IMuqavileSayghacService` |

`KreditMuqavileNomreService` artıq Oracle-a bağlanmır (`OracleConnection` yoxdur).
Nömrələmə də, məktub qeydi də tək yerdən — öz bazamızdan idarə olunur.

**Qayda:** Oracle-a **heç bir yazı** əlavə edilə bilməz — nə birbaşa, nə dolayı.
`KreditMuqavile:NomreYaz = false` (default) olduqda sayğaclar da, məktub da
**yazılmır** (preview); yalnız yoxlamadan sonra `true` edilir.

### Müqavilə Sayğacları — Semantika Fərqi (KRİTİK)

BMI-də `odb.muqavile_nomreleri` sütunları **iki fərqli mənada** işlənirdi:
- `KR_ZAMINLIK`, `KR_MENZIL` və digərləri → **NÖVBƏTİ** nömrə (kod dəyəri olduğu
  kimi işlədir, sonra +1 yazır);
- `KR_ZAMINLER` → **SONUNCU** verilmiş nömrə (`kr_zaminler + i` ilə işlənir).

FinNex-də `MuqavileSayghaci.SonNomre` **həmişə sonuncudur** (`EmrSayghaci` ilə eyni
qayda), növbəti = `SonNomre + 1`. Köçürmə zamanı "növbəti saxlayan" sayğaclardan
**1 çıxılır** (`MuqavileSayghacService.Novler` cədvəlindəki `OracleNovbetiSaxlayir`
bayrağı). Bu bayrağa toxunanda köçürmə ekranındakı **Növbəti** sütununu BMI-nin
verəcəyi nömrə ilə tutuşdur — bir vahid sürüşmə bütün müqavilə nömrələrini pozar.

### Jurnal Nömrəsi Öz Bazamızdan Verilirsə — ƏVVƏLCƏ İDXAL (KRİTİK)

FinNex-də jurnal nömrəsi (məktub Qeydiyyat №, həvalə №) **həmin ilin FinNex
sətirlərindən max+1** ilə hesablanır. Həmin ilin BMI datası hələ idxal
edilməyibsə nömrə **1-dən başlayır** və köhnə nömrələrlə toqquşur.

**Qayda:** bir jurnaldan real nömrə verməzdən əvvəl (məs. `KreditMuqavile:NomreYaz = true`
edilməzdən əvvəl) **ən azı cari il idxal edilmiş olmalıdır** — SenedDovriyyesi →
BMI-dən köçürmə. `NomreYaz=false` preview rejimində risk yoxdur (heç nə yazılmır),
amma preview-də görünən nömrə də natamam idxalda yanlış olar.

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
