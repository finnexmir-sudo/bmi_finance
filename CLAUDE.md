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

## Qabaqcadan Məzuniyyət — Vergi Bazası AYLARA BÖLÜNÜR (27.08.2026, KRİTİK)

Mühasibin uçot modeli qəbul edildi: qabaqcadan ödənilən məzuniyyətin brütü
**ödənilmə ayına toplu YOX**, məzuniyyətin düşdüyü **aylara bölünərək** vergi
bazasına daxil edilir. Kəsim: `appsettings → Mezuniyyet:AvansAylaraBolunmeBaslama`
(2026-08-01). **Geri qayıtmaq:** həmin açarı `2099-01-01` et — köhnə kod silinməyib,
şərtin o biri qolunda durur.

Real nümunə (Rüfət C., 31.08–11.09, brüt 365,37 / net 308,73, 27.08-də ödənilib):

| | Avqust | Sentyabr |
|---|---|---|
| KÖHNƏ vergi bazası | 1 127,27 (761,90 + **365,37**) | 472,73 |
| YENİ vergi bazası | **800,00** (761,90 + 38,10) | **800,00** (472,73 + 327,27) |
| **NET (dəyişmir!)** | **466,80** | **422,47** |

**İŞÇİYƏ ÖDƏNİLƏN MƏBLƏĞ HƏR İKİ MODELDƏ EYNİDİR.** Düstur (`FerdiHesabla` addım 11):
`net = brutMaas − [vergilər(brutMaas + pay) + avans − (pay − payın neti)]`.
«Payın neti» elə vergili və vergisiz bazanın fərqi kimi təyin olunub, ona görə iki
model riyazi olaraq eyni nəticəyə gəlir. Dəyişən **yalnız bəyan olunan baza və
tutulmalardır**. Model dəyişikliyini «maaş dəyişəcək» kimi təqdim etmə.

### İKİ BÖLGÜ VAR — QARIŞDIRMA

Eyni 365,37 iki cür bölünür və **hər ikisi cəmdə 365,37 verir**:

| Bölgü | Avqust | Sentyabr | Harada |
|---|---|---|---|
| **Təqvim günü** (`slice.Secilen`) | 30,45 | 334,92 | «Aya düşən pay» sütunu |
| **İş günü** (`slice.EH`) | 38,10 | 327,27 | vergi bazası, əvəzləşmə, qazanc tarixçəsi |

Sentyabrda təqvim payı (334,92) iş günü payından (327,27) **BÖYÜKDÜR** — tərəzidir,
avqustun qazandığını sentyabr itirir. «Biri səhvdir» demə.

**`30,45` (brüt, təqvim) ilə `32,20` (net, iş günü) müqayisə olunan rəqəmlər DEYİL.**
`30,45 − vergi` yazmaq **səhvdir** — real hadisə: mühasib `=200+30,45` yazdı, 468,55
çıxdı, düzgünü 466,80 idi. Əvəzləşməyə **həmişə `EvezlesmeNet`** yazılır.

### NORMALLAŞDIRICI `ΣEH`-dir, `CemiOdenis` DEYİL

`pay = ödənilənBrüt × EH / ΣEH`. ÜSUL B qalib gələndə `ΣEH = CemiOdenis` olur və pay
elə `EH`-in özüdür. **ÜSUL A qalibdirsə `ΣEH ≠ CemiOdenis`** — `CemiOdenis`-ə bölsən
payların cəmi ödənilən brütdən **az** çıxar və vergi bazası, əvəzləşmə, qazanc
tarixçəsi **səssizcə əskik** yazılar. Bu səhv yazılış anında edildi və düzəldildi.

### BİR KƏMİYYƏTƏ ÜÇ YAZICI — HAMISINI BİRLİKDƏ DƏYİŞ

Bu rəqəmin **üç** müstəqil hesablayıcısı var. Yalnız birini dəyişmək **kifayət etmir** —
real hadisə: servis dəyişdirildi, ekran hələ `1 127,27` göstərirdi:

1. `MaasHesablamaService.FerdiHesabla` — **əsl hesablama** (yazılan dəyər);
2. `MaasController` (TopluHesabla önizləmə datası) — `data-mav-*` atributları;
3. `wwwroot/js/maas-toplu.js` — serverin düsturunu **təkrarlayır**, amma məbləğləri
   #2-dən oxuyur → **JS düsturuna toxunmaq lazım deyil**, #2-nin göndərdiyi rəqəmi düzəlt.

Kəsim şərti `IMaasHesablamaService.AvansAylaraBolunurmu(il, ay)` ilə **tək yerdən**
oxunur — controller öz nüsxəsini saxlamamalıdır.

Ay-ay payların (brüt/vergi/net) **yeganə mənbəyi**:
`MaasHesablamaService.MezuniyyetAvansAyPaylariAsync`. Həm maaş, həm Mühasib
Detail səhifəsi onu çağırır. Netlərin cəmi **ödənilmiş NET-ə qəpiyinə** bağlanır
(qalıq son aya yazılır) — yoxsa bank köçürməsi ilə 1–2 qəpik fərq qalar.

### QƏSDƏN KÖHNƏ (KASSA) QALAN YERLƏR

Bunlar **bilərəkdən** dəyişdirilməyib — pulun həqiqətən çıxdığı ayı yazırlar:

- `MaasController.MezQabaqcadanBrutMapAsync` — **provodka** (xərc sətri);
- `MaasController` Maas Detail başlığı — `ViewBag.TamGross` / `TamNet`.

Yəni «ödənilmə ayı» filtri artıq önizləmə sorğusu ilə **eyni deyil**. Uçot tərəfi
bunları da aylara bölmək istəyirsə — **mühasibin qərarıdır**, özbaşına dəyişmə.

### Qazanc tarixçəsi də iş günü bölgüsündədir

`IsciAyliqQazanc` (məzuniyyət ortalamasının **yeganə** mənbəyi) də `EH` bölgüsünə
keçdi: avqust **800,00**, sentyabr **800,00** (əvvəl 792,35 / 807,65). Mühasib Exceli
ilə tutuşdu (`2026 Əmək haqqı.xls`, 08-2026, sətir 22 — «Cəmi hesablanmış aylıq
ödənişlər» 800,00). **Gələcək məzuniyyət hesablamaları buna görə dəyişir.**
Kəsimdən əvvəlki aylar toxunulmayıb; keçmişi düzəltmək üçün ayrıca SQL lazımdır
(hələ verilməyib).

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

## Namespace Adı Entity Adını Kölgələyir (CS0118) — Kaskad Build Xətası (KRİTİK)

C#-da alt namespace adı, valideyn namespace-dəki **tip adı** ilə eyni olarsa, həmin
valideyn namespace-in bütün fayllarında o ad artıq **tipi yox, namespace-i** göstərir
(CS0118: "is a namespace but is used like a type"). Fayllar bir-birinə toxunmasa belə.

Real nümunə (13.08.2026): BMI `kurval` cədvəli üçün `FinNex.Application.Services.Valyuta`
namespace-i yaradıldı. Mövcud `FinNex.Application/Services/ValyutaService.cs` faylı
`FinNex.Application.Services` namespace-indədir və `Repository<Valyuta>()` yazır —
burada `Valyuta` **entity**-dir (`FinNex.Domain.Entities.Valyuta`). Yeni alt namespace
həmin adı kölgələdi → `FinNex.Application` build olmadı → `FinNex.UI` və `FinNex.Tests`
**CS0006** verdi ("Metadata file 'FinNex.Application.dll' could not be found").
Görünən xəta 4 layihədə idi, kök səbəb isə tək bir qovluq adında.

**Qaydalar:**
- Yeni qovluq/namespace adı seçəndə əvvəlcə yoxla ki, həmin adda **entity/DTO/servis
  tipi** yoxdur: `grep -rn "class <Ad>\b\|record <Ad>\b\|enum <Ad>\b" --include=*.cs`.
- Toqquşma varsa namespace-i başqa cür adlandır (mənbə cədvəlin adı yaxşı seçimdir —
  burada `Kurval`), tipi yenidən adlandırma.
- CS0006 ("Metadata file … .dll could not be found") **əsl xəta deyil** — asılı olduğu
  layihə build olmayıb deməkdir. Həmişə build olmayan layihənin **öz** xətasından başla.

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

## Üst-Üstə Düşən Məzuniyyət — Yoxlama BÜTÜN Giriş Nöqtələrində (KRİTİK)

İşçi eyni tarixlərə **iki məzuniyyət** yaza bilməz — balansdan ikiqat gün düşür.
18.08.2026-ya qədər bu yoxlama **yalnız HR-ın «Geriyə qeyd» axınında** var idi;
işçinin öz müraciətində (`YaratAsync`) **yox idi**. Test: Anar 20–24.08.2026 üçün
iki eyni müraciət göndərdi, hər ikisi təsdiqləndi, balans **53 → 43** oldu.
Heç bir xəta çıxmadı.

İndi qayda ortaq metoddadır — `MezuniyyetService.TarixKonfliktiTapAsync`.
Tətbiq olunan **beş** yer:

| Giriş nöqtəsi | `xaricId` |
|---|---|
| `YaratAsync` (işçi müraciəti) | yox |
| `GeriyeQeydYaratAsync` (HR) | yox |
| `YenileAsync` (HR redaktə) | qeydin özü |
| `HrTarixDeyisAsync` | qeydin özü |
| `AdminTarixDeyisAsync` | qeydin özü |

**Yoxlama TAM BƏRABƏRLİYƏ yox, KƏSİŞMƏYƏ baxır** — klassik interval düsturu
(`A1 <= B2 && A2 >= B1`). Mövcud **20–24.08** üçün:

| Yeni aralıq | Nəticə |
|---|---|
| 20–24 (eyni) | ✗ blok |
| 22–26 (qismən) | ✗ blok |
| 18–26 (əhatə edir) | ✗ blok |
| 21–23 (içəridə) | ✗ blok |
| 24–28 (bir gün toxunur) | ✗ blok |
| 25–28 (tam ayrı) | ✓ keçir |

**Qaydalar:**
- `.Date` **hər iki tərəfdə** məcburidir. Bazadakı tarixlər saat komponenti daşıya
  bilir; saatsız müqayisədə sərhəd günü sürüşür (mövcud bitmə `24.08 00:00`, yeni
  başlama `24.08 10:00` → `00:00 >= 10:00` yalan çıxır və 24 avqust İKİ məzuniyyətə
  düşərdi).
- Diri statuslar: `Gozlemede`, `SobeReisiTesdiqinde`, `RehberTesdiqinde`,
  `HrTesdiqinde`, `Tesdiqlenib`. İmtina və ləğv **bloklamır**.
- Xəta mətni **toqquşan qeydin** tarixlərini və statusunu yazır, seçilənləri yox —
  işçi «niyə keçmədi» sualına cavabı ekranda görsün.
- **Növ şərti QƏSDƏN yoxdur** — işçi eyni gündə həm xəstə, həm məzuniyyətdə ola
  bilməz; fiziki olaraq bir statusdadır.
- Yoxlama **hər şeydən əvvəl** olmalıdır (balans, əmr, bildirişdən qabaq) —
  qeyd yaranandan sonra balansı geri qaytarmaq əl işi tələb edir.
- Redaktə yollarında `xaricId` **məcburidir**, yoxsa qeyd özü ilə toqquşar.
- Yeni bir yaratma/tarix dəyişmə yolu əlavə edəndə bu cədvələ sətir əlavə et.

## Rol Prioriteti — Servis ilə Göstərmə Qatı Eyni Sırada Olmalıdır (KRİTİK)

Bir işçidə **birdən çox rol** ola bilər (real nümunə: Anar İbrahimov — `Operator` +
`HR` + `Rehber`). Belə halda marşrutu **şərtlərin SIRASI** həll edir və göstərmə qatı
həmin sıranı **eyni ilə** təkrarlamalıdır.

**İki modulda prioritet QƏSDƏN FƏRQLİDİR — «eyniləşdirmək» olmaz:**

| Modul | Sıra | HR+Rəhbər olan işçinin öz müraciəti |
|---|---|---|
| `MezuniyyetService.YaratAsync:95` | **HR → Rəhbər → ŞöbəRəisi** | `RehberTesdiqinde` — Rəhbər addımı VAR |
| `IcazeService.YaratAsync:174` | **Rəhbər → HR → ŞöbəRəisi** | `Tesdiqlenib` — birbaşa təsdiq |

Real hadisə (18.08.2026): `MezuniyyetListDto.RehberKecildi` və
`User/Views/Mezuniyyet/Detail.cshtml` yalnız `MuracietSahibiRehberdirmi`-yə baxırdı.
Servis isə HR şərtini əvvəl yoxlayır → müraciət **Rəhbərdə gözləyirdi**, view isə Rəhbər
addımını **keçilmiş sayıb gizlədirdi**: işçi öz «Müraciət gedişatı» ekranında
«Müraciət göndərildi → HR» görürdü. Heç bir xəta çıxmırdı, sadəcə ekran yalan danışırdı.

**Qaydalar:**
- Addım şərtini markup içində qurma — VM-də hesabla (`SobeReisiAddimiVar`,
  `RehberAddimiVar`) və Razor yalnız oxusun.
- **Rol bayraqlarını BÜTÜN controller-lər göndərsin.** `MuracietController` (birləşmiş
  portal) `MuracietSahibiHrdirmi`-ni göndərmirdi (`false` qalırdı) — nəticədə EYNİ siyahı
  `Mezuniyyet/Index` və `Icaze/Index`-dən fərqli görünürdü. Göndərilməyən `bool` sahə
  susmur, `false` sayılır və səhv addımı gizlədir.
- Marşrut şərtinə toxunanda **hər iki modulun** DTO-sunu və view-larını tutuşdur.

## Aktiv Təyinat — `Aktivdir` vs `BitmeTarixi` (KRİTİK)

`IsciTeyinat`-da «cari təyinat» üçün **iki fərqli tərif** işlədilirdi və layihə ikiyə
bölünmüşdü: ~35 yer `t.Aktivdir`, ~37 yer `t.BitmeTarixi == null`.

**Doğru tərif `Aktivdir`-dir.** `BitmeTarixi` **planlaşdırılmış** bitmə tarixidir —
sətrin bitdiyini sübut etmir. `IsciService.TeyinatRedakteEtAsync` redaktədə
`BitmeTarixi`-ni formadan olduğu kimi yazır, `Aktivdir`-ə **toxunmur** → sətir
`Aktivdir=1` **VƏ** `BitmeTarixi=<tarix>` vəziyyətində qalır.

Real hadisə (17.08.2026): bazada 29 təyinatın **22-si** məhz belə idi. Nəticə —
**İşçilər** siyahısı (`Aktivdir`, `HRProfile.cs:63`) 26 işçinin hamısını şöbəsi ilə
göstərirdi, **Departamentlər** və **Organizasiya Sxemi** (`BitmeTarixi == null`) isə
cəmi 6-nı sayırdı: departamentlərin çoxu «0 işçi / İşçi yoxdur» görünürdü. Heç bir
xəta çıxmırdı — sadəcə şirkət boş görünürdü.

**Aktiv təyinat şərtinin DÖRD hissəsi də lazımdır:**

```csharp
t.Aktivdir                        // cari təyinat (köhnəsi say=ikiqat olmasın)
&& !t.Silinib                     // yumşaq silinmiş təyinat sayılmasın
&& t.Isci.Status == IsciStatus.Aktiv   // ÇIXMIŞ İŞÇİ — təyinat avtomatik bağlanmır
&& !t.Isci.Silinib
```

Üçüncü şərt xüsusilə vacibdir: işçi işdən çıxanda `IsciTeyinat` sətri **passivləşmir**,
`IsciStrukturRolu` sətri də deaktiv olmur. Filtr qoyulmasa çıxmış işçi sxemdə və
sayğacda qalır.

Düzəldilən yerlər: `DepartmentService` (3 sorğu), `VezifeService`,
`OrganizasiyaController` (təyinatlar + struktur rolları). **Qalan `BitmeTarixi == null`
istifadələri (Hesabat, Performans, MaasHesablamaService) hələ köhnədir** — onlar
filtered `Include` olduğu üçün yalnız şöbə/vəzifə **göstərişini** boşaldır, məbləği
pozmur; toxunanda bu bölməni tutuşdur.

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

## İcazə — Jetonla Uzatma (17.08.2026-dan)

Adi saatlıq icazə **3 saatdır** (`IcazeService.AdiIcazeMaxDeq`). Onu uzadan **iki**
güzəşt var və hər ikisinin **qarşılığı** sayğacdan çıxılır:

| Güzəşt | Pəncərəyə əlavə | Sayğacdan çıxılan |
|---|---|---|
| «Nahara çıxmıram» | +nahar fasiləsi (45 dəq) | `NaharCixilmaSaat` (sabit) |
| «Artıq müddəti jetonumdan ödə» | +jeton balansı qədər | `JetonOdenenSaat` |

Nəticədə **sayğaca yazılan icazə heç vaxt 3 saatı keçmir**. Nümunə: 13:00–17:45
(285 dəq) + nahar + 1 saat jeton → 285 − 45 − 60 = **180 dəq**.

**Jeton MİQDARI heç yerdə əl ilə yazılmır** — pəncərədən hesablanır:
`IcazeService.MecburiJetonSaat` (= `EffektivDeq − 180`, yuxarı yuvarlaqlaşdırılmış).
İşçi yalnız **checkbox** işarələyir. Miqdarı iki yerdə (formada və düsturda) saxlasaq,
biri dəyişəndə o biri köhnə qalar və illik 36 saatlıq balans səssizcə pozular.

**Toxunanda hamısını tutuşdur:**
- `IcazeService.YaratAsync` — limit + balans yoxlaması, `JetonOdenenSaat` yazılır;
- `IcazeService.RehberTesdiqAsync` — rəhbər jetonu **ARTIRA** bilər, məcburi həddin
  altına **SALA bilməz** (`Math.Max(secilen, mecburi)`); rəhbər nahar işarəsini
  götürsə effektiv müddət artır → məcburi jeton da artır;
- `IcazeService.HrTesdiqAsync` — rəhbər yoxdursa müraciət birbaşa HR-a düşür və
  formada jeton sahəsi YOXDUR; balans təsdiqdən **ƏVVƏL** yenidən yoxlanır
  (sonra yoxlasaq icazə artıq «Təsdiqlənib» olardı);
- `Create.cshtml` + `user_create_icaze.js` — göstərmə qatı, serverdəki düsturu təkrarlayır;
- `IcazeDetal.cshtml` — `min = mecburiJeton`, `value = max(istək, mecburi)`.

**Sayğac tərəfi əvvəldən hazırdır** — bu üç yer `JetonOdenenSaat`-ı onsuz da çıxır:
`DashboardService` (illik balans), `IcazeIndexVM.IstifadeOlunanSaat`,
`GetIsciIzlemeAsync.PlanEfektiv`. Yeni bir sayğac yazsan, orada da çıxmalıdır.

**Diqqət:** `RehberTesdiqAsync`-in `jetonOdenenSaat` parametri **nullable**-dır —
`null` «forma göndərməyib, işçinin miqdarını saxla», `0` isə «sıfırla» deməkdir.

### Təsdiq Ekranında ÜSTƏLƏNMƏYƏN Seçimlər (17.08.2026)

Təsdiq ekranından **iki checkbox çıxarıldı**; hər ikisi «məlumat onsuz da var idi,
düymə isə onu təkrar soruşurdu» kateqoriyasındandır:

1. **«Naharı nəzərə alma»** — seçim işçinindir. Rəhbər razı deyilsə səbəb yazıb
   **imtina edir**. Üstələmə saxlansaydı jeton qaydası ilə **çıxılmaz vəziyyət**
   yaranırdı: işarəni götürmək effektiv müddəti (və məcburi jetonu) artırır, balans
   çatmayan işçidə müraciət ümumiyyətlə təsdiqlənə bilmirdi — rəhbər isə pəncərəni
   o ekranda qısalda bilmir. `RehberTesdiqAsync` artıq `NaharNezereAlinmasin`-a
   **toxunmur** (imtinada da silmir).
2. **«Birdəfəlik çıxış»** — `IcazeService.BirdefelikMi` ilə **hesablanır**:
   `bitisSaati >= StandartCixisVaxti` → işçi qayıtmır. Rəhbər təsdiq anında bunu
   bilmirdi; üstəlik `IcazeFaktikiSaat` onsuz da eyni nəticəni çıxarırdı
   (`bitisSaati >= gunSonu`), yəni düymə unudulanda ADMS və gecə işi köhnə yolla
   gedirdi. Üç yerdə eyni helper-dən yazılır: `YaratAsync` (entity initializer),
   `RehberTesdiqAsync`, `HrTesdiqAsync`.

Bayraqdan asılı 5 yer var — `ADMSController` (çıxış statusu + ikinci skan),
`PlanUzreBaglamaBackgroundService`, `IcazeFaktikiSaat`, `CixisQayidisAnomaliya`.
Hamısı eyni bayrağı oxuduğu üçün qaydanı dəyişəndə avtomatik uyğunlaşır.
**İSTİSNA:** jeton redim axını (`JetonService`) qəsdən kənardadır — orada tam iş
günü `jTamIsGunu` ilə ayrıca idarə olunur (işçi ümumiyyətlə gəlmir, cihaz qeydi yoxdur).

**Ümumi qayda:** təsdiq ekranında yalnız təsdiqçinin **əlavə məlumatı olan** sahə
qalmalıdır. Sistemin özü hesablaya bildiyi və ya işçinin onsuz da bildirdiyi şeyi
təkrar soruşma — unudulanda səssizcə səhv dəyər yazılır.

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

## Razor — `@if {}` Blokunun İçində Kod Bloku Açma (RZ1010)

Razor-da kontekst iki cürdür və qayda **əksinədir**:

| Haradasan | C# ifadə yazmaq üçün |
|---|---|
| **Markup** içində (`<div>…`, səhifə gövdəsi) | `@{ var x = 5; }` — kod bloku AÇ |
| **Kod bloku** içində (`@if (…) { … }`, `@foreach`, `@{ }`) | birbaşa `var x = 5;` — AÇMA |

`@if` blokunun gövdəsi **onsuz da C# kontekstidir**; orada `@{` yazmaq
**RZ1010** verir: *"Unexpected '{' after '@' character…"*. HTML tag-ı görünən
kimi Razor markup-a keçir — ondan sonra yenidən `@{` düzgündür.

```cshtml
@if (sert)
{
    int a = 5;                 @* ✅ birbaşa *@
    <div>
        @{ var b = a * 2; }    @* ✅ artıq markup içindəyik *@
        @b
    </div>
}
```

17.08.2026-da `IcazeDetal.cshtml`-də `@if` gövdəsində `@{` yazıldı → tək bu xəta
bütün `FinNex.UI` build-ini dayandırdı. **Qayda:** şərtli mətni markup ortasında
qurma — dəyəri `@if` gövdəsinin əvvəlində hazır dəyişənə yaz, markup-da yalnız
`@dəyişən` çağır.

## Oracle Rəqəmi — `ToString()` + `Parse` = 100× SƏHV (KRİTİK)

`OracleService` sətirləri `reader.GetValue()` ilə oxuyur — NUMBER sütunu artıq
**`decimal` obyektidir**. Onu stringə çevirib geri parse etmək **dəqiq 100× səhv**
verir, çünki iki mədəniyyət qarışır:

```
120.58m  →  .ToString()          →  "120,58"   (CARİ mədəniyyət: az-AZ)
"120,58" →  TryParse(Any, Invariant) →  12058   (vergül = MİN AYIRICISI)
```

Real hadisə (19.08.2026, VM 98.2.1): Axundovun dövr faizi 120,58 ₼ əvəzinə
**12 058,00 ₼** göründü, hesabi gəlir 18,84 əvəzinə **1 884,06** çıxdı və
maaş formasına düşdü. Heç bir xəta yox idi — nisbət tam 100 olduğu üçün tapıldı.
Tam ədədlər (`isci_faizi`=8, `vk_faizi`=13) ayırıcı daşımadığı üçün düz oxunurdu,
yəni səhv YALNIZ onluqlu sütunlarda idi.

**Qaydalar:**
- Oracle rəqəmini **stringə çevirmə**. Tipi birbaşa götür:
  `case decimal d: return d;` … (nümunə: `RiskService.Dec`, `KreditMuqavileService.Dec`
  — onlar əvvəldən belədir, ona görə bu tələyə düşmürlər).
- Sütun həqiqətən **mətn** olanda parse et və `NumberStyles.Any` İŞLƏTMƏ —
  o, min ayırıcısına icazə verir. `NumberStyles.Float` ilə əvvəlcə invariant,
  uğursuz olsa cari mədəniyyət yoxla (vergüllü mətn belə düzgün oxunur).
- Yeni Oracle sahəsi əlavə edəndə **ondalıqlı bir dəyəri əl ilə tutuşdur** —
  tam ədədlər səhvi gizlədir.

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

## Bir Elementə İKİ YAZICI — Təxmin vs Serverin Dəqiq Rəqəmi (KRİTİK)

Eyni DOM elementinə həm **lokal təxmin**, həm də **serverin dəqiq cavabı** yazılırsa,
nəticə hansının sonra işləməsindən asılı olur — sıra zəmanəti yoxdur. Ekranda rəqəm
gah düzgün, gah səhv görünər və heç bir xəta çıxmaz.

Real hadisə (18.08.2026, Yeni məzuniyyət müraciəti): `#durationText`-ə
`user_create_mezuniyyet.js` təxmin (`Math.round(diff*5/7)` = həftəsonu ehtimalı),
`Create.cshtml` preview-u isə backend-in dəqiq `data.isGun`-unu yazırdı. 20–24.08.2026
üçün başlıq gah «~5 iş günü», gah «~4» göstərirdi; aşağıdakı **İŞ GÜNÜ kartı**
(yalnız backend yazır) isə həmişə **5** idi — yəni səhv olan başlıq idi. Preview-un
keş qoruyucusu (`key === lastKey` → fetch etmir) hallarında təxmin ekranda tək qalırdı.

Üstəlik təxminin özü yanlış idi: **əmək məzuniyyətində ödənilən gün TƏQVİM günüdür**
(`MezuniyyetService.HesablaIsGunuAsync` — həftəsonu **sayılır**, yalnız
`MezuniyyetdeHesablanir=false` bayramlar çıxılır). ×5/7 burada mənasızdır.

**HƏLL: hər yazıcıya ÖZ elementi.** Birinci düzəlişdə yalnız təxmin silinmişdi,
amma element hələ ortaq idi — `hesabla()` iş günü hissəsini silir, preview isə keş
qoruyucusuna ilişib fetch etmirdi → mötərizə **birdəfəlik itirdi** («5 təqvim günü»
yazılırdı, aşağıdakı kart isə 5 iş günü göstərirdi). İndi:
`#durationText` (təqvim günü, JS) + `#durationIsGun` (iş günü, preview) —
heç biri o birini üstələmir. Keş halında preview son cavabdan (`lastIsGun`) bərpa
edir; uğursuz cavabda `lastIsGun = null` olur ki, köhnə rəqəm yeni aralığın yanına
düşməsin.

**Qaydalar:**
- Lokal JS **rəqəm uydurmasın**. Dərhal göstərilə bilən hissəni yaz (təqvim günü),
  serverdən gələni gözlə. Serverin rəqəmi yazılırsa «~» qoyma — təxmin deyil.
- İki mənbə bir sətri paylaşırsa **iki element** işlət. «Yazıcılardan birini
  susdurmaq» kifayət deyil — susan yazıcı da elementi təmizləyir.
- Async cavabda **köhnəlmiş cavab qoruyucusu** olsun: sorğu başlayanda açarı saxla,
  cavab gələndə `if (key !== lastKey) return;`. Ard-arda dəyişikliklərdə cavablar
  sıra ilə gəlmir — gec gələn köhnə cavab düzgün hesablamanı üstələyər.
- İki göstərici eyni kəmiyyəti göstərirsə (başlıq + kart), **mənbələri də eyni olsun**.
  Fərqli mənbə = gec-tez fərqli rəqəm.

**Hələ uyğunsuz qalan (təsdiq gözləyir):** HR tərəfindəki `Mezuniyyet/Create.cshtml`,
`Mezuniyyet/Edit.cshtml`, `XestelikEzamiyyet/Create.cshtml`, `XestelikEzamiyyet/Edit.cshtml`
«Hesablanmış iş günü»-nü **həftəsonusuz** sayır (`day !== 0 && day !== 6`) — serverin
təqvim günü qaydası ilə uyğun gəlmir (20–24.08 → ekranda 3, balansdan 5 düşür).
Yalnız GÖSTƏRMƏ qatıdır (server `HesablaIsGunuAsync` ilə yenidən hesablayır), amma
HR-ı yanılda bilər. Toxunanda əvvəlcə xəstəlik/ezamiyyət üçün qaydanın fərqli olub-
olmadığını istifadəçidən soruş.

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

## İŞLƏK FUNKSİYANI SİLMƏK — YALNIZ AÇIQ İCAZƏ İLƏ (KRİTİK)

**Heç bir işlək funksiya istifadəçinin açıq icazəsi olmadan silinə bilməz.**
Refaktor, birləşdirmə, "dublikatı təmizləmə" — heç biri bunun istisnası deyil.

Real hadisə (29.07.2026 → 14.08.2026, 17 gün gizli qaldı): Rəhbər Davamiyyət
səhifəsi HR-dakının kopyası idi və birləşdirildi (`5fb0b698`). Amma silinən
`RehberDashboardController`-də **yalnız orada olan** `ErkenCixisIcazeVer`
action-ı da vardı — səhifə ilə birlikdə getdi. Düymə isə ortaq JS faylında
qaldı və vahid səhifədə görünməyə davam etdi.

Nəticə: rəhbər 29.07-yə qədər erkən çıxış icazəsi verə bilirdi, sonra **heç kim**
verə bilmədi. Səssiz idi — endpoint boş sətrə düşür, `fetch('')` cari səhifəyə
POST edir, `r.json()` sınır, `.catch` düyməni geri qaytarır. İşçilər isə həmin
günlərdə "tez çıxan" kimi qeydə düşdü.

**İKİNCİ DALĞA (17.08.2026):** action 14.08-də bərpa olundu, amma **onu qidalandıran
DATA da silinmişdi** və bu 3 gün də gözdən qaçdı. `hr-davamiyyet.js` serverdən
`isciId`, `erkenIcaze`, `cixisQirmizi`, `isSaatiQirmizi`, `isSaatiSebeb` oxuyurdu —
beşi də silinmiş `RehberDashboardController`-də idi, yeni `HR/DavamiyyetController`-ə
köçürülməmişdi. JS-də hamısı `undefined` olur, amma **heç bir xəta çıxmır**:

- `data-isci-id` boş → düymə POST-u `isciId=0` göndərir → servis «İşçi seçilməyib»
  qaytarır → JS yalnız düyməni bərpa edir → **rəhbər «vurdum» deyir, baza boşdur**;
- `data-erken-icaze` həmişə `0` → icazə verilsə də düymə yenidən çıxır;
- `data-issaati-sebeb` boş → «niyə qırmızı» izahı ümumiyyətlə görünmür.

**Qayda:** action bərpa edəndə **onun bütün giriş datasını** da bərpa et. JS-in
oxuduğu hər `r.<sahə>` üçün serverin həmin adı göndərdiyini `grep` ilə yoxla —
JavaScript olmayan sahəni `undefined` edir, səssizcə.

**Qaydalar:**
- Silinən faylın/sinifin içindəkiləri **bir-bir sadala**. "Dublikatdır" qərarı
  fayl adına görə verilə bilməz — iki səhifə eyni görünüb, birində əlavə
  action/düymə/məntiq ola bilər.
- Silməzdən əvvəl istifadəçiyə **siyahı ilə** göstər: "bunlar silinəcək,
  təsdiq edirsinizmi?" — ümumi "dublikatı silirəm" cümləsi icazə deyil.
- Kod iki yerdə idisə, birləşdirmə **birləşdirmə** olmalıdır: hər iki tərəfin
  unikal hissəsi qalan tərəfə **köçürülməli**, sonra silinməlidir.
- **Front-end ilə back-end ayrı fayllardadır**: controller action silinəndə
  onu çağıran düymə/JS də yoxlanmalıdır. `grep` ilə action adını, endpoint
  URL-ini və `data-*` atributunu axtar.
- JS-də endpoint həmişə **default URL** ilə oxunsun (`endpoint(ad, '/default')`),
  `|| ''` YAZMA — boş ünvana POST səssizcə uğursuz olur və heç bir iz qalmır.

## Əl ilə Yazılan Migration — `InsertData` İŞLƏMİR (KRİTİK)

Bu layihədə migration-lar **əl ilə** yazılır və `.Designer.cs` faylı olmur.
Belə migration-da **`migrationBuilder.InsertData` / `UpdateData` / `DeleteData`
İSTİFADƏ EDİLƏ BİLMƏZ.**

Səbəb: bu üç metod sütun **tiplərini** bilmək üçün migration-un `TargetModel`-inə
baxır. `TargetModel` isə `.Designer.cs`-dəki `BuildTargetModel` metodundan gəlir.
Designer yoxdursa model **BOŞ** olur və EF atır:

```
System.InvalidOperationException: There is no entity type mapped to the table
'<Cədvəl>' which is used in a data operation. Either add the corresponding
entity type to the model, or specify the column types in the data operation.
```

**ƏN TƏHLÜKƏLİ HİSSƏ:** xəta SQL icra olunanda YOX, **SQL yaradılan mərhələdə**
(`GenerateUpSql`) baş verir. Yəni migration **bütöv** sınır — `CreateTable`
əmrləri də icra olunmur. Görünən nəticə isə tamamilə başqa yerə yönəldir:
səhifələr «Invalid object name '<Cədvəl>'» verir və adam cədvəl adında,
FK-larda, cascade yollarında səhv axtarır.

Real hadisə (19.08.2026, Avtopark): migration 5 cədvəl yaradırdı və sonda
`InsertData` ilə 5 sətir standart müddət növü yazırdı. `InsertData` səbəbindən
**5 cədvəlin heç biri yaranmadı**. Diaqnoz saatlarla uzandı, çünki
`Program.cs`-dəki `catch` xətanı yalnız `Console.WriteLine` ilə yazırdı və
IIS Express altında o, **heç yerə düşmür** (Serilog `Console.WriteLine`-ı tutmur).

**Qaydalar:**
- Əl ilə yazılan migration-da data əlavəsi **həmişə `migrationBuilder.Sql(@"INSERT …")`**
  ilə olsun — raw SQL model-ə ümumiyyətlə baxmır. Azərbaycan hərfləri üçün `N'…'`.
- Yoxlama: `InsertData`/`UpdateData`/`DeleteData` işlədən hər migration üçün
  yanında `.Designer.cs` **olmalıdır**:
  ```bash
  cd DataAccess/Migrations
  for f in $(grep -ln "InsertData\|UpdateData\|DeleteData" *.cs | grep -v Designer); do
      [ -f "${f%.cs}.Designer.cs" ] || echo "RİSK: $f"
  done
  ```
- Migration xətası **görünən yerdə** olmalıdır. `Program.cs`-dəki catch artıq
  `Log.Error(ex, …)` işlədir → `FinNex.UI\Logs\log-yyyyMMdd.txt`. `ex` bütöv
  ötürülür ki, `InnerException` (SQL Server-in əsl mətni) itməsin.
- «Invalid object name» xətasında **əvvəlcə həmin log faylına bax** — cədvəl
  adında/FK-da səbəb axtarmaq vaxt itkisidir; migration ümumiyyətlə işə düşməyə
  bilər.

**Sürətli diaqnostika sırası:**
1. `SELECT TOP 6 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC`
   — migration tarixçəyə düşübmü?
2. `Logs\log-<tarix>.txt` → `[Migration XƏTA]` sətri — əsl səbəb.
3. Yalnız bundan sonra kodda axtar.

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

### ORA-12570 və Digər Keçici Şəbəkə Xətaları (18.08.2026)

Mühasibat → Balans İcmalı bəzən **«ORA-12570: TNS:packet reader failure»** verirdi,
bir azdan eyni səhifə normal açılırdı. Bu, sorğunun və ya kodun səhvi **DEYİL** —
Oracle ilə TCP sessiyası qırılır: hovuzdakı (pool) bağlantının sessiyasını aradakı
firewall/NAT boşdayanmaya görə səssizcə bağlayır, ADO.NET isə bunu bilmir.

`OracleService` indi belə xətalarda **yeni bağlantı ilə 3 dəfəyə qədər təkrar cəhd**
edir (`CehdEtAsync`, 200/400 ms fasilə + `ClearAllPools()`). Təkrar təhlükəsizdir:
servis yalnız SELECT icra edir (`YalnizSelect`), Oracle-a yazı onsuz da qadağandır.

**Siyahıya yalnız şəbəkə xətaları salınır** (12570, 12571, 12537, 12152, 3113, 3114,
12547, 12560). Sintaksis (ORA-00904), hüquq (ORA-00942) və vaxt aşımı (ORA-01013)
**təkrarlanmır** — onları təkrarlamaq xətanı gizlədib istifadəçini 3 dəfə uzun
gözlətməkdən başqa nəyə yaramaz.

Xəta yenə də təkrarlanırsa problem koddadır deyil: bağlantı sətrinə
`Validate Connection=true` əlavə edin (ölü hovuz bağlantısı istifadədən əvvəl
yoxlanır) və ya şəbəkə/firewall boşdayanma müddətinə baxın.

### Oracle `CASE` — Sadə (simple) vs Şərtli (searched) — ORA-00932 (KRİTİK)

Oracle-da iki `CASE` forması var və **avtomatik tip çevirmə qaydası fərqlidir**:

| Forma | Yazılış | Tip çevirmə |
|---|---|---|
| **Searched** | `case when t.kod_valuti = '00' then …` | **VAR** — `'00'` avtomatik `0`-a çevrilir, işləyir |
| **Simple** | `case t.kod_valuti when '00' then …` | **YOXDUR** — `ORA-00932: inconsistent datatypes: expected NUMBER got CHAR` |

Real hadisə (19.08.2026, AML Hesab üzrə sorğu): BMI-nin sorğusu `case when
t.kod_valuti='00' then 'AZN' else case when … end end` yazırdı (6 qat iç-içə).
Qısaltmaq üçün `case t.kod_valuti when '00' then 'AZN' when '01' …` formasına
keçirildi — **`arh_dd.kod_valuti` INTEGER-dir**, sorğu bütöv sındı. Xəta mətni
sütunun adını demir, yalnız kursorun mövqeyini (96:30) göstərir; 400 sətrlik
sorğuda tapmaq çətindir.

**Qaydalar:**
- Bir sütunun tipini bilmədən `case <sütun> when '<mətn>'` yazma. Ya searched
  formanı işlət, ya da operandı sütunun tipində yaz (`when 0`, `when 1`).
- `ORA-00932` **UNION xətası DEYİL** — UNION-da tip uyğunsuzluğu `ORA-01790`
  verir. `ORA-00932` görəndə `CASE` / `NVL` / `DECODE` / `||` / funksiya
  arqumentinə bax, UNION qollarını yoxlamaqla vaxt itirmə.
- Tipi bir sorğu ilə öyrən (`docs/sql/aml/00_Tip_Diaqnostikasi.sql` nümunədir):
  `select column_name, data_type from all_tab_columns where owner='ODB' and table_name='…'`.
- Hesabın valyutası üçün `substr(hesab,6,2)` (CHAR) daha təhlükəsizdir —
  `kod_valuti` (INTEGER) ilə mətn müqayisəsi tələyə düşür.

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

### Jurnal Nömrəsi Geri Qaytarılmır — Silinmişlər DƏ Sayılır (KRİTİK)

Avtomatik nömrələnən jurnalda (`XaricMektub`, `DaxilMektub`, `GedenHevale`) növbəti
nömrə `max+1` ilə hesablanır. Bu hesabda **silinmiş sətirlər də iştirak etməlidir**:
nömrə bir dəfə veriləndən sonra sənəd artıq o nömrə ilə göndərilib — qeydin silinməsi
onu geri qaytarmır.

Tələ: `EfRepositoryAsync.HamisiniGetirAsync` / `Query()` **avtomatik `!Silinib`**
tətbiq edir (EfRepositoryAsync:25, 123). Onunla ən böyük nömrəli qeyd silinsə, həmin
nömrə **növbəti sənədə yenidən verilir** və jurnalda eyni nömrəli iki sətir yaranır
(biri silinmiş). Heç bir xəta çıxmır.

**Qayda:** nömrə hesablayan sorğuda `QueryAll()` işlət (silinmişləri də gətirir).
Kanonik nümunə layihədə artıq var: `SenedService.cs:452` (SenedFayl versiya nömrəsi).

**İSTİSNA — əl ilə yazılan nömrə:** `GelenHevale`-də nömrəni operator jurnaldan
yazır. Orada dublikat yoxlaması silinmişləri **qəsdən saymır** — səhv nömrə yazılıb
qeyd silinibsə, düzgün nömrənin yenidən yazılmasına mane olmamalıdır. Avtomatik və
əl ilə nömrələnən jurnalların qaydası fərqlidir; birini o birinə "uyğunlaşdırma".

### Nömrə Ayrılmadan ƏVVƏL Bütün Yoxlamalar (KRİTİK)

Sayğacdan nömrə ayrılan an dəyişiklik **geri qaytarılmır** — sayğac artır, məktub
jurnala düşür. Ona görə uğursuz ola biləcək **hər şey** nömrədən əvvəl yoxlanmalıdır.

Real nümunə (13.08.2026, `KreditMuqavileController`): nömrələr 134/318-ci sətirdə
ayrılırdı, Word şablonunun mövcudluğu isə 219/361-də yoxlanılırdı. Şablon tapılmasa
istifadəçi sənəd almırdı, amma kredit/ipoteka/zamin nömrələri **yeyilmiş**, BTİ
məktubu isə jurnala **sənədsiz** düşmüş olurdu. `NomreYaz=false` olduğu üçün hələ
təzahür etməmişdi. Yoxlamalar nömrədən əvvələ keçirildi.

**Qayda:** `NomreAyirAsync` / `YaratAsync` çağırışından əvvəl: giriş validasiyası,
limitlər (məs. zamin sayı), fayl/şablon mövcudluğu, xarici asılılıqlar — hamısı
yoxlanmış olmalıdır. Xəta mətnində "nömrələr ayrılmadı, heç nə yazılmadı" yaz ki,
istifadəçi təkrar cəhd etməkdən çəkinməsin.

### Kredit Məbləği — `summakre` (müqavilə) vs `summa` (qalıq) (KRİTİK)

`odb.licschkre`-də iki məbləğ var və mənaları FƏRQLİDİR (13.08.2026, BMI datası
ilə təsdiqləndi):

| Sütun | Mənası | DTO |
|---|---|---|
| `summakre` | **Müqavilə məbləği** (verilən kredit) | `Mebleg` |
| `summa` | Cari **əsas qalıq** (amortizasiya ilə azalır) | `MeblegAzn` |

Müqaviləyə (`{k_meb}`, `{k_meb_soz}`) **`Mebleg` düşməlidir**. `MeblegAzn`
işlədilsə sənəddə kreditin cari qalığı yazılar — 10 000 AZN-lik kreditin
müqaviləsində 2 724 AZN. Səssiz, amma hüquqi olaraq dağıdıcı.

Yoxlama: açıq portfeldə `AVG(summa/summakre)` ≈ 0,27 (286 kredit) və 0,46
(22 kredit); yeni verilən kreditdə (son 30 gün) 4/4 **bərabər**. Yəni fərq
valyuta ekvivalenti DEYİL — `MeblegAzn` adı yanıldıcıdır, dəyişdirilmədi ki,
mövcud istinadlar pozulmasın.

### Pul Köçürməsi Ərizəsi — «Məbləğ» KÖÇÜRÜLƏNDİR (KRİTİK)

`Erize1.docx`-də «Məbləğ rəqəmlə» xanası **köçürülən** məbləğdir, müştəridən
alınan yox. Rial/Rubl köçürməsində bu, `Mebleg × IranRial`-dır:

```
Məbləğ 900 (USD) × kurs 850 000 = 765 000 000 (rial)   ← sənədin ƏSAS rəqəmi
Alınan valyuta və məbləğ: 900                          ← Mebleg
```

18.08.2026-ya qədər kod ora `Mebleg`-i (900) yazırdı — sənədin əsas rəqəmi səhv
idi. Dəyərlər BMI-nin köhnə formasından ölçülüb (istinad sənəd: 26-T-24).

**«Məbləğ yazı ilə» üçün `KreditSozeCevir.MebleghSoze` İŞLƏTMƏ** — o, «manat»/
«qəpik» sözlərini sabit əlavə edir (kredit müqaviləsi üçün yazılıb) və rial/dollar
köçürməsində səhv olar. Düzgünü **`MebleghSozeQepiksiz`**: yalnız tam hissə,
valyuta sözü olmadan (`765000000` → «yeddi yüz altmış beş milyon»).

Rəqəm formatı: qrup ayırıcısı **yoxdur**, artıq sıfır **yoxdur** (`0.##`, az-AZ).
Valyuta adı iki cür yazılır — «Valyuta növü» sətrində «İran **R**ialı», «Satılan»
sətrində «İran **r**ialı»; köhnə formada belədir, qəsdən saxlanılıb.

### Kredit Müqaviləsi — Şablonlar YALNIZ AZN üçündür (KRİTİK)

`{k_val}` `KreditMuqavileController`-də sabit `"AZN"` yazılır və
`KreditSozeCevir.MebleghSoze` (sətir 63, 72) «manat»/«qəpik» sözlərini **sabit**
əlavə edir. Valyutalı kreditdə hər ikisi səhv olar və **heç bir xəta verməz**.

Qoruyucu: `odb.licschkre.xarici_valyutada_kredit` → DTO `XariciValyuta` (`bool?`).
`true` olduqda müqavilə hazırlanmır (forma açılmır + POST bloklanır, nömrədən
ƏVVƏL). 13.08.2026-da açıq portfeldə **310/310 kredit `0`** — yəni bu gün heç nəyi
bloklamır, gələcək qoruyucusudur.

`null` = sorğuda sütun yoxdur → **bloklamır** (modul dayanmasın), amma formada sarı
xəbərdarlıq çıxır. Qoruyucu səssizcə söndürülü qalmamalıdır.
Sorğu dəyişikliyi: `docs/sql/kredit/Kredit_Muqavile_Valyuta_Sutunu.md`.

Valyutalı kredit lazım olsa **üç yer birlikdə** dəyişməlidir: `{k_val}`,
`MebleghSoze`, və kod→qısaltma xəritəsi (`kurval`-da USD/EUR qısaltması YOXDUR).

### Şablon Yer Tutucusu ilə Kod Limiti Bağlıdır (KRİTİK)

Word şablonundakı `{k_teminat1}`…`{k_teminat4}` yer tutucularının **sayı** ilə koddakı
limit (`KreditMuqavileController.MaxZamin`) eyni olmalıdır. Kod limitsiz olsa, artıq
zaminin zaminlik müqaviləsi yaranır və nömrəsi yeyilir, amma kredit müqaviləsinin
təminat bəndində **görünmür** — hüquqi boşluq, heç bir xəta vermir.

13.08.2026: şablonda 3 yer tutucu var idi, formada limit yox idi → 4-cü zamin səssizcə
düşürdü. `{k_teminat4}` əlavə edildi, `MaxZamin = 4` həm serverdə, həm formada tətbiq
olundu. Birini dəyişəndə o birini də dəyiş.

### Bir Jurnala İKİ Yazıcı — Nömrə Tək Mənbədən Verilməlidir (KRİTİK)

Eyni jurnalın nömrəsini birdən çox servis verirsə, hər biri **yalnız öz cədvəlinə**
baxdıqda nömrələr toqquşur. Görünən əlamət: yeni modul **1-dən başlayır**, halbuki
jurnalda onlarla qeyd var.

Real hadisə (18.08.2026): `{YY}-T-{N}` həvalə nömrəsini iki yer verirdi —
`GedenHevaleService` (`GedenHevale` cədvəli, 2026-da 23 sətir → **24**) və
`KocurmeService` (`Kocurme` cədvəli, **boş** → **1**). Əməliyyat → Pul köçürməsi
səhifəsi «26-T-1» təklif edirdi; 26-T-1 … 26-T-23 isə Gedən həvalə jurnalında
artıq mövcud idi → ilk 23 köçürmə **zəmanətli dublikat**. İkisi eyni jurnaldır:
Gedən həvalə BMI-dən idxal olunmuş tarixçə, Pul köçürməsi isə həmin əməliyyatı
FinNex-də etmək üçündür.

Əlavə: `KocurmeService` `HamisiniGetirAsync` işlədirdi (avtomatik `!Silinib`),
yəni ən böyük nömrəli köçürmə silinsə nömrə **yenidən verilirdi** —
`GedenHevaleService`-də 13.08-də düzəldilmiş səhvin **köçürülməmiş nüsxəsi**.

**Qaydalar:**
- Nömrə hesablaması **tək yerdə** olsun (nümunə: `HevaleNomreHelper`), hər iki
  servis onu çağırsın. İki nüsxə saxlansa biri mütləq köhnə qalır.
- Hesablama **bütün** yazıcı cədvəllərin birləşməsinə baxsın, yalnız özününküyə yox.
- Ölçmədən əvvəl yoxla: yeni modulun verdiyi nömrə köhnə jurnalda varmı?
  `SELECT` ilə bir dəfə baxmaq kifayətdir.
- Prefiksi **ayırıcı ilə birlikdə** müqayisə et (`"26-T-"`), yoxsa `26-TL-5`
  səhvən T fəzasına düşər.

### Pul Köçürməsi → Gedən Həvalə: ƏSAS JURNAL BİRDİR (18.08.2026)

İstifadəçi qaydası: **«həvalə nömrəsi Gedən həvaləyə yazılır, ƏSAS budur, nömrə
ordan gəlir; eyni qaydada həmin nömrə köçürmələrə qeyd edilir».** Yəni
`GedenHevale` **əsas jurnaldır**; Əməliyyat → Pul köçürməsi ora sətir yazır və
eyni nömrəni `Kocurme.HevaleNo`-da da saxlayır.

- Yazma `KocurmeService.YaratAsync`-dədir, **`BeginTransactionAsync` ilə**: əvvəl
  `Kocurme` (Id lazımdır), sonra `GedenHevale`, sonra commit. Ayrı-ayrı yazılsa
  ikinci yazı sınanda nömrə yeyilmiş, jurnal boş qalardı — nömrə geri qaytarılmır.
- **`IGedenHevaleService.YaratAsync` ÇAĞIRILMIR** — o, nömrəni özü ayırır və
  `YaddaSaxlaAsync`-i özü çağırır; çağırsaq ikinci nömrə yeyilər və tranzaksiya
  parçalanardı. Entity eyni `IUnitOfWork` üzərində birbaşa yazılır.
- Bağ **açıq sahə** ilədir: `GedenHevale.KocurmeId`. **Nömrə ilə bağlamaq OLMAZ** —
  mövcud datada nömrə hələ unikal deyil (test `Kocurme` «26-T-1» ↔ real BMI idxalı
  «26-T-1»); nömrə ilə axtarsaq test qeydinin silinməsi **real jurnal sətrini**
  silərdi.
- Köçürmə redaktə/silinəndə jurnal sətri də yenilənir/silinir (`BagliHevaleAsync`).
  Əksi bloklanıb: `GedenHevaleService.SilAsync` `KocurmeId != null` sətri silmir,
  istifadəçini köçürmə səhifəsinə yönəldir. **Redaktə isə açıqdır** — köçürmədən
  gələn 5 sahə üstələnir, əl ilə doldurulanlar (Ölkə, Hesab №, rezident tipi…) qalır.
- Şərt **prefiksə** bağlıdır (`Prefiks(novu) == PulPrefiksi`), növ adına yox — jurnal
  «-T-» fəzasıdır, Tələbə köçürməsi («TL») ora düşmür.
- **BMI sütunları dardır** (`SAA` 50, `AL_BANK` 40, `VAL_TIP` 10, `MEBLEG` 14,2),
  `Kocurme`-dəkilər geniş (adlar 3×80, `BankAd` 120). Kəsmədən yazsan SQL
  *«String or binary data would be truncated»* ilə bütün əməliyyatı sındırar —
  `Kes(...)` helper-i var. `VAL_TIP`-ə tam ad yazma («ABŞ dolları» 11 simvoldur,
  səssizcə «ABŞ dollar» olardı); valyuta **kodu** yazılır.
- `MEBLEG`-ə **köçürülən** məbləğ düşür (Rial/Rubl-da `Mebleg × IranRial`), alınan
  yox — Word ərizəsi ilə eyni qayda. İki nüsxə saxlanmasın deyə hesablama
  `Helpers/Emeliyyat/KocurmeValyuta.cs`-dədir; həm `KocurmeControllerBase.WordIxrac`,
  həm `KocurmeService` onu çağırır.
- Uyğunluğu bilinməyən sahələr (`HES_NOM`, `OLKE`, `TIP_RES`, `HEV_TIP`, `GON_TIP`,
  `MEN_OLKE`, `CONTRAC_NOM`, `DECLAR_NOM`, `ARAYIS`) **qəsdən boş** qalır — uydurma
  dəyər yazmaqdansa boş yaxşıdır. Qayda dəqiqləşəndə **yalnız** `HevaleSetriniDoldur`
  dəyişir (yaratma və redaktə yolu onu ortaq çağırır).

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
