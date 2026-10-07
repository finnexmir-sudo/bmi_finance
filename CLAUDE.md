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

## Jetonla Ödənilmiş Məzuniyyət — NƏ KƏSİNTİ, NƏ ÖDƏNİŞ (09.09.2026, KRİTİK)

`Mezuniyyet.JetonIleOdendi = true` olan qeyd maaşda **tamamilə neytraldır**: jeton
həmin günü ödəyib, gün adi iş günü kimi qalır. Entity sənədi əvvəldən belə deyirdi,
amma **kod üç yerdə onu pozurdu**:

| Yer | Səhv | İşçiyə təsiri |
|---|---|---|
| `OzHesabinaIsGunuSayAsync` (~1600) | `Nov == OzHesabina`, jeton yoxlanmırdı | jeton xərclənir **VƏ** günün baza haqqı kəsilir → **İKİ DƏFƏ itki** |
| `MaasHesablamaService:~815` | post-korreksiya sorğusu | jetonlu günə **əlavə məzuniyyət haqqı** |
| `MaasHesablamaService:~2620` | eyni sorğunun ikinci nüsxəsi | eyni ikiqat ödəniş |

Dördüncü yer (`~1678`, əsas məzuniyyət haqqı) **əvvəldən düzgün idi** — ona görə
səhv yalnız iki dar halda görünürdü və uzun müddət gizli qaldı.

**İstifadəçi qərarı: «jeton seçilibsə artıq öz hesabına anlamı qalmasın».**
Yəni jeton bayrağı növdən **ÜSTÜNDÜR**. İndi hər dörd sorğuda `!x.JetonIleOdendi` var.

**`OzHesabinaIsGunuSayAsync` TƏK MƏNBƏDİR** — həm `FerdiHesabla` (əsl hesablama),
həm `MaasController` (TopluHesabla önizləməsi) onu çağırır, ona görə ekran ilə
hesablama avtomatik uyğun gəlir. Nüsxə çıxarma.

**JETONLU MƏZUNİYYƏT YALNIZ YARADILMA ANINDA TƏYİN OLUNUR** — `HR → Geriyə qeyd`
formasındakı «Jeton ilə əvəzləşdir». Mövcud qeydi sonradan jetona çevirən düymə
YOXDUR; «Növ düzəlt (admin)» yalnız İllik ↔ Öz hesabına arasında keçir və
`JetonIleOdendi`-yə toxunmur. Çevirmək lazımdırsa: ləğv et → yenidən yaz.

⚠️ **DAVAMİYYƏT HƏLƏ UYĞUNSUZDUR:** jetonlu qeyd üçün davamiyyət statusu hələ
**növə görə** yazılır (`OzHesabina` → «Ödənişsiz məzuniyyət», `Illik` → «Məzuniyyətdə»),
halbuki tabel və maaş onu **adi iş günü** sayır. Yəni Davamiyyət səhifəsi ilə Tabel
eyni gün üçün fərqli danışır. Toxunulmadı — düzəltmək 4 çağırış yerini dəyişməkdir
(`MezuniyyetService` 547/965/1099/1180/2703), ayrıca qərardır.

## Yumşaq Silinmiş Sətir + UNİKAL İNDEKS = Səssiz INSERT Xətası (10.09.2026, KRİTİK)

`Davamiyyetler` cədvəlində **unikal indeks (IsciId, Tarix)** var və o, `Silinib`
sütununu **FİLTRLƏMİR**. Repozitoriya isə əksinə — `GetirAsync` / `Query()`
avtomatik `!Silinib` tətbiq edir (`EfRepositoryAsync:25, 42`).

İki qayda bir-birinə ziddir və nəticə budur:

1. Məzuniyyət ləğv edilir → `DavamiyyetIzleriniSilAsync` sətirləri **yumşaq**
   silir (sətir bazada, öz `IsciId`+`Tarix` ilə qalır);
2. HR eyni tarixlərə yenidən məzuniyyət yazır → `GetirAsync` həmin sətri
   **GÖRMÜR** → kod «qeyd yoxdur» deyib `INSERT` edir;
3. Unikal indeks pozulur → `DbUpdateException`.

Real hadisə: admin «öz hesabına» məzuniyyəti ləğv etdi, HR eyni günə **Geriyə
qeyd** yazdı → forma dayandı.

**HƏLL — `MezuniyyetService.DavamiyyetUpsertAsync` (yeganə yazıcı).** Sıra:
`aktiv qeyd` → `yumşaq silinmiş qeyd (dirilt)` → `yeni yarat`. Yeni Davamiyyət
yazma yolu əlavə edəndə **öz sorğunu yazma**, bunu çağır. Metod
`DavamiyyetUpsertNeticesi` qaytarır (Deyismedi / Yenilendi / Berpa / Yaradildi)
ki, çağıran tərəf sayğac saxlaya bilsin.

| Yer | Vəziyyət |
|---|---|
| `MezuniyyetService` — HR təsdiqi, Geriyə qeyd, Dövlət vəzifəsi | ✅ üçü də `DavamiyyetUpsertAsync` çağırır |
| `MezuniyyetService.HrTarixDeyisAsync` | ✅ elə həmin metodu işlədir |
| `JetonService` (jeton redim, tam iş günü) | ✅ inline dirildilmə (metod `private`-dır) |
| `QayibMarkerBackgroundService` | ✅ `silinmisDict` ilə öz toplu axınında |
| `ADMSController` | ⚠️ **BU SƏTR SƏHV ÇIXDI — bax aşağıdakı 07.10.2026 bölməsi.** "Risk yoxdur, filtrsiz baxır, onsuz da tapır" qənaəti natamam idi: tapmaq kifayət deyil, silinmiş sətri AŞKARDAN SONRA nə etdiyi vacibdir — ADMS onu `Silinib=true` olaraq saxlayıb üzərinə real giriş/çıxış yazırdı, bu da real datanı əbədi görünməz edirdi. 07.10.2026-da `!x.Silinib` filtri + dirilt-məntiqi əlavə edildi. |

⚠️ **`DavamiyyetUpsertAsync` `private`-dır** — `JetonService` onu çağıra bilmir və
qaydanın nüsxəsini saxlayır. Qaydaya toxunanda **ikisini birlikdə** dəyiş.

⚠️ **Üstələnən statuslar yerə görə fərqlidir.** Default: `Qayib + Isde + Gecikme`
(gəlmədi, yaxud səhvən cihaza basıb). Qəsdən qoyulmuş leave statuslarına
(İcazəli / Xəstəlik / Ezamiyyət / Dövlət vəzifəsi) **toxunulmur** — onları
üst-üstə düşmə yoxlaması tutur. İSTİSNA: **dövlət vəzifəsi korreksiyası** mövcud
əmək məzuniyyətini əvəz etdiyi üçün `İcazəli`-ni də üstələyir (`ustelenenStatuslar`
parametri ilə açıq verilir).

### ADMSController-in "Risk Yoxdur" Qənaəti SƏHV ÇIXDI — Real Gəliş Görünməz Qaldı (07.10.2026, KRİTİK)

Yuxarıdakı cədvəldə (10.09.2026) `ADMSController` üçün "risk yoxdur, filtrsiz baxır,
silinmiş sətri onsuz da tapır" yazılmışdı. Bu qənaət **natamam** idi — yalnız "tapırmı"
sualına cavab verirdi, "tapandan sonra nə yazırmı" sualına yox.

Real hadisə (IsciId=16, Gürşad B.): işçinin bugünkü məzuniyyəti **təsdiqlənib, sonra
ləğv edilmişdi** → `MezuniyyetService.DavamiyyetIzleriniSilAsync` günün `Davamiyyet`
sətrini yumşaq sildi (`Silinib=true`). İşçi sonra HƏQİQƏTƏN işə gəldi (08:12 giriş,
09:36 çıxış — axşama planlaşdırılmış ezamiyyətdən əvvəl). ADMS-in

```csharp
var movcud = await _db.Davamiyyetler
    .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Tarix == tarix);
```

sorğusu `!Silinib` filtri olmadığı üçün elə həmin silinmiş sətri tapdı, real
giriş/çıxış vaxtlarını **onun üzərinə yazdı**, amma `Silinib`-i heç vaxt `false`
etmədi. Nəticə: baza sətri (Id=4600) real data ilə dolu idi (`GirisVaxti=08:12:39,
CixisVaxti=09:36:55, Status=İşdə`), amma `Silinib=1` olduğu üçün **bütün** `!Silinib`
filtri tətbiq edən sorğulara (HR/Davamiyyət, hesabat, maaş) görünməz qaldı.

**İkinci qatda olan simptom:** gün üçün heç bir görünən qeyd olmadığı üçün işçi
`DavamiyyetController.GetByTarix`-dəki `ezamiyyetGozleyenIds` sintetik-sətir
məntiqinə düşdü (həmin yoxlama yalnız ezamiyyətin TARİX aralığına baxır, saata yox)
və saat 10:45-də, axşamkı 16:00 ezamiyyəti hələ başlamamış, ekranda boş Giriş/Çıxışla
"Ezamiyyət" statusu göstərildi. İstifadəçinin sualı — *"niyə? heleki getmeyib axi?"* —
məhz bu idi.

**Düzəliş (yalnız birinci qatda, `ADMSController.cs`):** `movcud` sorğusuna `&&
!x.Silinib` əlavə edildi. Silinmiş sətir tapılarsa (yeni sətir yaratmaq unikal
indeksi pozardı), `DavamiyyetUpsertAsync`-dəki "aktiv → silinmiş (dirilt) → yeni"
sırası təkrarlanır: sətir dirildilir (`Silinib=false`, köhnə status/vaxtlar
təmizlənir), sonra normal giriş/çıxış axını davam edir.

**İkinci qat (`ezamiyyetGozleyenIds`-in saata baxmaması) EYNİ GÜN DÜZƏLDİLDİ** —
ilk cavabda "günlük KPI dizaynının qəsdən bir hissəsidir, özbaşına toxunmuram"
deyə qeyd edilmişdi. İstifadəçi bunu rədd etdi: *"hal hazırda ezamiyyətdə deyil
axı, bu səhv yanaşmadır, ezamiyyəti 16:00-dadır"*. Haqlı idi — "tarix aralığı
örtür" ilə "hazırda ezamiyyətdədir" eyni şey deyil. Həll: `EzamiyyetCanliMi(basTarix,
basSaat, hedefGun)` helper-i (`HR/DavamiyyetController.cs` və eyni adla
`User/DavamiyyetController.cs` — iki ayrı controller, eyni qayda) — YALNIZ
bugünkü (hədəf = bu gün) ezamiyyətin BİRİNCİ günündə, `BaslamaSaati` veriləndə,
həmin saat keçməyibsə "canlı deyil" qaytarır. Keçmiş/gələcək tarixə baxışda,
çoxgünlük ezamiyyətin sonrakı günlərində, ya da saat verilməyibsə (tam günlük
ezamiyyət) — əvvəlki kimi tarix aralığı kifayətdir. Tətbiq olunan 4 yer:
`HR/DavamiyyetController` — Gecikmə→Ezamiyyət örtmə, `ezamiyyetGozleyenIds`,
`GetGozlenilen`-dəki `ezamiyyetdeIsciIds`; `User/DavamiyyetController` —
Gecikmə→Ezamiyyət örtmə (öz portalı).

⚠️ **`ADMSController.HesablaStatus`-a TOXUNULMADI** — eyni fərziyyə (tarix
örtürsə "ezamiyyət günüdür") orada da var (bax yuxarı "Davamiyyət — Ezamiyyət
Statusa toxunan bütün yerlər"), amma risk fərqlidir: bu, HƏR punch-da, HƏR
işçi üçün işləyən canlı giriş-hesablama yoludur — vaxt yoxlaması əlavə etmək
indi düzgün görünən ssenarini (səhər ezamiyyətdən qayıdış) poza bilər, təsdiq
olunmamış dəyişiklik. Əgər gələcəkdə "axşam ezamiyyəti olan işçinin səhər gecikməsi
səhvən bağışlanır" şikayəti gələrsə, bura bax.

**Qayda:** "filtrsiz sorğu silinmiş sətri tapır, deməli risk yoxdur" qənaəti
**natamamdır** — sualı "tapandansa, onun ÜZƏRİNƏ yeni data yazılırmı, və yazılırsa
`Silinib` geri `false` olurmu?" şəklində tamamla. Tapıb sadəcə oxumaq fərqlidir,
tapıb **yazmaq** fərqlidir. Eynilə, "tarix aralığını örtür" ilə "hazırda baş
verir" arasındakı fərqi də unutma — ezamiyyət/icazə/xəstəlik kimi saatlı
hadisələrdə TARİX kifayət etmir, SAAT da yoxlanmalıdır.

### `ex.Message` TƏK BAŞINA HEÇ NƏ DEMİR

EF-in `DbUpdateException.Message`-i həmişə eynidir — *«An error occurred while
saving the entity changes. See the inner exception for details.»* SQL Server-in
əsl mətni (indeks adı, FK, truncation) **`InnerException`-dadır**.

Yuxarıdakı hadisədə ekran məhz bu mətni göstərirdi və səbəb yalnız kodu oxumaqla
tapıldı. İndi yazma yolları `MezuniyyetService.KokSebeb(ex)` işlədir — ən dərin
inner exception-un mətni əsas mesaja əlavə olunur.

**Qayda:** `SaveChanges` ola bilən hər `catch`-də `ex.Message` YAZMA — kök səbəbi
də göstər. Oxuma sorğularında adi `ex.Message` kifayətdir.

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

## `FindFirstValue` CS1061 — Tam Ad Yazmaq `using`-i ƏVƏZ ETMİR (30.09.2026, KRİTİK)

Real hadisə: bu sessiyada `dotnet build` əlçatan olmadığı üçün (sandbox mühiti)
kod yalnız mötərizə/parentez balansı ilə əl ilə yoxlanıldı — "build yoxlandı"
DEYİL, "sintaksis səviyyəsində iflasa uğramır" demək idi. İstifadəçi Visual
Studio-da **real** `Rebuild All` işlədəndə 2 × **CS1061** çıxdı:

```
'ClaimsPrincipal' does not contain a definition for 'FindFirstValue'
```

**Səbəb:** iki yeni controllerdə (`VezifeController.IsStatuslariSaxla`,
`MezuniyyetHuquqParametrleriController.Index` POST) `User.FindFirstValue(...)`
çağırılırdı, amma arqument **tam adla** yazılmışdı:
`User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)`.

Bu, `ClaimTypes`-ı tanıdır, amma `FindFirstValue`-nun ÖZÜ **uzatma
(extension) metoddur** — C#-da extension metod yalnız onu daşıyan namespace
**`using` ilə fayl başına idxal olunanda** görünür. Arqumentin tipini tam adla
yazmaq bunu əvəz etmir, çünki compiler extension metod axtarışını yalnız
`using`-lərə görə aparır, ifadədəki tam adlara görə yox.

**Düzəliş:** hər iki fayla `using System.Security.Claims;` əlavə edildi,
çağırış `User.FindFirstValue(ClaimTypes.NameIdentifier)`-ə sadələşdirildi —
mövcud işləyən nümunə (`KreditFaizDerecesiController.cs`) məhz bu formanı
işlədir, əvvəlcədən yoxlanmalı idi.

**Qayda:**
- Extension metod çağırışında (`.FindFirstValue`, `.Select`, `.Where`, hər
  hansı `static class`-dakı `this` parametrli metod) arqumentin tipini tam
  adla yazmaq **kifayət etmir** — metodun öz namespace-i `using` ilə idxal
  olunmalıdır.
- `dotnet build` əlçatan olmayan mühitdə "mötərizə balanslıdır" **build
  sübutu DEYİL** — yalnız kobud bir yoxlamadır. Bunu istifadəçiyə açıq de
  ("build mümkün deyil, əl ilə yoxladım") və REAL build nəticəsini soruş,
  onsuz "işləyir" demə (bax aşağıdakı "Metod İmzası Dəyişikliyi" bölməsi).
- Yeni bir `using` əlavə edəndə, artıq işləyən oxşar bir fayldan (`grep`)
  köçürmək təxmin etməkdən daha etibarlıdır.

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

## Qovluq Adı ≠ Namespace — `using` Yazmazdan Əvvəl FAYLA BAX (CS0246)

`FinNex.Application/Interfaces/HR/` qovluğunda fayllar **iki fərqli namespace**
işlədir və qovluq adına baxıb `using` yazmaq **CS0246** verir
(*«The type or namespace name '…' could not be found»*):

| Namespace | Nümunə fayllar |
|---|---|
| `FinNex.Application.Interfaces.HR` | `IIsciAyliqQazancService`, `IXestelikService`, `IKompensasiyaService` |
| **`FinNex.Application.Interfaces`** (`.HR` YOX) | **`IUserPermissionService`**, `IPermissionService`, `IIsciTeyinatService`, `IIsciStrukturRoluService` |

Real hadisə (01.09.2026): `IUserPermissionService` üçün faylın yolu
`Interfaces/HR/`-dir deyə `using FinNex.Application.Interfaces.HR;` yazıldı →
`FinNex.UI` build olmadı (2 × CS0246). Namespace isə `FinNex.Application.Interfaces`
idi.

**Qayda:** yeni bir servis/interfeys işlədəndə `using`-i qovluq yolundan
**TƏXMİN ETMƏ** — faylın öz `namespace` sətrini oxu:

```bash
grep -m1 "^namespace" FinNex.Application/Interfaces/HR/IUserPermissionService.cs
```

Yaxud layihədə həmin tipin mövcud istifadəsinə bax (nümunə: `_UserLayout.cshtml`
onu tam adı ilə çağırır — `@inject FinNex.Application.Interfaces.IUserPermissionService _permSvc`).

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

### `IsciTeyinat` KÖK SORĞUDURSA XÜSUSİ TƏHLÜKƏLİDİR (08.09.2026)

Yuxarıdakı `Include(...Where(t => t.Aktivdir))` istifadələri **yalnız göstərişi**
boşaldır (şöbə adı «—» olur). Amma sorğunun **KÖKÜ** `IsciTeyinat`-dırsa, təyinat
sətri **kimin siyahıda olacağını** həll edir — çıxmış işçi bütöv sətir kimi qalır.

Real hadisə: **HR → Müqavilə Bitmə** səhifəsi. Şərt `!t.Silinib && t.Aktivdir &&
t.BitmeTarixi.HasValue` idi. İlkin Q. 17.07.2026-da işdən çıxıb, müqaviləsi 22.07-də
bitib → səhifədə «48 gün keçib» sətri və **«Uzat» düyməsi** görünürdü. «İşçilər»
səhifəsi onu «İşdən Çıxmış» sekmesində göstərirdi — yəni iki səhifə eyni adama
fərqli baxırdı.

**Bu funksiyanın İKİ yazıcısı var — birlikdə dəyiş:**

| Yer | Nə edir |
|---|---|
| `MuqavileBitmeController.LoadRowsAsync` | səhifə **və** Excel ixracı (ortaq metod) |
| `XatirlatmaBackgroundService` (~sətir 132) | 10 iş günü qalmış HR-a **xatırlatma** |

İkincisi köhnə qalsa ekranda görünməyən adam üçün bildiriş gələr — daha çaşdırıcı.

**`== IsciStatus.Aktiv` YAZMA, `!= IshtenCixib` yaz.** Məzuniyyətdəki işçi hələ
işləyir və onun müqaviləsi də bitir; `== Aktiv` onu gizlədər və real bitən müqavilə
gözdən qaçar. Şərt «İşçilər» səhifəsi ilə eyni olmalıdır (`IsciService.cs:86`) —
orada da «Aktiv» sekmesi `Status != IshtenCixib` deməkdir.

KPI kartları `Rows`-dan hesablanır (`MuqavileBitmeIndexVM`), ona görə say=siyahı
avtomatik tutuşur — filtri sorğuda saxla, view-da təkrarlama.

Düzəldilən yerlər: `DepartmentService` (3 sorğu), `VezifeService`,
`OrganizasiyaController` (təyinatlar + struktur rolları), `MuqavileBitmeController`,
`XatirlatmaBackgroundService`. **Qalan `BitmeTarixi == null`
istifadələri (Hesabat, Performans, MaasHesablamaService) hələ köhnədir** — onlar
filtered `Include` olduğu üçün yalnız şöbə/vəzifə **göstərişini** boşaldır, məbləği
pozmur; toxunanda bu bölməni tutuşdur.

## Ezamiyyət/İcazə Siyahıları — TARİX + SAAT, Yenidən Köhnəyə (08.09.2026)

İstifadəçi qərarı: bu siyahılarda ən son gələn **yuxarıda** olmalıdır və eyni günün
qeydləri **saata görə** sıralanmalıdır. Yalnız tarixə görə sıralasan eyni günün
sətirləri baza sırası ilə gəlir — 08.09-da 14:00 müraciəti 15:25-dən yuxarıda
görünür və istifadəçi «niyə qarışıqdır?» sualı verir.

| Metod | Səhifə | Kontekst |
|---|---|---|
| `EzamiyyetService.HamisiniGetirAsync` | **HR → Ezamiyyət İdarəetməsi** (+ AJAX filtri) | EF sorğusu |
| `EzamiyyetService.IsciMuracietleriAsync` | işçinin öz siyahısı | LINQ-to-Objects |
| `EzamiyyetService.GozleyenlerAsync` | Rəhbər **və** HR təsdiq paneli | LINQ-to-Objects |
| `IcazeService.GetRehberTesdiqindeAsync` | Rəhbər təsdiq paneli + Gələn Qutusu | LINQ-to-Objects |

**NULL saat = TAM GÜN ezamiyyəti və HƏMİŞƏ SONA düşür** (gün 00:00-da başlayır).
İki kontekstdə fərqli yazılır, nəticə eynidir:

- **EF sorğusu** — sadəcə `.ThenByDescending(x => x.BaslamaSaati)`. SQL Server-də
  DESC sıralamada NULL onsuz da sona düşür; `?? TimeSpan.Zero` yazmaq lazım deyil
  və tərcümə riski gətirər.
- **Materiallaşmış siyahı** — `.ThenByDescending(x => x.BaslamaSaati ?? TimeSpan.Zero)`.
  LINQ-to-Objects-də NULL ƏN ƏVVƏLƏ düşərdi, ona görə coalesce MƏCBURİDİR.

Sıralama **yalnız servisdədir** — view `foreach` edir, JS-də sıralama yoxdur.
Yeni siyahı metodu yazsan bu cədvələ sətir əlavə et.

⚠️ **`IcazeService`-də hələ yalnız tarixə görə sıralanan metodlar var** —
`GetAllAsync`, `GetGozlemededeAsync`, `GetSobeyeGoreIcazelerAsync`,
`GetHrTesdiqindeAsync`, `GetIsciIcazeleriAsync`, `GetIsciIzlemeAsync`,
`GetDovriyyeAsync`, `GetFiltrliAsync`. Qəsdən toxunulmayıb: hansı səhifəyə
baxdıqları təsdiqlənməyib. Şikayət gələndə əvvəlcə səhifənin hansı metodu
çağırdığını tap, sonra dəyiş.

## Tabel Excel — «M» vs «G»: ÖZ HESABINA AYRI SÜTUNDUR (09.09.2026)

Rəsmi tabel qalıbının işarələri: `İ` istirahət, `B` bayram, `E` ezamiyyət,
`X` xəstəlik, `M` məzuniyyət, **`G` işə gəlmədiyi günlər**.

**`MezuniyyetNovu.OzHesabina` (ödənişsiz, Ə.M. 129) tabeldə «M» DEYİL, «G»-dir**
və «Məz.» sütununa DÜŞMÜR — öz **«Öz hes.»** sütununda sayılır (istifadəçi qərarı:
«bu qalıb bizdə»). Əvvəl «M» yazılırdı və ödənişli məzuniyyətlə eyni xanada
toplanırdı; mühasib öz Exceli ilə tutuşdura bilmirdi.

Nümunə (Nərminə Q., 08.09.2026, 1 gün öz hesabına): `İş günü 21 · Məz. 0 ·
**Öz hes. 1**` — əvvəl `Məz. 1` idi. **İş günü/saatı DƏYİŞMİR** — ödənişsiz gün
onsuz da işlənməmiş sayılırdı.

**Kod bir neçə yerdədir — biri köhnə qalsa sütunlar sürüşür, xəta çıxmır:**

| Yer | Nə edir |
|---|---|
| `TabelService` — `gunMez.Nov` yoxlaması | «G» kodu + `OzHesabinaGun` sayğacı |
| `TabelDto.OzHesabinaGun` | DTO sahəsi (`MezuniyyetGun`-a daxil DEYİL) |
| `TabelController.YEKUN_SUTUN` sabiti | yekun sütunların **sayı** |
| `TabelController` — `sumHdrs`, sətir yazma, CƏMİ, sütun eni | 4 yer, hamısı sabitə bağlıdır |
| `Views/Tabel/Index.cshtml` — kodlar izahı | ekran ilə Excel eyni danışsın |

⚠️ `Any` YOX, **`FirstOrDefault`** — günü örtən qeydin **növü** lazımdır.

⚠️ **Məzuniyyət aralığındakı HƏFTƏSONU da sayılır** — bu, «M» üçün əvvəldən belə
idi (30 günlük məzuniyyət «Məz. 30» yazır) və «G» də eyni qaydadadır. Mühasib
yalnız iş günlərini istəyirsə bu QƏSDƏN dəyişilməlidir, öz-özünə fərz etmə.

⚠️ **`EzamiyyetGun` hələ HƏMİŞƏ 0-dır** və «E» kodu heç vaxt yazılmır —
`TabelService` `EzamiyyetMuraciet` cədvəlini ümumiyyətlə oxumur. Qalıbda sütun
və işarə var, data yoxdur. Ayrıca iş kimi qalır.

## Məzuniyyət Balansı — «Limit 14 gün» (09.09.2026)

Ə.M. md.137: əsas məzuniyyətin **hissələrindən biri ən azı 14 gün** olmalıdır.
`HR → Məzuniyyət Balansı` səhifəsində bu, **hər iş ili üçün AYRICA** göstərilir.

| Vəziyyət | Göstərilən |
|---|---|
| Həmin iş ilində birdəfəlik **≥14 günlük** məzuniyyət götürülüb | limit YOX — bütün qalıq sərbəst |
| Götürülməyib | sərbəst = `qalıq − 14` |
| Qalıq 14-dən az | **0** + «14 günlük hissə üçün qalıq çatmır» |

**MƏNFİ RƏQƏM VERİLMİR** (istifadəçi qərarı). Yekun sütun hər ilin sərbəst
hissəsinin **CƏMİdir** — «bütöv qalıqdan bir dəfə 14 çıxmaq» səhv olardı,
tələb hər iş ilinə ayrıca aiddir.

⚠️ **YALNIZ GÖSTƏRİŞDİR** — balansdan heç nə çıxılmır, heç bir müraciət bu
rəqəmə görə bloklanmır.

⚠️ **MÜHASİBİN EXCELİ İLƏ QƏSDƏN FƏRQLİDİR** (`İşçilərin məzuniyyətləri.xlsx`
→ «Cari qalıq», L sütunu). İki fərq var:
1. Excel 14-ü **həmişə** çıxır, hətta ≥14 günlük məzuniyyət artıq
   götürülmüşdüsə də (Nadirova 19 günlük məzuniyyət alıb, Excel yenə `−9`).
   İstifadəçi təsdiqi: «mənim qaydam, excel səhvdir».
2. Excel hansı il sütunundan çıxacağını **əl ilə** seçir və özü ilə
   ziddiyyətlidir — İbrahimov `D3-14`, Axundov `C4-14`, hər ikisi fevral
   işçisidir. Sistem iş ilini avtomatik təyin edir.

**İŞ İLİ ≠ TƏQVİM İLİ** — işə qəbul ildönümündən başlayır. Hesablayan iki yer
var və **eyni clamp qaydasındadır** (29 fevral → ayın son günü):
`MezuniyyetBalansController.IsIliniTap` və `Index.cshtml`-dəki `SonIldonum`.
Birini dəyişəndə o birini də dəyiş — yoxsa sütundakı il ilə limitin ili
sürüşər və **heç bir xəta çıxmaz**.

Məzuniyyət iki iş ilinə düşə bilər (`BalansiFifoKesAsync` FIFO kəsir), amma
«birdəfəlik 14 gün» tələbi **fasiləsizlik** haqqındadır — qeyd **BAŞLADIĞI**
iş ilinə yazılır.

## Şəxsiyyət Vəsiqəsi Müddəti — HEÇ YERƏ YAZILMIR (09.09.2026)

`HR → Müddətlər → Şəxsiyyət vəsiqəsi` sekmesi tarixi **BMI-dən canlı** oxuyur
(`regnom.pasport_date_close`, FİN üzrə). İstifadəçi qərarı: **«sadəcə
göstəririk, heç yerə yazmayacağıq»**.

- `Isci`-də vəsiqə tarixi sütunu **YOXDUR və olmamalıdır** — iki nüsxə
  saxlansa biri gec-tez köhnələr;
- «idxal et / yenilə» düyməsi **yoxdur** — səhifəni yeniləmək kifayətdir;
- Sorğu `OracleSorgular`-dadır (`SorguAdi = VESIQE_BITME`), mətn
  `docs/sql/hr/Vesiqe_Bitme_OracleSorgu.sql`. Quraşdırılmayıbsa səhifə
  **açıq xəbərdarlıq** verir, səssiz boş siyahı göstərmir.

**Sütun alias-ları məcburidir:** `FIN`, `VESIQE_BIT_TARIXI`. Ad dəyişsə dəyər
səssizcə boşalmır — servis açıq xəbərdarlıq qaytarır. Tarix `to_char` ilə
**mətnə çevrilməməlidir** (Oracle DATE tipi birbaşa oxunur).

Bir FİN üçün `regnom`-da birdən çox sətir ola bilər — **ƏN SON tarix**
götürülür ki, köhnə vəsiqə sətri yenisini üstələməsin.

**Tarixi tapılmayan işçi «bitib» SAYILMIR** — ayrıca «Tarix tapılmadı»
sayğacındadır. Yoxsa doldurulmamış işçilər təcili siyahını doldurardı.

**SIRALAMA `Sira`-YA GÖRƏDİR** (istifadəçi qərarı: «işçi sıralamasından
götürülməlidir») — ilk yazılışda qalan günə görə sıralanmışdı, səhv idi.
Təcililik siyahının sırasında yox, «Qalan gün» sütununun **rəngində** və
KPI kartlarında görünür.

📌 **AÇIQ İŞ — Müqavilə sekmesi də `Sira`-ya keçirilsin.** Qonşu **Müqavilə**
sekmesi hələ `QalanGun` üzrə sıralanır (`MuqavileBitmeController.LoadRowsAsync`
sonundakı `.OrderBy(r => r.QalanGun)`). İstifadəçi 09.09.2026-da bunu
**«yaxşı fikirdir»** deyə qiymətləndirdi, amma həmin an tətbiq edilmədi —
növbəti dəfə bu səhifəyə toxunanda et. Nəzərə al: o səhifədə «növbəti N gün»
filtri var, yəni sıra dəyişsə də siyahının **məzmunu** dəyişmir; təcililik
`Tecililik` sinfinin rəngində və KPI kartlarında qalır. Excel ixracı eyni
`LoadRowsAsync`-i çağırdığı üçün avtomatik uyğunlaşacaq.

`MuqavileBitmeController`-də iki action var, amma **sorğular birləşdirilməyib**:
müqavilə siyahısının kökü `IsciTeyinat`, vəsiqəninki BMI sətridir —
birləşdirsək müqavilə filtrləri (gün, `BitmeTarixi.HasValue`) səssizcə
vəsiqəyə də tətbiq olunardı.

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

### Ezamiyyətdən QAYIDIŞ → Səhv "Erkən Çıxış" Bildirişi (01.10.2026, KRİTİK)

`ADMSController.ProcessAttLogLineAsync`-da "Erkən çıxış qeydə alındı" bildirişini göndərən
qapı yalnız `ezamiyyetCixisi` dəyişəninə baxırdı — bu dəyişən ezamiyyətin **BaslamaSaati**-nə
±30 dəqiqə yaxın olan punch-ı (yəni ÇIXIŞI) tanıyır. Eyni funksiyada 11.08.2026-da əlavə
edilmiş "Qayıdış" bloku (yuxarıda) ezamiyyətdən QAYIDIŞ punch-ını tanıyıb `Davamiyyet.Status`-u
düzgün "İşdə"-yə qaytarırdı, AMMA bu status düzəlişindən dərhal sonra gələn bildiriş qapısı
eyni köhnə `ezamiyyetCixisi`-ə baxmağa davam edirdi — qayıdış demək olar HƏMİŞƏ başlama
saatından 30 dəqiqədən çox sonra olduğu üçün `ezamiyyetCixisi = false` qalır və sistem
qayıdışı sıravi "erkən çıxış" kimi qiymətləndirib bildiriş göndərirdi.

Real hadisə: işçi Gürşad B. eyni günə iki ezamiyyət yazmışdı (biri təsdiqlənmiş, biri
gözləyən) və 11:15–17:45 ezamiyyətindən 14:31-də qayıdanda "Erkən çıxış qeydə alındı"
bildirişi aldı — halbuki `Davamiyyet.Status` düzgün "İşdə" idi. İkinci (gözləyən) ezamiyyətin
bununla **əlaqəsi yox idi** — sorğu `Status == Tesdiqlendi` filtri ilə `bugunEzamiyyet`-i
tapdığı üçün gözləyən qeyd bu məntiqdə ümumiyyətlə görünmür. Səhv faktiki olaraq **başlama
saatından 30 dəqiqədən çox sonra ezamiyyətdən qayıdan İSTƏNİLƏN işçidə** baş verə bilirdi —
yəni demək olar bütün real ezamiyyətlərdə; sadəcə Davamiyyet statusu düzgün qaldığı üçün
indiyədək fərq olunmamışdı. Həmin gün daha iki işçiyə eyni səhv bildiriş getdi.

**Düzəliş:** "Qayıdış" şərti ayrıca `ezamiyyetQayidisi` bayrağına çıxarıldı və bildiriş
qapısının HƏR İKİ yoxlamasına (`gorushCixisi` hesablanmasından əvvəl və əsas `if`-də)
`!ezamiyyetQayidisi` əlavə edildi — eynilə çıxışın özü (`ezamiyyetCixisi`) necə istisna
edilibsə.

**Qayda:** bir statusu/datanı düzəldən şərt əlavə edəndə, həmin KOD BLOKUNUN İÇİNDƏ ondan
dərhal sonra gələn, AMMA ayrı dəyişənə görə qərar verən digər şərtləri (bildiriş, log,
hesablama) də yoxla — status düzəlib deyə yanındakı kodun da avtomatik düzəldiyini fərz etmə.

### Xəstəlik — EYNİ boşluq, HEÇ VAXT DÜZƏLDİLMƏMİŞDİ (24.09.2026, KRİTİK)

Xəstəlik vaxtilə `Mezuniyyet`-in bir növü idi (`MezuniyyetNovu.Xestelik`) və o axın
Davamiyyətə real sətir yazırdı. Sonra Xəstəlik **ayrı cədvələ/modula köçürüldü**
(`Xestelik` entity, `/HR/Xestelik`) — kodda bunu təsdiqləyən açıq şərh var
(`XestelikEzamiyyetController`): *"Xəstəlik artıq ayrıca cədvəldədir. Bu səhifə
yalnız ezamiyyət göstərir."* Amma köçürmə zamanı Davamiyyətlə əlaqə **heç yaradılmadı**
— nə yazma tərəfi (köhnə `Mezuniyyet` axını kimi), nə göstərmə tərəfi (Ezamiyyət kimi).
`DavamiyyetStatus.Xestelik = 5` enum dəyəri mövcuddur, KPI kartı onu sayır, amma heç
yerdə yazılmır → sayğac həmişə 0, təsdiqli bülleteni olan işçi isə "Gözlənilir"/"Qayıb"
düşürdü (real hadisə: Nərminə Q., 23–25.09.2026 xəstəlik, 24.09-da "Gözlənilir").

**Həll — Ezamiyyət ilə EYNİ göstərmə qatı naxışı, YALNIZ göstərmə** (istifadəçi
qərarı: bazaya yazma yoxdur, unikal-indeks/yumşaq-silmə tələsinə düşmə riski qalmır):

1. `HR/DavamiyyetController.GetByTarix` — `xestelikList` (təsdiqlənmiş bülletenlər),
   Gecikmə örtüyü (ezamiyyət kimi), `xestelikGozleyenIds`/`xestelikGozleyenRows`
   (cihaz qeydi olmayan xəstələr üçün sintetik sətir), KPI sayğacı
   `umumi.Count(...) + xestelikGozleyenIds.Count` (əvvəl yalnız `umumi.Count(...)` idi,
   həmişə 0 qalırdı), `tezCixanFlag`/`isSaatiSebeb` istisnası.
2. `HR/DavamiyyetController.GetGozlenilen` — `xestelikIsciIds` sorğusu, prioritet
   zənciri **İcazəli > Xəstəlik > Ezamiyyət > Tədbirdə**, `yalnizGozleyen` filtrinə
   `status != 5` əlavəsi.
3. Frontend (`hr-davamiyyet.js`, `Views/Davamiyyet/Index.cshtml`) artıq **hazır idi**
   — `status=5` badge, `kpiXestelik` elementi, KPI kartının `data-status="5"` klikləməsi
   hamısı mövcud idi, sadəcə backend heç vaxt düzgün rəqəm/sətir göndərmirdi.

**`User/Areas/User/Controllers/DavamiyyetController.cs`-ə TOXUNULMADI** — bu səhifə
memarlıq olaraq fərqlidir: yalnız işçinin **real** Davamiyyet sətirlərini göstərir,
HR-ın `GetGozlenilen`-i kimi "qeydi olmayan gün üçün sintetik sətir" mexanizmi
ÜMUMİYYƏTLƏ yoxdur (Qayıb üçün də yoxdur). Ona görə Xəstəlik günü orada sadəcə
**görünmür** (səhv status yox, sətir yox) — HR-dakı "Gözlənilir" yalanı ilə eyni
simptom deyil. Düzəltmək sintetik sətir mexanizmini oraya köçürməyi tələb edir —
ayrıca qərardır, bu sessiyada edilmədi.

**Qayda:** bir davamiyyət-təsiri olan modul (icazə növü, xəstəlik, ezamiyyət) başqa
cədvələ/modula köçürülürsə, köhnə modulun Davamiyyətə təsiri (yazma VƏ ya göstərmə)
YENİ modula da köçürülməlidir — "ayrıca cədvələdir" şərhi təkbaşına kifayət etmir,
əlaqə əl ilə yenidən qurulmalıdır.

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

## Qara Jeton — Dəyərli Kəsinti Mexanizmi (07.10.2026)

İstifadəçi tapşırığı: Qara Jeton (əvvəllər `JetonTeyinati.Id=4`, `Nov=Menfi`,
`SaatDeyeri=0` — YALNIZ bayraq idi, `AktivQaraJetonuVarmiAsync` ilə jeton-redim
sorğularını bloklayırdı) **dəyərli** oldu. Verilən AN kaskad işləyir:

1. İşçinin müsbət (mükafat) jeton balansından **FIFO** (köhnədən) çıxılır.
2. Qalıbsa — işçinin **cari ilin "36 Saat Hüququ" jetonunun** `QalanSaat`-ından çıxılır.
3. Yenə qalıbsa — `QaraJetonBorcu` "Gözləyir" statusunda yazılır. Növbəti müsbət
   jeton veriləndə əvvəlcə bu borc ödənilir (bax aşağı). İl sonunda ödənməmiş
   qalıq **bağışlanır** (Status=MuddetiBitib) — istifadəçi qərarı: "qara jeton
   cari ilin sonuna kimi qüvvədədir".

### "36 Saat Hüququ" — illik limit artıq JETONDUR, amma İCAZƏ MƏNTİQİ DƏYİŞMİR

Rəhbərliklə razılaşma: illik 36 saatlıq icazə hüququ artıq `DashboardService`-də
sabit `36` kimi yox, hər işçinin öz **"36 Saat Hüququ {il}"** adlı `IsciJetonu`
sətrinin `QalanSaat`-ı kimi təqdim olunur (`JetonService.IllikHuquqQalanSaatAsync`,
`EnsureIllikHuquqJetonuAsync` — yoxdursa avtomatik, tam dəyərlə yaradılır).

**İstifadəçinin öz sözü: "yenə eyni məntiqnən gedəcək, sadəcə görüntü və
adlandırma dəyişir — icazə yenədə 3 saat + müsbət jeton".** Yəni:
- İcazənin özünün hesablanması (3 saat pulsuz + `JetonOdenenSaat` ilə əlavə)
  **BİR SƏTİR BELƏ DƏYİŞMƏDİ** — `DashboardService`-də YALNIZ `dto.IcazeSaatLimiti`
  sabit `36` əvəzinə `IllikHuquqQalanSaatAsync`-dən oxunur, `IcazeIstifadeSaat`-ın
  öz hesablama düsturuna (yuxarıdakı `IcazeIstifade` funksiyası) **TOXUNULMADI**.
- Bu jeton sətri **adi icazə-jeton-ödənişi (`JetonOdenenSaat`) FIFO-sundan
  QƏSDƏN kənardadır** (`JetonTeyinati.Sistemli=true` bayrağı) — əks halda
  `IcazeUcunFifoJetonXercleAsync` (ən köhnə tarixli jetonu seçdiyi üçün, "36 Saat
  Hüququ" 1 yanvar tarixli olduğundan HƏR ZAMAN birinci seçilərdi) icazənin adi
  jeton-ödənişini bu hüquqdan yeyərdi — YALNIZ Qara Jeton kəsintisi ona toxunur.
  `Sistemli=true` olan tiplər həmçinin: HR-in adi jeton kataloqunda (`JetonTeyinatlariGetirAsync`)
  görünmür, əl ilə verilə/redaktə edilə/ləğv edilə bilmir (`JetonVerAsync`,
  `JetonTeyinatiYenileAsync`, `JetonLegvetAsync` — üçündə də açıq blok var),
  `AktivSaatBalansiAsync`-a (işçinin göstərilən mükafat balansı) daxil deyil,
  jeton-redim (`RedimTelebiYaratAsync`) seçimində görünmür.

**2026-cı il üçün də dərhal tətbiq olundu** (ayrıca keçid ili YOXDUR) — bu,
tam təhlükəsizdir, çünki icazə hesablaması dəyişmədiyi üçün "36 Saat Hüququ"
jetonu YALNIZ `QalanSaat=36`-dan başlayır (artıq yığılmış icazə istifadəsi bu
jetondan ÇIXILMIR, `DashboardService`-in öz formülü — `limit − istifadə` —
bunu ayrıca hesablayır, elə əvvəldən belə idi).

**İl Dashboard kartında da görünür (07.10.2026, istifadəçi istəyi).** İstifadəçi
sualı aydınlaşdı: "36 Saat Hüququ {il}" adı sırf daxili baza sətridir deyə
izah edildikdən sonra — *"mənim əslində istəyim elə bir şey idi... heç olmasa
səhifədə görüntü olaraq elə qeyd edə bilmərik?"*. Yəni hesablama məntiqinə
TOXUNULMADAN (düz, dəyişməyib), yalnız görüntüyə il əlavə edildi:
`User/Dashboard/Index.cshtml` → kart başlığı **"İcazə saatı — bu il"** →
**"İcazə saatı — bu il (@DateTime.Today.Year)"**. `DateTime.Today.Year`
seçimi qəsdən — `EnsureIllikHuquqJetonuAsync`-in özünün işlətdiyi İLƏ eynidir
(`JetonVerAsync` → `DateTime.Today.Year`), ayrıca "cari il" sahəsi saxlanmır.
Sırf mətn dəyişikliyi, DTO/servis toxunulmayıb.

### Borcun ödənməsi — `JetonVerAsync`-in Musbat qolu

Növbəti dəfə işçiyə MÜSBƏT jeton veriləndə (`JetonService.QaraJetonBorcunuOdeAsync`),
əvvəlcə cari ilin "Gözləyir" borcu (əgər varsa, ən köhnəsi) ödənilir: yeni jetonun
dəyəri qədər borc azalır. **Tam yeyərsə — yeni jeton SIFIR qalıqla, amma GÖRÜNƏN
sətir** olaraq saxlanır (status=İstifadəOlunub) — HR/işçi "mükafat verilib, dərhal
borcu ödəyib" kimi görür (istifadəçi qərarı). Qismən yeyərsə, qalan hissə normal
istifadə oluna bilən qalır. Keçən illərin ödənməmiş "Gözləyir" borcu bu çağırışda
bağışlanır (eyni an aşkarlanır və bağlanır).

⚠️ **Bir yeni jeton yalnız BİR (ən köhnə) borcu ödəyir** — eyni anda bir neçə
Qara Jeton borcu varsa (nadir hal), hər biri ayrıca müsbət jeton tələb edir.
`Eded>1` olan təklifdə hər sətir öz növbəsində işləyir, ona görə çox saylı
jeton verməklə bir neçə borc ardıcıl ödənə bilər.

### Qara Jeton "Xərcləmə Bloku" SİLİNDİ (07.10.2026, istifadəçi qərarı)

Əvvəl aktiv Qara Jeton varkən `AktivQaraJetonuVarmiAsync` bütün jeton-redim
sorğularını bloklayırdı (`RedimTelebiYaratAsync`, + `User/Jeton/Index`-dəki
"Qara jeton mövcuddur — xərcləmə bloklanıb" qırmızı xəbərdarlıq, + JS-dəki
`ujOpenRedimModal` qapısı). İstifadəçi: *"verilən qara jeton işini gördüsə,
yəni müsbətdən və ya illik saatdan çıxdısa vəssalamda, niyə blokda qalır ki"*.

Haqlı idi — bu blok Qara Jetonun dəyərsiz (SaatDeyeri=0) olduğu **köhnə**
dövrdən qalmışdı, o vaxt yeganə "cəza" elə bu blok idi. İndi dəyər kəsintisi
(yuxarı) əsl cəzanı verir — blok saxlansaydı işçi **İKİ DƏFƏ** cəzalanardı
(saat itkisi + redim qadağası).

**Silinən yerlər:** `JetonService.RedimTelebiYaratAsync` (funksional blok),
`User/JetonController.Index` (`ViewBag.QaraVar`), `Views/Jeton/Index.cshtml`
(qırmızı banner + bağlı CSS şərti), `user-jeton.js` (`ujOpenRedimModal`-dakı
qapı). **`AktivQaraJetonuVarmiAsync` metodu ÖZÜ silinmədi** — HR-in
`GetIsciJetonlar` JSON cavabında hələ informativ sahə kimi qalır (heç nəyi
bloklamır, UI onu oxumur), gələcəkdə lazım olsa deyə saxlanıldı.

### Qara Jeton Kartında "Verildi və Çıxıldı" Tarixçəsi (07.10.2026)

İstifadəçi tələbi: *"indi burda qara jeton işini gördüsə, bildirmək yaxşı
olmaz? tarixçə kimi qalsın amma verildi və çıxıldı"*. İşçinin "Cüzdanım"
səhifəsindəki Qara Jeton kartı əvvəl yalnız "Cəza" yazırdı (dəyərdən asılı
olmayaraq, `ujDeyerGoster`-in köhnə şərti) — nə qədər kəsildiyi görünmürdü.

**Düstur `IsciJetonuListDto.QaraJetonKesilenSaat`** (`JetonSaatDeyeri`
TEYİNATIN cari dəyəri, tarixə dondurulmur — digər jeton kartları ilə eyni
qayda): `JetonSaatDeyeri − (bu Qara Jetona bağlı QaraJetonBorcu sətirlərinin
QalanSaat CƏMİ)`. Niyə bu işləyir:
- Heç bir borc yaranmayıbsa (tam müsbət/illik-hüquqdan ödənilib) → cəm=0 →
  **tam dəyər "kəsildi" göstərilir**.
- Borc **Gözləyir**dirsə → `QalanSaat` hələ qalır → fərq qədər "kəsildi",
  qalanı "(X saat gözləyir)" qeydi ilə göstərilir.
- Borc **Odenildi**dirsə → ödəmə zamanı `QalanSaat` artıq 0-a düşür →
  cəmə təsir etmir, tam dəyər "kəsildi" sayılır (son nəticədə yığılıb).
- Borc **MuddetiBitib** (bağışlanıb) olsa → `QalanSaat` HEÇ VAXT sıfırlanmır
  (bax `QaraJetonBorcunuOdeAsync`-dəki `kohneBorclar` dövrü) → o hissə
  **"kəsildi"yə DAXİL EDİLMİR** — düzgündür, çünki həqiqətən yığılmayıb.

**`QaraJetonKesilenSaatDoldurAsync`** həm `IsciAktivJetonlariniGetirAsync`
(işçinin Cüzdanı), həm `JetonEmeliyyatlariGetirAsync` (HR-in tam tarixçəsi)
tərəfindən çağırılır — DTO-ya mapping-dən SONRA, ayrıca toplu sorğu ilə
(`GroupBy(QaraJetonId)`), çünki `MapJeton` statik metoddur, DB sorğusu apara
bilmir. **`hr-jeton.js` HƏLƏ bu sahəni OXUMUR** (yalnız `User/user-jeton.js`-
də tətbiq olundu, istifadəçi konkret "Cüzdanım" səhifəsini göstərmişdi) —
data artıq göndərilir, HR tərəfə əlavə etmək ayrıca kiçik iş qalır.

### Qara Jeton "Xərcləmə" Pəncərəsində Seçilə Bilən Göstərilirdi (07.10.2026, KRİTİK — DÜZƏLDİLDİ)

Blok silindikdən sonra istifadəçi ekran-görüntüsü ilə göstərdi: "Jeton Xərclə"
pəncərəsində Qara Jeton (Menfi) digər mükafat jetonları (Bürünc, Qızıl) ilə
YANAŞI checkbox kimi seçilə bilən görünürdü. Səbəb: həm kartların əsas
siyahısı (`ujLoadJetonlar`), həm xərcləmə pəncərəsinin seçim siyahısı
(`ujRenderSelectList`) **eyni `ujJetonlar` keşini** oxuyur
(`IsciAktivJetonlariniGetirAsync` — yalnız `Sistemli`-ni çıxarır, `Menfi`-ni YOX,
çünki kart siyahısında Qara Jetonun görünməsi düzgündür/lazımdır). Server
tərəfi (`RedimTelebiYaratAsync`) onsuz da `Nov == JetonNovu.Musbat` şərti ilə
qorunur — real itki yox idi, amma seçim ekranı cəza jetonunu "xərclənəsi"
kimi göstərməsi yanıldıcı idi.

**Düzəliş** — `ujRenderSelectList`-in əvvəlində `ujJetonlar.filter(j =>
j.jetonNovu !== 2)` ilə seçilə bilən siyahı ayrıca hazırlanır, yalnız o render
olunur. Əsas kart siyahısı (`ujLoadJetonlar`) **toxunulmadı** — orada Qara
Jetonun görünməsi məqsədli idi.

**Qayda:** eyni keş massivi (`ujJetonlar`) iki fərqli UI məqsədi üçün
(「göstər」 vs 「seç」) işlədilirsə, hər yazıcı öz filtrini tətbiq etməlidir —
keşin "hamısını göstərir" olması "hamısı seçilə bilər" demək deyil.

### "Cəza" Hələ Görünür — BUG DEYİL, TEYİNATIN CARİ DƏYƏRİ 0-DIR (07.10.2026)

İstifadəçi "nə dəyişdi?" sualı ilə göndərdiyi ekran-görüntüsündə Qara Jeton
kartı hələ "Cəza" yazırdı (yeni "X saat kəsildi" yox). Kodun özü düzgündür —
`ujDeyerGoster`-də Menfi qol **birinci** `j.jetonSaatDeyeri <= 0` yoxlamasına
baxır və yalnız bundan SONRA `qaraJetonKesilenSaat`-ı oxuyur. `JetonSaatDeyeri`
isə digər bütün jeton kartları ilə EYNİ qaydaya tabedir (yuxarı, "Bir Elementə
İKİ YAZICI" bölməsinin əksi deyil, elə öz qaydasıdır) — `x.JetonTeyinati.SaatDeyeri`
**CANLI** oxunur, verilmə anına dondurulmur.

Yəni "Cəza" göstərilməsinin yeganə səbəbi: bu konkret Qara Jetonun bağlı olduğu
`JetonTeyinati` sətrinin HAZIRKI `SaatDeyeri`-si **0**-dır. Bu, köhnə sistemin
defolt vəziyyətidir (bax yuxarı "Qara Jeton — Dəyərli Kəsinti Mexanizmi": "əvvəllər
… `SaatDeyeri=0` — YALNIZ bayraq idi") — yeni kaskad mexanizmi dəyəri HƏR ZAMAN
`teyinat.SaatDeyeri`-dən oxuyur, amma HR həmin konkret növün dəyərini `HR → Jeton
Növləri` idarəetməsindən **müsbət ədədə** dəyişməyibsə, kəsinti də 0 olur, kart da
"Cəza" göstərir — bu, iki fərqli yerin (əvvəlki uğurlu test, 1.25 saat kəsinti)
göstərdiyi nəticədən FƏRQLİ BİR Qara Jeton NÖVÜ/sətri ola bilər, əgər kataloqda
birdən çox Menfi tipli sətir varsa.

**Yoxlama addımı istifadəçi üçün:** `HR → Jeton Növləri` (və ya bənzər idarəetmə
səhifəsi) açılıb bu konkret Qara Jetonun növünün "Saat dəyəri" sahəsinə baxılmalıdır.
0-dırsa, "Cəza" **gözlənilən düzgün nəticədir** (digər 0-dəyərli mükafat jetonları
da eyni cür "Cəza" göstərir, sətir 20) — dəyər HR tərəfindən təyin ediləndə kart
avtomatik "X saat kəsildi"yə keçəcək, kodda əlavə dəyişiklik lazım deyil.

⚠️ **BU QEYD NATAMAM ÇIXDI — əsl səbəb "0" DEYİL, "MƏNFİ" İDİ (07.10.2026,
İKİNCİ DALĞA, KRİTİK).** İstifadəçinin göndərdiyi `HR → Jeton Kataloqu`
ekran-görüntüsü göstərdi ki, Qara Jetonun "Dəyər" sütunu **`-30 saat`**-dır —
HR dəyəri `-30` kimi yazıb, "Menfi" sözünü "mənfi ədəd" kimi başa düşərək
(məntiqli görünən konvensiya: Musbat jetonlar müsbət, Menfi mənfi). Kataloq
formasında (`hjtSaat`/`hjtcSaat` input-ları) heç bir `min="0"` məhdudiyyəti
yoxdur — mənfi yazmağa HEÇ NƏ mane olmur.

**Bu, yuxarıdakı qeyddən qat-qat ciddi idi.** `QaraJetonKesintisiTetbiqEtAsync`-də
`if (deyer <= 0) return;` yazılmışdı — mənfi dəyəri "təyin edilməyib" (0) ilə
EYNİ rəftar edirdi və **bütün kəsinti kaskadını səssizcə ləğv edirdi**: FIFO
kəsinti yox, "36 Saat Hüququ"ndan kəsinti yox, `QaraJetonBorcu` sətri belə
yaranmır. 07.10.2026-da verilmiş "test" Qara Jetonu buna görə HEÇ NƏYƏ təsir
etmədi — bu, "Qara Jeton — Dəyərli Kəsinti Mexanizmi" bölməsinin bütün
məqsədini (köhnə sırf-bayraq davranışına) geri qaytarırdı, HEÇ BİR XƏTA
vermədən.

**Düzəliş — `Math.Abs` ÜÇ yerdə:**
1. `QaraJetonKesintisiTetbiqEtAsync` — `miqdar = Math.Abs(deyer)`, kəsinti
   bundan hesablanır (işarədən asılı olmayaraq eyni nəticə: HR `-30` da,
   `30` da yazsa fərq etmir).
2. `QaraJetonKesilenSaatDoldurAsync` — `Math.Abs(dto.JetonSaatDeyeri) − gozleyen`.
3. `ujDeyerGoster` (`user-jeton.js`) — `Math.abs(j.jetonSaatDeyeri)`.

**Qayda: "Menfi" enum dəyəri VERGİ/İŞARƏ semantikası ilə DATA SAHƏSİNİN işarəsini
eyniləşdirmə.** `JetonNovu.Menfi` konseptual təsnifatdır (bu jeton MÜKAFAT deyil,
CƏZADIR); `SaatDeyeri`-nin özü isə HƏR YERDƏ "kəsiləcək MİQDAR"dır — işarəsi
NECƏ YAZILIRSA YAZILSIN (HR-ın intuitiv seçimi, forma məhdudiyyəti yoxdur),
miqdarı işlədən kod `Math.Abs` ilə qoruncaqlı olmalıdır. Eyni tələ başqa bir
"Menfi/Müsbət" tipli sahə əlavə olunanda da yoxlanmalıdır.

**HR üçün tövsiyə (istifadəçi sualı, 07.10.2026: "bundan sonra jetonu − ilə
qeyd edək yoxsa mənfisiz?"): MÜSBƏT yazılsın (`30`, `-30` YOX).** `Math.Abs`
hər ikisini eyni işlədir, ona görə texniki məcburiyyət yoxdur — amma müsbət
daha təhlükəsizdir: digər bütün jeton növləri (Qızıl, Gümüş, Bürünc, Platin)
müsbət yazılır, "Menfi" artıq `JetonNovu` enum-unda var (təkrar işarə lazım
deyil), və gələcəkdə `SaatDeyeri`-ni işlədən yeni bir yer `Math.Abs`-ı unudarsa
(bu, məhz bu bugun səbəbi idi) mənfi saxlanmış dəyər səssizcə yenə səhv verər.
Mövcud Qara Jeton növünün kataloqdakı dəyəri `-30`-dan `30`-a HR tərəfindən
əl ilə dəyişilə bilər (DB sətridir, kod dəyişikliyi deyil).

⚠️ **GERİYƏ TƏTBİQ OLUNMUR.** Kaskad yalnız Qara Jeton **verilmə ANINDA** bir
dəfə işləyir (`JetonVerAsync` → `QaraJetonKesintisiTetbiqEtAsync`). Fix yalnız
BUNDAN SONRA verilən Qara Jetonlara təsir edir — 07.10.2026-dakı "test" qeydi
(artıq effektsiz yaradılıb) **retroaktiv düzəlmir**. Lazım olsa, HR onu ləğv
edib (əgər ləğv imkanı varsa) yenidən versin, ya da ayrıca SQL/əl düzəlişi
tələb olunur — bu sessiyada edilmədi, istifadəçi tələb etməyib.

### "Kəsilən Saat" CANLI Kataloq Dəyərindən Asılı İdi — Dondurulmalı İdi (07.10.2026, ÜÇÜNCÜ DALĞA, KRİTİK)

İstifadəçi "30 saat kəsildi" göstərişini (HR kataloqu `-30`-a düzəldəndən sonra)
doğru qəbul edib soruşdu: "filan vaxtı azalma olub?" Cavab araşdırılanda üzə
çıxdı: **bu rəqəm YALANÇI İDİ.** Real tarixçə: 07.10.2026-dakı "test" Qara
Jetonu YARADILAN ANDA kataloqun dəyəri **1,25** idi (hələ `-30`-a
dəyişdirilməmişdi) — kaskad O ANDA düzgün işlədi və Qızıldan **1,25 saat**
(4-dən 2,75-ə) kəsdi, FIFO-da Bürüncə çatmadı. Sonra HR kataloqu test məqsədilə
`-30`-a dəyişdi. `MapJeton`-da `JetonSaatDeyeri = x.JetonTeyinati.SaatDeyeri`
**CANLI** oxunduğu üçün (digər jeton növləri ilə eyni, qəsdən belə yazılmış
qayda) kartın "kəsilən saat" hesabı da avtomatik yenidən hesablandı: əvvəl
"Cəza" (kataloq 0 olanda), sonra "30 saat kəsildi" (kataloq -30 olanda) —
HƏR İKİSİ SƏHV, çünki HƏQİQƏTƏN kəsilən məbləğ (1,25) heç vaxt dəyişməmişdi.

**Kök səbəb:** Musbat jetonlar üçün "canlı dəyər" qaydası DÜZGÜNDÜR (hələ
xərclənməmiş mükafatın CARİ dəyəridir, HR artırsa dərhal əks olunmalıdır).
Qara Jeton üçün isə "kəsilən saat" KEÇMİŞ bir HADİSƏNİN ölçüsüdür — tarixi
fakt, kataloqun gələcək redaktəsindən TƏSİRLƏNMƏMƏLİDİR. Eyni "dondurma"
prinsipi layihədə artıq var (`Kocurme.UsdEkvivalent`, `EmeliyyatRiskiHadisesi.
TedbirZererHeddiYaradilmaAninda`) — Qara Jetona da tətbiq edilməli idi, amma
ilk tətbiqdə unudulmuşdu.

**Həll — `IsciJetonu.MenfiMiqdar`** (yeni sütun, migration
`20261007110000_QaraJetonMenfiMiqdar.cs`, yalnız `AddColumn`, `InsertData`
yoxdur): Menfi növ jeton **VERİLMƏ ANINDA** `Math.Abs(teyinat.SaatDeyeri)`
bu sahəyə yazılır (`JetonVerAsync`), kaskad (`QaraJetonKesintisiTetbiqEtAsync`)
bu dondurulmuş dəyəri işlədir (artıq canlı `teyinat.SaatDeyeri`-ni yox),
`MapJeton` da Menfi növdə `MenfiMiqdar` varsa onu üstün tutur (yoxdursa canlı
dəyərə geri düşür — aşağı bax).

⚠️ **Köhnə (bu migrasiyadan ƏVVƏL yaradılmış) Qara Jetonlar DÜZƏLMİR.**
`MenfiMiqdar` onlarda NULL-dur, kod canlı dəyərə geri düşür — 07.10.2026-dakı
"test" qeydi bu fix-dən SONRA da "30 saat kəsildi" göstərməyə davam edəcək
(indi kataloq `-30`-da qaldığı müddətcə), çünki onun həqiqi tarixi miqdarını
(1,25) geriyə bərpa edən ayrıca ledger/audit jurnalı yoxdur. Bu, "test" qeydi
olduğu üçün zərərsizdir — real nəticəsi istifadəçiyə açıq bildirilib: lazım
olsa HR onu ləğv edib yenidən versin, bundan sonrakı HƏR Qara Jeton isə düzgün
dondurulmuş rəqəmi göstərəcək.

**Qayda: "kataloqun canlı dəyərini göstər" qaydası YALNIZ hələ XƏRCLƏNMƏMİŞ
mükafatlar üçün düzgündür.** Artıq BAŞ VERMİŞ bir hadisəni (kəsinti, ödəniş,
tətbiq olunmuş cəza) göstərən HƏR sahə VERİLMƏ/BAŞ VERMƏ ANINDA dondurulmalıdır
— əks halda kataloqun sonrakı redaktəsi tarixi səssizcə təhrif edir.

### Qara Jeton Kəsintiləri "Xərcləmə Tarixçəsi" Sekmesində (07.10.2026)

İstifadəçi tələbi: artıq tədbiq edilmiş Qara Jeton kəsintiləri "tədbiq edildi"
kimi qeyd olunsun və Cüzdanımın "Xərcləmə Tarixçəsi" sekmesində görünsün.
Seçilən variant (3 seçimdən): **AYRICA bölmə** — Qara Jeton kəsintisi işçinin
öz seçimi olan Redim sorğusu ilə EYNİ cədvəldə qarışdırılmır (konseptual
fərqli hadisələrdir: biri işçinin iradəsi, digəri avtomatik cəza).

**Tətbiq:** `Areas/User/Views/Jeton/Index.cshtml` — "Xərcləmə Tarixçəsi"
panelinin yuxarısına `ujKesintiWrap` bloku (yalnız Qara Jeton varsa görünür),
"Jeton Xərcləmə Sorğuları" cədvəli ilə yanaşı, ayrıca başlıq altında.
`user-jeton.js` — `ujRenderKesintiler()` **yeni AJAX çağırışı APARMIR**, artıq
"Jetonlarım" tabı üçün yüklənmiş `ujJetonlar` keşini (`GetJetonlarim`) filtrləyir
(`jetonNovu === 2`) — iki yer (kart + bu cədvəl) EYNİ `ujDeyerGoster()`
funksiyasından oxuduğu üçün rəqəm heç vaxt uyğunsuzlaşa bilməz (bax "Bir
Elementə İKİ YAZICI" qaydası — burada əksinə, İKİ OXUYUCU bir mənbədən oxuyur,
bu, təhlükəsiz formadır). Çağırılma nöqtələri: `ujLoadJetonlar()` (hər dəfə
keş yenilənəndə) və `ujSwitchTab('redimler')` (tab açılanda, ehtiyat üçün).

Status sütunu: `kəsilən >= dəyər` olanda "Tədbiq edilib" (yaşıl,
`uj-status--tesdiqlendi`), qismən qalıbsa "Qismən gözləyir" (sarı,
`uj-status--gozlenilir`) — hər ikisi mövcud CSS klasları, yeni stil əlavə
olunmayıb.

### Fayllar

`JetonTeyinati.Sistemli` (yeni sütun), `IsciJetonu.QaraJetonId` (yeni, FK
YOXDUR — sadə audit üçün int), `QaraJetonBorcu` (yeni cədvəl) —
migration `20261007100000_QaraJeton.cs` (yalnız sütun/cədvəl, **INSERT yoxdur** —
"36 Saat Hüququ" tipi runtime-da `EnsureIllikHuquqTeyinatiAsync` ilə öz-özünə
yaranır, CLAUDE.md-dəki `InsertData` tələsinə düşməmək üçün qəsdən belə).
`DashboardService` `IJetonService` inject edir (yeni asılılıq).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı, metod imzaları, DTO sahələri, namespace-lər bir-bir tutuşduruldu) —
bax "`FindFirstValue` CS1061" bölməsindəki xəbərdarlıq. İstifadəçi Visual Studio-da
**Rebuild All** ilə yoxlamalı, həm də real ssenari ilə test etməlidir (qara jeton
ver → balans azalsın → müsbət jeton ver → borc ödənsin) — "Maliyyəyə/balansa
toxunan hər dəyişiklik" qaydasına görə.

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

### Nümunə — VM-də sahə var, view-da input yoxdur (24.09.2026)

Yuxarıdakı qaydanın eyni sinifdən başqa forması: sahə view-dan **ümumiyyətlə
göndərilmirsə** (checkbox belə yoxdur, bütöv `<input>` yoxdur), ModelBinder onu
`null`/default alır və servis bunu **şərtsiz yazır** — nəticə eynidir: səssiz itki.

`TeyinatRedakteVM.BitmeTarixi` (`DateTime?`) servis qatında (`IsciService.
TeyinatRedakteEtAsync`) tam dəstəklənirdi və controller-in GET action-ı onu
mövcud təyinatdan düzgün doldururdu (`TeyinatRedakte` GET, `IsciController.cs`),
amma `TeyinatRedakte.cshtml` view-da bu sahə üçün **heç bir `<input>` yox idi** —
yalnız Departament/Vəzifə seçimi var idi. Nəticədə formanı göndərən istifadəçi
mövcud bitmə tarixini bilmədən **silərdi** (POST-da sahə heç gəlmədiyi üçün
ModelBinder `null` bağlayır, servis `entity.BitmeTarixi = null` yazır).

Tapılma səbəbi: işçinin müqaviləsinin bitmə tarixi yaradılış zamanı səhvən boş
qalmışdı və HR onu "Tayinat Redaktə"də axtarırdı — sahə heç görünmürdü.
**Düzgün yol bu vaxta qədər yalnız "Müqavilə Yenilə" idi**
(`MuqavileYenilemeController`, `YeniBitmeTarixi` `[Required]`) — o, ayrı
tarixçə sətri yaradan fərqli bir axındır.

**Düzəliş (24.09.2026):** `TeyinatRedakte.cshtml`-ə `asp-for="BitmeTarixi"
type="date"` sahəsi əlavə edildi — GET action onsuz da dəyəri doldururdu, indi
istifadəçi onu görür və dəyişdirmədən göndərsə eyni dəyər geri yazılır (silinmir).

**Qayda: VM-də sahə + servisdə yazma varsa, view-da onun inputu da olmalıdır.**
Yeni sahə əlavə edəndə "backend hazırdır" demək kifayət etmir — formanın
özündə görünüb-görünmədiyini yoxla. Əks halda sahə mövcuddur, işləyir,
sadəcə heç kim ona toxuna bilmir (və naməlum halda POST edilsə səssizcə silinər).

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

## Razor — ŞƏRHDƏ TAG HELPER ADI (RZ1034)

`.cshtml` faylında **şərh də Razor parser-indən keçir**. CSS və ya HTML şərhinin
içində tag helper hədəflənən elementin adını bucaq mötərizədə yazsan, Razor onu
əsl tag sayır və qapanan cütünü axtarır:

```
RZ1034: Found a malformed 'form' tag helper.
        Tag helpers must have a start and end tag or be self closing.
```

Real hadisə (08.09.2026, `_FormStyles.cshtml:131`): `<style>` blokunun şərhində
«Sinfi JS \`&lt;form&gt;\`-a qoyur» yazıldı → **bütün `FinNex.UI` build olmadı**.
Xəta CSS faylını göstərir, halbuki orada bir sətir də HTML yoxdur.

**Qaydalar:**
- Şərhdə element adını bucaqsız yaz: «forma elementinə», «`input` sahəsi».
- Risk yalnız **tag helper hədəflənən** elementlərdədir — `form`, `input`,
  `select`, `textarea`, `a`, `img`, `label`, `link`, `script`, `environment`.
  `<b>`, `<tr>` kimi adi taglar bu xətanı vermir (ona görə `<script>` içindəki
  `'<b>' + x + '</b>'` sətri problem yaratmır).
- **`@@{ }` blokundakı C# şərhi təhlükəsizdir** — o, markup deyil. `_Form.cshtml:8`
  onilliklərdir `<input type="number">` yazır və build olur.
- CSS/JS şərhi isə markup kontekstindədir — orada ehtiyatlı ol.

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

## Məzuniyyət Tarixçə Excel-i (ClosedXML) — Çərçivə (30.09.2026)

`MezuniyyetBalansController.IsciExcel` (işçinin şəxsi məzuniyyət tarixçəsi,
mühasibin "İşçilərin məzuniyyətləri.xlsx" şəxsi vərəqinin qarşılığı) —
**HTML-trick YOX**, real `ClosedXML.XLWorkbook` ilə `.xlsx` yaradır (bu
səbəbdən yuxarıdakı `x:num` qaydası bura aid deyil — ClosedXML rəqəmi
birbaşa hüceyrə tipində yazır, mətnə çevirmir).

İlk versiyada başlıq bloku (1-6 sətir) və data cədvəli **çərçivəsiz** idi —
yalnız qrup/sütun başlıqları (8-9 sətir) `OutsideBorder`la örtülmüşdü, daxili
xətlər yox idi. İstifadəçi real Excel-də açıb gördü: "exceli çərçivəyə sal,
səliqəli görünsün". Düzəliş:

- Başlıq bloku (sətir 1-6, sütun 1-7) — `OutsideBorder` + `InsideBorder`
  (tam grid) + açıq boz fon (`XLColor.FromArgb(245,247,250)`), 5/6-cı sətrin
  uzun mətnli xanaları (`Uşaqların təvəllüdü`, `Çap tarixi`) 2-7 sütuna
  **birləşdirilib** (`Merge()`) ki, boş bordürlü xanalar sağda qalmasın.
- Qrup/sütun başlığı (8-9) — `InsideBorder` əlavə edildi (əvvəl yalnız
  bayıra idi), `WrapText` + şaquli mərkəz — uzun başlıq mətni sütun enini
  şişirtmədən yerləşsin.
- Data cədvəli (8-cü sətirdən son yazılan sətrə qədər, bütün sütunlar) —
  loop bitəndən sonra (boş nəticə mesajı da daxil olmaqla) bir dəfə
  `OutsideBorder` + `InsideBorder` tətbiq olunur.

`XLColor.FromArgb` naxışı yeni deyil — `TabelController.cs` onsuz da
istifadə edir (rəng sabitləri), ona görə bu API-nin işlək olduğu əvvəlcədən
təsdiqlənmişdi.

## HTML-dən Excel İxracı — `x:num` Olmasa Sütun TOPLANMIR (KRİTİK)

Layihədə «Excel» ixracı əslində **HTML cədvəlidir** (`.xls` adı ilə, `xmlns:x=
"urn:schemas-microsoft-com:office:excel"`). Rəqəm xanası sadəcə mətn kimi
yazılırsa (`6003,86` — az-AZ vergülü ilə), **ingilis lokalında işləyən Excel
onu RƏQƏM SAYMIR**: sütunu seçəndə status zolağında cəm ümumiyyətlə çıxmır,
`SUM()` sıfır verir. Heç bir xəta yoxdur — sadəcə hər şey mətndir.

**Qayda:** hər rəqəm xanasına **`x:num` atributu** ilə xam dəyəri (NÖQTƏ ilə)
də göndər — Excel dəyəri oradan götürür, mətnə baxmır, lokaldan asılı deyil:

```js
const num  = v => Number(v).toFixed(2).replace('.', ',');  // insan oxuyan
const xnum = v => Number(v || 0).toFixed(2);               // Excel üçün xam
`<td x:num="${xnum(x)}" style="text-align:right">${num(x)}</td>`
```

- **Cəmi sətrini də unutma** — yalnız sətirlərə qoysan, «CƏMİ» sətri mətn qalar.
- **Mətn sütununa `x:num` QOYMA** (IBAN, hesab №, nömrə): Excel onu ədədə çevirər,
  öndəki sıfırlar itər və uzun nömrə eksponentə düşər.
- Yeni sütun əlavə edəndə `x:num`-u da əlavə et — unudulan sütun səssizcə mətn
  olur və yalnız «toplanmır» şikayəti ilə üzə çıxır (27.08.2026, real hadisə).

## Excel OXUNUŞU (ClosedXML) — SƏRT FƏRZİYYƏ = SƏSSİZ BOŞ NƏTİCƏ (22.09.2026)

İstifadəçinin yüklədiyi faylın quruluşunu **bilmirsən**. Risk → Məlumat Bazası
oxuyucusu üç sərt fərziyyə qurmuşdu və üçü də real faylda pozuldu:

| Fərziyyə | Pozulanda |
|---|---|
| data 1-ci vərəqdədir (`wb.Worksheet(1)`) | boş vərəq → «sətir tapılmadı» |
| 1-ci sətir başlıqdır (`.Skip(1)`) | başlıqsız faylda **yeganə data sətri yeyilir** |
| sütunlar A/B/C-dir | başqa sıralamada dəyərlər **səhv sahəyə** düşür, xəta yox |

**İki konkret ClosedXML tələsi:**

1. **`ws.RangeUsed()` BOŞ vərəqdə `null` qaytarır.** `RangeUsed()!.RowsUsed()`
   yazılmışdı — `!` yalnız kompilyatoru susdurur, icra anında
   `NullReferenceException` → ekranda *«Object reference not set to an instance
   of an object»*. Səbəb mətndən heç cür görünmür.
2. **`RangeUsed()` üzərindəki sətirdə `Cell(1)` NİSBİDİR** — istifadə olunan
   aralığın birinci sütunudur. A sütunu boşdursa `Cell(1)` = B olur və sütunlar
   **səssizcə sürüşür**. Bu ikincisi daha təhlükəlidir: xəta vermir, sadəcə
   ad VÖEN xanasına düşür.

**Qaydalar:**
- **`ws.RowsUsed()` işlət** — boş vərəqdə boş kolleksiya qaytarır və `Cell(n)`
  HƏMİŞƏ mütləq sütundur.
- Data olan **ilk vərəqi tap**, `Worksheet(1)`-ə bağlanma.
- Başlıq sətrini **axtar** (ilk ~10 sətirdə «Ad Soyad»/«VÖEN»/«FİN» sözləri),
  sütunları ada görə xəritələ. **Başlıq tapılmasa HEÇ NƏ ATMA** — `Skip(1)`
  başlıqsız faylda datanı yeyir.
- Azəri başlıqlarını müqayisədən əvvəl sadələşdir (`ə→e, ö→o, ü→u, ı→i, ğ→g,
  ş→s, ç→c` + `İ`-nin `ToLowerInvariant` qalıq nöqtəsi `\u0307`) — yoxsa
  «VÖEN» ilə «VOEN» fərqli sayılar.
- **Nəticə boş çıxanda faylda NƏ GÖRDÜYÜNÜ yaz** — ilk sətirlərin ilk
  xanalarını mesaja qoy. «Sətir tapılmadı» tək başına istifadəçini də, növbəti
  sessiyanı da kor qoyur.
- Oxunuşun **necə** aparıldığını ekranda göstər (vərəq adı, başlıq sətri, sütun
  hərfləri) — «niyə sütunlar sürüşüb» sualı koda baxmadan cavablansın.
- `.xlsx` olmayan faylı **adına görə əvvəlcədən rədd et**: köhnə `.doc`/`.xls`
  (OLE2) ClosedXML/OpenXML ilə açılmır, kitabxana mətni isə anlaşılmaz olur.

### İki addımlı axın: əvvəl GÖSTƏR, sonra SORĞU

İstifadəçi qərarı: «həmin exceli tabledə göstərsin və **sonra** bazada axtarmaq
işlərinə getsin buton ilə». Yükləmə Oracle-a sorğu **göndərmir** — yalnız oxuyub
cədvəldə göstərir; BMI sorğusu ikinci addımdadır.

Siyahı addımlar arasında **gizli sahədə JSON** kimi daşınır (fayl input-u
yenidən doldurula bilmir, `TempData` isə bu həcmi saxlamır). Hədd:
`FormOptions.ValueLengthLimit` defolt **4 MB** — sətir başına ~100 bayt, yəni
~40 000 sətir. Daha böyük siyahı lazım olsa fayl müvəqqəti saxlanmalıdır
(`C:\FinNex_DMS\`), JSON həddi artırılmamalıdır.

`AxtarisNeticeDto.AxtarisEdildi` bayrağı **məcburidir**: onsuz ekran
«tapılmadı» ilə «hələ axtarılmayıb» halını ayırd edə bilmir — ikisi də boş
`Uygunluqlar` deməkdir.

### Axtarış Rejimi — Ad / FİN-VÖEN / Hər ikisi (25.09.2026)

«Məlumat Bazası» (Risk → Dashboard, 2-ci addım) əvvəl tək `bool yalnizFinVoen`
checkbox-u idi (işarəli — yalnız FİN/VÖEN, işarəsiz — hər ikisi). İstifadəçi
tələbi ilə **üçüncü rejim** ("yalnız Ad") əlavə olundu, `bool` **enum**-a
çevrildi: `FinNex.Application.DTOs.Aml.AxtarisRejimi { HerIkisi, Ad, FinVoen }`.

Sentinel məntiqi eynidir, sadəcə istiqamət artıb — `BmiLatin.SiyahiQur`:
- `FinVoen` → `a_s_a` = `AdYoxdur` (ad şərti söndürülür, mövcud idi);
- `Ad` → `fin`/`voen` = `Bos` ("~") (FİN/VÖEN şərti söndürülür, YENİ);
- `HerIkisi` → heç biri toxunulmur (default).

View-da checkbox+gizli-sahə cütü **`<select name="rejim">`** ilə əvəz olundu —
radio qrupu YOX, çünki radio-da heç biri seçilməsə POST-da sahə ümumiyyətlə
getməz (checkbox tələsinin eyni forması); `<select>`-də həmişə seçili dəyər
var, səssiz itki riski yoxdur. ASP.NET Core `enum`-u `<option value="Ad">` kimi
adına görə avtomatik bağlayır — əlavə çevirmə lazım deyil.

**Dəyişən üç qat:** `IMelumatBazasiService.HazirlaAsync` (parametr),
`MelumatBazasiService.HazirlaAsync` (implementasiya + boş-nəticə mesajı),
`DashboardController.MelumatBazasiPaket` (action parametri) — imza dəyişikliyi
qaydasına (yuxarıda) uyğun **üçü də eyni anda** yeniləndi.

### Real şablon: `AMLexcel.xlsx`, vərəq «Axtarilanlar»

İşçinin doldurduğu fayl **dörd sütunludur**: `A=Adlar`, `B=VOEN`, `C=fin`,
`D=novu`. Başlıqlar Azəri hərfsiz və kiçik hərflə yazılır — tanıma buna görə
`Sadeles` ilə normallaşdırılmış müqayisə üzərində qurulub, hərfi bərabərlik
YOX (`«Adlar»` → `startsWith("ad")`, `«novu»` → `startsWith("nov")`).

⚠️ **«Növü» AXTARIŞDA İŞTİRAK ETMİR** — oxunur, cədvəldə və Excel ixracında
göstərilir, vəssalam. Nə demək olduğu (fiziki/hüquqi? siyahının mənbəyi?)
**istifadəçidən soruşulmayıb**. Uyğunluq qaydasına təsir etməlidirsə, əvvəlcə
soruş — özbaşına fərz etmə.

Boş sətir şərti: `ad`, `VÖEN`, `FİN` **üçü də** boşdursa sətir atılır. Yalnız
«Növü» dolu olması sətri saxlatmır — axtarılacaq heç nə yoxdur.

### FİN/VÖEN BİR SÜTUNDA DA OLA BİLƏR (23.09.2026)

İstifadəçi qərarı: operator adamın FİN, yoxsa VÖEN olduğunu bilməli deyil —
Exceldə **tək sütun** («FİN/VOEN» kimi başlıq) kifayətdir, sistem hər xananı
**formatına görə** ayırd edir (`DashboardController.SinifleFinVoen`):
VÖEN = yalnız rəqəm, 9-10 xanə; əks halda (7 simvol, hərf+rəqəm) FİN sayılır.

Sütun aşkarlanması (`ExceldenOxu`) əvvəlcə **birləşmiş** başlığı yoxlayır —
başlıqda HƏM «fin», HƏM «voen» sözü birlikdə varsa (`h.Contains("fin") &&
h.Contains("voen")`) tək sütun kimi oxunur. Yalnız «fin» VƏ YA yalnız «voen»
olan **ayrı** başlıqlı köhnə fayllar da eyni məntiqlə işləməyə davam edir —
iki format paralel dəstəklənir, biri o birini əvəz etmir.

⚠️ **BU SƏTR (yuxarıdakı, 23.09.2026) YANLIŞ ÇIXDI — 25.09.2026-da DÜZƏLDİLDİ.**
«İstifadəçi onsuz da düzgün sütuna yazıb» fərziyyəsi real data ilə pozuldu.

### Ayrı «VÖEN» Sütununda da FİN-formatlı Dəyər Ola Bilər (25.09.2026, KRİTİK)

Real hadisə: Excel faylında yalnız «VÖEN» başlıqlı sütun var idi (ayrıca «FİN»
sütunu yoxdur, `finC` = 0), operator həmin sütuna **FİN-formatlı dəyər**
(`65GPNXJ`, 7 simvol) yazmışdı. Köhnə kod bunu **olduğu kimi** `Voen` sahəsinə
yazırdı, `Fin` boş qalırdı. Nəticə: «Hər ikisi» rejimində axtarış **ad** üzrə
tapırdı (`Ad (göndərən)`) və bu, FİN-in də işlədiyi təsürini yaradırdı; «Yalnız
FİN/VÖEN» rejimində isə heç nə tapılmadı — çünki VÖEN-mətndə şərti
`length(trim(y.voen)) >= 9` tələb edir, 7 simvollu dəyər bu həddi keçmir, FİN
isə boş (`~`) olduğu üçün öz şərtinə heç girmirdi. Diaqnoz saatlarla çəkdi,
çünki SQL-in özü (Oracle-da əl ilə test edilib) **tam düzgün** idi — səbəb heç
Oracle-da deyildi, Excel oxuma mərhələsində idi.

**Həll — `ExceldenOxu`, ayrı-sütun budağı:** `voen`/`fin` oxunandan sonra hər
ikisi `SinifleFinVoen` ilə **yenidən yoxlanır** (əvvəl yalnız BİRLƏŞMİŞ sütunda
tətbiq olunurdu). VÖEN sahəsindəki dəyər 9-10 rəqəmli deyilsə və FİN sahəsi
boşdursa → FİN-ə köçür, VÖEN boşalır (və əksinə, FİN sahəsində 9-10 rəqəmli
dəyər tapılsa VÖEN-ə köçür). Yalnız **qarşı sahə boş olanda** köçürülür ki, hər
iki sütunu düzgün dolduran köhnə fayllara toxunmasın.

**Qayda: bir sütunun başlığı onun MƏZMUNUNU zəmanət etmir.** Operator «VÖEN»
yazılmış sütuna səhvən FİN yaza bilər (əksinə də) — sistem hər hansı FİN/VÖEN
xanasını **HƏMİŞƏ** formatına görə yoxlamalıdır, təkcə birləşmiş sütunda yox.

### BÖYÜK SİYAHI — Batch-lərə Bölünmə (23.09.2026, KRİTİK)

`BmiLatin.SiyahiQur` axtarılan şəxsləri `{SIYAHI}` blokuna (`union all select …`)
yapışdırır və bu blokun ölçüsü test edilmiş bir hədlə (`BmiLatin.MaxSetir = 5000`)
məhduddur — ondan çoxu Oracle-a göndərilən SQL mətnini təhlükəli dərəcədə
böyüdür. Real hadisə: 17554 sətirlik siyahının yalnız **ilk 5000-i** axtarılırdı,
qalan **12554 sətir səssizcə (xəbərdarlıqla, amma icrasız) atılırdı**.

**Həll — `MelumatBazasiService.HazirlaAsync` siyahını avtomatik BÖLÜR:**
`BmiLatin.MaxSetir`-lik hissələrə ayrılır, hər hissə üçün AYRI `{SIYAHI}` bloku
qurulur, 11 sorğunun HƏR BİRİ hər batch üçün AYRICA icra olunur (vərəq × batch
tapşırıq), nəticələr sonda vərəq üzrə **birləşdirilir**. Yeni ümumi hədd:
**`MelumatBazasiService.MaxUmumiSetir = 50000`** (10 batch) — bundan çoxu hələ
də kəsilir və istifadəçiyə açıq bildirilir.

**Paralellik dəyişmir** — `SemaphoreSlim(MaxParalel=4)` batch sayından asılı
olmayaraq Oracle-a eyni anda ən çox 4 sorğu buraxır; batch sayı artanda YALNIZ
növbə uzanır, Oracle-a düşən yük eyni qalır. Nəticə isə batch sayı qədər çox
vaxt aparır (17554 sətir = 4 batch → təxminən 4× vaxt).

⚠️ **Nəticə birbaşa `vereq.Setirler`-ə yazılmır** — eyni vərəqin bir neçə
batch-i paralel bitə bilər və `List<T>.Add` thread-safe deyil (Bildirişlər —
Paralel Yazı hadisəsi ilə EYNİ tələ). Hər batch öz nəticəsini ayrı `BatchIsi`
obyektində saxlayır, `Task.WhenAll`-dan **SONRA** (artıq ardıcıl mərhələdə)
vərəq üzrə birləşdirilir. Yeni yazma yolu əlavə edəndə bu sıraya riayət et.

Yükləmə ekranındakı kəsilmə xəbərdarlığı (`DashboardController.MelumatBazasiYukle`)
indi `_mb.MaxUmumiSetir`-i oxuyur, `BmiLatin.MaxSetir`-i YOX — ikisi fərqli
kəmiyyətdir (tək batch ölçüsü vs ümumi hədd), qarışdırma.

⚠️ **Batch sayı = daha çox Oracle round-trip = daha uzun sorğu vaxtı.**
4 dövrsüz sorğu (Owner, A_M_L, Kred_zamin, Aktiv_hesablar) tarix aralığından
ASILI DEYİL — vaxtları YALNIZ siyahının ölçüsündən (`{SIYAHI}` bloku) asılıdır.
Yəni «1 günlük tarix seçdim, niyə hələ sürünür» sualının cavabı çox vaxt
**siyahının ölçüsüdür**, dövr deyil — 17554 sətir 4 batch-ə bölünür, 44 Oracle
sorğusu gedir (əvvəl 11 idi). Uzun müddət `MelumatBazasiPaket` sinxron POST-dur
(progress-bar/websocket yoxdur) — çox böyük siyahılarda IIS/ASP.NET Core-un
default sorğu vaxt aşımına (adətən 100-230 san) dəyə bilər; bu hələ REAL
hadisə ilə görülməyib, yalnız nəzəri risk kimi qeyd olunur.

**İstifadəçi qərarı (23.09.2026): `MaxParalel` ARTIRILMIR.** 17554 sətirlik real
siyahı 4 batch-də ≈10 dəqiqə çəkdi (60 saniyəlik `CommandTimeout` × ~11 dövr
riyaziyyatına uyğun gəlir — bax yuxarı). İstifadəçi Oracle-a yükü artırmaq
əvəzinə **əməliyyat qaydasını** seçdi: böyük siyahılar operatorlar tərəfindən
**əl ilə ~1000 sətirlik fayllara bölünüb ayrı-ayrı yüklənəcək**. 1000 sətir
`BmiLatin.MaxSetir` (5000) həddindən çox aşağı olduğu üçün TƏK batch kimi işə
düşür — yəni əvvəlki (batch-siz) 11-sorğuluq sürətlə işləyir, kodda dəyişiklik
tələb etmir. Batch-ləmə məntiqi (yuxarıda) TOXUNULMAYIB — böyük siyahı YENƏ DƏ
yüklənərsə (məs. kimsə qaydaya əməl etməsə), avtomatik bölünüb axtarılacaq,
sadəcə uzun çəkəcək.

### Defolt Tarix — `SonTarix` GƏLƏCƏYƏ DÜŞMƏMƏLİDİR (23.09.2026)

`VarsayilanNetice()` əvvəl `SonTarix`-i **cari ayın son GÜNÜNƏ** (məs. 23.09-da
açılsa belə → 30.09.2026) sabitləyirdi. Ay hələ bitməyibsə bu, GƏLƏCƏK tarixdir
və o günə qədər data onsuz da yoxdur — istifadəçi «niyə 30 verir, bugünkü tarix
deyil axı» sualı verdi. İndi sadəcə `DateTime.Today`. `BasTarix` (keçən ayın son
günü) toxunulmadı — o, artıq BİTMİŞ bir ay olduğu üçün problemsizdir.

⚠️ **23.09.2026, İKİNCİ DÜZƏLİŞ — hər iki xana EYNİ GÜNƏ sabitləndi.** İstifadəçi
qərarı: operator adətən TƏK GÜNLÜK aralıq seçir (test axtarışları belə idi),
ona görə `BasTarix` artıq keçən ayın son gününə YOX, `SonTarix` ilə EYNİ günə
(`DateTime.Today`) sabitlənir — operator lazım gəldikcə aralığı əl ilə açır.
Eyni zamanda ekrandakı etiketlər dəyişdi: **"Əvvəlki ay son iş günü"** →
**"Dövr əvvəl"**, **"Cari ay son iş günü"** → **"Dövr son"**
(`MelumatBazasi.cshtml`) — köhnə adlar artıq həqiqətə uyğun deyildi, çünki
`BasTarix` artıq «əvvəlki ay» demək deyil.

## İcazə — «Plan Üzrə Sayım» vs Real Ölçmə (23.09.2026, KRİTİK)

İşçi icazə yazıb, amma pəncərədə cihaza vurmayıbsa (getməyibsə) sistem nə edir?
**İstifadəçi qərarı: «bu onun problemidir, sistem plan qədər hesablasın».** Bu
qərar özü düz idi, amma tətbiqi **iki yerdə ziddiyyətli** idi — Dövriyyə səhifəsi
«0 saat» göstərirdi, Dashboard isə eyni qeyd üçün **3 saat** balansdan düşürdü.

### Kök səbəb — ÜÇ QATLI mexanizm

**1-ci qat (canlı, `ADMSController.ProcessIcazeCixisGirisAsync:602`):** işçi
pəncərədə çıxmayıbsa, sistem **YUXARI HƏDD OLMADAN** növbəti istənilən punch-u
icazə çıxışı sayır:
```csharp
else if (vaxt >= icazeBaslamaDateTime.AddMinutes(-15))
    cg.CixisVaxt = vaxt;   // ⚠️ heç bir yuxarı sərhəd yoxdur
```
İşçi pəncərədə (09:00–12:45) heç yerə çıxmayıb, günün sonunda **normal iş günü
çıxışı** edibsə (17:03) — bu, səhvən icazə çıxışı kimi tutulur.

**2-ci qat (gecə, `PlanUzreBaglamaBackgroundService.cs:164`, 23:00-dən sonra):**
çatışmayan yarını (adətən qayıdışı) **PLAN üzrə** doldurur və qeydi **şərtsiz**
`Tamamlandı` edir — çıxış/qayıdış sırasının məntiqli olduğunu yoxlamadan:
```csharp
cg.Status = IcazeCixisGirisStatus.Tamamlandi;   // sağlamlıq yoxlaması yoxdur
```
Nəticə: çıxış (17:03, canlı, YANLIŞ bağlanmış) > qayıdış (12:45, plan, sintetik) —
məntiqsiz cüt, amma «Tamamlandı».

**3-cü qat (oxuma zamanı, İKİ AYRI YERDƏ FƏRQLİ):**
- `IcazeCixisGiris.FaktikiSaat` (Domain, DÜZƏLDİLDİ): əvvəl `QayidisVaxt −
  CixisVaxt`-ı **şərtsiz** qaytarırdı → mənfi (−4,3 saat). Bu, Dövriyyə
  səhifəsinin nahar-düzəlişindəki `Math.Max(0, …)`-a düşəndə **təsadüfən 0-a**
  sıxılırdı — «0 saat» real ölçmə DEYİL, gizli səhvin nəticəsi idi.
- `DashboardService.IcazeIstifade` — **statik** `IcazeService.IcazeFaktikiSaat`
  köməkçisini çağırır, o, `qayidis <= cixis` olanda düzgün `null` qaytarır, amma
  sonra `null`-u **«hələ baş verməyib»** ilə **«baş verib, data qırıqdır»**
  eyni cür oxuyub hər ikisində **PLANI** kreditə yazır.

### Həll (23.09.2026)

- `IcazeCixisGiris.FaktikiSaat` artıq `QayidisVaxt > CixisVaxt` şərtini yoxlayır —
  mənasız cüt üçün **`null`** qaytarır, mənfi ədəd bir daha görünmür.
- `IcazeService.GetDovriyyeAsync` (`PlanUzreSayGorEhtiyacOlsa`): qeyd bağlıdır
  (Tamamlandı), amma `FaktikiSaat` ölçülə bilmirsə — Dövriyyə də **eyni planı**
  göstərir (`dto.EffektivPlanSaat`) və `SayilanPlanUzredir=true` bayrağı ilə
  «plan üzrə» nişanı çıxır. **Artıq Dövriyyə və Dashboard EYNİ ədədi göstərir.**
- `IcazeListDto.IstifadeSaati` və `DashboardService.IcazeIstifade` — dəyişməyib
  (onsuz da planı sayırdı, istifadəçi qərarına uyğundur).

### HR ləğv imkanı (istifadəçi tələbi: «belə müraciətlər ola bilər»)

İşçinin ÜZÜRLÜ səbəbi ola bilər (təcili çağırılıb, rəhbər saxlayıb) — HR bu
KONKRET qeydin **yalnız plan-sayımını** ləğv edə bilər, icazənin özünə
(Status/Silinib/jeton) TOXUNULMUR:

- Sahələr: `IcazeCixisGiris.PlanSayimiLegvEdildi/Sebebi/Tarixi/EdenIsciId`
  (migration: `20260923090000_IcazePlanSayimLegvi`).
- Servis: `IcazeService.PlanUzreSayimiLegvEtAsync` — yalnız `FaktikiSaat == null`
  olan qeydə tətbiq olunur (real ölçülmüş qeydi HR «ləğv» edə bilməz).
- `DashboardService.IcazeIstifade` və `IcazeListDto.IstifadeSaati` bu bayrağı
  yoxlayır → ləğv edilmiş qeyd balansdan **0** düşür.
- Endpoint: `POST /User/Icaze/PlanSayimiLegv` (yalnız **HR/Admin** — istifadəçi
  qərarı, Rəhbər/ŞöbəReisi bura daxil deyil). `RehberHrLegvEtAsync`-dən FƏRQLİDİR
  — o, bütöv icazəni ləğv edir; bu, YALNIZ balansdan düşən saatı.
- **`RehberHrLegv`-lə EYNİ konvensiya**: `[ValidateAntiForgeryToken]` QƏSDƏN
  yoxdur (JS `fetch`+`prompt()` üsulu, token ötürmür) — əlavə etsək token
  uyğunsuzluğu POST-u səssizcə 400 ilə sındırardı.

⚠️ **Sintetik (CixisGiris qeydi olmayan) icazələrdə bu düymə yoxdur** — ləğv
ediləcək qeydin özü mövcud deyil. HR əvvəlcə `CixisGirisDuzelt` ilə qeyd
yaratmalıdır, sonra plan-sayımı ləğv edə bilər.

### Kök səbəb düzəlişi — ADMSController-ə YUXARI HƏDD (23.09.2026, İKİNCİ DALĞA)

İlk düzəlişdə (yuxarı) **1-ci qat (ADMSController) qəsdən toxunulmamışdı** —
göstərmə/balans tərəfi düzəldilmişdi, amma kök səbəb qalırdı: canlı proses
`vaxt >= icazeBaslamaDateTime.AddMinutes(-15)` kimi YALNIZ **aşağı** hədd
yoxlayırdı, yuxarı hədd yox idi. Nəticədə icazə pəncərəsində (məs. 09:00–12:45)
heç bir punch olmayanda, günün sonunda gələn **istənilən** növbəti punch (adi
işdən çıxış, 17:03) icazənin çıxışı/qayıdışı kimi yazılırdı.

İstifadəçi bunu təsdiqlədi və konkret həll təklif etdi: *«cihaza baxanda sistem
yoxlasın ki, bu aralıqda onun icazəsi varmı? varsa bu icazənin başlanğıcıdır,
yoxdursa artıq işdən çıxışdır»*. Bu, məhz `PlanUzreBaglamaBackgroundService`-in
(gecə xidməti) onsuz da işlətdiyi pəncərə məntiqi idi (`BaslamaSaati−30dəq …
BitisSaati+30dəq`, xam punch varsa götür, yoxdursa PLAN yaz) — sadəcə CANLI
proses həmin pəncərəni tətbiq etmirdi.

**Düzəliş:** `ADMSController.ProcessIcazeCixisGirisAsync`-da HƏR ÜÇ budağa
(səhər-icazəli qayıdış, gün-ortası çıxış, ikinci-skan qayıdış) eyni yuxarı hədd
əlavə edildi: `IcazePunchToleransDeq = 30` (gecə xidməti ilə **EYNİ ədəd** —
sinxron qalmalıdır). Punch bu həddən (`BitisSaati + 30 dəq`) sonradırsa,
icazəyə **TOXUNULMUR** — CixisGiris sahəsi boş qalır, adi davamiyyətə öz yolu
ilə yazılır, gecə xidməti sonra düzgün bağlayır (ya real pəncərə-daxili punch,
ya da yoxdursa plan).

**Əlavə (display qatı):** `CixisQayidisAnomaliya` (`IcazeListDto` və
`IcazeDovriyyeDto`, hər ikisi) əvvəl yalnız "çıxış çox erkən" və "qayıdış çox
gec" yoxlayırdı — "çıxış çox gec" (bu bugun tapılan hal) və "qayıdış ≤ çıxış"
(tərs cüt) heç yoxlanmırdı, ona görə "⚠ yoxla" işarəsi Nigarın 21.09 sətrində
çıxmırdı. İndi dördü də yoxlanır — köhnə (bu düzəlişdən ƏVVƏL yazılmış)
korrupt qeydlər HR-a görünəcək, `CixisGirisDuzelt` ilə əl ilə düzəldilə bilər.

⚠️ **Yalnız gecə xidmətinin "sağlamlıq yoxlaması" (şərtsiz `Tamamlandı`
yazması) TOXUNULMADI** — istifadəçi qərarına görə cihaza baxmayan işçi HR-a əl
ilə həvalə edilmir, plan üzrə bağlanması dizaynın qəbul edilmiş hissəsidir.
Dəyişən yalnız CANLI prosesin bir punch-u "icazəyə aiddir" sayma qaydasıdır.

## AML → «Məlumat Bazası» — BMI-nin İKİ NÜSXƏSİ VAR (22.09.2026, KRİTİK)

BMI-də bu hesabatın **iki** implementasiyası mövcuddur və onlar **eyni deyil**:

| Nüsxə | Yer | `odb.aml_yoxlama` |
|---|---|---|
| C# | `BMI/AML/Sorgular/MelumatBazasi.cs` | **YOXDUR** — sorğular bütün dövrü qaytarır |
| **FoxPro (əsl)** | `melumat_bazasi_kodlari.prg` | **11 sorğunun HAMISINDA var** |

FinNex-ə əvvəlcə **C# nüsxəsi** köçürülmüşdü, yəni hesabat siyahı üzrə
**süzmürdü**. Əsl məntiq FoxPro-dadır: hesabat heç vaxt «bütün dövr» olmayıb,
həmişə axtarılan şəxslər siyahısı üzrə süzülüb.

**Oracle-a yazmaq qadağan olduğu üçün** `aml_yoxlama` cədvəli `{SIYAHI}` tokeni
ilə əvəz olunub — Exceldən oxunan şəxslər sətiriçi blok kimi yapışdırılır:

```sql
from odb.arh_dd t,
     ( select 'HUSEYNOV SAMIR MIRHUSEYN' a_s_a, '1EZVKMS' fin, '~' voen, '~' tel from dual
       union all select 'QARADAG TIKINTI MMC', '~', '1234567890', '~' from dual ) y
```

Sütun adları BMI ilə **eynidir** (`a_s_a / fin / voen / tel`), ona görə 11 sorğunun
`where` hissəsi olduğu kimi köçüb. Quraşdırma: `docs/sql/aml/92_MelumatBazasi_OracleSorgular.sql`.

⚠️ **`{SIYAHI}` tokeni olmayan sorğu = köhnə variant.** `Replace` səssizcə heç nə
etməz və sorğu bütün dövrü qaytarar; istifadəçi isə onu «axtarışın nəticəsi» sanar.
`MelumatBazasiService` bunu **açıq yoxlayır** və vərəqə xəta yazır.

⚠️ **Boş siyahı ilə icra etmə** — `( ) y` sintaksis xətasıdır. Servis əvvəlcədən dayanır.

⚠️ **Boş ad göndərmə** — `like '%%'` **BÜTÜN sətirləri** qaytarar. Boş xanalara
`'~'`, söndürülən ad şərtinə `'~~AD_YOXDUR~~'` sentineli yazılır (`null` YOX:
Oracle-da `''` elə `null`-dır və `union all` qollarında tip qarışıqlığı yaradır).

### `func_utf8_to_latin` XƏRİTƏSİ — `Ə → A`-dır, `E` DEYİL

BMI-də ölçülüb (22.09.2026):

```
odb.func_utf8_to_latin('ƏLİYEVA ÜLVİYYƏ ŞÖVQİ İSMAYIL ÇƏMƏNZƏMİNLİ')
     →  ALIYEVA ULVIYYA SOVQI ISMAYIL CAMANZAMINLI
```

`Ə→A`  `İ→I`  `Ü→U`  `Ö→O`  `Ş→S`  `Ç→C`  `Ğ→G`

Sorğularda müqayisə həmin funksiyanın **çıxışı** ilə gedir, ona görə Exceldən
gələn ad da eyni xəritədən keçməlidir — **`FinNex.Application/Helpers/Aml/BmiLatin.cs`**.

🔴 **`DashboardController.Sadeles` BU İŞ ÜÇÜN YARAMIR** — o `ə → e` edir (Excel
BAŞLIQLARINI tanımaq üçün yazılıb, orada düzgündür). İşlətsən «MƏLAHƏT» → `MELAHET`
olar, bazada isə `MALAHAT`-dır: **heç vaxt tapılmaz və heç bir xəta verməz**.

`BmiLatin.Tehlukesiz` həm də ağ siyahı tətbiq edir (`A–Z 0–9 . - / & '`, apostrof
ikiləşdirilir) — mətn istifadəçinin Excel faylından gəlir və birbaşa SQL-ə
yapışdırılır (`IOracleService` bind parametri qəbul etmir).

### Mənbələrdə HANSI AÇAR VAR — «sütun yoxdur» ≠ «FİN tapılmır»

🔴 **BU, MƏNİM SƏHVİM OLDU.** Əvvəlcə 4 vərəq üçün «FİN sütunu yoxdur, o
vərəqlər həmişə boş gələcək» yazılmışdı. İstifadəçi düzəltdi: *«həmin hesabın
regnomda fini varda, bu müştəridir axı»*. Doğrudur — **hesab nömrəsi olan hər
sətirdə şəxs `regnom` üzərindən tapılır**:

```
licsch.registrac_nomer          → regnom.regnom   (hesab cədvəli)
substr(hesab_nömrəsi, 10, 6)    → regnom.regnom   (əməliyyat sətri: debet / kredit)
                                → regnom.pincode (FİN) , regnom.inn_regnom (VÖEN)
```

**Qayda: sorğuda hesab nömrəsi varsa, FİN/VÖEN DƏ VAR.** «Bu cədvəldə FİN
sütunu yoxdur» deyib dayanma — bir join uzaqdadır.

| Vərəq | FİN | VÖEN | Açar |
|---|---|---|---|
| Aktiv_hesablar | ✓ | ✓ | `regnom` (hesab) |
| Open_Accounts | ✓ | ✓ | `regnom` (hesab) |
| Kochurme_Daxili | ✓ | ✓ | `pincode_or_passport` + `regnom` (debet/kredit) |
| Kochurme_Mushteri | ✓ | ✓ | `pincode_or_passport` + `regnom` (debet/kredit) |
| 3-cu shexs | ✓ | ✓ | `regnom` (debet/kredit) — **hesab sahibi**, 3-cü şəxs YOX |
| Transfer | ✓ | ✓ | `regnom` (4 mənbənin hər birində ayrı düstur) |
| Owner | ✓ | ✓ | `pincode` + `inn_licsch` |
| A_M_L | ✓ | ✓ | `fin` (3-cü qolda həmin sütun **VÖEN saxlayır** — `kod_novu` ilə ayrılıb) |
| **Exchange** | ✓ | **—** | `pincode_or_passport`; hər iki tərəf **kassadır** (`1005`/`1006`), hesab sahibi yoxdur |
| **Emit_benef** | ✓ | **—** | `e_pincode` / `b_pincode`; cədvəldə VÖEN sütunu yoxdur |
| **Kred_zamin** | ✓ | **—** | `g.pincode`; cədvəldə VÖEN sütunu yoxdur |

⚠️ **`3-cu shexs`-də diqqət:** `regnom` join HESAB SAHİBİNİN FİN-ini verir,
əməliyyatı aparan 3-cü şəxsin yox. Yəni «bu adamın hesabında 3-cü şəxs
əməliyyat aparıb» deməkdir — `uygunluq` sütunu bunu açıq yazır
(`FİN (hesab sahibi)`).

Boş çıxan sütunlar (`b_pincode`, `creditinfoguarantee.pincode`/`telefon`,
`docfio.ssn`/`pasport`) — şərtlər **silinməyib**, doldurulsa işə düşəcək.
`creditinfoguarantee.guarantee_id` FİN deyil, **pasportdur** (`AZE00277678`).
`docfio.passport_id` = `AZE`, yəni **ölkə kodudur** — adı yanıldıcıdır.

### AD UZUNLUĞU — `like` İSTİQAMƏTİ SƏSSİZCƏ SINIR

Bazada ad bəzən **2 hissəlidir** (`soyadi||' '||adi`), Exceldə isə **3**
(«SOYAD AD ATAADI»). `«HUSEYNOV SAMIR» like '%HUSEYNOV SAMIR MIRHUSEYN%'` →
**heç vaxt tutmur**, çünki axtarılan mətn hədəfdən uzundur.

Düzəliş — **tərs qol** əlavə olunur (`Emit_benef`, `3-cu shexs`, `A_M_L`, `Kred_zamin`):

```sql
or ( length(trim(<ad_sütunu>)) >= 8
     and upper(y.a_s_a) like '%' || upper(trim(<ad_sütunu>)) || '%' )
```

⚠️ **`length >= 8` qoruyucusu MƏCBURİDİR** — qısa/zibil ad (`A`, `-`) tərs qolda
**HƏR adama** uyğun gələr və vərəq minlərlə yalançı sətirlə dolar.

### Emit_benef-ə Qoruyucu ƏLAVƏ EDİLMƏMİŞDİ — Oracle `NULL || ' '` Boşluğa Çevrilir (25.09.2026, KRİTİK — DÜZƏLDİLDİ)

Yuxarıdakı sətir (22.09.2026) "tərs qol `Emit_benef`-ə əlavə olunub" deyirdi —
bu **YANLIŞ** idi. Real sorğuda (`AML_MB_EMIT_BENEF`) `length >= 8` qoruyucusu
heç vaxt yazılmamışdı, yalnız `3-cu shexs`/`A_M_L`/`Kred_zamin`-ə əlavə edilmişdi.

Real hadisə: 1 nəfərlik ad-siyahısı ilə axtarış `Emit_benef` vərəqində **11851
sətir** qaytardı. Səbəb `NULL` cəmləmə deyil — **Oracle-un `||` operatoru**:

```
e_match = func_utf8_to_latin(upper(trim(k.e_soyadi) || ' ' || trim(k.e_adi)))
```

Emitent/benefisiar adı boş olan əməliyyatlarda (məs. birtərəfli "Alınmış
valyuta" kassa əməliyyatı — benefisiar sahəsi ümumiyyətlə doldurulmur)
`k.b_soyadi` və `k.b_adi` hər ikisi `NULL`-dur. Oracle-da `NULL || ' ' || NULL`
**`NULL` DEYİL, `' '` (bir boşluq) qaytarır** — Oracle `||`-də `NULL`-u boş
sətir kimi rəftar edir (digər verilənlər bazalarından fərqli). Yəni
`b.b_match = ' '`.

Sonra tərs qol: `upper(y.a_s_a) like '%' || b.b_match || '%'` → `like '% %'`
— **"axtarılan adda hardasa bir boşluq varmı?"** İki sözdən çox olan HƏR ad
(demək olar ki, bütün real adlar) bunu keçir → benefisiar adı boş olan bütün
əməliyyat sətirləri "tapılmış" sayılır, "Ad (benefisiar)" etiketi ilə.

**Fərq yuxarıdakı `A_M_L` qoruyucusundan:** orada `where length(trim(t.a_s_a))
> 0` XARİCİ sorğu səviyyəsində, BÜTÜN qollara tətbiq olunur. `Emit_benef`-də
belə ümumi filtr yox idi — hər tərəf (`e_match`/`b_match`) ÖZ LIKE qoluna
bağlıdır, ona görə qoruyucu da **hər qola ayrıca** yazılmalıdır.

**Həll:** `docs/sql/aml/92_MelumatBazasi_OracleSorgular.sql`, `AML_MB_EMIT_BENEF`
— `WHERE`-in hər iki qoluna VƏ `CASE`-in "Ad (emitent)" budağına
`length(trim(b.e_match)) >= 8` / `length(trim(b.b_match)) >= 8` qoruyucusu
əlavə edildi (yuxarıdakı ümumi qaydaya uyğun `>= 8`, sadəcə `> 0` yox — qısa
ad da zibil ola bilər).

**Qayda: "tərs LIKE" istifadə edən HƏR qol öz `length >= 8` qoruyucusuna
sahib olmalıdır** — XARİCİ `WHERE`-də ÜMUMİ filtr varsa (A_M_L kimi) bu
kifayətdir, YOXDURSA (Emit_benef kimi) hər qol ayrıca qorunmalıdır. Bu
sorğu SQL Server-də yenidən işlədilməlidir ki, `OracleSorgular` cədvəlinə
düşsün; tətbiqin özündə (Risk → Məlumat Bazası) hələ TEST OLUNMAYIB.

### BMI-də tapılan və düzəldilən səhvlər

| # | Harada | Nə |
|---|---|---|
| 1 | Owner | `(A and B or C)` — mötərizə yoxdur. `balschkli` cədvəlinin **yeganə** bağlantısı `A`-dır; FİN uyğun gələndə o qol keçilir və cədvəl **dekart hasili** verir. `distinct` çıxışı gizlədir, amma Oracle milyonlarla cütü qurub atır → **paketin ən yavaş yeri**. Düzəlişdən sonra **sətir sayı azalır** (4396-dan) |
| 2 | Open_Accounts | `icra` sütunu `qey_nezaret`-dəndir; C# nüsxəsi onu atıb `log_accounts`-a hər sətir üçün **iç-içə MAX** qoymuşdu |
| 3 | Open_Accounts | `qey_nezaret` alt sorğusu `qn` üzrə təkrarlanırdı → LEFT JOIN hesab sətrini **ikiləşdirirdi** |
| 4 | Exchange / Kochurme | `substr()` mətn qaytarır, amma hesab kodları **rəqəm** yazılmışdı — Exchange-də **eyni sətirdə** biri dırnaqlı, biri dırnaqsız. Hamısı dırnağa alındı (ORA-01722 qoruması) |
| 5 | A_M_L | `to_date(doguldugu_tarix)` **maskasız** idi. Sütun VARCHAR2-dur, format `DD-MM-YYYY`; `regexp_like` qoruyucusu da əlavə edildi ki, bir pozuq sətir bütün vərəqi sındırmasın |

### Excel şablonu — HƏR VƏRƏQİN DATA SƏTRİ FƏRQLİDİR

Şablon: `FinNex.UI/App_Data/Templates/Melumat_bazasi.xlsx` (BMI-nin öz faylı,
data sətirləri təmizlənib: 4,3 MB → 21 KB). Sıfırdan qurmuruq — `Esas_Sehife`
vərəqi hazırdır (`COUNT(...)` + `=HYPERLINK("#'"&C5&"'!A1","Bax")`).

| Vərəq | Data sətri |
|---|---|
| **Emit_benef** | **4** |
| **Kred_zamin** | **5** |
| qalan 9-u | **6** |

Bu sətirlər `Esas_Sehife`-dəki `COUNT` aralıqları ilə **bağlıdır** — səhv sətirdən
yazsan başlıq üstələnər **və** «Nəticə sayı» yanlış çıxar, heç bir xəta olmaz.
`Esas_Sehife` **A sütununu** sayır, ona görə № xanası mütləq yazılmalıdır.

⚠️ **`wb.SetForceFormulaRecalculation(true)` MƏCBURİDİR** — NPOI formulu
hesablamır, yalnız mətnini saxlayır. Bunsuz «Nəticə sayı» və vərəqlərdəki
`=Aktiv_hesablar!B4` istinadları **keşlənmiş (boş) dəyərlə** açılar.

⚠️ Stilləri (`CreateCellStyle`) **bir dəfə** yarat — hər xana üçün çağırsan
NPOI-nin 64 000 stil həddinə dəyər və fayl açılmaz olar.

### İlk Real İcrada Tapılan 2 SQL Bug (23.09.2026, KRİTİK — DÜZƏLDİLDİ)

11 sorğu yazılandan sonra İLK DƏFƏ real Oracle-a qarşı işlədilib və çıxan
Excel-in `Esas_Sehife`-si `A6`-da xəta mətni göstərdi (servis xətanı vərəqin
özünə yazır, paketi dayandırmır — CLAUDE.md-də sənədləşmiş davranış).

**`KOCURME_DAXILI`, `KOCURME_MUSTERI` — `ORA-00904: "T"."NAME_LICSCH":
invalid identifier`.** `t` = `odb.arh_dd` (əməliyyat sətri) — bu cədvəldə
`name_licsch` YOXDUR, o, `odb.licsch`-dədir. Sorğu FİN/VÖEN üçün `regnom`-a
(`rd`/`rk`) join edirdi, amma **ad üzrə axtarış üçün `licsch`-ə join etməyi
unutmuşdu** — `WHERE`-də isə `t.name_licsch` yazılmışdı, sanki `arh_dd`-də
varmış kimi. Düzəliş: `90_AML_OracleSorgular.sql`-də ARTIQ SINANMIŞ `p`/`s`
naxışı köçürüldü — `odb.licsch ld, odb.licsch lk` əlavə edildi,
`t.debet = ld.licsch(+)`, `t.kredit = lk.licsch(+)`. Ad uyğunluğu indi
**hər iki hesabın (debet VƏ kredit) sahibinin adına** baxır — FİN/VÖEN-də
`rd`/`rk` üçün olduğu kimi, bir tərəf uyğun gəlsə kifayətdir.

**`AKTIV_HESABLAR` — `ORA-00979: not a GROUP BY expression`.** `SELECT`-dəki
`uygunluq` (`CASE`) `y.fin`/`y.voen`/`y.tel`-ə istinad edirdi, `GROUP BY`-da
isə yalnız `y.a_s_a` var idi. `{SIYAHI}`-nin hər sətri (a_s_a, fin, voen,
tel) sabit dördlük olduğu üçün bu sütunları `GROUP BY`-a əlavə etmək
qruplaşma dənəviliyini DƏYİŞMİR, sadəcə Oracle-un tələbini ödəyir.

**`A_M_L`, `Kred_zamin` — `ORA-01013: user requested cancel of current
operation`** eyni faylda görünürdü. ⚠️ **BU SƏTRİN ƏVVƏLKİ VARİANTI SƏHV İDİ**
— «böyük siyahı ilə 60 saniyəlik `CommandTimeout`-a çatma» deyilmişdi. İstifadəçi
bunu 1000 nəfərlik (TƏK batch) siyahı ilə yenidən yoxladı və eyni xəta yenə
çıxdı — əsl səbəb aşağıdakı bölmədədir ("`func_utf8_to_latin` — PL/SQL Çağırış
Partlayışı"). Siyahı ölçüsü ilə heç bir əlaqəsi yox idi.

⚠️ **Düzəliş `docs/sql/aml/92_MelumatBazasi_OracleSorgular.sql`-dədir —
`OracleSorgular` cədvəlinə düşməsi üçün faylı yenidən SQL Server-də
işlətmək lazımdır.** Fayl "TƏKRAR İŞLƏDİLƏ BİLƏR" olaraq yazılıb (mövcud
`AML_MB_*` sətirləri yenilənir, Id qorunur) — sadəcə yenidən icra kifayətdir.

### `func_utf8_to_latin` — PL/SQL Çağırış Partlayışı (23.09.2026, KRİTİK)

Yuxarıdakı `ORA-01013` fərziyyəsi ("böyük siyahı = timeout") **YANLIŞ** çıxdı.
İstifadəçi 1000 nəfərlik TƏK batch siyahı ilə eyni xətanı aldı. Diaqnoz addım-
addım: (1) 3 mənbə cədvəli `COUNT(*)` ilə ölçüldü — 5965 / ~1132 / 177 sətir,
kiçikdir; (2) hər 3 sorğu 2 test adı ilə PL/SQL Developer-də birbaşa işlədildi —
hamısı <1 saniyə, deməli nə data həcmi, nə pis icra planı (Owner-dəki köhnə
dekart hasili bugu kimi) səbəb deyildi.

**Əsl səbəb:** `odb.func_utf8_to_latin` Oracle-un daxili funksiyası DEYİL,
**custom PL/SQL funksiyasıdır**. `WHERE` daxilində, `{SIYAHI}` (`y`) ilə CARTESIAN
olaraq YOXLANANDA, hər (baza sətri × y sətri) CÜTÜ üçün AYRICA çağırılır — SQL↔
PL/SQL keçidinin öz overhead-i var və cüzi bir funksiyanı milyonlarla çağırışa
çevirir: Aktiv_hesablar 5965×1000 ≈ **6 milyon** çağırış, Kred_zamin 177×1000×2
(iki ayrı LIKE qolu) ≈ **354 min**. Bazalar kiçik olsa da, funksiya HƏR CÜT üçün
təkrar-təkrar çağırılanda saniyələr dəqiqələrə çevrilir.

⚠️ **BU FƏRZİYYƏNİN "A_M_L/Transfer HEÇ VAXT düşməyib" hissəsi DƏ YANLIŞ
ÇIXDI** — bax aşağıdakı bölmə. Hər ikisinin `union all` alt sorğusu da eyni
`{SIYAHI}`-ilə cross-join strukturunu daşıyır, sadəcə A_M_L-in baza cədvəlləri
kiçik olduğu üçün adətən 60 saniyəyə çatmırdı.

**Qayda:** Oracle-da custom PL/SQL funksiyası (`odb.func_*`) `{SIYAHI}` və ya
hər hansı böyük çarpaz-birləşmə (`cross join` / `union all` sonrası çoxlu sətirlə
kəsişən `WHERE`) daxilindəki bir sütuna tətbiq olunursa, funksiyanı **əvvəlcə
bir alt sorğuda, baza sətri başına BİR DƏFƏ hesabla**, sonra həmin hazır sütunu
xarici `WHERE`-də adi `like`/`=` ilə müqayisə et. Yoxlama: sorğu mətnində
`{SIYAHI}`-nin son keçdiyi yerdən (cross join nöqtəsi) sonra `func_utf8_to_latin`
(və ya bənzər custom funksiya) görünməməlidir.

⚠️ **Eyni ifadənin iki fərqli forması** (`upper(ad)` vs `upper(trim(ad))`) bir-
birinə bərabər SAYILMAMALIDIR — boşluq fərqi bilinmirsə hər ikisi AYRI
precompute-lənmiş sütun kimi saxlanılmalıdır (`Kred_zamin`, `3-cu shexs`-də
belə edildi: `name_match_a`/`name_match_b`, `fio_match_a`/`fio_match_b`).

⚠️ **Düzəlişdən sonra hər sorğunu sütun adı üzrə yenidən tutuşdur** — outer
`SELECT`/`WHERE`/`ORDER BY`-dakı hər istinad daxili alt sorğunun verdiyi aliasla
DƏQİQ eyni olmalıdır. Səhv yazılmış alias `ORA-00904` verər — bu, artıq
performans səhvi deyil, sadə səhv yazılışdır, amma eyni cəldliklə yoxlanmalıdır.

### Yuxarıdakı Düzəliş TƏK BAŞINA KİFAYƏT ETMƏDİ — Oracle "View Merging" (23.09.2026, İKİNCİ DALĞA, KRİTİK)

Yuxarıdakı "baza sətri başına bir dəfə hesabla" düzəlişi tətbiq ediləndən sonra
istifadəçi EYNİ ~1000 nəfərlik siyahı ilə YENƏ eyni 3 vərəqdə (Aktiv_hesablar,
Kred_zamin, A_M_L) `ORA-01013` aldı. Əvvəlcə SQL Server-də `OracleSorgular`
cədvəlindəki `SorguMetni` yoxlanıldı — düzəliş HƏQİQƏTƏN yenilənmişdi, problem
tətbiqin köhnə mətn işlətməsi deyildi.

Sonra Aktiv_hesablar-ın YENİ (düzəlişli) mətni, sintetik 1000 sətirlik
`{SIYAHI}` ilə (heç kimə uyğun gəlməyən test adları — `connect by level`),
PL/SQL Developer-də birbaşa işlədildi: **83 saniyə** (tətbiqin 60 saniyəlik
`CommandTimeout`-unu keçir). Yəni "subquery bir dəfə hesablanır" fərziyyəsi
DÜZ idi, amma Oracle-un OPTİMİZATOR DAVRANIŞI bunu səssizcə pozurdu.

**Əsl səbəb:** Oracle-un cost-based optimizer-i sadə inline view-ları default
olaraq **birləşdirməyə (view merging)** meyllidir. Alt sorğu (`r`/`b`/`q`/`t`)
ilə `{SIYAHI}` (`y`) arasında heç bir indeksli join açarı yoxdur — yalnız
`OR`-lu `LIKE`/`=` şərtləri. Belə halda Oracle NESTED LOOPS seçə bilər: `y`-nin
HƏR sətri üçün alt sorğunu YENİDƏN başdan icra edir (o cümlədən içindəki
`func_utf8_to_latin` çağırışlarını) — yəni "baza sətri başına bir dəfə" hesabı
əslində "baza sətri × `{SIYAHI}` sətri başına bir dəfə" olur, eyni partlayış
gizli şəkildə geri qayıdır. Bu, A_M_L-in niyə HEÇ VAXT tam etibarlı olmadığını
da izah edir.

**HƏLL:** `from ( select … ) alias` forması `with alias as ( select /*+
MATERIALIZE */ … )` ilə əvəz olundu (bütün 11 sorğuda). `MATERIALIZE` hinti
Oracle-a bu alt sorğunu **müstəqil addım kimi, bir dəfə** hesablayıb müvəqqəti
seqmentə yazmağı əmr edir — view merging yolu bağlanır. `union all` olan
sorğularda (Transfer, A_M_L) hint yalnız BİRİNCİ qolun `select`-indən sonra
yazılır.

**Qayda:** Bir alt sorğunu "baza sətri başına bir dəfə hesablanmalıdır" deyə
yazmaq KİFAYƏT ETMİR — Oracle-a bunu MƏCBUR ET (`MATERIALIZE` və ya `NO_MERGE`
hinti). Xüsusilə alt sorğu ilə xarici cədvəl arasında indeksli join açarı
YOXDURSA (yalnız `LIKE`/`OR` şərtləri varsa), optimizatorun NESTED LOOPS seçib
subquery-ni təkrar-təkrar icra etmə riski YÜKSƏKDİR. Bu hinti YAZMADAN "compute
once" pattern-inə güvənmə — icra planına ({{EXPLAIN PLAN}} və ya real vaxt
ölçümü ilə) yoxlanmadan "düzəldi" demə.

⚠️ **`MATERIALIZE` DƏ TƏK BAŞINA KİFAYƏT ETMƏDİ** — bax aşağıdakı bölmə.
İstifadəçi skripti yenidən işlədib eyni ~1000 nəfərlik siyahı ilə A_M_L-i
tətbiqdən KƏNAR (paralel sorğu olmadan) test etdi: **40 saniyə** — timeout-a
düşmür, amma 1132 sətir × 1000 nəfər üçün hələ də mənasız yavaş.

### Əsl Kök Səbəb — `LIKE` + `=` Eyni `OR`-da = İndeks İtir (23.09.2026, ÜÇÜNCÜ DALĞA, KRİTİK)

`LIKE` (ad üzrə) ilə `=` (FİN/VÖEN dəqiq bərabərlik) şərtlərini **EYNİ `OR`-da**
yazmaq Oracle-u bütün predikatı indeksləşdirilə bilməyən sayır — bərabərlik
hissəsi belə HASH/INDEX JOIN ala bilmir, hər şey NESTED LOOPS/cartesian ilə
gedir. Sübut: A_M_L-in FİN/VÖEN şərti ayrı `union all` qoluna (TƏMİZ
`t.fin = y.fin` join-i, `OR` yoxdur) çıxarılıb Ad-LIKE-ı ayrıca qol
saxlananda: **40 saniyə → 1,575 saniyə** (≈25×). Eyni texnika Aktiv_hesablar-a
tətbiq olunanda: **83 saniyə → 4,7 saniyə** (≈17×).

⚠️ **Bu, hər sorğuya eyni dərəcədə aid deyil.** Owner (1,3 san) və Kred_zamin
(0,3 san) EYNİ qarışıq-`OR` strukturuna baxmayaraq artıq sürətlidir — yalnız
`MATERIALIZE` kifayət edib, TOXUNULMAYIB. Fərqin dəqiq səbəbi bilinmir (ehtimal:
A_M_L/Aktiv_hesablar-ın FİN/VÖEN şərti sadə `= y.fin` yox, əlavə şərtlə
(`kod_novu=''FIN''`) bağlıdır). **Qayda: "hamısı yavaşdır" fərz edib universal
dekompozisiya tətbiq etmə — hər sorğunu FƏRDİ ölçüb, yalnız real yavaş çıxanı
dəyiş.**

**HƏLL (yalnız A_M_L və Aktiv_hesablar-da tətbiq olundu):** hər uyğunluq
kriteriyası (FİN, VÖEN, Telefon, Ad) ayrı `union all` qoluna çıxarıldı. FİN/VÖEN
qolları TƏMİZ `join ... on sütun = sütun` (heç bir `OR` yoxdur), Ad/Telefon
qolu isə LIKE-lı cartesian olaraq qalır (sərbəst mətn axtarışından qaçış yoxdur),
amma ARTIQ YALNIZ o hissə bahalıdır.

⚠️ **PRİORİTET QORUNMALIDIR.** Orijinalda bir (müştəri, axtarılan şəxs) cütü
həm ada, həm FİN-ə uyğun gəlsə, YALNIZ BİR sətir çıxırdı ("FİN" etiketi ilə —
CASE sırası FİN > VÖEN > Telefon > Ad). Sadəcə `union all` ilə ayırsan EYNİ
cüt İKİ (fərqli etiketli) sətir kimi çıxardı. Həll: aşağı prioritetli qollara
`and not (yuxarı prioritetli şərtlər)` istisnası əlavə edildi — bu, WHERE-i
bir az çoxaldır, amma cartesian-ın ÖZÜNÜ yox, artıq mövcud sətir-səviyyəli
yoxlamanı genişləndirir, mürəkkəblik sinfini dəyişmir.

⚠️ **`{SIYAHI}` təkrarlanmadı.** Hər uyğunluq qolu üçün ayrıca `( {SIYAHI} ) y`
yazmaq əvəzinə, `{SIYAHI}` bir dəfə `y as ( {SIYAHI} )` WITH bloku kimi yazıldı,
bütün qollar `y`-ni çağırır — böyük siyahı literalının sorğu mətnində 3-4 dəfə
təkrarlanıb şişməsinin qarşısı alındı.

⚠️ **Yoxlanmamış qalan hissə:** bu ÜÇÜNCÜ DALĞA hələ real Oracle-a qarşı
PRODUCTION mətni ilə yenidən test edilməyib — yalnız oxşar (sadələşdirilmiş,
prioritetsiz) test versiyaları PL/SQL Developer-də sınanıb. Fayl yenidən SQL
Server-də işlədildikdən sonra Aktiv_hesablar və A_M_L tətbiqin ÖZÜNDƏ (Risk →
Məlumat Bazası) tam axınla test olunmalıdır — həm sürətə, həm nəticələrin
düzgünlüyünə (xüsusən prioritet etiketlərinə) baxaraq.

⚠️ **Qalan 7 dövrlü sorğu (Open_Accounts, Kochurme_Daxili, Kochurme_Mushteri,
Exchange, Emit_benef, 3-cu shexs, Transfer) HƏLƏ TEST OLUNMAYIB** — onlarda da
eyni qarışıq-`OR` strukturu var, amma vaxtları tarix aralığındakı əməliyyat
sayından asılıdır, ona görə real tarix aralığı ilə test edilməlidir.

### Transfer — FİN/VÖEN `regnom` Zənciri ETİBARSIZDIR, MƏTNDƏ Yazıla Bilər (25.09.2026, KRİTİK)

`AML_MB_TRANSFER` (`doc_vnesh_inval`/`nacval`/`swift`/`postupl`, 4 mənbə) FİN/VÖEN-i
`account_no → regnom → pincode/inn_regnom` zənciri ilə tapırdı. Real hadisə: 1 nəfərlik
FİN siyahısı ilə axtarış **heç nə** tapmadı, halbuki **eyni** şəxs "Ad" rejimində 7 sətir
tapdı. Diaqnoz addım-addım:

1. `doc_vnesh_swift.sender_account` — mənbədə 8 sətirdən 5-i **boş**, qalanları 10-16
   rəqəm arası, sabit format YOXDUR. Bu, BMI-nin daxili hesab nömrəsi deyil — SWIFT-in
   **xarici** tərəfidir (gələn köçürmədə göndərən xaricdədir), `regnom`-da ola bilməz.
2. İstifadəçi `doc_vnesh_inval.account_no`-nun konkret dəyərini (`4502304002000400000`)
   yoxladı: **bu, müştərinin öz hesabı deyil** — "adi köçürmə hesabı" (bankın ümumi/
   keçid hesabı, çoxlu fərqli müştərinin əməliyyatı elə bu bir hesab üzərindən keçir).
   Yəni `substr(account_no,10,6)=regnom` zənciri bu halda kimin FİN-inə baxırsa baxsın
   — **fərdi müştəriyə YOX**, ümumi hesabın öz qeydinə bağlanır.
3. **Əsl həll tapıldı:** `ACCOUNT_NAME` sütununun özünə baxanda FİN mətnin İÇİNDƏ
   yazılıb: `"RANQAMİZ MƏLEYKƏ ELDAR QIZI AB0325826 65GPNXJ"` — ad + sənəd № + FİN,
   boşluqla ayrılmış, tək VARCHAR sahədə. Fərdi hesabı olmayan (ümumi hesabdan keçən)
   əməliyyatlarda identifikasiya MƏHZ belə aparılıb — struktur sahə deyil, sərbəst mətn.

**Düzəliş:** WHERE-ə VƏ CASE-ə FİN/VÖEN-i **ad sahəsinin özündə** axtaran şərt əlavə
olundu (`upper(emit_name/ben_name) like '%'||upper(y.fin)||'%'`), `length >= 7`
(FİN) / `>= 9` (VÖEN) qoruyucusu ilə (qısa/sentinel dəyərlərin — "Ad" rejimindəki
`Bos="~"` kimi — səhvən uyğun gəlməsinin qarşısını almaq üçün). Etiket: **"FİN
(mətndə)"** / **"VÖEN (mətndə)"** — `regnom`-dan gələn dəqiq "FİN"/"VÖEN"-dən fərqli,
ki, operator mənbəni ayırd edə bilsin. Köhnə `regnom`-əsaslı yoxlama **silinmədi**
(ola bilər başqa əməliyyat növlərində düzgün işləyir), yeni şərt ona **əlavədir**.

⚠️ **AÇIQ QALAN, YOXLANMAMIŞ RİSK — ƏKS İSTİQAMƏTDƏ YALANÇI MÜSBƏT.** `regnom`
zənciri silinmədiyi üçün sual qalır: əgər ümumi köçürmə hesabının (`4502304002000400000`
və bənzərləri) `regnom`-da **öz** FİN/VÖEN-i varsa (kimə aid olursa olsun), kimsə
məhz **o** FİN/VÖEN ilə axtaranda sistem `doc_vnesh_inval`-dakı **BÜTÜN** əməliyyatları
(yəni yüzlərlə əlaqəsiz müştərinin köçürməsini) "FİN uyğun gəldi" deyə göstərəcək —
bu, Emit_benef-in `NULL||' '` bugundan da təhlükəli ola bilər, çünki konkret (səhv)
bir şəxsi göstərir. Yoxlama üçün SELECT verilib istifadəçiyə (`select regnom, pincode,
inn_regnom from odb.regnom where regnom = substr('4502304002000400000',10,6)`),
**nəticə hələ gəlməyib**. Növbəti dəfə bu sorğuya toxunanda əvvəlcə bunu yoxla —
əgər real FİN çıxırsa, `doc_vnesh_inval`/`nacval`/`postupl`-ın ümumi/keçid hesab
kimi işlədilən sətirlərində `regnom`-əsaslı FİN/VÖEN yoxlaması **çıxarılmalıdır**
(yalnız "mətndə" və "ad" yoxlamaları qalmalıdır).

✅ **YOXLANDI (25.09.2026).** SQL Server-də işlədilib, `OracleSorgular`-a yazılıb
(istifadəçi `SELECT SorguMetni FROM OracleSorgular WHERE SorguAdi='AML_MB_TRANSFER'`
ilə mətni özü təsdiqlədi) və Oracle-da təcrid olunmuş test (`func_utf8_to_latin(...)
like '%65GPNXJ%'`) 7/7 sətirdə `TAPDI` verdi — **bu sorğunun SQL məntiqi düzgündür**.

Buna baxmayaraq tətbiqdə "Yalnız FİN/VÖEN" axtarışı yenə heç nə tapmadı. Səbəb bu
sorğuda DEYİLDİ — Excel oxuma mərhələsində idi: yuxarıdakı **"Ayrı «VÖEN» Sütununda
da FİN-formatlı Dəyər Ola Bilər (25.09.2026)"** bölməsinə bax. Operator FİN dəyərini
«VÖEN» başlıqlı sütuna yazmışdı, sistem onu format yoxlamadan olduğu kimi qəbul
edirdi, ona görə `y.fin` boş, `y.voen` isə 7 simvollu (uzunluq şərtini keçməyən)
dəyər idi. `ExceldenOxu` düzəldildikdən sonra bu konkret hal işə düşdü.

**Dərs:** bir axtarış zənciri bir neçə mərhələdən keçirsə (Excel oxuma → siyahı
qurma → SQL), hər mərhələni AYRI-AYRI təsdiqləmək kifayət etmir — SQL-in özünün
düzgün olması ötürülən DƏYƏRİN düzgün olduğunu sübut etmir. "SQL-də TAPDI çıxdı,
niyə tətbiqdə yox" sualında əvvəlki mərhələni (bu halda: `{SIYAHI}` blokuna nə
DƏQİQ göndərildiyini) yoxlamaq lazımdır, sorğunu təkrar-təkrar oxumaq yox.

## Yekun Zolaq (Footer) BAĞLI SİSTEMDİR — Gross − Tutulma = NET

Toplu Maaş ekranının aşağı zolağında `Gross`, `Cəmi tutulma` və `NET` **bir-birini
yoxlayan üçlükdür**. Biri dəyişəndə o birilər də dəyişməlidir, yoxsa zolaq
öz-özü ilə bağlanmır və istifadəçi «hansı düzdür?» sualı ilə qalır.

Real hadisə (27.08.2026): `Gross` qabaqcadan ödənilmiş məzuniyyətin brütünü
**çıxarırdı** (4 204,41), `Cəmi tutulma` isə onun netini (3 438,80) **çıxmırdı**:

```
60 461,15 − 17 088,73 = 43 372,42     amma NET 43 419,03   → 46,61 fərq
```

Sətirlərin hamısı düz idi (hər işçinin NET-i Excel ilə qəpiyinə eyni) — **yalnız
yekun zolaq yalan danışırdı**. İndi hər ikisi mühasib formatındadır (Excel ixracı
onsuz da belə idi): `Gross += mavBrut`, `Avans += mavNet`, `Cəmi tutulma` isə
komponentlərdən **birbaşa** yığılır — `4 vergi + HYS + Avans`.

**«Məz. avansı vergisi» xanası «Cəmi tutulma»ya ƏLAVƏ EDİLMİR** — o məbləğ artıq
4 verginin içindədir (vergilər birləşmiş baza üzrə hesablanır). Xana yalnız
məlumat üçün qalıb və etiketində «(məlumat)» yazılır.

**Qayda:** ekran ilə Excel ixracı **eyni təqdimatda** olmalıdır. İkisi fərqli
«gross» tərifi işlədirsə, sual gec-tez qayıdır.

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

### `[Migration]` ATRİBUTU OLMASA EF FAYLI GÖRMÜR (KRİTİK)

Designer olmayan migration-da **iki atribut sinfin ÖZÜNDƏ olmalıdır**:

```csharp
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260901000000_EzamiyyetAvtoparkBaglantisi")]   // fayl adı ilə EYNİ
public partial class EzamiyyetAvtoparkBaglantisi : Migration
```

Normalda `[Migration]` **`.Designer.cs`-də** olur. Bu layihədə Designer yazılmır,
ona görə atribut əl ilə qoyulmalıdır.

**ƏN TƏHLÜKƏLİ HİSSƏSİ — HEÇ BİR XƏTA YOXDUR.** Atributsuz fayl sadəcə
`Migration`-dan törəyən adi sinifdir; EF onu **migration saymır**:
`GetPendingMigrations()` **boş** qayıdır, `Migrate()` heç nə etmir, log təmiz
qalır, tətbiq normal başlayır. Səhv yalnız səhifə açılanda görünür —
**«Invalid column name '<sütun>'»** — və adam səbəbi kodda, EF konfiqurasiyasında
axtarır. `[Migration] İŞLƏMİR` xətası ilə `InsertData` xətası tam əksdir:
biri səssiz keçir, o biri migration-u bütöv sındırır.

Real hadisə (01.09.2026, Ezamiyyət↔Avtopark): build 0 xəta, `Rebuild All
succeeded`, amma `/User/Muraciet` → *Invalid column name 'MasinId'*.

**Yoxlama — yeni migration yazandan sonra HƏMİŞƏ işlət:**

```bash
cd DataAccess/Migrations
for f in $(ls *.cs | grep -v Designer | grep -v Snapshot); do
    if ! grep -q "\[Migration(" "$f" && [ ! -f "${f%.cs}.Designer.cs" ]; then
        echo "RİSK — EF bu faylı görmür: $f"
    fi
done
```

**⚠️ KÖHNƏ ATRİBUTSUZ MİGRATION-A ATRİBUT ƏLAVƏ ETMƏ.** Aşağıdakı ikisi
əvvəldən atributsuzdur və EF onları heç vaxt görməyib — dəyişikliklər, yəqin ki,
əl ilə tətbiq olunub:

| Fayl | Vəziyyət |
|---|---|
| `20260514170000_AddGelenMailler.cs` | atributsuz, Designer yox |
| `20260525140000_MezuniyyetSenedYoluMax.cs` | atributsuz, Designer yox |

Atribut əlavə etsək onlar **«pending» olur** və `Migrate()` onları
**ID sırası ilə ƏVVƏLCƏ** icra edir. Obyektlər onsuz da mövcud olduğu üçün
«already exists» ilə sınacaq və **sonrakı bütün migration-ları bloklayacaq**.
Toxunmaq lazımdırsa əvvəlcə `__EFMigrationsHistory`-yə sətir əlavə edilməlidir
(yəni «tətbiq olunub» kimi işarələnməli) — bu, ayrıca qərardır.

## İKİ AYRI TARİXÇƏ — `main` vs `…xge7j5` (22.09.2026, KRİTİK)

Repozitoriyada **ortaq atası olmayan iki git tarixçəsi** var. `git merge-base`
bunların arasında **heç nə qaytarmır** (exit=1) — yəni bir-birinə qohum deyillər:

| Budaq | Commit sayı | Kök commit | Ağacın vəziyyəti |
|---|---|---|---|
| `main` (+ `…xge7j5-0murf5`) | **55** | `ab9221fc` — 01.09.2026 11:10 | **CARİ** — bütün 01–22.09 işi buradadır |
| `claude/…-xge7j5` | 3027 | `ec607b70` «first clean commit» | **01.09-da DONUB** + üstündə 1 commit |

01.09.2026-da bir sessiya repozitoriyanı **dayaz (shallow)** klonlayıb push edib:
ağac bütöv köçüb, **tarixçə qırılıb**. O vaxtdan bəri bütün iş orfan `main`
üzərində gedir. Köhnə `…xge7j5` budağı isə tarixçəni saxlayır, amma **ağacı
01.09-dan bəri yenilənmir**.

**55 commit mesajının HEÇ BİRİ o biri budaqda yoxdur** (`grep -Fxv` ilə yoxlandı)
— yəni `main`-dəki iş (Kredit Arayışları, Pul köçürməsi 20 000 USD limiti,
HR → Müddətlər/Vəsiqə, Məzuniyyət Balansı kompakt cədvəl, Mail Data Protection,
AML «Hesab üzrə sorğu», FinNex brendi, Balans İcmalı…) `…xge7j5`-də **ümumiyyətlə
mövcud deyil**.

⚠️ **`…xge7j5`-i pull etmək 3 həftə GERİ getməkdir.** Real hadisə: istifadəçi
onu pull etdi və Risk panelində «AML hesabatları» bölməsi yoxa çıxdı,
`AML_HESAB_SORGU_*` sətirləri xam şəkildə kart kimi göründü (o budaqda
`RiskService.RiskDoldurabilmir` süzgəci yoxdur). Heç bir xəta çıxmadı — sadəcə
kod köhnə idi.

**Qaydalar:**
- **İş `main`-dədir.** Yeni sessiya `…xge7j5`-ə commit etməməlidir.
- O budaqda qalan iş varsa **`git cherry-pick -x <sha>`** ilə gətir — tarixçələr
  qohum olmadığı üçün `merge` yalnız `--allow-unrelated-histories` ilə işləyir və
  bütün ağacı toqquşdurar. Cherry-pick təmiz keçir (yamaq kimi tətbiq olunur).
- Budaqların fərqini **mesaja görə** ölç, `git log A ^B` sayına görə yox:
  qohum olmayan tarixçələrdə o say heç nə demir.
  ```bash
  git log --format=%s origin/main > /tmp/a && git log --format=%s origin/claude/…-xge7j5 > /tmp/b
  grep -Fxv -f /tmp/b /tmp/a      # yalnız main-də olan iş
  ```
- Tarixçəni birləşdirmək (`--allow-unrelated-histories` və ya `replace --graft`)
  **ayrıca qərardır** — özbaşına etmə.

### Köhnə tarixçəyə aid ~90 QALIQ BUDAQ VAR — İSTİFADƏÇİ QƏRARI: TOXUNULMASIN (02.10.2026)

Yuxarıdakı `claude/…-xge7j5` təkcə NÜMUNƏ idi — real yoxlama (`git merge-base origin/main origin/<budaq>`,
hər biri üçün) göstərdi ki, `origin`-dəki demək olar **bütün digər `claude/*` budaqları** (təxminən
90 ədəd, o cümlədən `samir/local-isleri`, `fix/qa-btn-hover`) `main` ilə **heç bir ortaq əcdada
malik deyil** — hamısının kökü `ec607b70` "first clean commit" (19.03.2026), yəni 01.09.2026-dakı
reset-dən ƏVVƏLKİ bütöv köhnə tarixçəyə aiddirlər. Son aktivlik tarixləri fevral–iyul 2026 arası
(reset-dən sonra demək olar heç biri toxunulmayıb, `…xge7j5` istisna olmaqla — o, 22.09-a qədər
bixəbər davam etdirilib). Adların çoxu avtomatik generasiya olunmuş görünür (`happy-shannon-x7fgsu`,
`friendly-turing-6aUYy`, `festive-mayer-6ef50n` və s.).

**İstifadəçi qərarı (02.10.2026): bu budaqlar SAXLANILIR, silinmir.** Onlar `main`-ə heç cür
birləşə bilmədiyi üçün (unrelated histories), heç bir funksional/build/performans ziyanı yoxdur —
yalnız GitHub-un budaq siyahısında vizual qarışıqlıqdır.

**Qayda — sessiya başlanğıcı yoxlamasına dair:** `git log --oneline --remotes ^main --no-walk=unsorted`
əmri bu səbəbdən HƏR sessiyada boş olmayan nəticə qaytaracaq (109 "behind" ədədi sadəcə `main`-in
cəmi commit sayıdır — unrelated history-də gözlənilən normal nəticədir). Bu, yeni/naməlum problem
DEYİL — yuxarıdakı məlum vəziyyətdir. Yoxlamanı işlət, amma nəticədə YALNIZ bu siyahıdakı köhnə
budaqlar çıxırsa (yeni, tanımadığın bir budaq adı YOXDURSA), istifadəçiyə sual vermədən davam et.
Tanımadığın/yeni bir budaq adı görsən (bu siyahıda olmayan), o ayrıca araşdırmağa dəyər — ONDA sual ver.

### `OracleSorgular` DATA-dır, KOD DEYİL

Risk panelinin KPI kartları və hesabat kartları `OracleSorgular` cədvəlindən
(SQL Server) gəlir. Prod ilə lokal **fərqli SQL Server bazalarıdır**, ona görə:

- eyni səhifə iki mühitdə fərqli kart **sayı** və fərqli qrafik göstərə bilər;
- təkrarlanan kart (məs. «Qeyri-rezidentlər» iki dəfə) **cədvəldə iki sətir**
  deməkdir — kodda dublikat süzgəci yoxdur və qəsdən yoxdur.

«Pull-dan sonra rəqəm dəyişdi» şikayətində əvvəlcə **hansı bazaya baxdığını**
müəyyənləşdir; kodda səbəb axtarmaq vaxt itkisi ola bilər.

## Məzuniyyət Hüququ Qanuni Ədədləri — Artıq HARDCODE DEYİL (30.09.2026)

İstifadəçi sualı: *"qanunda yaş və ya əlavə gün dəyişdirilsə biz kodda dəyişməli
olacağıq, düz deyilmi? Qanun dəyişə bilməzki?"* — cavab bəli idi. Əvvəllər
`MezuniyyetHuquqService`-də yaş həddi (14/18), uşaq əlavə günü (2/5) və staj
pilləsi (5/10/15 il → 2/4/6 gün) ədədləri **kodda sabit** yazılmışdı.

**Həll:** `MezuniyyetHuquqParametrleri` TƏK sətirli cədvəl + `HR → Məzuniyyət
Balansı → Hüquq parametrləri` səhifəsi (yalnız HR/Admin — Rehber daxil deyil).
Sətir yoxdursa (`MezuniyyetHuquqParametrleri.Defolt`) köhnə hardcode ilə EYNİ
dəyərlər işə düşür — hesablama heç vaxt boş qalmır. `SaxlaAsync` həddlərin
ARTAN sırada olduğunu (staj: 1<2<3-cü hədd, uşaq: 2-günlük hədd < 5-günlük
hədd) yoxlayır, pozulsa **heç nə yazılmır**.

⚠️ **Vəzifəyə görə 21/30 seçimi bura DAXİL DEYİL** — o, əvvəldən
`Vezife.EsasMezuniyyetGunu` sütunundadır, ayrıca DB-dən idarə olunurdu.

### Code Review Tapdığı Real Bug — `double` Staj Həddi (KRİTİK)

İstifadəçi açıq tələb etdi: *"öz yazdığın koda da yoxlatdır"*. `code-review`
skili işə salınanda staj həddi sahələrinin (`StajHedd1/2/3Il`) `double` yazıldığını
tapdı — bu, **"Razor → CSS/JS Rəqəm — Mədəniyyət" bölməsindəki eyni tələdir**,
sadəcə əks istiqamətdə: server az-Latn-AZ mədəniyyətindədir, `<input
type="number">` isə yalnız NÖQTƏ qəbul edir. `FlexibleDecimalModelBinder`
(`FinNex.UI/Configurations/`) bu boşluğu **yalnız `decimal`/`decimal?` üçün**
bağlayır — `double` sahədə HR "5.5" (il) yazsa, mədəniyyət "." işarəsini minlik
ayırıcı sayıb **55** kimi oxuya bilərdi, heç bir xəta çıxmadan, və bu rəqəm
bütün işçilərin illik hüququna səssizcə təsir edərdi.

**Düzəliş:** üç sahə də (`StajHedd1Il/2Il/3Il`) `decimal`-a çevrildi (entity +
migration sütunu `decimal(5,2)` + DTO + servis müqayisəsi). View-da dəyər
**InvariantCulture** ilə yazılır (`Il()` funksiyası), `step="any"` verilir —
`asp-for` İŞLƏDİLMİR (default formatlayıcı vergüllə yazardı, brauzer onu
səssizcə boş göstərərdi). `MezuniyyetHuquqService`-də `stajIl` (double) ilə
müqayisədə `(double)p.StajHeddXIl` açıq cast edilir.

**Qayda:** hesablamada işlənən, formda əl ilə daxil edilə bilən **kəsrli**
(tam ədəd olmayan) hər sahə `decimal` olmalıdır, `double` YOX — az-AZ
mədəniyyətli layihədə bu, avtomatik qorunmayan tələdir.

**Review-un yalançı müsbət tapıntıları (yoxlanıb, düzəldilmədi):** eyni review
"TempData Success/Error görünməyəcək" iddiası ilə iki sahəyə toxunmağı təklif
etdi. Yoxlanıldı: `Areas/HR/Views/_ViewStart.cshtml` bütün HR səhifələrini
`_UserLayout.cshtml`-ə bağlayır, o isə TempData-nı `@RenderBody()`-dən ƏVVƏL
QLOBAL göstərir (`Vezife/Index.cshtml` də bunu sübut edir — öz bloku yoxdur,
işləyir). **Qayda: avtomatik review tapıntısını da kor-koranə tətbiq etmə —
layihənin öz konvensiyasına (bu halda `_ViewStart.cshtml` zənciri) baxıb
təsdiqlə, sonra qərar ver.**

## Əməliyyat Riski Hadisələri Jurnalı — MB Qərar 04/1, Əlavə 4 (30.09.2026)

İstifadəçi tələbi: *"biz bu bazanı qurmalıyıq. Risk departamentində buna uyğun
səhifələr olmalı. işçi müşahidə etdiyi an bazaya əlavə etməli."* — MB-nin
"Banklarda əməliyyat risklərinin idarə edilməsi Qaydası"nın (28.01.2025,
Qərar 04/1) Əlavə 4-ündəki daxili insident jurnalı quruldu.

**Əsas qərarlar (bu sessiyada, istifadəçi ilə birbaşa müzakirə olunub):**
- **Təsdiq zənciri YOXDUR** — qaydanın öz mətni (9.6-cı bənd) işçi→koordinator→
  şöbə rəisi→Risk departamenti zənciri təsvir edir, amma istifadəçi: *"risk
  və aml şöbədəki işçilər özləri təsdiq edə biləcək səviyyədədir"* — Risk/AML
  işçisi yaratdığı an qeyd rəsmidir.
- **Redaktə sərbəstdir, amma tam tarixçə saxlanılır** — hər redaktədən ƏVVƏL
  köhnə qeydin tam JSON snapshotu `EmeliyyatRiskiHadisesiTarixce`-yə yazılır
  (`Isci.EvvelkiStajPeriodlari` kimi "JSON-da struktur" naxışının bir forması,
  fərqi: burada AYRI cədvəldə, hər redaktə üçün bir sətir).
- **Əlavə 4-ün 23-26-cı sahələri (tədbirlər planı) YALNIZ bankın ÖZ təyin
  etdiyi zərər həddindən yuxarı MƏCBURİDİR** (Əlavənin öz qeydi) — bu hədd
  qanunda YAZILMIR, `EmeliyyatRiskiParametrleri` (TƏK sətirli, HR-in
  `MezuniyyetHuquqParametrleri` ilə EYNİ naxış) ilə Risk departamentinin özü
  sazlayır. **Bu, MB-nin 9.9-cu bənddəki hesabat həddi (100 000 AZN, qanunla
  SABİT) İLƏ EYNİ ŞEY DEYİL** — qarışdırma, o hədd hələ kodlanmayıb (Faza 2,
  Əlavə 5 hesabatı ilə birlikdə).
- **Faza 1 əhatəsi**: yalnız jurnal (yaratma/siyahı/detal/redaktə). Mərkəzi
  Banka rüblük hesabat (Əlavə 5, ≥100 000 AZN / 12 ayda 10+ təkrar / potensial
  ≥100 000 AZN threshold-ları) və İstilik xəritəsi (Əlavə 1) — HƏLƏ QURULMAYIB.
- **Biznes sahələri (Əlavə 2) və risk kateqoriyaları (Əlavə 3)** — sabit enum
  + seed (rəsmi normativ mətndir), yalnız 3-cü səviyyə (nümunə) sərbəst mətn,
  çünki Əlavə 3-ün öz qeydi: "nümunələr bank tərəfindən artırıla bilər".

**Struktur:** `EmeliyyatRiskiHadisesi` (Əlavə 4-ün 27 sahəsi — bəziləri
`BaseEntity`-dən gəlir, təkrar yazılmayıb: sahə 1=`YaradilmaTarixi`,
sahə 3=`YaradanIcraciId`, sahə 27-nin tarixi=`YenilenmeTarixi`),
`EmeliyyatRiskiHadisesiTarixce` (redaktə tarixçəsi), `EmeliyyatRiskiParametrleri`
(tək sətirli hədd). `QeydiyyatKodu` ("BS5R5.1" formatında, Əlavə 4 sahə 2-nin
öz nümunəsi) avtomatik hesablanır (`EmeliyyatRiskiAdlari.QeydiyyatKoduHesabla`)
— `RiskKateqoriyasi2` enum-un qiymətləri QƏSDƏN "onlar rəqəmi=1-ci səviyyə"
sxemi ilə seçilib (məs. 51=R5.1) ki, ikinci enum/lookup cədvəli saxlamağa
ehtiyac qalmasın.

⚠️ **"Kim etdi" İKİ FƏRQLİ CƏDVƏLDƏN gəlir, qarışdırma:**
`YaradanIcraciId`/`YenileyenIcraciId` (`BaseEntity`) **AppUser.Id**-dir (sistemə
login edən), `MueyyenlesdirenIsciId` isə **Isci.Id**-dir (istənilən əməkdaş,
sistem istifadəçisi olmaya bilər). Ad üçün birincidə `UserManager<AppUser>`
(canonik nümunə: `ReytingService.cs`), ikincidə `IUnitOfWork.Repository<Isci>()`
lazımdır — bir servisdə ikisini qarışdırmaq səssiz `NullReferenceException`
yox, sadəcə **boş ad** göstərər (dictionary-də tapılmır), diaqnozu çətinləşdirər.

### `code-review` Tapdığı 3 Real Bug (30.09.2026)

İstifadəçinin əvvəlki sessiyada qoyduğu qaydaya görə (*"öz yazdığın koda da
yoxlatdır"*) bu modul da `code-review --level high` ilə yoxlanıldı, 4 tapıntıdan
3-ü təsdiqləndi:

1. **Uzunluq validasiyası yox idi** — migration sütunları `nvarchar(200-2000)`
   ilə məhduddur, amma servis heç bir maksimum uzunluq yoxlamırdı. Nəticə:
   uzun mətn `SaveChangesAsync`-də *"String or binary data would be truncated"*
   ilə BÜTÖV formanı sındırardı — bu, BMI-nin dar sütun tələsi ("Bir Jurnala
   İKİ Yazıcı" bölməsindəki `Kes()` helper-i) ilə EYNİ kateqoriyadır. Düzəliş:
   `SahelərYoxla`-ya hər mətn sahəsi üçün `.Length >` yoxlaması əlavə edildi,
   formaya uyğun `maxlength` atributları qoyuldu.
2. **Tədbir mütləqliyi CANLI hədd ilə yoxlanılırdı, DONDURULMUŞ YOX** — hədd
   sonradan aşağı salınsa, köhnə (o vaxt qanuni şəkildə tədbirsiz qalmış)
   qeydlər növbəti redaktədə (hətta orfoqrafik düzəlişdə belə) birdən-birə
   "tədbirlər planı məcburidir" xətası ilə bloklanardı. **Eyni "dondurma"
   prinsipi layihədə artıq var** — Pul Köçürməsi limitindəki `UsdEkvivalent`
   (əməliyyat anında dondurulur, sonra kurs dəyişsə də keçmiş aylıq cəm
   dəyişmir). Düzəliş: `EmeliyyatRiskiHadisesi.TedbirZererHeddiYaradilmaAninda`
   sahəsi əlavə edildi — qeyd yaradılanda bir dəfə yazılır, redaktədə TOXUNULMUR,
   həm görüntü (`TedbirlerMecburidir`), həm validasiya bundan oxuyur.
3. **Sidebar "Hesabatlar" linki AML səhifəsini "aktiv" saymırdı** — narıqlaşdırma
   (`currentAction=="Index"` → `currentController=="Dashboard"`) yeni jurnal
   linklərini düzgün ayırd etsə də, `Risk/Aml/Index`-in öz sidebar sətri
   olmadığı üçün (Dashboard-dakı kartdan açılır) həmin səhifədə heç bir link
   "aktiv" görünmürdü. Düzəliş: şərtə `|| currentController == "Aml"` əlavə
   edildi.

Dördüncü tapıntı (RiskKateqoriyasi2 `<select>`-in JS uğursuz olanda boş
qalması) real idi, amma bu layihədə partial-larda `@@section` işlədilmədiyi
üçün skript birbaşa `<script>` kimi hər halda işə düşür — yenə də, "JS-ə
görə POST-da düzgün dəyər getməsi" prinsipini (bax "Şərtli Render Olunan
Form Sahəsi" bölməsi) gücləndirmək üçün `RiskKateqoriyasi2` seçimi indi
BÜTÜN variantlarla SERVERDƏ render olunur, JS yalnız `data-ust1` görə
görünməyəni gizlədir (silmir) — JS işə düşməsə belə düzgün seçilmiş dəyər
formadan gedər.

**Hələ tətbiq edilməyib (Faza 2, ayrıca iş):** MB-yə rüblük hesabat (Əlavə 5),
istilik xəritəsi (Əlavə 1), Əsas Risk/Fəaliyyət Göstəriciləri (ƏRG/ƏFG).

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

### Saxlanmış Sorğu `--` ŞƏRHİ İLƏ BAŞLAYA BİLMƏZ (09.09.2026)

`OracleService.YalnizSelect` (sətir 143-150) sorğunun **«SELECT» və ya «WITH»
ilə BAŞLAMASINI** tələb edir:

```csharp
var trimmed = sql.TrimStart();
if (!trimmed.StartsWith("SELECT", …) && !trimmed.StartsWith("WITH", …))
    throw new InvalidOperationException("Oracle-da yalnız SELECT sorğusuna icazə var.");
```

Yəni `OracleSorgular.SorguMetni` **`--` şərh sətri ilə başlayırsa** sorğu Oracle-a
**heç getmir**. Oracle özü belə mətni qəbul edərdi — məhdudiyyət bizimdir.

Real hadisə (09.09.2026, VESIQE_BITME): `docs/sql/hr/…` faylı **bütöv** yapışdırıldı,
mətn «`-- — TÖVSİYƏ OLUNAN VARİANT…`» ilə başladı. Ekranda isə **«BMI-yə bağlanmaq
alınmadı»** yazıldı, çünki servisin `catch (Exception)` bloku hər istisnanı bağlantı
xətası kimi təqdim edirdi — səbəb şəbəkədə axtarıldı.

**Qaydalar:**
- `docs/sql/**` fayllarında **«KOPYALANACAQ MƏTN»** bölməsini açıq işarələ; başlıq
  şərhini sorğunun ÜSTÜNDƏ yox, ayrıca blokda saxla.
- Saxlanmış sorğunu icra edən servisdə `InvalidOperationException`-u **ayrıca tut**
  və mətni olduğu kimi göstər. Ümumi «bağlanmaq alınmadı» mesajı diaqnozu yanlış
  istiqamətə aparır.
- Sorğunun **ORTASINDA** və sonunda `--` şərhi problem deyil — yalnız BAŞLANĞIC.

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

### Kredit Müraciəti — `ReddEdilib` Statusunun İKİ YAZICISI (02.09.2026, KRİTİK)

`KreditMuracietStatus.ReddEdilib` **iki müstəqil yerdən** yazılır. Rədd
məntiqinə toxunanda hər ikisini tutuşdur — biri köhnə qalsa xəta yalnız o yolda
görünər:

| Yazıcı | Yer | Tələb |
|---|---|---|
| **Komitə qərarı** | `KreditQerarService.QebulEtAsync` | protokol № + ən azı bir **aktiv** `KomiteUzvu` imzası, tranzaksiya |
| **Komitəsiz rədd** | `KreditMuracietService.KomitesizReddEtAsync` | səbəb (açar cədvəlindən, aktiv), status `Yeni`/`Yoxlanılır` |

**AYRICA STATUS QƏSDƏN YOXDUR** — ikisini `Qerar` ayırd edir:

```
Komitəsiz rədd  →  Status == ReddEdilib  VƏ  Qerar == null
Komitə rəddi    →  Status == ReddEdilib  VƏ  Qerar != null
```

Komitəsiz rəddə `KreditQerar` sətri **yaradılmır** — «Komitə Qərarları»
səhifəsi yalnız `KreditQerar` oxuduğu üçün işçi rəddi ora düşmür (istifadəçi
qərarı: komitə jurnalı təmiz qalsın).

**Qaydalar:**
- `ReddiGeriQaytarAsync` **komitə qərarına toxunmur** — `Qerar != null` olarsa
  istisna atır. Protokolla verilmiş qərarı bir işçinin düyməsi ilə ləğv etmək olmaz.
- Komitəyə göndərilmiş müraciəti işçi rədd **edə bilməz** (`Status <= Yoxlanilir`
  şərti). O mərhələdə qərar komitənindir.
- Rədd səbəbi **enum deyil, cədvəldir** (`KreditReddSebebleri`) — siyahı biznes
  qərarıdır və artır. Səbəb **silinmir, deaktiv edilir**: keçmiş müraciətlər FK
  ilə ona bağlıdır, silinsə tarixçə «səbəbsiz» qalar.
- Yeni status əlavə etsən enum kifayət etmir: `Index.cshtml` sekmeleri,
  `ViewBag.StatusSaylari` və `Detail.cshtml`-dəki `StatusText`/`StatusCls` də
  yenilənməlidir.

Ətraflı: `docs/kredit/Komitesiz_Redd.md`.

### Word Şablonu — Token Şablonda YOXDURSA Dəyər SƏSSİZCƏ İTİR (02.09.2026)

`KreditWordService.Doldur` şablonda tapmadığı tokeni **sadəcə ötürür** — nə xəta,
nə log. Kod tokeni doldurur, sənəddə isə heç nə görünmür.

Real nümunələr (Kredit Arayışları portu, BMI şablonları ilə tutuşdurma):

| Token | Kod doldurur | Şablonda | Nəticə |
|---|---|---|---|
| `{krtar}` | ✅ (BMI `zaminarayis`) | **YOXDUR** | dəyər heç yerə düşmür — «səhv kimi görünən» kod əslində effektsizdir |
| `{mektarixi}` | ✅ | `carsgirovcix1.docx`-də **YOXDUR** | məktubun tarixi sənəddə çap olunmur |
| `{muqno}` / `{muqNo}` | ikisi də var | **fərqli şablonlarda fərqli** | birini o birinə «uyğunlaşdırsan» token itir |

**Qaydalar:**
- Şablonu dəyişəndə/əvəz edəndə tokenləri **əl ilə tutuşdur**. Tez yol:
  `unzip -p <fayl>.docx word/document.xml | grep -o "{[a-zA-Z]*}" | sort -u`
- Nömrə ilə bağlı tokenlərdə `KreditWordService.TokenVarmi(...)` ilə **açıq
  yoxlama** qoy — səssiz itki orada dağıdıcıdır.
- Böyük/kiçik hərf fərqi tokeni **başqa token** edir.
- Köhnə `.doc` (OLE2) faylları `KreditWordService` ilə **açılmır** — OpenXML
  yalnız `.docx` oxuyur. Word-də «Farklı kaydet → .docx» lazımdır.

### Köhnə Word Şablonu — TOKEN ADI YALAN DANIŞIR + ŞRİFT UNICODE OXUMUR (02.09.2026, KRİTİK)

BMI şablonlarını köçürəndə iki tələ var; hər ikisi **xəta vermir**, yalnız
çıxan sənəddə görünür.

**1. Token adı ilə yeri uyğun gəlmir — şablondan şablona dəyişir.**

| Şablon | başlıq «№ … il» | mətn «… il tarixində bağlanmış» |
|---|---|---|
| DYP, Borcalan | `{muqtar}` | `{mektarixi}` |
| **Zamin** | **`{mektarixi}`** | **`{muqtar}`** ← tərsinə |
| Saipa ×2 | `{muqtar}` | token yoxdur |

`{mektarixi}` adı «məktub tarixi» kimi oxunur, amma DYP/Borcalan-da **müqavilə
tarixidir**. BMI-də görünmürdü, çünki hər ikisinə **bugünkü tarix** yazılırdı.
Real tarix veriləndə dərhal üzə çıxdı: başlıqda müqavilə tarixi, mətndə bugünkü.

**Qayda:** tokeni adına görə xəritələmə — şablonun İÇİNƏ bax. Token run-lara
bölünə bilir, xam `grep` kifayət etmir; paraqrafın bütün `<w:t>` parçalarını
birləşdir.

**2. Köhnə Azəri şriftləri Unicode hərfləri oxumur.**

`Times Latin`, `Ora Times`, `A3 Times AzLat`, `AzrTimes_Lat` — bu şriftlərdə
`Ə Ü İ Ğ Ş Ç Ö` **yoxdur** (mətn Kiril kod nöqtələri ilə yazılır və şrift onu
Azəri hərfi kimi göstərir). Belə run-a müasir Unicode ad yazılanda Word hər
xüsusi hərf üçün başqa şriftə keçir və aralarda **boşluq qalır**:

```
HÜSEYNOV SAMİR MİRHÜSEYN OĞLU  →  HÜ SEYNOV SAMİ R Mİ RHÜ SEYN OĞ LU
```

Şablonun özü də bunu bilir — «ilə» sözündəki `ə` ayrıca `Times New Roman`
run-una qoyulub.

**Həll:** `KreditWordService.Doldur(..., unicodeSrift: "Times New Roman")` —
dəyər yazılan run-un `w:ascii`/`w:hAnsi` şrifti dəyişir, **yalnız dəyərdə
ASCII-dən kənar hərf varsa**. Ölçü/qalın/maili/altxətt toxunulmur; `w:cs`
(complex script) də toxunulmur. Parametr **defolt `null`-dır** ki, mövcud
müqavilə şablonlarının görünüşü dəyişməsin.

**3. Şablondakı sabit şəkilçi.** `{muqtar}-cи il` yazılıbsa və kod tarixi
onsuz da şəkilçi ilə verirsə, nəticə «2021-ci-ci il» olur. Üstəlik şablondakı
sabit `-cи` **həmişə «-ci»** çıxır — 2026 üçün səhvdir. Düzgünü: şablondan
sabit şəkilçini götür, şəkilçini `KreditSozeCevir.TarixiSoze` versin.
⚠️ Şablon serverdəki orijinalla əvəz olunsa bu düzəliş İTİR.

### Kredit Arayışları — BMI-nin ÖLÜ MENYU BƏNDLƏRİ (02.09.2026)

BMI «Kredit DP → Arayışlar» menyusunda **7 bənd** var, amma **yalnız 4-ü işləyir**.
«BTİ arayış», «Qeydiyyata düşmə», «İcarə məktubu» — `Click` handler-i, forma sinfi,
SQL və şablonu OLMAYAN boş menyu elementləridir. Köçürüləndə bu üçü **qəsdən
buraxılıb**; lazım olsalar sıfırdan qurulmalıdır.

**Qayda:** köhnə sistemdən modul köçürəndə menyu siyahısını «tələb» sayma —
əvvəlcə hər bəndin handler-i və formasının **həqiqətən mövcud olduğunu** yoxla.
Olmayan bəndi «köçürdüm» kimi təqdim etmək daha pisdir: istifadəçi onu axtarır.

Ətraflı: `docs/kredit/Arayislar.md`.

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

### Məzənnə Sahəsi — `step` və `decimal(p,s)` BİR YERDƏ MƏHDUDLAŞDIRIR (KRİTİK)

Bir rəqəm sahəsinin real dəqiqliyini **üç** yer birlikdə müəyyən edir; biri dar
qalsa istifadəçi düzgün dəyəri ümumiyyətlə yaza bilmir və ya dəyər səssizcə itir:

| Qat | Yer | Səhv olanda nə olur |
|---|---|---|
| Brauzer | `<input type="number" step="…">` | **Görünən** xəta: «Please enter a valid value. The two nearest valid values are 0 and 0.0001» — forma göndərilmir |
| Baza | `HasPrecision(18, 4)` + migration | **SƏSSİZ**: `0,000002950` → `0,0000` yuvarlaqlaşır |
| Ekran | `ToString("#,0.00")` | **SƏSSİZ**: bazada düzgündür, ekranda «0,00» görünür |

Real hadisə (01.09.2026, Pul köçürməsi 26-T-29): İran rialının MB kursu
`0,000002950`-dir. `step="0.0001"` onu qəbul etmirdi, `Kocurme.RialCbar` isə
`decimal(18,4)` idi. İstifadəçi məcbur olub **10 000 dəfə böyüdülmüş** `0,0295`
yazırdı və bu, mühasibat yazılışının **dilinq fərqi sətrini 10 000 dəfə**
şişirdirdi: `5 014 800,00` əvəzinə `301,50`
(`PulKocurmeVoucher.DilingAdd` → `ferq = Mebleg × IranRial × RialCbar − Mebleg × ValyutaCbar`).

**Ən təhlükəli hal:** kurs `0`-a yuvarlaqlaşsa `DilingAdd()` sıfıra bölmə
qoruyucusuna düşür (`rcbar == 0` → `return`) və **dilinq fərqi sətri provodkadan
tamamilə yox olur**. Xəta yox, log yox — sadəcə bir sətir əskik.

**Qaydalar:**
- **Məzənnə sahəsində `step="any"`** yaz. Onluq yerlərin sayı məzənnədə sabit
  deyil. `step="0.01"` yalnız **pul** məbləğində düzgündür (qəpik həqiqətən 2 onluqdur).
- Baza sütununun `scale`-i real dəyərdən ən azı 2 rəqəm geniş olsun. MB kursları
  `decimal(18,10)`-dur (migration `20260901120000_KocurmeKursDeqiqliyi`).
  `IranRial` **qəsdən (18,2)** qalıb — o, «1 vahid = N rial» kursudur (850 000),
  yəni tam hissəsi böyükdür; 18,10 etsək tam hissəyə cəmi 8 rəqəm qalardı.
- Ekranda məzənnəni `"#,0.00"` ilə **yazma** — `0.##########` işlət
  (`Detal.cshtml` → `Kurs()`, `_Form.cshtml` → `K()`).
- `<input type="number">` **yalnız NÖQTƏ** qəbul edir → `value`-nu həmişə
  `InvariantCulture` ilə yaz. az-AZ vergülü ilə xana **boş** açılır və yadda
  saxlayanda dəyər itər. (Geri oxumaq təhlükəsizdir — `FlexibleDecimalModelBinder`
  həm nöqtəni, həm vergülü qəbul edir.)
- Miqyas dəyişikliyindən sonra **keçmiş sətirlər köhnə miqyasda qalır**. Diaqnostika:
  `docs/sql/emeliyyat/01_Kocurme_RialCbar_Yoxlama.sql` (yalnız SELECT; UPDATE
  şablonu şərh içindədir — düzgün kursu **yalnız mühasib** deyə bilər).
- SQL diaqnostikasında `Mebleg × IranRial × RialCbar` ifadəsini **`float`-a cast et** —
  `decimal(18,2)×decimal(18,2)×decimal(18,10)` 38 rəqəm həddini aşır və
  *«Arithmetic overflow error converting expression to data type numeric»* verir.

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

### Pul Köçürməsi — 20 000 USD AYLIQ LİMİTİ (07.09.2026, KRİTİK)

Qanun: «məqsədi bəyan edilməklə rezident və qeyri-rezident **fiziki şəxsin təqvim
ayı ərzində cəmi 20 000 ABŞ dolları EKVİVALENTİNƏDƏK** məbləğdə olan köçürmələri».
Həddi aşan hissə üçün **əsas sənəd** tələb olunur.

**Məntiq TƏK FAYLDADIR:** `FinNex.Application/Services/Emeliyyat/KocurmeLimit.cs`
(`KocurmeService`-in `partial` hissəsi). Hədd — `AylikLimitUsd` sabiti.

| Qərar | Dəyər | Səbəb |
|---|---|---|
| Sayılan məbləğ | **`Mebleg`** (alınan) | rialda köçürülən 765 000 000 elə həmin 900 USD-dir |
| Ekvivalent | Oracle `func_get_kurval` | əl ilə yazılmır |
| `Secim` | **hər üç variant** sayılır | qanun mətnindən asılı deyil, istifadəçi qərarı |
| `Novu` | **yalnız «Pul»** | Tələbə köçürməsi GƏLƏN puldur, fiziki şəxsdən çıxmır |
| Limit aşılır, sənəd yox | **BLOK** | əməliyyat qeydə alınmır |
| Kurs alınmadı | **BLOK** | «Oracle işləməsə heç nə işləməz» |

**`UsdEkvivalent` ƏMƏLİYYAT ANINDA DONDURULUR.** Aylıq cəm həmin sütunu
**toplayır**, yenidən hesablamır — yoxsa kurs dəyişəndə keçmiş ayın cəmi də
dəyişər və audit zamanı «dünən 19 800 idi, bu gün 20 100» vəziyyəti yaranar.

**FİN NORMALLAŞDIRMASI — `FinTemizle` (boşluqsuz, BÖYÜK hərf).** Yazan da,
axtaran da EYNİ bu metoddan keçir. Biri normallaşdırıb o biri keçməsə
«5ab2cd1» ilə «5AB2CD1» iki ayrı şəxs sayılar və limit **səssizcə ikiqat açılar**.

**REDAKTƏDƏ `xaricId` MƏCBURİDİR** — qeydin özü cəmdən çıxarılmalıdır, yoxsa
10 000-lik köçürməni açan operator məbləği artırmadan «limit aşıldı» alar
(üst-üstə düşən məzuniyyətdə eyni qayda).

**Yazma yolu İKİDİR** — `YaratAsync` və `YenileAsync`. Yoxlama ortaq metoddadır
(`LimitTetbiqEtAsync`); ayrı-ayrı yazılsa biri gec-tez köhnə qalar və limit
yalnız o yolda yan keçilər.

**Ekran SERVERİN rəqəmini göstərir** — `LimitYoxla` endpoint-i yadda saxlama ilə
**eyni** `LimitYoxlaAsync`-i çağırır. JS heç nə hesablamır; iki hesablayıcı
saxlansaydı göstərilən ilə tətbiq olunan gec-tez fərqlənərdi.

**Sənəd növləri açar cədvəldir** (`KocurmeSenedNovleri`) — **seed YOXDUR**,
siyahı boş başlayır. Operator lazım olanı elə köçürmə formasından əlavə edir
(«+ Yeni növ»), növbəti dəfə hamı siyahıdan seçir. Növ **silinmir, deaktiv edilir**.

**Sənəd sahələri formada HƏMİŞƏ render olunur** (yalnız `hidden` ilə gizlənir) —
şərtlə render etsək, göndərilməyən sahə POST-da `null` gəlib mövcud dəyəri
**səssizcə silərdi** (icazə nahar bayrağı hadisəsi).

**Oracle sorğusu:** `VALYUTA_KURSU` — `docs/sql/valyuta/Valyuta_Kursu_OracleSorgu.sql`.
Sütun adı **`KURS`** olmalıdır; adı dəyişsə kurs `null` qayıdar və **hər əməliyyat
bloklanar**. Yer tutucuları: `{KOD}`, `{TARIX}`.

**Formadakı valyuta MƏTNDİR** («USD»/«Avro»/«AZN»), Gedən həvalədən fərqli olaraq
(orada KOD saxlanılır) — `ValyutaKodu()` xəritəsi ona görə var. Formaya yeni mədaxil
valyutası əlavə olunsa **xəritəyə də əlavə edilməlidir**, yoxsa əməliyyat bloklanar.

**Keçmiş qeydlərdə FİN boşdur** — cəmə düşmür. İstifadəçi qərarı: köhnələrə FİN
**əl ilə** yazılacaq (redaktə səhifəsindən).

**FORMA FİN-DƏN BAŞLAYIR (1-ci addım).** İstifadəçi qərarı: «bəlkə limiti keçib,
boş yerə niyə doldursun». FİN xanası formanın ƏN ÜSTÜNDƏDİR (`pkLimitCard`,
`autofocus`), «Göndərən» kartının içində DEYİL — operator FİN yazan kimi serverdən
cavab gəlir və lazım olsa sənəd sahələri elə orada açılır. Xanaları aşağı qaytarsan
bütün mənası itir.

Məbləğ hələ yazılmayanda mesaj **ayrı qoldadır** (`YeniUsd <= 0`) — «bu əməliyyatla
… olur» yazmaq mövcud olmayan məbləğdən danışmaq olardı; o an lazım olan yeganə
rəqəm ayın cəmi və qalıqdır. Vurğulanan hissə serverdə `**…**` ilə işarələnir,
`_Form.cshtml`-dəki `yaz()` onu `<b>`-yə çevirir (əvvəl HTML qaçırılır, SONRA
nişan çevrilir). `TempData`-ya gedən mətndə nişan `KocurmeService.MesajDuz()`
ilə atılır — yoxsa istifadəçi hərfi `**19.117,65 USD**` görər.

### ARDICILLIQ KİLİDİ — `disabled` YOX, `inert` (08.09.2026)

İstifadəçi qərarı: «FİN yazmadan digər işlər … yazmaq mümkün olmasın».
FİN boş olanda formanın qalan hissəsi **solğun və toxunulmazdır**; ilk hərfdə açılır.

| Seçim | Səbəb |
|---|---|
| Gizlətmək YOX, **kilidləmək** | gizli sahə render olunmur → POST-da getmir → redaktədə dəyər silinər |
| `disabled` YOX, **`inert`** | `disabled` sahəni brauzer POST-a **ümumiyyətlə qoymur** — gizlətmək ilə eyni nəticə |
| `filter` YOX, yalnız `opacity` | `filter` mətni bulanıqlaşdırır, `position:fixed` üçün containing block yaradır |
| **Redaktədə kilid YOXDUR** | köhnə qeydlərdə FİN onsuz da boşdur; kilid onları düzəltməyi bloklayardı |
| `.dm-actions` bütöv kilidlənmir | «Ləğv et» həmişə işləməlidir — yalnız göndər düyməsi sönür |

Göndər düyməsi **`disabled` edilir** — o, data sahəsi deyil, POST-da itən bir şey
yoxdur. Əlavə sədd: `form.submit` hadisəsində FİN boşdursa `preventDefault()`
(Enter ilə göndərmə düymənin `disabled`-ını yan keçir).

Kilidlənən elementlər `pk-row--fin` sinfinə görə ayırd olunur — həmin sinif
silinsə **bütün forma kilidlənər** və heç bir xəta çıxmaz.

### FİN MƏCBURİDİR — İKİ QATDA (08.09.2026)

| Qat | Qayda |
|---|---|
| İnterfeys | kilid — FİN boşdursa forma açılmır, düymə sönükdür |
| **Server** | `YaratAsync` → FİN boşdursa `Result.Fail`, heç nə yazılmır |

Kilid **tək başına kifayət deyil**: JS sönük brauzer, birbaşa POST, köhnə açıq
səhifə onu yan keçir. Kilid rahatlıq üçündür, **qayda serverdədir** — layihədə
bunun beş real hadisəsi var (silinmiş action, `undefined` data-atribut, boş
endpoint: «ekranda düz görünürdü, arxada işləmirdi»).

**`YenileAsync` FƏRQLİDİR — orada FİN boş qala bilər.** Səbəb: köhnə qeydlərdə
FİN onsuz da yoxdur və məhz redaktə səhifəsindən yazılır (istifadəçi qərarı).
Məcburi etsək, FİN-i bilməyən operator həmin qeydin məzənnəsini belə düzəldə
bilməzdi. Qoruyucu var: **mövcud FİN SİLİNƏ BİLMƏZ** — dolu sahəni boşaltmaq
cəhdi bloklanır (yoxsa keçmiş əməliyyat aylıq cəmdən səssizcə düşərdi).

⚠️ **AÇIQ MƏSƏLƏ — əcnəbi göndərən.** Qanun qeyri-rezidenti də əhatə edir, amma
bir dəfəlik gələn əcnəbinin AZ FİN-i olmaya bilər. İndiki qayda onu FİN yazmağa
məcbur edir; sahə yoxdursa operator uydurma dəyər yazar və cəm **yalançı şəxs
altında** toplanar — limit işləmiş kimi görünər, əslində işləməz. Həlli
qərarlaşmayıb (ehtimal: FİN **və ya** passport üzrə tanıma). Şikayət gələndə
əvvəlcə bura bax — səbəb kodda yox, açarın seçimindədir.

**`fetch`-də NİSBİ ÜNVAN YAZMA.** `fetch('LimitYoxla')` yalnız `/…/Yarat`-da düz
işləyir; `/…/Redakte/5`-də brauzer onu `/…/Redakte/LimitYoxla` kimi həll edir və
yoxlama **səssizcə** dayanır (JS `.catch`-ə düşür). Üç yer də `@Url.Action(...)`
ilə mütləq ünvan yazır — `LimitYoxla`, `SenedNovuYarat` (`_Form.cshtml`),
`FinAyliqCem` (`Index.cshtml`). `id = (int?)null` ambient route dəyərini atır.

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
