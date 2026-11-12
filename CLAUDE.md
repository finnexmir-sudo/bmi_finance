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
- **Bir hesablama/düstur səhvini düzəldəndə, "düzəldim" demədən ƏVVƏL bütün repoda
  eyni ifadəni/dəyişəni `grep` ilə axtar** (26.10.2026, real hadisə: xəstəlik
  kəsintisi `MaasHesablamaService`-də düzəldildi, elan edildi, istifadəçi publish
  edib yoxladı — rəqəm DƏYİŞMƏDİ, çünki eyni köhnə düstur `MaasController`-in öz
  Toplu Maaş önizləməsində AYRICA, dublikat kodla təkrarlanırdı). "Əsl hesablama"nı
  (servis metodu) düzəltmək kifayət etmir — onu göstərən HƏR ekranın/preview-un,
  controller-in öz dublikat kodu ola bilər (bax "Bir Kəmiyyətə Üç Yazıcı" prinsipi).
  Axtarış açarı: dəyişən adı (`SirketGunSayi`), əməliyyatın şəkli (`/ ayIsGunu *`),
  və ya köhnə sahə adının özü. Tapılan HƏR yeri eyni düzəlişlə (tərcihən **tək
  paylaşılan metoda çıxararaq**, OzHesabinaIsGunuSayAsync nümunəsi kimi) bağla.
- **"Build 0 xəta verdi" = "sintaksis düzgündür" deməkdir, "məntiq hər yerdə
  düzəldi" demək DEYİL.** İkisini qarışdırıb istifadəçiyə "düzəldi" demə — əvvəlcə
  yuxarıdakı `grep` axtarışını bitir, sonra de.

## Maliyyə və Maaş Hesablamaları

- Payroll hesablamaları tarix aralığı əsasında işləyir — `IsGunlerininSayi` sıfırlamaq kifayət deyil, query-dən filtrlənməlidir.
- Məzuniyyət dəyişikliklərindən sonra `TopluHesabla` səhifəsini real data ilə yoxla.
- İkiqat sayılma riskini hər zaman nəzərə al (korreksiya + orijinal).
- **Hər maaş rəqəmi İKİ YERDƏN gələ bilər — servisin əsl hesablaması (`MaasHesablamaService.
  FerdiHesabla`, yazılan dəyər) VƏ Toplu Maaş ekranının ÖZ preview kodu
  (`MaasController.TopluHesabla`/`TopluOnizleme`).** Birini düzəldib o birini
  unutmaq — rəqəmin publish-dən sonra da "dəyişmədi" görünməsinin ən çox rast
  gəlinən səbəbidir. Hər ikisini tutuşdurmadan "düzəldi" demə.
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

- ~~`MaasController.MezQabaqcadanBrutMapAsync` — **provodka** (xərc sətri);~~
  **28.10.2026-da DƏYİŞDİRİLDİ** — bax aşağı "Provodka — Qabaqcadan Məzuniyyət
  Xərci AYLARA BÖLÜNMÜŞ Modelə Keçdi" bölməsi.
- `MaasController` Maas Detail başlığı — `ViewBag.TamGross` / `TamNet`. **HƏLƏ
  KÖHNƏDİR** — aşağıdakı 28.10.2026 düzəlişi BUNA toxunmayıb.

Yəni «ödənilmə ayı» filtri artıq önizləmə sorğusu ilə **eyni deyil**. Uçot tərəfi
bunu da aylara bölmək istəyirsə — **mühasibin qərarıdır**, özbaşına dəyişmə.

### Provodka — Qabaqcadan Məzuniyyət Xərci AYLARA BÖLÜNMÜŞ Modelə Keçdi (28.10.2026, KRİTİK)

Real hadisə: Elxan Axundov üçün Oktyabr 2026 provodkasının "rezident işçilərə
məzuniyyət haqqı" sətri mühasibin gözlədiyindən **4.813,85 AZN az** çıxdı —
dəqiq onun 01.10–31.10.2026 məzuniyyət avansının brüt dəyəri qədər. Maaş Detalı
səhifəsi "Mezuniyyet (qabaqcadan — bu ayın payı)" sətrində bu məbləği **artıq
oktyabra aid** göstərirdi (günlərin TAM oktyabrda olması). Mühasib: *"bu
hesablama oktyabra aiddir sentyabra yox axi"* — yəni avans sentyabrda
ödənilsə belə, XƏRCİN ÖZÜ məzuniyyət günlərinin düşdüyü aya (oktyabr) aid
olmalıdır — bu, yuxarıdakı "QƏSDƏN KÖHNƏ QALDI" qeydinin gözlədiyi məhz O
mühasib qərarıdır.

**Kök səbəb:** `MezQabaqcadanBrutMapAsync` 27.08.2026-dakı "Aylara Bölünmə"
keçidini (`AvansAylaraBolunurmu`) **heç yoxlamırdı** — kəsim tarixindən
(01.08.2026) SONRAKI aylar üçün də şərtsiz köhnə "ödənmə ayı" filtrini
işlədirdi. Halbuki `TopluHesabla` ekranının öz "Məzuniyyət haqqı" sütunu
(sətir ~1316-1355) elə HƏMİN kəsimi onsuz da yoxlayır və post-kəsim aylarda
`MaasHesablamaService.MezuniyyetAvansAyPaylariAsync`-dan (ay-ay bölünmüş
pay, **yeganə kanonik mənbə** — bax yuxarı) istifadə edir. Yəni provodka ilə
Toplu Hesabla ekranı artıq fərqli ay-attribusiya modelləri işlədirdi —
Toplu Hesabla düzgün (yeni) model idi, provodka köhnə qalmışdı.

**Düzəliş — `MaasController.MezQabaqcadanBrutMapAsync`:** indi `TopluHesabla`-nın
eyni naxışını (sorğu şərti + `MezuniyyetAvansAyPaylariAsync` çağırışı) təkrarlayır
— yeni düstur İCAD EDİLMƏYİB, mövcud kanonik mənbəyə müraciət edilib:
- `AvansAylaraBolunurmu(il, ay)` **true**-dursa → `Mezuniyyet` qeydləri **tarix
  kəsişməsi** üzrə seçilir (`BaslamaTarixi < növbətiAyİlk && BitmeTarixi >=
  ayİlkGünü`), hər qeyd üçün `MezuniyyetAvansAyPaylariAsync`-dan bu AYIN payı
  (`Brut`) oxunur, işçi üzrə cəmlənir.
- **false**-dursa (kəsimdən ƏVVƏLKİ aylar) → köhnə "ödənmə ayı" filtri olduğu
  kimi qalır — keçmiş aylar üçün artıq saxlanmış provodkaların yenidən
  hesablanması/dəyişməsi riski YOXDUR.

**Çağırış yerləri (ikisi də avtomatik düzəldi, ayrıca dəyişiklik lazım
olmadı):** `PravodkaExport` (saxlanmış maaşdan) VƏ `PravodkaOnizlemeExport`
(dry-run önizləmə) — hər ikisi eyni `MezQabaqcadanBrutMapAsync`-i çağırır
(sətir 105, 405), tək yerdə düzəliş kifayətdir.

⚠️ **`ExcelIxrac` (Mühasib "Avans hesablama.xls" ixracı, sətir 524) TOXUNULMADI**
— bu, PROVODKADAN AYRI, DÖRDÜNCÜ bir hesablama yoludur (`ayBas`/`ayBit`
`BaslamaTarixi` üzrə sadə filtr, `MezuniyyetAvansAyPaylariAsync`-ı ÇAĞIRMIR,
`OdenenMeblegBrut`-u TAM yazır). Bu sessiyada istifadəçi yalnız provodka
sətrini göstərdi — `ExcelIxrac` eyni bugu daşıya bilər (çoxaylı məzuniyyətdə
tam brütü YALNIZ başlanğıc ayına yazar), amma ayrıca təsdiqlənməyib,
dəyişdirilmədi.

⚠️ **`ViewBag.TamGross`/`TamNet` (Maas Detail başlığı) TOXUNULMADI** — yuxarıdakı
qeyddə göstərildiyi kimi bu, ayrıca bir "qəsdən köhnə qalan yer"dir, bu gün
tutulan qərar ona aid edilmədi.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — kəsimdən (01.08.2026) ƏVVƏLKİ aylar üçün
`avansAylaraBolunur=false` qalır, köhnə davranış dəyişmir. Artıq YARADILMIŞ
(bu düzəlişdən əvvəlki) Oktyabr 2026 provodkası özbaşına yenilənmir —
istifadəçi "Yenidən Hesabla"/təkrar `PravodkaExport` çağırışı ilə yeni
Excel-i təkrar yükləməlidir ki, düzəliş əks olunsun.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, `_hesablamaService`-in sinif sahəsi kimi artıq
mövcud olduğu təsdiqləndi — yeni asılılıq əlavə edilmədi). İstifadəçi Visual
Studio-da **Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır: Oktyabr
2026 provodkasını yenidən ixrac et → Elxan Axundovun (və bənzər hallarda
digər işçilərin) "rezident işçilərə məzuniyyət haqqı" sətrinin indi 4.813,85
AZN artdığını (yəni Maaş Detalı-dakı "bu ayın payı" ilə üst-üstə düşdüyünü)
təsdiqləməli — "Maliyyəyə toxunan hər dəyişiklik" qaydasına görə.

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

### Eyni Tələ İkinci Dəfə — `AppUser` `FinNex.Domain`-dədir, `.Entities.HR` YOX (09.10.2026)

Real hadisə — Kassa modulu (yuxarı bax), real Visual Studio **Rebuild** 2 × **CS0246**
verdi: `ExchangeTesdiqController.cs`-də `UserManager<AppUser>` sahəsi tanınmırdı.
Fayl `using FinNex.Domain.Entities.HR;` yazmışdı (`Isci`, `IsciTeyinat`, `IsciStatus`
elə oradandır) və `AppUser`-in də eyni qovluqda olacağı **güman edilmişdi** — amma
`AppUser` əslində **`FinNex.Domain`** namespace-indədir (`RoleNames` ilə eyni yerdə).
Eyni paketdəki digər iki controller (`ExchangeController`, admin
`KassaTesdiqEdiciController`) `using FinNex.Domain;`-i **artıq yazmışdı** (mövcud
`KreditBaxanIsciController`-dən köçürülüb) — unudulan TƏK fayl elə bu idi.

**Qayda:** eyni `Isci`-əsaslı controller qrupunda belə, **hər fayl üçün ayrıca**
yoxla ki, `AppUser` işlədirsə `using FinNex.Domain;` da var — "bu qrupdakı digər
fayllar düzgündür" fərziyyəsi hər faylı tək-tək örtmür. Bu sessiyada `dotnet build`
mümkün olmadığı üçün bu xəta yalnız istifadəçinin real Rebuild-ində üzə çıxdı —
"`FindFirstValue` CS1061" bölməsindəki xəbərdarlığın elə növbəti nümunəsi.

### ÜÇÜNCÜ DƏFƏ — Göndərilən Mail modulu (12.10.2026)

İstifadəçinin real Visual Studio Rebuild-i (ekran-görüntüsü) `GedenMailController.cs`
və `MailIzlemeController.cs`-də **4 × CS0246** göstərdi — EYNİ səhv: hər iki fayl
`UserManager<AppUser>` işlədirdi, amma yalnız `using FinNex.Domain.Entities.HR;`
yazmışdı (`GelenMailController.cs`-dən köçürülərkən bu faylın özündə **İKİ**
using olduğu — `FinNex.Domain` VƏ `FinNex.Domain.Entities.HR` — gözdən qaçırılıb,
yalnız HR-li sətir köçürülüb). Düzəliş: hər iki fayla `using FinNex.Domain;`
əlavə edildi; `using FinNex.Domain.Entities.HR;` isə heç bir HR tipi (Isci və s.)
işlətmədikləri üçün tamamilə **silindi** (ölü using qalmasın).

**Qayda bir daha təsdiqləndi:** `UserManager<AppUser>` yazılan HƏR yeni fayl üçün
`using FinNex.Domain;`-i mövcud bir fayldan "köçürmək" kifayət etmir — həmin
fayldakı BÜTÜN using sətirlərini (təkcə ən oxşar görünəni yox) tutuşdurmaq lazımdır.

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

**Sadə "(il)" mötərizəsi → jeton görünüşlü badge (07.10.2026, eyni gün, ikinci
tələb).** İstifadəçi: *"bunu jeton adlı falan biraz fərqli görüntü ilə edə
bilərdik?"* — sadə mətn mötərizəsi əvəzinə, başlığın yanına kiçik, qızılı
(`#fef3c7`/`#92400e`, mövcud "gözləyir" amber rəngi — `uj-status--gozlenilir`
ilə eyni ton) bir pill qoyuldu: `<i class="bi bi-award-fill"></i> 2026`,
`title="36 Saat Hüququ 2026"` tooltip-i ilə. Yenə sırf markup — `fn-card-title`
class-ı saxlanıldı (yalnız `display:flex` inline əlavə edildi), `fn-card-link`
("Hamısı →") toxunulmadı. Heç bir yeni CSS class/fayl əlavə olunmadı, mövcud
`bi-award-fill` ikonu (digər jeton kartlarında onsuz da işlənir) təkrar
istifadə edildi.

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

### `JetonAnalitikaController` — "Maliyyə Öhdəliyi" Qismən Xərclənmiş Jetonu Tam Dəyərdə Sayırdı (07.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi "HR → Jeton Analitika" kartındakı "52,5 saat / 52,5 ₼" rəqəmini
soruşanda üzə çıxdı: `JetonAnalitikaController.GetData()`-dakı `toplamSaat`
(KPI kartı **və** "Maliyyə Öhdəliyi" donut qrafiki) və `deptData.aktivSaat`
(Departament Müqayisəsi) `aktiveMusbetler.Sum(x => x.JetonTeyinati.
SaatDeyeri)` yazırdı — jetonun **tam nominal dəyərini**, `x.QalanSaat` isə
heç nəzərə alınmırdı.

Problem: bir müsbət jeton **qismən** xərclənsə (Qara Jeton kəsintisi və ya
redim) belə, `QalanSaat` azalsa da, `Status` **Aktiv** qalır (yalnız tam
sıfırlananda `IstifadeOlunub` olur — bax `QaraJetonKesintisiTetbiqEtAsync`).
Bu analitika sorğusu `Status==Aktiv` filtrindən sonra `QalanSaat`-a
baxmadığı üçün, belə bir jetonu **hələ də tam dəyərində** sayırdı — "Bank
üzrə gələcək xərc" kimi təqdim olunan rəqəm real öhdəlikdən **BÖYÜK** çıxa
bilərdi.

Kanonik düstur onsuz da layihədə var idi — `JetonService.AktivSaatBalansiAsync`
(Dashboard/Cüzdanımın işlətdiyi, düzgün yer): `x.QalanSaat ?? x.JetonTeyinati.
SaatDeyeri`. Analitika kontrolleri bunu təkrarlamaq əvəzinə öz (natamam)
sorğusunu yazmışdı — **Düzəliş: hər iki yerdə eyni `?? ` fallback-i əlavə
edildi.**

**Qayda:** "aktiv jeton balansı" hesablayan HƏR yeni sorğu/kontroller
`AktivSaatBalansiAsync`-in düsturunu təkrarlamalıdır (`QalanSaat ?? SaatDeyeri`),
`Status==Aktiv` filtri TƏK BAŞINA kifayət etmir — Aktiv ≠ tam dəyərli.

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

## Qara Jeton Ləğvi İndi Kəsintini TAM GERİ QAYTARIR (08.10.2026)

İstifadəçi sualı: *"qara jeton verdim və müsbətdən çıxıldı, sonra o qara jetonu
silmək olurmu? əgər olursa silinmişi geri qaytaracaqmı?"* — ikinci sual üçün
cavab əvvəlcə **XEYR** idi (kodu oxuyub tapıldı): `JetonLegvetAsync` yalnız
Qara Jetonun ÖZ statusunu "Ləğv edildi" edirdi, kaskadın (FIFO müsbətdən çıxma,
"36 Saat Hüququ"ndan çıxma, `QaraJetonBorcu` sətri) heç bir izinə TOXUNMURDU —
HR səhvən Qara Jeton versə, ləğv etsə belə zərər işçidə qalırdı. İstifadəçi
qərarı: *"bəli, çünki səhvən verilmə ehtimalı var axı"* — düzəldildi.

**Yeni ledger — `QaraJetonKesinti`** (hər kaskad addımını AYRI sətirdə saxlayır):
`QaraJetonId`, `Tur` (`MusbetKesinti`/`IllikHuquqKesinti`/`BorcYarandi`/`BorcOdenisi`),
`HedefJetonId` (`MusbetKesinti`/`IllikHuquqKesinti`/`BorcOdenisi` üçün — təsirlənən
`IsciJetonu.Id`), `BorcId` (`BorcYarandi`/`BorcOdenisi` üçün), `Miqdar`,
`GeriQaytarilib`.

**Niyə sadəcə `IsciJetonu.QaraJetonId`-yə güvənmək KİFAYƏT ETMİRDİ:** o, TƏK
int sahədir — eyni müsbət jeton gələcəkdə BAŞQA bir Qara Jetondan da kəsilsə,
üzərinə yazılıb köhnə əlaqəni itirər. Ledger isə hər addımı ayrı saxladığı üçün
ləğv ediləndə DƏQİQ "bu Qara Jeton bu jetondan NƏ QƏDƏR götürüb" bilinir.

**`QaraJetonKesintisiniGeriQaytarAsync`** (`JetonLegvetAsync`-dən Menfi jeton
üçün çağırılır) hər `GeriQaytarilib=false` addımı tərsinə çevirir:
- `MusbetKesinti`/`IllikHuquqKesinti`/`BorcOdenisi` → hədəf jetonun `QalanSaat`-ı
  `Miqdar` qədər artırılır (`JetonTeyinati.SaatDeyeri`-dən yuxarı keçmir —
  qoruyucu clamp), `İstifadəOlunub` idisə yenidən `Aktiv` olur. **`BorcOdenisi`
  xüsusilə vacibdir** — Qara Jetonun yaratdığı borcu SONRADAN verilmiş TAMAMİLƏ
  AYRI (mükafat) bir jeton ödəmiş ola bilər (`QaraJetonBorcunuOdeAsync`); əsas
  Qara Jeton ləğv ediləndə bu YENİ jetonun yediyi hissə də geri qayıtmalıdır —
  əks halda işçi haqsız yerə iki dəfə (əvvəlki kəsinti + sonrakı mükafatın
  yeyilməsi) zərər çəkmiş olardı.
- `BorcYarandi` → `QaraJetonBorcu.Status = LegvEdildi` (yeni status dəyəri,
  `MuddetiBitib`-lə QARIŞDIRMA — səbəb fərqlidir: biri "il bitdi, bağışlandı",
  o biri "əsas cəza səhv idi, ləğv edildi").

**Mesajdakı rəqəm İKİ DƏFƏ SAYILMAMALIDIR:** `BorcYarandi` addımının ÖZ
`Miqdar`-ı (borcun YARANDIĞI andakı TAM dəyəri) yox, borcun **CARİ** `QalanSaat`-ı
istifadə olunur — çünki bu borc artıq qismən `BorcOdenisi` ilə ödənmiş ola bilər
və həmin hissə onsuz da `berpaSaat` tərəfində sayılıb. İki ayrı cəm (`berpaSaat`
+ `legvBorcSaat`) saxlanılıb, sonda toplanıb — bu, `JetonKesilenSaatDoldurAsync`-
dəki "ikiqat sayılma" tələsinin (bax Payroll bölməsi) eyni növüdür, fərqli yerdə.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — ledger yalnız BUNDAN SONRA verilən Qara Jetonlar
üçün yazılır. Köhnə Qara Jeton ləğv edilsə, `QaraJetonKesintisiniGeriQaytarAsync`
0 addım tapır və `JetonLegvetAsync` açıq xəbərdarlıq qaytarır: *"Bu jeton köhnə
(jurnal yaradılmazdan əvvəlki) kəsintidir — kəsilən saat avtomatik geri
qaytarıla bilmədi, əl ilə yoxlayın."* — səssiz "uğurlu" mesaj YAZILMIR, HR
yanlış əmin olmasın.

**UI tərəfdə heç bir dəyişiklik lazım olmadı** — `IsciAktivJetonlariniGetirAsync`
(Cüzdanım) və `AktivSaatBalansiAsync` onsuz da `Status == Aktiv` filtri işlədir;
`jeton.Status = Legvedildi` olan kimi Qara Jeton özbaşına kart siyahısından
yoxa çıxır (eynilə müsbət jeton ləğvi kimi), HR-ın tam tarixçə cədvəlində isə
mövcud `hjStatusBadge(3)="Ləğv edildi"` badge-i avtomatik düzgün görünür.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı.**
İstifadəçi Visual Studio-da **Rebuild All** ilə yoxlamalı, həm də real ssenari
ilə test etməlidir (qara jeton ver → müsbətdən çıxılsın → ləğv et → müsbətin
`QalanSaat`-ı dəqiq geri qayıtsın, "36 Saat Hüququ" və gözləyən borc varsa onlar
da düzəlsin) — "Maliyyəyə/balansa toxunan hər dəyişiklik" qaydasına görə.

## `HR/JetonSaatlari` — "36 Saat Hüququ" Mükafat Balansına Qarışırdı + Rəhbər Üçün Qalıb/İstifadə Sütunları (14.10.2026)

İstifadəçi (Rəhbər rolunda) sual verdi: *"bizde mumkundurki iscilerin qalan
saatlarina baxmaq? bele bir sehifemiz var?"* — cavab olaraq mövcud
`HR → İşçi Jeton Saatları` (`/HR/JetonSaatlari`) göstərildi, amma eyni anda bir
qüsur aşkar edildi: bu səhifənin "Cari balans"/"Qazanılmış"/"İstifadə" sütunları
`IsciJetonu.Nov == Musbat` olan HƏR jetonu cəmləyirdi — **"36 Saat Hüququ"
jetonunu (`Sistemli=true`) FİLTRLƏMƏDƏN**. Bu, layihənin hər yerdə (6+ yerdə:
`AktivSaatBalansiAsync`, FIFO kəsinti, HR kataloqu, `JetonAnalitikaController`-in
07.10.2026-da düzəldilmiş eyni bugu və s.) riayət etdiyi `!x.JetonTeyinati.
Sistemli` qoruyucusunun BU səhifədə unudulmuş forması idi — mükafat balansı
illik icazə hüququ ilə səssizcə qarışırdı.

İstifadəçinin sonrakı tələbi dəqiqləşdi: *"rehber bilmek isteyirki hemde illik
saatlardan kimde ne qalib ne istifade olunub"* — yəni mükafat balansından AYRI,
konkret "36 Saat Hüququ" üçün hər işçidə qalıb/istifadə olunub sütunları.

**Düzəliş — iki hissədən ibarətdir:**

1. **Qarışıqlıq bugu düzəldildi** — `JetonSaatlariController.Index`-də `musbet`
   siyahısına `&& !x.JetonTeyinati.Sistemli` əlavə edildi. İndi "Mükafat
   qazanılmış/istifadə/balans" sütunları YALNIZ Qızıl/Gümüş/Bürünc/Platin kimi
   real mükafat jetonlarını əks etdirir.
2. **İKİ YENİ sütun əlavə edildi** (mövcud sütunlardan qalın sol xətt ilə
   vizual ayrılıb): **İllik hüquq**, **İstifadə olunub (@il)**, **Qalıb (@il)**.
   Mənbə — YENİ DÜSTUR YAZILMADI, iki artıq kanonik yerdən oxunur:
   - **Hüquq** — `IJetonService.IllikHuquqQalanSaatAsync(isciId, cariIl)` —
     `DashboardService`-in öz kartının ("İcazə saatı — bu il") işlətdiyi EYNİ metod
     (Qara Jeton kəsintisi nəzərə alınmış).
   - **İstifadə** — `IIcazeService.GetIsciIzlemeAsync` (cari il: 1 yanvar →
     bu gün), nəticənin `TesdiqSaat` sahəsi — bu, CLAUDE.md-də "İcazə — Nahar
     Güzəşti" bölməsində sadalanan icazə-sayılan-saat düsturunun (nahar + jeton
     çıxılmaqla) artıq mövcud, kanonik **altı** çağırış yerindən biridir.
   - **Qalıb** = Hüquq − İstifadə (DashboardService-in `IcazeQaligSaat`-ı ilə
     EYNİ düstur, mənfi ola bilər — `<0` olanda qırmızı işarələnir + KPI kartında
     "İllik hüququ aşan işçi" sayğacı).

⚠️ **N+1 qəbul edildi, optimallaşdırılmadı.** `IllikHuquqQalanSaatAsync` hər
işçi üçün ayrıca çağırılır (yoxdursa jetonu YARADIR — `DashboardService`-in də
etdiyi kimi) — bu, HR-ın bir dəfəlik, az-tezlikli hesabat səhifəsi olduğu üçün
qəbul edilən xərcdir, `GetIsciIzlemeAsync` isə BİR dəfə (bütün işçilər üçün
toplu) çağırılır, N+1 deyil.

⚠️ **Rol dəyişmədi** — səhifə əvvəldən HR/Admin/Rəhbərə açıq idi
(`JetonSaatlariController`-in sinif atributu), yeni icazə əlavə edilmədi.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(namespace-lər hər `using` üçün faylın öz mənbəyindən təsdiqləndi — `IIcazeService`
→ `FinNex.Application.Interfaces`, `IcazeIzlemeFiltrDto` → `FinNex.Application.
DTOs.HR.Icaze`, `IJetonService` qlobal namespace-dədir, `using` lazım deyil —
bax "Qovluq Adı ≠ Namespace" bölməsi). İstifadəçi Visual Studio-da **Rebuild
All** ilə yoxlamalı, həm də real ssenari ilə: (1) "Mükafat balans" rəqəminin
əvvəlkindən kiçildiyini (36-saat hüququ artıq qarışmır) və (2) yeni "Qalıb"
sütununun Dashboard-dakı "İcazə saatı — bu il" kartı ilə HƏR işçi üçün dəqiq
üst-üstə düşdüyünü təsdiqləməli.

### `HR/Jeton` ("Jeton İdarəetməsi") HƏLƏ KÖHNƏ RƏQƏMİ GÖSTƏRİRDİ — Yuxarıdaki Düzəliş Bu Səhifəyə KÖÇÜRÜLMƏMİŞDİ (15.10.2026, KRİTİK)

İstifadəçi iki ekran-görüntüsü göndərdi: eyni işçinin (Əjdər Dəmirov) "36 Saat
Hüququ 2026" jetonu `HR → Jeton İdarəetməsi` ("Əməliyyatlar" tabı) səhifəsində
**QALAN=36** (tam, toxunulmamış) göstərirdi, `HR → İşçi Jeton Saatları`
səhifəsində isə eyni işçi üçün **Qalıb=32,42** (36 − 3,58 istifadə) göstərirdi
— sual: *"ejdere bax, niye ferqlidir? isci jeton saatlarinda duzdur ama
idareetmede yox"*.

**Kök səbəb — yuxarıdakı (14.10.2026) düzəliş YALNIZ `JetonSaatlariController`-ə
yazılmışdı, "Jeton İdarəetməsi"ni çağıran `JetonService.JetonEmeliyyatlariGetirAsync`
isə TOXUNULMAMIŞ qalmışdı.** Bu metod (`GET /HR/Jeton/GetEmeliyyatlar`-ı
qidalandırır) `IsciJetonu.QalanSaat`-ı **olduğu kimi** DTO-ya yazır (`MapJeton`),
`hr-jeton.js`-in `hjQalanGoster`-i isə `qalanSaat ?? jetonSaatDeyeri` məntiqi ilə
göstərir. "36 Saat Hüququ" jetonunun `QalanSaat`-ı isə (yuxarı bax, "Qara Jeton"
bölməsi) **YALNIZ Qara Jeton kəsintisi ilə dəyişir** — adi icazə istifadəsi ONA
HEÇ TOXUNMUR (`IcazeUcunFifoJetonXercleAsync` və s. hamısı `!Sistemli` filtri ilə
bu jetonu bilərəkdən kənarda saxlayır). Nəticə: Qara Jeton heç verilməyibsə
`QalanSaat` HƏMİŞƏ `null`/tam dəyərdir, "Jeton İdarəetməsi" də HƏMİŞƏ 36/36
göstərir — işçi nə qədər icazə istifadə edib-etməməsindən asılı olmayaraq.

**Niyə sadəcə eyni hesablamanı `JetonService`-in özünə köçürmək OLMADI —
DAİRƏVİ DI ASILILIĞI:** `IcazeService` artıq `IJetonService`-i inject edir
(jeton-ödənişi FIFO üçün, `IcazeUcunFifoJetonXercleAsync`). `JetonService`-ə
`IIcazeService` əlavə etsəydik, `JetonService → IcazeService → JetonService`
dairəsi yaranardı — ASP.NET Core DI konteyneri bunu resolve anında
`InvalidOperationException` ("A circular dependency was detected...") ilə
rədd edərdi. Buna görə hesablama **`JetonController`-ə** (servis qatına yox,
controller-ə) köçürüldü — controller hər iki servisi sərbəst inject edə bilər,
dairə yalnız servislər arasında qadağandır.

**Düzəliş — üç fayl:**
1. `IsciJetonuListDto`-ya iki yeni sahə: `Sistemli` (`x.JetonTeyinati.Sistemli`,
   `JetonService.MapJeton`-da doldurulur) və `EffektivQalanSaat` (`decimal?`,
   yalnız Sistemli sətirlərdə dolu).
2. `JetonController.GetEmeliyyatlar` → yeni `EffektivQalanDoldurAsync` helper —
   siyahıdakı Sistemli sətirləri `QazanmaTarixi.Year`-ə görə qruplaşdırır (hər
   "36 Saat Hüququ {il}" öz ilinin icazə statistikasına baxmalıdır), hər il üçün
   **BİR DƏFƏ** `IIcazeService.GetIsciIzlemeAsync`-i çağırıb (`JetonSaatlariController`
   ilə EYNİ çağırış, nəticə `TesdiqSaat`) nəticəni `IsciId`-yə görə lüğətə yığır,
   sonra hər sətrə `EffektivQalanSaat = (QalanSaat ?? JetonSaatDeyeri) − İstifadə`
   yazır — `JetonSaatlariController`-in `IllikQaliqSaat` düsturu ilə BİT-Bİ-BİT
   eynidir, yeni düstur İCAD EDİLMƏDİ.
3. `hr-jeton.js`-in `hjQalanGoster`-i — `r.sistemli && r.effektivQalanSaat != null`
   olanda `effektivQalanSaat`-ı göstərir (tooltip-lə: "Hüquq (N saat) − bu ilin
   təsdiqlənmiş icazə saatı"), əks halda köhnə `qalanSaat` yolu dəyişmədən qalır
   (digər bütün jeton növləri — Platin/Qızıl/Gümüş/Bürünc/Qara — TOXUNULMADI).

⚠️ **Yalnız `JetonEmeliyyatlariGetirAsync`-i çağıran "Jeton İdarəetməsi" düzəldi
— `IsciAktivJetonlariniGetirAsync`-ə (işçinin öz "Cüzdanım" kart siyahısı)
TOXUNULMADI**, çünki o metod artıq `!x.JetonTeyinati.Sistemli` filtri ilə bu
jetonu TAMAMƏN kənarda saxlayır (işçi öz kartında "36 Saat Hüququ"nu
ümumiyyətlə mükafat kimi görmür) — orada göstəriləcək səhv rəqəm yoxdur.

#### İKİNCİ DALĞA — Formatlanmamış C# Decimal Cədvəli Pozdu (15.10.2026)

Düzəliş deploy olunandan dərhal sonra istifadəçi real ekranda QALAN sütununda
**"32.41666666666667 saat"** göstərildiyini bildirdi: *"siyahida seliqesizdiye,
hamisi ic ice"*. Səbəb: `EffektivQalanDoldurAsync`-dəki `huquq − istifade`
çıxarması (`decimal`, `TesdiqSaat`-dan gələn `double`-ın `(decimal)` cast-ı)
heç bir yuvarlaqlaşdırma olmadan **xam** DTO-ya yazılırdı, `hjQalanGoster`
isə onu `${eff}`-lə birbaşa çap edirdi — nəticə 15 onluq rəqəmli bir mətn
idi. `<td style="white-space:nowrap">` sütunu bu uzun mətnə görə həddindən
artıq genişlənir, cədvəlin qalan sütunları (Xərcləndi/Tarix/Səbəb/Status)
sıxışıb bir-birinin üstünə düşmüş görünürdü — "hamısı iç-içə" simptomunun
kök səbəbi elə BU rəqəmin uzunluğu idi, ayrıca CSS/layout bugu deyil.

**Düzəliş — `hr-jeton.js`, `hjQalanGoster`:** `effektivQalanSaat` indi
cədvəldəki DİGƏR bütün rəqəmlərlə (məs. `qalan/8` gün hesabı, sətir 159)
EYNİ naxışla formatlanır — `eff.toFixed(2).replace(/\.?0+$/, '')` — yəni
ən çox 2 onluq, artıq sıfırlar/nöqtə atılır (`32.42`, tam ədəddirsə `32`).
Bu, "İşçi Jeton Saatları"nın özünün Razor-dakı `"0.##"` formatı ilə EYNİ
NƏTİCƏNİ verir (fərq yalnız onluq ayırıcıda — JS `.` yazır, Razor az-AZ
mədəniyyətində `,` — hər ikisi öz kontekstində doğrudur, bax "Razor → CSS/JS
Rəqəm — Mədəniyyət" bölməsi: problem YALNIZ maşın-oxuyan tərəfdə yaranır,
burada hər iki tərəf artıq İNSAN-oxuyan mətndir).

**Qayda: serverdən gələn xam `decimal`/`double` DƏYƏRİ birbaşa `${...}`
şablon literalına YAZMA** — xüsusən çıxarma/bölmə kimi əməliyyatın nəticəsi
uzun onluq qalıqla gələ bilər. Cədvəl kontekstində bu, sadəcə "çirkin rəqəm"
DEYİL — sabit en olmayan sütunları genişləndirib bütün cədvəl sətrini
pozur. Yeni bir rəqəm sahəsi JS-dən render edəndə, HƏMİŞƏ həmin cədvəldəki
DİGƏR rəqəmlərin işlətdiyi formatlama funksiyasını (burada: `toFixed(2)` +
trailing-zero təmizləmə) təkrar istifadə et, yeni, formatlanmamış bir
çıxışı birbaşa buraxma.

#### ÜÇÜNCÜ DALĞA — Rəqəm Düzəlsə Belə Cədvəlin Özündə Qalan Köhnə Qüsurlar (15.10.2026)

Rəqəm formatlandıqdan sonra istifadəçi davam etdi: *"cedvel ozude seliqesiz
deyil?"* — haqlı idi, cədvəlin özündə uzun onluq rəqəmdən ASILI OLMAYAN
**iki köhnə qüsur** var idi:

1. **`vertical-align` HEÇ YERDƏ təyin olunmamışdı** — "Xərcləndi" sütunu
   (ikon + iki sətir: "⏱ İcazə" / tarix+saat / "xərcləndi: …") qonşu tək-
   sətirli sütunlardan (Dəyər, Qalan, Status) HÜNDÜR idi, brauzerin default
   `vertical-align` davranışı sətirləri şaquli FƏRQLİ bağlayırdı — nəticə
   "hamısı iç-içə" təəssüratı idi, heç bir CSS xətası olmadan.
2. **Rəqəm sütunları (Dəyər, Qalan) mətn kimi SOLA düzülürdü** — rəqəmlər
   üçün qeyri-standart görünüş, "Qalan" sütunu ilə yanaşı "Xərcləndi"
   sütunu arasında vizual ayrım zəifləyirdi.

**Düzəliş — `hr-jeton.css`:** `.hj-table td { vertical-align: middle; }`
(bütün 3 cədvələ — Əməliyyatlar/Tarixçə/Redim — tətbiq olunur, `.hj-table`
paylaşılan sinifdir) + `#hjEmeliyyatlarTable td:nth-child(3,4) { text-align:
right; }` (yalnız Dəyər/Qalan, yalnız bu cədvəl) + `.hj-td-sebeb` sinfinə
(markup-da artıq var idi, CSS-i YOX idi) `max-width:260px` + söz-sındırma.

⚠️ **`table-layout:fixed` + sərt faiz enlər QƏSDƏN İŞLƏDİLMƏDİ** — ilk
cəhddə bu yanaşma sınanmış, amma committə çatmadan geri götürülmüşdü: dar
faiz enli sütun (məs. Dəyər/Qalan `width:9%`) `white-space:nowrap` (bu
sütunlar artıq JS-dən inline nowrap alır) ilə birləşəndə, uzun məzmun
sütunu KƏSMİR, əvəzinə QONŞU SÜTUNUN ÜSTÜNƏ DAŞIR — bu, elə "32.41666…"
rəqəminin yaratdığı orijinal şikayətin başqa bir forması olardı, indi sabit
faiz enlə təkrar yaradılardı. `table-layout:auto` (default) sütunu real
məzmuna görə genişləndirdiyi üçün bu risk yoxdur — əvəzinə yalnız
`vertical-align` + `text-align` + Səbəb-ə `max-width` ilə kifayətləndi.

**Qayda: "cədvəl səliqəsizdir" şikayətində, bir pafoloji dəyəri (uzun
rəqəm) düzəltdikdən SONRA da əl ilə yoxla ki, cədvəlin ÖZÜNDƏ (vertical-
align, text-align, sərhədsiz sərbəst-mətn sütunu) ayrıca, pafoloji dəyərdən
asılı olmayan köhnə qüsurlar qalmayıb.** Bir simptomun bir səbəbi
düzəldilməsi "cədvəl indi səliqəlidir" demək deyil — istifadəçinin "cədvəlin
özü" sualı dəqiq bunu aşkar etdi.

**Qayda: eyni rəqəmin İKİ yazıcısı/oxuyucusu olanda (burada: bir sütun İKİ
AYRI controller-dən fərqli düsturla hesablanır), bir yerdə düzəliş edərkən
HƏR ÇAĞIRIŞ YERİNİ sadala, təkcə ƏN SON toxunulan səhifəni yox.** 14.10.2026-dakı
"HR/JetonSaatlari" düzəlişi "36 Saat Hüququ"nun yalnız BİR göstərilmə yolunu
(yeni, xüsusi hesabat səhifəsini) düzəltdi — "Jeton İdarəetməsi" həmin vaxt
MÖVCUD İDİ və eyni jetonu göstərirdi, amma ora toxunulmadı, çünki o zaman
istifadəçinin sualı konkret o səhifəyə aid deyildi. Nəticə: iki səhifə eyni
jeton üçün eyni real tarixdə fərqli rəqəm göstərməyə davam etdi, 15.10.2026-da
istifadəçi bunu fərq edənə qədər.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı, DI dairəsi əl ilə izlənildi — `IcazeService` konstruktoru
yoxlanıb `IJetonService` daşıdığı təsdiqləndi). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real mühitdə yoxlamalıdır: Əjdər Dəmirov üçün
"Jeton İdarəetməsi"ndəki "36 Saat Hüququ 2026" QALAN sütununun artıq
"İşçi Jeton Saatları"ndəki Qalıb (32,42) ilə DƏQİQ üst-üstə düşdüyünü,
digər (Platin/Qızıl/Gümüş/Bürünc/Qara) jeton sətirlərinin əvvəlki kimi
qaldığını.

#### DÖRDÜNCÜ DALĞA — Səbəb Sütunu Dar Qalırdı + Xətsiz Cədvəl (15.10.2026)

İstifadəçi real ekranlarda (iki ayrı ekran-görüntüsü) iki əlavə qüsur göstərdi:
*"Səbəb sütununu genişləndirsə cədvəl səliqəyə düşər"* + *"2-ci şəkildə də eyni
qayda ilə, bir az çərçivə qat cədvələ, xətlər görünəndə daha səliqəli olur,
işçi adını da genişləndir"*.

1. **`.hj-td-sebeb`-in köhnə `max-width:260px`-i DAR idi** — uzun səbəb mətni
   (məs. "Qeyri iş vaxtı 12.10.2026 tarixində saat 00:40-dək server otağının
   təmiri ilə əlaqədar işə çıxdığı üçün.") hər 2-3 sözdən sonra sətir keçib
   6-7 sətirlik nazik sütun yaradırdı — bu, qonşu tək-sətirli sütunlarla
   (İşçi/Jeton/Status) şaquli ən böyük uyğunsuzluğu elə BU sütun yaradırdı.
   `max-width` 420px-ə qaldırıldı, əlavə `min-width:280px` qoyuldu — orta
   uzunluqlu səbəb indi tək (və ya iki) sətirdə qalır, yalnız HƏDSİZ uzun
   mətn (yüzlərlə simvol) hələ də kəsilib bükülür.
2. **"İşçi" sütunu (nth-child(1))** eyni səbəbdən `min-width:170px` aldı —
   ad-soyad (məs. "Məhəmməd Bəkirov") əvvəl sabit eni olmayan sütunda iki
   sözə bölünüb iki sətirə düşə bilirdi.
3. **Grid/çərçivə görünüşü** — `.hj-table th`/`.hj-table td`-nin yalnız
   `border-bottom` yazan köhnə qaydası **tam `border: 1px solid`**-ə
   dəyişdirildi (üç cədvələ — Əməliyyatlar/Tarixçə/Redim — də tətbiq olunur,
   `.hj-table` paylaşılan sinifdir). Nəticə: hər xana öz xətti ilə ayrılır —
   istifadəçinin istədiyi "xətlər görünəndə daha səliqəli olur" effekti.
   Köhnə `tbody tr:last-child td { border-bottom: none }` qaydası artıq
   mənasız olduğu üçün silindi (indi HƏR sətrin öz tam sərhədi var).

⚠️ **`table-layout:fixed` YENƏ DƏ İŞLƏDİLMƏDİ** — `min-width` sütunun minimum
enini qaldırır, amma `auto` düzüm hələ də lazım gələndə ondan da genişlənə
bilir (fixed-width kimi sərt kəsmə yoxdur) — ƏVVƏLKİ DALĞADA (yuxarı) izah
edilən "dar faiz en + nowrap = qonşu sütuna daşma" riski bu üsulda yoxdur,
çünki `min-width` yalnız AŞAĞI hədd qoyur, YUXARI hədd qoymur.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (CSS mötərizə balansı, mojibake skanı — hər ikisi təmiz).
İstifadəçi real brauzerdə yoxlamalıdır: "Səbəb" sütununun artıq normal
enli olduğunu, "İşçi" adlarının tək sətirdə qaldığını, cədvəl xanalarının
indi görünən xətlərlə (grid) ayrıldığını — həm "Bütün işçilər" (tam siyahı),
həm filtrlənmiş (tək işçi) görünüşdə.

#### BEŞİNCİ DALĞA — `overflow:hidden` Sütunları KƏSİRDİ, Scroll YOX İDİ (15.10.2026, KRİTİK)

Dördüncü dalğanın (yuxarı) `min-width` düzəlişi deploy olunandan dərhal sonra
istifadəçi real ekranda cədvəlin **sağ kənarının kəsildiyini** göstərdi —
"STATUS" sütunu və silmə düyməsi ekrandan çıxmışdı, heç bir üfüqi scrollbar
yox idi: *"bu nedir? cox seliqesiz. xerclendi sutununuda genislendir"*.

**Kök səbəb:** `.hj-table-wrap`-in köhnə `overflow: hidden` qaydası —
dördüncü dalğada İşçi/Səbəb sütunlarına əlavə olunan `min-width` dəyərləri
cədvəlin ÜMUMİ enini konteynerdən GENİŞ etdi, amma `overflow:hidden`
daşan hissəni scroll ETMİR, sadəcə **görünməz edib kəsir**. Nəticə: Status
sütunu və əməliyyat düyməsi fiziki olaraq mövcud idi, amma konteynerin
kənarından kənarda, görünməz qalırdı.

**Düzəliş:** `overflow: hidden` → `overflow-x: auto` — cədvəl konteynerdən
genişdirsə indi **üfüqi scrollbar** çıxır, heç bir sütun kəsilmir. Bu, üç
cədvəlin (Əməliyyatlar/Tarixçə/Redim) HAMISINA aiddir, çünki `.hj-table-wrap`
paylaşılan sinifdir.

**Əlavə tələb — "Xərcləndi" sütunu da genişləndirildi** (`nth-child(5)`,
`min-width:210px`) — bu sütun multi-sətir məzmun daşıyır (ikon+növ, icazə
tarix-saat aralığı, "xərcləndi: TARİX" qeydi) və dar enlə hər sətir öz
daxilində də sıxışırdı.

**Qayda: `min-width` ilə sütun genişləndirəndə, konteynerin `overflow`
davranışını da yoxla.** `overflow:hidden` olan bir konteynerə `min-width`
əlavə etmək cədvəli konteynerdən genişlədə bilər — bu halda `hidden` sütunları
SCROLL ETMƏK ƏVƏZİNƏ KƏSİR, heç bir xəta/xəbərdarlıq olmadan. Konteyner
`overflow-x:auto` olmalıdır ki, genişlənmə scrollbar versin, kəsilmə yox.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (CSS mötərizə balansı). İstifadəçi real brauzerdə yoxlamalıdır:
cədvəlin sağ kənarının (Status + silmə düyməsi) artıq görünən qaldığını
(lazım gələrsə üfüqi scroll ilə), "Xərcləndi" sütununun daha geniş/səliqəli
göründüyünü.

#### ALTINCI DALĞA — "Overflow-x:auto" HƏLL DEYİLMİŞ, Status Sütunu "İTDİ" (15.10.2026, KRİTİK)

Beşinci dalğanın `overflow-x:auto` düzəlişi deploy olunandan sonra istifadəçi
real ekranda STATUS sütununun və silmə düyməsinin ÜMUMİYYƏTLƏ görünmədiyini
bildirdi: *"sağda hansı sütun var idi hanı o? status sütunu. çox bərbad
hazırlayırsan"*.

**Kök səbəb — "overflow-x:auto" texniki cəhətdən düz idi, amma PRAKTİKİ
İSTİFADƏ EDİLƏ BİLMƏZ idi.** Dördüncü dalğada İşçi/Xərcləndi/Səbəb
sütunlarına qoyulan `min-width` cəmi (170+210+280-420px) cədvəlin ümumi
enini `.hj-page {max-width:1200px}` konteynerindən (~1140px istifadə olunan
sahə) KEÇİRDİ. `overflow-x:auto` bunu SCROLL EDİLƏ bilən edir, AMMA həmin
scrollbar **cədvəlin ƏN ALTINDA** (bütün sətirlərdən SONRA) çıxır — çoxsaylı
sətirli (10+ sətir) bir cədvəldə bu, səhifəni lap sona qədər aşağı
sürüşdürmədən GÖRÜNMÜR/ƏLÇATAN DEYİL. İstifadəçi üçün nəticə — "Status
sütunu yoxa çıxdı" idi, texniki olaraq "scroll edilə bilən, amma tapıla
bilməyən".

**Düzgün həll — GENİŞLƏNMƏ ƏVƏZİNƏ MƏHDUDLAŞDIRMA:**
- `İşçi`/`Xərcləndi` sütunlarına qoyulan `min-width` TAM SİLİNDİ — auto
  düzüm bu sütunları onsuz da kifayət qədər sığdırırdı, overflow-ə onların
  PAYI həlledici deyildi, silinməsi zərərsizdir.
- `Səbəb` sütunu artıq GENİŞLƏNMİR — `-webkit-line-clamp: 2` ilə **İKİ
  SƏTİRƏ MƏHDUDLAŞDIRILIR**, uzun mətn "…" ilə kəsilir, `max-width:320px`
  (əvvəlki 420px-dən azaldıldı). Tam mətn itmir — `title` atributunda qalır
  (brauzerin öz hover-tooltip-i, `hr-jeton.js`-ə yeni `hjEsc()` helper-i ilə
  HTML-qaçırılmış şəkildə yazılır).
- `.hj-table-wrap`-in `overflow-x:auto`-su (beşinci dalğa) SİLİNMƏDİ — ehtiyat
  şəbəkəsi kimi qalır, amma artıq **adi halda işə düşməməlidir**, çünki
  cədvəl bir daha konteynerdən enli olmur.

**Qayda: "sütun çox sətirə bölünür, çirkin görünür" şikayətini HƏMİŞƏ
`min-width` ilə GENİŞLƏNDİRƏRƏK həll etmə** — konteyner sabit/məhdud enlidirsə
(`max-width` olan səhifə kimi), genişlənmə digər (xüsusən SAĞDAKI) sütunları
konteynerdən kənara itələyə bilər, nəticədə `overflow-x:auto` olsa belə həmin
sütunlar PRAKTİKİ olaraq itmiş görünər (scrollbar uzun cədvəldə əlçatmazdır).
**`-webkit-line-clamp` (mətni N sətirə kəsib "…" qoyma, tam mətni `title`-da
saxlama) sütunun ENİNƏ TOXUNMADAN eyni "çox sətirli çirkinlik" problemini
həll edir** — dar/məhdud konteynerlərdə bu, TƏHLÜKƏSİZ defolt seçimdir, geniş
konteyner isbat olunmayana qədər `min-width` ilə genişləndirməyə üstünlük
vermə.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(CSS mötərizə balansı, `node -c` ilə JS sintaksisi). İstifadəçi real brauzerdə
yoxlamalıdır: Status sütunu və silmə düyməsinin indi HEÇ bir scroll lazım
olmadan görünən qaldığını, Səbəb sütununun uzun mətndə iki sətirdən sonra
"…" ilə kəsildiyini və üzərinə hover edəndə tooltip-də tam mətnin çıxdığını.

#### YEDDİNCİ DALĞA — Sətir/Sütun Seçimi (GPT-mockup-un YALNIZ bu hissəsi, 15.10.2026)

İstifadəçi ChatGPT-nin hazırladığı bir "Jeton Analitika" mockup şəkli göstərdi
(KPI kartları, qrafik, avatarlı sətirlər, sağda "Son əməliyyatlar" paneli) və
*"gpt nin versiyasida yaxsidir mence, biraz cedvel goruntusunude yaratsaq,
sutun setior secilse dahada yaxsi olar mence"* dedi. Tələb qeyri-müəyyən idi
— həm əhatə (kiçik əlavə vs tam redizayn), həm hədəf (bu sessiyada işlənən
"Jeton İdarəetməsi" vs mockup-un öz başlığının adlandırdığı, tamamilə ayrı,
artıq mövcud "Jeton Analitika" səhifəsi) aydın deyildi. `AskUserQuestion` ilə
dəqiqləşdirildi, cavablar: **"Yalnız cədvələ sətir/sütun seçimi"** (KPI
kartı/qrafik/avatar YOX) + **"Jeton İdarəetməsi"** (Jeton Analitika YOX).

**Tətbiq — generik, hər üç cədvələ (Əməliyyatlar/Sorğu Tarixçəsi/Xərcləmə
Sorğuları):** `hr-jeton.js`-ə `hjInitTableSelection(tableId)` əlavə olundu —
başlıq xanasına klik bütün sütunu, sətrə klik (əməliyyat düyməsindən kənar
hər yerə) bütün sətri vurğulayır, hər ikisi **tək seçimdir** (təkrar klik
ləğv edir). Delegasiya `tbody`-nin ÖZÜNƏ (DOM elementinin, `innerHTML`-ə yox)
bir dəfə (`DOMContentLoaded`-da, `table.dataset.hjSelInit` qoruyucusu ilə)
qoşulur — reload-lar (filtr/"Yenilə") `tbody.innerHTML`-i dəyişir, konteyner
elementin özü dəyişmədiyi üçün delegasiya hər reload-dan SONRA da işləməyə
davam edir, yeniden qoşmağa ehtiyac yoxdur.

**Sütun vurğusu reload-da avtomatik İTİRDİYİ üçün** (`tbody.innerHTML`
dəyişəndə köhnə xanalar silinir, üzərlərindəki `.hj-col-selected` sinfi
onlarla birlikdə gedir) hər üç yükləmə funksiyası (`hjLoadEmeliyyatlar`,
`hjLoadTarixce`, `hjLoadRedimler`) öz uğurlu render-indən DƏRHAL sonra
`hjApplyColSelect(table)`-i çağırır — seçim yoxdursa (`dataset.hjSelCol`
boşdur) heç nə etmir, varsa cari sütunu təzə sətirlərə yenidən tətbiq edir.
Sətir seçimi isə RELOAD-DA QƏSDƏN SIFIRLANIR (yeni sətirlər köhnəni əvəz
etdiyi üçün bu, təbii və gözlənilən davranışdır — ayrıca "bərpa et" məntiqi
yazılmadı).

**Başlıqsız sütun (son, əməliyyat düyməsi) seçilə bilmir** — `hjInitTableSelection`
boş mətnli `<th>`-ə klik handler-i qoşmur (`if (!th.textContent.trim())
return;`), çünki orada vurğulanacaq məzmun yoxdur.

**CSS rəngi hover-dən (mövcud, boz) fərqləndirmək üçün `--hj-primary`
(bənövşəyi) tonunda, `color-mix` ilə** — sətir+sütun kəsişməsi daha tündü
bir ton alır ki, "hansı xana ikisinə də aiddir" aydın olsun.

**Qayda:** bir cədvəl üçün statik (reload-da dəyişməyən) DOM elementlərinə
(buradakı `<thead>`/`<tbody>` konteynerləri) bir dəfə qoşulan event
delegasiyası, `innerHTML` ilə edilən hər reload-dan ÖZBAŞINA sağ çıxır —
yenidən qoşmağa ehtiyac yoxdur. Yalnız reload-un SİLDİYİ vəziyyəti (bura:
sütun vurğusunun CSS sinfi) əl ilə bərpa etmək lazımdır, delegasiyanın
özünü yox.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (`node -c` ilə JS sintaksisi, CSS mötərizə balansı,
mojibake skanı — hər ikisi təmiz). İstifadəçi real brauzerdə yoxlamalıdır:
hər üç cədvəldə başlığa klikin sütunu, sətrə klikin sətri vurğuladığını,
təkrar klikin ləğv etdiyini, "Yenilə"/filtr dəyişəndə sütun vurğusunun
qaldığını (sətir vurğusunun isə təbii olaraq sıfırlandığını), və əməliyyat
düyməsinə (məs. "Ləğv et"/"Bax") klikin seçimi TƏTİKLƏMƏDİYİNİ.

## İmtiyazlarım — İşçiyə Verilmiş Fərdi İstisna (07.10.2026)

İstifadəçi tələbi: *"işçi öz səhifəsində ona olunmuş imtiyazları görməsi. misal.
əlil işçi qaydamıza həftənin 5-ci günü nahardan sonra gedə bilər."* Müzakirədən
sonra qərarlar:

- **YALNIZ GÖSTƏRİŞ** (istifadəçi qərarı: "Yalnız görüntü") — Davamiyyət
  statusuna, hesablamaya, heç bir avtomatik qaydaya TƏSİR ETMİR. Rəhbər/HR
  qeydi oxuyub öz qərarını özü verir. Bu, "Davamiyyət — Ezamiyyət Statusa
  TOXUNAN BÜTÜN YERLƏR" kimi hesablama zənciri YARATMIR — qəsdən.
- **Strukturlu sahələr**, sərbəst mətn YOX — `IsciImtiyaz` entity-si: `Baslik`,
  `HeftaGunu` (nullable `DayOfWeek`, null = hər gün), `BaslamaSaati`/`BitisSaati`
  (nullable `TimeSpan`, hər ikisi null = bütün gün), `Aciqlama`, `BaslamaTarixi`,
  `BitmeTarixi` (null = müddətsiz), `Aktivdir`. Bir işçinin bir neçə sətri ola
  bilər — mürəkkəb çox-günlü seçim widget-i YOXDUR ("üç oxşar sətir premature
  abstraction-dan yaxşıdır").
- **HR idarəetməsi**: İşçi Profili (`/HR/Isci/Detail/{id}`) səhifəsinə YENİ
  sekmə ("İmtiyazlar") — ayrıca səhifə YOX, mövcud tab pattern-ə (`isci-detail-tab`
  / `isci-tab-panel`, `Views/Isci/Detail.cshtml`-dəki skript) uyğun.
- **İşçi görüntüsü**: Dashboard-da kiçik kart (`Areas/User/Views/Dashboard/Index.cshtml`)
  — YALNIZ aktiv (bugün qüvvədə) imtiyazı olan işçidə görünür, heç kimdə yoxdursa
  kart ümumiyyətlə çıxmır.

**Fayllar:** `IsciImtiyaz` entity (`FinNex.Domain/Entities/HR/`), migration
`20261007120000_IsciImtiyaz.cs` (yalnız `CreateTable` — `InsertData` YOXDUR, seed
lazım deyil, siyahı boş başlayır), `IIsciImtiyazService`/`IsciImtiyazService`
(Clean Architecture: Controller yalnız servisi inject edir), DTO-lar
`FinNex.Application/DTOs/HR/Imtiyaz/`.

**İki oxuyucu, TƏK mənbə:** `IsciUzreHamisiniGetirAsync` (HR tab — hamısı,
aktiv/deaktiv) və `AktivImtiyazlarAsync` (Dashboard kartı — yalnız
`Aktivdir && BaslamaTarixi<=bugün && (BitmeTarixi==null || BitmeTarixi>=bugün)`)
eyni entity-ni fərqli filtrlə oxuyur — iki ayrı yazıcı yoxdur, səssiz uyğunsuzluq
riski yoxdur (bax "Bir Elementə İKİ YAZICI" qaydası — burada əksinə, təhlükəsiz
"iki oxuyucu bir mənbədən" formasıdır).

`DashboardService` üçüncü asılılıq kimi `IIsciImtiyazService` inject edir —
`IJetonService`-in "36 Saat Hüququ" üçün etdiyi ("başqa servisdən DTO-ya bir
sahə doldur") naxışın təkrarıdır.

⚠️ **Silinmir, deaktiv edilir** (`Aktivdir=false`) — `MasinMuddetService`/
`IsciGuzest` ilə eyni konvensiya, tarixçə qalsın. Server tərəfdə validasiya:
başlıq mütləq, saat aralığında bitiş > başlanğıc, bitmə tarixi başlamadan əvvəl
ola bilməz — "nə yazılacağını əvvəlcədən göstər" (SQL) qaydasına bənzər, yarımçıq
qeyd bazaya düşmür.

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

### Eyni Tələ, Fərqli Ekran — "Əmək Haqqı Tarixçəsi" Panelində VM 98.2.1 (08.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi "Əmək haqqı tarixçəsi" (User) səhifəsindəki "Hesablama detalı"
panelini göstərib soruşdu: *"soldaki rəqəmlər çaşdırıcıdır axı, sən gəlir
2181.36 yazmısan, məzuniyyət gəlirsən çıxıntısız, bu çaşdırıcı deyil?"*

Riyaziyyatı yoxlandı: `Gəlirlər CƏMİ (2181,36) − Tutulmalar CƏMİ (394,57) =
1786,79`, amma real Net **1773,51** idi — fərq **dəqiq 13,28** idi, yəni elə
"VM 98.2.1 Gəlirləri" sətrinin məbləği. Kök səbəb tapıldı:
`MaasHesablamaService.FerdiHesabla`-nın 7.2-ci addımında bu sətir
`Tip = "gelir"` yazılmışdı, halbuki **öz Izah mətni** açıq deyir: "brüt/net-ə
DAXİL DEYİL". `maas-tarixce.js`-dəki `section()` funksiyası `tip === 'gelir'`
olan HƏR sətri "GƏLİRLƏR" panelinin CƏMİsinə toplayır — kodun özü ilə mətnin
dediyi ZİDDİYYƏT İDİ, eynilə yuxarıdakı "Yekun Zolaq" bölməsindəki tələ, sadəcə
**fərqli ekranda** (Toplu Maaş footerı yox, fərdi işçinin öz tarixçə paneli).

**Niyə bu, "Məzuniyyət Ödənişi" / "Məzuniyyət Kəsintisi" cütlüyündən FƏRQLİDİR:**
o ikisi (Gəlirlərdə +268,08, Tutulmalarda −90,48) riyazi cəhətdən DÜZGÜN idi —
`Gross = Əsas Əməkhaqqı + Məzuniyyət Ödənişi − Məzuniyyət Kəsintisi` və bu,
qrafikdəki real "Gross Maaş" rəqəmi (2 077,60) ilə tam tutuşur (1900+268,08−90,48).
Yəni "gəlir bir sütunda, onun kəsintisi başqa sütunda" özü **problem deyil** —
problem YALNIZ VM 98.2.1-in **özü** nağdsız olduğu halda "gelir" tipinə
yazılıb CƏMİyə düşməsi idi, ona uyğun heç bir "kəsinti" cütü yoxdur.

**Düzəliş:** `Tip = "gelir"` → `Tip = "melumati"` (bir sətir). Eyni fayldakı
OXŞAR nağdsız/informativ addımların (VM 98.2.1-in digər 4 bazaya təsiri,
7 fərqli yerdə) artıq hamısı `"melumati"` yazılıb — 7.2-ci addım elə bu
konvensiyanın özündə unudulmuş istisna idi. `melumati` tipli sətirlər
`maas-tarixce.js`-də `hideTotal=true` olan ayrıca "Məlumat üçün" bölməsinə
düşür (CƏMİ göstərilmir) — HR-ın `Maas/Detal.cshtml` audit cədvəlindəki
rəng/işarə (`TipBadge`/`MeblegPrefix`) də eyni dəyişiklikdən avtomatik
düzəldi (ikisi də eyni `HesablamaIzahi` JSON-undan, eyni `Tip` sahəsini oxuyur).

⚠️ **Yalnız GÖSTƏRİŞ düzəlişidir — real hesablamaya SIFIR təsir.** `BrutMebleg`/
`NetMebleg` heç vaxt bu `izahatlar` siyahısından (və ya onun `Tip`-indən)
hesablanmır — ayrıca dəyişənlərlə hesablanıb entity-yə yazılır,
`HesablamaIzahi` sadəcə audit/göstəriş üçün JSON-dur. Yoxlandı: kodda
`izahatlar.Where(x => x.Tip == ...)`-dan brüt/net çıxaran HEÇ BİR yer yoxdur.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — `HesablamaIzahi` hər ay `FerdiHesabla`
çağırılanda JSON kimi DONDURULUB yazılır (bax "Kəsilən Saat CANLI Kataloq
Dəyərindən Asılı İdi" bölməsindəki eyni "dondurma" prinsipi). Keçmiş aylarda
VM 98.2.1 olan qeydlər hələ köhnə `"Tip":"gelir"` ilə saxlanılıb və "Əmək
haqqı tarixçəsi" panelində həmin aylar üçün CƏMİ YENƏ DƏ bir az yüksək
görünəcək (fərq VM9821 məbləği qədərdir) — yalnız BUNDAN SONRA hesablanan
aylar düzgün göstərəcək. Keçmişi düzəltmək maaşı YENİDƏN HESABLAMAĞI (və ya
saxlanmış JSON-u əl ilə patch etməyi) tələb edir — bu sessiyada edilmədi,
istifadəçi tələb etməyib.

### Detal Panelində "Net" Qabaqcadan Məzuniyyət Pulunu Göstərmirdi (08.10.2026)

İstifadəçi eyni "Əmək haqqı tarixçəsi" səhifəsində ikinci bir uyğunsuzluq
tapdı: "İyl 2026" üçün qrafikdəki "Net Maaş" **2 359,57** idi, amma həmin aya
klikləyəndə açılan detal panelinin footer-indəki "Net (ələ keçən)" **1 111,29**
göstərirdi — sual: *"aldığı məzuniyyət pulunu nəzərə almırsan detalda?"*

Araşdırma göstərdi ki, bu **hesablama səhvi deyil, İKİ EKRANIN qəsdən fərqli
əhatəsi idi** — "Qabaqcadan Məzuniyyət" bölməsindəki dual-attribution dizaynının
təbii nəticəsi:

- `GetTarixceData` (qrafik) — `MezQabaqAylikMapAsync` ilə qabaqcadan ödənilmiş
  məzuniyyətin brüt/net-ini **ÖDƏNİLMƏ ayına** əlavə edir ki, "başlıqda ayın TAM
  gross/net-i görünsün" (bu, kodda əvvəldən şərh olunub).
  **1 321,30 + 1 477,25 = 2 798,55** (Gross), **1 111,29 + 1 248,28 = 2 359,57** (Net).
- `GetDetay` (klik → detal paneli) **HƏMİN ƏLAVƏNİ ETMİRDİ** — `maas.BrutMebleg`/
  `NetMebleg` yalnız BU AYIN ÖZ hesablama nüvəsini qaytarırdı; qabaqcadan
  ödənilmiş məzuniyyət yalnız "Məlumat üçün" bölməsində mətnlə görünürdü
  (`Tip="melumati"`, CƏMİyə düşmür — `MaasHesablamaService.cs:616/638/661/673`-dəki
  4 sətirdən biri, "Mezuniyyet (qabaqcadan ödənildi)").

**Niyə sadəcə `GetDetay`-ə mez-məbləğini əlavə etmək KİFAYƏT ETMƏZDİ:** panelin
"Tutulmalar" sətri `res.brut - res.net` kimi TƏYİN OLUNUR (müstəqil cəmlənmir),
ona görə Gross/Net-ə birgə əlavə etsək "Yekun Zolaq"ın özü bağlanardı — AMMA
itemized "Gəlirlər"/"Tutulmalar" bölmələrinin öz "Cəmi"si (bu ayın addımlarından
hesablanır) yeni Net ilə bağlanmazdı, çünki mezin öz vergi sətirləri BAŞQA ayın
`HesablamaIzahi`-sində qalıb — **yeni, daha gizli bir "Yekun Zolaq" uyğunsuzluğu
yaradardı**, VM9821-in elə indi düzəldilmiş növündən.

**Həll — birləşdirmədən, AYRICA sətir kimi göstər:** `GetDetay` indi `brut`/`net`-i
TOXUNULMADAN qaytarır (itemized bölmələrlə bağlı qalır) və **əlavə sahə**
qaytarır: `mezuniyyetBrut`/`mezuniyyetNet` (mənbə — eyni `MezQabaqAylikMapAsync`,
bu dəfə tək (il,ay) açarı üçün). `maas-tarixce.js` bu sahə `>0`-dırsa footer-ə
bağlanmır — ÜSTÜNDƏN **iki yeni sətir** əlavə edir: "+ Məzuniyyət pulu (bu ay
ayrıca ödənilib)" və aşağısında **ayrıca, vurğulanmış** "CƏMİ bu ay əldə edilən"
sətri (`res.net + mezNet` — elə qrafikin göstərdiyi rəqəm). Mez olmadıqda
("Net (ələ keçən)" etiketi dəyişmir) panel köhnə kimi qalır — yalnız qabaqcadan
məzuniyyəti olan aylarda yeni sətirlər görünür.

**Qayda:** eyni ayın müxtəlif KPI-ları (qrafik başlığı vs detal paneli) müxtəlif
MƏQSƏD üçün fərqli əhatə seçə bilər (burada: "tam ayın pulu" vs "bu hesablama
nüvəsinin öz rəqəmi") — bu özü səhv deyil, AMMA iki ekran arasında rəqəm fərqi
varsa, böyük ekran kiçiyə **görünən, etiketli bir körpü** ilə bağlanmalıdır.
Sadəcə kiçik ədədi böyüyə "sükutla" tərəziləmək (mezi birbaşa Net-ə qatmaq)
yeni bir gizli "Yekun Zolaq" tələsinə aparar — bax yuxarıdakı VM9821 bölməsi.

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

## Kassa — Valyuta Mübadiləsi (Exchange), BMI frmExchange Köçürülməsi (09.10.2026)

BMI-nin (Visual Studio, legacy WinForms, `/home/user/BMI`) `Kassa/frmExchange` +
`ManagerApproval/frmExchangeTesdiq` modulu FinNex-ə köçürüldü. Kassanın günlük
nağd/qeyri-nağd USD/AVRO/IRR/AED/RUB alış-satış kursu + rəhbər təsdiqi.

**Oracle-a YAZI qadağan olduğu üçün (yuxarı bax "Oracle Verilənlər Bazası —
KRİTİK QAYDA") bu modul tamamilə FinNex-in öz SQL Server bazasındadır** —
BMI-nin Oracle `bmi_kassa_kurs` cədvəli ilə heç bir əlaqəsi yoxdur, oxunmur da.

### Qərarlar (istifadəçi ilə birbaşa müzakirə, 09.10.2026)

- **Departament: Kassa** — yeni Area (`Emeliyyat`/`Avtopark` ilə eyni səviyyədə),
  sidebar-da ayrıca bölmə, top-nav dropdown-da "Kassa" sətri.
- **Giriş (kim yaza bilər):** mövcud **`RoleNames.Kassa`** Identity rolu —
  Avtopark → Açar jurnalı (`Areas/Avtopark/Controllers/KassaController.cs`) ilə
  **EYNİ rol**. İlk təklif `UserDepartment` idi, amma kodda artıq "kassa işçisi"
  üçün işlənən rol tapıldıqdan sonra istifadəçi bunu seçdi — eyni insanlar iki
  fərqli admin ekranından idarə olunmasın deyə.
- **Təsdiq (kim təsdiqləyə bilər):** admin-idarəli siyahı, **`KreditBaxanIsci`**
  ilə EYNİ naxış (rolsuz, sadə "aktiv dövr" siyahısı) — `KassaTesdiqEdici`.
  İstifadəçi: *"kassa yazır, rəhbər təsdiqləyir və ya imtina verir — belə
  işləyirdi sistem"* → **tək təsdiqedici kifayətdir**, Kredit Komitəsi
  (`KomiteUzvu`) kimi kvorum/çoxluq YOXDUR.

### BMI-də tapılan 3 bug — FinNex-də TƏKRARLANMIR

1. **Təsdiq/imtina qlobal idi** — BMI-nin `UPDATE bmi_kassa_kurs SET APPROVAL=...
   WHERE APPROVAL='gözləmədə'` sorğusu tarixə/qrupa bağlı deyildi, bütün
   gözləyən sətirləri vururdu. FinNex-də status konkret `KassaKursBeyannamesi`
   sətrinə bağlıdır (`TesdiqEtAsync(beyannameId, ...)`), qlobal UPDATE yoxdur.
2. **Səssiz no-op + yalan "uğurlu" mesajı** — BMI-nin `UpdateExchange`-i yalnız
   `APPROVAL='imtina'` olan sətri yeniləyirdi, "gözləmədə" olanda 0 sətir
   dəyişirdi, amma yoxlamadan "uğurlu" mesajı göstərirdi. FinNex-də
   `SaxlaAsync` əvvəlcə statusu yoxlayır, "gözləyir"/"təsdiqləndi" olanda
   **açıq rədd** edir (`Result.Fail`), səssiz keçmir.
3. **Heç bir giriş icazəsi yoxdur idi** — BMI-nin `frmExchangeTesdiq`-i
   `GetUserManagmenet`-i (admin siyahısı) heç vaxt çağırmırdı, menyuya çıxışı
   olan İSTƏNİLƏN adam təsdiqləyə bilirdi. FinNex-də hər təsdiq/imtina action-ı
   `IKassaTesdiqEdiciService.TesdiqEdeBilerMiAsync` yoxlayır.

### Struktur

`FinNex.Domain/Entities/Kassa/` — `KassaKursBeyannamesi` (gün başına BİR sətir:
Tarix, İcraçıIsciId, Status, TəsdiqEdənIsciId, TəsdiqTarixi, İmtinaSəbəbi),
`KassaKursSetri` (beyannaməyə bağlı, valyuta başına BİR sətir — BMI-nin "yastı"
dizaynından fərqli, beyannaməyə FK ilə bağlıdır), `KassaKursStatus` enum
(Gözləyir/Təsdiqləndi/İmtina), `KassaTesdiqEdici`.

Migration: `20261009070000_Kassa.cs` (əl ilə, `InsertData` YOXDUR — hər iki
siyahı boş başlayır; "Kassa" departamenti də seed edilmir, adi Departament
idarəetmə ekranından yaradılmalıdır).

Valyuta siyahısı (`USD/AVRO/IRR/AED/RUB`) **tək yerdə** —
`KassaKursService.Valyutalar` static massivi. Yeni valyuta əlavə etmək
lazımdırsa, YALNIZ bura əlavə et.

⚠️ **Köçürülməyib (qapsam xaricində qaldı, BMI-də var idi):**
- `odb.calendar` ilə "qeyri-iş günü" yoxlaması (BMI-nin monthCalendar-ı bunu
  edirdi) — FinNex-də tarix seçimi sadə `<input type="date">`-dir, iş günü
  yoxlaması yoxdur.

BMI-də mövcud idi, FinNex-ə ilk keçiddə DAXİL EDİLMƏDİ (istifadəçi tələb
etməyib) — lazım olsa ayrıca iş kimi əlavə edilə bilər. (Word sənədi yaratma
aşağıda, 10.10.2026-da əlavə olundu — bura daxil deyil.)

✅ **Real Visual Studio Rebuild-də yoxlanıldı (09.10.2026).** İlk nəticə
2 × CS0246 idi — `ExchangeTesdiqController.cs`-də `using FinNex.Domain;`
unudulmuşdu (`AppUser` o namespace-dədir, `.Entities.HR` yox — bax "Qovluq
Adı ≠ Namespace" bölməsindəki əlavə). Düzəldildi, bir sətir.

### Görüntü — `fn-` Dizayn Sisteminə Uyğunlaşdırıldı (09.10.2026)

İstifadəçi canlı ekran göstərib soruşdu: *"layoutu duz secmisen? diger
sehifelerden ferqlenmirki?"* Haqlı idi — ilk versiya `fn-page`/`fn-card`/
`fn-btn` ilə başlayırdı (düzgün), amma cədvəl və input-lar **xam Bootstrap**
idi (`table`, `form-control`) — `user-area.css`-dəki brendli `.fn-input`
(qızılı fokus halqası, 8px radius) və modul-xüsusi cədvəl stilinin (`pk-*`,
Kocurme-də; `av-*`, Avtopark-da) heç birini işlətmirdi. Nəticə: mavi fokus
halqalı, boz başlıqlı "yad" görünüş.

**Düzəliş:** yeni, kiçik `wwwroot/css/Kassa/exchange.css` — `.kx-table`
(brend tonlu başlıq/sərhəd) + `form-control` → `fn-input`/`fn-textarea`,
`btn btn-*` → `fn-btn fn-btn--*`. Həm `Exchange/Index.cshtml`, həm
`ExchangeTesdiq/Index.cshtml` yeniləndi.

### MB (CBAR) Rəsmi Kursu — Bugünkü Gün, USD/AVRO (09.10.2026)

İstifadəçi tələbi: *"basda cbar saytindan cari gune 2 kursu gostermeyivi
isteyirem. USD ve avro, seliqeli sekilde."* Yeni Oracle sorğusu/cədvəl
qurulmadı — mövcud **`IBmiValyutaService.KursAsync`** (BMI `kurval`,
`VALYUTA_KURSU` saxlanmış sorğusu, Pul Köçürməsi 20k limitinin onsuz da
istifadə etdiyi mənbə) tək buraxıldı, sadəcə `ExchangeController.Index`-dən
`KursAsync("01", bugün)` (USD) və `KursAsync("02", bugün)` (AVRO) çağırılır.

⚠️ **Seçilmiş `tarix` parametrindən ASILI DEYİL** — kassir keçmiş günə baxsa
belə, yuxarıdakı MB kartı **həmişə bugünkü** rəsmi kursu göstərir (istifadəçi:
"cari günə"). Oracle əlçatmaz olsa `KursAsync` `null` qaytarır — kart İNDİ DƏ
GÖSTƏRİLİR («—» + izah mətni ilə, aşağı bax), gizlənmir (BMI özü-öz kursu
daxil etdiyi üçün bu MB rəqəmi sırf məlumat üçündür, heç bir hesablamaya girmir).

### İKİ BUG BİRLİKDƏ — Paralel `_uow` Çağırışı Həm AVRO-nu Gizlədirdi, Həm Səhifəni Sındırdı (09.10.2026, KRİTİK — DÜZƏLDİLDİ)

İlk versiyada `ExchangeController.Index` USD/AVRO kurslarını **paralel** çağırırdı
(`var usdTask = _bmiValyutaService.KursAsync(...)`, `await` sonra, `GunlukGetirAsync`/
`SonBeyannameleriGetirAsync`-dən SONRA). İstifadəçi əvvəlcə yalnız USD tilinin
göründüyünü, AVRO-nun ÜMUMİYYƏTLƏ görünmədiyini bildirdi — sonra, bir az sonra,
bütün `/Kassa/Exchange` səhifəsi **500 Internal Server Error** verməyə başladı:

```
InvalidOperationException: A second operation was started on this context
instance before a previous operation completed. This is usually caused by
different threads concurrently using the same instance of DbContext.
```

**Kök səbəb — EYNİ bug, İKİ fərqli simptom.** `IBmiValyutaService.KursAsync`
`_uow.Repository<OracleSorgu>()` ilə (`OracleSorgular` cədvəlindən saxlanmış
sorğunu oxumaq üçün) **scoped `DbContext`** işlədir — bu, `KassaKursService`-in
`GunlukGetirAsync`/`SonBeyannameleriGetirAsync` metodlarının işlətdiyi **EYNİ**
request-scoped `IUnitOfWork` nüsxəsidir. `usdTask`/`avroTask` `await` edilmədən
başladıldığı üçün onlar İKİSİ BİR-BİRİ İLƏ, sonra da `GunlukGetirAsync` çağırışı
İLƏ eyni anda həmin paylaşılan `DbContext`-ə müraciət edirdi:

1. **AVRO görünməməsi** — `KursAsync`-in özündə `catch { return null; }` var
   (Oracle əlçatmaz olanda "əlçatmazdır" sayıb null qaytarmaq üçün YAZILIB).
   `usdTask`/`avroTask` bir-biri ilə toqquşanda İKİSİNDƏN BİRİ bu catch-ə
   düşürdü — hansının uduzacağı qeyri-müəyyən idi, amma nəticə HƏR DƏFƏ
   «AVRO null, USD dolu» (yaxud əksi) kimi görünürdü, heç bir Oracle data
   boşluğu YOX idi.
2. **500 xətası** — sonrakı bir çağırışda (`GunlukGetirAsync`) toqquşma
   artıq `KassaKursService`-in İÇİNDƏ baş verdi, orada bu cür `catch` YOXDUR
   — səhv tutulmadan bütün səhifəni sındırdı.

Bu, CLAUDE.md-də artıq sənədləşmiş **"Bildirişlər — Paralel Yazı"** bölməsi
ilə EYNİ tələdir (paylaşılan scoped `DbContext` üzərində `Task.WhenAll`/paralel
çağırış) — fərq yalnız bunun burada **yazıda yox, OXUMADA** baş verməsidir.

**Düzəliş:** `ExchangeController.Index`-də bütün dörd çağırış (`Gunluk`,
`SonQeydler`, `UsdMbKurs`, `AvroMbKurs`) **ardıcıl `await`** edilir — heç biri
`Task` dəyişəninə ayrıca saxlanıb sonra gözlənmir. MB tilləri də həmişə
göstərilir (`Model.MbTarix` ilə tarix əlavə olundu) — dəyər `null`-dursa «—»
+ «Bugünkü kurs tapılmadı — BMI-də hələ dərc olunmayıb və ya Oracle
əlçatmazdır» qeydi (`.kx-mb-note`, amber rəng).

**Qayda: eyni request-scoped `IUnitOfWork`/`DbContext`-i işlədən İKİ servis
metodunu `Task`/`await` ilə PARALEL başlatma** — bu, yalnız `Task.WhenAll`
yazanda deyil, sadəcə `var t = service.MetodAsync(...)` yazıb AŞAĞIDA başqa
bir `await` çağırışından sonra `await t` yazanda da baş verir, çünki `t`
arxada DƏRHAL icraya başlayır. Nəticə hər dəfə EYNİ görünməyə bilər (race
condition) — bəzən səssiz `null`/boş nəticə (catch-lə tutulan yerdə), bəzən
tam 500 xətası (tutulmayan yerdə). Bir controller-də bir neçə servis
çağırışı varsa və onların heç biri açıq-aydın paralel-təhlükəsiz (məs.
`IOracleService`-in öz `CehdEtAsync` təkrar-cəhd məntiqi kimi) olmadığını
bilmirsənsə, **ƏN TƏHLÜKƏSİZ DEFOLT ardıcıl `await`-dir**.

### Təsdiqedicinin Kassa Rolu Yoxdursa Təsdiq Ekranına Yol TAPILMIRDI (09.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi real ssenarini sınadı: Admin → Kassa → Təsdiqedicilər-dən Anar
İ.-ni (müdir müavini, `RoleNames.Kassa` rolu YOXDUR) təsdiqedici təyin etdi,
sonra soruşdu: *"anara tesdiq icazesi vermisem ama o departament kassaya
gire bilmir, bunu nece tesdiqleyecek?"* Ekranda sübut da var idi — Anar
top-nav-dakı «Departamentlər → Kassa» linkinə basanda `/Kassa/Exchange`-ə
yönəlib **«İcazə yoxdur»** səhifəsinə düşürdü.

**Kök səbəb — giriş nöqtəsi catch-22-dir, icazə sxemi yox.**
`ExchangeTesdiqController` (təsdiq ekranı) heç bir rol tələb etmir —
yalnız `[Authorize]` + `IKassaTesdiqEdiciService.TesdiqEdeBilerMiAsync`
roster yoxlaması (bax yuxarı "BMI-də tapılan 3 bug" cədvəli, 3-cü sətir).
Sidebar-dakı «Kurs təsdiqi» linki də **hamıya görünür** (kodda açıq şərh
var: *"giriş yoxlaması ExchangeTesdiqController-in özündədir, linki
gizlətmirik"*). Problem: sidebar YALNIZ artıq `Area=Kassa` daxilindəysən
göstərilir (`DetectActiveModule`-da `currentArea=="Kassa"` şərti), top-nav
dropdown-dakı **YEGANƏ** giriş nöqtəsi isə birbaşa `Exchange/Index`-ə
gedirdi — o isə `[Authorize(Roles = RoleNames.Kassa + "," + RoleNames.Admin)]`
ilə **məhduddur**. Yəni Kassa rolu olmayan təsdiqedici bu linkə basanda
backend-in icazə verdiyi səhifəyə (`ExchangeTesdiq/Index`) heç çatmadan
Access Denied-ə düşürdü — sidebar-ın "hamıya görünür" qaydası mənasız
qalırdı, çünki onu göstərəcək səhifəyə girişin özü bloklanmışdı.

**İlk düzəliş (TƏRK EDİLDİ):** top-nav «Departamentlər» dropdown-una
**İKİNCİ, ayrıca** giriş əlavə edilmişdi — «Kurs təsdiqi» → `ExchangeTesdiq/Index`.
İstifadəçi bunu rədd etdi: *"kurs tesdiqini departamentlerde niye saldin,
kassaya girende orda tesdiq etsin"* — iki ayrı sıra (`Kassa` + `Kurs təsdiqi`)
UI-da qarışıq göründü, halbuki istifadəçinin gözlədiyi **TƏK** "Kassa" girişi
idi, rolu özü düzgün yerə aparmalı idi.

**Son düzəliş — `KassaController.Index` (rolsuz, yönləndirici):**
top-nav-dakı **TƏK** "Kassa" linki artıq `Exchange/Index`-ə YOX,
`Kassa/Index`-ə (yeni, kiçik controller, `[Authorize]` yetər, heç bir rol
şərti yoxdur) gedir. Bu controller giriş anında qərar verir:

```csharp
if (User.IsInRole(RoleNames.Kassa) || User.IsInRole(RoleNames.Admin))
    return RedirectToAction("Index", "Exchange");

var appUser = await _userManager.GetUserAsync(User);
if (appUser?.IsciId is int isciId && await _tesdiqEdiciService.TesdiqEdeBilerMiAsync(isciId))
    return RedirectToAction("Index", "ExchangeTesdiq");

return RedirectToAction("Index", "Exchange");   // heç biri deyilsə — tanış "İcazə yoxdur"
```

Kassir üçün «Kassa»ya basmaq **eyni** nəticəni verir (Exchange/Index).
Kassa rolu olmayan təsdiqedici üçün isə **eyni TƏK link** avtomatik
`ExchangeTesdiq/Index`-ə aparır — iki ayrı menyu sırası görünmür, hər kəs
«Kassa»ya girib öz səhifəsində çıxır. Sidebar-dakı «Exchange» + «Kurs
təsdiqi» cütü (Area=Kassa daxilində, hamıya görünür) **TOXUNULMADI** —
artıq içəridəsən deyə problemsizdir.

**Qayda: «giriş icazəsi backend-də düzgündür» kifayət etmir — NAVİQASİYA
YOLUNUN ÖZÜ də hər rol üçün mövcud olmalıdır, AMMA menyunu rol sayına görə
ÇOXALTMA.** Bir modulun birdən çox daxili səhifəsi (A: kurs daxiletmə, B:
təsdiq) fərqli rollara xidmət edirsə, **hər səhifə üçün ayrı top-level
menyu sırası YARATMA** — bu, istifadəçiyə "bunlardan hansını seçim?"
sualı verir. Əvəzinə modulun TƏK giriş nöqtəsini (burada: "Kassa") rol-
şüurlu bir yönləndiriciyə çevir, modulun öz daxilində (sidebar/tab) həmin
səhifələr arasında keçid saxla. Menyu sırasının sayı istifadəçinin rolundan
asılı olaraq DƏYİŞMƏMƏLİDİR — arxada hara aparacağı dəyişə bilər.

### "Gün Başına Bir Beyannamə" Qaydası Gün İçində Kurs Dəyişikliyini BLOKLAYIRDI (09.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi canlı ekran göstərdi: günün kursu artıq təsdiqlənmişdi (status
"Təsdiqləndi"), forma bütün xanaları **solğun/disabled** göstərirdi, heç bir
göndər düyməsi yox idi — soruşdu: *"gun icinde kursu yeniden deyismek imkani
yoxdur axi, daha dogrusu yeni kurs elave etmek imkani"*.

**Kök səbəb:** `KassaKursService.SaxlaAsync`-də `movcud != null &&
movcud.Status != KassaKursStatus.Imtina` şərti **"Gözləyir" VƏ "Təsdiqləndi"**-ni
EYNİ cür bloklayırdı — "artıq göndərilib, yenidən yazıla bilməz". "Gözləyir"i
bloklamaq doğrudur (iki təklif eyni anda gözləmədə qalmasın), amma
"Təsdiqləndi"ni bloklamaq SƏHV idi — valyuta məzənnəsi gün ərzində bir neçə
dəfə dəyişə bilər (BMI-dən köçürülmüş real tarixçə bunu göstərirdi — eyni
günə bir neçə fərqli kurs). `KassaKursGunlukDto.YazaBiler` də eyni şərti
güddüyü üçün forma tam kiliddə qalırdı.

**Həll — üçüncü bir hal əlavə edildi, iki köhnəsi SAXLANILDI:**

| Günün cari statusu | Yeni yazıla bilər? | Nə baş verir |
|---|---|---|
| Yoxdur | ✅ | yeni beyannamə |
| **Gözləyir** | ❌ | *dəyişmədi* — iki təklif eyni anda gözləmədə qalmasın |
| **Təsdiqləndi** | ✅ **(yeni)** | **YENİ** beyannamə yaradılır, köhnə təsdiqlənmiş sətir **TOXUNULMADAN tarixçədə qalır** |
| İmtina | ✅ | *dəyişmədi* — mövcud sətir yerində redaktə olunur (rədd olunan təklif hələ qüvvəyə minməyib) |

`KassaKursBeyannamesi.Tarix` üzərində **unikal indeks yoxdur** (`IX_
KassaKursBeyannameleri_Tarix` adi indeksdir) — eyni günə birdən çox sətir
yazmaq DB səviyyəsində əvvəldən icazəlidir, yalnız servis qaydası bunu
bloklayırdı. Migration dəyişikliyi lazım olmadı.

⚠️ **"Təsdiqləndi" üzərinə YAZILMIR, YENİ sətir açılır** — "İmtina"dan fərqli.
Səbəb: təsdiqlənmiş kurs artıq **real istifadə olunmuş** bir qərardır (bəlkə
onunla əməliyyat aparılıb) — üzərinə yazsaq audit tarixçəsi təhrif olunardı.
Rədd olunan (İmtina) təklif isə HEÇ VAXT qüvvəyə minməyib, onu yerində
düzəldib yenidən göndərmək məntiqlidir — bu fərq qəsdəndir.

**Görüntü:** "Təsdiqləndi" info-box-una izah əlavə edildi ("gün ərzində kurs
dəyişibsə aşağıda yeni dəyər yazıb göndərə bilərsiniz"), göndər düyməsinin
mətni statusa görə dəyişir (`"İcra et"` / `"Yenidən göndər"` / `"Yeni kurs
göndər"`). `GunlukGetirAsync` onsuz da **ən son (ID-yə görə)** beyannaməni
göstərdiyi üçün yeni göndərilən kurs avtomatik "cari status" kimi görünür,
əlavə dəyişiklik lazım olmadı.

### Word Sənədi Generasiyası — "WordPrint" Köçürülməsi Tamamlandı (10.10.2026)

İstifadəçi: *"menim word sablonum var idi BMI de onudan duzeldek bitsin bu
is"* — yuxarıda "qapsam xaricində" sayılan Word generasiyası indi əlavə
olundu. BMI-nin `frmExchange.cs`-dəki `dtg_Exchange_CellContentClick` handler-i
(grid-in "WordPrint" başlıqlı düymə sütunu) oxundu və tam köçürüldü.

**Şablon faylı — ƏVVƏLCƏ KÖHNƏ BİNAR `.doc` İDİ.** İstifadəçinin göndərdiyi
ilk fayl OLE2/CFB (Word 97-2003) formatında idi — bayt başlığı (`D0 CF 11 E0`)
ilə yoxlanıldı. OpenXML (`KreditWordService`-in əsaslandığı
`WordprocessingDocument`) yalnız `.docx` (zip/OPC) aça bilir — bu, artıq
sənədləşmiş qayda ("Köhnə `.doc` (OLE2) faylları `KreditWordService` ilə
AÇILMIR" — yuxarı, "Word Şablonu" bölməsi). İstifadəçiyə açıq bildirildi,
Word-də "Fərqli saxla → .docx" ilə yenidən göndərildi — real `.docx` (OOXML
zip) olduğu təsdiqləndi, kod yalnız bundan sonra yazıldı.

**Tokenlər — BMI-nin öz kodundan dəqiqləşdirildi** (həm əl ilə OLE stream
parsing, həm paralel işə salınmış araşdırma agent-i ilə İKİ MÜSTƏQİL üsulla
təsdiqləndi, nəticələr tam üst-üstə düşdü):
- `{tar}` — şablonda **İKİ dəfə** keçir, hər ikisi sözlə yazılmış tarixdir
  (`KreditSozeCevir.TarixiSoze` — BMI-nin `Aletler.TarixiSozeCevir`-inin
  FinNex qarşılığı, suffiks qaydası da daxil, nüsxə çıxarılmadı).
- `{v1}`…`{v25}` — 5 valyuta × 5 sütun (Valyuta adı, Nağd alış, Nağd satış,
  Qeyri nağd alış, Qeyri nağd satış).
- İmzalayan adları ("Bank Melli İran Bakı filialının müdiri:
  A.K.Najafimarganmaskan", "Əslini aldım … Axundov E.A") şablonda **SABİT
  MƏTNDİR**, token DEYİL — BMI-nin `replacements` dictionary-sində heç vaxt
  olmayıb. Dinamik etmək YENİ funksionallıq olardı, köçürmə deyil — əlavə
  edilmədi, istifadəçi tələb etməyib.
- "WordPrint" bazada heç bir sütun/bayraq deyilmiş (BMI-nin bütün
  `bmi_kassa_kurs` sütunları və SQL-ləri yoxlanıldı) — sadəcə grid düyməsinin
  başlıq mətni idi. Ona görə FinNex-də də "artıq yaradılıb" izi saxlanmır,
  hər çağırışda sənəd yenidən yaradılır (BMI-dəki kimi).

⚠️ **VALYUTA SIRASI EKRANDAN FƏRQLİDİR — bu, tapılan ən kritik fərqdir.**
`{v1}…{v25}`-in sırası `KassaKursService.Valyutalar`-ın (ekran sırası:
USD→AVRO→**IRR→AED→RUB**) YOX, BMI-nin `cl_Database.GetKassaKurs()`
sorğusunun öz `ORDER BY CASE Valyuta`-sının (USD→AVRO→**RUB→AED→IRR**)
sırasıdır. Bu səbəbdən **`KassaKursService.WordSablonSirasi`** adlı AYRI
massiv əlavə edildi — Word generasiyası `Valyutalar`-ı işlətsəydi IRR/AED/RUB
dəyərləri şablonda bir-birinin yerinə düşərdi, heç bir xəta vermədən (klassik
"sütun adı məzmunu zəmanət etmir" sinfindən, fərqli formada: "ekran sırası
BAŞQA bir sistemin sabit sırasını zəmanət etmir").

**Köhnə Azəri şriftləri.** Şablonda (həm orijinal `.doc`-da, həm yenidən
saxlanmış `.docx`-da) `Azeri_Bookman_Lat`/`Times Latin` run-ları var —
`{tar}`/`{v1-25}` məhz bu run-ların İÇİNDƏdir. `KreditWordService.Doldur`-un
`unicodeSrift: "Times New Roman"` parametri işə salındı (dəyərdə ASCII-dən
kənar hərf olanda — məs. il sıra şəkilçisi "-cı"/"-cü" — run şriftini əvəz
edir, rəqəmlər toxunulmur) — Kredit modulunda artıq sınanmış qoruyucu.

**Çıxış — lokal Word AÇILMIR, BRAUZERƏ YÜKLƏNİR.** BMI `Process.Start` ilə
sənədi kassirin öz kompüterindəki Word-də açırdı — veb mühitdə qarşılığı
yoxdur, `KreditWordService`-in bütün digər çağırışları ilə EYNİ naxış:
`File(bayt, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ad)`.

**Giriş nöqtəsi — YALNIZ `ExchangeController` (kassir ekranı).**
`ExchangeTesdiqController`-ə (təsdiq ekranı) ƏLAVƏ EDİLMƏDİ — BMI-nin
`frmExchangeTesdiq.cs`-i Word/print-lə bağlı HEÇ BİR koda malik deyil (yoxlanıldı),
bu, faydalı əlavə YOX, qapsamın xaricinə çıxmaq olardı.

**İki giriş yeri, EYNİ action:**
1. Günün öz formasında (`Exchange/Index`) — status nə olursa olsun (`g.BeyannameId`
   varsa) "Word sənədi" düyməsi, status zolağının yanında.
2. "Son qeydlər" tarixçə cədvəlində — hər beyannamə 5 valyuta sətri kimi göründüyü
   üçün (`KassaKursSiyahiDto`, bir sətir = bir (gün×valyuta) cütü) düymə təkrarlanmasın
   deyə `HashSet<int>` ilə **yalnız hər qrupun ilk sətrində** göstərilir.

**Servis qatı:** `IKassaKursService.BeyannameGetirAsync(int beyannameId)` əlavə
olundu (interfeys + implementasiya birlikdə — "Metod İmzası Dəyişikliyi"
qaydasına uyğun). `GunlukGetirAsync` və bu metod EYNİ `MapGunluk` private
helper-ini paylaşır — iki nüsxə saxlanmadı.

⚠️ **Şablon fayl DMS-dədir, repoda DEYİL.** Runtime yolu:
`C:\FinNex_DMS\hesabat-sablonlari\kassa\Exchange.docx` (`YolVereqesiController`-in
Avtopark şablonu ilə EYNİ konvensiya). İstifadəçi faylı özü ora qoyacaq — bu
sessiyada repoya kopyalanmadı (istifadəçi qərarı: "sen yerini de, men lazim
olan yere fayli ataram ozum"). Fayl yoxdursa `Word` action-ı `TempData["Error"]`
ilə dəqiq yolu göstərir, boş/xəta sənəd qaytarmır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(namespace-lər hər bir `using` üçün faylın öz `namespace` sətrindən təsdiqləndi,
mötərizə balansı yoxlanıldı). İstifadəçi Visual Studio-da **Rebuild All** ilə
yoxlamalı, həm real şablonla (DMS-ə qoyulandan sonra) sənəd yaradıb açmalıdır —
xüsusilə valyuta sırasının (USD→AVRO→RUB→AED→IRR) düzgün göründüyünü və Azəri
hərflərinin (il şəkilçisi) qopuq çıxmadığını.

### Oxuma İcazəsi Genişləndi — YAZMA Kassa/Admin-də QALIR (12.10.2026)

İstifadəçi: *"men hemin exchangeni kimlerse oxumaq imkani vere bilim, copy
goture bilsin kurslardan, cunki hemin kursu basqa yerde istifade edirik, tam
blok olmasin sehife"* — qərar soruşuldu, cavab: **bütün giriş etmiş işçilər**
(admin-idarəli ayrıca "oxuyucu" siyahısı YOX — sadə, əlavə idarəetmə lazım
deyil).

**Dəyişən YALNIZ `Index` (göstərmə).** `ExchangeController`-in sinif səviyyəli
`[Authorize(Roles = Kassa+Admin)]`-i **`[Authorize]`**-ə (rolsuz) sadələşdi.
Yazma (`Saxla`) və sənəd (`Word`) **hər biri ÖZ action-unda** eyni
`[Authorize(Roles = Kassa+Admin)]`-i aldı — sinif səviyyəsindən enməklə itən
qoruma geri qaytarıldı, YALNIZ başqa yerə köçürüldü.

**Top-nav/sidebar linkləri ARTIQ dəyişiklik tələb etmədi** — yoxlanıldı,
"Departamentlər → Kassa" (top-nav) və Area=Kassa sidebar-dakı "Exchange"/"Kurs
təsdiqi" linkləri **əvvəldən** rolsuz idi (hamıya görünürdü), bloklayan yeganə
qat elə `ExchangeController`-in sinif atributu idi.

⚠️ **`disabled` YOX, `readonly` — kopyalama tələbi buna görə seçimi həll etdi.**
İlk cəhddə qeyri-Kassa işçi üçün input-lar `disabled` edilmişdi (mövcud
`g.YazaBiler` məntiqinin təbii uzantısı kimi) — AMMA `disabled` input-un mətni
bir çox brauzerdə seçilə/kopyalana BİLMİR, elə istifadəçinin əsas tələbini
(«copy götürə bilsin») pozardı. Həll: `canEdit` (`User.IsInRole(Kassa) ||
IsInRole(Admin)`) bayrağı ilə input-lar `readonly="@(!canEdit || !g.YazaBiler)"`
— redaktə bağlıdır, seçim/Ctrl+C açıqdır. Göndər düyməsi və "Word sənədi"
düymələri (həm formada, həm "Son qeydlər" cədvəlində) `canEdit` olmayanda
tamamilə GİZLƏDİLİR (server action-ı onsuz da rədd edəcək, dead-end klik
yaratmamaq üçün).

**Qayda: "oxumaq icazəsi ver" tələbini `disabled` ilə həll etmə** — əgər
məqsəd dəyəri görmək/kopyalamaqdırsa (redaktəni YOX), `readonly` düzgün
seçimdir. `disabled` yalnız "bu sahə HEÇ bir məna daşımır, görünməsin belə"
hallarına uyğundur.

### "Son qeydlər" Cədvəlində Təsdiq Tarixi Göstərilmirdi (12.10.2026)

İstifadəçi: *"ve birde tesdiqleme saatinida UI da goster"*. Yoxlanıldı —
təsdiq saatı artıq **iki yerdə** göstərilirdi: `Exchange/Index.cshtml`-in öz
günün info-box-unda (`g.TesdiqTarixi`) və `ExchangeTesdiq/Index.cshtml`-in
"Son nəticələnmiş qeydlər" cədvəlində (`b.TesdiqTarixi`). **Yalnız**
`Exchange/Index.cshtml`-in "Son qeydlər" tarixçə cədvəlində yox idi —
`KassaKursSiyahiDto` (`Model.SonQeydler`) həmin sahələri (`TesdiqEdenAdi`,
`TesdiqTarixi`) onsuz da daşıyırdı, sadəcə cədvələ çıxarılmamışdı.

**Düzəliş:** cədvələ «Təsdiqləyən» və «Təsdiq tarixi» (`dd.MM.yyyy HH:mm` —
`ExchangeTesdiq`-dəki formatla EYNİ) sütunları əlavə edildi, boş-nəticə
sətrinin `colspan`-ı 9-dan 11-ə yeniləndi.

- Səhv aşkar olarsa dərhal bildirr — gizlətmə, bəhanə axtarma.
- Nə səhv olduğunu, niyə olduğunu, necə düzəldildiyini izah et.
- Eyni tip səhvin bir daha olmaması üçün bu fayla əlavə et.

## Göndərilən Mail + Mail İzləmə Siyahısı (12.10.2026)

İstifadəçi tələbi: *"geden mailleride gormek imkani olsun, cox seliqeli, mail
proqramina oxsarliqda"* + *"iscide siyahi elave etmek imkanida olsun ki,
istediyi mail unvanlarini elave etsin, secimi olsun ki yalniz bu mailden
gelen ve gedenleri gorsun, ve elave edekki hemin maillerden gelenlarin
bildirisi cixsin ekrana"*. Üç hissədən ibarətdir, hamısı `GelenMail`-in
(Gələn Mail, bax yuxarıdakı "mail_istifade" bölmələri) **güzgüsü/əlavəsidir**
— izolyasiya, icazə (`[MailIcazesi]`) qaydası da EYNİdir.

### 1. Göndərilən Maillər (`GedenMail`) — IMAP "Sent" sinxronu

`GelenMailImapSyncer` (Inbox) ilə **tam simmetrik** yeni servis —
`GedenMailImapSyncer.SyncSentNowAsync`. Fərq yalnız qovluqdadır: əvvəlcə
MailKit-in `GetFolder(SpecialFolder.Sent)` (SPECIAL-USE/XLIST) sınanır,
dəstəklənmirsə `BilinenSentAdlari` siyahısı (`Sent`, `Sent Items`, `INBOX.Sent`
və s.) üzrə ad-axtarışına keçilir. Nəticə **yalnız göstərmə üçündür** — real
göndərmə hələ də SMTP ilə gedir (`GelenMailController.CavabGonder`), bu servis
heç nə göndərmir, poçt qutusunun artıq göndərdiyini oxuyur.

Fon sinxronu (`GelenMailSyncService`) hər dövrdə **hər iki** istiqaməti
(Inbox + Sent) ayrı scope/ayrı cəhdlə sinxronlaşdırır — Sent üçün bildiriş
YARADILMIR (istifadəçi özü göndərib, xəbər vermə mənasızdır).

⚠️ **UI qəsdən sadədir** — AI təhlili, tapşırma, cavab yazma YOXDUR (bunlar
Gələn Mailə məxsus). Yalnız axtarış/filtr/silmə + qoşma mətnini önizləmə.
Baxılan sıra: `Exchange`/`Kassa` modulunda olduğu kimi, görüntü ÜÇÜN `~/css/
User/gelen-mail.css`-dəki mövcud `gm-*` klasları TƏKRAR işlədilir — yeni CSS
faylı yaradılmadı.

⚠️ **Bu modulun DB cədvəlləri EF migration-la DEYİL** — `GelenMail`-in özü
kimi, `FinNex.UI/Program.cs`-dəki idempotent `IF NOT EXISTS` SQL bloklarıyla
qurulur (startup-da). Bu, layihənin əsas "Əl ilə Yazılan Migration" qaydasından
(`DataAccess/Migrations/*.cs`) FƏRQLİ, köhnə Gələn Mail modulunun davam etdirdiyi
**üçüncü bir naxışdır** — toxunanda qarışdırma, mövcud `GelenMailler`/
`GelenMailQosmalar` bloklarının yanında, EYNİ üslubda yazılmalıdır.

### 2. Mail İzləmə Siyahısı (`MailIzlemeUnvani`)

İşçi `/User/MailIzleme` səhifəsində istədiyi qədər e-poçt ünvanı əlavə edir
(şəxsi siyahı — `SahibUserId`). Gələn/Göndərilən Mail səhifələrində **"Yalnız
izlənənlər"** checkbox-u bu siyahı ilə uyğun gələn mailləri süzür:
- Gələn Mail → `KimdenEmail` (göndərən) siyahıdadırsa göstərilir;
- Göndərilən Mail → **yalnız İLK (əsas) alıcı** (`KimeEmail`) yoxlanılır,
  `KimeToplu`-dakı Cc-lər YOX (dinamik siyahı üzrə sətir-daxili LIKE SQL-ə
  yaxşı tərcümə olunmur — qəsdən buraxılan sadələşdirmədir).

**Normallaşdırma MƏCBURİDİR** (Pul Köçürməsi-dəki `FinTemizle` ilə EYNİ
prinsip) — ünvan həmişə `Trim().ToLowerInvariant()` ilə saxlanılır
(`MailIzlemeService.Normallashdir`), yoxsa `Qara@X.com` ilə `qara@x.com`
səssizcə iki ayrı ünvan sayılar və filtr/bildiriş keçər.

⚠️ **Yumşaq Silinmiş Sətir + Unikal İndeks tələsi BURADA DA var** (bax yuxarı
"Yumşaq Silinmiş Sətir..." bölməsi) — `(SahibUserId, Email)` üzərində unikal
indeks `Silinib`-i filtrləmir. `EkleAsync` əvvəlcə `QueryAll()` (filtrsiz) ilə
axtarır: tapılmayıbsa yeni yaradır, tapılıb AKTİVdirsə rədd edir, tapılıb
SİLİNİBsə **dirildir** (`Silinib=false`) — `Query()`-lə (avtomatik `!Silinib`)
axtarsaydı bu "dirilt" qolu heç vaxt işə düşməzdi və silinmiş ünvanı yenidən
əlavə etmək cəhdi `DbUpdateException` ilə sınardı.

### 3. İzlənən ünvandan mail gələndə AYRICA bildiriş

`GelenMailSyncService.SendIzlemeBildirisleriAsync` — hər sinxron dövründə,
yeni mail tapılıbsa, sinxron BAŞLAMAZDAN ƏVVƏLKİ anı (`syncBasladi`, 2 saniyə
ehtiyatla) qeyd edib sonra `YaradilmaTarixi >= syncBasladi` olan TƏZƏ sətirləri
işçinin izləmə siyahısı ilə tutuşdurur. Uyğun gələn HƏR mail üçün **ayrıca**
bildiriş göndərilir (başlıqda göndərənin adı) — bu, `SendOwnerBildirisAsync`-in
ümumi "N yeni mail" bildirişini ƏVƏZ ETMİR, ona **əlavədir** (fərqli başlıq/
mətn daşıdığı üçün `BildirisService`-in 15 saniyəlik dublikat qoruması
bunları bir-birini bloklamır).

**Ekrana "bildiriş çıxması" artıq hazır mexanizmdən gəlir** — `BildirisService.
YaratAsync` HƏR bildirişdə `IDesktopBildirisService.PushAsync`-i fire-and-forget
çağırır (SignalR ilə masaüstü push) — yeni kod YAZILMADI, mövcud borunu
işlətdi, sadəcə YENİ, daha konkret bir bildiriş mətni əlavə etdi.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(namespace/using-lər, metod imzaları, DTO sahələri bir-bir tutuşduruldu — bax
"`FindFirstValue` CS1061" bölməsi). İstifadəçi Visual Studio-da **Rebuild All**
ilə yoxlamalı, SMTP/IMAP hesabı olan real bir istifadəçi ilə ucdan-uca test
etməlidir: (1) Göndərilən Maillər səhifəsi "Yenilə" ilə Sent qovluğunu
gətirsin, (2) Mail İzləmə-yə ünvan əlavə edib Gələn/Göndərilən-də "Yalnız
izlənənlər" filtri işləsin, (3) həmin ünvandan real mail göndərilib fon
sinxronu (5 dəq) və ya "Yenilə" ilə gələndə ekrana xüsusi bildiriş çıxsın.

### Canlı Test Tapdığı 2 Bug — Boş İzləmə Siyahısı "Hamısı" Sayırdı + Filtr Yadda Qalmırdı (12.10.2026, KRİTİK)

İstifadəçi yuxarıdakı funksiyaları real mühitdə test edib iki ekran-görüntüsü
göndərdi: *"izleme bosdur axi,ve birde yadda saxlamir bunu? herdefe secim
etmeliyik?"*

**1. Boş izləmə siyahısı ilə "Yalnız izlənənlər" HAMISINI göstərirdi.**
`GelenMailService.GetListAsync` və `GedenMailService.GetListAsync`-in filtr
şərti `if (izlenenEmaillar != null && izlenenEmaillar.Count > 0)` idi — bu,
**«filtr tələb olunmayıb» (`null`) ilə «filtr tələb olunub, amma siyahı
boşdur» (boş, `null`-dan FƏRQLİ list) hallarını EYNİ cür rəftar edirdi**,
hər ikisində `.Where()` tətbiq olunmurdu. İstifadəçinin izləmə siyahısı
TAMAMILƏ boş olanda (`MailIzlemeUnvanlari`-də sıfır sətir) checkbox-u
işarələyəndə 50 maildən **hamısı** göstərilirdi — gözlənilən nəticə isə SIFIR
idi («izləmə siyahınız boşdur, uyğun gələn heç nə yoxdur»).

Düzəliş — hər iki servisdə eyni forma:
```csharp
if (izlenenEmaillar != null)
{
    query = izlenenEmaillar.Count > 0
        ? query.Where(x => izlenenEmaillar.Contains(x.KimdenEmail.ToLower()))  // GedenMail-də: x.KimeEmail
        : query.Where(x => false);
}
```
`null` yenə «toxunma» deməkdir (filtr heç tələb olunmayıb), **boş (non-null)
siyahı isə indi açıq şəkildə SIFIR nəticəyə məcbur edir**. Hər iki boş-siyahı
görüntüsündə («Hələ mail yoxdur» yox) istifadəçiyə izah edən mesaj +
«İzləmə siyahısına ünvan əlavə et» düyməsi əlavə olundu (`GelenMail/
Index.cshtml`, `GedenMail/Index.cshtml`) ki, səbəb açıq görünsün.

**Qayda: bir filtr parametri `List<T>?` tipindədirsə, `null` və «boş,
amma göndərilmiş» siyahı arasında İKİ AYRI anlam var** — `Count > 0`
yoxlamasını `!= null` yoxlamasından ayırma. Bu, "Üst-Üstə Düşən Məzuniyyət"
və "Limit 20 000 USD" bölmələrindəki eyni «filtr effektiv-boşdursa nəticə də
boş olmalıdır, "hamısı" DEYİL» prinsipinin YENİ bir forması idi.

**2. "İzlənən" checkbox-u yadda saxlanmırdı — hər səhifə ziyarətində yenidən
işarələmək lazım idi.** Checkbox GET formuna bağlıdır və `onchange` ilə
avtomatik submit olunur — server tərəfdə heç bir "istifadəçinin defolt
seçimi" saxlanmırdı, hər yeni ziyarət `izleme` parametrsiz başlayırdı.

Həll — **`localStorage`** (açar: `gmIzlemeFilter`, `GelenMail/Index.cshtml`
VƏ `GedenMail/Index.cshtml` arasında **PAYLAŞILIR** — konseptual eyni
seçimdir, ayrı saxlasaq istifadəçi «birində işarələdim, digərində yenə
soruşur» vəziyyətinə düşərdi):
- Checkbox-un `onchange`-i DƏRHAL (`this.form.submit()`-dən ƏVVƏL)
  `gmIzlemeDeyisdi(this.checked)` çağırır → cari vəziyyət (həm `true`, həm
  `false`) yazılır.
- Səhifə başında (CSS link-dən dərhal sonra, bloklayıcı `<script>`, DOM
  render olunmadan) — URL-də `izleme` parametri **ÜMUMİYYƏTLƏ YOXDURSA**
  (`!params.has('izleme')`), `localStorage`-dan oxunur; `'1'`-dirsə
  `location.replace` ilə `?izleme=true` əlavə edilib yönləndirilir.

⚠️ **`!params.has('izleme')` yoxlaması MƏCBURİDİR, sadəcə `!= 'true'` yox** —
checkbox GET formunda **unchecked** vəziyyətdə heç bir parametr göndərmir
(«Şərtli Render Olunan Form Sahəsi» bölməsindəki eyni HTML-checkbox xüsusiyyəti).
Yəni istifadəçi işarəni GÖTÜRƏNDƏ də URL-də `izleme` olmayacaq — bu, "heç vaxt
seçilməyib" ilə eyni görünər. Fərqi `localStorage`-un özü saxlayır: `onchange`
HƏR DƏFƏ (işarələnsə də, götürülsə də) yeni vəziyyəti yazdığı üçün, növbəti
səhifə ziyarətində `saved` artıq `'0'`-dır və avtomatik yönləndirmə işə
düşmür — istifadəçinin "götür" seçimi doğru xatırlanır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (metod imzaları, `JavaScript`-in brauzerdə işləyə bilən sintaksisi,
CSHTML bloklarının balansı). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra real ssenari ilə yoxlamalıdır: (1) izləmə siyahısı boşdursa "İzlənən"
işarələnəndə sıfır nəticə + izahlı mesaj gəlsin, (2) checkbox işarələnib
səhifədən çıxılıb yenidən girəndə vəziyyət qorunsun, (3) işarə götürüləndə də
qorunsun (yenidən avtomatik işarələnməsin).

## Masaüstü Bildiriş Klikinin "Login" Açması — Agent Bu REPODA DEYİL (12.10.2026, KRİTİK)

İstifadəçi şikayəti: ekrana düşən masaüstü bildirişə klikləyəndə yeni səhifə
açılır və **login/şifrə istəyir**, baxmayaraq ki, sayt artıq başqa tabda açıq
və giriş edilib. Araşdırma zamanı diaqnostik suallar "digər sessiya"ya
göndərildi (həmin sessiya agentin real klik məntiqini göstərən cavab verdi:
`NotificationPopup.cs:280-299`, `OnMouseDown` → `Process.Start` ilə sistemin
**defolt brauzerində** açılır, `TrayAgent.cs:246`-dakı `_config.BaseUrl` ilə
nisbi yolu tam ünvana çevirir).

⚠️ **BU FAYL BMI_FINANCE REPOSUNDA MÖVCUD DEYİL — hər branch, bütün tarixçə
yoxlanıldı (`git log --all -- '**/NotificationPopup.cs'`, nəticə BOŞ).**
Bu repoda olan **`FinNex.DesktopAgent/TrayAgent.cs`** (177 sətir) heç bir
`url`/`BaseUrl`/`Process.Start`/klik məntiqinə malik DEYİL — `ShowBalloon`
yalnız başlıq+mətn göstərir, SignalR payload-ındaki `url` sahəsini heç
OXUMUR BELƏ. Yəni real maşınlarda işləyən agent **bu repodan FƏRQLİ,
başqa bir kod bazasından build olunub** — "digər sessiya"nın gördüyü
`NotificationPopup.cs` buradan deyil, haradasa başqa yerdə yaşayır.

**Nəticə: bmi_finance TƏRƏFİ agentın klik davranışını birbaşa düzəldə
BİLMƏZ** — yalnız SignalR-la ONA NƏ GÖNDƏRDİYİNİ idarə edə bilər.
İstifadəçi qərarı: *"əgər linki biz bmifinancedən verəriksə, onda boş link
göndərək, klik edəndə heç nə baş verməsin, çünki agentdə bunu etmək bütün
kompüterləri yenidən yükləmək deməkdir"*. Yəni: düzgün işləməyən davranışdansa,
**heç bir davranış** seçildi.

**Həll — `SignalRDesktopBildirisService.PushAsync`, TƏK YERDƏ:** SignalR
payload-ındakı `url` sahəsi indi **HƏMİŞƏ `null`** göndərilir, metodun öz
`url` parametri nə olursa olsun (`url = (string?)null` payload-da sabit).
Bu, **beş çağırış yerinin** (`BildirisService.YaratAsync`,
`ChatController` ×3, `ChatHub`) heç birinə toxunmadan, tək bir yerdən
bütün masaüstü push-ları eyni qaydaya tabe edir — yeni bir çağırış yeri
əlavə olunsa belə, avtomatik qorunur.

⚠️ **`Bildiris.RedirectUrl` (DB sütunu) TOXUNULMADI** — bu, SignalR push-dan
TAMAMİLƏ AYRI bir mexanizmdir: `BildirisService.YaratAsync` onu entity-yə
yazır, **veb UI-nin öz Gələn Qutusu** (`Areas/User/Views/Inbox/Index.cshtml`,
«Bax →» linki) oradan oxuyur. O, artıq AÇIQ olan brauzer tabında işləyir,
heç bir login problemi yoxdur — YALNIZ masaüstü (SignalR) push-un göndərdiyi
`url` sahəsi söndürüldü, DB-dəki `RedirectUrl` yazılması davam edir.

**Qayda: "klik davranışını düzəlt" tələbi gələndə əvvəlcə HƏQİQƏTƏN hansı
kodun işlədiyini yoxla** — "WinForm agent bu reponun hissəsidir" fərziyyəsi
(əvvəlki sessiyada edilmiş) `git log --all` ilə YOXLANMADAN qəbul edilmişdi.
Başqa sessiyanın verdiyi fayl/sətir nömrələri bu repoda axtarılanda sıfır
nəticə çıxdı — bu, "kim doğru deyir" sualını deyil, "bu, İKİ AYRI KOD BAZASI"
faktını göstərir. Bu halda düzəliş yalnız **göndərilən datanın** üzərində
aparıla bilər (özü agentın məntiqini dəyişə bilməz) — agentın özünü
düzəltmək başqa bir repo/deployment-ə aid ayrıca işdir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı.** İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real
ssenari ilə yoxlamalıdır: bildiriş gələndə balloon/popup-a klikləyəndə HEÇ
NƏ baş verməməlidir (əvvəlki kimi login səhifəsi açılmamalıdır) — Gələn
Qutusundaki «Bax →» linki isə əvvəlki kimi işləməlidir.

## Mail Qoşmalarını Açmaq/Yükləmək — Endpoint Yox İdi (12.10.2026)

İstifadəçi tələbi: *"maillerde qosma fayllarida gormek mumkun olsun"*. Yoxlanıldı —
`GelenMail`/`GedenMail` qoşma kartlarında yalnız **ad + ölçü + (varsa) çıxarılmış
mətnin önizləməsi** var idi, faylın ÖZÜNÜ açan/yükləyən heç bir keçid YOXDU.
`FaylYolu` (disk yolu) `GelenMailQosma`/`GedenMailQosma`-da onsuz da saxlanılırdı
— sadəcə buna gedən GET endpoint unudulmuşdu.

**Oxşar funksiya onsuz da mövcud idi, amma FƏRQLİ məqsəd üçün:**
`IGelenMailService.QosmaGetirAsync` + `TapshiriqController.QosmaYukle` — bu,
yalnız «həmin maila TAPŞIRILMIŞ işçi» üçündür (`GelenMailIsci` yoxlaması).
Gələn/Göndərilən Mail isə **qutunun SAHİBİ** modelindədir (`SahibUserId`) — ona
görə **yeni, ayrıca** `QosmaYukleAsync(qosmaId, sahibUserId)` metodu əlavə
olundu (hər iki servisdə), köhnə `QosmaGetirAsync`-a toxunulmadı (başqa axının
icazə qaydasını dəyişmək səhv olardı).

**Endpoint:** `GET /User/GelenMail/QosmaYukle/{id}`, `GET /User/GedenMail/
QosmaYukle/{id}` — `TapshiriqController.QosmaYukle`-dəki EYNİ naxış
(`PhysicalFile`/`File(bytes, contentType, faylAdi)`), sadəcə icazə şərti fərqli
(tapşırıq yox, mülkiyyət: `qosma.GelenMail.SahibUserId == appUser.Id`).

**Dörd yerdə "Aç / Yüklə" düyməsi** — split-panel (`_MailDetail.cshtml`, həm
Gələn, həm Göndərilən) VƏ tam səhifə (`Detail.cshtml`, həm Gələn, həm
Göndərilən) eyni `gm-attachment-card`-ı TƏKRARLAYIR (iki ayrı layout, eyni
markup) — dördündə də eyni `<a href="@Url.Action("QosmaYukle", ...)" target=
"_blank">` əlavə edildi. Yeni bir attachment-göstərmə yolu əlavə olunsa, bu
dörd yerin hamısını tutuşdur.

⚠️ **Fayllar hələ `wwwroot/mail-qosmalar/`-dadır (DMS-də YOX)** — bu, CLAUDE.md-nin
yuxarıdakı "Fayl Yükləmə — SƏNƏD SAXLAMA QAYDASI" bölməsinə görə köhnə qaydadır
(`GelenMailImapSyncer`-in əvvəldən belə olan davranışı, `GedenMailImapSyncer`
onu köçürərkən eyni naxışı təkrarlayıb — bax "Göndərilən Mail + Mail İzləmə"
bölməsi). Bu sessiyada DƏYİŞDİRİLMƏDİ (qapsam xaricində) — yalnız mövcud
yerdəki faylı açmaq üçün endpoint əlavə olundu. `wwwroot` static middleware-i
bu faylları TEORİK olaraq icazəsiz də serve edə bilər (əgər konkret yol
bilinərsə) — amma GUID adlı fayllar üçün bu, təcrübədə aşağı risklidir; əsl
düzəliş (DMS-ə köçürmə + icazəli serve) ayrıca iş kimi qalır.

## Göndərilən/Gələn Mail Detalı — Görüntü İncəlişdirmə (12.10.2026)

İstifadəçi ekran-görüntüsü göstərib: *"burda bezi seyler cox iridir ve kobud
gorsenir, biraz incelesdirek"*. Split-panel detalının başlıq zolağı (avatar,
«Kimə:» adı, Mövzu) defolt ölçülərdə həddindən artıq **iri/qalın** görünürdü
(40px avatar, 1.25rem/700 mövzu, 600 çəkili ad) — adi bir mail-client-dən
gözlənildiyindən çox «kobud».

**Düzəliş — `gelen-mail.css`, YALNIZ şriftlər/ölçülər, markup TOXUNULMADI:**
- `.gm-mail-avatar`: 40px→32px, çəki 700→600, qradient bir az yumşaldıldı;
- `.gm-mail-from-name`/`.gm-mail-from-email`/`.gm-panel-date`: çəki/ölçü
  azaldıldı, rəng tündlükdən açıq boz-göyə (`#334155`/`#94a3b8`) keçirildi;
- `.gm-mail-subject`/`.gm-panel-subject` (iki ayrı layout, eyni konsept):
  1.2–1.25rem/700 → 1.05rem/600, `letter-spacing:-.01em` əlavə edildi;
- `.gm-attachments-title`/`.gm-attachment-name`/`.gm-attachment-card`:
  kiçik versal başlıq (uppercase, .78rem) + daha nazik kart padding-i.

**İKİ `_MailDetail.cshtml`-dəki "Kimə:" sətirləri INLINE STYLE idi** (CSS
class-ı deyil) — tam səhifə `Detail.cshtml`-lər artıq `.gm-mail-from-name`/
`.gm-mail-from-email` class-larını işlədirdi, split-panel isə eyni görünüşü
inline təkrarlayırdı. İnline-lar silinib class-lara keçirildi ki, bundan sonra
ölçü dəyişikliyi **tək yerdən** (CSS) idarə olunsun — iki yerdə ayrı-ayrı
"iri/kiçik" qalma riski aradan qalxdı.

⚠️ **Mail MƏTNİNİN ÖZÜ (imza, göndərənin HTML-i) TOXUNULMADI** — ekranda görünən
qalın göy imza mətni (məs. "T.Q.Məmmadov", "Bank Melli Iran Baku branch")
bizim CSS-dən YOX, orijinal e-poçtun öz HTML/inline-style-ından gəlir
(`Html.Raw(Model.MetinHtml)`). Onu "sadələşdirmək" inline `style="font-weight:
bold;color:navy"` kimi atributları override etmək (yalnız `!important` ilə
mümkündür) tələb edər — bu, başqa məktublardakı HƏQİQİ vurğulamanı da
(məs. "TƏCİLİ" kimi sözləri) səssizcə söndürə bilər. Qəsdən toxunulmadı;
istifadəçi konkret bunu da istəsə, ayrıca qərar və diqqətli əhatə (yalnız
imza bloku, bütün HTML yox) lazımdır.

### İkinci Dalğa — Başlıq Təkrarı + Siyahı Sətirləri Hələ İri İdi (12.10.2026)

İstifadəçi yeni ekran-görüntüləri göndərdi: *"men hecne gormurem, icolar boyukdur,
burdakilar cox hundurdur, bunuda naziklesdir"* + *"3-cu sekilde gonderilen
mailleri sil, zaten yixarida yizilir, bu halda bu divde naziklesecek"*.

**1. "Göndərilən Maillər" / "Gələn Maillər" başlığı İKİ DƏFƏ yazılırdı** —
`_UserLayout.cshtml`-in öz üst zolağı (`fn-topbar-title`, sətir 1551) onsuz
da `ViewData["Title"]`-ı göstərir («‹ Geri | Göndərilən Maillər | 12 oktyabr
2026»); səhifənin öz `gm-header`-indəki `<h1 class="gm-title">` eyni mətni
TƏKRARLAYIRDI. İkisi fərqli yerdən gəlir (layout vs səhifə), ona görə kodda
görünməyən bir dublikat idi. **Düzəliş:** hər iki `Index.cshtml`-dən `<h1>`
silindi, yalnız düymə sırası (Yenilə/Gələn-Göndərilən/İzləmə) qaldı.
`GelenMail`-dəki oxunmamış sayı (`oxunmamisSay`) İTİRİLMƏDİ — "Yenilə"
düyməsinin yanına kiçik pill kimi köçürüldü.

⚠️ **`GelenMail/TapshirilanMailler.cshtml`-ə TOXUNULMADI** — o, eyni
`gm-header`/`gm-split-top` class-larını paylaşır, amma öz `<h1>`-i var və
istifadəçi onu göstərmədi. `.gm-header`-in paylaşılan `margin-bottom`-u
(20px→0) və `.gm-split-top`-un padding-i (16px 0 12px→10px 0) **ÜÇ səhifəyə
də** tətbiq olundu (yeganə üç istifadəçisi bunlardır) — TapshirilanMailler-in
öz başlığı bir az daha yaxın görünəcək, amma kəsilmir/pozulmur.

**2. Sol siyahının sətirləri ("burdakilar cox hundurdur") kiçildildi** —
`.gm-item` padding 14px→8px, avatar 40px→30px (font .78rem), `.gm-from-name`
.9rem/600→.82rem/500, `.gm-item-subject` .88rem→.8rem, tarix/meta sahələri
.75rem→.7rem. Dəyişiklik `.gm-item`/`.gm-item-avatar` class-ları üzərindədir —
split-panel VƏ tam səhifə siyahıları eyni class-ları paylaşdığı üçün hər yerdə
avtomatik tətbiq olunur, ayrı-ayrı yer axtarmaq lazım olmadı.

**Qayda:** paylaşılan layout (`_UserLayout.cshtml`) artıq səhifə başlığını
göstərirsə, səhifənin öz görünən h1/h2 başlığı TƏKRAR yazmasın — layout-un
nə göstərdiyini yoxlamadan "səhifəyə öz başlığını qoy" fərziyyəsi, iki fərqli
mənbədən gələn gizli dublikata aparır (kodda `grep` ilə bir-birini görmür,
yalnız ekranda görünür).

### Mail İzləmə Siyahısı — 4-Sütunlu Grid-ə 2 Element Verəndə Sütunlar Sürüşür (12.10.2026, KRİTİK)

İstifadəçi ünvan əlavə edəndən sonra `/User/MailIzleme` səhifəsində sətir
yalnız kiçik, bənövşəyi, içində «fin...» kimi kəsilmiş mətn olan bir dairə
kimi göründü — nə ikon, nə tam email görünürdü: *"izlenenleri yazandan sonra
ico ve mail gorunmur"*.

**Kök səbəb:** `.gm-item` (`gelen-mail.css`) `display:grid; grid-template-columns:
3px 30px 1fr auto;` — DÖRD sütun gözləyir: indikator (3px), avatar (30px),
body (1fr), meta (auto). Bu, mail siyahısı sətirlərinin (`GelenMail`/`GedenMail`
Index-ləri) markup-unda HƏR DƖFƏ dörd uşaq elementlə təmin olunur — birincisi
`.gm-item-indicator` (boş, yalnız rəngli zolaq üçün).

`MailIzleme/Index.cshtml`-in sətri isə bu klassı TƏKRAR işlədərkən yalnız
**İKİ** uşaq verirdi: `.gm-item-avatar` və `.gm-item-body`. Grid-in avtomatik
yerləşdirməsi onları sıra ilə 1-ci (3px) və 2-ci (30px) sütuna qoydu —
avatar 3px-lik yerə, ad+email mətni olan body isə 30px-lik yerə sıxışdı.
Nəticə: email mətni həmin dar sütunda `text-overflow:ellipsis` ilə «fin...»-ə
qədər kəsildi, avatar da öz sabit 30px ölçüsü ilə 3px-lik sütundan çıxıb
mətnlə üst-üstə düşdü — ekranda hamısı bir "bənövşəyi badge" kimi görünürdü.

**Düzəliş:** `MailIzleme/Index.cshtml`-in sətrinə çatışmayan `<div
class="gm-item-indicator"></div>` (avatar-dan ƏVVƏL) əlavə edildi — indi üç
sütun düzgün yerləşir (4-cü, `auto`/meta sütunu bu sətirdə məzmunsuz qalır,
bu zərərsizdir). Backend (`MailIzlemeService.EkleAsync`) artıq düzgün
işləyirdi — "Ünvan əlavə edildi" bildirişi göstərir ki, bu, sırf GÖRÜNTÜ
(front-end) sütun sürüşməsi idi.

**Qayda: paylaşılan grid-əsaslı klassı (`.gm-item` kimi) başqa bir səhifədə
TƏKRAR işlədəndə, həmin grid-in GÖZLƏDİYİ bütün sütun sayını (burada: 4)
tutuşdur — əskik uşaq elementi (bura: `.gm-item-indicator`) grid-i sındırmır,
sükutla QALAN bütün sütunları SÜRÜŞDÜRÜR. Bu, «paylaşılan CSS klassını
başqa bir markup-da işlətmək» riskinin YENİ bir forması idi — əvvəlki
«başlıq təkrarı» tələsi kimi, kodda heç bir xəta yoxdur, yalnız ekranda
görünür.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (CSS grid sütun sayı ilə markup-dakı uşaq element sayı
tutuşduruldu). İstifadəçi real brauzerdə yoxlamalıdır: yeni ünvan əlavə
edib siyahıda avatar dairəsi + ad/email mətninin düzgün yan-yana
göründüyünü təsdiqləməli.

## Qoşma Önizləməsi — Titan-vari Modal (12.10.2026)

İstifadəçi Titan (xarici poçt müştərisi) ilə FinNex-in qoşma önizləməsini
müqayisə edib göndərdi: *"bizde bele gosterir. ama titanda gor nece
seliqelidir. nece qeseng acilir. bizde bele bir sey ede bilerik?"* — Titan-da
PDF qoşmaya klikləyəndə təmiz bir modal pəncərədə birbaşa render olunur
(yüklə/çap et/bağla düymələri ilə), FinNex-də isə "göz" düyməsi sadəcə
çıxarılmış **xam mətni** (`CixarilmisMetin`) kart altında açıb-bağlayırdı —
PDF-in özü heç görünmürdü.

**Həll — yeni, paylaşılan modal (`gm-preview-overlay`):**

1. **Yeni endpoint — `QosmaBax`** (`GelenMailController`, `GedenMailController`,
   hər ikisi). Mövcud `QosmaYukle`-dən fərqi: `File(bytes, contentType, faylAdi)`
   əvəzinə **`File(bytes, contentType)`** — fayl adı verilmədiyi üçün ASP.NET Core
   `Content-Disposition: attachment` başlığı YAZMIR, brauzer PDF/şəkli
   `<iframe>`/`<img src>` daxilində **İNLINE** göstərə bilir. Hər ikisi mövcud
   `QosmaYukleAsync(qosmaId, sahibUserId)` servis metodunu (eyni mülkiyyət
   yoxlaması) çağırır — yeni icazə məntiqi YAZILMADI.
2. **Yeni paylaşılan JS — `wwwroot/js/User/gm-attachment-preview.js`.**
   Modal DOM-u lazımi anda JS-in özü qurur (Razor-da təkrarlanan modal
   markup-u yoxdur) — `.gm-preview-btn` klikinin `data-*` atributlarını
   (`name`, `ext`, `view`, `download`, `textid`) oxuyur: `.pdf` → `<iframe>`,
   şəkil genişlənmələri → `<img>`, əks halda (varsa) çıxarılmış mətni
   `<pre>`-də göstərir, heç biri yoxdursa "önizləmə dəstəklənmir + Yüklə"
   mesajı. Klik `document`-ə delegasiya ilə tutulur ki, AJAX-la sonradan
   əlavə olunan düymələr də işləsin.
3. **CSS — `gelen-mail.css`-ə `.gm-preview-*` bloku əlavə edildi** (overlay,
   dialoq, başlıq, iframe, şəkil çərçivəsi, boş-nəticə vəziyyəti) — mövcud
   `--gm-border`/`--gm-text-muted` kimi dəyişənləri təkrar işlədir.
4. **Dörd yer yeniləndi** (bu modulun artıq sənədləşmiş qaydasına uyğun —
   bax "Mail Qoşmalarını Açmaq/Yükləmək" bölməsi): `GelenMail/_MailDetail.cshtml`,
   `GelenMail/Detail.cshtml`, `GedenMail/_MailDetail.cshtml`,
   `GedenMail/Detail.cshtml`. Köhnə `onclick="toggleText(...)"` + görünən/gizli
   `.gm-attachment-text` blokları silindi, əvəzinə həmişə görünən "Önizlə"
   düyməsi (əvvəl yalnız `CixarilmisMetin` varsa görünürdü — indi BÜTÜN
   qoşmalarda, çünki PDF/şəkil önizləməsi çıxarılmış mətndən ASILI DEYİL).

⚠️ **Script faylı AJAX-la YENİDƏN YÜKLƏNƏN split-panel-in İÇİNƏ YOX, panel-i
daşıyan `Index.cshtml`-in ÖZÜNƏ qoyuldu.** `GelenMail/Index.cshtml` və
`GedenMail/Index.cshtml`-in sağ paneli (`#gmRightPanel`) hər mail klikində
`fetch` ilə `_MailDetail.cshtml`-i gətirir və `executeScripts()` ilə
paneldəki HƏR `<script>` teqini YENİDƏN icra edir (əks halda `innerHTML`
daxilindəki script işləməz). `gm-attachment-preview.js`-i panelin İÇİNƏ
qoysaydıq, hər mail dəyişəndə fayl yenidən icra olunub `document`-ə TƏZƏ bir
click-listener əlavə edərdi — funksional xəta yaratmazdı (listener-lər
idempotentdir, sadəcə `open()` eyni modalı yenidən açardı), amma onlarla
mail açıldıqca onlarla "ölü" listener yığılardı. Script `Index.cshtml`-in
ÖZÜNDƏ (AJAX-la yenilənməyən hissədə) — bir dəfə yüklənir.

**Qayda: paylaşılan JS-i AJAX-la dəfələrlə yenidən yüklənən bir partial-a
YOX, onu əhatə edən, bir dəfə yüklənən səhifəyə qoy** — xüsusən faylın
özü `document`-ə qlobal event listener qeydiyyatdan keçirirsə (bura: klik
delegasiyası). Əks halda hər panel yeniləməsi görünməyən, yavaş-yavaş
yığılan bir "listener sızması" yaradır.

⚠️ **PDF/şəkil-dən BAŞQA fayl növləri (docx, xlsx və s.) hələ brauzerdə
render olunmur** — yalnız (varsa) çıxarılmış mətni göstərir, olmadıqda
"önizləmə dəstəklənmir, yüklə" mesajı verir. Bu, qəsdən MƏHDUD qapsamdır
(Titan da əslində yalnız PDF-i belə göstərir, ekran-görüntüsündə xlsx/docx
nümunəsi yox idi) — docx/xlsx-i brauzerdə render etmək ayrıca, ağır
kitabxana (məs. SheetJS) tələb edər, bu sessiyada edilmədi.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (JS Node ilə sintaksis yoxlamasından keçdi, hər dəyişən `.cshtml`/
`.cs` faylında mötərizə sayı tutuşduruldu). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real mühitdə yoxlamalıdır: PDF qoşmaya "Önizlə"
basanda modal açılıb faylın iframe-də göründüyünü, şəkil qoşmasında
`<img>`-in göründüyünü, "Yüklə" düyməsinin ayrıca işlədiyini və Çap
düyməsinin (yalnız PDF-də görünür) işə düşdüyünü təsdiqləməli.

### PDF Modalda Göstərmirdi — Qlobal `X-Frame-Options: DENY` Öz İframe-ini Bloklayırdı (12.10.2026, KRİTİK)

İstifadəçi yuxarıdakı modalı test edəndə: *"pdf -de gostermir"* — PDF qoşmaya
basanda modal açılırdı, amma iframe-in içində boş, sındırılmış-fayl ikonu
görünürdü (həm PDF, həm sonra göstərilən .docx önizləməsi).

**Kök səbəb:** `FinNex.UI/Middleware/SecurityHeadersMiddleware.cs` HƏR
cavaba qlobal `X-Frame-Options: DENY` + `Content-Security-Policy:
frame-ancestors 'none'` yazır (clickjacking qoruması). Bu başlıqlar "bu
cavab HEÇ BİR səhifənin iframe-i daxilində göstərilə bilməz" deməkdir —
**öz saytımızın öz iframe-i də daxil olmaqla**. `QosmaBax` cavabı bu
qlobal başlıqları daşıdığı üçün brauzer öz `<iframe src="...QosmaBax/5">`-
imizi özü bloklayırdı.

**Bu, YENİ kəşf edilən tələ deyildi — layihədə artıq həll edilmiş presedent
var idi**, sadəcə görmədim: `SenedDovriyyesi/SenedController.Preview`
(Sənəd Dövriyyəsi modulunun öz PDF-önizləmə endpoint-i) məhz bu problemi
əvvəlcədən konkret bu iki başlığı **override edərək** həll edib:

```csharp
Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";
```

Middleware bu başlıqları `_next(context)`-dən ƏVVƏL yazır, controller
action isə SONRA işləyir — `Response.Headers[...] = ...` overwrite kimi
işləyir (nə `Add` kimi dublikat xətası, nə "header artıq göndərilib" xətası,
çünki body hələ flush olunmayıb). Eyni naxış `QosmaBax`-a (hər iki
controller) köçürüldü, əlavə olaraq `Content-Disposition: inline;
filename="..."` əl ilə yazıldı (əvvəl `File()`-a fayl adı VERİLMİRDİ ki,
ASP.NET avtomatik "attachment" yazmasın — indi əvəzinə fayl adı HƏLƏ DƏ
`File()`-a verilmir, amma `Content-Disposition` əl ilə "inline" olaraq
yazılır, PDF viewer-ə fayl adını da ötürür).

**Qayda: "iframe-də boş/sınmış ikon görünür, şəbəkə xətası yoxdur" simptomu
əvvəlcə CSP/`X-Frame-Options`-u yoxlasın** — bu, "fayl tapılmadı" və ya "yanlış
MIME tipi"ndən FƏRQLİ bir uğursuzluq sinfidir: server 200 qaytarır, amma
BRAUZER cavabı GÖSTƏRMƏKDƏN özü imtina edir, DevTools konsolunda isə adətən
sakit bir "Refused to display '...' in a frame because it set
'X-Frame-Options' to 'deny'" xətası olur. Layihədə **qlobal təhlükəsizlik
middleware-i olan hər yerdə**, öz-sayt daxili iframe/embed əlavə edəndə bu
presedenti axtar (`grep -rn "X-Frame-Options" FinNex.UI`) — təzədən
kəşf etməyə ehtiyac yoxdur.

### .docx/.xlsx Qoşmaları da Modalda Formatlı Göstərilir (12.10.2026)

Eyni mesajda istifadəçi daha bir fərq göstərdi: Titan-da .docx qoşma
açılanda formatlı (başlıqlar, cədvəllər) görünür, bizdə isə "göz" düyməsi
çıxarılmış XAM MƏTNİ (`CixarilmisMetin` — Word-ün daxili `PAGEREF`/TOC sahə
kodları da daxil, formatsız bir divar) göstərirdi.

**Həll — brauzerdə client-side çevirmə, server dəyişmədi:**
- **`.docx`** → **mammoth.js** (`cdn.jsdelivr.net/npm/mammoth@1.8.0`) —
  `QosmaBax`-dan gələn xam baytları (`fetch().arrayBuffer()`)
  `mammoth.convertToHtml({arrayBuffer})` ilə semantik HTML-ə (başlıqlar,
  paraqraflar, cədvəllər, qalın/mail) çevirir, `.gm-preview-docx`
  konteynerində göstərir.
- **`.xlsx`/`.xls`** → **SheetJS (`xlsx@0.18.5`)** —
  `XLSX.read(buf,{type:'array'})` + `XLSX.utils.sheet_to_html(sheet)`;
  birdən çox vərəq varsa yuxarıda tab düymələri (`.gm-preview-xlsx-tabs`)
  göstərilir, SheetJS **köhnə binar `.xls`-i də** oxuya bilir (mammoth
  köhnə `.doc`-u OXUYA BİLMİR — bax aşağı).
- Hər iki kitabxana **kəşf olunduqda (lazy) bir dəfə** yüklənir
  (`loadScript` + `libPromises` keşi) — modal hər açılanda təkrar
  yüklənmir, həm də əvvəllər heç bir docx/xlsx açılmayıbsa səhifə ilk
  yüklənəndə əlavə ağırlıq gətirmir.
- Çevirmə/şəbəkə xətası olarsa (ən çox səbəb: köhnə binar `.doc` — mammoth
  yalnız OOXML `.docx` oxuyur) **səssizcə sınmır** — `catch` bloku (varsa)
  çıxarılmış mətnə, olmasa "ekranda açıla bilmədi, yüklə" mesajına düşür.

⚠️ **CSP-nin `script-src`-i artıq `https://cdn.jsdelivr.net`-ə icazə
verirdi** (`marked.min.js` AI xülasə render-i üçün onsuz da işlədilirdi) —
mammoth/xlsx üçün middleware-ə TOXUNMAQ LAZIM OLMADI, eyni icazəli host
kifayət etdi. Yeni bir CDN host əlavə etsəydik, `SecurityHeadersMiddleware`-
in `script-src` sətrinə də əlavə etmək LAZIM OLARDI — yoxsa skript
səssizcə CSP tərəfindən bloklanardı (eyni "görünən xəta yox, brauzer özü
imtina edir" sinfi).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (C# mötərizə/dırnaq balansı, JS Node sintaksis yoxlaması).
İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real mühitdə
yoxlamalıdır: PDF-in indi iframe-də göründüyünü, .docx-in formatlı (cədvəl/
başlıqlarla) açıldığını, çoxvərəqli .xlsx-də tab-ların işlədiyini və köhnə
binar `.doc` faylında səliqəli "açıla bilmədi" mesajının çıxdığını (sükutla
sınmadığını).

### Modal Genişləndirildi + Yüklə Düyməsi "Kölgə" Flikeri (12.10.2026)

İstifadəçi Titan-ın PDF görüntüləyicisini (geniş, demək olar tam ekran) göstərib
soruşdu: *"imkan varda bunu biraz enlede bilerikde, 2ci sekil kimi acila biler?"*
+ ikinci şikayət: *"birde yuklemek olmur onada baxa, klik edirem, nese birsey
kolge kimi ekrana gelir gedir"*.

**1. Genişlik.** `.gm-preview-dialog` əvvəl `width: min(920px, 100%)` idi —
dar qalırdı. İndi `width: min(1440px, 97vw); height: 96vh` (əvvəl yalnız
`max-height: 92vh` idi, sabit hündürlük yox idi) — overlay-in kənar boşluğu
`24px`-dən `14px`-ə endirildi ki, pəncərə Titan-ın göstərdiyi kimi demək olar
tam ekranı tutsun. `.gm-preview-iframe`-in `min-height`-ı 72vh→80vh.

**2. "Kölgə kimi gəlir-gedir" — Yüklə düyməsindəki `target="_blank"`.**
Kök səbəb: `gm-preview-download` linki `target="_blank"` ilə yeni tab açırdı.
`QosmaYukle` cavabı `Content-Disposition: attachment` yazdığı üçün brauzer
həmin yeni tabda HEÇ NƏ göstərmədən dərhal yükləməni başladır və tabı
avtomatik bağlayır (Chrome-un bu davranışı) — istifadəçiyə bu, ekranda bir
anlıq boş/qara "kölgə" kimi görünür. Fayl əslində yüklənirdi (funksional
xəta yox idi), amma görüntü çaşdırıcı idi.

**Düzəliş:** `target="_blank"` SİLİNDİ — Content-Disposition artıq "attachment"
olduğu üçün brauzer naviqasiya etmədən, cari səhifədə qalaraq sadəcə faylı
yükləyir (standart brauzer davranışı). Əlavə qoruyucu: `download="@fayl_adı"`
atributu da JS-dən dinamik təyin olunur — Content-Disposition-un fayl adı
hər hansı səbəbdən itərsə, brauzer local `download` atributundan adı götürür.

**Qayda: server artıq `Content-Disposition: attachment` yazırsa, linkə
`target="_blank"` ƏLAVƏ ETMƏ** — bu, lazımsız bir tab açıb-bağladır və
istifadəçiyə "nəsə pozulub" təəssüratı yaradır. Sadə `<a href="...">`
(yeni pəncərə YOX) faylı cari səhifədən çıxmadan yükləyir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (CSS mötərizə balansı, JS Node sintaksis yoxlaması). İstifadəçi
real brauzerdə yoxlamalıdır: modal pəncərəsinin indi daha geniş/hündür
açıldığını və "Yüklə" düyməsinin heç bir yeni tab açmadan faylı birbaşa
yüklədiyini.

### "İşçiyə Tapşır" Defolt Bağlı + Sidebar-da "Mail" Öz Etiketi Altına Keçdi (12.10.2026)

İstifadəçi (əvvəlki düzəlişi "mohtesem" adlandırandan sonra) iki kiçik görüntü
tələbi verdi: *"indi isciye tapsir hissesi daim aciq gelmesin istese klik ile
acib isledsin"* + *"birdeki solda mail ucun AI modullarinin ustunde Mailler
adi altina sal"*.

**1. «İşçiyə Tapşır» — `<div>` → `<details>`/`<summary>`.** Split-panel-dəki
(`GelenMail/_MailDetail.cshtml`) tapşırma bölməsi həmişə açıq görünürdü,
mail panelinin aşağısını doldururdu. Ən sadə, JS-siz həll: native HTML
`<details>` elementi — defolt BAĞLIDIR, `<summary>`-ə klik açır/bağlayır,
heç bir listener yazmağa ehtiyac yoxdur. Başlığa (`<summary>`) artıq, əgər
`TapalanIsciler` boş deyilsə, **"(N nəfər)"** sayğacı əlavə edildi ki, panel
bağlı olanda belə kimin tapşırıldığı görünsün — açmaq MƏCBURİ deyil, yalnız
redaktə/yeni tapşırma üçün lazımdır. Fırlanan ox ikonu (`.gm-panel-assign-
chevron`, `details[open]` seçicisi ilə 180° rotasiya) CSS-dədir.

⚠️ **Yalnız split-panel dəyişdi** — `GelenMail/Detail.cshtml` (tam səhifə)-
dəki OXŞAR bölmə fərqli markup-dadır (`gm-card-header`, `gm-panel-assign`
DEYİL) və istifadəçi onu göstərmədi/şikayət etmədi; tam səhifədə yer
problemi yoxdur (normal skroll var). Toxunulmadı.

**2. Sidebar — "Mail" öz etiketi altına, "AI Modulları"ndan ƏVVƏL.**
Kök səbəb: `Gələn/Göndərilən Mail + Mail İzləmə` linkləri kodda **Rəhbər/
Admin blokundan (AI Modulları qrupunu da əhatə edən) SONRA**, öz başlıqları
OLMADAN yazılmışdı. "AI Modulları" isə **kolaps olunan** qrupdur
(`fn-nav-g`) — cari səhifə onun öz alt-səhifələrindən biri olmayanda
(`aiAnyActive=false`) body-si gizlənir, ekranda YALNIZ "AI Modulları"
başlığı qalır. Nəticə: mail linkləri vizual olaraq elə "AI Modulları"
başlığının altında aşağı sıralanırdı — sanki o qrupun bir hissəsiymiş kimi.

**Düzəliş:** mail bloku (`@if (hasMail)`) Dashboard linkindən DƏRHAL sonraya,
Rəhbər/Admin blokundan (deməli AI Modulları qrupundan da) ƏVVƏLƏ köçürüldü,
başına `<div class="fn-nav-label">Mail</div>` (mövcud "Əsas" etiketi ilə
EYNİ klass) əlavə edildi. `hasMail` şərti TOXUNULMADI — mail linkinin
Rəhbər/Admin blokundan müstəqil qalması qaydası ("mail_istifade" icazəsi
olan adi işçi də görməlidir) qorundu, sadəcə YERİ dəyişdi.

**Qayda: kolaps olunan bir naviqasiya qrupunun (`fn-nav-g`) ARDINCA, öz
başlığı olmayan bir blok yazma** — qrup bağlı görünəndə (defolt vəziyyət)
həmin blok vizual olaraq elə qrupun öz hissəsi kimi oxunur, baxmayaraq ki
kodda ayrı, şərtsiz bir elementdir. Hər məzmun blokunun öz `fn-nav-label`-i
(və ya oxşar ayırıcı başlığı) olmalıdır, "qonşu qrupun kölgəsinə" güvənmə.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (Razor `@if`/`{}` balansı, HTML `<details>`/`</details>` cütü).
İstifadəçi real brauzerdə yoxlamalıdır: "İşçiyə Tapşır"-ın bağlı açıldığını
və kliklə düzgün açılıb-bağlandığını, sidebar-da "Mail" etiketinin
"AI Modulları"ndan ƏVVƏL göründüyünü.

### "Mail" Etiketi → Kolaps Olunan Qrup (12.10.2026, eyni gün, davam)

İstifadəçi yuxarıdakı düzəlişdən bir az sonra daha dəqiq nə istədiyini
göstərdi: *"Mail -combobox kimi et, bagli gelsin, iseyende acsin."* — yəni
sadə `fn-nav-label` (statik mətn başlığı, "Əsas" kimi) kifayət deyildi;
istifadəçi "AI Modulları" ilə EYNİ davranışı istəyirdi — qapalı başlayan,
klikləndə açılan bir qrup.

**Həll:** "Mail" bloku sadə `<div class="fn-nav-label">` əvəzinə mövcud
`fn-nav-g` kolaps qrupu naxışına (`fn-nav-g-head` düyməsi + `fn-nav-g-body`)
köçürüldü — bu, "AI Modulları"nın özünün də istifadə etdiyi, artıq
sınanmış, paylaşılan bir komponentdir. **Heç bir yeni JS yazılmadı** —
`_UserLayout.cshtml`-in sonundakı ortaq skript artıq SƏHİFƏDƏKİ BÜTÜN
`.fn-nav-g-head` düymələrinə klik-toggle əlavə edir
(`document.querySelectorAll('.fn-nav-g-head')`), ona görə yeni button
avtomatik işə düşür.

Defolt açıq/bağlı vəziyyət digər qruplarla EYNİ qaydaya tabedir: yeni
`mailAnyActive` dəyişəni (`currentController is "GelenMail" or "GedenMail"
or "MailIzleme"`) — istifadəçi artıq mail səhifələrindən birindədirsə qrup
AÇIQ başlayır (məs. "Gələn Maillər"i açıbsa sidebar-da "Mail" artıq bağlı
görünməsin), əks halda bağlıdır. Elə `aiAnyActive`-in "AI Modulları" üçün
etdiyi işin eynisi.

⚠️ **Top-nav-dakı ayrıca oxunmamış say nişanı (zərf ikonu, `#gelenMailBtn`)
TOXUNULMADI** — o, sidebar-dakı `fn-mail-badge`-dən TAMAMİLƏ MÜSTƏQİL bir
göstəricidir (header-in sağ üstündə, həmişə görünür). Yəni "Mail" qrupu
bağlı olanda belə, oxunmamış say top-nav-da görünməyə davam edir —
istifadəçi info itirmir, qrupu açmaq MƏCBURİ deyil.

**Qayda: istifadəçi "combobox kimi", "AI Modulları kimi" deyə mövcud bir
UI naxışına açıq işarə edəndə, oxşar GÖRÜNTÜ yaratmaq kifayət etmir —
HƏMİN NAXIŞIN ÖZÜNÜ (eyni CSS class-ları, eyni JS mexanizmi) təkrar
istifadə et.** Fərqli bir həll (məs. `<details>`, yeni bir toggle class)
işləsə belə, istifadəçinin "bu artıq tanıdığım davranış olsun" gözləntisini
tam qarşılamır və sidebar-da iki fərqli kolaps mexanizmi yaradardı.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (Razor mötərizə balansı, mövcud `fn-nav-g` nümunələri ilə
markup-un eyniliyi tutuşduruldu). İstifadəçi real brauzerdə yoxlamalıdır:
"Mail" başlığının indi "AI Modulları" kimi bağlı başladığını, kliklə
açılıb-bağlandığını, mail səhifələrindən birindəykən avtomatik açıq
gəldiyini.

### Üç İş — Combobox Sərhədi, "Hamısını Oxundu Et", Pagination "Əvvəlki" Bugu (12.10.2026)

İstifadəçi eyni gün üç ayrı qüsur bildirdi: *"comboboxu acanda ona aid
olanlar bilinmir."* + *"bizde hamisi oxundu imkani mumukundur?... ilk
acanda butun mailler oxunmamis gelir mecbur hamisini oumaq lazim
gelecek, bu vaxt itgisidir."* + *"novbetini vurdum sehife yenilendi ama
evvelki hara getdi? burda nese uygunsuzluq var."*

**1. Combobox sərhədi görünmürdü — `.fn-nav-g-body`-yə sol xətt + çəkilmə
(`topnav.css`).** Yuxarıdakı "Mail" qrupunu (və bütün digər `fn-nav-g`
qruplarını — "AI Modulları" daxil) açanda, daxili linklər qrupdan SONRA
gələn ayrı elementlərdən vizual olaraq FƏRQLƏNMİRDİ (eyni padding, eyni
fon) — "AI Modulları → HR Dashboard" keçidi ilə eyni köhnə tələ, sadəcə
İNDİ "Mail → Rəhbər Panel" sərhədində. **Qlobal CSS düzəlişi** (bütün
`fn-nav-g` instansiyalarına aiddir, təkcə Mail-ə yox): `.fn-nav-g-body`-yə
`border-left: 2px solid rgba(255,255,255,.12)` + `margin-left: 10px`
əlavə edildi — açıq qrupun daxili linkləri indi görünən bir "qutu/xətt"
içindədir. Nested sub-qruplar (Davamiyyət/Məzuniyyət) bu xəttdən İSTİSNA
edildi (`.fn-nav-g-body--nested`) — onların öz fon-zolaqlı başlığı
(`.fn-nav-g-head--sub`) artıq kifayət edir, ikiqat sərhəd lazımsızdır.

**2. "Hamısını oxundu et" — yeni funksiya.** `IGelenMailService.
HamisiniOxunduIsareEtAsync(sahibUserId)` (+ implementasiya) — işçinin
`Silinib=false && Oxunub=false` olan BÜTÜN maillərini TƏK `SaveChanges`
çağırışında oxunmuş işarələyir (`OxunduIsareEtAsync`-in tək-qeyd versiyası
ilə EYNİ şərt, təkcə kütləvi). `GelenMailController.
HamisiniOxunduIsareEt` POST endpoint-i, `GelenMail/Index.cshtml`-də
YALNIZ `oxunmamisSay > 0` olanda görünən "Hamısını oxundu et" düyməsi
("Yenilə" düyməsinin yanında, EYNİ fetch+token naxışı). Təsdiq dialoqu
(`confirm`) var — kütləvi, geri-qaytarılması çətin bir dəyişiklikdir
(hər mailin `OxunmaTarixi`-si itir). Uğurlu olduqda səhifə yenilənir —
həm bu səhifənin "74" nişanı, həm top-nav-dakı zərf nişanı (ayrı, müstəqil
`fn-mail-badge` AJAX sorğusu) avtomatik yeni (0-a yaxın) rəqəmi göstərir,
əlavə sync kodu yazmağa ehtiyac olmadı.

⚠️ **`GedenMail`/`MailIzleme`-yə ƏLAVƏ EDİLMƏDİ** — "oxunmamış" konsepti
yalnız Gələn Mailə aiddir (göndərilən/izlənən maillərdə "oxunub" sütunu
onsuz da yoxdur/mənasızdır).

**3. Pagination — "Əvvəlki" BÜTÖV panellə birlikdə yoxa çıxırdı (KRİTİK,
hər iki `GelenMail/Index.cshtml` VƏ `GedenMail/Index.cshtml`-də EYNİ
bug).** Kök səbəb: `@if (Model.Count == 50) { ... BÜTÖV pagination div,
HƏM "Əvvəlki", HƏM "Növbəti" ... }` — "Növbəti"nin görünməsi üçün məntiqli
şərt idi (50 nəticə = daha da ola bilər), AMMA eyni şərt "Əvvəlki"ni DƏ
gizlədirdi. Nəticə: istifadəçi son səhifəyə (adətən 50-dən AZ nəticəli)
keçən kimi BÜTÖV naviqasiya paneli yoxa çıxırdı — geri qayıtmağın HEÇ BİR
UI yolu qalmırdı (yalnız brauzerin öz "geri" düyməsi). Bu, elə istifadəçinin
gördüyü "Növbətini vurdum, Əvvəlki hara getdi" simptomudur.

**Düzəliş:** panel indi `currentPage > 1 || Model.Count == 50` şərti ilə
göstərilir (istənilən tərəfə getmək mümkündürsə panel görünür), "Əvvəlki"
öz müstəqil `currentPage > 1` şərtini saxlayır, "Növbəti" isə ayrıca
`Model.Count == 50`-ə köçürüldü — iki düymə artıq BİR-BİRİNDƏN ASILI
DEYİL, biri olmadan o biri də yoxa çıxmır.

**Qayda: "Növbəti üçün düzgün olan şərt, "Əvvəlki" üçün də düzgündür"
fərziyyəsi SƏHVDİR** — ikisi fərqli sualların cavabıdır ("bundan sonra
daha var?" vs "bundan əvvəl artıq görmüşəm"). Hər pagination düyməsini
AYRI şərtlə qoru, ortaq bir `@if`-in içinə hər ikisini atma — əks halda
biri digərini özü ilə birlikdə gizlədə bilər.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (C#/Razor/CSS mötərizə balansı, dörd `<script>` blokunun
hamısı Node ilə ayrıca sintaksis yoxlamasından keçdi). İstifadəçi real
brauzerdə yoxlamalıdır: "Mail" qrupu açılanda sol xəttin göründüyünü,
"Hamısını oxundu et"in həqiqətən bütün maili oxunmuş etdiyini, son
səhifədə "Əvvəlki" düyməsinin artıq görünməyə davam etdiyini.

### Xlsx Qoşma Önizləməsi — SheetJS Pulsuz Versiyası Hüceyrə Stilini Saxlamır (12.10.2026)

İstifadəçi Titan-ın (xarici poçt müştərisi) bir `.xlsx` qoşmasını necə göstərdiyini
ekran-görüntüsü ilə göstərdi (tünd başlıq banner-i, açıq-göy başlıq sətri, nizamlı
sərhədlər, uzun mətnli xanalarda sətir keçmə) və FinNex-in eyni faylı AÇIQ RƏNG-SİZ,
büzülməmiş xam cədvəl kimi göstərdiyini müqayisə etdi: *"1- bu titandaki. 2- bizdeki.
bizde bele seliqeli ede bilerik?"*

**Kök səbəb:** `gm-attachment-preview.js`-in `renderXlsx` funksiyası
`window.XLSX.utils.sheet_to_html(sheet, {...})` ilə birbaşa SheetJS-in ÇIXARDIĞI
xam HTML-i göstərirdi. **SheetJS-in pulsuz (CDN-dən yüklənən `xlsx.full.min.js`)
versiyası mənbə faylın hüceyrə STİLİNİ (fon rəngi, şrift qalınlığı, rəng) OXUMUR
— bu, pullu (Pro) versiyaya məxsus xüsusiyyətdir.** Yəni Excel-in özündəki tünd
banner/açıq-göy başlıq rəngləri kitabxananın çıxardığı datada ümumiyyətlə yoxdur,
kodun səhvi deyil, kitabxananın məhdudiyyətidir.

**Həll — rəng DEYİL, QURULUŞ üzrə TƏXMİN edilmiş görüntü:**
`gm-attachment-preview.js`-ə `polishXlsxTable(table)` funksiyası əlavə olundu
(`renderXlsx`-in hər vərəq döngəsində, `sheet_to_html`-dən sonra çağırılır):
- Birinci sətir **tək, bütün sütunları örtən BİRLƏŞDİRİLMİŞ xanadırsa** (SheetJS
  `!merges`-i `colspan`-a çevirdiyi üçün bu, strukturdan bilinir) —
  `gm-xlsx-title-row` sinfi ilə tünd-göy, mərkəzləşdirilmiş banner kimi göstərilir;
- Ondan sonrakı (və ya birləşmə yoxdursa, birinci) sətir `gm-xlsx-header-row`
  sinfi ilə açıq-göy fon + qalın şrift + `position:sticky` (skrollda yuxarıda qalır);
- Qalan sətirlərə bir-bir keçən `gm-xlsx-row-alt` zebra-zolağı.

`gelen-mail.css`-də uyğun üç sinif + ümumi xana stili yeniləndi: sərhədlər
`#dde3ea`-ya incələşdi, kölgəli ağ cədvəl fonu + boz səhifə arxa fonu (kart kimi
görünüş), **`white-space: nowrap` → `white-space: normal` + `word-break` +
`max-width:440px`** (Titan-ın uzun mətnli, sətir keçən izah xanalarını — "Status
üzrə interpretasiya" kimi — indi FinNex də eyni cür göstərir, əvvəl bu mətnlər
üfüqi sürüşmə ilə bir sətirdə qalırdı).

**Qayda: "başqa proqram daha gözəl göstərir" şikayətində əvvəlcə işlədilən
kitabxananın PULSUZ versiyasının MƏHDUDİYYƏTLƏRİNİ yoxla** — rəng/stil məlumatı
mənbədə olsa belə, oxuyan kitabxana onu heç parsing mərhələsində saxlamırsa, CSS
tərəfində "düzəltmək" mümkün deyil (məlumatın özü yoxdur). Bu halda **STRUKTURDAN
(birləşmə, sətir sırası) çıxan heç still TƏXMİNİ** real rəngə ən yaxın, əldə
oluna bilən alternativdir — istifadəçiyə bunun TƏXMİN olduğunu (yəni mənbədəki
əsl rəng DEYİL) açıq demək lazımdır, "indi tam eynidir" demək yanlış olardı.

⚠️ **Bu, HƏR Excel faylında dəqiq Titan kimi görünməyəcək** — tək-sətir
birləşməsi olmayan, yaxud başlığı ikinci/üçüncü sətirdə olan fayllarda heuristika
səhv təxmin edə bilər (məs. birləşmə yoxdursa, sadəcə BİRİNCİ sətir header kimi
işarələnir — bu, əksər cədvəl faylında düzgün nəticə verir, amma qayda deyil).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(`node -c` ilə JS sintaksisi, CSS-də mötərizə balansı). İstifadəçi real brauzerdə
"Özünüqiymətləndirmə cədvəli" faylını yenidən açıb göstərməlidir: sərlövhə
banner-inin tünd-göy, başlıq sətrinin açıq-göy fonla göründüyünü, uzun mətnli
xanaların artıq sətir keçdiyini (üfüqi sürüşmədən çıxdığını) təsdiqləməli.

## Gələn/Göndərilən Mail Saatı — UTC Yazılır, Server Yerli Vaxtını Göstərir (12.10.2026, KRİTİK)

İstifadəçi ekran-görüntüsü ilə göstərdi: Titan-da (poçt müştərisi) mail **15:44**-də
gəlib, FinNex-in "Gələn Maillər" siyahısında isə **11:44** göstərilirdi — dəqiq
**4 saat** fərq, Azərbaycanın UTC+4 ofsetinə bərabər.

**Kök səbəb:** `GelenMailImapSyncer.cs:75` mail tarixini **`msg.Date.UtcDateTime`**
ilə yazırdı (MailKit-in `DateTimeOffset`-ini UTC-yə çevirib saxlayırdı), **tam
yanındakı sətir isə** (`YaradilmaTarixi = DateTime.Now`) server **YERLİ** vaxtını
yazırdı — eyni entity-nin iki qonşu sahəsi iki fərqli konvensiyada idi.
`GedenMailImapSyncer.cs:97` (`GonderilmeTarixi`) **eyni bugu güzgü kimi
təkrarlayırdı** (bu modul `GelenMail`-in güzgüsüdür — bax yuxarı "Göndərilən
Mail + Mail İzləmə" bölməsi). Göstərmə qatı (`GelenMail/Index.cshtml:273`,
`@mail.AlinmaTarixi.ToString("dd MMM HH:mm")`) heç bir çevirmə aparmır — bazadakı
dəyəri **olduğu kimi** çap edir.

**Layihənin ümumi konvensiyası budur ki, serverin özü artıq Azərbaycan vaxt
qurşağındadır** (`DateTime.Now`/`DateTime.Today` bütün digər modullarda —
Payroll, Davamiyyət, Məzuniyyət və s. — HEÇ BİR tərcümə olmadan birbaşa işlədilir,
layihədə `TimeZoneInfo` əsaslı heç bir çevirmə mexanizmi yoxdur). Yəni "yerli
vaxt göstərmək" üçün UTC-dən geri çevirmək LAZIM DEYİL — sadəcə UTC-yə
çevirməmək kifayətdir. IMAP sinxronu isə `msg.Date` (e-poçtun öz `Date:`
başlığı, doğru ofsetli `DateTimeOffset`) üzərində **əlavə** `.UtcDateTime`
çağırışı ilə bu konvensiyanı pozmuşdu.

**Düzəliş — iki sətir, ikisi də `.UtcDateTime` → `.LocalDateTime`:**
```csharp
AlinmaTarixi = msg.Date.LocalDateTime,       // GelenMailImapSyncer.cs:75
GonderilmeTarixi = msg.Date.LocalDateTime,   // GedenMailImapSyncer.cs:97
```
`DateTimeOffset.LocalDateTime` mesajın öz ofsetindən (göndərənin saatından,
nəyi olursa olsun) **serverin sistem vaxt qurşağına** çevirir — nəticə `YaradilmaTarixi`
ilə eyni konvensiyada olur, göstərmə qatına toxunmaq lazım deyil.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR.** Sinxron `knownSet.Contains(msgId)) continue;` ilə
artıq bilinən `MessageId`-ləri **yenidən oxumur** — bu fix YALNIZ bundan sonra
ilk dəfə sinxronlaşan (yeni gələn/göndərilən) mailləri düzəldir. Bazada onsuz da
olan köhnə sətirlərin `AlinmaTarixi`/`GonderilmeTarixi`-si UTC-də qalıb (4 saat
geri görünməyə davam edəcək).

**Real hadisə (eyni gün, DƏRHAL): istifadəçi kodu deploy etdi, eyni "11:44"
sətrini yenidən göstərdi — "yenə 11:44-dür".** Bu, REQRESSIYA DEYİL, yuxarıdakı
geriyə-tətbiq-olunmama qaydasının dəqiq gözlənilən nəticəsidir: göstərilən
sətir ELƏ EYNİ mail idi (eyni mövzu, eyni göndərən) — fix-dən ƏVVƏL artıq
sinxronlaşmış, deməli `MessageId` `knownSet`-ə düşmüş bir sətir. Yeni kod
yalnız BUNDAN SONRA ilk dəfə gələn/göndərilən mailə təsir edir. Köhnə sətirləri
düzəltmək üçün **`docs/sql/mail/02_Mail_Saat_UTC_Duzelis.sql`** yazıldı —
diaqnostik SELECT-lər + şərh içində UPDATE şablonu (`DATEADD(HOUR, 4, …)`,
yalnız deploy anından ƏVVƏLKİ sətirlərə, WHERE şərtinə öz deploy vaxtını
yazmaq LAZIMDIR ki, düzəlişdən SONRA gələn, artıq doğru sətirlər TƏKRAR
sürüşdürülməsin). Bu sessiyada İCRA EDİLMƏDİ — istifadəçi faylı SQL Server-də
özü işlətməlidir (əvvəlcə yalnız SELECT hissəsini, nəticəni görüb qərar
versin).

**Qayda:** eyni entity-nin iki QONŞU tarix sahəsi (burada: `AlinmaTarixi` və
`YaradilmaTarixi`) fərqli mənbədən (xarici mesaj vs `DateTime.Now`) doldurulursa,
HƏR İKİSİNİN **eyni saat qurşağı konvensiyasında** olduğunu yoxla — xarici
kitabxananın (`MailKit`) "düzgün" göstərdiyi bir API (`.UtcDateTime`) layihənin
ÖZ konvensiyasına (burada: "server = yerli vaxt, çevirmə yoxdur") zidd ola bilər.
Bu, "Razor → CSS/JS Rəqəm — Mədəniyyət" bölməsindəki eyni prinsipin başqa bir
formasıdır: iki qat arasında gizli bir fərziyyə fərqi (mədəniyyət/saat qurşağı)
heç bir xəta vermədən rəqəmi/saatı səssizcə sürüşdürür.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı). İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real
IMAP hesabı ilə (SMTP/IMAP qurulmuş istifadəçi) yoxlamalıdır: yeni gələn/göndərilən
mailin saatının Titan-dakı real saatla (server Azərbaycan vaxt qurşağındadırsa)
tam uyğun gəldiyini.

## Kassa — "MB (CBAR) Kursu" Kartı MÜVƏQQƏTİ YIĞIŞDIRILDI (13.10.2026)

İstifadəçi əvvəlcə ümumi təhlükəsizlik sualı verdi: *"biz ilk dəfə kənardan
məlumat aldıq, cbardan kursları alırıq, bu bizim proqram kənardan kiminsə
girişinə və ya müdaxiləsinə açıq qapı deyil?"* — araşdırma göstərdi ki,
"MB kursu" kartı (bax yuxarı "Kassa — Valyuta Mübadiləsi" bölməsi, 09.10.2026)
əslində cbar.az-a birbaşa müraciət ETMİR — Oracle-dakı (BMI-nin öz bazası)
`kurval`/`odb.func_get_kurval` funksiyasını **oxuyur**, yəni xaricə çıxan
heç bir HTTP çağırışı yoxdur (nə yeni, nə köhnə BMI WinForms kodunda —
hər ikisi araşdırıldı, təsdiqləndi: giriş qapısı açılmayıb).

İstifadəçi bunu eşidəndə aydınlaşdırdı: *"əslində mən həmin kursu cbardan
alınmağını istəmişdim, çünki oracledəki sorğu ya mən əlavə etdiyimdir"* —
yəni `VALYUTA_KURSU` Oracle sorğusunun (`docs/sql/valyuta/Valyuta_Kursu_
OracleSorgu.sql`) ÖZÜ istifadəçinin bu sessiyada (07.09.2026-da, 20 000 USD
limiti üçün) əlavə etdiyi bir sorğudur — yəni kartın göstərdiyi "CBAR kursu"
əslində **canlı/rəsmi CBAR mənbəyi deyil**, sadəcə `func_get_kurval`-ın
(bankın öz köhnə, mənbəyi bilinməyən Oracle funksiyası) qaytardığı rəqəmdir.
Qərar: *"ən yaxşısı, hələlik o kartı yığışdıraq, çünki mənasız olur, yenə
mən yazdığımdır"*.

**Silinən (müvəqqəti, görüntü + ötürmə qatı):**
- `ExchangeController.Index` — `IBmiValyutaService` asılılığı, `UsdKodu`/
  `AvroKodu` sabitləri, `KursAsync` çağırışları silindi (hər səhifə
  açılışında 2 lazımsız Oracle round-trip aradan qalxdı).
- `ExchangeIndexVM.UsdMbKurs`/`AvroMbKurs`/`MbTarix` sahələri silindi.
- `Exchange/Index.cshtml` — `kx-mb-card` bloku (və onun `Mb()` helper
  funksiyası) silindi, yalnız "Təsdiq ekranı" düyməsi qaldı.

**TOXUNULMAYIB — kritik asılılıq var:**
`IBmiValyutaService.KursAsync`-in ÖZÜ (`BmiValyutaService.cs`) silinmədi —
`KocurmeLimit.cs` (Pul Köçürməsi, 20 000 USD aylıq limiti) onu **hələ də
işlədir** və bu, kritik, istifadə olunan bir funksiyadır. CLAUDE.md-nin
"İŞLƏK FUNKSİYANI SİLMƏK — YALNIZ AÇIQ İCAZƏ İLƏ" qaydasına uyğun: yalnız
istifadəçinin konkret icazə verdiyi hissə (Exchange-dəki kart) götürüldü,
servisin özü və onun başqa, kritik istehlakçısı TOXUNULMADI.

**`.kx-mb-*` CSS sinifləri (`exchange.css`) QƏSDƏN SİLİNMƏDİ** — "hələlik"
sözü (müvəqqəti) gələcəkdə əsl CBAR inteqrasiyası qurulsa kartın geri
qaytarılması ehtimalını saxlayır; ölü CSS zərərsizdir, markup onsuz da onu
çağırmır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə/Razor şərh balansı). İstifadəçi Visual Studio-da
**Rebuild All** ilə yoxlamalı — xüsusilə `IBmiValyutaService` DI
qeydiyyatının (`AddApplicationServices.cs`) toxunulmadığını (başqa
servislər onu hələ istifadə edir) və Exchange səhifəsinin kartsız düzgün
açıldığını.

## Kassa — Köhnə `bmi_kassa_kurs` Tarixçəsinin SQL Server-ə İdxalı (15.10.2026)

İstifadəçi tələbi: *"biz exchangdeki kohne bazadaki melumatlari yeniye
kecirtmeliyik"* — BMI-nin köhnə Oracle `bmi_kassa_kurs` cədvəlindəki bütün
tarixi kurs qeydlərini yeni Kassa modulunun (`KassaKursBeyannameleri`/
`KassaKursSetirleri`) özünə köçürmək. İstifadəçi cədvəlin tam export-unu
(`select * from bmi_kassa_kurs`, Excel/.xls) birbaşa verdi — `IOracleService`
üzərindən CANLI sorğu APARILMADI (bu sessiyada Oracle-a bağlantı yoxdur,
üstəlik export artıq əldə idi).

**Mənbə məlumatın quruluşu** (1505 sətir, 2025-03-03 — 2026-10-14,
`xlrd` ilə oxundu — köhnə OLE2 `.xls`, `openpyxl` BUNU AÇA BİLMƏZ, bax
"Excel OXUNUŞU" bölməsi, eyni qayda `.xlsx`-dən əlavə köhnə `.xls` üçün də
keçərlidir): `TARIX, VALYUTA, N_ALIS, N_SATIS, QN_ALIS, QN_SATIS, ICRACI,
APPROVAL, APPROVAL_DATE, APPROVED_BY, MESAJ, INSERT_GROUP_NO`. Təmiz data:
bütün 1505 sətir `APPROVAL='təsdiqləndi'`, İCRACİ həmişə "Axundov Elxan",
APPROVED_BY "Najafi Asghar" (1445) və ya "Ibrahimov Anar" (60). `IRR`/`AED`/
`RUB` sətirlərində alış-satış sütunları HƏMİŞƏ boşdur (yalnız USD/AVRO-da
real dəyər var) — target cədvəlin `NagdAlis` və s. sütunlarının nullable
olması elə bu reallığa görədir.

**`INSERT_GROUP_NO` = bir günlük beyannamə, İSTİSNA TƏKDİR.** 296 qrupdan
295-i tam 5 sətirlik (bir gün × 5 valyuta) təmiz qrupdur. **`GroupNo=1`
İSTİSNADIR** — 30 sətir, 6 MÜXTƏLİF tarixə yayılıb (köhnə sistemin ilk
doldurma zamanı bir dəfəlik toplu yazdığı ilkin qeydlər görünür). Skript
bunu tarixə görə 6 ayrı beyannaməyə bölür (sintetik açar `100000+`, real
qrup nömrələri 1–297 arasında olduğu üçün toqquşma yoxdur). Yekun: **301
beyannamə, 1505 setir sətri**.

⚠️ **8 gündə BİRDƏN ÇOX qrup var (10 və ya 15 sətir)** — kursun həmin gün
ərzində bir neçə dəfə göndərildiyini göstərir. Bu, YENİ sistemin özünün də
dəstəklədiyi "Gün İçində Kurs Dəyişikliyi" davranışıdır (bax yuxarı, 09.10.2026)
— HƏR qrup AYRI, tam beyannamə kimi idxal olunur (sonuncusu "cari" sayılır,
`GunlukGetirAsync`-in `OrderByDescending(Id)` qaydası ilə eyni), BİRİ o
birini əvəz etmir.

**İKİ AYRI, TAM MÜSTƏQİL fayl** (CLAUDE.md-nin "SQL migration vermədən əvvəl
SELECT ilə göstər" qaydasına uyğun) — istifadəçi "niyə sətir seçməliyəm,
niyə tam vermirsən" deyə haqlı etiraz etdikdən sonra (15.10.2026) BİR
kombinə fayl (ADDIM 1 + ADDIM 2, "bu sətirdən bu sətrə qədər seç" təlimatı
ilə) İKİ tam, öz-özünə yetərli fayla bölündü — hər biri `#Kaynak` staging
cədvəlini (1505 sətirlik `INSERT`) ÖZ İÇİNDƏ TƏKRARLAYIR ki, istifadəçi
faylı SSMS-də açıb **sadəcə tam F5 ilə işlətsin**, heç bir sətir seçməsin:

- **`docs/sql/kassa/01_ADDIM1_Yoxlama.sql`** — yalnız SELECT, heç nə yazmır.
  (a) aşağıdakı 3 `Isciler.Id`-nin mövcud/aktiv olduğunu göstərir;
  (b) hansı tarixlərdə artıq `KassaKursBeyannameleri`-də qeyd var (09.10.2026
  modul işə düşəndən sonra canlı sistemdən yazılmış ola bilər — xüsusilə
  2026-10-09/12/13/14, bu export elə həmin günləri də əhatə edir);
  (c) ADDIM 2-nin HƏQİQƏTƏN nə qədər yeni beyannamə yaradacağının sayı.
- **`docs/sql/kassa/02_ADDIM2_Idxal.sql`** — yazır. `#Kaynak` staging
  cədvəlindən **CURSOR** ilə (OUTPUT+Tarix üzrə geri-uyğunlaşdırma QƏSDƏN
  İŞLƏDİLMƏDİ — 8 "bir gündə bir neçə qrup" halında Tarix unikal deyil,
  OUTPUT-u Tarix-ə görə geri bağlasaq sətirlər səhv beyannaməyə düşərdi)
  hər qrupu öz tranzaksiyası daxilində `KassaKursBeyannameleri` +
  `KassaKursSetirleri`-yə yazır, Status birbaşa **Tesdiqlendi (2)** ilə
  (tarixi data onsuz da təsdiqlənib, Gözləyir→Tesdiqlendi iki addımlı
  keçidə ehtiyac yoxdur). YALNIZ 1-ci faylın nəticəsini gördükdən sonra
  işlədilməlidir.

**Qayda: bir SQL skripti bir neçə addımdan ibarətdirsə (YOXLA → YAZ) və
istifadəçi SSMS-dən əl ilə işlədəcəksə, addımları TƏK fayl daxilində
"bu sətirdən bu sətrə qədər seç" təlimatı ilə AYIRMA — hər addım üçün
AYRI, tam, başdan-sona F5 ilə işə düşən fayl yaz, lazım gələn staging
datanı (temp cədvəl + INSERT-lər) hər faylın özündə TƏKRARLA.** Fayl ölçüsü
bir az artır (burada hər fayl ~1600 sətir), amma istifadəçi sətir seçimi
kimi səhvə açıq, çaşdırıcı addımla məşğul olmur — "aç, F5, nəticəyə bax"
qədər sadədir.

### İdempotentlik — Dəqiq Cütlük YERİNƏ TARİX SƏVİYYƏSİNDƏ QORUMA (15.10.2026, KRİTİK)

İlk versiyada idempotentlik açarı `(Tarix, TesdiqTarixi)` CÜTÜ idi — tək `Tarix`
ilə yoxlasaydıq, eyni günün 2-3 tarixi qrupundan yalnız BİRİNCİSİ yazılıb
qalanları "artıq var" deyə səhvən atlanacaqdı. Bu, SKRİPTİN TƏKRAR İŞLƏDİLMƏ
təhlükəsizliyi üçün düzgün idi, amma **CANLI SİSTEMLƏ TOQQUŞMA RİSKİNİ görmürdü**.

ADDIM 1-in 1b sorğusu göstərdi ki, `KassaKursBeyannameleri`-də **2026-10-09**
üçün artıq 2 sətir var (modulun özü 09.10.2026-da işə düşəndə yaradılmış) —
dəqiq `(Tarix, TesdiqTarixi)` cütü ilə yoxlasaydıq, bu canlı sətirlərin
`TesdiqTarixi`-si BMI export-undakı (`APPROVAL_DATE`) tarixi dəyərdən FƏRQLİ
olduğu üçün **uyğunluq tapılmayacaq və skript 2026-10-09 üçün YENİ bir
beyannamə YARADACAQDI** — canlı sətirlərin yanında.

**Niyə bu, sadəcə "əlavə sətir" ilə bitməzdi:** `KassaKursService.
GunlukGetirAsync` günün "cari" kursunu **`OrderByDescending(x => x.Id)`** ilə
seçir — ən böyük Id "ən son". Bulk idxal BUGÜN (15.10.2026) icra olunduğu üçün
import edilən tarixi sətir, Id sırasına görə canlı 09.10.2026 sətirlərindən
**həmişə sonra** gəlir, deməli **daha böyük Id alır**. Nəticə: canlı sistemin
09.10.2026-da həqiqətən təsdiqlədiyi kurs ekrandan yoxa çıxar, əvəzinə
BMI-nin köhnə tarixi kursu "bugünkü status" kimi göstərilərdi — səssiz, heç
bir xəta vermədən.

**Həll — `#QorunanTarixler` (qorunan-tarixlər) snapshot-u, CURSOR dövrəsindən
ƏVVƏL bir dəfə çəkilir:**

```sql
SELECT DISTINCT Tarix INTO #QorunanTarixler
FROM KassaKursBeyannameleri WHERE Silinib = 0;
```

Cursor-un yoxlaması indi dəqiq cütlük DEYİL, **tarix səviyyəsindədir**:
`IF NOT EXISTS (SELECT 1 FROM #QorunanTarixler WHERE Tarix = @Tarix)`. Bir
tarixin artıq İSTƏNİLƏN canlı (silinməmiş) beyannaməsi varsa, o tarixin BÜTÜN
BMI qrupları (bu dataset-də yalnız 2026-10-09, 1 qrup/5 sətir) ATLANIR —
`TesdiqTarixi` saatı uyğun gəlsə də, gəlməsə də. ADDIM 1-in 1c sorğusu da
eyni qaydaya köçürüldü: "Artıq mövcud tarix olduğu üçün ATLANACAQ beyannamə
sayı" (tarix-səviyyəli) və "Yaradılacaq YENİ beyannamə sayı" göstərir.

**8 "bir gündə bir neçə qrup" halı (öz-öz üst-üstə düşməsi) bundan
TƏSİRLƏNMİR** — o hallar `#Kaynak`-ın öz daxilində, HEÇ BİRİNİN
`KassaKursBeyannameleri`-də canlı qarşılığı olmadığı tarixlərdədir; yalnız
canlı sistemdə ARTIQ mövcud olan tarix (bu dataset-də sırf 2026-10-09)
qorunur, qalan 300 tarix adi qaydada (hamısı yeni) idxal olunur.

**İstifadəçi aydınlaşdırması (15.10.2026): "bu menim oz bazamdakidir, problem
yoxdur, canlida bosdur exchange"** — ADDIM 1-dəki 2026-10-09 sətirləri
istifadəçinin öz (test) bazasındaki qeydlərdir, HƏQİQİ canlı/production
Exchange isə hələ BOŞDUR. Yəni real idxal zamanı qorunan-tarix siyahısı çox
güman boş çıxacaq və BÜTÜN 301 qrup yeni yazılacaq — `#QorunanTarixler`
mexanizmi YENƏ DƏ saxlanılır (zərərsizdir, boş olanda heç nəyi atlamır) ki,
gələcəkdə модуl istifadəyə başlayandan SONRA bu skript təkrar işlədilərsə
(məs. unudulmuş köhnə qrup aşkar olunsa), canlı data yenə qorunsun.

**Qayda: "skripti təkrar işlətmək təhlükəsizdir" YALNIZ öz-özünə dublikat
yaratmamaq demək deyil — HƏDƏF cədvəldə skriptdən TAMAMİLƏ MÜSTƏQİL yaranmış
(canlı istifadədən gələn) sətirlərlə TOQQUŞMA ehtimalını da AYRICA yoxla.**
Burada hər ikisi lazım idi: `#Kaynak`-ın öz-özü ilə (8 çoxqruplu tarix) VƏ
hədəf cədvəlin canlı vəziyyəti ilə (2026-10-09) — iki fərqli risk, iki fərqli
qoruma mexanizmi (daxili dedup yoxdur, çünki hər qrup qəsdən ayrı beyannamə;
xarici qoruma isə `#QorunanTarixler`).

### Ad Uyğunluğu — SQL-DƏ "TƏXMİN" YOX, İSTİFADƏÇİ ƏL İLƏ TƏSDİQLƏDİ (15.10.2026)

İlk versiyada 3 adın (Axundov Elxan/Najafi Asghar/Ibrahimov Anar) `Isciler.Id`-si
SQL-in özündə (`LTRIM`/`RTRIM`/`CHARINDEX` ilə "Soyad Ad" formatını ayıraraq)
**təxmin edilirdi**. İstifadəçi SSMS-də əl ilə yoxlayanda **`Soyad = N'Ibrahimov'
AND Ad = N'Anar'` dəqiq uyğunluğu TAPMADI** (0 sətir), halbuki `Isciler`-də belə
bir sətir **var idi** (Id=3) — yəqin gizli diakritik/kodlaşdırma fərqi (məs. Azəri
"İ" ilə ASCII "I" arasında collation-a görə fərq) idi. `LIKE '%ibrahim%'` geniş
axtarışı isə tapdı. Eyni axtarışla "Najafi Asghar" da aşkarlandı — amma **real
soyad "Najafimarganmaskan"dır** (Ad=Asghar), BMI-nin köhnə datası yalnız qısaldılmış
"Najafi" formasını saxlayıb — bu, mənim "Soyad=birinci söz" fərziyyəmin xətası idi
(`SQLQuery1`, Id=25).

**Düzəliş:** ad-uyğunlaşdırma SQL-dən **tamamilə çıxarıldı**. Mənbədə cəmi 3 fərqli
ad olduğu üçün (ICRACI həmişə eyni, TESDIQ_EDEN yalnız 2 variant), istifadəçinin
SSMS-də canlı tapdığı 3 ID **generasiya skriptinin özündə** (Python, `gen_sql2.py`)
sabit yazıldı və hər `#Kaynak` sətrinə birbaşa `IcraciIsciId`/`TesdiqEdenIsciId`
tam ədəd kimi daxil edildi:

```
"Axundov Elxan"  → Isciler.Id = 2
"Najafi Asghar"  → Isciler.Id = 25   (əslində Ad=Asghar, Soyad=Najafimarganmaskan)
"Ibrahimov Anar" → Isciler.Id = 3
```

ADDIM 1a indi sadəcə bu 3 ID-nin `Isciler`-də mövcud/aktiv olduğunu göstərir
(`WHERE Id IN (2,3,25)`) — SQL-də heç bir `CHARINDEX`/`LTRIM` "təxmini ad
tapma" YOXDUR, ADDIM 2-nin cursor-u da artıq ad-uyğunlaşdırma etmir, birbaşa
`#Kaynak`-dakı hazır ID-ləri yazır.

**Qayda:** mənbə datada fərqli ad SAYI KİÇİKdirsə (burada: cəmi 3), SQL-də
fuzzy-matching alqoritmi yazmaqdansa, adları **bir dəfə, insan gözü ilə** (SSMS-də
sadə `SELECT ... LIKE`) tapıb ID-ni **sabit yazmaq** daha etibarlıdır — xüsusən
mənbə (BMI) və hədəf (`Isciler`) arasında soyad formatı/diakritik fərqi ola
biləcəyi məlum olanda. Yalnız fərqli ad sayı böyük olanda (onlarla/yüzlərlə, məs.
AML Excel idxalı kimi) SQL-səviyyəli fuzzy-matching əsaslandırılır.

⚠️ **GÖRÜNTÜ/YAZMA AUDİT SAHƏLƏRİ (`YaradanIcraciId` və s.) BOŞ QALIR** —
bu, keçmiş sistemdən gələn tarixi idxaldır, "kim idxal etdi" mənasında bir
`AppUser` yoxdur; `YaradilmaTarixi`/`TesdiqTarixi` real tarixi dəyərlə (BMI-nin
`APPROVAL_DATE`-i) doldurulur ki, "Son qeydlər" siyahısı düzgün xronoloji
sırada görünsün.

⚠️ **BU SESSİYADA SKRİPT İCRA EDİLMƏDİ** — bu mühitdə canlı SQL Server
bağlantısı yoxdur (Oracle-a da bağlantı yoxdur, mənbə artıq Excel export
kimi verilmişdi). İstifadəçi skripti öz SQL Server-ində **ƏVVƏLCƏ ADDIM 1**
ilə işlədib nəticəyə (xüsusilə ad uyğunluğu və tarix overlap-i) baxmalı,
yalnız bundan sonra ADDIM 2-ni icra etməlidir.

**Mənbə fayl saxlanmadı** — Excel export-u birbaşa `#Kaynak` staging
cədvəlinə çevrilib skriptin özünə (1505 literal `INSERT ... VALUES` sətri,
hər biri ≤500 sətirlik toplu bloklarda, SQL Server-in 1000-sətir/ifadə
həddinə görə) yazıldı; ayrıca `.xls`/`.csv` fayl təqdim olunmur.

## Kassa Exchange — "Son Qeydlər" Səhifələnməsi + "Yenilə" Düyməsi (15.10.2026)

Yuxarıdakı tarixi idxal nəticəsində "Son qeydlər" cədvəli **yüzlərlə** gün
qrupuna (hər qrup 5 valyuta sətri) çatacaqdı — istifadəçi canlı ekranda bunu
görüb şikayətləndi: *"SEHIFE BELE UZANIR, BELKE EN SONUNCUNU VEREK, EGER
ISTEYERSE ISCI HAMISINI SECERE, SEGIFELEME ILE. VE BIR DE EKRANDA YENILE
DUYMESI OLSUN, CUNKI TESDIQ ANINI IZLEMEK UCUN LAZIMDIR"*.

**Səhifələmə BEYANNAMƏ (gün qrupu) sayı ilə ölçülür, sətir sayı ilə YOX** —
hər beyannamə 5 valyuta sətri doğurur, səhifəni sətir sayına görə kəssək bir
beyannamənin valyutaları iki səhifəyə bölünərdi.

**Dəyişən üç qat** (Metod İmzası Dəyişikliyi qaydasına uyğun, yeganə çağırış
yeri olduğu üçün köhnə imza dərhal əvəz edildi, geriyə-uyğunluq şimi YOXDUR):

1. `IKassaKursService.SonBeyannameleriGetirAsync(int gunSayi = 60)` →
   `SonBeyannameleriGetirAsync(int sehife = 1, int sehifeOlcusu = 1)` —
   **`gunSayi`(60 günlük pəncərə) tamamilə çıxarıldı**, indi `Skip`/`Take`
   birbaşa beyannamə sırasına tətbiq olunur (SQL səviyyəsində, yaddaşa
   hamısı yüklənmir).
2. İmplementasiya (`KassaKursService.cs`) — yeni `KassaKursSiyahiSehifeDto`
   (`FinNex.Application/DTOs/Kassa/`) qaytarır: `Setirler` (cari səhifənin
   sətirləri) + `Sehife`/`SehifeOlcusu`/`UmumiBeyannameSayi` +
   hesablanan `HamisiSecilib`/`EvvelkiVarMi`/`NovbetiVarMi`.
3. `ExchangeController.Index(DateTime? tarix, int sehife = 1, int olcu = 1)`
   — yeni query-string parametrləri, `ExchangeIndexVM.SonQeydler` tipi
   `IList<KassaKursSiyahiDto>` → `KassaKursSiyahiSehifeDto`-ya dəyişdi.

**Default `sehifeOlcusu=1`** — "ən sonuncunu verək" tələbinə uyğun, açılışda
YALNIZ ən son beyannamə (5 sətir) görünür. Ekranda **"Göstər:" açılan siyahı**
(Ən sonuncu / Son 10 / Son 30 / Hamısı) — dəyişən kimi `onchange="this.form.
submit()"` ilə avtomatik göndərilir, `sehife` İSTƏR-İSTƏMƏZ `1`-ə sıfırlanır
(gizli sahə) ki, köhnə ölçüdə 2-ci səhifədə ikən ölçünü dəyişən istifadəçi
boş/yanlış nəticə almasın.

**`olcu<=0` = "Hamısı"** — bu halda `Skip`/`Take` tətbiq olunmur, amma
limitsiz DEYİL: `KassaKursService.HamisiTavan = 2000` təhlükəsizlik tavanı
var (CLAUDE.md-nin AML "BÖYÜK SİYAHI" bölməsindəki eyni prinsip — "limitsiz"
sözü "hədsiz" demək deyil). "Hamısı" seçilibsə pagination paneli
ÜMUMİYYƏTLƏ göstərilmir (`EvvelkiVarMi`/`NovbetiVarMi` hər ikisi `false`
qayıdır).

**Pagination düymələri İKİ MÜSTƏQİL şərtdədir** — 12.10.2026-da Mail
modulunda tapılan "Əvvəlki bir yoxlamaya bağlı qalıb Növbəti ilə birlikdə
yoxa çıxırdı" bugunun EYNİ naxışla qarşısı alınır (bax yuxarı "Göndərilən/
Gələn Mail Detalı... Pagination 'Əvvəlki' Bugu" bölməsi): `EvvelkiVarMi`
(`Sehife > 1`) və `NovbetiVarMi` (`Sehife * SehifeOlcusu < UmumiBeyannameSayi`)
bir-birindən tamamilə asılı deyil, hər link öz `@if`-indədir.

**"Yenilə" düyməsi — AJAX YOX, sadəcə `location.reload()`.** Mail modulunun
"Yenilə" düyməsindən FƏRQLİ (o, xarici IMAP qutusunu sinxronlaşdırır, ona
görə fetch+token lazımdır) — buradaki data artıq öz SQL Server bazamızdadır,
`Index` action-ı onsuz da HƏR GET-də təzə sorğu aparır. Sadə səhifə yenidən
yüklənməsi `tarix`/`sehife`/`olcu` query-sini saxlayır (URL dəyişmir) və
yuxarıdaki status nişanını (`g.Status` — Gözləyir/Təsdiqləndi/İmtina) təzədən
oxuyur, elə istifadəçinin istədiyi "təsdiq anını izləmək" buna kifayətdir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı, DTO/interfeys/implementasiya/controller
imza uyğunluğu). İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real
mühitdə yoxlamalıdır: açılışda yalnız 1 beyannamənin göründüyünü, "Son 10"/
"Son 30"/"Hamısı" seçimlərinin düzgün say qaytardığını, Əvvəlki/Növbəti
düymələrinin sərhəddə (ilk/son səhifə) düzgün gizləndiyini və "Yenilə"
düyməsinin cari görünüşü (xüsusən status nişanını) itirmədən yenilədiyini.

## Məzuniyyət Müraciəti — "Təxmini Net Maaş" Dondurulub Saxlanır (14.10.2026)

İşçinin sualı HR vasitəsilə gəldi: *"mezuniyyet yazdigi zaman teqribi maasi
gosterir, amma sonra onu hec yerde gore bilmirem"*. Araşdırma göstərdi ki,
`MezuniyyetController.Preview` (`GET /User/Mezuniyyet/Preview`) müraciət
formasında göstərilən rəqəmi (`umumiNet`) **real hesablama mühərriki**
(`MaasHesablamaService.MezuniyyetOdenisiDetalliHesablaAsync` + `Tutulmalari
HesablaAsync`) ilə hazırlayır — təxmin yox, HYS/avans/vergi daxil tam
hesablamadır — amma bu cavab **heç yerə yazılmırdı**, işçiyə bir dəfə
göstərilib itirdi.

**Həll — `Mezuniyyet.TexminiNetMaas`** (yeni sütun, migration
`20261014090000_MezuniyyetTexminiNetMaas.cs`, yalnız `AddColumn`,
`InsertData` yoxdur): müraciət forması (`Create.cshtml`) JS-i `refreshPreview`
cavabından (`data.umumiNet`) gizli bir sahəni (`#texminiNetMaasInput`,
`MezuniyyetCreateVM.TexminiNetMaas`) doldurur, `Create` POST bunu
`MezuniyyetCreateDto`-ya, oradan (AutoMapper, convention-based) entity-yə
keçirir. İşçinin öz **Müraciət Detayı** səhifəsində (`/User/Mezuniyyet/
Detail/{id}`) sağdakı "Müraciət məlumatları" kartına əlavə sətir kimi
göstərilir, etiketi **"Təxmini net maaş (müraciət zamanı)"**.

⚠️ **QƏSDƏN "TƏXMİNİ" — real ödənişlə EYNİ OLACAĞINA ZƏMANƏT YOXDUR.** Bu,
"Qara Jeton — MenfiMiqdar" və "Pul Köçürməsi — UsdEkvivalent" ilə EYNİ
"əməliyyat anında dondur" prinsipidir: `TexminiNetMaas` müraciət YARADILAN
anın hesablamasıdır, real ödəniş (`OdenenMebleg`) isə HR təsdiqində AYRICA,
sonradan yenidən hesablanır (başqa məzuniyyət, korreksiya, qazanc tarixçəsi
dəyişikliyi s. ola bilər) — iki rəqəm fərqli çıxa bilər. Görüntü qatında bu
tooltip ilə açıq bildirilir, "rəsmi ödəniş" kimi TƏQDİM EDİLMİR.

**"Öz hesabına" (ödənişsiz) növündə sahə BOŞ qalır** — JS tərəfi `novToggle`-
də (Nov dəyişəndə) və preview uğursuz/gizli olanda gizli sahəni açıq
təmizləyir (`texminiInp.value = ''`), yoxsa Əmək məzuniyyəti üçün hesablanmış
köhnə rəqəm səhvən ödənişsiz müraciətə keçə bilərdi.

**Üç qat yeniləndi** (Metod İmzası Dəyişikliyi qaydasına uyğun — DTO, Entity,
görüntü eyni anda): `MezuniyyetCreateDto`/`MezuniyyetDto`/`MezuniyyetDetailVM`
hamısı `TexminiNetMaas`-ı daşıyır; `MezuniyyetService.IdIleGetirAsync` **əl
ilə** map edir (bu metod AutoMapper DEYİL, manual DTO qurur — unudulsa sahə
həmişə `null` qalardı, Detail səhifəsi heç vaxt göstərməzdi).

⚠️ **HR tərəfinin öz Mezuniyyet Detal səhifəsinə ƏLAVƏ EDİLMƏDİ** — sual
konkret işçinin öz portalından gəldi (ekran-görüntüsü `/User/Mezuniyyet/
Detail`-dan idi), qapsam ora məhdudlaşdırıldı. HR də istəsə, eyni sahə
onsuz da DB-də var — yalnız HR-in öz detal view-una bir sətir əlavə etmək
kifayətdir.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — bu düzəlişdən ƏVVƏL yaradılmış müraciətlərdə
sütun NULL-dur (o anın preview cavabı heç yerdə saxlanılmayıb, retroaktiv
bərpa mümkün deyil) — Detail səhifəsi həmin köhnə qeydlərdə sadəcə bu sətiri
göstərmir (şərt `HasValue`).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə/brace balansı skriptlə yoxlanıldı — düzəlişin özü
balanslı, faylların pre-existing balanssızlığı şərh mətnindəki mötərizələrdən
gəlir, kodun özündən deyil). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra real ssenari ilə yoxlamalıdır: yeni "Əmək məzuniyyəti" müraciəti yazıb
göndərəndə Müraciət Detayı səhifəsində "Təxmini net maaş" sətrinin formada
göstərilən rəqəmlə DƏQİQ eyni göründüyünü, "Öz hesabına" müraciətdə isə
sətrin ümumiyyətlə görünmədiyini.

## Gələn Mail — Oxundu Statusu Serverə Yazılır (`\Seen`) — Outlook ilə Sinxron (16.10.2026)

İstifadəçi şikayəti: *"mailləri titan ilə bağlamışıq, amma işdə Outlook işlədirlər —
FinNex-də oxuyanda Outlook-da bilinmirki oxundu"*. Kök səbəb tapıldı:
`GelenMailService.OxunduIsareEtAsync`/`HamisiniOxunduIsareEtAsync` "oxundu"-nu
**yalnız öz bazamızda** (`Oxunub=true`) yazırdı, IMAP serverinə heç müraciət
etmirdi. `GelenMailImapSyncer.SyncNowAsync` da qutunu **`FolderAccess.ReadOnly`**
ilə açırdı — sinxronizasiya YALNIZ bir istiqamətli idi (serverdən bizə).

**Niyə bu, Outlook-u da düzəldir:** `\Seen` bayrağı IMAP protokolunun ÖZÜNÜN
(Titan-a xas DEYİL, hər IMAP serverində — Dovecot/Zimbra/Exchange və s.) **server-
tərəfli, paylaşılan** vəziyyətidir. Eyni poçt qutusuna qoşulan HƏR müştəri (FinNex-in
öz IMAP bağlantısı, Outlook-un öz IMAP bağlantısı) EYNİ bayrağa baxır — FinNex
serverə yazsa, Outlook da onu dərhal oxunmuş görür, əlavə inteqrasiya lazım deyil.

**Tətbiq — iki fayl:**
1. `IGelenMailImapSyncer.OxunduIsaretleServerdeAsync(imapHost, email, password,
   messageIds)` + implementasiya (`GelenMailImapSyncer.cs`) — qutunu
   **`FolderAccess.ReadWrite`** ilə açır (SyncNowAsync-in `ReadOnly`-si ilə
   QARIŞDIRMA — bayraq yazmaq üçün yazma icazəsi məcburidir), hər `messageId`
   üçün `Message-Id` header-i üzrə **IMAP SEARCH** aparır, tapılan UID-lərə
   `AddFlagsAsync(uids, MessageFlags.Seen, true)` yazır.
2. `GelenMailService` — iki yazma yolunda da (tək mail, "Hamısını oxundu et")
   yerli DB yeniləməsindən **SONRA** `ServerdeOxunduIsaretleFireAndForget`-i
   çağırır — `BildirisService.YaratAsync`-in `_desktop.PushAsync`-ı necə
   fire-and-forget (`_ = `) çağırdığı ilə EYNİ naxış (bax "Bildirişlər" bölməsi).

⚠️ **UID SAXLANMIR, HƏR DƏFƏ AXTARIŞ APARILIR — QƏSDƏN.** `GelenMail.MessageId`
yalnız `Message-Id` header-idir, IMAP UID deyil. UID-i sync anında keşləmək
sürətli olardı, AMMA **UIDVALIDITY** dəyişsə (admin qutunu yenidən qursa) köhnə
UID başqa bir mailə yapışmış ola bilər — bu, **SƏHV mailı "oxundu" işarələmək**
demək olardı, ən pis nəticə. Message-Id üzrə təzə axtarış YAVAŞDIR, amma
TƏHLÜKƏSİZDİR: ya DOĞRU mailı tapır, ya HEÇ NƏYİ (səhv mailı HEÇ VAXT YOX).

⚠️ **Best-effort — YEREL "oxundu" statusunu HEÇ VAXT bloklamır.** IMAP yazması
`try/catch` ilə TAM əhatə olunub, heç vaxt istisna ATMIR (server əlçatmaz, şifrə
açılmır, mail tapılmır — hamısı sakit `LogWarning`). Çağıran DB-ni artıq (IMAP
cəhdindən ƏVVƏL) yeniləyib — FinNex-in öz UI-si IMAP-ın nəticəsindən ASILI DEYİL.

⚠️ **Yalnız işçi ÖZÜ Profil → Mail Ayarları-nda IMAP qurubsa işləyir.** `user.
MailSmtpEmail`/`MailSmtpParol` boşdursa (əksər adi işçidə belədir — yalnız
Gələn Mail modulunu işlədən Rəhbər/Admin/`mail_istifade` icazəli işçilər bunu
doldurur) sakit keçir, xəta YOXDUR — bu, hər istifadəçi üçün məcburi deyil.

⚠️ **Göndərici (sender) Outlook-u "oxundu" GÖRMƜR — bu AYRI məsələdir.** `\Seen`
yalnız ALICININ (bizim) öz qutusunda, YALNIZ bizim öz müştərilərimizə (FinNex,
öz Outlook-umuz) görünür. Göndərənin Outlook-unda "oxundu" bildirişi (Read
Receipt / MDN, RFC 3798) **tamam fərqli** bir protokol — FinNex bunu dəstəkləmir
və bu düzəliş bunu HƏLL ETMİR, qarışdırma.

⚠️ **GedenMail (Göndərilən Mail) toxunulmadı** — "oxundu" konsepti yalnız Gələn
Mailə aiddir, bu modul qapsam xaricindədir.

**Titandan öz serverə keçid təsirsizdir:** `\Seen` IMAP-ın ÖZ xüsusiyyətidir —
`GelenMailImapSyncer.DeriveImapHost` "smtp.X → imap.X" qaydası yeni serverdə
də eyni sxemi izləyirsə (standart IMAP, 993 port, istifadəçi+parol girişi),
kodda HEÇ nə dəyişmədən işləyəcək — hər işçi sadəcə Profil səhifəsindəki SMTP
Host-u yeniləyir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı, MailKit API imzaları — `SearchQuery.HeaderContains`,
`AddFlagsAsync(uids, MessageFlags.Seen, true, ct)` — yaddaşdan təsdiqləndi,
NuGet sənədinə birbaşa baxılmadı). İstifadəçi Visual Studio-da **Rebuild All**
ilə, sonra real IMAP hesabı ilə yoxlamalıdır: FinNex-də mail açılanda, EYNİ
qutuya qoşulan Outlook-da da (bir neçə saniyə/Outlook-un öz sinxron dövrü
ərzində) mailin oxunmuş kimi göründüyünü.

### İKİ BUG — Canlı Testdə Tapıldı (16.10.2026, EYNİ GÜN, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi yuxarıdakı funksiyanı dərhal real mühitdə (öz-özünə test maili
göndərib, FinNex-də oxuyub, Titan-ın öz webmail-ini başqa tab-da yoxlayaraq)
sınadı və iki bug tapdı: *"men indi test etdim… bmifinansda maile girdim ama
titanda hemin mail helede oxunmamis idi, sehifeni yeniledimde olmadi. biz
bunu duzeltmedikki?"* + *"birde yenile duymesini vurdum mail yoxdur yazdi
ama sehifeni yeniledim mail gorundu, bunada bax"*.

**Bug A — `\Seen` yazması serverə HEÇ VAXT çatmırdı (scoped DI "fire-and-
forget"dən sonra dispose).** `GelenMailService`-in konstruktoru
`UserManager<AppUser>`/`IDataProtectionProvider`/`IGelenMailImapSyncer`-i
REQUEST-SCOPED nüsxə kimi birbaşa saxlayırdı. `ServerdeOxunduIsaretleFireAndForget`
isə `_ = ServerdeOxunduIsaretleAsync(...)` ilə **gözlənilmədən** (fire-and-forget)
çağrılırdı — controller action HTTP cavabını dərhal qaytarır, onun DI scope-u
demək olar DƏRHAL dispose olunur, background task isə hələ ilk `await
_userManager.FindByIdAsync(...)`-ə çatmamış ola bilər. `UserManager`-in
arxasındakı scoped `AppDbContext` artıq dispose edildiyi üçün bu çağırış
`ObjectDisposedException` atırdı — bu da elə öz `catch (Exception ex)`
blokumuzda SAKİTCƏ tutulub `LogWarning`-ə yazılırdı. Nəticə: yerli "oxundu"
statusu düz işləyirdi, amma IMAP `\Seen` heç vaxt yazılmırdı — kod "uğurlu"
görünən bir warning-lə susurdu.

Bu, elə layihənin ÖZ `GelenMailSyncService.cs` (fon sinxron job-u) ARTIQ
HƏLL ETDİYİ tələdir — o, hər background əməliyyat üçün `_scopeFactory.
CreateScope()` ilə TƏZƏ, request-dən asılı olmayan bir scope açır. Yeni
`\Seen` yazma metodu bu qaydaya tabe DEYİLDİ.

**Düzəliş:** `GelenMailService`-in konstruktoru `UserManager<AppUser>`/
`IDataProtectionProvider`/`IGelenMailImapSyncer`-i **artıq saxlamır** (bu
üçü sinifdə YALNIZ bu bir metodda işlənirdi, başqa heç yerdə) — əvəzinə
`IServiceScopeFactory _scopeFactory` saxlanılır. `ServerdeOxunduIsaretleAsync`
indi `using var scope = _scopeFactory.CreateScope();` açır, bu üç servisi
`scope.ServiceProvider.GetRequiredService<T>()` ilə O scope-dan alır —
`GelenMailSyncService.GetAllImapCredentialsAsync`-in dəqiq eyni naxışı.

**Bug B — "Yenilə" düyməsi "mail yoxdur" dedi, amma F5 maili göstərdi.**
`GelenMail/Index.cshtml` (və güzgüsü `GedenMail/Index.cshtml`) "Yenilə"
düyməsinin JS-i səhifəni yalnız `d.success && d.count > 0` olanda yeniləyirdi.
`count` isə YALNIZ bu KONKRET `ManualSync` çağırışının IMAP-dan tapdığı YENİ
mail sayıdır — fon sinxronu (`GelenMailSyncService`, hər 5 dəqiqə) bu klikdən
BİR AZ ƏVVƏL eyni maili artıq bazaya yazmış ola bilər. Belə halda `ManualSync`
düzgün "0 yeni" qaytarır (IMAP-da onsuz da artıq görülmüş mail var), AMMA
səhifənin ilk GET-də render olunmuş statik siyahısı bazadakı YENİ (artıq
mövcud) mail ilə uyğunlaşdırılmırdı — yalnız əl ilə F5 (tam yeni GET) bunu
üzə çıxarırdı.

**Düzəliş — hər iki `Index.cshtml`-də eyni sətir:** `if (d.success && d.count
> 0)` → **`if (d.success)`** — "Yenilə" düyməsi indi öz adına uyğun, HƏR
uğurlu cavabda səhifəni yeniləyir, `count`-un sıfır olması ("bu konkret IMAP
çağırışı yeni heç nə tapmadı") ilə "bazada göstərilməli yeni heç nə yoxdur"
eyni şey DEYİL — yalnız sonuncunu yoxlamaq mümkün deyildi (səhifənin öz
statik siyahısı ilə bazanın canlı vəziyyətini tutuşdurmaq üçün əlavə sorğu
lazım olardı), ona görə ən sadə, təhlükəsiz həll hər uğurlu "Yenilə"-də
yenidən yükləməkdir — "heç nə yoxdursa" zərərsiz, görünməz bir reload olur.

**Qayda (Bug A):** bir servisin konstruktoru request-scoped bir asılılığı
(`UserManager`, `DbContext`, və onlardan asılı hər şey) saxlayır VƏ həmin
sinifdə fire-and-forget (`_ = Metod(...)`, gözlənilmədən) bir background
task varsa — o task ƏSLA konstruktor-scoped asılılığı işlətməməlidir, çünki
onun yaşadığı müddət controller action-ın HTTP cavabı qədərdir, background
task-ın öz icra müddəti qədər YOX. `IServiceScopeFactory`-dən yeni scope
açmaq MƏCBURİDİR — bu, elə `GelenMailSyncService`-in başdan bu cür yazılmasının
səbəbidir, yeni bir fire-and-forget yol əlavə edəndə bu presedenti unutmaq olmaz.

**Qayda (Bug B):** "N yeni tapıldı" sayğacı ilə "ekran indi bazanın həqiqi
vəziyyətini göstərir" FƏRQLİ təsdiqlərdir. Manual sinxron düyməsinin arxasında
FON sinxron job-u da VARSA, düymənin öz sayğacı sıfır ola bilsin deyə, ekranı
yeniləmə qərarını bu sayğaca bağlama — "Yenilə" əməliyyatının ÖZÜ uğurlu
olub-olmamasına bağla.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı, JS/C# sintaksisi). İstifadəçi Visual Studio-da **Rebuild
All** ilə, sonra real mühitdə yenidən test etməlidir: FinNex-də mail oxuyandan
bir neçə saniyə sonra Titan-ın öz "oxunmamış" sayğacının azaldığını, "Yenilə"
düyməsinin "mail yoxdur" desə belə səhifəni yeniləyib cari siyahını göstərdiyini.

### ÜÇÜNCÜ DALĞA — Scope-fix Rebuild Olunub Yenidən Test Edilsə DƏ İŞLƏMİRDİ (21.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi yuxarıdakı Bug A düzəlişini (IServiceScopeFactory) rebuild edib
yenidən test etdi — eyni problem davam etdi: *"bizde oxunub ama titanda
helede oxunmamisdir"* (ekran-görüntüsü ilə). Scope-fix-in özü DÜZGÜN idi
(yoxlanıldı, təsdiqləndi), amma bir şərt onu **ümumiyyətlə çağırılmaqdan**
saxlayırdı.

**Kök səbəb:** `GelenMailService.OxunduIsareEtAsync`-də `\Seen` yazısı yalnız
`if (!mail.Oxunub) { ... ServerdeOxunduIsaretleFireAndForget(...); }` blokunun
**İÇİNDƏ**, yəni YALNIZ "oxunmamış→oxunmuş" **İLK keçiddə** çağırılırdı. Bu
konkret test mailları ƏVVƏLKİ (scope-fix-dən ƏVVƏLKİ) build-də bir dəfə
açılmışdı — FinNex-in öz bazasında artıq `Oxunub=true` idi. Rebuild-dən sonra
eyni maili YENİDƏN açmaq `if (!mail.Oxunub)` şərtini **yalan** tapırdı —
IMAP-a heç bir YENİ cəhd getmirdi. Fix-in özü düz işləyirdi, sadəcə heç vaxt
İCRA OLUNMURDU — simptom "fix işləmir" kimi görünürdü, əslində "fix heç
çağırılmır" idi.

**Həll — `GelenMail.ServerdeSeenIsarelendi`** (yeni `BIT NOT NULL DEFAULT 0`
sütunu, `Program.cs`-in `GelenMailler` idempotent `ALTER TABLE` blokuna əlavə
edildi — bu modulun DB cədvəlləri EF migration-la DEYİL, bax yuxarı
"Göndərilən Mail + Mail İzləmə" bölməsi). `Oxunub`-dan **qəsdən AYRI**
sahədir:
- `Oxunub` — yerli, FinNex-in öz "oxundu" statusu (UI-da göstərilən).
- `ServerdeSeenIsarelendi` — IMAP `\Seen` yazısının **HƏQİQƏTƏN təsdiqləndiyi**
  sahə (server tapdı VƏ `AddFlagsAsync` xətasız qayıtdı).

`OxunduIsareEtAsync` indi **iki AYRI şərtə** baxır: `Oxunub`-u (lokal statusu)
yalnız hələ `false`-dursa yazır, AMMA `\Seen` cəhdini `!mail.
ServerdeSeenIsarelendi` şərtinə görə edir — `Oxunub` artıq `true` olsa belə.
Yəni mail **neçə dəfə açılırsa-açılsın**, server-yazısı HƏQİQƏTƏN
təsdiqlənənə qədər HƏR açılışda təkrar cəhd olunur (uğursuz cəhdlər — server
əlçatmaz, mail tapılmadı — səssizcə qalır, növbəti açılışda yenidən sınanır).

**`IGelenMailImapSyncer.OxunduIsaretleServerdeAsync` imzası dəyişdi** —
əvvəl `Task` (heç nə qaytarmırdı, çağıran tərəf HANSI mesajların uğurlu
olduğunu BİLMİRDİ) → indi **`Task<HashSet<string>>`**, yalnız HƏQİQƏTƏN
`AddFlagsAsync`-i xətasız keçən Message-Id-ləri qaytarır. `GelenMailService`
bu dəst ilə `mailIdToMessageId` lüğətini **tərs axtarıb** (Message-Id →
GelenMail.Id) yalnız TƏSDİQLƏNMİŞ sətirlərə `ServerdeSeenIsarelendi=true`
yazır — **TƏZƏ bir `IUnitOfWork`** ilə, eyni YENİ scope-dan (constructor-un
`_uow`-u YOX, o, HTTP request-in köhnə, dispose olmuş scope-undadır — Bug
A-nın elə özü, burada TƏKRAR düşməmək üçün).

**Əlavə diaqnostik log** (`GelenMailImapSyncer.OxunduIsaretleServerdeAsync`,
`GelenMailService.ServerdeOxunduIsaretleAsync`) — əvvəl YALNIZ xəta halında
log yazılırdı, uğurlu yazmada HEÇ BİR iz qalmırdı. İndi başlanğıc
(`Information`), "IMAP qurulmayıb" (`Information`, əvvəl sakit `return`
idi) və hər uğurlu `\Seen` yazısı (`Information`, Message-Id + UID-lərlə)
qeyd olunur — növbəti dəfə oxşar şikayət gələrsə, `Logs\log-<tarix>.txt`
faylında `\Seen` sözünü axtarmaqla HARADA dayandığı DƏRHAL görünür.

**Qayda:** fire-and-forget bir background əməliyyəti (`\Seen` yazısı kimi)
yerli bir "bayraq" (`Oxunub`) ilə EYNİ şərtə bağlamaq TƏHLÜKƏLİDİR — əgər
background əməliyyət HƏLƏ tətbiq olunmazdan ƏVVƏL həmin bayraq artıq `true`
olubsa (köhnə kod, əvvəlki uğursuz cəhd, əl ilə redaktə və s.), yeni kod
HEÇ VAXT işə düşmür, baxmayaraq ki özü düzgündür. İki fərqli sualın
("istifadəçi üçün oxunubmu?" vs "arxa-plan əməliyyatı TƏSDİQLƏNİBMİ?") CAVABI
AYRI sahələrdə saxlanmalıdır — "Qara Jeton MenfiMiqdar" və "VM9821 Tip"
bölmələrindəki "iki fərqli sualın iki fərqli cavabı var" prinsipinin YENİ
forması.

⚠️ **GERİYƏ TƏTBİQ OLUNUR, AMMA GECİKMƏLİ** — köhnə (bu düzəlişdən əvvəl
`Oxunub=true` yazılmış) mailların `ServerdeSeenIsarelendi`-si default
`false`-dur, ona görə onlar ÖZBAŞINA düzəlmir, AMMA növbəti dəfə İSTİFADƏÇİ
O MAİLİ YENİDƏN AÇANDA (MailPanel-ə təkrar klik) avtomatik təkrar cəhd
ediləcək — əlavə SQL/miqrasiya lazım deyil, təbii istifadə zamanı özü düzəlir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı, DTO/interfeys/implementasiya imza uyğunluğu). İstifadəçi
Visual Studio-da **Rebuild All** ilə, sonra EYNİ (artıq bir dəfə açılmış)
test mailını YENİDƏN açaraq test etməlidir — bu dəfə "Oxunub artıq true"
şərti onu bloklamayacaq. Əlavə olaraq: `Logs\log-<bugünkü-tarix>.txt`
faylında "\Seen" axtarıb hansı addımda dayandığını (başladı/tapılmadı/
yazıldı/xəta) göstərən sətirləri tapıb bura əlavə etmək, gələcək diaqnoz
üçün çox dəyərli olacaq.

### DÖRDÜNCÜ DALĞA — Retry Mexanizmi İşlədi, AMMA Titan-ın IMAP `SEARCH`-i Mailı Heç Vaxt TAPMADI (21.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi Rebuild All edib ÜÇÜNCÜ DALĞA düzəlişini (`ServerdeSeenIsarelendi`
retry mexanizmi) yenidən test etdi və TƏSDİQLƏDİ ki, bu dəfə rebuild
edilmiş kod işə düşmüşdü (`AskUserQuestion`-a "Bəli, rebuild edib yenidən
sınadım" cavabı). Nəticə yenə **"olmadi yene"** idi — amma bu dəfə istifadəçi
tam `Logs\log-….txt` çıxışını yapıştırdı, bu da diaqnozu kökündən dəyişdi.

**Loglar nə sübut etdi:**
- `GelenMail \Seen yazma BAŞLADI` sətri bir neçə dəfə, YAXIN vaxtlarla
  görünürdü → retry mexanizmi (ÜÇÜNCÜ DALĞA) **düzgün işləyirdi**, heç bir
  `ObjectDisposedException`/scope xətası YOX idi → Bug A-nın (DI scope)
  fix-i də TƏSDİQ olundu.
- Hər dəfə ardınca **`GelenMail \Seen yazma: mail serverdə tapılmadı …`**
  (`WARN`) gəlirdi — yəni problem artıq retry-da YOX, `inbox.SearchAsync
  (SearchQuery.HeaderContains("Message-Id", msgId))` çağırışının ÖZÜNDƏ idi.
- Həlledici sübut: `GelenMailImapSyncer: 1 yeni mail yükləndi.` log sətri
  (fon sinxron job-u, `SyncNowAsync`) saat 11:22:20-də yazılmışdı, DƏRHAL
  ardınca (11:22:29-30) ELƏ HƏMİN mailin `\Seen` axtarışı "tapılmadı"
  qaytarırdı — yəni `SEARCH` ELƏ İNDİCƏ `FetchAsync` ilə TAPILMIŞ, bazaya
  yazılmış bir mesajı belə tapa bilmirdi. Bu, Titan-ın IMAP `SEARCH HEADER`
  əmrinin (ən azı `Message-Id` header-i üzərində substring axtarışda)
  **etibarsız/işləməyən** olduğunu göstərir — kod məntiqi deyil, server
  tərəfinin məhdudiyyətidir.

**Kök səbəb:** server-side `SEARCH` əmrinə güvənmək. `SyncNowAsync`
(fon sinxronu, EYNİ fayl) heç vaxt `SEARCH` işlətmir — son N mesajı
`FetchAsync` ilə YÜKLƏYİR, `Envelope.MessageId`-ni YERLİ olaraq (C#
`Dictionary`/`HashSet` ilə) müqayisə edir. `OxunduIsaretleServerdeAsync`
isə bunun ƏKSİNƏ, hər mesaj üçün SERVERƏ `SEARCH` göndərirdi — elə bu fərq
labüd idi.

**Düzəliş — `OxunduIsaretleServerdeAsync`, `SyncNowAsync`-in ÖZÜ İLƏ EYNİ
nümunə:** `SEARCH` tamamilə çıxarıldı. Əvəzinə: `inbox.Count` və
`FetchAsync(startIndex, -1, UniqueId | Envelope, ct)` ilə son **300**
mesajın (`\Seen` yazması adətən bir neçə mail üçün çağırılır, sinxron
pəncərəsi — 100 — ilə eyni olması vacib deyil, bir az geniş tutuldu) UID+
Envelope-u BİR DƏFƏ yüklənir, `Dictionary<string, UniqueId>`
(`Envelope.MessageId → UniqueId`) qurulur, hər hədəf `msgId` bu lüğətdən
axtarılır (`TryGetValue`) — tapılarsa `AddFlagsAsync(uid, Seen, true, ct)`,
tapılmazsa (həqiqətən pəncərədən kənardadırsa — köhnə mail, silinib,
köçürülüb) əvvəlki kimi sakit `continue` + `LogWarning`.

⚠️ **Pəncərə (300) hələ də MƏHDUDDUR** — `\Seen` yazılacaq mail 300-dən
əvvəlki (çox köhnə) mesajlardandırsa, YENƏ "tapılmadı" qalacaq. Bu, praktik
risk aşağıdır (işçi adətən öz GƏLƏN QUTUSUNDA son mailləri açır, bu da elə
sinxronun (100-lük pəncərə) idxal etdiyi diapazondur), amma SƏHV DEYİL —
sadəcə qəbul edilmiş bir güzəşt. Lazım gəlsə pəncərə böyüdülə bilər, amma
hər böyütmə `FETCH`-in özünün gətirdiyi məlumat həcmini artırır.

**Qayda:** eyni fayldakı İKİ metoddan BİRİ (`SyncNowAsync`) artıq sınanmış,
işlək bir server-sorğu nümunəsi işlədirsə, YENİ bir metod (`OxunduIsaretle
ServerdeAsync`) AYNI serverlə danışarkən FƏRQLİ bir ilkin yanaşma (bura:
`SEARCH`) seçməzdən əvvəl, həmin FƏRQİN səbəbini sual et — "niyə qonşu metod
bunu belə etmir?" sualının cavabı tez-tez "çünki o üsul bu serverdə işləmir"
olur, xüsusən fərqli IMAP server implementasiyaları arasında `SEARCH`
dəstəyi/davranışı kəskin fərqlənə bilir (RFC 3501-in `SEARCH`-i geniş, bəzi
serverlər yalnız alt-dəstəyini düzgün dəstəkləyir).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə/dırnaq balansı skriptlə yoxlanıldı, `MailKit.
IMailFolder.AddFlagsAsync(UniqueId, MessageFlags, bool, CancellationToken)`
overload-u `SyncNowAsync`-in özündəki import/namespace-lərlə eynidir, əlavə
`using` lazım olmadı). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra real mühitdə YENİDƏN test etməlidir — mail açılanda (və ya "Hamısını
oxundu et" ilə) `Logs\log-<bugünkü-tarix>.txt`-də bu dəfə **"GelenMail
\Seen yazıldı — Message-Id=…, UID=…"** (`Information`) sətrinin göründüyünü,
VƏ bir neçə saniyə sonra Titan-ın öz qutusunda/Outlook-da mailin
OXUNMUŞ kimi göründüyünü yoxlamalıdır. Hələ "tapılmadı" çıxırsa, bu dəfə
görünən pəncərə ölçüsü (300) ilə bağlı ola bilər — neçə mesaj arxada
qaldığını (`inbox.Count` ilə həmin mailin sırası) bildirmək növbəti
diaqnozu sürətləndirər.

✅ **TƏSDİQLƏNDİ (21.10.2026, eyni gün).** İstifadəçi rebuild edib real
mühitdə yenidən test etdi, `Logs\log-20261021.txt`-i Notepad++-da göstərdi:
`11:41:47.930 [INF] GelenMail \Seen yazma BAŞLADI — …, MesajSayi=1` dərhal
ardınca `11:41:50.050 [INF] GelenMail \Seen yazıldı — Message-Id=…,
UID=160` — **heç bir "tapılmadı" xəbərdarlığı yoxdur**. Yerli `FetchAsync`+
`Dictionary` müqayisəsi Titan-da `SEARCH`-in əvəzinə HƏQİQƏTƏN işləyir.
Dörd dalğalıq zəncir (DI-scope → `ServerdeSeenIsarelendi` retry →
`SEARCH`→yerli-müqayisə) bununla TAM bağlandı.

## HYS — Bir Aylıq Dondurma (26.10.2026)

İstifadəçi sualı: *"isci HYS ni misal bir ayliq dondurmaq isteyir ve bu hemin
ayda nezere alinmamalidir. bunun ucun bizim kodlarda ne var?"* — kodda belə bir
funksiya YOX idi; iki dizayn təklif olundu (IsciHYS-in özünü ləğv/yenidən yarat
vs ayrıca audit cədvəli), istifadəçi ikincisini seçdi və **"son dərəcədə
ehtiyyatlı iş istəyirəm, maksimum diqqətli, yaddan çıxan heç nə qalmasın"**
tapşırığı ilə həyata keçirildi.

**Niyə `IsciHYS.BaslamaTarixi/BitmeTarixi`-ni bölmək YOX:** bir ayı "kəsmək"
üçün mövcud sətri bitirib yeni sətirlə davam etmək (iki ayrı dövr) tarixçəni
ikiqat artırar və hər HYS-ə görə fərqli ola bilən 50%-limit/dövr-kəsişmə
yoxlamalarını təkrar-təkrar işə salardı. Əvəzinə **`IsciHYSDondurma`** — bir
sətir = bir (IsciHYSId, Il, Ay). `IsciHYS`-in öz tarixləri **TOXUNULMUR**,
növbəti ay avtomatik davam edir. Silinmir, deaktiv edilir (`Silinib=true`) —
`MasinMuddetService`/`IsciGuzest` ilə eyni konvensiya.

**TƏK mənbə — `IIsciHYSDondurmaService.DondurulmusIdlerAsync(il, ay)`.**
Verilmiş (il, ay) üçün dondurulmuş **bütün** `IsciHYS.Id`-lərin dəstini
qaytarır; çağıran tərəf öz `IsciHYS` siyahısından bu dəstdə olanları filtrləyib
sonra cəmləyir. Kodda HYS-i aylıq pəncərə ilə sorğulayan **altı** yer var idi —
hamısı tapıldı (literal `IsciHYS` grep-i ilə başlandı, sonra çağırış zəncirini
izləyərək `TutulmalariHesablaAsync`-in dolayı çağıranları da aşkar edildi) və
hamısına **eyni naxış** (`siyahı çək → DondurulmusIdlerAsync ilə filtrlə →
sonra cəmlə`) tətbiq olundu:

| Yer | Nə üçün |
|---|---|
| `MaasHesablamaService.FerdiHesabla` (8.5-ci addım) | əsl aylıq hesablama, yazılan dəyər |
| `MaasHesablamaService.TutulmalariHesablaAsync` | məzuniyyət ödənişi vergi/DSMF bazası (bir çox çağıran: `MaasController`, `MezuniyyetOdenisController`, `User/MezuniyyetController`) |
| `HR/MaasController` (provodka, D) HYS bölməsi) | mühasibat əməliyyat yazılışı |
| `HR/MaasController` (TopluHesabla önizləməsi) | ekranla əsl hesablama uyğunsuzlaşmasın |
| `HR/MezuniyyetOdenisController` (Detal/`maasGuniNet`) | məzuniyyət ödənişinin maaş günü qalığı |
| `User/MezuniyyetController` (`Preview`, "Təxmini Net Maaş") | işçinin öz müraciət preview-u HR-ın gördüyündən fərqlənməsin |

**Audit sahələri ƏL İLƏ YAZILMIR.** `AppDbContext.SaveChangesAsync`-in
interceptor-u (`EntityState.Added` qolu) `YaradanIcraciId`-i HttpContext-dəki
cari istifadəçi ilə **şərtsiz üstələyir** — ilk versiyada servis metodlarına
`int? icraciId` parametri əlavə edilmişdi, amma bu, interceptor tərəfindən hər
zaman əvəz olunacağı üçün tamamilə mənasız idi. Silindi. `LegvEtAsync` isə
`EfRepositoryAsync.YumshakSilAsync`-in etdiyi İLƏ EYNİ: yalnız `Silinib=true` +
`SilinmeTarixi=DateTime.Now` yazır, `SilenIcraciId`-ə toxunmur (interceptor
`SilinmeTarixi` artıq dolu olanda onu doldurmur — bu, layihənin **bütün**
`YumshakSilAsync` yollarında mövcud, qəsdən dəyişdirilməmiş davranışdır).

**UI — `HR/IsciHYS`:** `Index.cshtml`-ə hər sətrə "Dondur" (qar dənəciyi ikonu)
düyməsi əlavə olundu, yeni `Dondur.cshtml` səhifəsi (il/ay/səbəb formu + mövcud
dondurmaların siyahısı, hər birində "Ləğv et"). Dondurulacaq ay **HYS-in öz
aktiv dövrünə düşməlidir** (servis yoxlayır) — əks halda dondurmağın mənası
yoxdur, o ay onsuz da hesablamaya daxil olmayacaq.

⚠️ **Routing tələsi — `DondurmaLegvEt` formunda `asp-route-id` AÇIQ yazılıb,
hidden input YOX.** `Dondur.cshtml` GET səhifəsi `{controller}/{action}/{id?}`
ambient route-unda `id=IsciHYS.Id` ilə açılır. Həmin səhifədəki
`DondurmaLegvEt` formunun öz `id` parametri (dondurma qeydinin Id-si) ambient
"id" dəyəri ilə QARIŞA bilərdi — `asp-route-id="@d.Id"` açıq yazılaraq
(`Index.cshtml`-dəki Delete formu ilə EYNİ konvensiya) bu qarışıqlığın qarşısı
alındı. "Dondur" (yarat) formunda bu risk yoxdur, çünki o action-un imzasında
`id` adlı parametr YOXDUR.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — dondurma yalnız BUNDAN SONRA hesablanan aylara
təsir edir, keçmiş `HesablamaIzahi`/`IsciAyliqQazanc` sətirlərini dəyişmir
(eyni "dondurma" prinsipi, bax "Qara Jeton MenfiMiqdar"/VM9821 bölmələri).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, hər 6 çağırış yerinin dəyişən əhatəsi — `input.Il/
Ay`, `tarix.Year/Month`, `il/ay`, `cIl/cAy`, `mezIl/mezAy`, `baslama.Year/
Month` — təkbətək oxunub təsdiqləndi, namespace-lər `FinNex.Application.
Interfaces.HR` qaydasına uyğun yoxlanıldı). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır: bir HYS-in bir ayını
dondur → FerdiHesabla/TopluHesabla önizləməsi/mühasibat provodkası/məzuniyyət
ödənişi preview-larının (HR və işçi tərəfi) HAMISININ həmin ay üçün HYS-i
çıxarmadığını təsdiqlə; sonra ləğv et → növbəti hesablamada yenidən çıxıldığını
təsdiqlə.

## Xəstəlik — "Son 12 Ay" SÜRÜŞƏN YOX, "Son 4 Rüb" SABİT Dövr İdi (26.10.2026, KRİTİK)

İstifadəçi qanun mətnini dəqiqləşdirdi: 189 saylı Əsasnamənin (30.01.2026-cı il
tarixli 31 saylı Qərarla dəyişiklik, 01.05.2026-dan qüvvədə) qaydası
**"əmək qabiliyyətinin müvəqqəti itirildiyi AYDAN ƏVVƏLKİ ARDICIL GƏLƏN 4 RÜBƏ"**
deyir — bu, SABİT təqvim rübləridir (Yan-Mar/Apr-İyun/İyul-Sen/Okt-Dek), kodun
əvvəlki "xəstəlik ayından əvvəlki sürüşən 12 tam ay" modeli DEYİL. İki müstəqil
mənbə (vergiler.az-ın iki ayrı məqaləsi, konkret tarixli nümunələrlə) eyni
qaydanı təsdiqlədi.

**Fərq yalnız rübün İLK ayında (Yanvar/Aprel/İyul/Oktyabr) başlayan xəstəliklərdə
GÖRÜNMÜR** (təsadüfən eyni nəticə verir) — qalan 8 ayda (fevral, mart, may, iyun,
avqust, sentyabr, noyabr, dekabr) 1-2 ay sürüşmə var idi, bazanı həm əskik, həm
artıq göstərə bilərdi.

**Həll — `XestelikService.Son4RubDovruTap(xestelikBaslama)`** (yeni, ortaq, `static`
helper): xəstəlik ayını əvvəlcə öz rübünün İLK ayına yuvarlaqlaşdırır
(`((ay-1)/3)*3+1` → 1, 4, 7 və ya 10), sonra 1 gün geri (dövrün sonu) və 12 ay
geri (dövrün başlanğıcı) çəkir. Xəstəliyin ÖZ rübü tamamilə kənarda qalır —
rübün hansı ayında başlamasından (ilk/orta/son) asılı olmayaraq eyni dövrü verir.

Məsələn: May 2026 (Apr-May-İyun rübü) → dövr **01.04.2025 – 31.03.2026**
(Aprel/May/İyunun HƏR biri eyni nəticəni verir — rüb daxilində fərq yoxdur).

**İKİ canlı yazıcı yeri tapıldı, ikisinə də tətbiq olundu** (literal `AddMonths(-11)`
grep-i ilə bütün repo axtarıldı):

| Yer | Nə üçün |
|---|---|
| `XestelikService.PreviewAsync` → `YeniXestelikHesablaAsync` | HR-in "Yeni Xəstəlik Bülleteni" formasında görünən ÖNİZLƏMƏ |
| `XestelikService.OdenisleriYaratAsync` | `CreateAsync`-dən çağırılan ƏSL yazma yolu — `XestelikOdenis` sətirlərini yaradır, real ödəniş |

Üçüncü bir metod, `Son12AyIsGununuHesablaAsync` (iş günü əsaslı, köhnə — May
2026-dan əvvəlki DSMF-əsaslı olmayan düsturun qalığı) **heç yerdən çağırılmır**
(grep ilə təsdiqləndi) — ölü koddur, TOXUNULMADI (dəyişiklik heç bir davranışa
təsir etməyəcəkdi).

⚠️ **Görüntü qatına (ekran) TOXUNMAQ LAZIM OLMADI.** `HR/Xestelik/Create.cshtml`-dəki
"DSMF cəmi (son 12 ay …)" etiketinin yanındakı `#ppDovr` elementi serverin
qaytardığı `Son12AyBaslangic`/`Son12AyBitis` DTO sahələrini **dinamik** göstərir
(JS-dən doldurulur) — servis düzgün tarixi qaytaran kimi ekran da avtomatik
düzəlir, ayrıca JS/Razor dəyişikliyi lazım olmadı. "Son 12 ay" etiketi hələ də
HƏQİQƏTƏ UYĞUNDUR — dövr indi də dəqiq 12 ay təşkil edir, sadəcə sürüşən deyil,
rüb sərhədlərinə bağlıdır.

**SQL diaqnostik skripti yeniləndi** — `docs/sql/hr/Xestelik_DSMF_Pencere_Yoxlama.sql`
əvvəl "sürüşən 12 ay"ı "kodun hazırkı pəncərəsi" deyə etiketləyirdi, bu artıq
doğru deyil; etiketlər "KÖHNƏ (sürüşən)" / "YENİ (sabit rüb, kodun hazırkı
pəncərəsi)" olaraq tərsinə çevrildi.

⚠️ **"Öz hesabına" (ödənişsiz məzuniyyət) — ayrıca, həll olunmuş sual.** Eyni
müzakirə zamanı aydınlaşdırıldı: öz hesabına məzuniyyət günləri bu 4-rüblük
dövrün MƏXRƏCİNDƏN (ÇTG) ÇIXILMIR — mühasiblə birgə təsdiqləndi, rəsmi mənbələrdə
(sosial.gov.az) tapılmadı. Kod onsuz da bunu heç vaxt çıxarmırdı
(`Son12AyXestelikTeqvimGunAsync` YALNIZ `Xestelikler` cədvəlini oxuyur,
`MezuniyyetNovu.OzHesabina`-ya toxunmur) — deməli bu hissədə **heç bir kod
dəyişikliyi lazım olmadı**, mövcud davranış artıq düzgün idi.

⚠️ **Rəsmi ÇTG siyahısında daha iki kateqoriya var (hamiləlik/doğum məzuniyyəti,
qismən ödənişli sosial məzuniyyət) — bunlar HƏLƏ TƏTBİQ OLUNMAYIB**, çünki
sistemdə bu iki məzuniyyət növü üçün ayrıca kateqoriya belə YOXDUR
(`MezuniyyetNovu` enum-u yalnız Illik/Xəstəlik/Ezamiyyət/DövlətVəzifəsi/
ÖzHesabına daşıyır). Bu, bugünkü işin əhatəsinə DAXİL DEYİL — ayrıca müzakirə
tələb edir.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — düzəliş yalnız BUNDAN SONRA yaradılan xəstəlik
bülletenlərinə təsir edir. Keçmiş `XestelikOdenis` sətirləri (əvvəlki sürüşən-
pəncərə ilə hesablanmış) dəyişmir, yenidən hesablanmır — bu, layihənin digər
"dondurma" presedentləri ilə eyni qaydadır (keçmişi düzəltmək ayrı iş, SQL/əl
düzəlişi tələb edir, bu sessiyada edilmədi, istifadəçi tələb etməyib).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, hər iki çağırış yerinin `var (ayBaslangic, ayBitis) =
Son4RubDovruTap(...)` tuple-deconstruction sintaksisi əl ilə oxunub təsdiqləndi).
İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə
yoxlamalıdır: rübün ORTA/SON ayında (məs. fevral, iyun, sentyabr, dekabr)
başlayan yeni bir xəstəlik bülleteni yarat → önizləmədəki dövr tarixini (`#ppDovr`)
yuxarıdakı cədvəllə tutuşdur, həm mühasib Exceli ilə ən azı bir real halı
yoxla.

## Xəstəlik Kəsintisi — TƏQVİM Günü İŞ Günü Dərəcəsinə Vurulurdu (26.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi Toplu Maaş siyahısında İlkanə Mahmudovanın "XƏSTƏLİK" kartına baxıb şübhələndi:
*"11 gün xəstəliyin günüdür amma iş günü deyil, biz cari aydakı iş günündən -11 etmişik
deyəsən, araşdır"* — haqlı çıxdı.

`MaasHesablamaService.FerdiHesabla` addım 6.5-də:
```csharp
int xestelikGun = xestelikSirketGun + xestelikDsmfGun;   // TƏQVİM günüdür!
xestelikKesinti = Math.Round(esasMaas / ayIsGunu * xestelikGun, 2);
```
`xestelikSirketGun`/`DsmfGun` (`XestelikService.OdenisleriYaratAsync`-də yaranır:
`aySirketGun = Math.Min(ayTeqvimGun, limitQalan)`) **TƏQVİM günüdür** — bu, DSMF/189 saylı
Əsasnamə qaydasına görə ÖDƏNİŞ (`xestelikSirketOdenis`) tərəfi üçün DÜZGÜNDÜR. Amma
`esasMaas / ayIsGunu` **İŞ GÜNÜ** dərəcəsidir — bunu birbaşa təqvim gün sayına vurmaq
şənbə/bazar günlərini də kəsir, halbuki işçi o günləri onsuz da işləməyəcəkdi. Hətta izah
mətni özü "{xestelikSirketGun} **iş günü**" yazırdı — əslində təqvim günü idi.

**Müqayisə — məzuniyyət bunu DOĞRU edir** (eyni fayl, "Mezuniyyet Odenisi"/"Mezuniyyet
Kesintisi" addımları): iki ayrı dəyişən var — `aySonuIGS` (bütün günlər, ÖDƏNİŞ üçün) və
`aySonuHIGS` (yalnız real iş günü, həftəsonu/bayram çıxılmış, **KƏSİNTİ üçün**). Xəstəlikdə
bu ayrım yox idi — tək `xestelikGun` (təqvim) həm ödənişə, həm kəsintiyə eyni cür gedirdi.

**Düzəliş — İKİ yerdə, EYNİ məntiqlə** (ikisi də tapıldı, ikisi də düzəldildi):
1. **Addım 6.5** (cari ayın xəstəliyi) — yeni `xestelikHaqiqiIsGun`: hər `Xestelik`
   qeydinin bu aya düşən hissəsi (`BaslamaTarixi`/`BitmeTarixi` aya kəsilmiş) üzrə,
   `AyinIsGunleriniHesablaAsync` ilə **EYNİ** gün-təsnifatından (BayramGunu.Tip==IsGunu →
   sayılır, Bayram → sayılmır, default → şənbə/bazar xaric sayılır) hesablanır. Kəsinti
   indi `esasMaas/ayIsGunu × xestelikHaqiqiIsGun`-dur, ödəniş tərəfi (`xestelikSirketOdenis`,
   təqvim əsaslı) TOXUNULMADI.
2. **Addım 6.6** (əvvəlki ay post-maaş korreksiyası) — EYNİ bug, EYNİ fayl, `xstGun` yerinə
   `xstHaqiqiIsGun`. `korreksiyaXstOdenisler` sorğusuna `.Include(x => x.Xestelik)` əlavə
   edildi (əvvəl `Xestelik` naviqasiyası yüklənmirdi, `BaslamaTarixi`/`BitmeTarixi`-ə çatmaq
   üçün lazımdır) — tapılmasa (ehtiyat) köhnə təqvim-əsaslı dəyərə geri düşür.

**İzah mətnindəki mislabel də düzəldildi**: "Xəstəlik Ödənişi (Şirkət)" addımında
`"{xestelikSirketGun} iş günü"` → `"{xestelikSirketGun} təqvim günü"`.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — yalnız BUNDAN SONRA hesablanan aylara təsir edir (eyni
"dondurma" prinsipi — keçmiş `HesablamaIzahi`/`Maas` qeydləri toxunulmayıb).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı** (mötərizə
balansı, `BayramGunu`/`GunTipi` tiplərinin faylda artıq istifadə olunduğu təsdiqləndi,
dəyişən adları mövcud scope ilə toqquşdurulmadı — `xst`/`ayBaslangic` kimi adlar başqa
metodlarda işləndiyi üçün yeni dəyişənlərə `xst`/`xstAy...` prefiksi verildi). İstifadəçi
Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır: İlkanə
Mahmudovanın (və ya həftəsonu əhatə edən hər hansı xəstəlik bülletenli işçinin) Oktyabr
kəsintisinin əvvəlkindən AZ olduğunu (yalnız real iş günləri qədər), xəstəlik ödənişinin
(831,32 ₼) dəyişmədiyini — mühasib Exceli ilə bir real halı tutuşdurmalıdır.

### ÜÇÜNCÜ DUBLİKAT — Toplu Maaş Önizləməsi (eyni gün, publish-dən sonra)

İstifadəçi publish edib yenidən baxdı — İlkanənin ekranı (Toplu Maaş →
Oktyabr) **dəqiq eyni** qalmışdı, heç nə dəyişməmişdi. Səbəb: `MaasController.
TopluHesabla` (bu ekranı yaradan GET action) `isciXestelikMap`-i **öz,
ayrıca** inline kodla hesablayırdı — eyni köhnə `esas/ayIsGunu ×
(sirketGun+dsmfGun)` (təqvim günü) düsturu, `FerdiHesabla`-nın özündən
TAMAMİLƏ MÜSTƏQİL. Bu, "Bir Kəmiyyətə Üç Yazıcı" qaydasının klassik
nümunəsidir — mən yalnız BİR yazıcını (əsl hesablama) düzəltmişdim, ekranı
yaradan İKİNCİ yazıcı toxunulmamış qalmışdı.

**Kök həll — düstur bir yerə çıxarıldı:** `MaasHesablamaService.
XestelikHaqiqiIsGunuSayAsync(isciId, il, ay)` (+ private `HaqiqiIsGunuSay
HelperAsync`, tarix-aralığı siyahısı qəbul edən ümumi helper) — bu, elə
`OzHesabinaIsGunuSayAsync`-in artıq istifadə etdiyi "paylaşılan servis
metodu" naxışıdır (`MaasController` özü bu metodu `FerdiHesabla`-nın
"Öz hesabına" kəsintisi üçün onsuz da çağırırdı, şərh ilə: *"FerdiHesablaAsync-ın
saxladığı kəsinti ilə EYNİ"* — xəstəlikdə bu NAXIŞ unudulub, əvəzinə inline
kod yazılmışdı). İndi **üç** çağıran (`FerdiHesabla` addım 6.5, addım 6.6,
`MaasController.TopluHesabla`) eyni metodu işlədir — gələcəkdə düstur
dəyişsə, tək yerdə dəyişdirmək kifayətdir.

⚠️ **Dictionary-nin tuple sahə adları diqqətlə saxlanıldı** — `Views/Maas/
TopluHesabla.cshtml:454` `ViewBag.IsciXestelikMap`-i **açıq tip adı ilə**
cast edir (`as Dictionary<int, (int sirketGun, int dsmfGun, decimal
sirketOdenis, decimal dsmfOdenis, decimal kesinti)>`) — tuple sahə adları
(`sirketGun`, `dsmfGun`...) bir hərf də dəyişsəydi `as` sakitcə `null`
qaytarardı və bütün kart "—" göstərərdi, heç bir xəta vermədən.

**Qayda (təkrar, bu dəfə üçüncü dalğada təsdiqləndi):** "düzəlişi push etdim,
build 0 xəta verdi" demək "bütün ekranlar düzəldi" demək DEYİL — eyni
rəqəmi göstərən HƏR ekranın/preview-un ÖZ hesablama yolunu olub-olmadığını
yoxla. `grep` ilə köhnə düsturun (burada: `SirketGunSayi + DsmfGunSayi`
birbaşa kəsintiyə vurulan yer) BAŞQA fayllarda da olub-olmadığına bax —
yalnız `MaasHesablamaService.cs`-i düzəltmək kifayət etmədi.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı, `ViewBag` cast-ının tuple sahə adları ilə tam eyniliyi
sətir-sətir yoxlanıldı). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra YENİDƏN publish edib İlkanənin Oktyabr sətrinə baxmalıdır — bu dəfə
kəsinti dəyişməlidir.

## Daxili Maarifləndirmə — Pilot (27.10.2026)

İstifadəçi tapşırığı: işçilərə daxili/xarici qaydalar üzərindən AI ilə bilik-yoxlama
sualları hazırlayıb göndərmək. **Pilot mərhələsi — idarəetmə TAM Admin əlindədir**,
avtomatik cədvəl/aralıqlı təkrar (spaced repetition) **QƏSDƏN YOXDUR**: Admin qayda
atır → AI qaralama hazırlayır → Admin redaktə/təsdiq edir → seçdiyi işçilərə göndərir.

**Axın:** `Admin → Daxili Maarifləndirmə → Yeni sual dəsti` → mənbə seç (aşağı bax) →
"Suallar hazırla" (AI, bir dəfəlik çağırış) → hər sualı redaktə/sil/əlavə et → Başlıq
yaz → "Yadda saxla" (Qaralama) → "İşçi seç və göndər" → işçi `/User/Marif`-də görür,
sual-sual cavablayır, **hər cavabdan DƏRHAL SONRA** düzgün cavab+izah görünür.

### Mənbə — ÜÇ seçim, kəşf olunmuş mövcud infrastruktur

İlk araşdırmada (bu sessiyanın əvvəlində) səhvən "daxili qaydalar üçün ayrıca yer
yoxdur" deyilmişdi — **bu YANLIŞ idi**. `HRDaxiliQayda` (`FinNex.Domain.Entities.AI`)
artıq mövcuddur və `HRMeslehetciController`-in (`User/HRMeslehetci/Qayda*` action-ları)
tam funksional modulu var — `Kod` (HR-001...), `Ad`, `Mezmun` (mətn), `Kateqoriya`,
`Versiya`, `Aktiv`. Sadəcə diqqətdən qaçmışdı (ilk grep yalnız `HRQanunFayl`-ı tapdı).
Mənbə indi bunu PRİORİTET sayır:

| Seçim | Mənbə | Nə üçün |
|---|---|---|
| **Daxili Qayda** (defolt) | `HRDaxiliQaydalar.Mezmun` | HR-in öz strukturlaşdırılmış, versiyalı qaydaları |
| **Qanun Faylı** | `HRQanunFayllar.MetnContent` | Xarici qanunvericilik (Əmək Məcəlləsi və s.), HR Məsləhətçinin artıq yüklədiyi fayllar |
| **Mətn / Fayl** | Admin-in özünün yapışdırdığı mətn **və ya** yüklədiyi PDF/DOC/DOCX | Heç biri uyğun gəlmirsə — fayl YALNIZ mətn çıxarmaq üçün müvəqqəti saxlanır, DMS-ə YAZILMIR (yalnız çıxarılan mətn `MenbeMetn`-ə düşür) |

`MarifSualSeti`-də hər üçü üçün nullable FK/mətn sahəsi var (`HRDaxiliQaydaId`,
`HRQanunFaylId`, `MenbeMetn`) — yalnız biri doldurulur.

### AI — yalnız GENERASİYA anında, bir dəfə

`IMarifAiService.SuallarHazirlaAsync` (`FinNex.Application/Services/AI/MarifAiService.cs`)
`HRMeslehetciService`-in Anthropic açarını/klientini (`"Anthropic"` adlı `HttpClient`)
işlədir — **ayrıca abunəlik/API açarı lazım deyil**. Sual YARANDIQDAN sonra (Admin
təsdiqləyib yadda saxlayandan sonra) AI BİR DAHA ÇAĞIRILMIR — işçiyə göstərmək,
cavablamaq, nəticəni görmək hamısı statik DB oxumasıdır.

⚠️ **Model `Claude Opus 4.7` → `Claude Sonnet 4.7`-yə DÜŞÜRÜLDÜ (27.10.2026, real
istifadə xərci).** İlk versiyada «orta sənəd (~40k token) üçün Opus ≈ $0,25, Haiku
≈ $0,005, bir dəfəlik olduğu üçün fərq əhəmiyyətsizdir» deyilib Opus seçilmişdi.
İstifadəçi real sessiyalarda faktiki xərci gördükdən sonra (iki sual dəsti ≈ 0,06
ABŞ dolları) aşağı salmağı istədi. **Haiku-ya YOX, Sonnet-ə keçirildi** — bu sual
növü diqqət tələb edən bir generasiyadır (yanlış variantların "ağlabatan səhv"
olması, uzunluq/forma balansı, sistem promptundakı 8+ incə qayda), Haiku-nun bunu
etibarlı yerinə yetirəcəyinə az güvənildi. Sonnet Opus-dan açıq-aşkar ucuzdur,
amma struktur+məntiq tələb edən bu tip generasiyada keyfiyyəti Haiku-dan daha
yaxın saxlayır. Dəyişiklik tək sətirdir (`MarifAiService.Model`), `HRMeslehetciService`/
`SenedAiService` (Opus-da qalır — kontekstual Soru-Cavab/sənəd təhlili, davamlı
istifadə, fərqli risk profili) TOXUNULMADI.

AI-dən **ciddi JSON formatında** cavab istənir (`suallar: [{tip, sual, variantA-D,
duzgunVariant, izah}]` və ya açıq sual üçün `duzgunCavab`); ```json kod bloku ilə
əhatələnərsə `Parse()` təmizləyir. JSON anlaşılmazsa açıq xəta qaytarılır, boş/yarımçıq
sual YARADILMIR.

### Sual növü — HƏR İKİSİ (istifadəçi qərarı)

`MarifSualTipi.CoxSecimli` (A/B/C/D, avtomatik qiymətləndirilir,
`MarifSual.DuzgunVariant`) və `MarifSualTipi.Acig` (sərbəst mətn, qiymətləndirilmir —
yalnız `DuzgunCavabMetni` "nümunəvi cavab" kimi göstərilir). AI-dən defolt YARISI
çoxseçimli, YARISI açıq istənir — Admin "Yarat" ekranında istənilən sualın tipini
dəyişə, silə, yeni əlavə edə bilər.

### Yanlış variantlar — "açıq-aşkar səhv" OLMASIN (istifadəçi sualı, 27.10.2026)

İstifadəçi haqlı sual verdi: variantlar bir-birindən çox fərqli olsa, işçi QAYDANI
bilmədən, sadəcə formaya baxıb düzgün cavabı tapa bilər (uzun/detallı olan, yaxud
absurd görünən yanlışlar arasından seçmək). İlk promptda bu barədə **heç bir qayda
yox idi**. Əlavə edilən açıq qaydalar (`MarifAiService.SystemPrompt`):
- 4 variant OXŞAR uzunluq/detal səviyyəsində olsun (uzunluq ipucu olmasın);
- yanlış variantlar mətnə YAXIN, "ağlabatan səhvlər" olsun (yaxın rəqəm/faiz,
  qonşu qayda, köhnəlmiş şərt) — təsadüfi/absurd DEYİL;
- "Yuxarıdakıların hamısı"/"Heç biri" İŞLƏDİLMİR;
- düzgün cavabın hərfi (A/B/C/D) təsadüfi paylansın, hər sual eyni hərfə cəmlənməsin.

⚠️ **Bu, AI-yə YALNIZ TÖVSİYƏDİR, zəmanət DEYİL** — LLM hər dəfə qaydaya tam əməl
etməyə bilər. Ona görə `Yarat.cshtml`-ə («3. Suallar» bölməsinin üstünə) Admin üçün
açıq yoxlama siyahısı əlavə edildi (eyni bəndlərin qısaldılmış forması) — son
nəzarət HƏMİŞƏ Admin-in gözündədir, AI-nin "qaralama"sı sözün əsl mənasında
qaralamadır.

**Eyni gün — başqa bir layihənin (dərslik üçün DİM-səviyyəli sual yazma sistemi,
kor-həll/rəyçi agent zənciri olan, yüzlərlə sual üçün nəzərdə tutulmuş) qaydalar
sənədindən ucuz, birbaşa prompta sığan bəndlər götürüldü** (ağır infrastruktur —
blind-solve agent, struktur skripti, pg_trgm təkrar yoxlaması, B/T/A çətinlik
metadatası, kurikulum-sırası qaydaları — QƏSDƏN KÖÇÜRÜLMƏDİ, bizim miqyasımıza
uyğun deyil, Admin-in əl ilə baxışı bizim "rəyçi"mizdir):
- **"Tipik səhv" üsulu** — hər yanlış variantı yazmazdan əvvəl "bu səhvi kim, niyə
  edər?" sualına cavab tapıb, O cavabı yazmaq (təsadüfi uydurmaq yox).
- **Variantların FORMASI da balanslı olsun**, təkcə uzunluq yox — 3 rəqəm + 1 mətn,
  yaxud 3 "Bəli" + 1 "Xeyr" kimi bölgü özü ipucu olmasın (DİM termini: VARIANT-LEAK).
- **"Mütləq söz" (yalnız/həmişə/heç vaxt) balansı** — yalnız yanlış variantlarda
  olub düzgündə olmaması (və ya əksi) bilmədən tapmağı asanlaşdırır.
- **Suallar bir-birinin cavabını açmasın** — hər sual müstəqil oxunub cavablana bilsin.
- **İzah "səhvin yolunu" desin**, qaydanı sadəcə təkrarlamasın.

### "Dərhal sonra izah" — hər sual AYRI-AYRI cavablanır

İstifadəçi qərarı: işçi hər suala cavab verən kimi (bütün testi bitirmədən) düzgün
cavab+izah görsün. `MarifController.CavabVerAsync` (User) **hər sual üçün AYRI AJAX
çağırışı** qəbul edir, `Sual.cshtml`-in JS-i (`User/Views/Marif/Sual.cshtml`) sual-sual
irəliləyir — bütöv formanı bir dəfəyə göndərmə YOXDUR.

**`CavabVerAsync` İDEMPOTENTDİR** — eyni (TesyinatId, SualId) üçün ikinci çağırış
yeni sətir YARATMIR, mövcud nəticəni geri qaytarır. Bu, həm "səhvən iki dəfə klikləmə"
qoruyucusudur, həm də **artıq cavablanmış suala qayıdanda** (`Sual.cshtml` səhifəni
yeniləsə) eyni nəticənin yenidən göstərilməsini təmin edir — `dto.Cavab` belə hallarda
boş göndərilir, servis onu oxumur (`movcudCavab != null` qolu).

### Bildiriş — `BildirisNovu.MarifTesyinatGonderildi` (yeni, 55)

`MarifService.GonderAsync` seçilmiş işçilərin HƏR BİRİNƏ **ARDICIL** (`Task.WhenAll`
YOX — bax CLAUDE.md "Bildirişlər — Paralel Yazı") bildiriş göndərir,
`redirectUrl = "/User/Marif/Sual/{tesyinatId}"`. Artıq göndərilmiş işçi **təkrar
əlavə olunmur** (`Query()` ilə mövcud `MarifTesyinat.IsciId` siyahısı yoxlanır) —
"Əlavə işçiyə göndər" düyməsi mövcud göndərişi pozmadan YENİ işçi əlavə edə bilir.

### Audit sahələri — ƏL İLƏ YAZILMIR

Bu modulda `YaradanIcraciId`/`YenileyenIcraciId`/`SilenIcraciId` servis metodlarına
**ayrıca `userId` parametri kimi ötürülmür** — `AppDbContext.SaveChangesAsync`
(~sətir 1996) bunları HttpContext-dəki cari istifadəçiyə görə artıq ŞƏRTSİZ doldurur
(bax "Qara Jeton — Dondurma" bölmələrindəki eyni interceptor). `IsciImtiyazService`
kimi köhnə servislərin `userId`-i əl ilə TƏKRAR yazması — interceptor tərəfindən
üstələndiyi üçün — artıq lazımsız clutter idi, bu modulda TƏKRARLANMADI.

### Anonim tip + `dynamic` Razor view-da İŞLƏDİLMƏDİ — açıq DTO seçildi

İlk versiyada dropdown/işçi siyahıları `Select(x => new { x.Id, x.Ad })` anonim
tipi + Razor-da `@model IEnumerable<dynamic>` ilə göstərilirdi (layihədə HEÇ BİR
presedenti yoxdur — yoxlanıldı, `grep "@model.*dynamic"` sıfır nəticə verdi). Bu,
Razor view-ların ayrı assembly-də compile oluna biləcəyi hallarda anonim tipin
`internal` görünürlüyünə görə runtime xətası riski daşıyır. Əvəzinə **açıq DTO-lar**
yazıldı — `MarifIsciSecimDto` (Id/Ad/Soyad), `MarifMenbeSecimDto` (Id/Etiket,
hazır formatlanmış mətnlə) — `FinNex.Application/DTOs/Marif/MarifSecimDto.cs`.

### JSON case tələsi — Razor-da `JsonSerializer.Serialize` birbaşa çağırılanda

`Sual.cshtml` sualları JS-ə ötürmək üçün `@Html.Raw(System.Text.Json.JsonSerializer.
Serialize(Model.Suallar))` yazmışdı — **bu, MVC-nin `Json()` helper-indən FƏRQLİ
defolt daşıyır**: `Json()` (bütün `fetch` cavablarında işlənən) ASP.NET Core-un
qlobal `JsonOptions`-u ilə **camelCase** verir, amma `JsonSerializer.Serialize`
birbaşa, options vermədən çağırılanda **PascalCase** (`SualId`, `VariantA`) qaytarır.
JS isə `s.sualId`/`s.variantA` (camelCase) oxuyurdu — uyğunsuzluq `undefined`
dəyərlərlə nəticələnərdi. Düzəliş: `Serialize`-ə açıq
`new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }`
verildi. **Qayda: Razor view-da `JsonSerializer.Serialize`-i HEÇ VAXT options
vermədən çağırma** — MVC-nin öz `Json()` nəticəsi ilə eyni səhifədə qarışdırılırsa
casing səssizcə fərqlənər.

### Fayllar

Entity-lər: `FinNex.Domain/Entities/Marif/` (`MarifSualSeti`, `MarifSual`,
`MarifTesyinat`, `MarifCavab`, `MarifSetStatusu`, `MarifSualTipi`). Migration:
`DataAccess/Migrations/20261027090000_Marif.cs` (əl ilə, `InsertData` YOXDUR).
DTO-lar: `FinNex.Application/DTOs/Marif/`. Servislər:
`FinNex.Application/Services/AI/MarifAiService.cs` (generasiya),
`FinNex.Application/Services/Marif/MarifService.cs` (CRUD/axın). Controller-lər:
`Areas/Admin/Controllers/MarifController.cs`, `Areas/User/Controllers/MarifController.cs`.

### Açıq qalan (qəsdən bu mərhələdə edilmədi)

- **Aralıqlı təkrar/cədvəl (spaced repetition) YOXDUR** — istifadəçi qərarı,
  "ilk iş və sınaqdır" sözü ilə qəsdən pilot miqyasında saxlanıldı.
- **`MarifService.SetYenileAsync`** (Qaralama statusunda redaktə) servis qatında
  YAZILIB, amma heç bir controller action/view-a BAĞLANMAYIB — hazırkı axında
  "Yarat" ekranının özü tam redaktə imkanı verdiyi üçün ehtiyac yox idi, gələcəkdə
  "artıq saxlanmış qaralamaya qayıt" funksiyası əlavə olunsa hazır metoddur.
  (Bu minillik "görünür deyil, çağırılmır" bir vəziyyətdir — gələcəkdə görünmür
  əlavə edilsə, `MarifSualSeti.Suallar` navigasiyasının `Include` zamanı soft-delete
  filtri yalnız KÖK sorğuya tətbiq olunduğunu, uşaq kolleksiyasına YOX olduğunu
  nəzərə al — bax CLAUDE.md "EF Core — Filtered Include + Tracking Tələsi".)
- **Nəticə analitikası** (hansı mövzuda çox səhv edilir, hansı şöbəyə əlavə izah
  lazımdır) — `MarifTesyinatNeticeDto` yalnız tək-tək işçi nəticəsini göstərir,
  toplu/şöbə-üzrə analitika YOXDUR.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, interfeys↔implementasiya↔DI↔çağıran dörd qatı əl ilə
tutuşduruldu, namespace-lər hər faylın öz mənbəyindən təsdiqləndi). İstifadəçi
Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır: qayda
seçib sual hazırlat → redaktə et → yadda saxla → işçi seçib göndər → işçi
`/User/Marif`-də görüb sual-sual cavablasın, hər cavabdan sonra izahın dərhal
göründüyünü təsdiqləsin.

## GOAML — Export: Görünən Təsdiq + Önizləmə Siyahısı (13.11.2026)

İstifadəçi iki şey soruşdu: *"ixracda tarixler neyi bildirir"* (cavablandı: `DateOper`
— "Əməliyyat tarixi", `CandidateDateOper`-ə fallback YOXDUR Export filtrində) və
*"bu sari olan yerdeki yazi muveqqetidir? adamin gozune girir, bilinir AI yazib"*
(aşağıda, ayrıca bəndə bax). Araşdırma zamanı REAL bir UX bugu tapıldı: **uğurlu
ixracda HEÇ BİR görünən təsdiq mesajı çıxmırdı.**

### Kök səbəb — `File()` qaytaran POST, TempData-nı HEÇ VAXT göstərmir

`EmeliyyatlarController.Export` (POST) uğurlu olanda **birbaşa `File(...)`**
qaytarırdı, redirect ETMİRDİ:
```csharp
TempData["Success"] = $"{netice.Data.SetirSayi} qeyd ixrac edildi — ...";
return File(Encoding.UTF8.GetBytes(netice.Data.JsonMetni), "application/json", ...);
```
`_UserLayout.cshtml` `TempData["Success"]`-u YALNIZ YENİ bir səhifə render
olunanda göstərir — fayl yükləməsi isə səhifəni YENİDƏN render ETMİR (brauzer
sadəcə faylı yaddaşa köçürür, ekran olduğu kimi qalır). Nəticə: ixrac
**UĞURLU OLSA BELƏ** heç bir "N qeyd ixrac edildi" mesajı görünmürdü.

### Həll — PRG (Post-Redirect-Get), "Sil" action-unun ARTIQ işlətdiyi naxış

`Export` POST indi: `JsonIxracAsync` → DMS-ə yaz (**İNDİ MƏCBURİDİR**, əvvəl
best-effort idi — PRG-də faktiki brauzer-yükləməsi AYRI bir GET-in DİSKDƏKİ
fayldan oxumasına bağlıdır, yazı sınsa `_logger.LogError` + açıq xəbərdarlıq,
qeydlərin statusu geri qaytarılmır) → `TempData["Success"]`+`TempData["IxracFayli"]`
→ `RedirectToAction(nameof(Export))`. Yeni **GET `IxracFayli(string fayl)`** —
`Path.GetFileName(fayl)` ilə TƏMİZLƏNMİŞ ad, DMS-dən oxuyub `File()` qaytarır
(qovluq-keçişi riski yoxdur). `Export.cshtml`-də `TempData["IxracFayli"]` varsa
yaşıl banner (`.gml-success-box` — **`fn-alert` DEYİL**, qlobal 4-saniyəlik
auto-hide-a düşməsin deyə, bax "Bildirişlər — fn-alert tələsi" qaydası) +
"Faylı yüklə" düyməsi göstərilir, JS səhifə açılan kimi həmin linkə avtomatik
klikləyir (brauzer bloklasa, operator əl ilə basa bilsin deyə düymə qalır).

Köhnə, səhifənin öz lokal `@if (TempData["Error"] != null)` bloku **silindi** —
layout artıq TempData Error/Success-u QLOBAL göstərir, yerli təkrar ikiqat
mesaj göstərərdi.

### Önizləmə — "hansı sənədlər ixrac olunacaq onu göstər"

Yeni `IGoamlService.IxracOnizlemeAsync(filtr)` — `JsonIxracAsync`-in İSTİFADƏ
ETDİYİ EYNİ filtr şərtini (`IxracSertiQur`, hər ikisindən çağırılan TƏK private
metod) paylaşır, **HEÇ NƏYİ dəyişdirmir** (Status mutasiyası yoxdur). "Namizədləri
göstər" düyməsi (`Onizle` AJAX POST) bu siyahını (Əməliyyat №, Əməliyyat tarixi,
Yaradılma tarixi, Debet, Kredit, Tip, Məlumat növü, Məbləğ, Təsdiqləyən) cədvəl
kimi göstərir — **"Yaradılma tarixi" ilə "Əməliyyat tarixi" yan-yana göstərilir**,
elə bu iki anlayışın FƏRQLİ olduğunu (istifadəçinin əvvəlki sualının səbəbi) əyani
edir. "İxrac et (JSON)" düyməsi önizləmə BOŞ olmayan nəticə verənə qədər
`disabled`-dir; filtr sahələrindən biri dəyişəndə (onchange) köhnə önizləmə
etibarsız sayılır, düymə yenidən sönür.

**`GoamlEmeliyyatListDto`** paylaşılan mapper-ə (`SetirleriDtoyaCevirAsync`,
`HamisiniGetirAsync`-dən ÇIXARILIB, indi `IxracOnizlemeAsync` da onu çağırır)
iki yeni string sahə aldı — `TipEtiketi`/`ReportCodeEtiketi`. **Səbəb: `Json()`
MVC helper-i enum-u DEFOLT RƏQƏM kimi yazır, string YOX** — `Siyahi.cshtml`
(server-rendered Razor) bunu `@e.Tip.Value.Etiket()` ilə özü həll edir, AMMA
AJAX/JSON nəticəsində (bu önizləmə kimi) eyni çağırış mümkün deyil; bu iki
sahə məhz onun AJAX qarşılığıdır, server tərəfdə BİR DƏFƏ hesablanır.

### Sarı "MVP" qutusu — AI-nin öz inkişaf qeydi idi, operator-dilinə çevrildi

İstifadəçi: *"bu sari olan yerdeki yazi muveqqetidir? adamin gozune girir,
bilinir AI yazib"*. Haqlı idi — köhnə blok (`<strong>Diqqət — bu, MVP (ilk
artım) ixracıdır...` + daxili sahə adları `dubl_number`/`V_YURIK__NAME`/
`ATTRIB`/`C_INN_BANK`) sırf mənim (AI-nin) öz inkişaf-zamanı qeydim idi, hər
zaman AÇIQ görünən böyük sarı qutuda. **Məzmun (bu boşluqlar REAL-dır, JSON-u
göndərmədən əvvəl yoxlanmalıdır) SAXLANILDI** — silinsəydi, operator qeyri-
tam bir faylı real portala göndərə bilərdi. Yalnız TƏQDİMAT dəyişdi:
`<details class="gml-limits">` (qatlanmış, defolt bağlı) — bir sətirlik
neytral başlıq ("Hazırlanan faylı göndərmədən əvvəl bilməli olduqlarınız"),
daxili Oracle sahə adları ÇIXARILDI, sadə, operator-dilində 2 bəndə endirildi.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı `git diff`-in əlavə/silinən sətirləri ayrıca ölçülərək —
hər ikisi balanslı; `TipEtiketi`/`ReportCodeEtiketi`-nin `Json()`-un camelCase
qaydasına uyğun JS-dəki `s.tipEtiketi`/`s.reportCodeEtiketi` adları ilə
tutuşdurulması əl ilə edildi). İstifadəçi Visual Studio-da **Rebuild All**
ilə, sonra real ssenari ilə yoxlamalıdır: filtr doldurub "Namizədləri göstər"ə
basanda cədvəlin düzgün gəldiyini (Tip/Məlumat növü mətn kimi, rəqəm YOX),
filtri dəyişəndə "İxrac et" düyməsinin yenidən sönmüş olduğunu, "İxrac et"ə
basıb təsdiqlədikdən sonra yaşıl banner+avtomatik fayl-yükləməsinin işlədiyini,
DMS qovluğuna yazıla bilməyən (məs. ad səhv) halda açıq xəbərdarlıq çıxdığını.

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

## "Paylaşımlar" — Admin-in Bölüşdüyü Rich-text Bilik Lenti (27.10.2026)

İstifadəçi tələbi: xaricdən aldığı mənalı məlumatı (tarix, maraqlı fakt və s.)
işçilərlə şəkilli, səliqəli formatda bölüşmək. **Elanlardan QƏSDƏN AYRI modul** —
istifadəçinin öz sözü: *"bu məlumatdir axı, elan deyil"*. Elan əməliyyat
xəbərdarlığıdır (vacib bayrağı, bitmə tarixi, tək banner şəkil, sadə mətn qutusu);
Paylaşım isə sırf oxuma məqsədli, mətnin İÇİNƏ bir neçə şəkil yerləşdirilə bilən,
rich-text formatlı bilik lentidir.

**Struktur:** `Melumat` entity (`Entities/Communication/`, Elanla EYNİ FK naxışı —
`GonderenIsciId → Isciler`), migration `20261027140000_Paylasim` (seed yoxdur).
`IPaylasimService`/`PaylasimService` — Clean Architecture qaydasına uyğun yeni
servis qatı (Elan-ın özü hələ köhnə, controller-də birbaşa `IUnitOfWork` işlədən
nümunədir — Paylaşım onu TƏKRARLAMADI).

**Rich-text — Quill.js (jsdelivr CDN, `quill@2.0.3`).** Admin tərəfində
(`Admin/Paylasim/Yarat`, `Redakte`) toolbar: başlıq (H2/H3), qalın/kursiv/altxətt,
siyahı, sitat, link, **mətnin içinə birbaşa şəkil**. Şəkillər ayrıca `SekilYukle`
AJAX endpoint-i ilə **`FinNex_DMS\paylasim-sekiller\`**-ə yazılır (`wwwroot`-a YOX
— mövcud DMS qaydasına uyğun), `/dms/paylasim-sekiller/{guid}.ext` kimi embed
olunur.

**Saxlamazdan ƏVVƏL server-tərəfli sanitizasiya — `HtmlSanitizer` (Ganss.Xss,
NuGet).** `PaylasimService`-in konstruktorunda bir dəfə qurulur, YARADIQ/YENİLƏ
hər ikisində `MetnHtml` saxlanmazdan əvvəl `_sanitizer.Sanitize(...)`-dan keçir —
`<script>`/`onerror` və s. atılır. Bu, layihədə `Html.Raw` ilə göstərilən İLK
admin-yaratdığı sərbəst HTML-dir (Mail HTML-i xarici mənbədəndir, Chat/Elan isə
sadə mətndir) — ona görə sanitizasiya **MƏCBURİ** sayıldı, sırf Admin-rol
məhdudiyyətinə güvənilmədi.

⚠️ **CSP-nin `style-src`-i Quill-in CSS-ini SƏSSİZCƏ BLOKLAYA BİLƏRDİ.**
`SecurityHeadersMiddleware`-in `script-src`-i artıq `cdn.jsdelivr.net`-ə icazə
verirdi (əvvəlki `marked.min.js`/mammoth/xlsx üçün, bax "Qoşma Önizləməsi"
bölməsi), AMMA **`style-src`** eyni host-u daşımırdı — `quill.snow.css`
yüklənərdi, `<link>` cavabı 200 qaytarardı, amma brauzer onu CSP-yə görə
**tətbiq etməzdi** (redaktor stilsiz, çirkin görünərdi, DevTools-da sakit bir
CSP xəbərdarlığı olardı). Toxunmazdan ƏVVƏL aşkar edilib düzəldildi —
`style-src`-ə də `https://cdn.jsdelivr.net` əlavə olundu. **Qayda:** yeni bir
CDN host-dan HƏM script, HƏM stylesheet yüklənirsə, CSP-nin İKİ DİREKTİVİNİ DƏ
(`script-src` VƏ `style-src`) yoxla — biri unudulsa, simptom "işləyir amma
eybəcərdir", funksional xəta yox.

**İşçi görüntüsü — `User/Paylasim/Index.cshtml` + `paylasim.css`**, Elanların
`elan.css` feed dizaynından ilhamlanıb (kart, kölgə, yuvarlaq künc), amma
RICH-TEXT tipoqrafiyası üçün ayrıca stil bloku (`py-body h2/h3/p/ul/ol/
blockquote/img/a`) — sadə mətn göstərən Elan-ın öz CSS-i bunun üçün kifayət
etməzdi.

**Sidebar:** Admin-də "Daxili Maarifləndirmə"nin yanında, User-də
"Maarifləndirmə"nin yanında — iki ayrı, müstəqil sətir (Marif-in quiz-əsaslı
funksiyası ilə QARIŞDIRILMADI, sadəcə qonşu yerləşdirildi).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, interfeys↔implementasiya↔DI↔çağıran dörd qatı əl
ilə tutuşduruldu, `Ganss.Xss` namespace/sinif adı veb axtarışı ilə təsdiqləndi).
İstifadəçi Visual Studio-da **Rebuild All** (HtmlSanitizer NuGet paketini
restore etməlidir) ilə, sonra real ssenari ilə yoxlamalıdır: Admin-də bir neçə
şəkilli, formatlı paylaşım yazıb göndər → işçi tərəfdə "Paylaşımlar"da gözəl
görünüşlü kart kimi göründüyünü, şəkillərin `FinNex_DMS\paylasim-sekiller\`-ə
düşdüyünü, "Gizlət"in lentdən çıxardığını təsdiqləməli.

### "Göndərən" Isci YOX, AppUser Olmalı İdi — Admin HEÇ BİR Paylaşım Yarada Bilmirdi (27.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi real mühitdə "Yeni Paylaşım" yaradıb göndərdi, nəticə **"İşçi profiliniz
tapılmadı"** xətası idi — Admin paylaşım yarada BİLMİRDİ, modulun özü istifadəyə
yararsız idi.

**Kök səbəb:** `Melumat.GonderenIsciId` **`Isci`-yə bağlı NOT NULL FK** idi (Elan-ın
öz konvensiyasını təqlid edərək). Amma Elan **HR Area**-dan (`Areas/HR/Controllers/
ElanController.cs`) yaradılır — oranı işlədən HR işçisinin `AppUser.IsciId`-si demək
olar həmişə doludur. Paylaşımlar isə **Admin Area**-dandır — Admin super-user
hesabının `IsciId`-si **boş ola bilər** (işçi profilinə bağlı deyil, sırf sistem
hesabıdır). `GetCurrentIsciIdAsync()` → `appUser?.IsciId` → `null` → hər publish
cəhdi "İşçi profiliniz tapılmadı" ilə bloklanırdı, baxmayaraq ki, Admin tam
autentifikasiya olunmuş, icazəli bir istifadəçi idi.

**Düzəliş — `Isci` asılılığı tamamilə çıxarıldı, `AppUser` ilə əvəz olundu:**
`Melumat.GonderenIsciId`/`GonderenIsci` → **`GonderenAppUserId`/`GonderenAppUser`**.
`AppUser.Id` autentifikasiya olunmuş HƏR sessiyada mütləq mövcuddur (Isci kimi
"bağlı ola bilər, ya bilməz" riski yoxdur) — `GetCurrentIsciIdAsync` →
`GetCurrentAppUserIdAsync` (`appUser?.Id`), `GonderenAdi` indi `AppUser.Ad`/
`AppUser.Soyad`-dan oxunur (Isci-də olduğu kimi bu sahələr AppUser-də də var).

**Yeni migration — `20261027170000_PaylasimGonderenAppUser.cs`** (ƏVVƏLKİ
`20261027140000_Paylasim.cs`-i DƏYİŞMƏDİM — o artıq istifadəçinin bazasında tətbiq
olunmuşdu, `__EFMigrationsHistory`-də qeydlidir; redaktə etsəydim EF onu "artıq
tətbiq olunub" sayıb təkrar işlətməzdi və sxem köhnə qalardı). Yeni migration
`GonderenIsciId` sütununu/FK-nı/indeksini **atır**, `GonderenAppUserId`-ni
`AspNetUsers(Id)`-ə FK ilə **əlavə edir**. `Melumatlar` cədvəli bu anda **boş**
idi (ilk paylaşım hələ yaradıla bilmədiyi üçün) — data itkisi riski yox idi.

**Qayda:** "Göndərən" sahəsini modelləşdirərkən **modulun HANSI Area-dan
istifadə ediləcəyini** nəzərə al — HR/User Area-dan yaradılan bir məzmun üçün
`Isci` FK-sı təhlükəsizdir (o istifadəçilərin demək olar hamısının işçi profili
var), **Admin Area-dan yaradılan** məzmun üçün isə YOX — Admin hesabı sistem
hesabı ola bilər, `AppUser.IsciId`-si boş qala bilər. Belə hallarda "göndərən"
`AppUser`-ə bağlanmalıdır, `Isci`-yə YOX.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı, `using FinNex.Domain;` namespace qaydasına uyğun — "Qovluq Adı
≠ Namespace" bölməsi, `AppUser` `FinNex.Domain`-dədir, `.Entities.HR` YOX). İstifadəçi
Visual Studio-da **Rebuild All**, sonra `Update-Database` (yeni migration tətbiq
olunsun) ilə, sonra real ssenari ilə yoxlamalıdır: Admin "Yeni Paylaşım" yaradıb
göndərsin — bu dəfə xətasız keçməli, "Göndərən" sütununda Admin hesabının adı
görünməlidir.

### Paylaşımlar — Hədəf İşçi Seçimi + "Son Seçilmişlər" Yaddaşı (27.10.2026)

İstifadəçi tələbi: *"paylasanda hami ya gedir? secim yoxdur? siyahi olardi secili
yaddasa vereredim herdefe heminki secilmisler gelerdi,istesem bir klik ile
artirardim"*. Əvvəl `AktivLentAsync()`-in heç bir hədəf filtri yox idi — hər aktiv
post bütün işçilərə görünürdü.

**Struktur — iki yeni cədvəl** (migration `20261027180000_PaylasimHedef.cs`):

- **`MelumatHedef`** (`MelumatId`, `IsciId`) — bir `Melumat`-ın HEÇ bir sətri
  YOXDURSA post **"Hamısına"**dır (geriyə uyğun — mövcud postlar toxunulmadan
  hamıya görünməyə davam edir). Sətirlər varsa, YALNIZ o işçilərə görünür.
- **`PaylasimSonHedefKitlesi`** — hər admin (`AdminAppUserId`) üçün TƏK sətir,
  "Seçilmiş işçilər" rejimində hər publish-də CSV (`IsciIdlerCsv`) olaraq
  üzərinə yazılır. Sırf UX keşidir, `MelumatHedef`-i ƏVƏZ ETMİR.

**`AktivLentAsync(int isciId)`** (imza dəyişdi — əvvəl parametrsiz idi, "Metod
İmzası Dəyişikliyi" qaydasına uyğun interfeys+implementasiya+çağıran (User
`PaylasimController`) eyni anda yeniləndi) — EXISTS alt-sorğusu ilə:
`Aktivdir && (MelumatHedef sətri YOXDUR || (MelumatHedef sətri VAR və
IsciId=bu işçi))`. `User/PaylasimController` görən işçinin `AppUser.IsciId`-sini
oxuyur; `null`dursa `0` keçirir (heç bir real Isci.Id-yə uyğun gəlmir, yalnız
"Hamısına" postlar görünür — "admin de olmadi" bugundakı eyni "AppUser.IsciId
boş ola bilər" riskinin BURADA da nəzərə alınmış forması).

**Admin Yarat/Redakte formunda — paylaşılan partial, `_HedefSecim.cshtml`:**
radio toggle ("Hamısına" / "Seçilmiş işçilər", defolt Hamısına), seçilibsə aktiv
işçilərin tam siyahısı (checkbox, client-side axtarış filtri, "Hamısını seç" /
"Təmizlə" / "İlkin seçimi bərpa et" düymələri — sonuncusu Yarat-da admin-in
"son seçilmişlər" keşini, Redakte-də postun öz mövcud hədəfini bərpa edir).
Checkbox siyahısı `AktivIscilerAsync()`-dən **İşçi Sıralaması qaydası ilə**
(Sira/Ad/Soyad) gəlir — ayrıca sıralama ƏL İLƏ YAZILMADI.

**Yarat GET-də pre-check mənbəyi `SonHedefKitlesiAsync(appUserId)`-dir** (admin-in
son istifadə etdiyi siyahı) — "bir klik ilə artırardım" tələbi elə bununla
qarşılanır: checkboxlar artıq işarəli gəlir, admin sadəcə əlavə/çıxarma edir.
**Redakte GET-də isə pre-check mənbəyi `HedefIsciIdleriAsync(melumatId)`-dir**
(postun ÖZ cari hədəfi) — admin-in ÜMUMİ son seçimi YOX, bu KONKRET postun nəyə
göndərildiyi göstərilir (düzgün gözlənti: redaktədə "bu post kimə gedib"
görünməlidir, "mən adətən kimə göndərirəm" yox).

**Validasiya** (`Yoxla`, server tərəfdə + eyni qayda JS-də): "Seçilmiş işçilər"
seçilib heç kim işarələnməyibsə — `Result.Fail`, heç nə yazılmır. "Hamısına"
seçilibsə checkbox vəziyyətindən ASILI OLMAYARAQ (gizli blokda qalan köhnə
işarələr ola bilər — CSS `display:none` POST-u bloklamır, bax "Şərtli Render
Olunan Form Sahəsi" bölməsi) `HedefleriTetbiqEtAsync` dərhal `return` edir,
`SeciliIsciIdler`-ə heç toxunmur.

**Admin siyahısında (Index.cshtml) yeni nişan** — `PaylasimListDto.HedefSayi`
(0 = "Hamısına" mavi nişan, >0 = "N işçi" sarı nişan). `HamisiniGetirAsync`
bunu **bir dəfəlik qruplaşdırılmış sayğacla** doldurur (hər post üçün ayrıca
sorğu YOX — N+1 qoyulmadı).

⚠️ **Yeni paylaşım yaradılanda İKİ `SaveChanges` lazımdır** — `MelumatHedef.
MelumatId` üçün əvvəlcə `Melumat.Id`-nin generasiya olunması (identity sütun)
şərtdir; açıq tranzaksiya işlədilmədi (aralıq uğursuzluq riski aşağıdır —
ikinci `SaveChanges` uğursuz olsa post "Hamısına" kimi sağ qalır, korrupt
vəziyyət yaranmır).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, JS `node -c` ilə sintaksis yoxlaması, interfeys↔
implementasiya↔çağıran üç qatı əl ilə tutuşduruldu). İstifadəçi Visual
Studio-da **Rebuild All**, `Update-Database`, sonra real ssenari ilə
yoxlamalıdır: "Seçilmiş işçilər" ilə 2-3 nəfərə paylaşım göndər → yalnız
onların `/User/Paylasim`-də gördüyünü, digərlərinin görmədiyini, Admin
siyahısında "N işçi" nişanının göründüyünü, yeni paylaşım yaradanda
əvvəlki seçimin avtomatik işarəli gəldiyini təsdiqləməli.

### Paylaşımlar — Kateqoriya + Qaralama/Paylaş Axını, GPT Mockup-una Uyğun Redizayn (27.10.2026)

İstifadəçi ChatGPT-də hazırlanmış "Yeni Paylaşım" və "Paylaşımlar" lenti mockup-larını
göstərdi (*"gpt nin tesviri mence yaxsi idi"*). Mockup-da bizdə olmayan 5 yeni şey var idi:
Kateqoriya, Qısa təsvir, ayrıca Üz qabığı şəkli, Qaralama/Paylaş axını, Axtarış+"Digər
paylaşımlar"+"Yadda saxla". Model öz tövsiyəsini verdi (*"bu, pilot modul, hamısı lazım
deyil"*) — istifadəçi **Kateqoriya + Qaralama/Paylaş**-ı seçdi, Qısa təsvir/Üz qabığı
(Quill-in artıq etdiyini təkrarlayır) və Axtarış/Yadda saxla/Digər paylaşımlar (say az
olanda mənasız) buraxıldı.

**Kateqoriya — açar-cədvəl** (`PaylasimKateqoriyasi`, `KocurmeSenedNovleri` ilə EYNİ
naxış): migration `20261027190000_PaylasimKateqoriyaQaralama.cs` 4 başlanğıc dəyərlə
**seed olunur** (`migrationBuilder.Sql(INSERT)` — `InsertData` YOX): Bankçılıq, Maliyyə
tarixi, Təhlükəsizlik, Digər — bu, boş başlayan açar-cədvəllərdən (KocurmeSenedNovleri)
QƏSDƏN FƏRQLİDİR, çünki istifadəçi mockup-dakı konkret adları göstərmişdi, boş dropdown
ilə başlamaq addım geri olardı. Admin "+ Yeni" düyməsi ilə (AJAX, `KateqoriyaYarat`
endpoint) sərbəst əlavə edə bilir — eyni ad artıq varsa (aktiv), dublikat yaratmır,
mövcudu qaytarır.

**Qaralama/Paylaş — `Melumat.Qaralamadir` (yeni bool), `Aktivdir`-dən AYRI.**
`Aktivdir` YALNIZ artıq dərc olunmuş postun Göstər/Gizlət vəziyyətini idarə edir;
`Qaralamadir=true` isə "hələ HEÇ KİMƏ görünməyib" deməkdir — iki bayraq qarışdırılsaydı
(məs. tək `Aktivdir`-dən "qaralama" da ifadə etməyə çalışsaydıq) bir qaralamanın
"Gizlədilmiş paylaşılmış post" ilə eyni sayılması riski olardı.

- **Yarat formu** — iki submit düyməsi, EYNİ `name="QaralamaOlaraqSaxla"`, fərqli
  `value` (`"false"`=Paylaş, `"true"`=Qaralama saxla) — standart HTML submit-button
  davranışı: yalnız basılan düymənin ad/dəyər cütü POST-a düşür, əlavə JS/hidden sahə
  lazım deyil.
- **Redaktə formu** — `ViewBag.Qaralamadir` (DB-dəki CARİ dəyər) hansı düymə dəstinin
  göstəriləcəyini həll edir: hələ Qaralamadırsa → eyni iki düymə; artıq paylaşılıbsa →
  tək "Yadda saxla" (bu halda `dto.QaralamaOlaraqSaxla` POST-a heç düşmür, servis onu
  oxumur da — `melumat.Qaralamadir` DB-dən fetch edilən CARİ dəyər `true` olmadıqca
  servis bu sahəyə toxunmur).
- `PaylaAsync(id)` — yeni, ayrıca endpoint (`POST /Admin/Paylasim/Payla/5`), Admin
  siyahısında qaralama kartının "Gizlət/Göstər" əvəzinə göstərdiyi **"Paylaş"** düyməsi
  bunu çağırır. Artıq paylaşılmış postda çağırılarsa (UI-dan mümkün deyil, amma
  defensiv) `Result.Fail("Bu paylaşım artıq dərc olunub.")` — səssiz no-op YOX.

**`AktivLentAsync` (işçi lenti) indi `!x.Qaralamadir`-i də yoxlayır** — `Aktivdir`
default `!dto.QaralamaOlaraqSaxla` ilə təyin olunduğu üçün nəzəri olaraq ehtiyac
yoxdur (qaralama heç vaxt `Aktivdir=true` olmur), amma ikinci qoruyucu kimi saxlanıldı
— eyni prinsip "Aktiv ≠ tam dəyərli" (Qara Jeton) qaydasının bu formasıdır: bir
bayrağa güvənmək əvəzinə hər iki şərti aydın yazmaq.

**Görüntü — `_KateqoriyaSecim.cshtml` yeni partial**, `_HedefSecim.cshtml` ilə
birlikdə `Yarat.cshtml`/`Redakte.cshtml`-in YENİ iki-sütunlu (`col-lg-8` məzmun,
`col-lg-4` "Paylaşım ayarları" sidebar) düzümünə köçürüldü. `_HedefSecim.cshtml`-in
ÖZ `<div class="card">` örtüyü SİLİNDİ (əvvəl müstəqil kart idi) — indi sidebar-ın
ÖZ kartının içindədir, iki iç-içə kart görünməsin deyə.

**Admin siyahısında** üç-vəziyyətli nişan (Qaralama/Aktiv/Gizli) + kateqoriya nişanı.
**İşçi lentində** kateqoriya nişanı (kart başlığının sağında) + **client-side
filtr pill-ləri** ("Hamısı" + mövcud postlarda rast gəlinən kateqoriyalar) — server
sorğusu YOX, sadəcə `.py-card[data-kat]`-ə görə JS `style.display` keçidi (say az
olduğu üçün AJAX/server-side axtarış əsaslandırılmır, istifadəçi ilə razılaşmış
əhatəyə uyğun).

⚠️ **GERİYƏ TƏTBİQ OLUNUR, DAVRANIŞ DƏYİŞMİR** — mövcud paylaşımların `KateqoriyaId`
`null`, `Qaralamadir` `false` (sütun defolt dəyəri) olaraq qalır — onlar indi də
"Aktiv, kateqoriyasız" kimi görünməyə davam edir, heç biri qəfil "Qaralama"ya düşmür.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, JS `node -c` ilə hər iki yeni script bloku, interfeys↔
implementasiya↔çağıran üç qatı əl ilə tutuşduruldu). İstifadəçi Visual Studio-da
**Rebuild All**, `Update-Database`, sonra real ssenari ilə yoxlamalıdır: yeni
paylaşım "Qaralama saxla" ilə göndər → heç kimə görünmədiyini, Admin siyahısında
"Qaralama" nişanı + "Paylaş" düyməsi olduğunu təsdiqlə; "Paylaş"a bas → işçi
lentində göründüyünü yoxla; "+ Yeni" ilə yeni kateqoriya yarat, dropdown-da
dərhal seçili gəldiyini, işçi lentindəki filtr pill-lərinin düzgün süzdüyünü
təsdiqlə.

## Razor — "Bare" Şərtli Atribut (`@(...)` AD OLMADAN) = RZ1031 (27.10.2026, KRİTİK)

`<option>`/`<input>` kimi tag-helper-hədəflənən bir elementdə, mövcud atributlardan
SONRA **ad OLMADAN** birbaşa `@(...)` (və ya `@deyisenAdi`) yazmaq **RZ1031** verir:
*«The tag helper 'option' must not have C# in the element's attribute declaration
area.»* Real hadisə: Visual Studio Rebuild-də `FinNex.UI` build FAILED oldu,
Paylaşımlar modulunun `_KateqoriyaSecim.cshtml:13` və `_HedefSecim.cshtml:11/14/37`
sətirlərində:

```cshtml
❌ <option value="@k.Id" @(Model.KateqoriyaId == k.Id ? "selected" : "")>@k.Ad</option>
❌ <input type="radio" ... value="true" @(hamisinaGonder ? "checked" : "")>
❌ <input class="form-check-input" ... value="@isci.Id" id="pyIsci_@isci.Id" @checkedAttr>
```

Bu, köhnə ("əsas-Razor") səhifələrdə bəzən işləyən bir qısayoldur, amma
tag-helper-hədəflənən elementlərdə (CLAUDE.md-dəki "Şərhdə Tag Helper Adı" siyahısı:
`form, input, select, textarea, a, img, label, link, script, environment`) Razor
atributu AD-DƏYƏR cütü kimi parse etməyə çalışır — adsız, floating bir C# ifadəsi
bu parsinqi sındırır.

**Düzgün üsul — Razor-un ÖZ "şərtli atribut" mexanizmi, atribut AD-I ilə:**

```cshtml
✅ <option value="@k.Id" selected="@(Model.KateqoriyaId == k.Id ? "selected" : null)">@k.Ad</option>
✅ <input type="radio" ... value="true" checked="@hamisinaGonder">
✅ <input ... value="@isci.Id" id="pyIsci_@isci.Id" checked="@secili">
```

Razor-un daxili qaydası: atribut dəyəri **TƏK bir ifadədirsə** və nəticə `bool`-dursa
(`false` → atribut TAMAMƏN ATILIR, `true` → `attr="attr"` kimi render olunur) **və ya**
`string`-dirsə və `null`-dursa (atribut atılır) — bu, `checked`/`selected`/`disabled`/
`readonly`/`multiple` kimi HTML boolean atributları üçün təhlükəsiz, standart yoldur.
**`""` (boş sətir) ATILMIR** — `checked=""` kimi render olunur ki, HTML-də bu, "işarəli"
sayılır (atributun VARLIĞI kifayətdir, dəyəri yox) — ona görə `"checked"/""` string
cütü əvəzinə **`bool` dəyişən** (`checked="@boolDeyisen"`) işlətmək daha təhlükəsizdir,
ya da string/`null` cütü (boş sətir YOX, `null`) istifadə et.

**Qayda:**
- Tag-helper-hədəflənən bir elementdə şərtli atribut yazanda HƏMİŞƏ **`atribut-adı=
  "@(...)"`** formasını işlət — heç vaxt adsız, floating `@(...)`/`@dəyişən` yazma.
- Şərtli boolean HTML atributları (`checked`, `selected`, `disabled`, `readonly`)
  üçün ən təhlükəsiz yol: dəyəri **`bool`** kimi hesablayıb birbaşa `attr="@boolIfade"`
  yaz — `"checked"`/`""` string oyunundan çəkin (boş sətir "atribut var" deməkdir).
- Bu sessiyada `dotnet build` mümkün olmadığı üçün bu bug yalnız istifadəçinin real
  Visual Studio Rebuild-ində üzə çıxdı — "yalnız əl ilə yoxlanıldı" xəbərdarlığının
  elə özünün nümunəsidir (mötərizə balansı bu cür sintaksis xətasını TUTMUR, çünki
  RZ1031 Razor-un tag-helper parserinin SEMANTIK qaydasıdır, ümumi `{}`/`()` balansı
  deyil).

## Paylaşımlar — Yeni Post Nişanı (Sidebar) + "Yeni" Pilli Kart (27.10.2026)

İstifadəçi tələbi: *"paylasimlar terefde, yeni paylasim oldugunu bildire bilerik?
misal ustunde bir isare olsa, ne deyirsen?"* — iki yüngül, DB-yə minimum toxunan
əlavə edildi.

**1. Sidebar nişanı — "son baxış" tarixinə görə sayğac.** Yeni cədvəl
`PaylasimBaxisler` (`PaylasimBaxis` entity, `PaylasimSonHedefKitlesi` ilə EYNİ
naxış — hər `AppUser` üçün tək sətir, migration `20261027200000_PaylasimBaxis.cs`,
`InsertData` YOXDUR). `/User/Paylasim` açılanda `GorulduIsaretleAsync` sətri
`DateTime.Now`-a yeniləyir; `GET /User/Paylasim/YeniSay` (`{ say: N }`) bu
tarixdən sonra yaradılmış, işçiyə GÖRÜNƏN (eyni `AktivLentAsync`-in
Aktivdir/Qaralamadir/hədəf filtri) post sayını qaytarır.

**Dublikat düstur yazılmadı — `GorunenlerSorgusu(isciId)` ortaq private helper-ə
çıxarıldı.** Əvvəl `AktivLentAsync`-in özündə olan Aktivdir/Qaralamadir/hədəf-EXISTS
şərti indi `IQueryable<Melumat>` qaytaran bir metoddadır, həm `AktivLentAsync`
(lentin özü), həm `YeniSayAsync` (sayğac) onu çağırır — "Bir Kəmiyyətə Üç Yazıcı"
prinsipinin əksi: **iki oxuyucu bir mənbədən**, filtr heç vaxt uyğunsuzlaşa bilməz.

**Görüntü — Mail-in `.fn-mail-badge`-i ilə EYNİ dizayn, amma GLOBAL CSS-də.**
Kəşf edildi: `.fn-mail-badge`-in CSS-i `gelen-mail.css`-dədir, bu fayl isə YALNIZ
mail səhifələrində yüklənir — sidebar isə hər səhifədə göstərilir. Yeni
`.fn-paylasim-badge` sinifi bu səbəbdən **global `topnav.css`-ə** yazıldı (Mail
badge-in köhnə, kiçik bir boşluğunu TƏKRARLAMAMAQ üçün) — sidebar HƏR səhifədə
(təkcə Paylaşım səhifələrində yox) düzgün görünür. JS tərəfi Mail-in öz
`refreshMailBadge`-inə TOXUNMADAN, AYRI, kiçik bir blokdadır (2 dəqiqəlik
interval, eyni fetch+say naxışı) — risk minimuma endirildi.

**2. "Yeni" pilli kart — sırf görüntü, DB lazım deyil.** İşçi lentindəki hər
kartda `YaradilmaTarixi`-dən 3 gün keçməyibsə yaşıl "Yeni" nişanı (kateqoriya
nişanı ilə yanaşı, `py-head-badges` ortaq konteynerində). Bu, sidebar nişanından
**TAMAMİLƏ MÜSTƏQİLDİR** — biri "mən bunu hələ görməmişəm" sualına (son-baxış
tarixi), o biri "bu, təzə yazılıb" sualına (yaradılma tarixi) cavab verir, iki
fərqli məqsəd, iki fərqli mənbə.

⚠️ **Push bildirişi (desktop toast) ƏLAVƏ EDİLMƏDİ** — qərar istifadəçi ilə
müzakirə edildi, hər post üçün bütün hədəf işçilərə bildiriş göndərmək (Marif-in
etdiyi kimi) pilot miqyasda artıq olardı; sadə sidebar nişanı kifayət sayıldı.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, JS `node -c` ilə yeni script bloku, interfeys↔
implementasiya↔controller↔view qatları əl ilə tutuşduruldu). İstifadəçi Visual
Studio-da **Rebuild All**, `Update-Database`, sonra real ssenari ilə
yoxlamalıdır: yeni paylaşım göndər (başqa istifadəçi kimi) → sidebar-da qırmızı
rəqəm çıxsın → `/User/Paylasim`-ə gir → nişan sıfırlansın, kartda "Yeni" pilinin
göründüyünü təsdiqlə.

### Paylaşımlar — Tipoqrafiya, Eni, Başlıq Təkrarı, Önizləmə/"Davamını oxu" (27.10.2026)

İstifadəçi canlı ekrana baxıb konkret oxunaqlılıq şikayətləri verdi: mətn xırda
(15,5px/1,75), abzaslar arası həddindən artıq açıq (boş `<p><br></p>` sətirləri
Quill-dən qalırdı), şəkil həddindən artıq yer tuturdu, "Paylaşımlar" başlığı
EKRANDA İKİ DƏFƏ görünürdü. Sonra kartın enini (760px → 850-900px) açmağı,
HƏM DƏ lentin uzun olmamasını (hər post üçün şəkil + 2-3 sətir önizləmə +
"Davamını oxu" ilə tam mətn açılsın) istədi.

**Başlıq təkrarı — kök səbəb `_UserLayout.cshtml`-in öz topbar-ı.** Yoxlanıldı:
`fn-topbar-title` elementi onsuz da `@ViewData["Title"]`-ı ("Paylaşımlar")
göstərir (sətir 1578) — səhifənin öz `fn-page-head`-indəki `fn-page-title`
divi bunu TƏKRARLAYIRDI. Yalnız o div SİLİNDİ, `fn-page-sub` ("Oxumağa dəyər
məlumatlar") saxlanıldı — bu, Mail modulunda eyni bugun (bax yuxarı "Üç İş"
bölməsi) tətbiq olunmuş düzəlişin eyni forması.

**Tipoqrafiya — `paylasim.css`, `.py-body`:** `font-size: 15.5px → 16px`,
`line-height: 1.75 → 1.6`, paraqraf `margin-bottom: 14px → 16px`, siyahı
bəndi `margin-bottom: 4px → 2px` (daha yaxın). Mobil media sorğusundaki
ayrıca kiçik `font-size: 14.5px` override-i SİLİNDİ — mobil də indi 16px.

**Boş paraqraflar — CSS YOX, JS.** Quill boş sətirləri `<p><br></p>` kimi
yazır; CSS `:has(> br:only-child)` seçicisi bunu həll edə bilərdi, amma
brauzer dəstəyinə görünməz güvənmək əvəzinə JS seçildi: səhifə yükləndikdə
`.py-body p` üzərində gəzib, mətni boş VƏ içində şəkil YOXDURSA `display:none`
yazılır — DB-yə TOXUNULMUR, sırf göstərmə qatıdır.

**Şəkil hündürlüyü — `max-height: 420px` (əvvəl heç bir hədd yox idi, yalnız
`max-width:100%`).** `width:auto; height:auto` ilə — nisbət POZULMUR, şəkil
YALNIZ kiçilir (heç vaxt kəsilmir, `object-fit:cover` kimi kəsmə üsulu
İŞLƏDİLMƏDİ — istifadəçinin "şəkildəki yazıları kəsmə" tələbinə görə).

**Kart eni — `.py-filters`/`.py-feed`, `max-width: 760px → 880px`.**

**Lent önizləməsi + "Davamını oxu" — TAMAMILƏ CSS/JS, BACKEND DƏYİŞMƏDİ.**
Ayrıca "qısa təsvir"/"üz qabığı" DB sahəsi (bu modulun ƏVVƏLKİ bir dalğasında
GPT-mockup müzakirəsində İSTİFADƏÇİ TƏRƏFİNDƏN QƏSDƏN rədd edilmişdi — bax
yuxarı "Kateqoriya + Qaralama/Paylaş Axını" bölməsi) YARADILMADI — əvəzinə
`.py-body`yə default `py-collapsed` sinfi (max-height:260px, overflow:hidden,
aşağıda ağ qradient fade) qoyulur, kolaps halında `.py-body.py-collapsed img`
əlavə olaraq `max-height:150px`-ə kiçildilir (belə ki, kolaps təxminən
"kiçik şəkil + bir neçə sətir" effekti versin — bu, dəqiq "2-3 sətir" ÖLÇÜSÜ
DEYİL, GÖZLƏ YAXIN bir təxmindir, çünki məzmun qarışıq rich-HTML-dir, sərt
sətir-kəsimi mümkün deyil).

**Button YALNIZ lazım olanda görünür.** Hər kartın altında gizli (`style=
"display:none"`) "Davamını oxu" düyməsi var; səhifə tam yükləndikdə (`window.
load`, ŞƏKİLLƏR DAXİL) hər kartın `scrollHeight`-i kolaps hündürlüyündən
böyükdürsə düymə göstərilir, əks halda `py-collapsed` sinfi tamamilə
götürülür (qısa post heç vaxt kəsilmir, boş düymə görünmür). Kliklə
`classList.toggle('py-collapsed')` — mətn açılır/bağlanır, düymə mətni
"Davamını oxu" ↔ "Bağla" arasında dəyişir.

⚠️ **`window.addEventListener('load', ...)` TƏK BAŞINA RİSKLİ İDİ — özüm
yoxlayarkən tapıb düzəltdim.** Skript səhifənin SONUNDA yerləşir; şəkillər
keşdən sürətli yüklənərsə, brauzerin `load` hadisəsi bu listener qoşulmazdan
ƏVVƏL artıq atəşlənmiş ola bilər — belə halda listener HEÇ VAXT çağırılmaz
və bütün kartlar ƏBƏDI kolaps qalardı (düymə də görünməzdi, çünki onu göstərən
kod da elə bu funksiyanın içindədir). Düzəliş: `document.readyState ===
'complete'` yoxlanılır — artıq yüklənibsə funksiya DƏRHAL çağırılır, əks
halda `load`-a qoşulur.

**Qayda (təkrar, YENİ formada):** `window.load`/`DOMContentLoaded` kimi
"bir dəfə atəşlənən" hadisələrə skriptin ÖZ YÜKLƏNMƏ VAXTINDAN asılı
olmadan güvənmə — hadisə artıq keçmiş ola bilər. Əvvəlcə `document.
readyState` (və ya instər oxşar bayraq) yoxla, lazım gələrsə dərhal çağır,
yoxsa listener qoş.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(CSS/Razor mötərizə balansı skriptlə). İstifadəçi real brauzerdə yoxlamalıdır:
mətnin indi 16px/1.6 ilə rahat oxunduğunu, boş sətirlərin getdiyini, şəklin
daha kiçik və KƏSİLMƏDƏN göründüyünü, "Paylaşımlar" başlığının bir dəfə
göründüyünü, kartın bir qədər enli olduğunu, uzun postda "Davamını oxu"
düyməsinin çıxıb tam mətni açdığını, qısa postda isə düymənin ümumiyyətlə
görünmədiyini.

### Paylaşımlar — "Böyük kart" Lentindən Sıx SİYAHI Dizaynına (28.10.2026, GPT mockup)

İstifadəçi ChatGPT-də hazırladığı bir redizayn mockup-ı göstərdi (*"bele daha
seliqeli gorunur mence, sen bize daha seliqeli dizayna sala bilmirsene"*) —
hər post tam genişlikdə açıq kart kimi yox, **kompakt sətir** kimi: solda
kiçik ikon/miniatür, üstdə "Yeni | tarix | kateqoriya" meta sətri, qalın
başlıq, 2 sətirlik boz önizləmə mətni, göy "Oxumağa davam et →" linki, sağda
ox işarəsi. Üstdə "Son paylaşımlar · N paylaşım" başlıq zolağı.

**Əvvəlki "kolaps + Davamını oxu" mexanizmi (27.10.2026-da qurulmuşdu) TAM
ƏVƏZ OLUNDU** — köhnə üsul `.py-body`-ni 260px-ə kəsib aşağıda qradient
göstərirdi (hələ də tam HTML-in bir hissəsi görünürdü); yeni üsulda
DEFOLT vəziyyətdə body ÜMUMİYYƏTLƏ görünmür, yalnız ondan ÇIXARILAN
(ikon/mətn) qısa xülasə göstərilir, klikləyəndə TAM GÖRÜNTÜ (`.py-row-full`)
əvəz edir.

**Backend-ə HEÇ TOXUNULMADI — sırf CSS/JS.** "Qısa təsvir"/"üz qabığı" DB
sahəsi YENƏ DƏ əlavə edilmədi (bu, modul daxilində artıq İKİNCİ dəfə
istifadəçinin qərarı ilə qəsdən buraxılan bir şeydir — bax yuxarı
"Kateqoriya + Qaralama/Paylaş Axını" bölməsi) — əvəzinə client-side JS,
artıq DOM-da (gizli) olan `.py-body`-nin HTML-indən iki şey çıxarır:
1. **Miniatür** — body-nin İLK `<img>`-i varsa onun `src`-i miniatür kimi
   göstərilir (`object-fit:cover`, 92×84px); yoxdursa, sətir indeksinə görə
   6 rəngdən birini alan boş bir ikon kvadratı (`py-thumb-c0..c5`,
   `bi-journal-richtext`) göstərilir.
2. **Önizləmə mətni** — `bodyEl.textContent` (bütün teqlər atılmış xam mətn),
   130 simvoldan sonra "…" ilə kəsilir, CSS `-webkit-line-clamp:2` ilə 2
   sətirə də MƏHDUDLAŞDIRILIR (qoşa qoruyucu — uzun sözlü dillərdə JS kəsimi
   təsadüfən 3-cü sətrə düşə bilər, CSS son sərhəddir).

**Sadələşmə — `window.load` gözləməsi artıq LAZIM DEYİL.** Əvvəlki "Davamını
oxu" mexanizmi `scrollHeight`-i ÖLÇMƏLİ olduğu üçün şəkillərin yüklənməsini
(`window.load`) gözləməli idi (bax yuxarı "`window.addEventListener('load',
...)` TƏK BAŞINA RİSKLİ İDİ" bölməsi — orada tapılan `readyState` qoruyucusu
bu yeni dizaynda artıq İŞLƏNMİR). Yeni üsulda miniatür üçün yalnız `<img>`
elementinin `src` ATRİBUTU lazımdır (şəklin ÖZÜNÜN yüklənməsi yox) — bu,
DOM parse olunan kimi (DOMContentLoaded-dan ƏVVƏL belə) mövcuddur, ona görə
skript sadəcə səhifənin sonunda, heç bir hadisəyə qoşulmadan dərhal işləyir.

**Klik davranışı:** `.py-row-summary`-ə (bütün sətrə, təkcə linkə yox) klik
`.py-row-full`-u göstərir, `.py-row-summary`-ni gizlədir; aşağıdakı "Bağla"
düyməsi tərsini edir və `scrollIntoView({block:'nearest'})` ilə sətri
yenidən görünən sahəyə qaytarır (uzun post bağlananda səhifə yuxarı
sıçramasın deyə).

⚠️ **Mockup-dakı FƏRQLİ-FƏRQLİ ikonlar (bank, zərf+qalxan, kilid) BİRƏ-BİR
TƏKRARLANMADI** — onlar GPT-nin konkret məzmuna uyğun çəkdiyi illüstrasiyalardır,
bizim datada belə bir "ikon seçimi" sahəsi yoxdur (və əlavə edilmədi — yuxarıdakı
qərara görə). Əvəzinə: şəkilli postda ÖZ şəkli, şəkilsiz postda isə NEYTRAL,
rəng-rotasiyalı bir ikon istifadə olunur — mockup-un GÖRÜNTÜ dilini (kvadrat
rəngli tile) qorudu, konkret ikon seçimini YOX.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(Razor/CSS mötərizə balansı skriptlə, hər iki `<script>` bloku `node -c` ilə
JS sintaksisi üçün ayrıca yoxlanıldı — hər ikisi təmiz). İstifadəçi real
brauzerdə yoxlamalıdır: siyahının indi sıx sətirlər şəklində göründüyünü,
şəkilli postda miniatürün, şəkilsiz postda rəngli ikon kvadratının çıxdığını,
sətrə klikləyəndə tam mətnin (şəkil/format daxil) açıldığını, "Bağla"
düyməsinin bağlayıb sətri geri gətirdiyini, kateqoriya filtrinin YENİ
`.py-row` elementlərinə görə işlədiyini.

### Paylaşımlar — Dəqiq Ölçü/Proporsiya Düzəlişi (278.png maketinə görə, 28.10.2026)

İstifadəçi əvvəlki sıx-siyahı redizaynını (yuxarı) "funksiya yox, ölçü/
proporsiya" məsələsi kimi dəqiqləşdirdi — ayrı bir maket (`278.png`) ilə
dəqiq piksel/nisbət tələbləri verdi. Yeddi konkret dəyişiklik edildi,
**backend-ə TOXUNULMADI**, yalnız `Index.cshtml` + `paylasim.css` +
`_UserLayout.cshtml`-də bir bayraq:

1. **Siyahının eni** — `.py-filters`/`.py-list-wrap` indi `max-width:1200px;
   width:100%; margin:0 auto; padding:0 32px; box-sizing:border-box;`
   (əvvəlki 880px götürüldü). `.py-empty` artıq öz max-width-ini SAXLAMIR —
   o, `.py-list-wrap`-in DAXİLİNDƏdir, valideynin 32px gutter-i kifayətdir
   (iki qat 32px padding stacking-i qarşısı alındı).

2. **Desktop grid** — `.py-row-summary` `display:flex`-dən **`display:grid;
   grid-template-columns:280px minmax(0,1fr) 20px; gap:28px; padding:24px;
   align-items:center;`**-ə keçdi. Markup-un özü dəyişmədi — sətir artıq
   DƏQİQ üç uşaq elementdən ibarət idi (`.py-row-thumb`, `.py-row-main`,
   `.py-row-chevron`), elə bu üçü grid-in üç sütununa birbaşa düşdü.

3. **Şəkil** — `.py-row-thumb` `92×84px`-dən `280×158px`-ə sabitləndi,
   `object-fit:cover` → **`contain`** (şəkildə yazı ola bilər, kəsilməsin).
   Rəngli `.py-thumb-cN` fonu saxlanıldı — `contain`-in boş qalan kənarları
   indi bu rəngi göstərir (letterbox effekti, qəsdən).

4. **Şriftlər** — başlıq 16px/700 → **22px/1.3/600**; önizləmə 13px →
   **16px/1.6**; meta sətri (tarix+kateqoriya+"Yeni") 11.5px → **13px**;
   "Oxumağa davam et" 12.5px → **15px**.

5. **Önizləmə 3 sətir** — `-webkit-line-clamp:2` → **`3`**. Mətn mənbəyi
   DƏYİŞMƏDİ — JS artıq `bodyEl.textContent` işlədirdi (bu, şəkil/teq
   daşımayan TƏMİZ mətndir, heç bir dəyişiklik lazım olmadı), yalnız ehtiyat
   hərf həddi (CSS-in əsl kəsici olduğu, JS-in ötürdüyü xam mətni həddindən
   artıq böyütməmək üçün) 130-dan **260**-a qaldırıldı (sütun indi qat-qat
   enlidir, 3 sətirə daha çox söz sığır).

6. **Yuxarı ağ "Geri / Paylaşımlar / tarix" zolağı — YALNIZ bu səhifədə
   gizlədildi.** `_UserLayout.cshtml`-ə yeni, qlobal defolt-u DƏYİŞMƏYƏN bayraq
   əlavə edildi: `@if (ViewData["HideTopbar"] == null) { <div class=
   "fn-topbar-wrap">...</div> }` — bayraq `null` olduqda (bütün DİGƏR
   səhifələrdə) əvvəlki kimi göstərilir, YALNIZ Paylaşım-ın özü
   `ViewData["HideTopbar"] = true;` yazdığı üçün öz topbar-ı yoxa çıxır.
   Əvəzinə yeni `.py-page-header` bloku: **28px** `.py-page-title` +
   altında `.py-page-desc`. Header-in `margin-bottom:8px`-i `.fn-page`-in
   öz flex `gap:16px`-i ilə BİRLİKDƏ dəqiq **24px** verir (iki ayrı CSS
   mənbəyinin cəmi, `.fn-page`-in paylaşılan `gap` qaydasına TOXUNULMADAN).

7. **Mobil — şəkil yuxarıda, mətn aşağıda.** Əvvəlki kiçik-inline-thumb +
   gizli-excerpt üsulu SİLİNDİ, əvəzinə `@media (max-width:680px)` daxilində
   `.py-row-summary` `grid-template-areas:"thumb" "main";` ilə TƏK sütuna
   keçir — şəkil (`aspect-ratio:280/158`, tam en) yuxarıda, mətn aşağıda.
   Üfüqi daşmanın qarşısını almaq üçün `.py-row-title`/`.py-row-excerpt`-ə
   `overflow-wrap:break-word; word-break:break-word;` əlavə olundu (uzun,
   boşluqsuz bir sözün sütunu basması riskinə qarşı).

⚠️ **Canlı kodda tapılan, ÖZÜM aşkar edib düzəltdiyim gizli bug — "Bağla"
düyməsi grid-i flex-ə çevirirdi.** `py-row-collapse-btn` klikində JS
`row2.querySelector('.py-row-summary').style.display = 'flex';` yazırdı —
bu, (3)-cü addımdan ƏVVƏL `.py-row-summary` CSS-də `display:flex` olduğu
üçün düzgün idi, amma indi `display:grid`-dir. İnline stil CSS sinif
qaydasını ƏZDİYİ üçün, bir dəfə bağlanıb YENİDƏN açılan sətir grid-dən
flex-ə düşüb bütün sütun düzümünü sındırardı — heç bir xəta vermədən,
yalnız "Bağla"dan sonra həmin TƏK sətirdə. Düzəliş: `style.display = ''`
(boş sətir) — inline stil silinir, CSS-in özü (desktop grid və ya mobil
grid-template-areas, hansı media sorğusu aktivdirsə) yenidən idarə edir.
**Qayda (təkrar, YENİ forması):** bir elementin `display` dəyərini JS-dən
inline `style` ilə "aç/bağla" məntiqi ilə idarə edəndə, CSS-dəki əsl dəyəri
(`flex`/`grid`/`block`) sabit yazma — sonra CSS-in özü dəyişsə (flex→grid
kimi) inline-dakı köhnə sətir unudulub qalar və yalnız O YOLDA (bura: bağla
düyməsi) sakitcə sınar.

⚠️ **Sxrinşot müqayisəsi EDİLMƏDİ — bu mühitdən mümkün deyil.** İstifadəçinin
canlı tətbiqi öz Visual Studio/IIS Express-ində, `localhost:44331`-dədir —
bu cloud sessiyasından ƏLÇATAN DEYİL, ona görə "100% miqyasda ekran görüntüsü
ilə maketlə müqayisə et" tələbi **hərfi mənada yerinə yetirilmədi** — bunu
açıq demək, "yoxladım, uyğundur" kimi yalan iddia etməkdən YAXŞIDIR. Əvəzinə
hər 7 bənd ƏDƏD-ƏDƏD (280px, 158px, 22px, 16px, 13px, 15px, 3 sətir, 1200px,
32px, 28px, 24px) kodda dəqiq tutuşduruldu. İstifadəçi Visual Studio-da
**Rebuild All**, sonra brauzerdə (100% miqyas) `/User/Paylasim`-ı maketlə
yan-yana qoyub yoxlamalıdır: şəklin 280×158px sabit qaldığını (kəsilmədən,
`contain` ilə), üç-sütunlu grid-in düzgün düzüldüyünü, başlıq/mətn/tarix
ölçülərinin uyğun olduğunu, yuxarıdaki köhnə ağ zolağın İNDİ yalnız bu
səhifədə yoxa çıxdığını (digər səhifələrdə DƏYİŞMƏDİYİNİ — məs. `/User/
Dashboard`-a baxıb topbar-ın orada hələ göründüyünü təsdiqləsə yaxşı olar),
mobil enlikdə şəklin yuxarıda, mətnin aşağıda, üfüqi scroll olmadan
göründüyünü.

### Paylaşımlar — "Mərkəzləşdir" Tələbinin Nəticəsi İstəniləndən Fərqli Çıxdı — Sola Yaxın + Qutulu Başlıq (28.10.2026)

Yuxarıdakı (278.png maketinə görə) düzəliş `.py-page-header`/`.py-filters`/
`.py-list-wrap`-i `margin:0 auto` ilə **mərkəzləşdirmişdi** (bənd 1-in hərfi
tələbinə uyğun). İstifadəçi real ekranda nəticəni gördü — geniş monitorda
(1920px) sidebar-dan sonra qalan sahə ~1680px olduğu üçün 1200px-lik qutu
hər iki tərəfdə ~250-280px boş sahə buraxırdı, məzmun "ortada boğulmuş" kimi
görünürdü. İstifadəçi iki ekran-görüntüsü göndərdi: **279.png** (canlı tətbiq)
və **280.webp** (ChatGPT-nin şəkil-redaktə alətində eyni skrinşotun üzərinə
çəkilmiş maket — sual: *"biz bunu sola yaxin, ve biraz basliqli ede
bilmirikki? gpt ya dedim etdi, bizde nese problem var buna gore?"*).

**Bu dəfə hər iki şəkil `Read` aləti ilə birbaşa açılıb piksel-piksel
müqayisə edildi** — əvvəlki dalğadakı "skrinşot müqayisəsi mümkün deyil"
xəbərdarlığı YALNIZ istifadəçinin öz canlı `localhost:44331`-inə CANLI
baxışa aiddir (bu mühitdən əlçatan deyil); istifadəçinin BU SESSİYADA
GÖNDƏRDİYİ fayllar isə `/tmp/.../images/*.png`-də yerli fayllardır və
`Read` onları açıb göstərə bilir — iki fərqli şey qarışdırılmamalıdır.

**280.webp-dən aydın oldu:** GPT-nin "redaktəsi" əslində kod dəyişikliyi
DEYİL — sadəcə ChatGPT-nin şəkil annotasiya alətində eyni skrinşotun üzərinə
əl ilə çəkilmiş vizual maketdir (alətin "İşarətlə/Sil/Yenidən boyutlandır"
paneli görünür). Amma maket aydın iki fərq göstərirdi:
1. **Sola yaxın, mərkəzləşdirilməmiş** — qutu sidebar-dan dərhal sonra
   başlayır, sağda boş sahə qalır (sol-sağ SİMMETRİK boşluq YOX).
2. **Başlıq "qutulu"** — "Paylaşımlar" sadə mətn yox, ağ/bordürlü, kiçik
   ikonlu (paylaşım işarəsi, dairə fonunda) bir kart kimi, sağında "N
   paylaşım" nişanı ilə BİRLİKDƏ.

**Düzəliş — iki hissə, backend-ə YENƏ TOXUNULMADI:**

1. **`margin:0 auto` → `margin:0`** (`.py-page-header`, `.py-filters`,
   `.py-list-wrap`) — mərkəzləşdirmə YOX, qutular `.fn-page`-in öz 24px
   padding-indən DƏRHAL sonra, sol kənara bağlı başlayır. `max-width:1200px`
   saxlanıldı (çox geniş ekranda hədsiz uzanmasın) — fərq YALNIZ artıq
   boşluğun sağda qalmasıdır, hər iki tərəfə bölünmür. Əvvəlki ayrıca 32px
   yan padding-lər (`.py-filters`/`.py-list-wrap`-da) də silindi — `.fn-page`-in
   öz 24px-i artıq kifayət qədər gutter verir, ikiqat padding lazım deyil.

2. **Başlıq qutuya çevrildi** — `.py-page-header` indi `display:flex;
   align-items:center;` ilə ÜÇ hissədən ibarətdir: **ikon** (40×40px, açıq
   bənövşəyi dairə, `bi-share-fill`, `.py-kat-badge`-in rəng tonu ilə EYNİ
   — yeni rəng icad edilmədi), **başlıq+izah** (ortada, flex:1), **"N
   paylaşım" nişanı** (sağda, elə o eyni rəng tonunda pill). Bütün qutu ağ
   fon + nazik sərhəd + yüngül kölgə ilə kart kimi görünür (`.py-list`-in
   özünün sərhəd/kölgə dilini təkrarlayır — yeni vizual dil icad edilmədi).

**Köhnə `.py-list-header` ("Son paylaşımlar · N paylaşım" ayrıca sətri)
TAMAMİLƏ SİLİNDİ** — say artıq yuxarıdaki qutuda bir dəfə göstərilir, iki
yerdə təkrarlanmır (`Index.cshtml`-dəki `else` qolunun başındakı div bloku
çıxarıldı, CSS-dən `.py-list-header`/`.py-list-title`/`.py-list-count`
qaydaları silindi — `grep` ilə hər iki fayldan referansların qalmadığı
təsdiqləndi).

⚠️ **Mobil media sorğusu yenidən yazıldı** — əvvəlki `.py-filters`/
`.py-list-wrap`-ə mobil-xüsusi `padding-left/right:16px` override-i artıq
lazımsız idi (əsas qaydada bu padding-lər onsuz da silinib) — SİLİNDİ, yerinə
yeni qutulu başlığın öz mobil kiçilmə qaydaları (ikon 34px, başlıq 16.5px,
nişan 11px) əlavə olundu.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, hər iki fayldan köhnə sinif adlarının `grep`-lə
tam silindiyi təsdiqləndi). İstifadəçi Visual Studio-da **Rebuild All**,
sonra real brauzerdə yoxlamalıdır: başlığın indi sola yaxın, ikonlu/qutulu
göründüyünü, "N paylaşım" nişanının həmin qutunun sağında olduğunu, siyahının
(filtr pill-ləri + post kartı) da eyni sol kənara bağlı başladığını, geniş
monitorda artıq boşluğun YALNIZ sağda qaldığını (iki tərəfə bölünmədiyini).

## Daxili Maarifləndirmə — Generasiya Anında Sual Növü Seçimi (28.10.2026)

İstifadəçi sualı: *"men sual hazirlatdirarken, secme sansim varki ancaq
variantli olsun ve ya yazili?"* — cavab o anda **XEYR** idi: AI həmişə default
50/50 qarışıq (yarısı çoxseçimli, yarısı açıq) qaytarırdı, yeganə yol
generasiyadan SONRA Admin "Yarat" ekranında hər sualı TƏK-TƏK əl ilə dəyişmək
idi. İstifadəçi: *"yoxdursa onuda hazirla"* — generasiya ANINDA seçim əlavə
olundu.

**Üç qat, "Metod İmzası Dəyişikliyi" qaydasına uyğun eyni anda yeniləndi:**

1. `IMarifAiService.SuallarHazirlaAsync` — yeni, **nullable** `MarifSualTipi?
   yalnizTip = null` parametri (nullable DEFAULT dəyəri qoruyur — köhnə davranış
   dəyişmir, `null` = əvvəlki "qarışıq" istək).
2. `MarifAiService` — `yalnizTip`-ə görə AI-yə göndərilən mətn dəyişir
   ("SUALLARIN HAMISI çoxseçimli olsun" / "...açıq/yazılı olsun" / köhnə
   50/50 mətni). **Qoruyucu süzgəc əlavə edildi** — AI tələbə tam əməl
   etməyib qarışıq tip qaytarsa (bu, YALNIZ tövsiyədir, zəmanət deyil — eyni
   "ağlabatan səhv" prinsipinin əksi, burada etibarsız tərəf AI-nin özüdür),
   `Parse()`-dan sonra `yalnizTip` veriləndə uyğun gəlməyən suallar **atılır**;
   hamısı atılsa (nəticə boş) açıq `Result.Fail` qaytarılır — yarım-qarışıq
   dəst admin-ə göstərilmir.
3. `MarifController.SuallarHazirla` — yeni `string? sualNovu` form sahəsi
   (`"coxsecimli"`/`"aciq"`/hər hansı başqa dəyər və ya boş = qarışıq),
   `MarifSualTipi?`-ə map olunub servisə ötürülür.

**Görüntü — `Yarat.cshtml`:** "Sual sayı" xanasının yanına `<select
id="mfSualNovu">` (Hər ikisi / Yalnız çoxseçimli / Yalnız yazılı, defolt
"Hər ikisi") əlavə olundu, `fd.append('sualNovu', ...)` ilə `SuallarHazirla`
AJAX çağırışına ötürülür.

⚠️ **Yalnız GENERASİYA anında seçimdir — generasiyadan SONRA admin istədiyi
kimi hər sualın tipini hələ də dəyişə/sil/əlavə edə bilər** (bu, əvvəldən
mövcud idi, toxunulmadı). İkisi bir-birini əvəz etmir: generasiya seçimi
"AI-dən nə istə" sualına cavabdır, əl ilə redaktə isə "nəticədə nə qalsın"
sualına.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə, interfeys↔implementasiya↔controller↔
view dörd qatı əl ilə tutuşduruldu). İstifadəçi Visual Studio-da **Rebuild
All** ilə, sonra real ssenari ilə yoxlamalıdır: "Yalnız çoxseçimli" seçib
sual hazırlat → bütün suallar çoxseçimli gəlsin; "Yalnız yazılı" seçib
hazırlat → hamısı açıq/yazılı gəlsin; "Hər ikisi" (default) əvvəlki qarışıq
davranışı qorusun.

## Login Səhifəsi — İkonlar Boş Kvadrat Kimi Görünürdü — `font-src` CSP-dən Unudulmuşdu (28.10.2026, KRİTİK)

İstifadəçi canlı login ekranının skrinşotunu göstərdi — "admin"/"Şifrə" xanalarının
yanındakı ikonlar (zərf, qıfıl) ikon ƏVƏZİNƏ **boş, kvadrat qlif qutusu** kimi
görünürdü (brauzerin "glyph tapılmadı" standart göstərişi — bu, data itkisi/
backend bugu DEYİL, sırf vizual).

**Kök səbəb — `SecurityHeadersMiddleware.cs`-in CSP başlığı, `font-src` direktivi.**
`Login.cshtml` (və bütün layihə) ikonları **Bootstrap Icons** webfontu ilə göstərir
(`<i class="bi bi-envelope">`), CSS-i `_AuthLayout.cshtml`-dən `cdn.jsdelivr.net`-dən
yüklənir. CSS faylının özü (`style-src`-ə `cdn.jsdelivr.net` daxil olduğu üçün)
AÇILIR, amma CSS-in İÇİNDƏKİ `@font-face { src: url(...woff2) }` — faktiki **şrift
fayl(lar)ı** — AYRI bir CSP direktivi, **`font-src`**, ilə idarə olunur. Köhnə
qayda yalnız `'self' https://fonts.gstatic.com` icazə verirdi — `cdn.jsdelivr.net`
YOX idi. Nəticə: stylesheet yükləndi, amma şriftin özü brauzer tərəfindən SƏSSİZCƏ
bloklandı, DevTools-da adi bir CSP xəbərdarlığı qaldı, ekranda isə hər ikon yerinə
boş kvadrat (qlif-yoxdur göstərişi) çıxdı.

**Bu, CLAUDE.md-də ARTIQ sənədləşmiş eyni bugun ÜÇÜNCÜ forması idi** —
"Paylaşımlar" modulunda Quill-in CSS-i üçün `style-src`, mail önizləməsində
mammoth/xlsx üçün `script-src` unudulmuşdu (bax yuxarı müvafiq bölmələr); bu
dəfə İKİ İLDİR layihədə olan `_AuthLayout.cshtml`-in öz Bootstrap Icons-u üçün
`font-src` unudulmuşdu — demək ki, bu, YENİ bir bug deyil, köhnə, elə əvvəldən
mövcud bir boşluq idi, sadəcə istifadəçi indiyə qədər fərq etməmişdi.

**Düzəliş — tək sətir:** `font-src 'self' https://fonts.gstatic.com` →
`font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net`.

**Qayda (bu dəfə ÜÇÜNCÜ dalğada təkrar təsdiqləndi, ÜMUMİLƏŞDİRİLMİŞ forma):**
bir CDN host-dan (`cdn.jsdelivr.net` kimi) HƏR HANSI resurs tipi yüklənirsə
(script, stylesheet, **VƏ YA webfont**), CSP-nin O TİPƏ uyğun ÜÇ direktivini
ayrı-ayrı yoxla — `script-src`, `style-src`, **`font-src`** — bir CSS faylının
AÇILMASI onun İÇİNDƏKİ `@font-face`/`@import` ilə yüklənən İKİNCİ dərəcəli
resursların da açıq olacağına ZƏMANƏT VERMİR. Simptom hər dəfə eynidir:
server 200 qaytarır, resurs "yüklənir" (Network tabında görünür), amma brauzer
onu İSTİFADƏ ETMƏKDƏN imtina edir — funksional xəta yox, sadəcə "eybəcər/boş"
görünüş.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(tək sətirlik mətn dəyişikliyi, mötərizə/dırnaq riski yoxdur). İstifadəçi Visual
Studio-da **Rebuild All** ilə, sonra login səhifəsini brauzerdə yenidən açıb
zərf/qıfıl ikonlarının artıq doğru göründüyünü (boş kvadrat deyil) təsdiqləməlidir
— tam yeniləmə üçün brauzerin keşini təmizləmək (Ctrl+Shift+R) lazım ola bilər,
çünki köhnə CSP başlığı/CSS cavabı keşdə qala bilər.

## Login — "İstifadəçi Adı" Xanasında Zərf İkonu Səhv İdi (28.10.2026)

Yuxarıdakı font-src düzəlişindən sonra ikonlar artıq GÖRÜNÜRDÜ, amma istifadəçi
məntiqi fərqi gördü: *"mektub icosu niye?"* — xana "İstifadəçi adı" (username)
üçündür, amma `bi-envelope` (zərf/mail) ikonu daşıyırdı. Bu, email xanası DEYİL —
sahə `UserName` DTO-dur, placeholder "İstifadəçi adı" yazır, autocomplete
`username-new`-dir. Zərf ikonu sırf vizual kopyalama xətası idi (email-login
formalarının adi ikonu təsadüfən bura düşmüşdü).

**Düzəliş:** `bi-envelope` → `bi-person` (`Login.cshtml`, "İstifadəçi adı" xanası).
Şifrə xanasındakı `bi-lock` TOXUNULMADI — düzgündür.

## AI Çağırışları — "CallApiAsync" HƏR ÜÇ SERVİSDƏ ƏSL XƏTANI UDURDU (28.10.2026, KRİTİK)

İstifadəçi Admin → Daxili Maarifləndirmə → Yeni Sual Dəsti-də PDF yükləyib sual
hazırlatmağa çalışdı (açarı artıq User Secrets-ə əlavə etdikdən sonra) və
**"AI cavabı alınmadı. Zəhmət olmasa yenidən cəhd edin."** aldı — heç bir əlavə
məlumat yox.

**Kök səbəb — `MarifAiService.CallApiAsync` (və iki bacı servisi) Anthropic-in
ƏSL xətasını (HTTP status kodu + cavab mətni: səhv açar, kredit bitib, səhv
model adı, rate limit, timeout) heç yerə YAZMADAN sadəcə `null` qaytarırdı:**

```csharp
var response = await client.PostAsync(ApiUrl, content);
if (!response.IsSuccessStatusCode) return null;   // ⚠️ SƏBƏB İTİR
```

Bu, **"`ex.Message` TƏK BAŞINA HEÇ NƏ DEMİR"** qaydasının (bax yuxarı) HTTP
formasıdır — fərq orada `DbUpdateException.Message`-in generik olması idi,
burada isə HTTP cavabının ÖZÜ heç oxunmur belə. Üstəlik `grep` ilə eyni
nümunənin **ÜÇ** AI servisində (ikisi `SenedAiService`-də) sözbəsöz təkrarlandığı
tapıldı — "Bir Kəmiyyətə Üç Yazıcı" prinsipinin HTTP-xəta-udma forması.

**Düzəliş — `MarifAiService`, `HRMeslehetciService`, `SenedAiService` (4 çağırış
yeri, hamısı eyni naxışla):**
1. `PostAsync`-ı `try/catch`-ə alıb şəbəkə/DNS/TLS/timeout xətasını
   `ILogger.LogError`-a yazır (server `api.anthropic.com`-a ÜMUMİYYƏTLƏ çata
   bilmirsə — firewall/outbound proxy — bu budaq işə düşür);
2. `!IsSuccessStatusCode` olanda **status kodu + Anthropic-in əsl cavab
   mətnini** (`body`) `ILogger.LogWarning`-ə yazır, SONRA `null` qaytarır;
3. JSON parse `try/catch`-ə alınıb — Anthropic gözlənilməz format qaytarsa
   (nadir, amma mümkün) tutulmamış istisna əvəzinə log + `null`.

Hər üç servisə konstruktor vasitəsilə `ILogger<T>` inject edildi (Serilog
artıq `Program.cs`-də qurulub, `Logs\log-yyyyMMdd.txt`-ə yazır — yeni
infrastruktur lazım olmadı).

**Qayda:** `IsSuccessStatusCode == false` olan HƏR xarici HTTP çağırışında
(nəinki Anthropic — gələcək hər inteqrasiya) cavab BODY-sini oxuyub LOGLA,
sadəcə `null`/`false` qaytarma — uğursuzluğun SƏBƏBİ yalnız o body-dədir
(status kodu tək başına "401" deyir, "niyə 401" demir). Bu, `catch`
bloklarında `InnerException`-ı göstərmək qaydasının eyni məntiqinin HTTP
tərəfidir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra YENİDƏN eyni PDF ilə sual hazırlatmağı sınamalı —
xəta YENƏ çıxarsa, bu dəfə `Logs\log-<bugünkü-tarix>.txt`-də "Marif AI
çağırışı uğursuz — HTTP ..." sətrini tapıb bura (və ya birbaşa mənə)
göstərməlidir ki, əsl səbəb (açar səhvdir / kredit yoxdur / şəbəkə
bloklanıb) aydınlaşsın.

### "claude-sonnet-4-7" Model Adı Model Dəyişikliyindən Bəri HEÇ VAXT Uğurlu Olmamışdı (28.10.2026, KRİTİK — DÜZƏLDİLDİ)

Loglama əlavə olunandan sonra belə eyni PDF ilə "AI cavabı alınmadı" davam etdi.
**Fərqləndirici test** — HR Məsləhətçi-yə "salam" yazıldı, **O DƏRHAL cavab
verdi** (eyni serverdə, eyni `Anthropic:ApiKey`, eyni `"Anthropic"` adlı
`HttpClient`). Bu, açarın/kreditin/şəbəkənin düzgün olduğunu dərhal sübut
etdi — problem YALNIZ Marif-in özündə idi.

**Kök səbəb — 27.10.2026-dakı model dəyişikliyi (bax yuxarı "Model `Claude
Opus 4.7` → `Claude Sonnet 4.7`-yə DÜŞÜRÜLDÜ" bölməsi) HEÇ VAXT real
Anthropic API-sinə qarşı yoxlanılmamışdı.** Həmin bölmədəki "iki sual dəsti
≈ 0,06 ABŞ dolları" xərci — model DƏYİŞDİRİLMƏZDƏN ƏVVƏL, köhnə
`"claude-opus-4-7"` ilə yaranmışdı ("İlk versiyada … Opus seçilmişdi …
istifadəçi real sessiyalarda FAKTİKİ XƏRCİ GÖRDÜKDƏN SONRA … aşağı salmağı
istədi" — yəni xərc görülüb, SONRA model dəyişdirilib). Model adı
`"claude-sonnet-4-7"`-yə dəyişdirildikdən sonra Marif HEÇ bir sual dəsti
uğurla hazırlamamışdı — 28.10.2026-dakı bütün "AI cavabı alınmadı" hadisələri
elə BU günə, elə bu dəyişikliyə aiddir. `"claude-sonnet-4-7"` ya mövcud
olmayan, ya da hesabda açıq olmayan bir model ID-dir — Anthropic bunu
`!IsSuccessStatusCode` ilə rədd edir, bizim kod isə (düzəlişdən əvvəl) səbəbi
loglamadığı üçün bu, uzun müddət gizli qaldı.

**Düzəliş:** `MarifAiService.Model` → yenidən `"claude-opus-4-7"` (HR
Məsləhətçi-nin işlətdiyi, SÜBUT OLUNMUŞ model ID). Xərc-azaltma niyyəti
(ucuz model) özü doğru qərar idi, amma **model adının özü yoxlanılmadan**
tətbiq edilmişdi — nəticə əks oldu: xərci azaltmaq əvəzinə modulu tamamilə
sındırdı.

**Qayda:** bir AI servisinin model ID-sini dəyişəndə (xərc azaltmaq, yeni
versiyaya keçmək və s. səbəblə) — "sintaksis düzgündür" (string dəyişib,
kompilyasiya olunur) ilə "bu model ID Anthropic-də HƏQİQƏTƏN mövcuddur və
hesabda açıqdır" EYNİ ŞEY DEYİL. Dəyişiklikdən sonra **ən azı bir real
sınaq** (real API çağırışı, uğurlu cavab) olmadan "artıq ucuzdur" demə —
əks halda model sadəcə ÜMUMIYYƏTLƏ İŞLƏMİR, "ucuz" sualı mənasız qalır.
Fərqləndirici test kimi: EYNİ açarı/HttpClient-i işlədən, artıq sınanmış
başqa bir servisi (bura: HR Məsləhətçi) sınamaq — açar/şəbəkə ilə model-
spesifik xəta arasında saniyələr içində aydın sərhəd çəkir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (tək sətirlik sabit dəyər dəyişikliyi). İstifadəçi Visual
Studio-da **Rebuild All** ilə, sonra YENİDƏN publish edib eyni PDF ilə
sual hazırlatmağı sınamalıdır — bu dəfə uğurlu nəticə gözlənilir
(HR Məsləhətçi-nin elə indi göstərdiyi eyni, işlək model ID-dir).

### İKİNCİ BUG, EYNİ GÜN — `max_tokens` Azlığı JSON-u Yarımçıq Kəsirdi (28.10.2026, KRİTİK — DÜZƏLDİLDİ)

Model adı düzələndən sonra istifadəçi YENİDƏN sınadı (5 sual, "Hər ikisi
qarışıq", `MUT.pdf`) — bu dəfə API-yə çatdı (əvvəlki "AI cavabı alınmadı"
yox oldu), amma **fərqli** xəta çıxdı: *"AI cavabı anlaşılmadı (JSON formatı
gözlənildiyi kimi deyil)"*. İstifadəçinin öz sözü: *"bu indi ferqli xeta
verdi, hemde pulumu yedi"* — çağırış real API-yə getmişdi, pul xərclənmişdi,
amma nəticə istifadə edilə bilmirdi.

**Kök səbəb:** `max_tokens` düsturu `Math.Max(1500, sualSayi * 500)` idi —
5 sual üçün cəmi **2500 token**. Sistem promptu HƏR sualda ("izahda SƏHVİN
YOLUNU de", 4 oxşar-uzunluqlu variant, "ağlabatan səhv" detalları) nisbətən
uzun JSON tələb edir — 2500 token qarışıq (çoxseçimli+açıq) 5 sual üçün
kifayət etmədi, Anthropic cavabı **yarımçıq kəsdi** (`stop_reason=
"max_tokens"`), nəticədə JSON-un sonu (son `}`-lər) yox idi →
`JsonDocument.Parse` `JsonException` atdı → `Parse()` sakitcə `null`
qaytarırdı, heç bir iz qalmırdı.

**Düzəliş — üç hissə:**
1. **Hədd qaldırıldı:** `Math.Min(8000, Math.Max(3000, sualSayi * 900))`.
   Bu, ƏLAVƏ XƏRC DEYİL — Anthropic `max_tokens`-i tavana görə yox, **həqiqətən
   istifadə olunan** tokenə görə hesablayır; tavanı qaldırmaq yalnız kəsilmə
   riskini azaldır, artıq pul yemir.
2. **`Parse()` daha dözümlü oldu** — əvvəl YALNIZ mətnin BAŞI ` ``` `-dirsə
   fence təmizlənirdi. İndi ilk sınaq uğursuz olarsa, mətndəki **ilk `{` ilə
   son `}`** arasını kəsib İKİNCİ dəfə sınayır (AI əlavə mətn/fence-i mətnin
   ortasında/sonunda qoysa belə).
3. **Diaqnostik logging əlavə olundu** — `Parse()` uğursuz olanda xam AI
   cavabı (4000 simvola qədər) `LogWarning`-ə yazılır; `CallApiAsync`-da
   Anthropic-in öz `stop_reason` sahəsi oxunur, `"max_tokens"`-dirsə AÇIQ
   xəbərdarlıq ("cavab YARIMÇIQ KƏSİLİB") logda qalır — növbəti belə hadisədə
   səbəb TƏXMİN yox, `Logs\log-<tarix>.txt`-dən birbaşa FAKT olacaq.

**Qayda:** bir AI servisinin `max_tokens` düsturunu "orta ölçüdə kifayətdir"
deyə təxmin etmə — xüsusən sistem promptu UZUN, DETALLI çıxış tələb edirsə
(bura: hər sualda 4 oxşar-uzunluqlu variant + "səhvin yolunu izah et").
Anthropic-də `max_tokens` sırf TAVANDIR, xərci ARTIRMIR — ona görə həddi
"ehtiyat qədər geniş" seçmək PULSUZ təhlükəsizlikdir, DAR seçmək isə
(bura olduğu kimi) pullu çağırışı səssizcə zay edir. `stop_reason`
sahəsini HƏR AI inteqrasiyasında log üçün oxumaq, təkcə uğursuzluq
budağında yox, bu bugun kök səbəbini DƏRHAL görünən edərdi.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə, `out` parametrli `TryParseSuallar`
helper-in köhnə `Parse()`-un bütün dəyişən adlarını (yerli `netice` →
`lokalNetice`, parametr adı ilə toqquşma) düzgün ayırdığı əl ilə təsdiqləndi).
İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra YENİDƏN eyni PDF
(`MUT.pdf`, 5 sual, "Hər ikisi") ilə sınamalıdır — uğursuz olsa belə, bu
dəfə `Logs\log-<bugünkü-tarix>.txt`-də "Marif AI cavabı..." sətri əsl
səbəbi (yarımçıq kəsilmə, yoxsa başqa format problemi) göstərəcək.

## Kassa — Kurs Təsdiqləndikdə Sabit Siyahıya Bildiriş (28.10.2026)

İstifadəçi tələbi: *"bizde kurslari rehber tesdiqledikden sonra secdiyim
iscilere bildiris gelmeyini isteyirem"* — Kassa günlük valyuta kursu
(`KassaKursBeyannamesi`) `ExchangeTesdiqController.Tesdiq` ilə təsdiqlənəndə
seçilmiş işçilərə bildiriş getsin. İki seçim arasında (sabit admin-siyahı vs
hər göndərişdə əl ilə seçim) istifadəçi **sabit siyahını** seçdi — kassir/
təsdiqedici hər dəfə kimə göndəriləcəyini seçmir.

**Struktur — `KassaTesdiqEdici`-nin DƏQIQ eyni naxışı, AYRI konsept.**
`KassaBildirisAlani` (entity, migration `20261028090000_KassaBildirisAlani.cs`,
cədvəl `KassaBildirisAlanlari`) — bu siyahıdakı işçilər TƏSDİQ ETMİR, yalnız
nəticədən XƏBƏRDAR OLUR. `IKassaBildirisAlaniService`/`KassaBildirisAlaniService`
`KassaTesdiqEdiciService`-in CRUD-unu (aktiv-pəncərə, soft-delete) sözbəsöz
təkrarlayır — `AktivIsciIdleriniGetirAsync()` əlavə metodu bildiriş döngəsi
üçün birbaşa Id siyahısı qaytarır.

**Admin idarəetməsi:** `Admin/KassaBildirisAlani` (controller+views,
`KassaTesdiqEdiciController`/`Index.cshtml`/`Secim.cshtml`-in sözbəsöz
köçürülməsi — eyni `IsciSecimVM` paylaşılan VM-i işlədir). Sidebar-da
"Kurs təsdiqediciləri"-nin dərhal yanında "Bildiriş alanları" sətri
(`_AdminLayout.cshtml`).

**Göndərmə — TƏK yerdə, `KassaKursService.TesdiqEtAsync`-in ÖZÜNDƏ** (controller-də
YOX) — beyannamə hansı controller/UI-dan təsdiqlənsə belə bildiriş avtomatik
gedir, "Bir Kəmiyyətə Üç Yazıcı" riskinin qarşısı alınır. `KassaKursService`
konstruktoruna `IBildirisService` + `IKassaBildirisAlaniService` əlavə edildi
(dairəvi asılılıq yoxdur — `BildirisService` yalnız `IUnitOfWork` +
`IDesktopBildirisService` daşıyır). `TesdiqEtAsync`-in `SaveChanges`-dən
SONRA, roster üzrə **ARDICIL `foreach`** (`Task.WhenAll` YOX — bax "Bildirişlər
— Paralel Yazı" qaydası, paylaşılan `DbContext` eyni anda yazıla bilməz)
`IBildirisService.YaratAsync` çağırılır, `redirectUrl: "/Kassa/Exchange"`.

**Yeni enum — `BildirisNovu.KassaKursTesdiqlendi = 56`** (ardıcıl, ən son
dəyərdən sonra, `MarifTesyinatGonderildi = 55`-dən sonra).

⚠️ **`ImtinaEtAsync`-ə (rədd olunanda) TOXUNULMADI** — istifadəçi yalnız
TƏSDİQ anından danışdı, rədd bildirişi tələb olunmayıb.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, interfeys↔implementasiya↔DI↔çağıran qatları əl ilə
tutuşduruldu, `AppDbContext`-ə yeni `DbSet` + fluent `HasOne/WithMany` əlavə
olundu — `KassaTesdiqEdici`-nin elə yanındaki konfiqurasiya bloku təkrarlandı).
İstifadəçi Visual Studio-da **Rebuild All**, `Update-Database`, sonra real
ssenari ilə yoxlamalıdır: Admin → Kassa → Bildiriş alanları-na bir-iki işçi
əlavə et → Kassa kursu göndər → başqa istifadəçi ilə (təsdiqedici roluna
sahib) təsdiqlə → seçilmiş işçilərin bildiriş qutusunda "Kassa kursu
təsdiqləndi" mesajının göründüyünü, kliklə `/Kassa/Exchange`-ə yönəldiyini
təsdiqləməli.

## Daxili Maarifləndirmə — Naviqasiya Edəndə Pullu AI Nəticəsi İTİRDİ (28.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi ÜÇÜNCÜ dəfə (əvvəlki iki — "model adı" və "max_tokens" — yuxarıda,
"AI Çağırışları" bölməsində düzəldilib) eyni "Suallar hazırla" axınında pul
itkisi bildirdi: *"bu yene pulumu yedi,hazirlanir yazirdi,men basqa sehifeye
girdim qayitdim gordum evvelki veziyyetdedir ve pulum gedib"* — "hazırlanır"
göstərilirdi, admin başqa səhifəyə keçdi, qayıdanda forma İLKİN (boş)
vəziyyətdə idi, AI çağırışı isə artıq (pullu) icra olunmuşdu.

**Kök səbəb — bu, server BUGU deyil, DİZAYN BOŞLUĞU idi.** `MarifController.
SuallarHazirla` AI nəticəsini YALNIZ HTTP cavabında qaytarırdı, heç yerə
YAZMIRDI. `Yarat.cshtml`-in JS-i (`state.suallar`) nəticəni YALNIZ brauzerin
yaddaşında saxlayırdı — admin naviqasiya edəndə (başqa səhifəyə keçəndə) bu
JS konteksti TAMAMILƏ MƏHV OLUR, geri qayıdanda fərqli bir `GET /Admin/Marif/
Yarat` sorğusu təzə, boş bir səhifə render edir. Server tərəfdə `_ai.
SuallarHazirlaAsync(...)` (Anthropic-ə PULLU çağırış) ARTIQ icra olunub,
amma nəticə HEÇ YERƏ yazılmadığı üçün bərpa oluna bilmirdi — "generasiya
+ yadda saxlama" iki ayrı, aralarında HEÇ BİR server-tərəfli körpü olmayan
addım idi, körpü yalnız brauzerin öz yaddaşı idi.

**Həll — nəticə generasiya ANINDA (brauzerə qaytarmadan ƏVVƏL) Qaralama
kimi avtomatik YAZILIR**, "Yadda saxla" isə artıq YENİ sətir yaratmır, HƏMİN
sətri YENİLƏYİR:

1. `MarifSualSetiDetailDto`-ya `HRDaxiliQaydaId`/`HRQanunFaylId`/`MenbeMetn`
   əlavə olundu (`MarifService.DetailGetirAsync`-dən doldurulur) — bərpa
   zamanı mənbəni də tanımaq lazımdır.
2. `MarifController.SuallarHazirla` — yeni `int? existingSetId`, `string?
   basliq` parametrləri. AI uğurla qayıdan kimi nəticə `MarifSualSetiYaratDto`-ya
   yığılır (`Basliq` admin artıq yazıbsa onu, yoxdursa tarix-damğalı placeholder
   — `"(Qaralama — dd.MM.yyyy HH:mm, başlıq əlavə edin)"`) və `existingSetId`
   veriləndə `_marif.SetYenileAsync`, verilməyəndə `_marif.SetYaratAsync` ilə
   DƏRHAL yazılır. Cavaba `setId` (yeni/yenilənən sətrin Id-si) və (nadir
   persist-uğursuzluğu halında) `saxlamaXeberdarligi` əlavə olundu.
3. Yeni `POST /Admin/Marif/SetYenile/{id}` (`[FromBody] MarifSualSetiYaratDto`)
   — `IMarifService.SetYenileAsync`-i çağırır. Bu metod **artıq mövcud idi**
   (27.10.2026-dan, "açıq qalan" kimi qeyd olunmuşdu: *"servis qatında
   YAZILIB, amma heç bir controller action/view-a BAĞLANMAYIB"*) — bu sessiya
   onu birinci dəfə işə saldı.
4. `MarifController.Yarat(int? id)` — `id` veriləndə (və status Qaralamadırsa)
   `_marif.DetailGetirAsync`-dən mövcud qaralamanı yükləyib `ViewBag.MovcudSet`-ə
   qoyur. `Yarat.cshtml` bunu `JsonSerializer.Serialize(..., CamelCase)` ilə
   `window.__mfResume`-ə yazır (bax "Razor → CSS/JS" qaydası — `Json()` helper-i
   ilə EYNİ casing, qarışmasın), JS səhifə açılanda bunu oxuyub başlığı/mənbəni/
   sualları formaya geri doldurur və redaktə blokunu birbaşa göstərir —
   "Suallar hazırla"ya YENİDƏN basmağa EHTİYAC qalmır.
5. `Detail.cshtml` — Qaralama statusunda yeni "Redaktə et" düyməsi (→
   `Yarat?id=`) əlavə olundu; əvvəl YALNIZ "İşçi seç və göndər" + "Sil" var
   idi, yazılmış-amma-bərpa-edilə-bilməyən qaralamaya baxmaq/davam etmək
   yolu YOX idi.
6. `Yarat.cshtml`-ə iki yeni, ayrı banner: yaşıl **"#N qaralaması avtomatik
   yadda saxlanıldı"** (hər uğurlu generasiyadan sonra, reassurance) və sarı
   **xəbərdarlıq** (yalnız avtomatik yazma uğursuz olanda — "bu səhifədən
   çıxmadan özünüz Yadda saxlayın, yoxsa nəticə itəcək"). İkinci bu, artıq
   NADIR halda (AI-nin bir sahəni boş qaytarması kimi validasiya xətası) baş
   verə bilər — o zaman belə admin açıq xəbərdar olunur, səssiz itki YOXDUR.

**Yan təsir (düzəliş) — fayl-mənbəli generasiyada `MenbeMetn` artıq
İTMİR.** Əvvəlki "Yadda saxla" payload-u HƏMİŞƏ `$('mfSerbestMetn').value`-nı
göndərirdi — admin FAYL yükləyibsə (mətn yapışdırmayıbsa) bu xana həmişə
boş idi, yəni generasiya üçün çıxarılan həqiqi mətn son yadda saxlamada
`null`-a düşürdü (kiçik, əlaqəli, əvvəldən mövcud boşluq). İndi `state.
menbeMetn` (serverin `SuallarHazirla` cavabında qaytardığı əsl çıxarılmış
mətn) canlı textarea boşdursa ehtiyat mənbə kimi işlədilir — mətn yapışdırma
halında isə canlı dəyər (admin redaktə etmiş ola bilər) üstün tutulur.

⚠️ **Hər "Suallar hazırla" kliki ARTIQ ayrıca qaralama YARATMIR** —
`existingSetId` (JS-dəki `state.setId`) hər dəfə ötürülür, server HƏMİN
sətri yeniləyir. Yalnız səhifə TƏZƏDƏN açılanda (ilk "Suallar hazırla",
`state.setId` hələ `null`-dursa) yeni sətir yaranır.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — bu düzəlişdən ƏVVƏL itmiş (heç yerə
yazılmamış) generasiyalar bərpa oluna bilməz, artıq xərclənmiş pul geri
qayıtmır. Yalnız BUNDAN SONRAKI "Suallar hazırla" çağırışları qorunur.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə, `node -c` ilə hər iki JS vəziyyəti —
`window.__mfResume = null` və real DTO JSON-u — sınandı, interfeys↔
implementasiya↔controller↔view dörd qatı əl ilə tutuşduruldu). İstifadəçi
Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır:
"Suallar hazırla"ya bas → yaşıl "#N avtomatik yadda saxlanıldı" bildirişini
gör → başqa səhifəyə keç → "Daxili Maarifləndirmə" siyahısında həmin #N
qaralamasının göründüyünü təsdiqlə → "Bax" → "Redaktə et" → sualların/mənbənin
düzgün geri doldurulduğunu, "Yadda saxla"nın İKİNCİ (dublikat) sətir
yaratmadığını (eyni #N-i yenilədiyini) təsdiqləməli.

### Daxili Maarifləndirmə — AI Generasiyası Mövcud Sualları BİLMİR (açıq iş, 28.10.2026)

İstifadəçi sualı: *"hazir qayda var elimde, buna 5 sual verdim hazirladi,
sonra ne vaxtsa yeniden desem 4 sualda hazirla, bu bazadaki suallari
yoxlayirki eyni suali hazirlamasin? yoxsa bece yanasir?"*

**Cavab — HƏLƏ "kor" yanaşmadır, dublikat yoxlaması YOXDUR.**
`MarifController.SuallarHazirla` → `IMarifAiService.SuallarHazirlaAsync`
AI-yə YALNIZ mənbə mətnini (qayda/qanun/fayl) + sual sayını + tipi göndərir.
Bazada həmin mənbə üçün ARTIQ mövcud olan (başqa sual setlərindəki də daxil)
suallar AI-yə heç vaxt ötürülmür — AI hər çağırışda sıfırdan, əvvəlki
generasiyalardan tam xəbərsiz yazır. Nəticədə:
- Eyni qaydaya ikinci dəfə "4 sual hazırla" desən, AI əvvəlki 5-dən bixəbər
  işləyir — üst-üstə düşən/oxşar sual çıxma riski var (xüsusən qayda mətni
  qısa/dar olduqda).
- Əgər bu, HƏMİN qaralamanın üstündə (hələ "Yadda saxla" etmədən yenidən
  "Suallar hazırla"ya basaraq) olursa — köhnə suallar YENİ suallarla
  ƏVƏZ OLUNUR (bax yuxarı "Naviqasiya Edəndə Pullu AI Nəticəsi İTİRDİ"
  bölməsi, `existingSetId`/`SetYenileAsync`), YIĞILMIR. Fərqli vaxtda,
  YENİ bir "Sual Dəsti" açıb eyni mənbəni seçsən, o tamamilə ayrı,
  müstəqil bir generasiyadır.

**Qərar (28.10.2026): bu, pilot mərhələdə HƏLƏ tətbiq edilmir — istifadəçi
"lazım olanda edərik" dedi.** Mümkün həll (müzakirə edilib, YAZILMAYIB):
bazada həmin mənbəyə (`HRDaxiliQaydaId`/`HRQanunFaylId`/eyni `MenbeMetn`)
aid bütün `MarifSual.SualMetni`-ləri (YALNIZ sual mətni — variant/izah
YOX, token qənaəti üçün) `SuallarHazirla`-da bir dəfə sorğulayıb promptun
sonuna "bunlardan fərqli sual yaz" bəndi kimi əlavə etmək. Token artımı
kiçikdir (hər mövcud sual ~40-60 token, adətən 5-20 ədəd arası — cəmi
bir neçə yüz token, output tokeninə (giriş qiymətinin 5 qatı) toxunmur),
amma TƏK TÖVSİYƏDİR — AI-nin tam riayət edəcəyinə zəmanət yoxdur (eyni
"ağlabatan səhv" prinsipinin əksi: LLM-ə etibar, AI-nin özü).

⚠️ **Tətbiq ediləndə yoxlanmalı yerlər (əvvəlcədən qeyd):** `MarifAiService.
SuallarHazirlaAsync` imzasına yeni parametr (`List<string>? movcudSuallar`)
əlavə ediləcəksə, "Metod İmzası Dəyişikliyi" qaydasına uyğun həm interfeys,
həm `MarifController.SuallarHazirla`-dakı çağırış eyni anda yenilənməlidir.
Sorğu mənbəyə görə (`HRDaxiliQaydaId`/`HRQanunFaylId`/`MenbeMetn` üçlüyü)
bağlanmalıdır — fərqli mənbəli suallar qarışdırılmamalıdır.

## Paylaşımlar — Şəkil Mətndən AYRI Sahəyə Çıxarıldı (28.10.2026)

İstifadəçi ilk real paylaşımı (bax yuxarı modul tarixçəsi) hazırlayanda ekran-
görüntüsü göstərdi: Quill-in "image" toolbar düyməsi ilə yüklənən bir poster-
tərzi qrafik (bütün başlıq/tarix mətni şəklin ÖZÜNƏ bişirilmiş) bütün "Məzmun"
sahəsini doldurmuşdu. *"paylasim hazirlamada cetinlik cekireme,olmazki sekil
ucun ayrica elave etme olsun metn ucun ayri"* — şəkil və mətn ayrı-ayrı sahələr
istədi.

**Həll — yeni, MÜSTƏQİL "Üz qabığı şəkli" sahəsi, Quill-in öz inline-şəkil
imkanı SİLİNMƏDİ.** `Melumat.UzQabigiSekilUrl` (`string?`, migration
`20261028100000_PaylasimUzQabigiSekil.cs`, yalnız `AddColumn`) — Admin
formunda (`_UzQabigiSekilSecim.cshtml`, yeni paylaşılan partial, `_Kateqoriya
Secim`/`_HedefSecim`-in yanında) ayrıca "Şəkil seç" düyməsi, önizləmə və "Sil"
düyməsi olan bir sahədir, Quill-dən TAM MÜSTƏQİLDİR. Eyni, artıq mövcud
`SekilYukle` AJAX endpoint-i (`PaylasimController`, `/dms/paylasim-sekiller/`-ə
yazır) TƏKRAR işlədilir — yeni upload-yolu İCAD EDİLMƏDİ, mövcud infrastruktur
ikinci dəfə çağırılır.

**Göstərmə prioriteti — server-rendered cover ÜSTÜN, JS-fallback KÖHNƏ
postlar üçün qalır:**
- İşçi lentinin sıx sətir görünüşündə (`.py-row-thumb`) — `UzQabigiSekilUrl`
  varsa serverdən birbaşa `<img>` render olunur, `data-cover="true"` ilə
  işarələnir. JS-in köhnə "mətnin içindən ilk şəkli çıxar" məntiqi (bax
  "Böyük kart Lentindən Sıx SİYAHI Dizaynına" bölməsi) `data-cover="true"`
  olan sətirlərdə ARTIQ ÇALIŞMIR (overwrite etmir) — AMMA excerpt (qısa mətn)
  hesablaması eyni iterasiyada davam edir, JS-dən erkən `return` YAZILMADI
  (yazılsaydı excerpt də skip olardı — bax "Pagination Əvvəlki" bugundakı
  eyni "iki müstəqil şərt" dərsi, fərqli formada).
- `UzQabigiSekilUrl` YOXDURSA (köhnə postlar, elə ilk "İlk bankomat" paylaşımı
  daxil) — əvvəlki JS-fallback (body-dəki ilk `<img>`-i çıxar) TOXUNULMADI,
  köhnə postlar əvvəlki kimi işləməyə davam edir.
- Tam-görüntü açılanda (`.py-row-full`) üz qabığı şəkli `.py-card-head`-dən
  SONRA, `.py-title`-dan ƏVVƏL banner kimi göstərilir (`.py-cover-wrap`/
  `.py-cover-img`, `max-height:360px`, `object-fit:contain` — `.py-body img`
  ilə EYNİ "mətn/rəqəm kəsilməsin" qaydası, çünki bu şəkillər də adətən
  mətn daşıyır).
- Admin idarəetmə siyahısında (`Index.cshtml`) da kiçik bir kart-üstü
  thumbnail əlavə olundu (sırf görüntü, filtrə təsiri yoxdur).

⚠️ **GERİYƏ TƏTBİQ OLUNMUR, DAVRANIŞ POZULMUR** — yeni sütun `NULL`-dan
başlayır, mövcud (artıq Quill-dən inline şəkilli) postlar `UzQabigiSekilUrl`
olmadan əvvəlki kimi göstərilməyə davam edir. İstifadəçinin artıq göndərdiyi
"İlk bankomat" postu YENİDƏN REDAKTƏ EDİLMƏLİDİR ki, yeni "Üz qabığı şəkli"
sahəsindən faydalansın — avtomatik köçürülmür (admin istəsə, mövcud inline
şəkli saxlayıb YA DA üz qabığına köçürüb mətndən silə bilər, hər ikisi əl
işidir).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə hər dəyişən fayl üzrə, mojibake skanı — hər ikisi
təmiz, DTO↔Entity↔Servis↔Controller↔View beş qatı əl ilə tutuşduruldu).
İstifadəçi Visual Studio-da **Rebuild All**, `Update-Database`, sonra real
ssenari ilə yoxlamalıdır: yeni paylaşım yaradanda "Üz qabığı şəkli" sahəsinə
şəkil yükləyib ayrıca mətn yazsın → işçi lentində düzgün miniatür+banner
göründüyünü, köhnə ("İlk bankomat") postun hələ əvvəlki kimi işlədiyini,
"Şəkli sil" düyməsinin sahəni təmizlədiyini təsdiqləməli.

## Mühasibat — "Hesabatlar" Balans İcmalının Tab-Dropdown-undan Ayrıca Sidebar Səhifəsinə Köçürüldü (29.10.2026)

İstifadəçi tələbi: *"men isteyiremki muhasibatliqda tam ayri hesabatlar sehifesi
edek, cunki hesabatlar artmaga baslayir. indiki yerini beyenmirem."* Araşdırma
göstərdi ki, ayrıca bir "Hesabatlar" kart-grid səhifəsi (`Areas/Muhasibat/
Controllers/HesabatController.cs` + `Views/Hesabat/Index.cshtml`) **artıq
mövcud idi** — problem konseptual yox, **yer** problemi idi: bu səhifəyə YEGANƏ
giriş yolu Balans İcmalının üfüqi tab-zolağındakı ("İcmal / Kreditlər / Depozit
& Müştəri / Mənfəət-Zərər / Likvidlik & Bazar / **Hesabatlar**") dropdown idi —
sol sidebar-da heç bir bəndi yox idi.

**"Hesabatlar" adı TƏTBİQDƏ ARTIQ İKİ YERDƏ işlənirdi** — üst naviqasiyadakı
top-level "Hesabatlar" (ekranda ən görünən) əslində `User/HesabatController`-dir,
**Məzuniyyət/İcazə** hesabatıdır, maliyyə ilə heç bir əlaqəsi yoxdur; əlavə
olaraq **HR-in öz "HR Hesabatları"** sidebar bəndi də var
(`Areas/HR/Controllers/HesabatController.cs`). Üçüncü bir "Hesabatlar" əlavə
etsək istifadəçi "hansı Hesabatlar?" sualı verəcəkdi — ona görə Mühasibat
sidebar bəndinin adı **"Maliyyə Hesabatları"** seçildi (URL/controller eyni
qalıb — `Muhasibat/Hesabat/Index`, yalnız giriş yolu dəyişib, "İşlək Funksiyanı
Silmək" qaydasına uyğun heç nə silinmədi).

### Qərarlar (istifadəçi ilə 4 sualda birbaşa razılaşıb)

1. **Ad: "Maliyyə Hesabatları"** (yuxarıdakı qarışıqlığa görə).
2. **Balans İcmalının canlı tabları (Balans icmalı, Mənfəət/Zərər, Kredit
   portfeli, Depozit & Müştəri, Likvidlik) yeni səhifədə QISAYOL KARTI kimi
   görünür** — heç bir yeni məntiq/səhifə YARADILMADI, kartlar sadəcə mövcud
   `Dashboard/{Index,Menfeet,Kredit,Depozit,Likvidlik}` action-larına birbaşa
   keçiddir (bu action-lar artıq ayrıca, addresslənə bilən URL-lərdir —
   `_MhTabs.cshtml`-dən təsdiqləndi, query-param əsaslı tab DEYİL).
3. **DSMF Vahid Hesabat Forması (gələcək iş) bu yeni səhifədə yerləşəcək** —
   nəticə etibarı ilə mühasibatlıq/vergi sənədidir, hesablama məntiqi HR-in
   `MaasHesablamaService`-ində qalsa da, hesabatın ÖZÜ bura aiddir. ⚠️ **Bu
   sessiyada DSMF hesabatının ÖZÜ (6 Excel generasiyası) HƏLƏ QURULMADI** —
   yalnız gələcəkdə buraya əlavə olunacaq kataloq yeri hazırlandı.
4. **"Seçilmişlər" (★) funksiyası İNDİ QURULDU** (istifadəçi "sonraya saxla"
   tövsiyəsini rədd etdi, "İndi qur" seçdi).

### Struktur

**Kataloq KODDADIR, DB-DƏ DEYİL** — `HesabatController.Katalog` (statik
`List<HesabatKartDto>`), Kassa-nın `Valyutalar` massivi/Qara Jetonun statik
siyahı prinsipi ilə EYNİ: Kod, Ad, Təsvir, Kateqoriya, İkon, Area/Controller/
Action. Yeni hesabat əlavə etmək lazımdırsa, YALNIZ bu siyahıya əlavə et.
8 başlanğıc kart: Balans İcmalı/Mənfəət-Zərər/Depozit (Maliyyə), Kredit
Portfeli (Kreditlər), Likvidlik/IFRS 9 ECL/AMB MHBS 9 (Risk), Xərc Hesabatı
(Xərclər, `HR/XercHesabat`-a keçid).

⚠️ **"AMB MHBS 9" kartı "IFRS 9 ECL" kartı İLƏ EYNİ URL-ə gedir**
(`Hesabat/Ifrs9`) — bu, kopyalama SƏHVİ DEYİL: AMB A1 Excel export-u (`AmbA1
Excel`) elə IFRS9 səhifəsinin daxilindəki bir düymədir, ayrıca səhifəsi
YOXDUR (köhnə kod yoxlanılıb təsdiqləndi).

**★ Seçilmişlər — `HesabatSecilmis` entity** (`FinNex.Domain/Entities/
Muhasibat/`), migration `20261029090000_HesabatSecilmis.cs` (`InsertData`
YOXDUR). `MailIzlemeUnvani` ilə EYNİ "Yumşaq Silinmiş Sətir + Unikal İndeks"
qaydası: `(AppUserId, HesabatKod)` üzərində unikal indeks `Silinib`-i
FİLTRLƏMİR, ona görə `HesabatSecilmisService.SecVeyaCixarAsync`
`QueryAll()`-la (filtrsiz) axtarır — "aktiv → yumşaq silinmiş (dirilt) → yeni
yarat" sırası ilə (CLAUDE.md-dəki sənədləşmiş tələyə düşmədən).

**Star klikində AJAX → `SeciliDeyis` POST → uğurlu olanda `location.reload()`**
— Mail modulunun "Hamısını oxundu et" düyməsindəki sadəlik prinsipi ("AJAX-la
yenidən render etmək əvəzinə sadə reload") burada da seçildi, çünki "Seçilmişlər"
siyahısının client-side yenidən qurulması (kart əlavə/çıxarma + say yeniləmə)
iki paralel render yolunu (siyahı + sayğac) sinxron saxlamağı tələb edərdi —
reload ən sadə, ən təhlükəsiz yoldur.

### Naviqasiya

`_UserLayout.cshtml`-in "Maliyyə" sidebar qrupuna (və `panelYalniz`
— Rəhbər/risk-icazəli — qrupuna) "Maliyyə & Risk Paneli"nin DƏRHAL ardınca
yeni bir sətir əlavə olundu. **Bu, eyni zamanda GİZLİ BİR BUGU da üzə
çıxardı və düzəltdi:** "Maliyyə & Risk Paneli" linkinin `active` şərti əvvəl
yalnız `currentArea == "Muhasibat"` idi — yəni yeni "Maliyyə Hesabatları"
(eyni Area-da, fərqli Controller) səhifəsindəykən İKİ sətir eyni anda
"aktiv" görünərdi. Hər iki linkin şərtinə indi `&& currentController ==
"Dashboard"` / `"Hesabat"` əlavə edildi.

`_MhTabs.cshtml`-dəki "Hesabatlar" dropdown-unun "Bütün hesabatlar" sətri
"Maliyyə Hesabatları"na çevrildi (eyni URL, sadəcə ad uyğunlaşdırıldı) —
iki giriş nöqtəsi (sidebar + tab-dropdown) eyni səhifəyə aparır, bu, qəsdən
saxlanıldı (Mail/Paylaşımlar modullarında da "eyni hədəfə iki giriş"
zərərsiz sayılıb).

⚠️ **Top-nav-dakı "Departamentlər" dropdown-undakı tək "Mühasibatlıq" sətrinə
TOXUNULMADI** — CLAUDE.md-nin "TƏK giriş sırası" qaydasına uyğun (Kassa/Kurs
təsdiqi presedenti), bir modula bir dropdown sətri kifayətdir, Dashboard-dan
içəri girəndən sonra tab-bar/sidebar naviqasiya edir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə bütün dəyişən fayllarda, JS Node ilə
sintaksis yoxlaması, interfeys↔implementasiya↔DI↔controller↔view beş qatı
əl ilə tutuşduruldu). İstifadəçi Visual Studio-da **Rebuild All**,
`Update-Database`, sonra real ssenari ilə yoxlamalıdır: sidebar-da "Maliyyə
Hesabatları" bəndinin göründüyünü, kartların/sətirlərin düzgün keçid
verdiyini, axtarış+kateqoriya filtrinin işlədiyini, ★ klikinin seçimi
yadda saxlayıb "Seçilmişlər" bölməsində göstərdiyini.

### DÜZƏLİŞ — Canlı Dashboard Tabları "Hesabatlar" DEYİL, Bura QARIŞMIR (29.10.2026, EYNİ GÜN)

Yuxarıdakı 2-ci qərar ("Balans İcmalının canlı tabları QISAYOL KARTI kimi
görünsün") canlı nəticəni görəndən DƏRHAL SONRA istifadəçi tərəfindən
**tərsinə çevrildi**. İstifadəçi: *"icmal, kreditler olan hisseni tekrar niye
gosterdin ki? burda yalniz hesabatlarin hazirlanmasi olacq, o biriler ise
sorgular idi, qatisdirma."* Fərq konseptualdır: Dashboard-un tabları (İcmal,
Kreditlər, Depozit & Müştəri, Mənfəət/Zərər, Likvidlik & Bazar) **canlı
SORĞU/KPI ekranlarıdır** — "Hesabatlar" səhifəsi isə YALNIZ **hazırlanan,
export olunan** sənədlər üçün nəzərdə tutulub (IFRS 9 ECL, AMB MHBS 9, Xərc
Hesabatı, gələcəkdə DSMF). AskUserQuestion-da verilən cavab canlı ekranı
görmədən alınmışdı — "qısayol kartı" ideyası kağız üzərində məntiqli
görünsə də, real nəticədə iki fərqli məqsədi (sorğu vs hesabat) eyni
səhifədə qarışdırırdı.

**Düzəliş — iki yer:**
1. `Views/Hesabat/Index.cshtml` — `<partial name="…/_MhTabs.cshtml" />`
   (yuxarıdakı tab-zolağı) səhifədən TAMAMİLƏ ÇIXARILDI. `_MhTabs.cshtml`-in
   özü TOXUNULMADI — Dashboard-un digər səhifələrində (Balans İcmalı, Kredit
   Portfeli və s.) hələ də göstərilir, çünki O ailənin naviqasiyasıdır;
   problem YALNIZ bu tab-zolağının YENİ, MÜSTƏQİL "Hesabatlar" səhifəsində
   TƏKRARLANMASI idi.
2. `HesabatController.Katalog`-dan **5 qısayol kartı SİLİNDİ**: `balans-icmali`,
   `menfeet-zerer`, `depozit-musteri`, `kredit-portfeli`, `likvidlik`. Kataloq
   indi YALNIZ həqiqi hesabatları daşıyır: `ifrs9-ecl`, `amb-mhbs9`,
   `xerc-hesabati` (+ gələcəkdə DSMF).

**BMI-nin köhnə menyu strukturu gələcək istiqaməti göstərdi.** İstifadəçi
BMI-nin "Maliyyə DP → Hesabatlar" menyusunun ekran-görüntüsünü göstərdi —
**Günlük / Aylıq / Rüblük** (Daily/Monthly/Quarterly) alt-kateqoriyaları,
hər birinin öz hesabat siyahısı (Daily Report, Daily comments, Tələbli və
müddətli depozitlər və s.): *"bizde o hesabatlari yavaş yavaş bura
geitireceyik"* — yəni bu kataloq GƏLƏCƏKDƏ BMI-dən köçürülən hesabatlarla
dolacaq. Bu səbəbdən `Index.cshtml`-dəki kateqoriya pill sırası (`kateqoriya
Sirasi`) əvvəlcədən BMI-nin öz sxeminə uyğunlaşdırıldı: `{ "Günlük", "Aylıq",
"Rüblük", "Risk", "Xərclər" }` (əvvəlki `{ "Maliyyə", "Kreditlər", "Risk",
"Xərclər" }` əvəzinə) — filtr yalnız MODEL-də REAL mövcud olan kateqoriyaları
göstərdiyi üçün (`Where(k => Model.Any(x => x.Kateqoriya == k))`) bu, indi
boş pill göstərmir, sadəcə gələcək əlavələr üçün hazır sıra qoyur. Uyğun CSS
badge sinifləri də əlavə olundu: `.hs-kat--günlük`, `.hs-kat--aylıq`,
`.hs-kat--rüblük` (köhnə `.hs-kat--maliyyə`/`.hs-kat--kreditlər` sinifləri
hazırda heç bir kartda işlənmir, amma zərərsiz olduğu üçün silinmədi —
"Maliyyə" kateqoriyalı qeyri-BMI mənşəli bir hesabat gələcəkdə əlavə olunsa
hazır qalır).

⚠️ **DSMF Vahid Hesabat Forması HƏLƏ DƏ QURULMADI** — yuxarıdakı 3-cü qərar
(yeri bu səhifədir) dəyişmədi, yalnız BMI-nin Günlük/Aylıq/Rüblük bölgüsü
onun (və gələcək BMI-mənşəli digər hesabatların) hansı kateqoriyaya
düşəcəyini aydınlaşdırdı — DSMF rüblük bir hesabatdır, deməli "Rüblük"
kateqoriyasına düşəcək.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real brauzerdə yoxlamalıdır: "Maliyyə
Hesabatları" səhifəsində artıq İcmal/Kreditlər/Depozit/Mənfəət-Zərər/
Likvidlik tab-zolağının görünmədiyini, kataloqda yalnız IFRS 9 ECL/AMB
MHBS 9/Xərc Hesabatının qaldığını, Balans İcmalı (`/Muhasibat`) kimi
digər Dashboard səhifələrində isə tab-zolağının əvvəlki kimi işlək
qaldığını.

### DÜZƏLİŞ #2 — Kateqoriyalar HƏMİŞƏ Göstərilir + IFRS9-un Özündə də Tab-Zolağı Qalmışdı (29.10.2026)

İstifadəçi iki əlavə tələb verdi:

1. *"indi gunluk,ayliq ve rubluk secimide goster,cunki hesabatlar bunun
   icinde olacaq"* — Günlük/Aylıq/Rüblük pill-ləri əvvəl yalnız kartı olan
   kateqoriyalarda görünürdü (`Where(k => Model.Any(...))`), yəni BMI-dən
   hesabat köçürülənə qədər **heç biri görünmürdü** — struktur özü gizli
   qalırdı. Düzəliş: `Index.cshtml`-də filtr çıxarıldı, bütün 5 kateqoriya
   (Günlük/Aylıq/Rüblük/Risk/Xərclər) **HƏMİŞƏ**, hər birinin yanında real
   say nişanı ilə göstərilir; boş kateqoriya kəsik haşiyə ilə fərqlənir,
   seçiləndə siyahı öz "Bu kateqoriyada hələ hesabat yoxdur" boş-nəticə
   mesajını göstərir (`#hsEmpty`) — boş `.hs-list` deyil.

2. *"ifrs 9 burda acilacaq? burda istemirem axi"* (ekran-görüntüsü, IFRS9
   səhifəsi açıq, üstündə İcmal/Kreditlər/Depozit & Müştəri/Mənfəət-Zərər/
   Likvidlik & Bazar/Hesabatlar tab-zolağı görünür) — Birinci düzəlişdə
   (yuxarı) `_MhTabs` YALNIZ `Hesabat/Index.cshtml`-dən çıxarılmışdı,
   `Hesabat/Ifrs9.cshtml` VƏ `Hesabat/Ifrs9Parametr.cshtml` hələ eyni
   partial-ı çağırırdı (hər ikisi `grep` ilə tapıldı) — yəni IFRS9 "hesabat"
   kimi təqdim edilsə də, görüntüdə hələ canlı Dashboard ailəsinin bir
   hissəsi kimi görünürdü, bu da elə əvvəlki düzəlişin arxasındakı "sorğu ≠
   hesabat" ayrımını pozurdu. Düzəliş: `_MhTabs` partial-ı hər iki fayldan
   da çıxarıldı — naviqasiya itmir, çünki hər ikisinin öz hero-sundakı
   düymələr (ECL detalı/İş kağızları/AMB/Metodologiya/Parametrlər/"IFRS 9
   səhifəsinə qayıt") onsuz da `_MhTabs`-dan müstəqildir.

**Kataloqdan silinən "Xərc Hesabatı" kartı** — istifadəçi: *"xerc hesabati
onu gizlet,heleki isledilmir"* (hələ real istifadədə deyil). `HesabatController.
Katalog`-dan `xerc-hesabati` sətri silindi, yerinə necə geri əlavə
ediləcəyini göstərən şərh qoyuldu — modul (`HR/XercHesabat`) özü SİLİNMƏDİ,
yalnız kataloqdan (göstərişdən) çıxarıldı ("İşlək Funksiyanı Silmək" qaydası —
burada "funksiya" artıq əvvəldən işlək DEYİLDİ, ona görə silinməsi "açıq
icazə" tələb etmədi, sadəcə yeni, hələ istifadəyə verilməmiş bir kataloq
qeydinin geri götürülməsidir).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real brauzerdə yoxlamalıdır: "Maliyyə
Hesabatları" səhifəsində Günlük/Aylıq/Rüblük/Risk/Xərclər pill-lərinin
(sayları ilə) həmişə göründüyünü, boş kateqoriya seçiləndə "hələ hesabat
yoxdur" mesajının çıxdığını, "Xərc Hesabatı" kartının artıq görünmədiyini,
IFRS 9 (həm əsas, həm "Floor parametrləri") səhifələrində artıq İcmal/
Kreditlər/Depozit/Mənfəət-Zərər/Likvidlik tab-zolağının göstərilmədiyini —
AMMA hər iki səhifənin öz hero düymələrinin (ECL detalı, AMB, Parametrlər,
"IFRS 9 səhifəsinə qayıt") əvvəlki kimi işlədiyini.

### DÜZƏLİŞ #3 — "IFRS 9 — ECL" vs "AMB MHBS 9" KARTLARI EYNİ SƏHİFƏYƏ AÇILIRDI (29.10.2026)

İstifadəçi "Maliyyə Hesabatları" səhifəsindəki iki kartı (IFRS 9 — ECL, AMB
MHBS 9) sınayıb hər ikisinin EYNİ nəticəyə — eyni başlıqlı ("IFRS 9 —
Gözlənilən Kredit İtkiləri (ECL)") səhifəyə — açıldığını gördü:
*"ikiside eyni seyi acir? biri artiqdir?"*, sonra dəqiqləşdirdi:
*"1 ci sekildeki iki hasabatdakini acilanidir,ikiside eyni addadir axi,
gozlenilen kredit itkileri"*.

Bu, **yeni bir bug deyildi** — əvvəlki sessiyada (bax yuxarı, "Mühasibat —
'Hesabatlar'..." bölməsinin əsas şərhi) bu iki kartın eyni URL-ə getməsi
AÇIQ VƏ QƏSDƏN belə qurulmuşdu: "AMB A1 Excel ixracı elə IFRS9 səhifəsinin
daxilindəki bir düymədir, ayrıca səhifəsi YOXDUR". Həmin qərar texniki
cəhətdən doğru idi (kopyalama səhvi deyildi), amma **istifadəçi təcrübəsi
baxımından səhv idi** — iki ayrı, eyni görünüşlü kart təqdim edib hər
ikisinin arxasında FƏRQSİZ bir nəticə göstərmək qarışıqlıq yaradır,
özəlliklə hər ikisi "eyni adda" (eyni başlıqda) açılanda.

**Həll — AskUserQuestion ilə istifadəçiyə iki seçim təklif edildi, "Kataloqdan
AMB kartını sil" seçildi.** `HesabatController.Katalog`-dan `amb-mhbs9` sətri
TAMAMİLƏ ÇIXARILDI — indi kataloqda YALNIZ "IFRS 9 — ECL" kartı var, onun
təsvirinə *"· AMB MHBS 9 A1 ixracı da buradadır"* qeydi əlavə olundu ki,
AMB funksiyasının itmədiyi, sadəcə təkrarlanan girişin silindiyi aydın olsun.
`AmbA1Excel` action-unun ÖZÜ (və `Ifrs9.cshtml`-dəki "AMB MHBS 9 · A1"
düyməsi) **TOXUNULMADI** — AMB ixracı funksional olaraq əlçatandır, yalnız
ayrıca, dubl bir kataloq kartı kimi TƏKRAR göstərilmir.

**Qayda:** eyni destinasiyaya (`Area`+`Controller`+`Action`) gedən iki ayrı
kataloq/menyu kartı yaratmaq — hətta "biri əsas səhifə, biri onun daxilindəki
bir funksiyanı vurğulamaq üçündür" kimi əsaslandırılsa belə — istifadəçiyə
"bunlar iki fərqli şeydir" siqnalı verir. Əgər hədəf səhifə HƏR İKİ girişdə
EYNİ başlıq/görüntü ilə açılırsa (fərqləndirici parametr, scroll-to-section,
fərqli alt-başlıq YOXDURSA), bu iki giriş BİR kataloq kartına birləşdirilməli,
ikinci funksiya isə hədəf səhifənin öz daxilində (düymə/bölmə) qalmalıdır —
"Bir Kəmiyyətə Üç Yazıcı" qaydasının əksi deyil, "Bir Destinasiyaya İKİ Qapı
Açma" formasıdır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, `amb-mhbs9` kodunun başqa heç bir `.cshtml`/`.cs`
faylında kataloq-kodu kimi istinad edilmədiyi `grep` ilə təsdiqləndi — yalnız
DMS fayl-yolu qovluq adı kimi qalan 3 istinad var, kataloqla əlaqəsi yoxdur).
İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real brauzerdə
yoxlamalıdır: "Maliyyə Hesabatları" səhifəsində "Risk" kateqoriyasında indi
YALNIZ BİR kartın ("IFRS 9 — ECL") olduğunu, kartı açanda hələ də "AMB MHBS 9 ·
A1" ixrac düyməsinin səhifənin öz daxilində göründüyünü (funksiya itməyib)
təsdiqləməli.

## DSMF Vahid Hesabat Forması — Pilot: Əlavə №1 Hissə 3 (29.10.2026)

İstifadəçi tapşırığı: *"indi atdigim excellerdeki melumatlari hazirlamaq
lazimdir. teklifim il ve rub secilsin. hemin rub uzre 3 ayin melumatlari
istenilir."* DSMF (Dövlət Sosial Müdafiə Fondu) rüblük Vahid Hesabat Forması
**6 rəsmi Excel əlavəsindən** ibarətdir — biri maaş tərkibinin mürəkkəb
kompozit düsturu (Əlavə №1 Hissə 1), dördü vergi-güzəşti cədvəlləri
(Əlavə №4 Bölmə 2-5), biri isə **işçinin işləmədiyi iş günləri** (Əlavə №1
Hissə 3). Dövlətə təqdim olunan rəsmi sənəd olduğu üçün (vergi/sosial
sığorta hüquqi nəticəli) `AskUserQuestion` ilə əhatə dəqiqləşdirildi —
istifadəçi **"Əlavə №1 Hissə 3-dən başla"** seçdi, çünki bu, HR modullarından
(Məzuniyyət/Xəstəlik/işə qəbul-çıxma tarixi) **mexaniki olaraq** dəqiq
çıxarıla bilər, Hissə 1/Bölmə 2-5 isə hələ ayrıca, gələcək mərhələlərdir.

Eyni müzakirə zamanı istifadəçi *"18 yas meselesi lazim deyil,onu nezere
alma"* dedi — bu, Hissə 1-in (hələ QURULMAYIB) mürəkkəb düstur qeydlərindəki
uşaq-əlilliyi yaş-18 həddi ilə bağlıdır, BU sessiyada tikilən Hissə 3-ə aid
DEYİL. **Qeyd gələcək üçün saxlanılır** — Hissə 1 işinə başlananda bu
göstərişə əməl edilməlidir (yaş-18 həddi MODELLƏŞDİRİLMƏMƏLİDİR).

### Şablonun özü — "combo" vərəqində 12 rəsmi səbəb kodu

`UNIFIED_APPENDIX_1_UNEMPLOYED_DAYS.xlsx` iki vərəqdən ibarətdir: **"Əlavə
№1 - Hissə 3"** (data) və **"combo"** (12 səbəbin rəsmi mətni, dropdown
mənbəyi). Sütunlar (1-based): A=Sıra№, B=Ad-soyad-ata, C=FİN, D/E/F=1-2-3-cü
ayın gün sayı, G=Səbəb mətni (dropdown). Data **7-ci sətirdən** başlayır
(0-əsaslı NPOI-də sətir 6) — şablonun özündə YALNIZ **iki nümunə sətir**
(Sıra№=1,2, Səbəb="1-İşçinin ödənişli əsas məzuniyyətdən...") var, qalan
~80 sətir boşdur.

**12 səbəbdən YALNIZ 6-sı** mövcud HR modullarına birbaşa uyğundur:

| Kod | DSMF mətni | Mənbə |
|---|---|---|
| 1 | Ödənişli əsas məzuniyyət | `Mezuniyyet.Nov == Illik`, real iş günü |
| 3 | Ödənişsiz məzuniyyət (Ə.M. 129) | `IMaasHesablamaService.OzHesabinaIsGunuSayAsync` (mövcud, sınanmış) |
| 5 | Xəstələnmə | `IMaasHesablamaService.XestelikHaqiqiIsGunuSayAsync` (mövcud, sınanmış) |
| 6 | Ay ərzində əmək müqaviləsinə xitam | `Isci.IsdenAyrilmaTarixi` (ayın son günü deyil) |
| 8 | Ayın ortasında işə qəbul | `Isci.IsheQebulTarixi` (ayın 1-ci günü deyil) |
| 9 | Həmin ay üzrə müqavilə ümumiyyətlə bağlanmayıb | tarix-kəsişməsinin olmaması |
| 11 | Dövlət/ictimai vəzifə | `Mezuniyyet.Nov == DovletVezifelerininIcrasi`, real iş günü |

**Qalan 5 kod (2,4,7,10,12 — sosial məzuniyyət, təhsil məzuniyyəti, növbəli
iş, işəmuzd, məhkəmə hökmü) sistemdə HEÇ BİR modula uyğun gəlmir** — belə
status/entity YOXDUR. Bu, **səssizcə buraxılmır**: `DsmfHissesi3Dto.
AvtomatikAskarlanmayanSebebler` siyahısı önizləmə ekranında açıq, sarı
xəbərdarlıq kimi göstərilir ("bu 5 səbəb avtomatik aşkarlanmır, belə hal
varsa mühasib bu sətri Excel-ə ÖZÜ əl ilə əlavə etsin").

### Niyə `MaasHesablamaService`-in özünə TOXUNULMADI

Payroll-un "real iş günü" hesablayıcıları (`OzHesabinaIsGunuSayAsync`,
`HaqiqiIsGunuSayHelperAsync`, `XestelikHaqiqiIsGunuSayAsync`) **private**-dir
və maaş hesablamasının əsl (yazılan) mənbəyidir — CLAUDE.md-nin "Maliyyəyə
toxunan hər dəyişiklik xüsusi diqqət tələb edir" qaydasına görə bu metodlara
toxunmaq/onlara yeni asılılıq əlavə etmək **risk** daşıyır, baxmayaraq ki
DSMF yalnız OXUYUR, yazmır. **Həll:** `DsmfHesabatService` öz, tam
MÜSTƏQİL, OXU-YALNIZ gün-təsnifatı helper-ini (`HaqiqiIsGunuSayAsync`,
`BayramGunu`-ya əsaslanan) yazır — Kod 1/6/8/9/11 bu nüsxəni işlədir. YALNIZ
Kod 3 və 5-də, artıq **PUBLIC** və sınanmış iki metod (`OzHesabinaIsGunuSayAsync`,
`XestelikHaqiqiIsGunuSayAsync`) birbaşa çağırılır — bunlar artıq payroll-un
öz hesablamasında işlək, təkrar yazmaq "Bir Kəmiyyətə Üç Yazıcı" riski
yaradardı.

⚠️ **Kod 1 üçün `MezuniyyetGunleriniSayGenisAsync` İŞLƏDİLMƏDİ** — o metod
Illik+Ezamiyyəti BİRLİKDƏ qaytarır, Ezamiyyət isə DSMF-in 12 səbəbindən biri
DEYİL (işçi ezamiyyətdə İŞLƏYİR, "işləmədiyi gün" sayılmır). Ona görə Kod
1/11 üçün ayrıca, `Mezuniyyet.Nov`-a görə süzülmüş bir sorğu yazıldı
(`NovUzreHaqiqiIsGunuAsync`).

### Struktur

`FinNex.Application/DTOs/Dsmf/` — `DsmfHissesi3Dto`/`DsmfHissesi3SetirDto`
(nəticə), `DsmfIslemeyenGunSebebi` (12 mətn, "combo" vərəqindən HƏRFİ
köçürülüb, `AvtomatikAskarlanmayanKodlar = {2,4,7,10,12}`). `IDsmfHesabatService`/
`DsmfHesabatService` (`FinNex.Application/Interfaces|Services/Dsmf/`) —
TƏK metod: `Hissesi3HazirlaAsync(il, rub)`. DI: `AddApplicationServices.cs`-ə
`AddScoped<IDsmfHesabatService, DsmfHesabatService>()`. Controller/View —
mövcud `Areas/Muhasibat/Controllers/HesabatController.cs`-ə əlavə action-lar
(`Dsmf3` önizləmə, `Dsmf3Excel` ixrac), `Views/Hesabat/Dsmf3.cshtml` (yeni),
kataloqa `"dsmf-hisse3"` kartı ("Rüblük" kateqoriyası — yuxarıdakı BMI
Günlük/Aylıq/Rüblük sxeminə uyğun).

**İşçi seçimi rüb-səviyyəlidir, "aktiv status" YOX** — rüb ərzində İSTƏNİLƏN
gün aktiv olmuş (işə qəbul/çıxma tarixi rüblə kəsişən) işçi daxil edilir,
`IsciStatus.Aktiv` kanonik filtri BURAYA YARAMAZ (çıxmış işçi də Kod 6 üçün
lazımdır). Hər ay üçün ayrıca tarix-kəsişməsi yoxlanır; heç bir səbəb günü
tapılmayan işçi sətir ALMIR (boş sətirlər Excel-ə düşmür).

**Excel ixracı — mövcud `AmbA1Excel` naxışının təkrarı**: DMS-dəki rəsmi
şablonu (`C:\FinNex_DMS\hesabat-sablonlari\muhasibat\dsmf\
UNIFIED_APPENDIX_1_UNEMPLOYED_DAYS.xlsx`) `XSSFWorkbook` ilə doldurur (varsa),
yoxdursa sadə `HSSFWorkbook` fallback. **Şablonun 2 nümunə sətiri (7,8-ci
sətirlər) açıq təmizlənir** — real sətir sayı 0 və ya 1-dirsə, örtülməyən
nümunə sətir (Sıra№ dolu, Səbəb mətni dolu, amma ad/FİN boş) sakitcə Excel-ə
düşərdi, bu, Dsmf3SablonDoldur-da `numuneSetirSayi=2` dövrü ilə qarşısı
alınıb.

⚠️ **GƏLƏCƏK İŞ (bu sessiyada edilmədi)**: Əlavə №1 Hissə 1 (maaş tərkibi —
yuxarıdakı "18 yaş" qeydinə görə bu məntiq MODELLƏŞDİRİLMƏMƏLİDİR), Əlavə №4
Bölmə 2-5 (vergi güzəşti cədvəlləri). Bu dörd/beş cədvəl istifadəçinin öz
sözü ilə *"yavaş-yavaş"* əlavə olunacaq — kataloqa yeni `Kod`/Action əlavə
etmək kifayətdir, mövcud "Rüblük" kateqoriyası onları da əhatə edəcək.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə bütün 6 yeni/dəyişən faylda,
interfeys↔implementasiya↔DI↔controller↔view beş qatı əl ilə tutuşduruldu,
hər istifadə olunan entity sahəsi/enum dəyəri — `Isci.FIN/AtaAdi/Sira`,
`Mezuniyyet.Nov/Status/JetonIleOdendi`, `BayramGunu.Tip/Tarix`,
`MezuniyyetNovu.Illik/DovletVezifelerininIcrasi`, `MezuniyyetStatus.
Tesdiqlenib` — mənbə fayllarından təsdiqləndi). Bir `@model DsmfHissesi3Dto`
üçün lazım olan `@using FinNex.Application.DTOs.Dsmf` sətri bu yoxlamada
unudulmuşdu (Muhasibat Area-nın `_ViewImports.cshtml`-i yalnız `DTOs.
Muhasibat`-ı idxal edir) — tapılıb düzəldildi, bu, "RZ1031"/"CS0246" kimi
yalnız real Rebuild-də üzə çıxan tələlərin başqa bir nümunəsi idi. İstifadəçi
Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır:
(1) bir rübdə illik məzuniyyəti olan işçi, (2) rüb ərzində işə qəbul olunan
işçi, (3) rüb ərzində işdən çıxan işçi, (4) iki ayı əhatə edən xəstəlik
bülleteni olan işçi — hər dördünün düzgün sütunda/sətirdə, düzgün gün sayı
ilə göründüyünü, Excel ixracının (şablon DMS-ə qoyulandan sonra) iki
nümunə sətiri səssizcə saxlamadığını təsdiqləməli.

## DSMF — Əlavə №4 Bölmə 3-4-5 — Sığorta-İstisna Gəlirlər (29.10.2026)

İstifadəçi tapşırığı: *"indi novbeti"* (Hissə 3 bitəndən sonra). İkinci dalğa
olaraq Əlavə №4-ün 3 bölməsi quruldu — bunlar Hissə 3-dən **struktur olaraq
tamamilə fərqlidir**: işçi-sətri DEYİL, **qanun maddəsi-sətridir** (hər sətir
bir VM/qanun bəndi, ay üzrə "işçi sayı" + "cəlb olunmayan məbləğ").

### "Əsgər pulu" nə idi — istifadəçinin öz şablon qeydindən tapıldı

İstifadəçi öz Excel şablonunda (`UNIFIED_APPENDIX_4_NON_INVOLVED_UNEMPLOYED.xlsx`/
`_ITS.xlsx`) maddə 6.3/7.2-nin "Cəlb olunmayan gəlirlər" xanasına **əl ilə**
"esger pulu ve xestelik pulu" qeydi yazmışdı. Araşdırma göstərdi ki, bu,
HR → Əmək haqqı parametrləri → **"Konfiqurasiyalı Gəlir Növləri"**-ndəki
("mühasibin yaratdığı xüsusi gəlirlər", məs. **"Hərbi Xidmət"**) bir
`MaasNovu` sətridir (`ManualGelir=true`) — ekran-görüntüsündə DSMF=✓,
İşsizlik=—, İTSS=— bayraqları göstərilirdi.

**Kodda dəqiq təsdiqləndi** (`MaasHesablamaService.cs:1077-1081,1147-1149`):
hər manual gəlir növünün **öz bayraqları** (`VergiyeCelb`/`DsmfyeCelb`/
`IssizliyeCelb`/`ItsseCelb`) vergi/haqq bazalarından çıxılıb-çıxılmayacağını
müəyyən edir — "Hərbi Xidmət"in `IssizliyeCelb=false`/`ItsseCelb=false`
olması onu DƏQIQ Bölmə 4 (6.3) və Bölmə 5 (7.2)-yə uyğun edir (Bölmə 3-ə YOX,
çünki `DsmfyeCelb=true`). Eyni kod bloku daha bir şey göstərdi: **xəstəlik
şirkət ödənişi** (`XestelikSirketOdenis`) təkcə DSMF-dən (Bölmə 3) yox,
**İşsizlik VƏ İTSS bazasından da** çıxılır (`dsmfBazasi`/`issizlikBazasi`/
`itssBazasi`-nin HƏR üçü `xestelikSirketOdenis`-i çıxır) — "esger pulu VE
**xestelik** pulu" qeydinin "VE xestelik" hissəsi dəqiq budur.

Əlavə tapıntı: Bölmə 3-ün birinci nümunə sətri (**5.2.9**, istifadəçinin
"cemi HY" qeydi) **HYS**-dir (`IsciHYS.Mebleg`, işçi payı) — eyni kod
blokunda `dsmfBazasi`-dən çıxılan `hysMebleg` ilə təsdiqləndi. "HY" = "HYS"
(Həyat Yığım Sığortası), ilk baxışda aydın olmadı, kod oxunaraq tapıldı.

**Dövlət hesabatı olduğu üçün (vergi/sosial sığorta hüquqi nəticəli)
`AskUserQuestion` ilə hər addımda təsdiqləndi** — "əsgər pulu"nun mənası,
sonra tam xəritə (B3=HYS+xəstəlik, B4/B5=xəstəlik+manual-gəlir-bayraqlı) —
heç biri təxmin edilib kor-koranə yazılmadı.

### Xəritə

| Bölmə | Maddə | Mənbə |
|---|---|---|
| 3 | 5.2.9 | `IsciHYS.Mebleg` (işçi payı) + işəgötürən payı (`MaasParametrNovu.HysIsegoturenFaizi`, default 15%) |
| 3 | 5.2.12 | `XestelikOdenis.SirketOdenis` |
| 4 | 6.3 | `XestelikOdenis.SirketOdenis` + manual `MaasNovu` (`IssizliyeCelb=false`) üzrə `MaasDetay.Mebleg`, BİRLİKDƏ (tək sətir) |
| 5 | 7.2 | `XestelikOdenis.SirketOdenis` + manual `MaasNovu` (`ItsseCelb=false`) üzrə `MaasDetay.Mebleg`, BİRLİKDƏ (tək sətir) |

**Bölmə 4/5-də "İşçi sayı" iki mənbənin BİRLƏŞMİŞ (distinct) işçi dəstidir** —
sadəcə iki sayı toplamaq SƏHV olardı (eyni işçi həm xəstəlik, həm manual-gəlir
istisnası aldıqda ikiqat sayılardı). `HashSet<int>.UnionWith` ilə düzəldi.

**Bayraqla tutulur, ADLA YOX.** "Hərbi Xidmət" heç yerdə ad üzrə axtarılmır —
`ManualGelirIstisnasiAsync(il, ay, issizlikFlag)` YALNIZ `IssizliyeCelb`/
`ItsseCelb` bayraqlarına baxır. Gələcəkdə HR başqa bir manual gəlir növü
yaradıb eyni bayraqla işarələsə (məs. "Fəxri fərman mükafatı" İşsizlikdən
azad edilsə), DSMF hesabatı kodda DƏYİŞİKLİK LAZIM OLMADAN avtomatik tutur —
bu, qəsdən belə dizayn edildi (sistemin öz flag-əsaslı arxitekturasına uyğun).

**Yalnız TƏSDİQLƏNMİŞ/ÖDƏNİLMİŞ maaş sayılır, Layihə YOX.** Həm
`XestelikSirketOdenisiAsync` (`XestelikOdenis.MaasId != null` + `Maas.Status`),
həm `ManualGelirIstisnasiAsync` (`MaasDetay` → `Maas.Status`) yalnız
`MaasStatus.Tesdiqlendi`/`Odenildi` sayır — hələ hesablanmamış (`Layihe`)
və ya ləğv edilmiş (`LegvEdildi`) ayın məlumatı DSMF-ə düşmür. HYS-in isə
`Maas`-a FK-sı yoxdur (payroll-un GİRİŞİdir, çıxışı deyil) — onun yeganə
qoruyucusu `IIsciHYSDondurmaService.DondurulmusIdlerAsync` (26.10.2026-dakı
"bir aylıq dondurma" — TƏK mənbə, öz sorğu yazılmadı).

### Struktur

`DsmfIstisnaBolmesiDto`/`DsmfIstisnaSetirDto` (`FinNex.Application/DTOs/Dsmf/
DsmfIstisnaBolmesiDto.cs`) — Hissə 3-ün DTO-sundan AYRI tip (fərqli sütun
sxemi). `DsmfIstisnaGostericileri.cs` — 4 maddənin mətni "combo" vərəqlərindən
HƏRFİ köçürülüb (qalan 27 maddə bu banka aid olmadığı üçün DAXİL EDİLMƏYİB).
`IDsmfHesabatService`-ə 3 yeni metod (`Bolme3/4/5HazirlaAsync`) — Hissə 3-ün
metodu ilə EYNİ interfeysdə. `DsmfHesabatService`-ə yeni `IIsciHYSDondurmaService`
asılılığı (konstruktor).

Controller/View — TƏK kataloq kartı (`dsmf-bolme345`), TƏK önizləmə səhifəsi
(`Bolme345.cshtml`, üç bölmənin hamısı eyni il+rüb seçimi ilə stack-lənib) —
"DÜZƏLİŞ #3"ün "eyni destinasiyaya iki qapı açma" qaydasının MƏNTİQİ davamı:
üç YAXIN məzmun süni şəkildə üç ayrı kart kimi çoxaldılmadı. Excel ixracı isə
HƏR bölmə üçün AYRI action-dadır (`Bolme3/4/5Excel`) — 3 fərqli rəsmi şablon
faylına (`UNIFIED_APPENDIX_4_NON_INVOLVED_{MDSS,UNEMPLOYED,ITS}.xlsx`) yazır,
Hissə 3-ün `Dsmf3SablonDoldur`-u ilə EYNİ NPOI naxışı (`XSSFWorkbook` doldur,
yoxdursa `HSSFWorkbook` fallback).

**Şablonun "sızma" qoruyucusu ÜMUMİLƏŞDİRİLDİ** (Hissə 3-ün `numuneSetirSayi`
sabit ədədindən fərqli) — `BolmeSablonDoldur` yazdığı sətirlərdən SONRA,
hələ "Göstəricilər" mətni olan HƏR şablon sətrini (nə qədər olursa olsun)
açıq təmizləyir. İndiki templatelərdə sızma riski YOXDUR (hər üçünün
əvvəlcədən doldurduğu nümunə sətir sayı mənim hesabladığım sətir sayı ilə
DƏQİQ üst-üstə düşür — B3=2, B4=1, B5=1), amma gələcəkdə şablon dəyişsə bu
qoruyucu özü uyğunlaşır.

⚠️ **GƏLƏCƏK İŞ**: Əlavə №1 Hissə 1 (maaş tərkibi, "18 yaş" məsələsi İSTİFADƏ
OLUNMAYACAQ) və Əlavə №4 Bölmə 2 (VM maddələri üzrə gəlir vergisi güzəştləri)
hələ qurulmadı — istifadəçi "yavaş-yavaş" sözü ilə qəsdən sonraya saxladı.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə bütün 6 yeni/dəyişən faylda,
interfeys↔implementasiya↔DI↔controller↔view beş qatı əl ilə tutuşduruldu —
`XestelikOdenis.MaasId/SirketOdenis`, `IsciHYS.Mebleg/BaslamaTarixi/
BitmeTarixi`, `MaasNovu.ManualGelir/IssizliyeCelb/ItsseCelb`, `MaasDetay.
MaasNovuId/Mebleg`, `Maas.Il/Ay/Status`, `MaasParametri.Nov/Deyer`,
`MaasParametrNovu.HysIsegoturenFaizi`, `MaasStatus.Tesdiqlendi/Odenildi` —
hamısı mənbə fayllarından bir-bir təsdiqləndi). Bu dəfə `Bolme345.cshtml`-də
unudulan `.pr-warn` CSS tərifi YAZILARKƏN tapılıb özüm düzəltdim (Dsmf3.cshtml-dəki
eyni "paylaşılan faylda yoxdur" tələsi) — publish-dən əvvəl. İstifadəçi Visual
Studio-da **Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır: (1) o rübdə
HYS-i olan işçi (5.2.9-da görünsün), (2) o rübdə xəstəlik bülleteni təsdiqlənib
maaşa bağlanmış işçi (5.2.12/6.3/7.2-də görünsün), (3) "Hərbi Xidmət" (və ya
oxşar, İşsizlik/İTSS-dən azad bir manual gəlir növü) məbləği daxil edilib
maaşı TƏSDİQLƏNMİŞ işçi (6.3/7.2-də görünsün, 5.2.12-də YOX) — hər üçünün
düzgün bölmədə, düzgün "işçi sayı"+"məbləğ" ilə göründüyünü, HƏLƏ "Layihə"
statuslu (hesablanmamış) ayın heç yerdə görünmədiyini təsdiqləməli.

### DSMF — Vahid Hub (29.10.2026) + Rəsmi Şablonların DMS-ə Köçürülməsi

İstifadəçi real mühitdə sınadı — iki şey üzə çıxdı:

1. **Bölmə 5 export-u `.xls` fallback (generik) formatda çıxdı, bütün rəqəmlər
   0 idi.** Səbəb: `C:\FinNex_DMS\hesabat-sablonlari\muhasibat\dsmf\` qovluğunda
   rəsmi şablon fayllar hələ yox idi — kod avtomatik `HSSFWorkbook` fallback-a
   keçdi (gözlənilən davranış, bax Hissə 3-ün "varsa… yoxdursa…" şərhi). 0
   dəyərlərin ÖZÜ ayrıca sualdır (rüb üçün real DSMF-istisna datası olub-
   olmaması DB-dən asılıdır, bu mühitdən yoxlana bilmədi) — istifadəçi "1-ci
   şablon exceldir, eyni çıxmalıdır, o şablonlardan istifadə et hamısında"
   dedi, yəni ƏN AZI formatın rəsmi şablona uyğun çıxmasını istədi. Dörd rəsmi
   fayl (bu sessiyanın əvvəlində artıq inspeksiya üçün oxunmuş, scratchpad-da
   saxlanılan nüsxələr) **istifadəçiyə geri göndərildi** (`SendUserFile`,
   dəqiq DMS yolu ilə) — `UNIFIED_APPENDIX_1_UNEMPLOYED_DAYS.xlsx`,
   `_4_NON_INVOLVED_{MDSS,UNEMPLOYED,ITS}.xlsx`. Kod özü DƏYİŞMƏDİ — problem
   faylların YERLƏŞDİYİ yer idi, kodun məntiqi deyil.
2. **"Vahid hesabat adını yaz, hazırladıqlarını onun içində göstər, altısını
   da"** — kataloqda İKİ ayrı DSMF kartı ("Əlavə №1 Hissə 3" + "Əlavə №4
   Bölmə 3-4-5") əvəzinə **TƏK kart** ("DSMF — Vahid Hesabat Forması") və
   onun açdığı **hub səhifəsi** (`DsmfVahid.cshtml`) — 6 rəsmi əlavənin
   HAMISINI adı ilə sadalayır (Hissə 1, Hissə 3, Bölmə 2, 3, 4, 5), hazır
   olan 4-ü (Hissə 3 + Bölmə 3/4/5, Bölmə 3/4/5 eyni `Bolme345` səhifəsinə
   açılır) yaşıl "Hazırdır" + "Aç" düyməsi ilə, hələ qurulmayan 2-si (Hissə 1,
   Bölmə 2) boz "Hazırlanır" nişanı ilə.

**Katalogdan silinən iki köhnə Kod ("dsmf-hisse3", "dsmf-bolme345")
TƏHLÜKƏSİZDİR** — `HesabatController.Index()` yalnız CARİ `Katalog`
siyahısını iterasiya edir, saxlanmış ★ seçimdə (`HesabatSecilmis`) bu
kodlardan biri qalsa belə, kataloqda olmadığı üçün sadəcə görünməz qalır
(mövcud qoruyucu şərh: "Kataloqda olmayan kod göndərilsə belə zərərsizdir").

**Yeni DTO** — `DsmfVahidKomponentDto` (`FinNex.Application/DTOs/Muhasibat/`,
`HesabatKartDto` ilə eyni qovluq/konvensiya): `ResmiAd`/`Tesvir`/`Hazirdir`/
`Action` (nullable — hazır deyilsə keçid göstərilmir). `DsmfVahid()` action-u
bu 6 sətiri STATİK siyahı kimi (Katalog prinsipi, DB-də saxlanmır) qaytarır.

⚠️ **Bölmə 3/4/5 sətirlərinin ÜÇÜ DƏ EYNİ `Bolme345` action-una açılır** —
qəsdən: istifadəçi "altısını da [adı ilə göstər]" dedi, amma onların ÜÇÜ
TƏK səhifədə birlikdədir (bax yuxarı "DÜZƏLİŞ #3" — eyni destinasiyaya çox
qapı açmaqdan çəkinmə qaydası) — bu, həmin qaydanı POZMUR, çünki bura bir
NAVİQASİYA qəfəsi deyil, STATUS SİYAHISIDIR (checklist), üç adın ayrı-ayrı
görünməsi "6-nın hamısını göstər" tələbinə xidmət edir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə, `HesabatKartDto` qovluğu ilə eyni konvensiya əl ilə
təsdiqləndi). İstifadəçi göndərilən 4 faylı dəqiq adı ilə DMS yoluna qoyub
Visual Studio-da **Rebuild All** ilə yoxlamalıdır: "Maliyyə Hesabatları"nda
indi TƏK "DSMF — Vahid Hesabat Forması" kartının olduğunu, onu açanda 6
sətirin (4 "Hazırdır" + 2 "Hazırlanır") göründüyünü, "Aç" düymələrinin düzgün
səhifəyə keçdiyini, Excel ixracının bu dəfə RƏSMİ şablon formatında (generik
fallback yox) və real rəqəmlərlə çıxdığını.

### DSMF Əlavə №4 Bölmə 2 (29.10.2026) + Bölmə 3-ün 5.2.9-u Düzəldildi — "sirket yox"

Yuxarıdakı bölmədə Bölmə 2 "Hazırlanır" idi — bu sessiyada quruldu. Mənbə
şablon: `UNIFIED_APPENDIX_4_DISCOUNTS.xlsx` ("Əlavə №4 - Bölmə 2. Gəlir
vergisi üzrə güzəşt və azadolmalar barədə məlumat"). İstifadəçi ilə iki
ambiguous nöqtə birbaşa həll edildi:

1. **HYS (VM 102.1.8) — yalnız işçi payı** (istifadəçi: "1. yalniz isci.").
2. **"Qaçqın" (VM 102.4.5) → `GuzestNovu.MecburiKockun`** (istifadəçi:
   "Məcburi Köçkün").

**5 sətir, kanonik əhatə** (qalan 27/32 VM maddəsi — neft-qaz, texnologiya
parkı, Ələt AİZ, diplomatik xidmət və s. — bu banka aid deyil):

| Sətir | Maddə | Mənbə |
|---|---|---|
| 4.2.6 (102.1.8) | HYS, YALNIZ işçi payı | `IsciHYS.Mebleg`, dondurma ilə (`IIsciHYSDondurmaService`) |
| 4.2.5 (102.1.6) | Standart 200 AZN güzəşt | `Maas.HesablamaIzahi` JSON-dan oxunur (aşağı bax) |
| 4.5.3 (102.4.5) | "Qaçqın" | `IsciGuzest`+`Guzest`, `Novu == MecburiKockun` |
| 4.4 (102.3) | "Əlillər" | `IsciGuzest`+`Guzest`, `Novu == Elillik` |
| 4.2.7.7 (102.1.14.16) | "əsgər pulu" | manual gəlir, `Min(MaasDetay.Mebleg, MaasNovu.GelirVergisiAzadMebleg)` |

**Standart 200 AZN güzəşti — TƏKRAR YAZILMADI, PERSİST edilmiş JSON-dan
OXUNUR.** `MaasHesablamaService.FerdiHesabla`-nın "9.2 Standart 200 AZN
güzəşti" düsturu (`brutMaasGuzestYoxlama ≤ firstBracketMax`) `elaveCemi`/
`mezuniyyetAvansBrutu`/`VM9821Meblegi` kimi dərin daxili dəyişənlərdən
asılıdır — DSMF-də müstəqil təkrar yazmaq "Bir Kəmiyyətə Üç Yazıcı" riski
yaradardı (payroll dəyişsə, DSMF sakitcə köhnə qalardı). Əvəzinə, `FerdiHesabla`
artıq HƏR ayın nəticəsini `Maas.HesablamaIzahi`-yə (JSON, `List<HesablamaIzahiDto>`,
`Addim="Vergi Guzesti"` → `Mebleg = standartGuzest + maxIsciGuzesti`, varsa
`Addim="İşçi güzəştləri"` → `Mebleg = maxIsciGuzesti`) yazır — DSMF servisi bu
JSON-u `JsonSerializer.Deserialize` ilə oxuyub `standartGuzest = VergiGuzesti.Mebleg
− İşçiGüzəştləri.Mebleg` fərqini çıxarır. Parse xətası (köhnə/korrupt JSON)
sakit `0` qaytarır — sətir əskik qalar, səhv rəqəm YAZILMAZ.

**Əlillik/Qaçqın — yalnız HƏQİQƏTƏN QALİB GƏLƏN güzəşt sayılır.** Bir işçidə
eyni anda İKİ fərqli növdə (məs. həm Əlillik, həm Məcburi Köçkün) aktiv güzəşt
olsa, vergi hesablamasında yalnız ƏN BÖYÜYÜ tətbiq olunur (Maddə 102 —
toplanmır). `GuzestNovuUzreAsync` hər növü ayrıca sorğulayır, amma sətrə YALNIZ
`Maas.HesablamaIzahi`-dəki "İşçi güzəştləri" addımının `Mebleg`-i HƏMİN
Guzest-in `Mebleg`-inə DƏQİQ bərabərdirsə yazır — əks halda güzəşt qeydiyyatda
var, amma o AY tətbiq olunmayıb (başqa güzəşt qalib gəlib) deməkdir, DSMF-ə
yazılmır. Bu, nadir bir edge-case üçün, formula-təkrarından daha etibarlı bir
cross-validation həllidir.

### "sirket yox" — Bölmə 3-ün 5.2.9-u da düzəldildi (RETROAKTİV KOD DƏYİŞİKLİYİ)

İstifadəçi Bölmə 2-nin HYS sualına "yalniz isci" cavabından SONRA, açıq şəkildə
əlavə etdi: *"Bölmə 3-dəki 5.2.9 kimi — yalniz iscilerden gedir sohbet, sirket
yox"* — yəni Bölmə 3-ün ARTIQ COMMIT OLUNMUŞ (əvvəlki, bu sessiyanın öz daxilindəki
turda) 5.2.9 sətri də (`HysCemiAsync`, işçi `IsciHYS.Mebleg` + işəgötürən 15%
faizini BİRLİKDƏ göstərirdi) YALNIZ işçi payına düzəldilməlidir. `AskUserQuestion`
ilə birbaşa təsdiqləndi (ambiguous idi: "sirket yox" Bölmə 2-yə aiddirmi, yoxsa
Bölmə 3-ü DƏ nəzərdə tutur?) — cavab: **"Bəli, Bölmə 3-ü də düzəlt"**.

**Düzəliş:** `HysCemiAsync` (işçi+işəgötürən, `MaasParametrNovu.HysIsegoturenFaizi`
15%) VƏ onun `HysIsegoturenFaiziAsync` köməkçisi TAMAMILƏ SİLİNDİ (başqa heç bir
çağıran qalmadı) — `Bolme3HazirlaAsync` indi Bölmə 2-nin özünün çağırdığı EYNİ
`HysIsciPayiAsync`-i (yalnız işçi) çağırır. Bu, "Bir Kəmiyyətə Üç Yazıcı"
qaydasının TƏRSİ — iki fərqli qanun maddəsi (102.1.8 vs 5.2.9) EYNİ "yalnız
işçi" qaydasını paylaşdığı üçün ortaq metod işlədilir, iki nüsxə saxlanmır.

⚠️ **Qayda: istifadəçinin "X kimi" ("bunun kimi") dediyi bir cavab, "X"-in
ÖZÜNÜN düzgün olduğu fərziyyəsinə əsaslanmasın** — burada "Bölmə 3-dəki 5.2.9
kimi" referens nöqtəsi əslində ÖZÜ səhv (köhnə, employee+employer) idi, istifadəçi
bunu YENİ TƏLƏB kimi işlətdi ("bu da elə O olmalıdır ki, mən artıq düzgün
bildiyim formadır"), kod isə hələ köhnə vəziyyətdə idi. Analoji istinadlı bir
cavabda HƏMİŞƏ istinad edilən yerin CARİ koddakı HƏQİQİ vəziyyətini yoxla —
uyğunsuzluq varsa (bura kimi) AÇIQ sual ver, səssizcə bir tərəfi fərz etmə.

### Hub + Önizləmə Səhifəsi Yeniləndi

`HesabatController.DsmfVahid()`-də Bölmə 2 sətri `Hazirdir = false` →
`Hazirdir = true, Action = "Bolme2345"` (indi 5/6 "Hazırdır", yalnız Hissə 1
qalır). **`Bolme345` action/view `Bolme2345`-ə YENİDƏN ADLANDIRILDI** (Bölmə
3/4/5 sətirlərinin `Action`-u da `Bolme2345`-ə köçürüldü) — səhifə indi 4
bölməni (2, 3, 4, 5) EYNİ `@foreach (var b in Model)` döngəsində göstərir,
çünki `DsmfIstisnaBolmesiDto`/`DsmfIstisnaSetirDto` strukturu Bölmə 2 üçün
DƏ eynən uyğun gəldi (yeni DTO yazılmadı). Yeni `Bolme2Excel` action-u
`Bolme3/4/5Excel` ilə EYNİ `BolmeExcelQaytar` helper-ini çağırır.

⚠️ **KRİTİK — Bölmə 2 şablonunun VƏRƏQ TAB ADI, EKRANDAKI başlıqdan
FƏRQLİDİR.** `UNIFIED_APPENDIX_4_DISCOUNTS.xlsx`-in A1 xanasında "Əlavə №4 -
**Bölmə 2**..." yazsa da, Excel-in ÖZ vərəq sekmesinin adı **"Əlavə №4 -
Hissə 1"**-dir (dövlət şablonunun özündə qalan köhnə/səhv ad — başqa bir
əlavədən kopyalanıb unudulmuş görünür). `BolmeSablonDoldur` `wb.GetSheet(vereqAdi)`
ilə TAB ADINA görə axtarır, hüceyrə mətninə görə YOX — `Bolme2Excel`
çağırışında `vereqAdi = "Əlavə №4 - Hissə 1"` yazılıb (EKRANDAKI "Bölmə 2"
mətni DEYİL). Səhv tab adı ilə çağırılsaydı `GetSheet` `null` qaytarardı,
`if (ws != null)` qoruyucusu doldurma blokunu TAMAMILƏ ATLAYARDI və fayl
SƏSSİZCƏ boş (yalnız şablonun öz nümunə sətirləri ilə) enərdi — heç bir xəta.

**Qayda: rəsmi dövlət Excel şablonunda vərəq SEKMESİNİN adı ilə hüceyrə
İÇİNDƏKİ başlıq mətni EYNİ OLACAĞINA GÜVƏNMƏ** — hər ikisini AYRI-AYRI yoxla
(`openpyxl`-la `wb.sheetnames` VƏ `ws['A1'].value`), xüsusilə şablon başqa bir
əlavədən kopyalanıb redaktə edilmiş görünürsə.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — Bölmə 3-ün 5.2.9 düzəlişi yalnız BUNDAN SONRA
çağırılan hesablamalara təsir edir; bu sessiyada əvvəllər (əgər varsa) real
mühitdə test edilmiş/ixrac olunmuş Bölmə 3 Excel-ləri YENİDƏN ixrac edilməlidir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə bütün 5 dəyişən/yeni faylda, `HesablamaIzahiDto`-nun
namespace-i (`FinNex.Application.DTOs.HR.Maas`) və `Addim`/`Mebleg` sahə adları
`MaasHesablamaService.cs`-in öz yazma kodundan hərfi təsdiqləndi). İstifadəçi
Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır: (1)
HYS-i olan, (2) brüt maaşı ≤2500 olan (standart güzəşt), (3) aktiv Əlillik/
Məcburi Köçkün güzəşti olan, (4) "əsgər pulu" kimi gəlir vergisi azadlığı olan
manual gəlir aldığı işçilərin Bölmə 2-də düzgün sətirdə göründüyünü; Bölmə 3-ün
HYS sətrinin artıq işəgötürən payını DAXİL ETMƏDİYİNİ (əvvəlki ixracla
müqayisə edərək) təsdiqləməlidir.

### Hub-dan "Aç" — HƏR Bölmə ÖZ Cədvəlini Göstərməlidir, 4-ü BİRLİKDƏ YOX (29.10.2026)

İstifadəçi real ekranda (`Bolme2345`) dörd cədvəlin (Bölmə 2/3/4/5) BİRLİKDƏ,
üst-üstə göründüyünü görüb qarışdı — bir neçə dəfə "bu nədi, neçə Excel var,
mən 6 göndərmişəm" sualı ilə geri döndü, son olaraq AYDIN tələb etdi:
**"hansını AÇDISA O EXCELƏ AİD OLANI GÖSTƏR"** — hub-da "Bölmə 2" sətrinin
"Aç" düyməsinə basanda YALNIZ Bölmə 2 görünsün, "Bölmə 3"ə basanda YALNIZ
Bölmə 3, və s. — dördü eyni səhifədə BİRLİKDƏ YOX.

**Kök səbəb:** `DsmfVahidKomponentDto`-nun 4 sətrinin (Bölmə 2/3/4/5) `Action`
sahəsi hamısı `"Bolme2345"` idi, AMMA heç bir fərqləndirici route dəyəri
ötürülmürdü — controller də HƏMİŞƏ bütün 4 bölməni eyni `List`-ə yığıb
qaytarırdı. Bir obyektiv baxımdan "bu, qəsdən status-checklist dizaynıdır,
DÜZƏLİŞ #3-ü pozmur" deyə əsaslandırılmışdı (bax yuxarı, eyni günün əvvəlki
qeydi) — amma istifadəçinin REAL gözləntisi fərqli idi: "Aç" bir NAVİQASİYA
düyməsidir, status-siyahısı deyil, ona görə YALNIZ klikləniləni açmalıdır.

**Düzəliş — `bolmeNo` route dəyəri:**
- `DsmfVahidKomponentDto.BolmeNo` (yeni, nullable `int?`) — Bölmə 2/3/4/5
  sətirlərinə `2`/`3`/`4`/`5` yazılır, Hissə 1/3 üçün `null` qalır (onların
  öz ayrıca action-u var, bu sahəyə ehtiyac yoxdur).
- `DsmfVahid.cshtml` — "Aç" linkinə `asp-route-bolmeNo="@k.BolmeNo"` əlavə
  olundu. `BolmeNo` `null`-dursa tag helper parametri URL-ə ÜMUMİYYƏTLƏ
  qoşmur (standart ASP.NET Core davranışı) — Hissə 1/3 linklərinə təsir
  etmir.
- `HesabatController.Bolme2345(int? bolmeNo, int? il, int? rub)` — `bolmeNo`
  veriləndə (2/3/4/5) YALNIZ o bölmənin `BolmeXHazirlaAsync`-ini çağırıb
  **TƏK elementli** siyahı qaytarır; `bolmeNo` verilmədikdə (kimsə köhnə/
  təmiz URL-ə əl ilə girsə) köhnə "4-ü birlikdə" davranış **geriyə uyğunluq
  üçün** qalır (`switch` ifadəsinin `_` qolu).
- `Bolme2345.cshtml` — `tekBolme` (`Model.Count==1` olanda `Model[0].BolmeNo`)
  dəyişəninə görə: başlıq ("Bölmə 2" vs "Bölmə 2-3-4-5"), "Əhatə" xəbərdarlıq
  qutusu (yalnız həmin bölmənin maddələri, lüğət `bolmeEhate`) VƏ İl/Rüb
  filtrindəki **gizli `bolmeNo` input-u** (filtri dəyişib "Göstər"ə basanda
  eyni bölmədə qalsın, 4-ə geri düşməsin) dinamik dəyişir.

**Qayda: "status-siyahısı" ilə "naviqasiya" eyni görünə bilər, amma İSTİFADƏÇİ
ÜÇÜN eyni şey DEYİL.** Hub-da "bu 6 şeydən hansı hazırdır" sualına cavab
tapmaq (status) ilə "bu KONKRET şeyə bax" demək (naviqasiya) iki fərqli
niyyətdir — bir "Aç" düyməsi İKİNCİSİNİ vəd edir, bu səbəbdən klikləndiyi
elementdən FƏRQLİ, daha geniş bir nəticə göstərməsi (bura: 4-ü birlikdə)
baxmayaraq "düzgün əsaslandırılmış dizayn qərarı" olsa da, istifadəçini
"bu nədir, niyə başqa şeylər də var" sualına sürükləyir. Bir neçə komponent
EYNİ görüntü səhifəsini paylaşırsa, həmin səhifə HƏMİŞƏ "hansı elementdən
açıldığını" BİLMƏLİ və YALNIZ ONU göstərməlidir — ümumi/bütöv görünüş YALNIZ
açıq tələb olunanda (route parametri verilmədikdə) ehtiyat kimi qalmalıdır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə 4 dəyişən faylda). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real mühitdə yoxlamalıdır: hub-da "Bölmə 2"nin "Aç"
düyməsinə basanda YALNIZ Bölmə 2 cədvəlinin göründüyünü, "Bölmə 3"ə basanda
YALNIZ Bölmə 3-ün, İl/Rüb dəyişib "Göstər"ə basanda eyni bölmədə qaldığını
(4-ə geri düşmədiyini).

## DSMF Vahid Hesabat Forması — Son Parça: Əlavə №1 Hissə 1 (29.10.2026)

Altı rəsmi əlavədən SONUNCUSU quruldu — "qur" əmri ilə başladı. Mənbə şablon:
`UNIFIED_APPENDIX_1_EMPLOYEE_LIST.xlsx`, vərəq `Əlavə1_Hissə1_Muzdlu işə`.
Hissə 3 kimi İŞÇİ-sətridir (qanun-maddəsi-sətri YOX), amma hər işçinin hər ay
üzrə **11 göstəricisi** var (2.1–6.1, real başlıq sətirlərindən (3-5) və
mühasibin öz nümunə sətrindəki (10-cu sətir) "e/h dan S+O", "e/h M+T+U" kimi
daxili cross-ref qeydlərdən çıxarıldı — bu qeydlər HƏRFİ İMPLEMENTASİYA ÜÇÜN
DEYİL, yalnız hansı sütunun NƏYƏ uyğun olduğunu təsdiqləmək üçün oxundu).

### Sütun xəritəsi — 4-dən 2-si YENİ hesablama, 4-ü MÖVCUD Bölmə 2-5-in cəmi

| Kod | Sütun | Mənbə |
|---|---|---|
| 2.1 | Əsas iş yeri olması | **HƏMİŞƏ "Bəli"** — aşağı bax |
| 2.2 | Faktiki iş günü | Ayın real iş günü − **Hissə 3-ün** bu işçinin işləmədiyi gün cəmi |
| 2.3 | Hesablanmış əmək haqqı | `Maas.BrutMebleg` |
| 2.3.1 | …tərkibinə daxil olmayan gəlirlər | **HƏMİŞƏ boş** (mühasibin öz "bos" qeydi) |
| 2.5 | HYS — erkən xitam | **HƏMİŞƏ boş** (mühasibin öz "bos" qeydi, sistemdə iz yoxdur) |
| 2.6 | Texnologiya parkı seçimi | **HƏMİŞƏ boş** (N/A — bank, texnologiya parkı deyil) |
| 3.1 | Azadolunan/güzəşt cəmi | **Bölmə 2**-nin bu işçi üçün CƏMİ (5 sətrin hamısı) |
| 4.1 | DSMF azad cəmi | **Bölmə 3**-ün bu işçi üçün CƏMİ |
| 4.5 | HYS-DSMF əlaqəli | **HƏMİŞƏ boş** (2.5 ilə əlaqəli, "bos") |
| 5.1 | İşsizlik azad cəmi | **Bölmə 4**-ün bu işçi üçün CƏMİ |
| 6.1 | İTSS azad cəmi | **Bölmə 5**-in bu işçi üçün CƏMİ |

**3.1/4.1/5.1/6.1 üçün YENİ DÜSTUR YAZILMADI** — Bölmə 2/3/4/5-in artıq
qurulmuş per-işçi hesablayıcıları (`HysIsciPayiAsync`, `StandartGuzestAsync`,
`GuzestNovuUzreAsync`, `ManualGelirVergiIstisnasiAsync`, `XestelikSirketOdenisiAsync`,
`ManualGelirIstisnasiAsync`) elə Hissə 1-də DƏ çağırılır. Bu, həm dəqiqlik
(eyni rəqəm hər iki yerdə), həm "Bir Kəmiyyətə Üç Yazıcı" riskinin qarşısını
alır.

### Refaktor — `(HashSet<int>, decimal)` → `Dictionary<int, decimal>`

Bölmə 2/3/4/5-in 6 köməkçi metodu əvvəl `(HashSet<int> Iscilər, decimal Cemi)`
(aqreqat) qaytarırdı — Hissə 1-ə hər işçinin ÖZ PAYI lazım olduğu üçün hamısı
`Dictionary<int, decimal>` (IsciId → Mebleg) qaytaracaq şəkildə dəyişdirildi.
Bölmə 2/3/4/5-in özü `dict.Count`/`dict.Values.Sum()` ilə əvvəlki aqreqat
nəticəni DƏQIQ eyni formada alır — **rəqəmlərdə heç bir dəyişiklik yoxdur**,
yalnız görüntü forması. Bu, 29.10.2026-ın özündə TƏKRAR-TƏKRAR işlənən bir
naxışdır (əvvəlcə "sirket yox" düzəlişi ilə HYS metodu Bölmə 2 İLƏ Bölmə 3
arasında paylaşıldı, indi HAMISI Hissə 1 İLƏ də paylaşılır).

### Faktiki iş günü — Hissə 3-ün ÖZ nəticəsini TƏKRAR HESABLAMADAN istifadə

`Hissesi1HazirlaAsync` birbaşa `await Hissesi3HazirlaAsync(il, rub)` çağırır
və onun `Setirler`-ini (hər işçinin hər ay üzrə işləmədiyi gün CƏMİNİ, bütün
səbəb kodları üzrə toplanmış) bir `Dictionary<(IsciId,AyIndex), int>`-ə yığır.
Sonra: `FaktikiIsGunu = Ayın_real_iş_günü − bu_cəm`. Yeni "gün sayma" məntiqi
YAZILMADI — gün-təsnifatının ÖZÜ (`HaqiqiIsGunuSayAsync`, `OzHesabinaIsGunuSayAsync`
və s.) Hissə 3-dən bəri dəyişməz qalır, bura yalnız ONUN NƏTİCƏSİNİ ƏKS
ÇEVİRİR. "Rübdə aktiv işçi" sorğusu da (`IscilərRubdeAktivAsync`, yeni şərhi)
Hissə 3-dən ÇIXARILIB PAYLAŞILAN helper edildi — iki yerdə eyni sorğu
YAZILMASIN deyə.

### "Əsas iş yeri olması" — HƏMİŞƏ "Bəli", AÇIQ QƏBUL EDİLMİŞ FƏRZİYYƏ

Real şablonda 27 işçidən 2-sinin bəzi aylarda "Xeyr" olduğu aşkarlandı, amma
B (Ad)/C (FİN) sütunları şablonda BOŞ idi — KONKRET kimin "Xeyr" olduğu
bilinmir. Sistemdə "əsas/əlavə iş yeri" bayrağı HEÇ VAXT olmayıb (`Isci`
entity-sində belə sahə yoxdur). İstifadəçiyə birbaşa soruşuldu —
**"Hamısına Bəli yaz"** cavabı aldı. Bu, ~7% (2/27) real nümunədə YANLIŞ
nəticə verəcək bir fərziyyədir, amma sistemdə izi olmadığı üçün başqa yol
yoxdur — açıq qeyd olunub, gizlədilməyib.

### Excel ixracı — Vərəq adı BU DƏFƏ EKRAN başlığı ilə UYĞUNDUR

`Bölmə 2`-nin şablonunda (`UNIFIED_APPENDIX_4_DISCOUNTS.xlsx`) vərəq adı
yanıltıcı idi ("Hissə 1" yazırdı, amma Bölmə 2 idi) — bu, AYRI bir fayldır,
QARIŞDIRILMASIN. Hissə 1-in ÖZ şablonunda vərəq adı `Əlavə1_Hissə1_Muzdlu işə`
— həm ekran başlığına, həm məzmuna UYĞUNDUR, heç bir tələ yoxdur.

Data **10-cu sətirdən** (NPOI 0-əsaslı 9) başlayır. Şablon artıq 27 REAL
işçinin (mühasibin özünün qismən doldurduğu) sətirlərini daşıyır — bizim
real nəticəmizin sətir sayı bundan FƏRQLİ ola bilər (çoxala/azala bilər),
ona görə `Bolme2345`-in "sızma qoruyucusu" naxışı tətbiq olundu: yazdığımız
sətirdən SONRA, hələ **Sıra№ (A sütunu) dolu qalan** HƏR sətri açıq təmizləyir
— sabit "numuneSetirSayi=27" ƏDƏDİNƏ GÜVƏNMƏDİ (şablon versiyası dəyişsə belə
işləməyə davam edir).

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — bax yuxarı eyni qayda (persist edilmiş
`HesablamaIzahi` JSON-dan oxunan sütunlar, keçmiş aylarda dəyişmir).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə 5 dəyişən/yeni faylda, DTO↔servis↔controller↔view
dörd qatı sahə-adı-adı əl ilə tutuşduruldu — `FaktikiIsGunu`/`HesablananMebleg`/
`AzadGuzestCemi`/`DsmfAzadCemi`/`IssizlikAzadCemi`/`ItssAzadCemi`/`EsasIsYeri`).
İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə
yoxlamalıdır: hub-da indi 6/6 "Hazırdır" olduğunu, Hissə 1-i açıb bir neçə
işçinin sətirinin (faktiki iş günü, brüt maaş, Bölmə 2/3/4/5 cəmləri) düzgün
göründüyünü, Excel ixracının rəsmi şablona (27 köhnə nümunə sətiri qalmadan)
uyğun çıxdığını.

### DSMF — Gələcək Rüb Üçün Yarım-Dolu, Yanlış Rəqəmlər Göstərirdi (29.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi real ekranda (Hissə 1) İl-i qəsdən **2027**-yə dəyişdi — hələ
başlamamış, tamamilə gələcək bir il/rüb. Gözlənilən nəticə boş cədvəl idi
("belə dövr hələ yaşanmayıb"), amma səhifə 24 işçi üçün sətir göstərdi: **İş
günü** sütunu real ay təqviminə görə tam qiymət (21/22/23) yazırdı, eyni
sətrin **Hesablanmış** sütunu isə "—" idi (Maas qeydi yoxdur, hələ hesablanmayıb).
Sual: *"2027 yaziram melumat gelir, sehvdir axi"* — haqlı idi.

**Kök səbəb:** `Hissesi1HazirlaAsync`-in (və onun daxilən çağırdığı
`Hissesi3HazirlaAsync`-in) "aktiv işçi" filtri (`IscilərRubdeAktivAsync`)
yalnız işə-qəbul/işdən-çıxma tarixinə baxır — işdən çıxmamış işçi HƏR rübə
(keçmiş, cari, gələcək) "aktiv" sayılır, ÜMUMİYYƏTLƏ. "Faktiki iş günü" isə
saf TƏQVİM hesabıdır (`HaqiqiIsGunuSayAsync`, bayram/həftəsonu çıxılmış) —
Maas, Mezuniyyet, Xəstəlik kimi HEÇ BİR real data mənbəyinə baxmır, HƏR ayın
real iş günü sayını qaytarır, ay hələ baş verməmiş olsa belə. Nəticədə
gələcək bir rübdə "iş günü" DOLU, "Hesablanmış" BOŞ — yarım-dolu, çaşdırıcı
bir sətir.

**Niyə Bölmə 2/3/4/5 bu buga düşmür:** onların hər sətri `Maas.Il==il &&
Maas.Ay==ay` üzərindən REAL Maas qeydinə bağlıdır (`HysIsciPayiAsync`,
`StandartGuzestAsync`, `XestelikSirketOdenisiAsync` və s.) — gələcək dövrdə
Maas yoxdur, bu dict-lər təbii olaraq BOŞ qalır, "0 işçi / 0 ₼" göstərir. Bug
YALNIZ saf-təqvim əsaslı "iş günü" hesablayıcılarında idi (Hissə 1/3).

**Düzəliş — `DsmfHesabatService.RubGelecekdedir(il, ay1)`** (yeni, `static`,
`new DateTime(il, ay1, 1) > DateTime.Today`): HƏM `Hissesi3HazirlaAsync`, HƏM
`Hissesi1HazirlaAsync` öz əvvəlində bu şərti yoxlayır, `true`-dursa ağır
per-işçi hesablamanı (DB sorğuları daxil) BAŞLAMADAN `GelecekRubdur=true`
ilə boş nəticə qaytarır. **Hissə 1-in ÖZ ayrıca yoxlaması MƏCBURİDİR** — Hissə
3-ün erkən qayıtması kifayət etməzdi: Hissə 1 öz "ayinRealIsGunu"-nu (saf
təqvim) MÜSTƏQİL hesablayır, Hissə 3-ün boş "işləməyib" cəmi ilə birləşəndə
"tam ay işləyib" kimi YANLIŞ nəticəyə gələrdi.

**Yeni DTO sahəsi — `GelecekRubdur`** (`DsmfHissesi1Dto`, `DsmfHissesi3Dto`)
— görüntü qatı bunu `Model.Setirler.Count == 0`-dan AYRI, ÖNCƏLİKLİ şərt kimi
yoxlayır: "heç bir işçi aktiv olmayıb" (yanlış, çaşdırıcı mesaj — işçilər
aktivdir, sadəcə dövr hələ yoxdur) ƏVƏZİNƏ **"bu rüb hələ başlamayıb — gələcək
dövrdür"** göstərir.

**Qayda (layihənin "İlin Son Günü" prinsipinin GÜZGÜ forması):** "son gün"
aşkarlanmasında tək tarix-müqayisəsinə güvənmək SƏHVDİR (real DATA şərti
lazımdır) — BURADA isə ƏKSİNƏ, saf tarix-müqayisəsi ("bu dövr hələ
başlamayıbmı?") elə DOĞRU, kifayət edən yoxlamadır, çünki sual data varlığı
haqqında DEYİL, dövrün ÖZÜNÜN baş verib-vermədiyi haqqındadır. Təqvim-əsaslı
(Maas/Mezuniyyet-dən MÜSTƏQİL) bir hesablayıcı yazanda, onu HƏR ZAMAN "bu
dövr artıq başlayıbmı?" sualı ilə qoruyun — əks halda gələcək bir tarix
seçimi saf riyazi cəhətdən "düzgün" (amma mənasız) rəqəmlər istehsal edər.

⚠️ **Bölmə 2/3/4/5-ə TOXUNULMADI** — onlar artıq Maas-a bağlı olduğu üçün
gələcək dövrdə təbii olaraq boş/sıfır göstərir, əlavə qoruyucu lazım deyil.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə, DTO↔servis↔view sahə adı `GelecekRubdur`
üç yerdə də tutuşduruldu). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra real brauzerdə yoxlamalıdır: 2027 (və ya hər hansı hələ başlamamış
rüb) seçəndə Hissə 1 və Hissə 3-ün hər ikisinin "hələ başlamayıb" mesajını
göstərdiyini, keçmiş/cari bir rübün isə əvvəlki kimi düzgün işlədiyini.

### DSMF — İl/Rüb Seçim Qutusu Səliqəsiz İdi — `<select>` Heç Stil Almırdı (29.10.2026)

İstifadəçi (Hissə 1 hero banner-indəki İl/Rüb/Göstər bloku göstərib): *"rub
yazilan yeride seliqeye sal"*, sonra dəqiq eyni bloku yaxından göstərib:
*"buranidi duzeltde, ag fon ile seliqesizdir axi"*.

**Kök səbəb — İKİ qat:**
1. **`.mh-datef select` CSS QAYDASI ÜMUMİYYƏTLƏ YOX İDİ** — paylaşılan
   `_MhStyles.cshtml`-də yalnız `.mh-datef input` (digər Dashboard
   səhifələrinin `<input type="date">`-i üçün) var idi. DSMF-in İl/Rüb
   `<select>`-ləri heç bir stil almadan, brauzerin ÇILPAQ, kəskin-künclü ağ
   qutusu kimi göründü — tünd-yaşıl hero fonunun üstündə "yad" cisim.
2. **Label-lər select-in ÜSTÜNDƏ dayanmırdı, YANINDA sıxışırdı.** Markup
   `<label>İl</label><select>...</select><label>Rüb</label>...` idi —
   `.mh-datef` özü `display:flex` (sətir istiqamətində) olduğu üçün hər
   `<label>`/`<select>` AYRI, bərabər flex-uşağı idi; `label`-in `display:
   block; width:100%` qaydası heç nəyə xidmət etmirdi (flex sətirdə
   "növbəti sibling-i aşağı itələmək" effekti vermir, sadəcə flex-shrink
   ilə öz mətninə sıxılır). Nəticə: "İl [2027] Rüb [Rüb 4...]" hamısı bir
   sətirdə, label-lər balaca, sıxışmış görünürdü.

**Düzəliş — `_MhStyles.cshtml`-ə əlavə (başqa heç bir səhifəyə TOXUNULMADI,
tamamilə ADDİTİV):** `.mh-datef select` qaydası (`input`-la EYNİ ağ/rounded
görünüş + özəl SVG chevron ikonu, `appearance:none` ilə OS-defolt oxu
götürülüb) + yeni `.mh-datef-f` sinfi (`flex-direction:column` — label+select
cütünü öz sütununa yığır). Üç DSMF formu (`Dsmf1.cshtml`, `Dsmf3.cshtml`,
`Bolme2345.cshtml`, hamısı EYNİ markup-u paylaşır) hər İl/Rüb cütünü
`<div class="mh-datef-f">`-ə alacaq şəkildə yeniləndi.

⚠️ **Dashboard-un təkfield `<input type="date">` formaları (Icmal/Kredit/
Depozit/Menfeet/Likvidlik/Ifrs9 və s., 13 fayl) TOXUNULMADI** — onlarda
`.mh-datef-f` wrapper yoxdur, `.mh-datef select` qaydası da onlara aid deyil
(select işlətmirlər) — yeni CSS tamamilə additiv olduğu üçün bu səhifələrin
görünüşü dəyişmədi.

⚠️ **Qayda: `display:block` + `width:100%` olan bir label, yalnız öz
valideyni VERTIKAL axında (və ya ayrıca flex-column konteynerdə) olanda
"select-dən əvvəl öz sətrini tutur".** Eyni label həmin `display:flex`
(sətir istiqamətli) bir konteynerin DİRƏKT uşağıdırsa, bu qayda sükutla
effektsiz qalır — hər uşaq öz MƏZMUNUNA görə sıxılır, label select-in
YANINDA qalır, ÜSTÜNDƏ yox. Flex-sətir konteynerində "label üstdə, control
altda" düzümü istəyirsənsə, cütü AYRI, `flex-direction:column` bir wrapper-ə
al — CSS-in `display:block` qaydasına güvənmə kifayət etmir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə/div balansı skriptlə). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real brauzerdə yoxlamalıdır: Hissə 1/Hissə 3/
Bölmə 2-5 səhifələrinin HƏR ÜÇÜNDƏ "İl" və "Rüb" etiketlərinin indi öz
seçimlərinin ÜSTÜNDƏ dayandığını, select qutularının rounded/ağ/oxlu
göründüyünü, digər Mühasibat Dashboard səhifələrinin (tarix inputu olan)
əvvəlki kimi qaldığını.

### DSMF Hub — Hər Kartda Rəsmi Excel Şablonunun Fayl Adı (29.10.2026)

İstifadəçi (Visual Studio-dakı `C:\...\2026-3\` qovluğunu göstərib): *"bir
seyide elave et, exceldeki adlar vare, employee, discount, onlar hansina
aiddirse onu yaz, bilek ki hansi exceldi"* — "DSMF — Vahid Hesabat Forması"
hub-ında hər kartın hansı rəsmi `UNIFIED_APPENDIX_*.xlsx` faylını
doldurub ixrac etdiyini görmək istədi.

**Yeni sahə — `DsmfVahidKomponentDto.SablonFaylAdi`** — `HesabatController.
DsmfVahid()`-in statik siyahısına əlavə olundu, `Dsmf1/Dsmf3/Bolme2/3/4/
5Excel`-in ÖZ `"...xlsx"` sabitlərindən HƏRFİ köçürülüb (yeni araşdırma
aparılmadı, elə həmin action-ların kodunda artıq var idi):

| Kart | Fayl |
|---|---|
| Əlavə №1 — Hissə 1 | `UNIFIED_APPENDIX_1_EMPLOYEE_LIST.xlsx` |
| Əlavə №1 — Hissə 3 | `UNIFIED_APPENDIX_1_UNEMPLOYED_DAYS.xlsx` |
| Əlavə №4 — Bölmə 2 | `UNIFIED_APPENDIX_4_DISCOUNTS.xlsx` |
| Əlavə №4 — Bölmə 3 | `UNIFIED_APPENDIX_4_NON_INVOLVED_MDSS.xlsx` |
| Əlavə №4 — Bölmə 4 | `UNIFIED_APPENDIX_4_NON_INVOLVED_UNEMPLOYED.xlsx` |
| Əlavə №4 — Bölmə 5 | `UNIFIED_APPENDIX_4_NON_INVOLVED_ITS.xlsx` |

Görüntü — `DsmfVahid.cshtml`-də hər kartın təsvirindən dərhal sonra kiçik,
yaşıl/mono-şrift bir "etiket" (`.dv-sablon`) kimi göstərilir — sırf oxunuşluq
üçün, keçid/filtr mənası daşımır.

⚠️ **Bu, SADƏCƏ GÖSTƏRİŞDİR — hansı faylın hansı DMS yoluna qoyulacağına
(`hesabat-sablonlari/muhasibat/dsmf/`) TƏSİR ETMİR**, sadəcə istifadəçiyə
("bilək ki hansı excel-dir") əvvəlcədən mövcud olan `Dsmf*Excel` action-
larının öz sabit fayl adını əks etdirir — iki yerdə (controller-in daxili
sabiti və hub-dakı bu DTO sahəsi) eyni ad TƏKRAR yazılıb; gələcəkdə fayl
adı dəyişsə, HƏR İKİSİ yenilənməlidir (hal-hazırda avtomatik sinxronlaşan
tək bir mənbə yoxdur — sadə, statik siyahı olduğu üçün qəsdən belə saxlanıldı).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə/div balansı skriptlə). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real brauzerdə yoxlamalıdır: hər 6 kartın
altında düzgün `.xlsx` adının göründüyünü.

### DSMF Hub — "Hazırdır" Pill Lazımsız Təkrar İdi (29.10.2026)

İstifadəçi: *"hazirdir ve tesdiq isaresi neye lazimdirki burda?"* — haqlı
sual. Hər sətirdə HƏM sol tərəfdəki yaşıl ✓ ikonu, HƏM sağdakı yaşıl
"Hazırdır" pill-i, HƏM "Aç" düyməsi eyni şeyi ("bu komponent hazırdır")
**üç dəfə** deyirdi. "Hazırlanır" tərəfi (hourglass ikon + pill) bu
tələkamına düşmür — orada "Aç" düyməsi YOXDUR, pill yeganə mətn-izahdır.

**Düzəliş — `DsmfVahid.cshtml`:** `Hazirdir=true` qolundan `<span
class="dv-status dv-status--ready">Hazırdır</span>` SİLİNDİ, yalnız ✓ ikon +
"Aç" düyməsi qaldı. Ölü CSS qaydası (`.dv-status--ready`) da silindi.
`Hazirdir=false` qolu TOXUNULMADI.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı**. İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real
brauzerdə yoxlamalıdır.

### DSMF Bölmə 2 (4.2.6/102.1.8) — İşəgötürən HYS Payı ƏSKİK İDİ (29.10.2026, KRİTİK — DÜZƏLDİLDİ)

Yuxarıdakı "Bölmə 2-nin 5.2.9-u kimi, sirket yox" düzəlişindən (bax yuxarı
"'sirket yox'" bölməsi) dərhal sonra, EYNİ GÜN, istifadəçi geri döndü:
*"Azad güzəşt-men burda sene sehv dedime + sirketin odediyi (İşgötürən
tərəfindən ödənilən həyatın yığım sığortası haqqları)"* — yəni həmin "sirket
yox" qərarı Bölmə 3-ün (5.2.9, DSMF-dən azad) 5.2.9-u üçün **düzgün** idi,
amma Bölmə 2-nin (4.2.6, VM 102.1.8, gəlir vergisindən azad) eyni "HYS" sətri
üçün **səhv** idi — ikisi eyni görünən, amma fərqli hüquqi əsasda olan sətirlərdir.

**Riyazi sübut — `MaasHesablamaService.FerdiHesabla`-nın öz düsturlarından:**
```
brutMaas   = esasBrut + hysIsegoturen                     // 2.3-ə (Hesablanmış) DAXİLDİR
vergiBazasi = esasBrut + ... - hysMebleg + ...             // hysIsegoturen HEÇ VAXT yoxdur (nə +, nə -)
dsmfBazasi  = esasBrut + ... - hysMebleg - ... + ...        // eyni, hysIsegoturen heç vaxt yoxdur
```
`esasBrut` özü `hysIsegoturen`-i daşımır (`brutMaas = esasBrut + hysIsegoturen`
düsturunun özü bunu göstərir) — yəni işəgötürən HYS payı **gəlir vergisi
bazasına HEÇ VAXT DÜŞMƏYİB**, kodun öz şərhi (MaasHesablamaService.cs ~1132)
bunu açıq deyir: *"İşəgötürən HYS payı: Gəlir vergisi və DSMF-dən AZAD"*.
Amma eyni məbləğ `BrutMebleg`-in (və deməli DSMF Hissə 1-in 2.3 "Hesablanmış"
sütununun) İÇİNDƏDİR — yəni 2.3 onu göstərir, vergi bazası onu göstərmir.
Bu fərq DSMF Bölmə 2-də **"gəlir vergisindən azad olan məbləğ"** kimi
GÖRÜNMƏLİDİR, əks halda 2.3 − 3.1 real vergi bazası ilə uzlaşmır.

`dsmfBazasi` isə AYRI düsturdur — `hysIsegoturen`-ə heç bir sətirdə toxunmur
(nə əlavə, nə çıxma). Yəni Bölmə 3-ün 5.2.9-u üçün "sirket yox" qərarı
**riyazi olaraq həmişə doğru olub** — DSMF bazası işəgötürən HYS-ə heç vaxt
toxunmayıb, ona görə onu "azad" kimi göstərməyə ehtiyac yoxdur (artıq
BAZADA yoxdur). Bölmə 2-nin VM 102.1.8-i isə tam fərqli bir qanuni əsasdır
(gəlir vergisi, 2.3-ün içindəki BrütMebleg-dən çıxılma) — iki sətir eyni
söz (HYS) daşısa da, konseptual olaraq EYNİ DEYİL.

**Düzəliş — `FinNex.Application/Services/Dsmf/DsmfHesabatService.cs`:**
1. Yeni `HysIsegoturenMapAsync(il, ay)` — `MaasDetay`-dan (`MaasNovu.Ad ==
   "HYS (İşəgötürən)"`, `Tip = IsegoturenXerci` — `MaasHesablamaService.cs:1436`-
   dakı `DetayEkle` çağırışının ÖZÜ) işəgötürənin ödədiyi HYS məbləğini
   işçi üzrə oxuyur. Yeni hesablama YAZILMADI, artıq persist edilmiş sətir
   oxunur (digər Bölmə 2-5 köməkçiləri ilə EYNİ "Maas.HesablamaIzahi/
   MaasDetay-dan oxu, təkrar hesablama" prinsipi).
2. `Bolme2HazirlaAsync`-in 4.2.6 (HYS) sətri artıq `HysIsciPayiAsync`
   (işçi) + `HysIsegoturenMapAsync` (işəgötürən) **cəmini** göstərir.
3. `Hissesi1HazirlaAsync`-in **3.1 "Azad güzəşt"** sütunu da eyni cəmi
   (hys + hysIseg) daxil edir.
4. `Hissesi1HazirlaAsync`-in **4.1 "DSMF azad"** sütunu İSƏ **TOXUNULMADI**
   — hələ də YALNIZ `hys[i]` (işçi payı), çünki DSMF bazası işəgötürən
   HYS-ə heç vaxt toxunmayıb (yuxarıdakı riyazi sübut).
5. `DsmfIstisnaGostericileri.Madde1018`-in şərhi və `Bolme2345.cshtml`-dəki
   "Əhatə" izah mətni ("4.2.6 (HYS, yalnız işçi payı)") yeniləndi.

**Qayda (bu sessiyanın özündə İKİ DƏFƏ təkrarlanan tələ):** eyni termini
("HYS") daşıyan, amma FƏRQLİ DSMF bölmələrinə aid iki sətir (burada: Bölmə 2
VM 102.1.8 vs Bölmə 3 5.2.9) eyni qaydaya (burada: "yalnız işçi payı")
avtomatik tabe sayılmasın — hər sətrin öz qanuni əsasını (hansı VERGİ BAZASI-
na aid olduğunu) ARAŞDIR, əks halda bir düzəliş başqa, konseptual oxşar
görünən sətri **səhvən** eyni istiqamətə çəkə bilər (bax "Bölmə 3-dəki 5.2.9
kimi" tələsi, yuxarıdakı "sirket yox" bölməsinin özündəki qayda).

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — düzəliş yalnız BUNDAN SONRA çağırılan
hesablamalara təsir edir; bu gün daha əvvəl (bu düzəlişdən əvvəl) ixrac
edilmiş Bölmə 2/Hissə 1 Excel-ləri YENİDƏN ixrac edilməlidir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə, `MaasDetay.MaasNovu` naviqasiyasının
EF Core-da `x.Maas.Il` ilə EYNİ qaydada (implicit join, `.Include` lazım
deyil) işlədiyi, artıq mövcud `x.Maas.Il == il` istifadələri ilə
tutuşdurularaq təsdiqləndi). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra real ssenari ilə yoxlamalıdır: HYS-i olan bir işçinin Bölmə 2
cədvəlindəki (4.2.6) məbləğinin artıq həm işçi, həm işəgötürən payını
EHTİVA ETDİYİNİ (əvvəlkindən böyük çıxmalıdır), Hissə 1-in "Azad güzəşt"
(3.1) sütununun da uyğun artdığını, "DSMF azad" (4.1) sütununun İSƏ
DƏYİŞMƏDİYİNİ təsdiqləməli.

### DSMF Hissə 1 (2.3) — VM 98.2.1 Gəlirləri ƏSKİK İDİ (29.10.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi: *"Əlavə №1 — Hissə 1-bu hesabatda + VM-nin 98.2.1-ci maddəsinə
əsasən vergiyə cəlb olunan gəlirlər"* — ekran-görüntüsü "HESABLANMIŞ" sütununu
göstərirdi. Araşdırmada tapıldı: `Hissesi1HazirlaAsync`-in 2.3 "Hesablanmış"
sütunu YALNIZ `Maas.BrutMebleg`-dən oxuyurdu, halbuki eyni metodun öz
şərhində sütun artıq **"Hesablanmış əmək haqqı VƏ DİGƏR GƏLİRLƏR"** adlanırdı —
"digər gəlirlər" hissəsi faktiki olaraq heç nə daşımırdı.

**Səbəb — `MaasHesablamaService.FerdiHesabla`-nın öz şərhi (1099-1100):**
*"VM 98.2.1 gəlirləri BRÜT-ə DAXİL EDİLMİR (gəlir kimi maaşı ARTIRMIR)"* —
VM9821 (işçi kreditləri üzrə hesabi/imputed gəlir) `BrutMebleg`-ə HEÇ VAXT
yazılmır, AMMA `input.VM9821Meblegi` BÜTÜN 4 vergi/sığorta bazasına
(`vergiBazasi`, `dsmfBazasi`, `issizlikBazasi`, `itssBazasi`) **ƏLAVƏ OLUNUR**
(kodun öz şərhi: *"VM 98.2.1 gəlirləri [...] BÜTÜN 4 bazaya əlavə olunur —
brüt-ə YOX"*). Yəni real DSMF/vergi bazası `BrutMebleg`-dən **böyükdür** — bu
fərq DSMF Hissə 1-in 2.3 sütununda görünməlidir, çünki Hissə 1 DSMF-ə təqdim
edilən, DSMF-in öz baxışından olan bir rəqəmdir — bankın daxili "brüt maaş"
konsepti ilə eyni olmaq MƏCBURİ DEYİL.

**Düzəliş — `DsmfHesabatService.cs`:** yeni `Vm9821MapAsync(il, ay)` —
`MaasDetay`-dan (`MaasNovu.Ad == "VM 98.2.1 Gəlirləri"`, `Tip = Gelir` —
`MaasHesablamaService.cs:1417`-dəki `DetayEkle` çağırışının ÖZÜ) oxuyur.
`Hissesi1HazirlaAsync`-in 2.3 ("Hesablanmış") sütunu indi `brutMaplar[i] +
vm9821[i]` cəmidir.

⚠️ **YALNIZ 2.3-ə əlavə olunur — 3.1/4.1/5.1/6.1 "azad" sütunlarına
TOXUNULMUR.** VM9821 "güzəşt/azadolma" DEYİL — əksinə, qəsdən HƏR DÖRD bazaya
vergiyə-cəlb-olunan gəlir kimi əlavə edilən bir məbləğdir (adı da bunu deyir:
"vergiyə cəlb olunan gəlirlər"). Onu hər hansı "azad" sütununa yazmaq
TAMAMİLƏ YANLIŞ olardı.

⚠️ **Bu, 08.10.2026-da sənədləşmiş "VM9821 brütə düşmür" qaydası İLƏ
ZİDDİYYƏT DEYİL** — o qayda (yuxarı, "Yekun Zolaq" bölməsindəki "Əmək Haqqı
Tarixçəsi" düzəlişi) VM9821-in `Maas.BrutMebleg`/`NetMebleg`-ə (bankın öz
"brüt/net maaş" göstərişinə) DAXİL EDİLMƏDİYİNİ təsdiqləyir — bu, DƏYİŞMİR,
`BrutMebleg`-in özünə TOXUNULMADI. DSMF-in 2.3 sütunu isə bankın öz brüt
konseptindən AYRI, DSMF-in TƏLƏB etdiyi "hesablanmış əmək haqqı və digər
gəlirlər" tərifidir — ikisi FƏRQLİ MƏQSƏD üçün FƏRQLİ rəqəmlər ola bilər,
eynilə "Qabaqcadan Məzuniyyət Pulu" bölməsindəki "iki ekran, iki əhatə"
prinsipi kimi (bax yuxarı, "Net Qabaqcadan Məzuniyyət Pulunu Göstərmirdi").

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — yalnız BUNDAN SONRA çağırılan Hissə 1
hesablamalarına təsir edir; bu gün daha əvvəl ixrac edilmiş Excel-lər
YENİDƏN ixrac edilməlidir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə, `MaasDetay.MaasNovu.Ad` filtri
yuxarıdakı HYS düzəlişi ilə EYNİ, artıq təsdiqlənmiş naxışla yazıldı).
İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə
yoxlamalıdır: VM 98.2.1 gəliri olan (işçi kreditləri üzrə, HR → Toplu
Hesablama-dakı "VM 98.2.1" sahəsindən daxil edilmiş) bir işçinin Hissə 1-dəki
"Hesablanmış" (2.3) rəqəminin `BrutMebleg`-dən DƏQIQ VM9821 məbləği qədər
BÖYÜK çıxdığını, VM9821 heç olmayan ayda isə rəqəmin əvvəlki kimi
(`BrutMebleg`-ə bərabər) qaldığını təsdiqləməli.

### DSMF Bölmə 3 (5.2.9) / Hissə 1 (4.1) — "sirket yox" Qərarı RƏSMİ ŞABLONLA TƏRSİNƏ ÇEVRİLDİ (29.10.2026, KRİTİK — DÜZƏLDİLDİ, ÜÇÜNCÜ DALĞA)

Eyni günün əvvəlki iki düzəlişindən (yuxarı: "DSMF Bölmə 2... İşəgötürən HYS
Payı ƏSKİK İDİ" və "DSMF Hissə 1 (2.3)... VM 98.2.1") sonra istifadəçi rəsmi
şablonun (`UNIFIED_APPENDIX_1_EMPLOYEE_LIST___.xlsx`, vərəq
`Əlavə1_Hissə1_Mudzlu işə`) ekran-görüntüsünü göstərdi: xana **K10** (sütun
4.1 — "Məcburi Dövlət Sosial Sığorta haqqına cəlb olunmayan cəmi məbləğ",
yəni DSMF azad) mühasibin öz qeydini daşıyır — **"e/h M+T+U"**. Qonşu **N10**
(6.1 — İTSS azad) isə sadəcə **"e/h M"**. Bu fərq (ÜÇ komponent vs BİR)
K10-un (4.1) İKİ yox, ÜÇ şeydən ibarət olduğunu göstərdi.

İstifadəçi bunu 9-sütunlu bir arayış cədvəli ilə (Xəstəlik vərəqəsi, Yalnız
mdss haqqı hesablanan digər gəlirlər, VM 98.2.1, VM 98.2.3, Əlavə əmək haqqı,
HYS 3-il-tamam-olmamış-qayıdan, Cəmi hesablanmış aylıq ödənişlər, Ödənilmiş
(işçi) HYS, İşgötürən ödənilən HYS) müşayiət etdi — *"bunlarin toplami
olmalidir Məcburi Dövlət Sosial Sığorta haqqına cəlb olunmayan cəmi
məbləğ"*. `AskUserQuestion` ilə dəqiqləşdirildi (dövlət hesabatı olduğu, həm
də elə HƏMİN GÜN əvvəlcə "sirket yox" sonra "sirket var" (Bölmə 2 üçün) kimi
iki dəfə dəyişmiş bir qərarın ÜÇÜNCÜ dəfə TƏRS İSTİQAMƏTDƏ dəyişməsi tələb
olunduğu üçün) — istifadəçi təsdiqlədi: **M+T+U = Xəstəlik vərəqəsi + Ödənilmiş
(işçi) HYS + İşgötürən ödənilən HYS**. Qalan 4 yeni anlayış (VM 98.2.3, HYS
3-il-erkən-xitam, Əlavə əmək haqqı, "yalnız mdss") **bu dəfə MODELLƏŞDİRİLMİR**
— istifadəçi: *"Hələlik yox — yalnız M+T+U sualını həll et, qalanı sonraya
saxla"*.

**Nəticə — "sirket yox" qərarı Bölmə 3 üçün DƏ TƏRSİNƏ ÇEVRİLDİ:**
`Bolme3HazirlaAsync`-in 5.2.9 sətri (və onunla Hissə 1-in `DsmfAzadCemi`,
4.1 sütunu) indi `HysIsciPayiAsync` (işçi) + `HysIsegoturenMapAsync`
(işəgötürən) CƏMİDİR — Bölmə 2-nin 4.2.6 sətri (bir əvvəlki dalğada
düzəldilmiş) İLƏ EYNİ naxış, həm işçi həm işəgötürən dəstinin `UnionWith`
ilə birləşdirilməsi daxil.

**Riyazi dəstək (hesablama DÜSTURUNA güvənməkdən istifadəçinin göndərdiyi
RƏSMİ ŞABLONUN qeydinə üstünlük verildi, amma bu, təsadüfi deyil — ikisi
üst-üstə düşür):** Hissə 1-in 2.3 ("Hesablanmış") sütunu `Maas.BrutMebleg`-dən
oxunur, o isə `brutMaas = esasBrut + hysIsegoturen` düsturuna görə İŞGÖTÜRƏN
HYS-i ONSUZ DA DAŞIYIR. `dsmfBazasi` düsturunun özü `hysIsegoturen`-ə heç vaxt
toxunmasa da (nə əlavə, nə çıxma — çünki `esasBrut` onu onsuz da daşımır),
RƏSMİ HESABATIN məqsədi "2.3-ün tərkibini izah etmək"dir: 2.3-ün bir hissəsi
(işəgötürən HYS) DSMF-dən AZADDIR — bu, daxili `dsmfBazasi` dəyərini
dəyişdirmir, amma hesabatın "2.3 − (3.1+4.1+5.1+6.1)" özü-özünü yoxlayan
strukturunu tələb edir. Beləcə əvvəlki "dsmfBazasi heç vaxt toxunmayıb, demək
exempt sayılmağa ehtiyac yoxdur" məntiqi YARIMÇIQ idi — "baza nəyi daşıyır"
sualı ilə "hesabat NƏYİ necə izah edir" sualı EYNİ ŞEY DEYİL.

**Dəyişən fayllar:**
- `DsmfHesabatService.cs` — `Bolme3HazirlaAsync` (5.2.9 sətri), `Hissesi1HazirlaAsync`
  (`DsmfAzadCemi`), `HysIsciPayiAsync`/`HysIsegoturenMapAsync`-in öz baş şərhləri
  (hər ikisi İNDİ dörd çağıranın hamısının eyni cəmi işlətdiyini əks etdirir).
- `Views/Hesabat/Bolme2345.cshtml` — `bolmeEhate[3]` mətni "yalnız işçi
  payı" → "işçi + işəgötürən payı".

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — yalnız BUNDAN SONRA çağırılan Bölmə 3/Hissə 1
hesablamalarına təsir edir; bu gün daha əvvəl (bu düzəlişdən əvvəl) ixrac
edilmiş Bölmə 3/Hissə 1 Excel-ləri YENİDƏN ixrac edilməlidir.

⚠️ **Qayda (bu sessiyanın özündə ÜÇÜNCÜ dəfə təkrarlanan dərs):** eyni
HYS-in iki fərqli DSMF sütununa (burada: Bölmə 2 gəlir-vergisi vs Bölmə 3
DSMF) "eyni qaydayla" (bura: "sirket yox") tabe olacağını FƏRZ ETMƏ — hər
sütunun öz qanuni/hesabat əsasını (ilk növbədə rəsmi şablonun ÖZÜNDƏKİ
nümunə/qeydləri, yalnız daxili düstur araşdırmasını YOX) AYRI-AYRI yoxla.
Rəsmi dövlət şablonunun öz worked-example qeydi (K10/N10-dakı "e/h M+T+U"/
"e/h M"), daxili `dsmfBazasi` düsturunun "bu məbləğ heç vaxt bazaya
toxunmayıb" məntiqindən DAHA GÜCLÜ bir dəlildir — ikisi arasında ziddiyyət
görünəndə şablonun özünə üstünlük ver, amma dövlətə gedən hesabat olduğu
üçün HƏMİŞƏ `AskUserQuestion` ilə təsdiqlədikdən sonra kodu dəyiş.

⚠️ **Açıq qalan (istifadəçi qərarı: sonraya saxlanıldı) — VM 98.2.3,
"HYS müqavilələri üzrə 3 il tamam olmamış qayıdan məbləğlər", "Əlavə əmək
haqqı", "Yalnız mdss haqqı hesablanan digər gəlirlər".** Bu 4 anlayışın
sistemdə HEÇ BİR qarşılığı (MaasNovu/MaasDetay sahəsi) yoxdur — araşdırılmadı,
modelləşdirilmədi. Toxunulanda əvvəlcə HƏR BİRİNİN BMI-də real mövcud olub-
olmadığını, sonra hansı `MaasDetay`/yeni entity sahəsinə uyğun gəldiyini
müəyyənləşdirmək lazımdır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə, Bölmə 2/Bölmə 3-ün yeni eyni-naxışlı
`HashSet.UnionWith` bloklarının bir-biri ilə eyniliyi əl ilə tutuşduruldu).
İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə
yoxlamalıdır: işəgötürən HYS-i olan bir işçinin Bölmə 3 (5.2.9) və Hissə 1
(4.1) sütunlarındakı məbləğin indi İŞÇİ+İŞGÖTÜRƏN cəmini göstərdiyini
(əvvəlkindən böyük çıxmalıdır), Bölmə 2 (4.2.6)/Hissə 1 (3.1)-in artıq
dəyişmədiyini (əvvəlki dalğada düzəlmişdi) təsdiqləməlidir.

## DSMF Önizləmə Səhifələri — Üfüqi Scroll AŞAĞIDA, Hero HÜNDÜR İdi (30.10.2026)

İstifadəçi iki ekran-görüntüsü göndərdi (Əlavə №1 Hissə 1 səhifəsi): *"burda
ede bilerikki, cedveli saga cevirmek ucun gerekki sehifeni tam asagi edesen
sobra scrolu gorunur, hemde yuxardiki 2 ci sekil, bu hisseni biraz
hundurluynude azalda bilerik"* — (1) cədvəli SAĞA sürüşdürmək üçün ƏVVƏLCƏ
BÜTÖV SƏHİFƏNİ aşağı düşürmək lazım gəlirdi (yalnız onda üfüqi scrollbar
görünürdü), (2) yaşıl hero qutusunun hündürlüyü azaldılsın.

**Kök səbəb (1) — `overflow-x:auto` TƏK BAŞINA kifayət etmirdi, çünki
konteynerin HÜNDÜRLÜYÜ məhdud deyildi.** `Dsmf1.cshtml` (18 sütun × 26 sətir),
`Dsmf3.cshtml`, `Bolme2345.cshtml` hər üçündə cədvəl `<div
style="overflow-x:auto;">` ilə örtülmüşdü — bu, `hr-jeton.css`-in "BEŞİNCİ
DALĞA" qeydindəki qaydaya (`overflow:hidden` YOX, `overflow-x:auto`) riayət
edirdi və sütunları KƏSMİRDİ, AMMA konteynerin özü sərbəst hündürlükdə
olduğu üçün cədvəlin BÜTÜN sətirləri (uzun siyahılarda onlarla sətir) adi
səhifə axınında göstərilirdi — üfüqi scrollbar YALNIZ konteynerin ən
ALTINDA (demək ki, bütün sətirlərdən SONRA) çıxırdı. Uzun bir cədvəldə bu,
elə `hr-jeton.css`-in "ALTINCI DALĞA" qeydindəki simptomun EYNİSİ idi
("scroll edilə bilən, amma TAPILA bilməyən") — fərq yalnız istiqamətdə idi
(orada şaquli scroll-un altında itən üfüqi scroll, burada bütöv səhifənin
altında itən üfüqi scroll).

**Düzəliş — `.dsmf-scroll` (yeni sinif, HƏR ÜÇ səhifənin öz yerli `<style>`
blokunda, "paylaşılan faylda yoxdur, yerli nüsxə" konvensiyasına uyğun):**
```css
.dsmf-scroll { overflow: auto; max-height: 60vh; border-radius: 0 0 14px 14px; }
.dsmf-scroll table.hj-table thead th { position: sticky; top: 0; z-index: 2; background: var(--hj-bg); }
```
`overflow-x:auto;` inline stili olan hər `<div>` (Dsmf1: 1 yer, Dsmf3: 1 yer,
Bolme2345: `@foreach`-in İÇİNDƏ, bütün bölmələr üçün 1 yer) `class="dsmf-scroll"`-a
keçirildi. `max-height:60vh` konteyneri PƏNCƏRƏYƏ GÖRƏ məhdudlaşdırır — nəticə:
üfüqi scrollbar artıq cədvəlin ÖZÜNÜN altında (görünən sahədə) çıxır, bütöv
səhifəni aşağı düşürməyə ehtiyac yoxdur; sətir sayı çoxdursa, indi həmin
qutunun DAXİLİNDƏ şaquli scroll əmələ gəlir (bu da əlavə fayda — səhifənin
özü də artıq uzanmır). `thead th`-ə əlavə olunan `position:sticky` isə bu
daxili şaquli scrollun təbii nəticəsidir — sütun başlıqları aşağı sürüşəndə
yox olmasın deyə (`background: var(--hj-bg)` AÇIQ yazıldı, çünki sticky-ə
keçən `<th>` öz arxa fonunu `<tr>`-dən MİRAS GÖTÜRMƜ zəmanəti yoxdur —
`hr-jeton.css`-də fon yalnız `.hj-table thead tr`-ə verilib, `th`-in özünə
yox).

**Kök səbəb (2) + Düzəliş — hər üç səhifənin yerli `<style>` blokuna
`.mh-hero--dsmf` üçün daraldılmış override:** `padding:26px 30px` (paylaşılan
`_MhStyles.cshtml`-dəki `.mh-hero` defolt dəyəri — BÜTÜN Dashboard
səhifələrində, toxunulmadı) → DSMF-ə xas `padding:16px 26px`, başlıq
`font-size:28px` → `20px`, alt-mətn `13.5px` → `12px`, badge/başlıq arası
boşluqlar sıxıldı, İl/Rüb seçim qutusunun öz paddingi/hündürlüyü də (36px →
30px) azaldıldı. **`_MhStyles.cshtml`-in ÖZÜNƏ TOXUNULMADI** — o, Balans
İcmalı/Kredit/Depozit/Mənfəət-Zərər/Likvidlik/Ifrs9 kimi onlarla başqa
Dashboard səhifəsində paylaşılır, qlobal dəyişiklik həmin səhifələrin
görünüşünü də (sorğusuz) dəyişdirərdi — düzəliş YALNIZ `.mh-hero--dsmf`
sinfinə (indiyədək də yalnız DSMF səhifələrində işlənən, fon rəngini
dəyişdirən bir modifier-dir) bağlı, page-scoped qaldı.

**Qayda (təkrar, YENİ forması):** `overflow-x:auto` (və ya `overflow-y:auto`)
olan bir konteyner sınırsız (auto) HÜNDÜRLÜKDƏdirsə, tərs-istiqamətli scrollbar
konteynerin ÖZ SONUNDA çıxır — çox sətirli/sütunlu cədvəllərdə bu, "scroll
edilə bilən, amma görünən sahədən kənarda, tapıla bilməyən" vəziyyətinə
aparır (eyni sinif, `hr-jeton.css`-in "ALTINCI DALĞA" qeydindəki kimi).
Konteynerə bir `max-height` (və `overflow:auto`, hər iki istiqamətdə) qoymaq
scrollbar(lar)ı HƏMİŞƏ görünən sahənin kənarında saxlayır — bu, "min-width ilə
genişləndirmə" həllindən (əvvəlki dalğalarda sınanmış, sonra tərsinə
çevrilmiş) FƏRQLİDİR: BURADA problem "sütun genişliyi" deyil, "konteynerin
HÜNDÜRLÜYÜ sərbəstdir" idi.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə hər üç faylda, `<style>`/`</style>`
və `<div>`/`</div>` sayları tutuşduruldu — Dsmf3.cshtml-dəki "style-open=2"
görüntüsü Razor ŞƏRHİ içindəki `<style>` sözündən irəli gəlirdi, real
mötərizə balanssız deyil). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra real brauzerdə yoxlamalıdır: Hissə 1 (və Hissə 3, Bölmə 2-5) cədvəlinin
indi öz altında (bütöv səhifəni aşağı düşürmədən) üfüqi scrollbar göstərdiyini,
çox sətirli nəticədə qutunun daxilində şaquli scroll olanda sütun başlıqlarının
yuxarıda yapışıb qaldığını, yaşıl hero qutusunun hündürlüyünün azaldığını,
AMMA digər Mühasibat Dashboard səhifələrinin (Balans İcmalı və s.) öz
hero-sunun ƏVVƏLKİ kimi qaldığını (çünki `_MhStyles.cshtml`-ə toxunulmayıb).

## Paylaşımlar — Admin-ə "Kim Oxudu?" Göstərişi (30.10.2026)

İstifadəçi tapşırığı: *"bizim paylaşdıqlarımızı kimlər oxudu, biləm ki maraq
varmı buna, admin kimi görə bilim"*.

**`MelumatOxunma` (yeni cədvəl) — `PaylasimBaxis`-dan QƏSDƏN AYRI.**
`PaylasimBaxis` (27.10.2026) hər `AppUser` üçün TƏK sətirdir, "lentə SON NƏ
VAXT baxdı" sualına cavab verir (yalnız sidebar sayğacı üçün) — bu, HANSI
KONKRET postu oxuduğunu bilmir. Yeni `MelumatOxunmalari` isə bir sətir =
bir **(Post, İşçi)** cütü — hər postun kim tərəfindən açıldığını ayrıca
saxlayır. İki cədvəl fərqli sualın cavabıdır, biri o birini əvəz etmir.

**"Oxundu" SİQNALI — lentdə görünmək YOX, sətri AÇMAQ.** `/User/Paylasim`-in
sıx-siyahı görünüşündə (28.10.2026-dan) hər post əvvəlcə qısa önizləmə kimi
gəlir, tam mətn YALNIZ sətrə klikləyəndə (`.py-row-summary` → `.py-row-full`)
açılır. "Oxundu" qeydi məhz BU klikdə yazılır (`pyOxunduIsaretle`,
`User/Views/Paylasim/Index.cshtml`) — sırf lentdə görünən bir başlıq
"oxunmuş" sayılmır, işçi həqiqətən açıb baxmalıdır.

**Server tərəfi:** `IPaylasimService.OxunduIsaretleAsync(melumatId, appUserId)`
— İDEMPOTENT (eyni cütü ikinci dəfə yazmır, ilk oxunma vaxtı qalır), unikal
indeks (`MelumatId`+`AppUserId`) son sədd kimi qoruyur. Endpoint
(`User/PaylasimController.Oxundu`) `RehberHrLegv`-lə EYNİ konvensiya ilə
`[ValidateAntiForgeryToken]` DAŞIMIR — səhifədə heç bir forma/token yoxdur,
JS sadə `fetch(...).catch(()=>{})` ilə fire-and-forget çağırır.

**Admin görüntüsü:** `Admin/Paylasim/Index.cshtml`-dəki hər kartda (yalnız
Qaralama OLMAYAN postlarda) "👁 N nəfər oxudu" linki — `Admin/Paylasim/
Oxuyanlar/{id}` səhifəsinə aparır, orada ad-soyad + oxunma tarixi siyahısı
var (ən sondan əvvələ sıralı). `HamisiniGetirAsync` bu sayı `MelumatOxunma`-
dan **bir dəfəlik qruplaşdırılmış sorğu** ilə doldurur (`oxuyanSaylari`
dictionary-si — `hedefSaylari`-nin elə yanında, N+1 yoxdur).

⚠️ **"Hədəf sayı" (kimə göndərilib) ilə "oxuyan sayı" (kim açıb baxıb)
QƏSDƏN NİSBƏTLƏNDİRİLMİR (faiz/"N-dən M nəfər" göstərilmir).** Səbəb —
hədəfləmə (`MelumatHedef`) **Isci.Id** üzərindən, oxuma izi isə
**AppUser.Id** üzərindən gedir (istifadəçi login edəndə AppUser kimi
tanınır, Isci-yə bağlantı — `AppUser.IsciId` — boş ola bilər). İki fərqli
kimlik sistemini süni şəkildə bir nisbətə sıxışdırmaq (məs. "12/45 oxudu")
yanlış dəqiqlik təəssüratı yaradardı — ona görə YALNIZ xam "N nəfər oxudu"
sayı göstərilir, məxrəcsiz.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — bu düzəlişdən ƏVVƏL açılmış postlar üçün heç
bir oxuma izi yoxdur (cədvəl boş başlayır), "0 nəfər oxudu" göstərəcək,
baxmayaraq ki real olaraq kimsə görmüş ola bilər — yalnız BUNDAN SONRAKI
açılışlar izlənir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə bütün 11 yeni/dəyişən faylda,
interfeys↔implementasiya↔DI↔controller↔view beş qatı əl ilə tutuşduruldu,
`AppUser.Ad/Soyad` sahələrinin birbaşa mövcudluğu mənbə fayldan təsdiqləndi).
İstifadəçi Visual Studio-da **Rebuild All**, `Update-Database`, sonra real
ssenari ilə yoxlamalıdır: bir paylaşımı işçi kimi açıb oxu → Admin → həmin
postun kartında "1 nəfər oxudu" göründüyünü, linkə basanda ad-soyad +
vaxtın düzgün göründüyünü təsdiqləməli.

## HR → Görüşlər İdarəetməsi — İlk Dəfə HR-ın Baxış Ekranı (04.11.2026)

İstifadəçi Davamiyyət səhifəsindəki "Tədbirdə" statusunu araşdırarkən
(«Rüfət Camalzadə — 09:00 · Ağacəkmə aksiyası») suala gəldi: *"tədbirdə olanı
işçi özü yazır yoxsa HR? və bunları hardansa izləmək olur?"* Kod araşdırması
göstərdi ki, bu status **Ezamiyyət DEYİL** — tamam başqa, `GorushIshtirakci`
(Görüşlər) modulundan gəlir (`HR/DavamiyyetController`-in prioritet zənciri:
*İcazəli > Xəstəlik > Ezamiyyət > Tədbirdə*, "Tədbirdə" elə bu modulun
qeydidir). **Görüşlər modulunun HR tərəfdə HEÇ bir baxış/idarəetmə ekranı
yox idi** — yalnız `/User/Gorush` (işçinin özü, təşkilatçı və ya iştirakçı
kimi) var idi, HR bunu yalnız Davamiyyət sətrindəki kiçik mətndən görürdü,
detallara (gündəm, iştirakçılar, status) baxacağı yer yox idi.

**Qərar — YALNIZ OXUMA ekranı qurulur, yaratma/redaktə/ləğv HƏLƏ DƏ yalnız
işçinin (təşkilatçının) öz səhifəsindən.** Görüş yaratma/status-dəyişmə
məntiqinə (`GorushService.YaratAsync/StatusYenileAsync/EditAsync/SilAsync`,
hamısı `TeshkilatciIsciId != isciId` şərti ilə qorunur) TOXUNULMADI — HR-ın
bu əməliyyatlara nəzarəti sorulmayıb, əlavə etsək icazə modelini dəyişmək
lazım gələrdi (ayrıca qərar).

**Yeni servis metodu — `IGorushService.GetHamisiniAsync(GorushFiltrDto?)`.**
Mövcud `GetMenimGorushlerimAsync`/`GetTeshkilEtdiklerimAsync`-dən FƏRQLİ —
təşkilatçıdan ASILI OLMADAN BÜTÜN görüşləri gətirir (tarix aralığı, status,
departament — `TeshkilatciIsci.IsciTeyinatlari` üzərindən, Ezamiyyət-in
departament filtri ilə EYNİ naxış — və başlıq/təşkilatçı adı üzrə axtarışla).
Detal üçün YENİ metod YAZILMADI — mövcud `GetDetayAsync(id, isciId)` artıq
`isciId`-yə görə BLOKLAMIR (yalnız "mənim statusum" göstərişi üçün işlədir),
ona görə HR `GetDetayAsync(id, 0)` ilə birbaşa çağırır — `0` heç bir real
`Isci.Id`-yə uyğun gəlmir, sentinel kimi təhlükəsizdir.

**`Areas/HR/Controllers/GorushController.cs`** (yeni) — `[Authorize(Roles =
"Admin,HR,Rehber,SobeReisi")]`, Ezamiyyət İdarəetməsi ilə EYNİ rol dəstəsi
(bu, artıq "bütün şirkət üzrə baxış" səhifələri üçün qəbul edilmiş qaydadır).
`Index` (filtr+KPI səhifəsi) + `GetGorushler` (AJAX JSON) + `Detay` (tam
səhifə, redaktə YOXDUR). Controller yalnız `IGorushService`/`IDepartmentService`
inject edir — `IUnitOfWork` YOXDUR (Clean Architecture qaydası; `EzamiyyetController`-in
özü bunu pozur — departament siyahısını birbaşa `IUnitOfWork`-dan çəkir — amma
bu, KÖHNƏ kodun miras qalmış İNKONSİSTENTLİYİDİR, YENİ kod onu TƏKRARLAMADI,
əvəzinə mövcud `IDepartmentService.HamisiniGetirAsync()` çağırıldı).

**Görüntü — `Areas/HR/Views/Gorush/{Index,Detay}.cshtml` + `js/hr/hr-gorush.js`.**
`Index` Ezamiyyət İdarəetməsinin (`fn-card`/KPI/filtr/cədvəl) eyni naxışını
təkrarlayır, amma modal YOXDUR — "Bax" düyməsi tam səhifəyə (`Detay`) aparır
(read-only olduğu üçün modal-da redaktə düyməsi saxlamaq mənasız olardı).
`Detay` isə `/User/Gorush/Detay.cshtml`-in HAZIR, artıq gözəl dizaynlı
hero/sidebar markup-unu (`.td-*` siniflər) TƏKRARLADI — yeni CSS yazılmadı,
`gorush.css` (`.grsh-status`/`.grsh-btn`) elə həmişəki kimi linkləndi.

⚠️ **TAPILAN, DÜZƏLDİLƏN YAN BUG — User-in ÖZ `Gorush/Detay.cshtml`-i
`tsk-detay-grid` sinfini İŞLƏDİR, AMMA onu DAŞIYAN `tapshiriq.css`-i HEÇ VAXT
LİNKLƏMƏYİB** (`@section Styles` yalnız `gorush.css`-i yükləyir) — yəni həmin
səhifə indiyə qədər 2-sütunlu grid-SİZ, tək sütunlu görünüb, kodda heç bir
xəta vermədən (sadəcə `tsk-detay-grid` naməlum sinif kimi sükutla keçilir).
**YENİ HR səhifəmdə bu unudulma TƏKRARLANMASIN deyə `tapshiriq.css` AÇIQ
LİNKLƏNDİ** — HR-ın Görüşlər detalı indi düzgün 2-sütunlu grid-dədir.
**`User/Gorush/Detay.cshtml`-in ÖZÜ TOXUNULMADI** (bu sessiyanın əhatəsi
xaricində — tapılan, amma HƏLƏ düzəldilməyən bir bug kimi qeyd olunur,
istifadəçi ayrıca tələb etsə düzəldilə bilər).

**Sidebar:** `_UserLayout.cshtml`-də "Ezamiyyət İzləmə"nin DƏRHAL ardınca,
eyni "MƏZUNİYYƏT" qrupunda (HR naviqasiyası bu paylaşılan layout-dadır —
`_HRLayout.cshtml`-də DEYİL, bax "`_ViewStart.cshtml` bütün HR səhifələrini
`_UserLayout.cshtml`-ə bağlayır" qeydi yuxarıda) yeni "Görüşlər" sətri.

⚠️ **GenişLƏNDİRİLMƏ FİKRİ, EDİLMƏDİ:** "idarəetmə" sözü istifadəçinin öz
tələbində var idi, amma HR-ın görüşü LƏĞV/REDAKTƏ edə bilməsi AÇIQ
soruşulmadı — bu, icazə modelinin (`StatusYenileAsync`-in `TeshkilatciIsciId
!= isciId` şərti) HR/Admin üçün ayrıca bir qol tələb edərdi (Ezamiyyət-in
`RehberHrLegvEtAsync`-i kimi). İstifadəçi bunu AYRICA istəsə, həmin naxış
tətbiq oluna bilər — bu sessiyada YALNIZ baxış qurulub.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə/div balansı skriptlə, `node -c` ilə JS sintaksisi, interfeys↔
implementasiya↔DI↔controller↔view beş qatı əl ilə tutuşduruldu — `IDepartmentService`/
`IGorushService`-in hər ikisinin artıq `AddApplicationServices.cs`-də qeydiyyatdan
keçdiyi təsdiqləndi, yeni DI əlavəsi lazım olmadı). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real mühitdə yoxlamalıdır: sidebar-da "Görüşlər"
sətrinin göründüyünü, siyahının filtrlərlə (axtarış/status/departament/tarix)
düzgün işlədiyini, "Bax"a basanda detalın (gündəm, iştirakçı statusları,
təşkilatçı) düzgün göründüyünü — xüsusilə "Ağacəkmə aksiyası" kimi real bir
görüşü tapıb.

## GOAML — Risk/AML Şübhəli Əməliyyat Hesabatı (05.11.2026, İŞ BAŞLANĞICI)

İstifadəçi BMI-nin köhnə Oracle Forms "AYSEL/GOAML" ekranının FinNex-ə
köçürülməsini istədi — 15 sol-menyu tablı (Əməliyyatlar, Risk, Keçməyən
əməliyyat, Hesab risk, Risk Progress, Cash IN, Card To Card, 18% 4%, Setup,
SİYAHI, kassa >30, Təhlil, MMX, SCORING, Export to file) böyük bir sistem.
Bu, "goAML" (BMFX/FIU-lara şübhəli əməliyyat hesabatı üçün beynəlxalq standart)
anlayışına uyğundur.

**Qərar: bmi_finance-in DAXİLİNDƏ, ayrı bir Area kimi.** TV-slayd layihəsindən
(ayrıca repo — kənar internetdən əlçatan olmalı idi) FƏRQLİ olaraq, GOAML
TAM ƏKSİNƏ — daxili, həssas maliyyə-cinayət məlumatıdır, heç vaxt kənar
şəbəkədən əlçatan olmamalıdır. Üstəlik layihədə artıq güclü AML/Risk
infrastrukturu var (Oracle `IOracleService`, `RiskService`, "AML → Məlumat
Bazası" modulu) — ayrı layihə qursaq bunların hamısını təkrar qurmaq
lazım gələrdi.

### İcazə — Risk-in ÖZÜNDƏN QƏSDƏN AYRI

Kod araşdırılanda üzə çıxdı: Risk Area-nın 4 controller-inin (Dashboard, Aml,
EmeliyyatRiski, EmeliyyatRiskiParametrleri) heç birində rol/icazə şərti
YOXDUR — sadəcə `[Authorize]`, yəni **istənilən giriş etmiş işçi** hazırda
`/Risk/*`-ə daxil ola bilər. "Mövcud Risk icazəsi ilə idarə edək" tələbi
əslində "bütün şirkətə aç" demək olardı — GOAML-in daşıdığı məlumat (PİN,
şəxsiyyət sənədi, tam ünvan, telefon) buna uyğun deyil. İstifadəçi qərarı:
**yeni, dar bir icazə açarı** — `IcazeKodlari.GoamlBax` ("goaml_bax") —
yarat, Risk-in özünə TOXUNMA (ayrıca, gələcək qərardır).

**İstifadə olunan mexanizm — layihədə ARTIQ hazır, bir sətirlik sistem**
(`FinNex.UI/Filters/IcazeAttribute.cs`, Mail/Avtopark-ın da işlətdiyi):

```csharp
[Authorize]
[Icaze(IcazeKodlari.GoamlBax)]
public class DashboardController : Controller
```

`IcazeAttribute.OnAuthorizationAsync` daxilən: Admin HƏMİŞƏ keçir (əlavə kod
lazım deyil), `ElaveRol` veriləndə o rol da keçir (GOAML-də BU PARAMETR
QƏSDƏN BOŞ qoyuldu — istifadəçi qərarı: "yalnız Admin avtomatik keçsin,
Rəhbər daxil hər kəs açıq icazə alsın"), qalanı `IUserPermissionService.
HasPermissionAsync(userId, kod)`-a baxır. Bu, Mühasibatlığın köhnə, əl ilə
yazılmış `IcazeVarAsync()` üsulundan DAHA TƏMİZDİR — Avtopark/Mail bunu
artıq işlədir, GOAML üçüncü istifadəçisidir.

### Struktur (bu mərhələdə yalnız QABIQ — real data/forma YOXDUR)

- `FinNex.Domain/IcazeKodlari.cs` — `GoamlBax = "goaml_bax"` sabiti əlavə edildi.
- `FinNex.UI/Areas/Goaml/Controllers/DashboardController.cs` (yeni) — `[Area("Goaml")]
  [Authorize] [Icaze(IcazeKodlari.GoamlBax)]`, tək `Index()` action.
- `FinNex.UI/Areas/Goaml/Views/_ViewStart.cshtml` + `_ViewImports.cshtml` —
  Kassa/Muhasibat-ın EYNİ naxışı (`Layout = "~/Areas/User/Views/Shared/_UserLayout.cshtml"`).
  **Ayrıca Area-xüsusi layout faylı YOXDUR** — layihədə HEÇ bir Area-nın yoxdur,
  hamısı TƏK `_UserLayout.cshtml`-in daxilindəki `DetectActiveModule()` switch-casei
  ilə fərqli sidebar məzmunu göstərir.
- `FinNex.UI/Areas/Goaml/Views/Dashboard/Index.cshtml` (yeni) — sadə qarşılama
  kartı, 15 tabın statik siyahısı (Hesabat kataloqunun "statik siyahı, DB-də
  saxlanmır" naxışı ilə eyni) — "Hazır"/"Hələ yoxdur" ikonu ilə, ekran-ekran
  təsdiqləndikcə `Hazir = true`-ya keçiriləcək.

### Naviqasiya

İstifadəçi qərarı: **top-nav-da "Avtopark"ın yanında, görünən/gizlənən ayrıca
düymə** — "Departamentlər" dropdown-unda DEYİL (orada hər sətir icazəsiz də
görünür, GOAML isə dar bir komandaya aiddir). `hasGoaml` dəyişəni (Avtopark-ın
`hasAvtoparkIdare`-i ilə EYNİ naxış) `@if (hasGoaml)` ilə düyməni gizlədir —
icazəsi olmayan işçi heç görmür.

`_UserLayout.cshtml`-də dörd dəyişiklik:
1. `DetectActiveModule()` — `if (currentArea == "Goaml") return "goaml";`
2. `hasGoaml` hesablanması (Admin + `GoamlBax` icazəsi).
3. Top-nav-da "Avtopark"dan dərhal sonra, `@if (hasGoaml)` ilə qorunan "GOAML" düyməsi.
4. Sidebar-a `case "goaml":` bloku — hələlik yalnız "Əsas" sətri (Dashboard/Index-ə),
   qalan tablar gəldikcə bura əlavə olunacaq.

### Admin tərəfi

`Admin/SistemIcaze/Index.cshtml`-dəki "turnkey" naxış (Mühasibatlığın
`muhasibat_dashboard_bax`-ı özbaşına yaratdığı kimi) GOAML üçün də tətbiq
edildi — `SistemIcazeController.Index()` açılanda `"goaml_bax"` icazəsi
yoxdursa avtomatik yaradılır. Admin əl ilə "Yeni icazə" formasını doldurmaq
MƏCBURİ DEYİL — Sistem İcazələri səhifəsini açan kimi "GOAML" sətri artıq
görünür, Admin ora kimin girə biləcəyini seçə bilər (checkbox siyahı,
mövcud `Idare`/`Saxla` action-ları — kod YAZILMADI, hazır mexanizm təkrar
işlədildi).

### Açıq qalan (qəsdən bu mərhələdə edilmədi)

- **"Əməliyyatlar" tabının ÖZÜ (əsas forma, 11+ sahə qrupu — Nümayəndə,
  Hesab imza hüququ olan şəxs, Hüquqi şəxs, Alan fiziki şəxs, Banka aid
  olmayan hüquqi şəxs) HƏLƏ QURULMADI** — istifadəçi ilə açıq qalan sual:
  data (Debet/Kredit/məbləğ) operator tərəfindən ƏL İLƏ yazılır, yoxsa
  Oracle BMI-dən (bizim real əməliyyat cədvəllərimizdən) AVTOMATİK gəlir?
  Bu, entity dizaynını kökündən dəyişir (standalone forma vs. mövcud
  əməliyyata "əlavə KYC qatı") — cavablanmadan struktur qurulmayacaq.
- **Qalan 14 tab** (Risk, Keçməyən əməliyyat, Hesab risk, Risk Progress,
  Cash IN, Card To Card, 18% 4%, Setup, SİYAHI, kassa >30, Təhlil, MMX,
  SCORING, Export to file) — istifadəçi ekran-ekran göndərəcək, hər biri
  təsdiqləndikcə planlaşdırılacaq.
- **Entity/DB sxemi HƏLƏ YOXDUR** — yalnız giriş nöqtəsi (icazə + boş səhifə)
  quruldu. Migration YAZILMADI, çünki hələ hansı sahələrin lazım olduğu
  (xüsusilə yuxarıdakı "data haradan gəlir" sualı) bilinmir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə, `IcazeAttribute`/`IcazeKodlari`-nin
mövcud çağırış nümunələri — Mail/Avtopark — ilə sahə-sahə tutuşduruldu).
İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real mühitdə
yoxlamalıdır: Admin olaraq top-nav-da "GOAML" düyməsinin göründüyünü,
klikləyəndə boş (15 tablı) qarşılama səhifəsinin açıldığını; Admin → Sistem
İcazələri-də "GOAML" sətrinin avtomatik göründüyünü; icazəsi olmayan adi
işçi ilə girəndə düymənin HEÇ görünmədiyini (və birbaşa URL yazılsa 403
alındığını).

✅ **TƏSDİQLƏNDİ (05.11.2026, eyni gün).** İstifadəçi real mühitdə (Admin
hesabı) yoxladı — ilk cəhddə düymə görünmürdü, səbəb kodda DEYİL, yalnız
`git pull` edilməmiş olması idi (Package Manager Console-da səhvən `add pull`
yazılmışdı, `git pull` YOX). Düzgün pull + **Rebuild All**-dan sonra: top-nav-da
"GOAML" düyməsi, sidebar-da "Əsas" sətri, 15 tablıq siyahı (hamısı "Hazır
deyil" saat-ikonu ilə) tam gözlənildiyi kimi göründü — giriş nöqtəsi uçdan-uca
işlək təsdiqləndi.

**Qayda:** "kod düzgün görünür, amma istifadəçi gözlədiyini görmür" halında —
xüsusən bu layihədə `dotnet build` BU mühitdə mümkün olmadığı üçün — İLK
yoxlanacaq şey HƏMİŞƏ "istifadəçi real mühitdə son commiti çəkib (pull) və
yenidən qurubmu (Rebuild All)" sualıdır, kodun özündə səhv axtarmazdan ƏVVƏL.

## GOAML — "Əməliyyatlar" Tabı TAMAMLANDI: Entity+Servis+Ekranlar (06.11.2026)

Yuxarıdakı "açıq qalan" sualın cavabı bu sessiyada tapıldı: **canlı Oracle
araşdırması** (`v$sql` ilə AYSEL-in real "Təsdiq+Save+Update New" zəncirini
izləyərək — bax aşağıdakı "GOAML Arxitekturası — AYSEL-in Üç Addımlı Zənciri"
bölməsi) göstərdi ki, namizəd əməliyyat **Oracle-dan (`DOC_DNA_ALL_TAB` +
`DOC_DNA_ALL_RISK`, `DATE_OPER+FILIALNUMBER+RECNUM` açarı ilə JOIN) OXUNUR**,
operator KYC-ni doldurub təsdiqləyəndə isə **bizim ÖZ SQL Server bazamıza**
yazılır — Oracle-un `GOAML_CLIENTS`-inin güzgüsü, amma FinNex-in daxilində.

**Struktur** (hamısı bu sessiyada quruldu): `GoamlEmeliyyat` entity
(`FinNex.Domain/Entities/Goaml/`, Oracle-un 124 sütununu BİR-BİR güzgüləyir,
hər sahənin şərhində əsl Oracle adı saxlanılır), migration
`20261106090000_GoamlEmeliyyat.cs` (`InsertData` yoxdur), `GoamlTipi` (13
dəyər, Oracle `TIP_GOAML.TIP`-dən RƏQƏMLƏRİ EYNİ saxlayaraq köçürülüb) və
`GoamlHesabatNovu` (16 dəyər, "Məlumatın növü") enum-ları, `IGoamlService`/
`GoamlService`, `EmeliyyatlarController` (`[Icaze(IcazeKodlari.GoamlBax)]`).

**Namizəd sorğusu `docs/sql/goaml/90_Goaml_OracleSorgular.sql`-dədir**
(`OracleSorgular.SorguAdi='GOAML_EMELIYYAT_NAMIZED'`) — AYSEL-in
`P_DOC_DNA_GOAML` prosedurundan `v$sql` ilə çıxarılan ƏSL biznes qaydalarını
daşıyır (GL texniki hesabları xaric etmə, `BALCH_NET` netting-cütlərini xaric
etmə). ⚠️ **Fayl "FAZA 1 (paralel dövr)" üçündür** — AYSEL hələ işlədiyi
müddətcə `DOC_DNA_ALL_TAB`-ı O doldurur, biz YALNIZ oxuyuruq; AYSEL
dayandırılandan sonra sorğu `odb.arh_dd`-dən yenidən qurulmalıdır (edilmədi).

**Eyni namizəd İKİNCİ dəfə təsdiqlənə bilməz** — unikal olmayan, amma
sorğulanan indeks (`IX_GoamlEmeliyyatlar_Candidate`,
`CandidateFilialNumber+CandidateDateOper+CandidateRecNum`), `TesdiqleAsync`
yazmazdan ƏVVƏL `MovcuddurmuAsync` ilə yoxlayır. Axtarış nəticəsində də hər
sətrə `ArtiqTesdiqlenib` bayrağı qoyulur (eyni 3-hissəli açarla) ki, operator
artıq təsdiqlənmiş sətri siyahıda görsün, təkrar klikləməsin.

### Üç ekran (bu sessiyada yazıldı)

1. **`Index.cshtml`** — tarix aralığı + minimum məbləğ + VÖEN/hesab filtri,
   **AJAX POST** (`Axtar` endpoint) ilə nəticə cədvəli JS-dən doldurulur
   (server-render YOX — sorğu hər dəfə Oracle-a canlı gedir). Artıq
   təsdiqlənmiş sətirdə "Təsdiqlənib" bədgi, əks halda "Təsdiqlə" düyməsi
   (namizədin bütün açar+ilkin sahələrini query-string ilə `Tesdiq` GET-inə
   ötürür — controller-in mövcud parametr siyahısı ilə BİR-BİR tutuşduruldu).
2. **`Tesdiq.cshtml`** — ~130 sahəlik KYC forması. **Tip + Məlumatın növü**
   (məcburi, servis bunları yoxlayır) ayrıca, vurğulanmış kartda; qalan 5 blok
   (Nümayəndənin/_CON, Hesab imza hüququ olan şəxsin/_SIG, Hüquqi şəxsin,
   Alan fiziki şəxsin/_TO fiziki, Banka aid olmayan hüquqi şəxsin/_TO hüquqi)
   `<details>`/`<summary>`-lə qatlanan bloklardadır ("İşçiyə Tapşır" bölməsinin
   eyni JS-siz naxışı) — entity-nin öz baş şərhindəki 5-blok xəritəsi BİRƏ-BİR
   təkrarlanıb. Entity şərhinin "tam bloka aid edilməməyib" dediyi sahələr
   (Gender/Snn/Occupation/SenderId*/RecId* və s.) AYRICA "Əlavə sahələr"
   blokunda, açıq xəbərdarlıqla göstərilir — gizlədilmir.
3. **`Siyahi.cshtml`** — təsdiqlənmiş qeydlərin sadə cədvəli (`Tip.Etiket()`/
   `ReportCode.Kod()` ilə insan-oxunan görüntü).

**Enum görüntü mətni DOMAIN QATINDA, Razor-da DEYİL** — `GoamlTipiExtensions.
Etiket()` və `GoamlHesabatNovuExtensions.Aciqlama()` (`GoamlEnums.cs`-ə bu
sessiyada əlavə edildi) — "bir kəmiyyətə bir yazıcı" prinsipinin əksi:
BİR mənbə, BİR NEÇƏ OXUYUCU (Tesdiq-in dropdown-u, Siyahi-nin cədvəli,
gələcəkdə hər hansı başqa ekran) — etiket mətni Razor-da səpələnməsin deyə.

⚠️ **Decimal input-larda InvariantCulture tələsi ƏVVƏLCƏDƏN tutuldu.**
`asp-for` + `type="number"` decimal sahələrdə (`SummaVInval`, `SummaVNacval`,
`KursValuti`, `AmountLocal`, `ForeignAmount`, `ForeignExchangeRate`) az-AZ
mədəniyyəti ilə vergüllü render edərdi — CLAUDE.md-dəki "Dəyər Sahəsi — step
və decimal(p,s)" tələsinin elə özüdür (`<input type="number">` yalnız NÖQTƏ
qəbul edir). Bu altı sahə `asp-for`-dan çıxarılıb, `value="@Model.X?.
ToString(CultureInfo.InvariantCulture)"` ilə əl ilə yazıldı — `KodValuti`
(`int?`) toxunulmadı, tam ədəddə ayırıcı riski yoxdur.

⚠️ **Açıq qalan — `DetalGetirAsync(int id)` servisdə VAR, controller-dən
ÇAĞIRILMIR.** `Siyahi.cshtml` yalnız siyahı göstərir, tək qeydin tam detalına
baxan "Bax" action/view HƏLƏ yazılmayıb — gələcəkdə asanlıqla əlavə oluna
bilər (servis metodu hazırdır).

⚠️ **Qalan 14 tab (Risk, Keçməyən əməliyyat, Hesab risk, Risk Progress, Cash
IN, Card To Card, 18% 4%, Setup, SİYAHI, kassa >30, Təhlil, MMX, SCORING,
Export to file) HƏLƏ QURULMAYIB** — istifadəçi ekran-ekran göndərəcək.

### GOAML Arxitekturası — AYSEL-in Üç Addımlı Zənciri (canlı Oracle araşdırması, 06.11.2026)

Köhnə BMI Oracle Forms "AYSEL/GOAML" modulunun **Əməliyyatlar** VƏ **Risk**
tablarının necə işlədiyi `v$sql` ilə CANLI izlənərək tapıldı (heç bir
fərziyyə yox — hər addım real Oracle sorğusu ilə sübut olundu):

1. **Təsdiq** — operator ekranda namizədin KYC sahələrini doldurur.
2. **Save** — `ODB.GOAML_CLIENTS`-ə **124-sütunlu parametrli INSERT**
   (`INSERT INTO odb.GOAML_CLIENTS(DEBET,FILIALNUMBER,KREDIT,...)
   VALUES(:1,:2,...)`) yazılır — bu, YALNIZ Save-dən sonra baş verir (Təsdiq
   təkbaşına COMMIT ETMİR, `count(*)` ilə sübut olundu).
3. **Update New** — **150-dən çox AYRI UPDATE əmrindən ibarət nəhəng kaskad**,
   HAMISI eyni saniyədə, HƏR BİRİ `WHERE DATE_OPER BETWEEN :tarix1 AND :tarix2
   AND VIP_NET_DA=1` şərti ilə — yəni TƏK sətri yox, bütün dövrdəki namizədləri
   bir anda emal edir: `TIP`-i hesab prefiksindən təyin edir, `REPORT_CODE`-u
   (`STR`/`PEP`/`BLR`) qara siyahı/PEP/sanksiya cədvəllərinə baxıb avtomatik
   qoyur, KYC-ni (ad/soyad/ünvan/sənəd) `V_FIZIKI`/`VIEW_HUQUQI_SHEXS_ALL`/
   `LICSCH_ALL_2` kimi ƏSAS bank reyestrlərindən köçürür.

**Risk VƏ Əməliyyatlar EYNİ `GOAML_CLIENTS`-ə, AMMA FƏRQLİ sütun sırası ilə
yazır** (iki ayrı literal INSERT mətni `v$sql`-də tapıldı — Risk-in
`foreign_currency_code_from` sütunu yoxdur, Əməliyyatlar-ınkı var) — demək
AYSEL-in bu iki tabının HƏR BİRİNİN öz yazma yolu var, ortaq yalnız hədəf
cədvəldir. `GOAML_DOCNVA` adlı YEGANƏ DB-saxlanmış prosedur (`all_source`
axtarışı ilə tapıldı) isə bunların heç biri DEYİL — o, `doc_dna_all_risk`-dən
≥20.000 AZN əməliyyatları "skelet" (12 sütun, `VIP_NET_DA=1`) olaraq
`GOAML_CLIENTS`-ə ƏVVƏLCƏDƏN doldurur; nə `DBMS_SCHEDULER`-də, nə `DBMS_JOB`-da
planlaşdırılmayıb — əl ilə, bir düymədən çağırılır.

**`GOAML_CLIENTS`-in HƏQİQİ məqsədi — `XML_GOAML_*_NEW` prosedur ailəsinin
(hər `TIP` dəyəri üçün AYRI, ~13 prosedur) oxuduğu mənbədir.** Bir prosedurun
(`XML_GOAML_FIZIKI_FIZIKIYA_new`) TAM mətni oxunub təsdiqləndi:
`xmlelement(...)` ilə goAML-in (UNODC-nin BEYNƏLXALQ FIU hesabat standartının)
RƏSMİ XML teqlərini (`<t_conductor>`, `<reporting_person>`, `<identification>`,
`<source_of_wealth>` və s.) qurur, `UTL_FILE` ilə `D:\GOAML\XML\...xml`
yoluna REAL fayl yazır. Ekrandakı "Səbəb"/"ACTION" xanaları da birbaşa bu
XML-in `<reason>`/`<action>` teqlərinə düşür.

**"Bir-bir Təsdiq" olmasının səbəbi hesabat FORMATI DEYİL** — XML/kaskad
TOPLU (bütün dövr üçün bir anda) işləyir, "bir-bir" olmasının səbəbi İNSAN AML
nəzarətidir: hər namizədin HƏQİQƏTƏN şübhəli olub-olmadığını, Tip-ini,
Səbəbini YALNIZ analitik qiymətləndirə bilər — bu, qanunvericilik tələbidir.

**FinNex-ə köçürmə qərarı:** Oracle-a YAZI qadağan olduğu üçün (layihənin kök
qaydası) bütün "yazma" tərəfi (Təsdiq+Save+Update New kaskadı+XML generasiyası)
FinNex-in ÖZ SQL Server bazasında TƏKRARLANMALIDIR, namizəd oxuması isə Oracle-
da QALIR. "150+ UPDATE" kaskadı 6 məntiqi qrupa bölünür (TIP təyini, REPORT_CODE/
Səbəb, KYC-nin bank reyestrlərindən doldurulması — ən böyük qrup, valyuta/
məzənnə, TRANSMODE_CODE, təmizlik/defolt dəyərlər) — İLK MƏRHƏLƏDƏ (bu sessiya)
YALNIZ struktur (entity+Tesdiq forması) quruldu, kaskadın ÖZÜ HƏLƏ KÖÇÜRÜLMƏYİB
— operator hazırda TIP/REPORT_CODE/KYC-ni ƏL İLƏ seçir, Oracle-dakı kimi
avtomatik doldurma YOXDUR. Gələcək mərhələdə (istifadəçi "yavaş-yavaş" dedi)
bu avtomatikanı C#/SQL Server-də təkrarlamaq planlaşdırılır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə hər 5 yeni/dəyişən faylda,
`asp-for`/manual name="" uyğunluğu DTO sahə adları ilə bir-bir tutuşduruldu,
Index.cshtml-in JS-dəki camelCase sahə adları `Json()` helper-inin default
casing-i ilə təsdiqləndi). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra real ssenari ilə yoxlamalıdır: "Əməliyyatlar"da bir tarix aralığı axtar
→ nəticə cədvəlinin gəldiyini, "Təsdiqlə"yə basıb KYC formasını (bütün 5
blok + məcburi Tip/Məlumat növü) dolduraraq göndərdiyini, "Təsdiqlənmiş
siyahı"da yeni sətrin göründüyünü, eyni namizədi İKİNCİ dəfə açmağa cəhd
edəndə "Təsdiqlənib" bədgisinin göründüyünü (təkrar yazıla bilmədiyini).

## GOAML — Təsdiq Forması 130 Sahədən 6 Qrupa Endirildi (06.11.2026, KRİTİK)

İstifadəçi real AML operatorundan soruşub cavabı gətirdi: *"burda olanlardan
hansılarını sən işlədirsənsə onu göstərək, bir şeyi ki işlənmirsə boş yerə
niyə göstərək. onsuzda json ətraflı şəkildə hazırlanacaq."*

**Bu, AYSEL-in real iş axınını tam aydınlaşdırdı — mənim əvvəlki 130-sahəlik
Təsdiq formam SƏHV fərziyyə üzərində qurulmuşdu** ("operator hər sahəni əl
ilə doldurur"). Həqiqi axın:

1. Operator YALNIZ 6 qrupu doldurub **Təsdiqlə**yir (aşağıda).
2. AYSEL-in ÖZÜ (06.11.2026-dakı əvvəlki araşdırmada tapılan "Update New"
   kaskadı — 150+ UPDATE, `V_FIZIKI`/`VIEW_HUQUQI_SHEXS_ALL`/`LICSCH_ALL_2`
   bank reyestrlərindən) **qalan BÜTÜN sahələri AVTOMATİK doldurur**.
3. Proqram əmin olmadığı yerləri **boş qoyur** — operator bunları ƏL İLƏ
   düzəldir (yaxud səhv olanda düzəldib yenidən təsdiqləyir).
4. Son olaraq qeydlər **Tip üzrə qruplaşdırılıb** JSON/XML-ə ixrac olunur
   (bu, əvvəlki sessiyada tapılan `XML_GOAML_*_NEW` prosedur ailəsidir).

**İşçinin verdiyi 6 qrup** (`Tesdiq.cshtml`-də SAXLANILAN YEGANƏ sahələr):
1. Əməliyyat tarixi (`DateOper`)
2. Əməl növü (`Tip`)
3. Məlumat növü (`ReportCode`)
4. Nümayəndənin VƏ Hesab imza hüququ olan şəxsin (Blok 1 + Blok 2, tam)
5. Vəzifə + İş yerinin adı (Blok 2-nin daxilində, `SigVezife`/`SigIsYeriAdi`)
6. Fiziki/Sahibkar/Hüquqi checkbox **+ VÖEN/Ölkə/ADI/Bankın adı** (istifadəçi
   açıq təsdiqlədi: tək checkbox mənasız qalardı, kimin olduğunu bilmək
   üçün bu 4 sahə lazımdır)

**Çıxarılan (~100 sahə) — FORMADAN, DB-DƏN YOX:** "Hüquqi şəxsin (göndərən
tərəf)" bloku tam, "Alan fiziki şəxsin"in tam şəxsi detalı (Doğulduğu tarix/
yer, Vətəndaşlıq, Telefon, Ünvan, Sənəd məlumatları), "Banka aid olmayan..."un
SWIFT/Hesab/Əlaqə növü sahələri, "Əlavə sahələr" bloku tam, "Əməliyyatın özü"
kartının 24 digər sahəsi (Valyuta kodu, Məzənnə, Manat ekvivalenti, Xarici
valyutada məbləğ, Qeydlər, ACTION, Səbəb, URL və s.).

⚠️ **`GoamlEmeliyyat` entity, `GoamlTesdiqDto`, migration — HEÇ BİRİNƏ
TOXUNULMADI.** Yalnız `Tesdiq.cshtml` (görüntü qatı) dəyişdi. Səbəb: istifadəçinin
öz sözü — "onsuzda json ətraflı şəkildə hazırlanacaq" — yəni gələcəkdə AYSEL-in
"Update New" kaskadının ekvivalenti qurulanda (hələ EDİLMƏYİB, "Export to file"
tabı ilə birlikdə gələcək iş kimi qalır) bu sahələr bank reyestrlərindən
avtomatik doldurulacaq — indi silinsəydi, həmin iş üçün sxemi yenidən qurmaq
lazım gələrdi.

**Qayda:** köhnə sistemdən bir ekranı "tam, hərfi" köçürmək (hər görünən sahəni
formaya əlavə etmək) TƏHLÜKƏLİDİR — AYSEL kimi köhnə Oracle Forms ekranları
onilliklər boyu YIĞILMIŞ sahələr daşıya bilər, onların çoxu HEÇ VAXT əl ilə
doldurulmayıb (proqramın öz avtomatikası doldurub). Köçürmədən ƏVVƏL real
istifadəçidən **"bunlardan hansını SƏN özün yazırsan?"** soruşmaq — "ekranda
nə var" sualından tamamilə fərqli, daha doğru bir sualdır.

⚠️ **AÇIQ QALAN İŞ** — AYSEL-in "Update New" kaskadının FinNex-dəki ekvivalenti
(bank reyestrlərindən avtomatik doldurma) hələ qurulmayıb. Operator hazırda
Təsdiqlədikdən sonra qalan ~100 sahə `GoamlEmeliyyat`-da `NULL` qalır — gələcək
iş budur ki, Təsdiq anında (və ya ayrıca bir "Doldur" düyməsi ilə) bu sahələr
VÖEN/FİN/hesab üzrə uyğun Oracle reyestrlərindən (analoji: `regnom`, `licsch`,
`docfio` — AML "Məlumat Bazası" modulunda artıq oxunan cədvəllər) axtarılıb
avtomatik doldurulsun, operator yalnız YOXLASIN/DÜZƏLTSİN.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı skriptlə). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra real ssenari ilə yoxlamalıdır: "Təsdiqlə"yə basanda indi YALNIZ 3 blokun
(Təsnifat + Nümayəndə + İmza hüququ olan şəxs + Banka aid olmayan müştəri)
göründüyünü, formanın köhnə 5-blok/130-sahə halından qat-qat qısa olduğunu.

## GOAML — "Geri" Qayıdışda Axtarış İtirdi + Namizəddən Bilinən Ad SƏSSİZCƏ İtirilirdi (06.11.2026, KRİTİK)

İstifadəçi iki konkret qüsur bildirdi: (1) «Təsdiqlə»də "Geri"yə basanda
Əməliyyatlar siyahısı BOŞ açılır, filtri/nəticəni yenidən axtarmaq lazım gəlir;
(2) "Təsdiqlə"yə girəndə "Nümayəndənin" blokundakı sahələr HƏR DƏFƏ boşdur —
sual verdi ki, bu belə QALACAQ?

**(1) Kök səbəb — `EmeliyyatlarController.Index()` HƏR GET-də sabit default
qaytarır** (`BasTarix = DateTime.Today, SonTarix = DateTime.Today`), axtarış
isə TAMAMİLƏ AJAX-dır (`POST /Axtar`, nəticə yalnız DOM-a yazılır, heç yerə
saxlanmır). `Tesdiq.cshtml`-dəki "Geri"/"Ləğv et" linkləri sadə `<a asp-action=
"Index">`-dir (tam GET naviqasiya) — geri qayıdanda `Index()` yenidən sıfırdan
işə düşür, əvvəlki axtarış izsiz itir.

**Düzəliş — `sessionStorage`, Mail modulundakı `gmIzlemeFilter` ilə EYNİ
prinsip (localStorage YOX — bu, giriş sessiyaları arasında qalmamalıdır, tab
bağlananda təbii təmizlənsin):** hər uğurlu "Axtar" filtri
`sessionStorage['gmlSonAxtaris']`-ə yazılır; səhifə açılanda (`gmlBerpaEt`
IIFE) saxlanmış filtr varsa, xanalara geri yazılır VƏ **avtomatik yenidən
axtarılır** (`gmlAxtar()`, köhnə `submit` handler-indən bir funksiyaya çıxarıldı,
`gmlSonAcar` köhnəlmiş-cavab qoruyucusu TOXUNULMADI). Keşlənmiş NƏTİCƏ (sətirlər)
YOX, yalnız FİLTR saxlanılır — geri qayıdanda sorğu YENİDƏN Oracle-a gedir ki,
indicə təsdiqlənən sətir "Təsdiqlənib" bədgisi ilə TƏZƏ görünsün, köhnə DOM
keşi ilə yalançı "hələ təsdiqlənməyib" göstərməsin.

**(2) Cavab — BƏLİ, hələlik həmişə boş gələcək, AMMA bir hissəsi SƏSSİZCƏ
ATILIRDI, bu HİSSƏSİ düzəldi.** `Tesdiq(int filialNumber, ...)` GET action-ı
namizəddən gələn hesab sahibi adlarını (`nameLicschDe`/`nameLicschKre` —
axtarış cədvəlindəki "Debet sahibi"/"Kredit sahibi" sütunları) `GoamlTesdiqDto.
NameUr`/`ToAdi`-ə yazırdı — AMMA bu iki sahə 06.11.2026-dakı "130→6 qrup"
trimm-indən sonra formada HEÇ BİR `<input>`-ə bağlı DEYİLDİ: dəyər DTO-da var
idi, ekranda HEÇ GÖRÜNMÜRDÜ. Bu, "Şərtli Render Olunan Form Sahəsi" bölməsindəki
tələnin güzgüsüdür — fərq orada POST-da itirdi, burada isə hələ GÖSTƏRİLMƏDƏN
(heç POST-a çatmadan) itirdi.

**Düzəliş — `Tesdiq.cshtml`-ə yeni, SIRF OXUNUŞ `gml-info-strip`** (Tarix,
Debet, Debet sahibi, Kredit, Kredit sahibi, Məbləğ, Qeyd) — heç bir `name`/
`<input>` daşımır, formaya GÖNDƏRİLMİR, YALNIZ operatora "sistem artıq bunları
bilir" kontekstini verir. Şəxsi KYC sahələri (ad/soyad/ünvan/sənəd —
Nümayəndə/İmza hüququ/Banka aid olmayan müştərinin "Adı") isə **HƏQİQƏTƏN
hələ də həmişə boş gələcək** — bu, AYSEL-in "Update New" kaskadının (bax yuxarı
"AÇIQ QALAN İŞ") hələ qurulmaması deməkdir, YANLIŞLIQ DEYİL, gözlənilən,
sənədləşmiş vəziyyətdir.

**(3) Üçüncü sual — AYSEL "Təsdiq et" edəndə özü nə tapdığını göstərir, "o
selectləri" necə tapaq?** Əvvəlki sessiyada AYSEL-in "Update New" kaskadının
(150+ UPDATE, bax yuxarı "GOAML Arxitekturası") INSERT/UPDATE mətni `v$sql`
ilə (canlı Oracle SQL keşindən) tutulmuşdu — eyni ÜSUL indi "Təsdiq et" anında
işə düşən reyestr-SELECT-ləri tapmaq üçün də tətbiq edilə bilər. Yeni
**`docs/sql/goaml/92_Goaml_Avtomatik_Doldurma_Diaqnostikasi.sql`** yazıldı:
A hissəsi son 10 dəqiqədə `V_FIZIKI`/`HUQUQI_SHEXS`/`LICSCH_ALL`/`REGNOM`/
`DOCFIO`/`PINCODE`/`GOAML_CLIENTS` açar sözlərinə uyğun gələn bütün `v$sql`
sətirlərini (schema-ya görə süzmədən — AYSEL-in hansı hesabla bağlandığı
bilinmir) göstərir; B hissəsi isə `v$sql_bind_capture`-la DƏQIQ bir FİN/VÖEN/
hesab dəyərini bağlı parametr kimi oxuyan sorğu(lar)ı tapır (`SQL_TEXT`-də
`:B1` kimi yer tutucu olsa belə işləyir). İstifadəçi skripti AYSEL-də bir
namizədi "Təsdiq et"+Save edəndən DƏRHAL sonra (keş tez köçürülür) PL/SQL
Developer-də, əvvəlki v$sql araşdırmasında işlətdiyi EYNİ hesabla icra edib
nəticəni göndərməlidir — bu sessiyada İCRA EDİLMƏDİ (Oracle-a bu mühitdən
bağlantı yoxdur).

**İKİNCİ DALĞA (06.11.2026, eyni gün) — skript real Oracle-a qarşı işlədildi,
iki şey üzə çıxdı, HƏR İKİSİ 92-ci fayla düzəldilib:**

1. **`ORA-00904: "S"."PROGRAM": invalid identifier`** — mənim öz səhvim:
   `PROGRAM` **`V$SESSION`**-un sütunudur, **`V$SQL`**-də BELƏ BİR SÜTUN
   YOXDUR (`V$SQL`-də yalnız `MODULE` var). A hissəsindən `s.program`
   çıxarıldı.
2. **B hissəsi (`v$sql_bind_capture`) 0 sətir qaytardı** — gözlənilən (sənəddə
   artıq qeyd olunmuşdu), Oracle bind dəyərini HƏR sorğuda saxlamır. Bundan
   sonra B **son çarə** sayılır, ƏSAS üsul İNDİ **C hissəsidir**: əvvəlcə
   `v$session`-dan AYSEL-in öz SID-i tapılır (öz PL/SQL Developer sessiyasını —
   `USERNAME='ODB'`, `PROGRAM='plsqldev.exe'` — çıxarıb qalanlardan), sonra
   `v$open_cursor` (HƏMİN sessiyanın öz açıq kursorları, bind capture-dan
   ASILI DEYİL, Oracle Forms kursorları aqressiv keşlədiyi üçün zəngin nəticə
   verir) `v$sql` ilə join edilib SID-ə görə süzülür — bu, "hansı sorğu bu
   konkret sessiyada icra olunub" sualına bind capture-un FLAKİ cavabından
   daha etibarlı cavab verir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı + JS sintaksisi skriptlə). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır: axtarış edib
"Təsdiqlə"yə girib "Geri" basanda ƏVVƏLKİ filtr+nəticənin AVTOMATİK (yenidən
Axtar basmadan) göründüyünü, "Təsdiqlə" formasında yeni "Debet sahibi"/"Kredit
sahibi" sətirinin göründüyünü.

**ÜÇÜNCÜ DALĞA (06.11.2026, eyni gün) — NƏTİCƏ: TƏSDİQLƏNDİ, "Təsdiq et" + Save
ÖZÜ reyestr-KYC doldurmur.** İstifadəçi `C` sorğusunu (SID=1014) İKİ dəfə işlətdi:

- **1-ci dəfə** — nəticə göndərildikdən SONRA üzə çıxdı ki, bu əslində "Təsdiq
  et"+Save edilmədən ÇƏKİLMİŞ bir AYSEL-in sadəcə AÇIQ olduğu an idi (operator
  heç bir əməliyyat etməmişdi) — mən bunu bilmədən "reyestr doldurma yoxdur"
  qənaətinə gəldim, istifadəçi düzəltdi: *"bunu etmeden atmisam sene"*.
- **2-ci dəfə** — bu dəfə HƏQİQƏTƏN "Təsdiq et" + Save edildikdən DƏRHAL sonra
  (16:34:05–16:34:34 aralığı, dublikat yoxlaması → 3 əsl sorğu → statistika)
  işlədildi. Görünən YALNIZ: `GOAML_CLIENTS` dublikat yoxlaması, `ODB.
  LICSCH_ALL_2` (hesab sahibinin adı+INN-i — bizim artıq namizəd siyahısında
  göstərdiyimiz "Debet/Kredit sahibi" ilə EYNİ, fərdi KYC DEYİL) və Save-dən
  sonra formanı yenidən göstərmək üçün `GOAML_CLIENTS`-in bütöv sətrinin geri-
  oxunması. **`V_FIZIKI`/`REGNOM`/`HUQUQI_SHEXS`/`LICSCH_ALL`/`DOCFIO` HEÇ
  YERDƏ yoxdur** — ikinci (bu dəfə həqiqətən doğru) testdə də.

**Qəti nəticə: "Təsdiq et" + Save AYSEL-in ÖZÜNDƏ reyestr-əsaslı şəxsi KYC
doldurma TRİGER ETMİR.** Bu, yuxarıdakı "AÇIQ QALAN İŞ" qeydini DƏQİQLƏŞDİRİR
(dəyişdirmir — o qeyd onsuz da "hələ qurulmayıb" deyirdi): avtomatik doldurma
("Update New" kaskadı) normal təsdiq axınının bir hissəsi DEYİL, tamamilə AYRI
(çox güman toplu/əl ilə tətiklənən) bir addımdır — əvvəlki sessiyada görülən,
AYSEL-in əsas pəncərəsi ilə yanaşı açılan konsol/applet pəncərəsi buna uyğun
gəlir, amma HƏLƏ TƏSDİQLƏNMƏYİB (işə salınıb SID-i tapılıb izlənməyib).

**İstifadəçi qərarı (06.11.2026): araşdırma BURADA DAYANDIRILDI** — "Update
New"-un öz tetikləyicisini tapmaq ARTIQ bu sessiyanın işi deyil, gələcək bir
mərhələdə (FinNex-in öz avtomatik-doldurma mexanizmini qurarkən) aparılacaq.
`92_Goaml_Avtomatik_Doldurma_Diaqnostikasi.sql` hazır qalır — o mərhələdə eyni
`C` üsulu (bu dəfə konsol/applet pəncərəsinin öz SID-i ilə) təkrar işlədilə bilər.


## GOAML — Əməliyyatlar Tabının "Bax" Detal Ekranı Bağlandı (09.11.2026)

İstifadəçinin sıra tələbi: Əməliyyatlar → Risk → Export to file əvvəlcə bitsin,
Setup sonraya qalsın. İlk addım olaraq "Əməliyyatlar"ın qalıq işi — `IGoamlService.
DetalGetirAsync(int id)` servisdə var idi, amma heç bir controller action/view-a
bağlı deyildi (bax yuxarı, "GOAML — 'Əməliyyatlar' Tabı TAMAMLANDI" bölməsindəki
"Açıq qalan" qeydi).

**Dəyişiklik — yeni, ayrıca DTO (`GoamlEmeliyyatDetalDto : GoamlTesdiqDto`).**
`DetalGetirAsync`-ın qaytardığı tip `GoamlTesdiqDto`-dan (yalnız KYC sahələri,
Tesdiq formunun POST-u üçün) `GoamlEmeliyyatDetalDto`-ya (+ `Id`/`Status`/
`TesdiqleyenAdi`/`YaradilmaTarixi`) dəyişdi — "Metod İmzası Dəyişikliyi" qaydasına
uyğun interfeys+implementasiya+controller eyni anda yeniləndi. `TesdiqleyenAdi`
`HamisiniGetirAsync`-ın işlətdiyi EYNİ naxışla (`Isci`-dən ad+soyad) doldurulur.

**`EmeliyyatlarController.Detal(int id)`** (yeni GET action) → `Siyahi.cshtml`-ə
hər sətrə "Bax" düyməsi əlavə edildi. **`Detal.cshtml`** (yeni, sırf oxunuş) —
`Tesdiq.cshtml`-in eyni 3 blok strukturunu (Nümayəndə/_CON, İmza hüququ/_SIG,
Banka aid olmayan müştəri) təkrarlayır, amma `<input>` əvəzinə sadə mətn
göstərir. Üstəlik bağlı başlayan **4-cü, "Əlavə sahələr" bloku** — formadan
06.11.2026-da çıxarılmış, hələ avtomatik doldurulmayan ~100 sahəni (Hüquqi
şəxsin göndərən tərəf, Alan fiziki şəxsin tam detalı, qalan _TO hüquqi sahələr,
əməliyyatın digər sahələri) göstərir — hazırda hamısı "—" (NULL) görünür, bu
GÖZLƏNİLƏN vəziyyətdir (AYSEL-in "Update New" ekvivalenti hələ qurulmayıb) —
gələcəkdə həmin mexanizm quruləndə bu ekran ƏLAVƏ DƏYİŞİKLİK TƏLƏB ETMİR,
sahələr avtomatik dolacaq.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə/div balansı skriptlə, DTO↔servis↔controller↔view sahə adları bir-bir
tutuşduruldu). İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra real
mühitdə yoxlamalıdır: "Təsdiqlənmiş əməliyyatlar" siyahısında "Bax" düyməsinin
tam KYC detalını (bütün 3 blok + yeni "Əlavə sahələr" bloku) düzgün göstərdiyini.


## GOAML — "Update New" Avtomatik Doldurma Mexanizmi Köçürüldü (07.11.2026)

05-06.11.2026-dakı araşdırma (bax yuxarı, "GOAML Arxitekturası" və sonrakı
bölmələr) AYSEL-in "UPDATE NEW" düyməsinin **49 UPDATE** əmrindən ibarət
kaskadını `v$sql`/`v$open_cursor` ilə TAM, kəsilməmiş mətnlə tapmışdı. Bu
sessiyada bunun FinNex ekvivalenti quruldu: **`GoamlService.AvtoDolduruAsync`**
— operator "Təsdiqlə" basan kimi, HƏMİN KONKRET qeyd üçün avtomatik işə düşür
(AYSEL-dəki kimi ayrıca "Update New" düyməsi YOXDUR — istifadəçi qərarı).

### 49 qaydadan 26-sı köçürüldü, 23-ü BİLƏRƏKDƏN ATLANDI

İlk planda "42 qayda" deyilmişdi (yalnız ID_VD-yə bağlı 7-ni çıxaraq), amma
kodu yazarkən İKİNCİ bir, daha mühüm fərq üzə çıxdı: **AYSEL-də TIP və
REPORT_CODE BOŞ başlayır, bu kaskad onları HESABLAYIR — FinNex-də isə əksinə,
hər ikisi operatorun "Təsdiqlə" formasında ƏL İLƏ seçdiyi, MƏCBURİ sahədir**
(`TesdiqleAsync`-in başındakı yoxlama: `if (dto.Tip == null) return Fail(...)`).
Bu 19 qaydanı (TIP təyini ×15 + REPORT_CODE ×4) avtomatik yenidən hesablamaq
operatorun artıq etdiyi AÇIQ seçimi SƏSSİZCƏ üstələyərdi — buna görə bunlar
ATLANDI, istifadəçiyə bu RAPORTUN özündə açıq bildirildi (əvvəlki "42" rəqəmi
TƏSHİH edildi).

| Kateqoriya | Say | Səbəb |
|---|---|---|
| **Köçürülüb** | **26** | aşağı bax |
| TIP təyini | 15 | operator ƏL İLƏ seçir (məcburi sahə) |
| REPORT_CODE (CTR ×3, ETR fallback) | 4 | eyni səbəb, "Məlumatın növü" də məcburidir |
| "No-op" (`SET X=0 WHERE X=0`) | 3 | Oracle-un ÖZÜNDƏ də heç nə etmir |
| `ID_VD`-yə bağlı (SWIFT/İnvaluta/Postupl/Nacval/Daxili İnvaluta) | 7 | bizim namizəd sorğumuzda bu açar YOXDUR |

### Köçürülən 26 qayda — qruplar üzrə

1. **VÖEN-in 10-cu rəqəminə görə Fiziki/Hüquqi/Sahibkar** (3 qayda:
   `58hwazj6jxj4q`/`5x3d8n7bth8aq`/`9gh5b10zb284n`) — **YALNIZ operator HEÇ
   BİR checkbox-ı (Fizik/Sahibkar/Hüquqi) işarələməyibsə** tətbiq olunur (bu
   3 sahə də `Tesdiq.cshtml`-də operatorun ƏL İLƏ işarələyə biləcəyi
   checkbox-lardır — TIP/ReportCode ilə EYNİ "operator seçimi toxunulmaz
   qalır" prinsipi). `Tesdiq.cshtml`-də plain `<input type="checkbox">`
   (hidden fallback-sız) olduğu üçün işarələnməyəndə dəyər `null` gəlir
   (`false` YOX) — "heç biri seçilməyib" ilə "Fiziki seçilib" fərqi bununla
   ayırd edilir.
2. **Sənəd seriya/nömrəsinin parçalanması** (CON/SIG/TO, 9 qayda) —
   `MMX_ID_SERIAL`-dan canlı oxunan siyahıya görə **ƏN UZUN uyğun gələn
   prefiksi** (1/2/3 simvol) seçir. AYSEL-in özündə bu 3 uzunluq AYRI,
   sırasız UPDATE idi (hamısı eyni şərtlə) — "ən uzun uyğunluq qalib"
   bunun TƏK, deterministik ekvivalentidir.
3. **Sənəd növünün goAML MMX kodu** (CON/SIG/TO, 3 qayda) —
   `TYPES_OF_DOCUMENTS.ISARE → MMX_ID` xəritəsi, canlı oxunur.
4. **Alan tərəfin (TO) KYC-si** — ƏVVƏLCƏ imza hüququ olan şəxsdən (SIG)
   köçür (TIP Fizik/Hüquqi IN/OUT və ya F-to-F olduqda), YENƏ DƏ boşdursa
   `NAME_UR_TO` mətnindən ("SOYAD AD ATA_ADI" sırası) parçalayır (TIP
   F-to-H-in/F-to-F, alan fiziki/sahibkar olduqda). **İKİ AYRI `if`** —
   `else if` YAZILMADI, çünki AYSEL-də bunlar İKİ MÜSTƏQİL UPDATE-dir: TIP=
   FToF-da SIG özü boş olsa, fallback YENƏ işə düşməlidir.
5. **Alan tərəf bizim ÖZ bankımızın hesabıdırsa** (DEBET prefiksi
   `BALSCHT_UR`-da, TIP Hüquqi-Hüquqi/Fiziki istiqamətində) — adı/VÖEN
   `LICSCH_ALL_2`-dən, bank adı/SWIFT/ölkə kodu bizim ÖZ `ATTRIB`
   sətrimizdən (bu cədvəl TƏK sətirlik — bankın ÖZ profilidir), filial adı
   `MFO`-dan. ⚠️ AYSEL-in öz sorğusu `ATTRIB.INN_BANK`-ı
   `INCORPORATION_COUNTRY_CODE_TO`-ya yazır (semantik qəribə görünür) —
   BİRƏ-BİR köçürüldü, dəyişdirilmədi.
6. **Bank adı SWIFT koduna görə** (`BIC_CODES`, `RPAD(SWIFT,11,'X')` açarı) —
   yalnız 5-ci addım doldurmayıbsa.
7. **TRANSMODE_CODE** — DEBET fiziki şəxs hesabıdırsa VƏ hər iki tərəf "41"
   balans sinfindədirsə (kassa/nəğd keçid hesabları). AYSEL-in öz
   `GOAML_NAME_FIL` filial şərti **ÇIXARILDI** — bank tək filialdır
   (istifadəçi təsdiqi).
8. **Defolt sahələr** (NVL-tipli, Oracle sorğusu lazım deyil) — `FROM_COUNTRY`/
   `TO_COUNTRY_TO`/`INCORPORATION_COUNTRY_CODE` = 'AZ' (boşdursa), `SNN`/
   `SNN_TO`-dakı `'-'` placeholder-i `NULL`-a çevirir, `TO_FUNDS_CODE` = 'A'.

### Yeni sahə — `InstitutionNameBranchTo`

Entity-də çatışmırdı (5-ci qrup onu doldurur) — migration
`20261107080000_GoamlInstitutionBranch.cs` (yalnız `AddColumn`, `InsertData`
YOXDUR). `GoamlTesdiqDto`-ya da əlavə olundu (`GoamlEmeliyyatDetalDto` ondan
miras aldığı üçün "Bax" ekranında da avtomatik görünür) — operator-input
DEYİL, YALNIZ `AvtoDolduruAsync` yazır, `Tesdiq.cshtml` formasına `<input>`
ƏLAVƏ EDİLMƏDİ (operatorun doldura biləcəyi sahə deyil).

### Arxitektura — "ən yaxşı cəhd", Oracle-a YALNIZ OXUMA

`AvtoDolduruAsync` Oracle-a **yeddi AYRI canlı, YALNIZ-OXU sorğu** göndərir
(`MMX_ID_SERIAL`, `TYPES_OF_DOCUMENTS`, `BALSCHT_UR`/`BALSCHT_FIZ` üzvlük
yoxlaması, `LICSCH_ALL_2`, `ATTRIB`, `MFO`+`VIEW_LICSCH_ALL`, `BIC_CODES`) —
İSTİNAD CƏDVƏLLƏRİNİN MƏZMUNU HARDCODE EDİLMƏYİB, hər çağırışda canlı
sorğulanır ki, Oracle-dakı dəyişiklik koda toxunmadan əks olunsun. **Hər bir
Oracle çağırışı ÖZ köməkçisində try/catch-lə əhatə olunub** — Oracle əlçatmaz
olsa belə "Təsdiqlə" ƏMƏLİYYATI UĞURSUZ OLMUR (operator artıq doldurduğu
KYC-ni itirməsin deyə), sadəcə həmin addım(lar) sakitcə ötürülür.

⚠️ **Oracle-a HEÇ NƏ YAZILMIR** — bütün hesablanmış dəyərlər bizim ÖZ
`GoamlEmeliyyat` sətrinə (hələ `SaveChanges`-ə getməmiş, yaddaşdakı obyektə)
yazılır, `TesdiqleAsync`-in `YaratAsync`+`YaddaSaxlaAsync`-ından ƏVVƏL
çağırılır — tək DB round-trip-də (bizim öz bazamıza) yekunlaşır.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — bu düzəlişdən ƏVVƏL təsdiqlənmiş qeydlər
(bu sessiyaya qədər, əgər varsa) yenidən emal edilmir, yalnız BUNDAN SONRAKI
"Təsdiqlə" çağırışları avtomatik dolur.

⚠️ **Açıq qalan iş (gələcək, 06.11.2026-dan)**: 7 `ID_VD`-asılı qayda
(SWIFT/xarici invaluta/"postupl"/"nacval"/daxili invaluta ×2) — bu açarın
bizim namizəd sorğumuzda (`DOC_DNA_ALL_TAB`/`_RISK`) haradan gəldiyi hələ
araşdırılmayıb, ayrıca Oracle araşdırması (`v$sql`/`v$open_cursor`, 93-94-cü
fayllardakı üsulla) tələb edir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə/brace balansı Python skriptlə — şərh/mətn daxilindəki
dekorativ mötərizələr çıxarılıb YENİDƏN yoxlanıldı, kodun özü balanslıdır;
`GoamlTipi`/`GoamlHesabatNovu` enum dəyərləri `GoamlEnums.cs`-dən bir-bir
təsdiqləndi — TIP=88 bizim enum-da YOXDUR, ona görə həmin dəyərli IN-siyahı
üzvləri kodda ÇIXARILDI, çünki operator onu HEÇ VAXT seçə bilməz). İstifadəçi
Visual Studio-da **Rebuild All**, `Update-Database`, sonra real ssenari ilə
yoxlamalıdır: (1) VÖEN-i daxil edib heç bir Fizik/Sahibkar/Hüquqi checkbox-u
İŞARƏLƏMƏDƏN "Təsdiqlə" et → "Bax" ekranında müvafiq checkbox-un avtomatik
işarələndiyini; (2) "Hesab imza hüququ olan şəxs" bloku dolu, "Alan fiziki
şəxs" bloku BOŞ saxlanmış, TIP=Fiziki (OUT/IN) seçilmiş bir namizədi təsdiqlə
→ "Alan fiziki şəxs" məlumatlarının SIG-dən köçürüldüyünü; (3) öz bankımızın
DEBET hesabı ilə, TIP=H to H olan bir namizədi təsdiqlə → "Bankın adı"/SWIFT/
"Bank filialı"-nın doldurulduğunu təsdiqləməlidir.

## GOAML — Təsdiqlə Formunda Debet/Kredit/Məbləğ HƏR DƏFƏ "—" Yazılırdı (09.11.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi real mühitdə ilk tam sınağı (Tip, ReportCode, "Hesab imza hüququ
olan şəxs" doldurub "Təsdiqlə və Saxla") apardı — "Bax" detal ekranında
**Debet, Kredit, Məbləğ (AZN) hamısı "—"** göründü, halbuki axtarış siyahısında
(və Tesdiq formunun özünün yuxarısındakı boz info-zolağında) bu dəyərlər
AÇIQ görünürdü.

**Kök səbəb — "Geri Qayıdışda Axtarış İtirdi" düzəlişinin (06.11.2026)
özündə yaranmış, həmin vaxtdan qalan reqressiya.** O düzəliş `Tesdiq.cshtml`-ə
sırf-OXUNUŞ `gml-info-strip` əlavə edərkən, köhnə formada bu dəyərləri
(Debet/Kredit/NameUr/ToAdi/SummaVNacval/Primechanie) daşıyan `<input>`-ları
**SİLİB, əvəzinə heç bir `name` atributu olmayan `<span>` göstərişi** qoymuşdu
— şərhin özü bunu "formaya göndərilmir, yalnız göstəriş üçündür" deyə
AÇIQ YAZMIŞDI, amma bu fərziyyə SƏHV idi: bu altı sahə `GoamlEmeliyyat`-ın
ÖZÜNDƏ daimi saxlanan sütunlardır (ekran göstərişi üçün YOX), ona görə
göndərilməli idi. Nəticə: `TesdiqleAsync`-ə POST olunan `GoamlTesdiqDto`-da
bu altı sahə HƏR DƏFƏ `null` gəlirdi, entity-yə `null` yazılırdı, SaveChanges
uğurla keçirdi (bu sahələr nullable olduğu üçün) — **HEÇ BİR XƏTA ÇIXMADAN**.

Bu, CLAUDE.md-nin "Şərtli Render Olunan Form Sahəsi + Default Parametr =
Səssiz Data İtkisi" bölməsindəki qaydanın EYNİ, YALNIZ daha geniş formasıdır —
fərq: o qaydada sahə ŞƏRTLƏ render olunmurdu, burada isə SAHƏ
ÜMUMİYYƏTLƏ (heç bir şərtsiz) `<input>`-dən MƏHRUM EDİLMİŞDİ.

**Niyə "Əməliyyat tarixi" sütunu düz görünürdü, Debet/Kredit yox:**
`DateOper`-in öz görünən `<input type="date" name="DateOper">`-i var idi
(forma yuxarısında, "Əməliyyat tarixi" sahəsi) — o, düzgün göndərilirdi.
Siyahı (`Siyahi.cshtml`) səhifəsindəki "ƏMƏL. TARİXİ" sütunu isə ayrıca
`DateOper ?? CandidateDateOper` fallback-i daşıyır (`HamisiniGetirAsync`,
sətir 300) — CandidateDateOper hər zaman hidden input-la gəldiyi üçün tarix
sütunu YALANDAN "düzgün" görünürdü, halbuki `DetalGetirAsync`-ın özündə
belə fallback YOXDUR (`DateOper = e.DateOper`, sətir 343) — DateOper-in
əslində düz göndərildiyi üçün bu fərq praktikada görünmədi, amma Debet/
Kredit/Məbləğ üçün HEÇ bir fallback yoxdur, ona görə onlar çılpaq "—" qaldı.

**Düzəliş — `Tesdiq.cshtml`-in formasına 7 hidden input əlavə edildi**
(`Debet`, `Kredit`, `NameUr`, `ToAdi`, `Primechanie`, `KodValuti` — hamısı
`asp-for`, + `SummaVNacval` — `decimal?` olduğu üçün InvariantCulture ilə əl
ilə yazıldı, "Razor → CSS/JS Rəqəm — Mədəniyyət" qaydasına uyğun, az-AZ
vergülü hidden input-a düşməsin deyə). `gml-info-strip`-in ÖZÜ TOXUNULMADI —
o, yenə də sırf göstərmə qatıdır, indi yanında AYRI, görünməz hidden
input-lar var.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR, VƏ RETROAKTİV DÜZƏLİŞ MÜMKÜN DEYİL.** Bu
düzəlişdən ƏVVƏL (06.11.2026 — 09.11.2026 arası) təsdiqlənmiş HƏR qeydin
Debet/Kredit/NameUr/ToAdi/SummaVNacval/Primechanie sütunları artıq `NULL`-dur
— Oracle-un öz namizəd mənbəyi (`DOC_DNA_ALL_TAB`) bu məlumatı saxlamır,
yalnız AXTARIŞ ANINDA canlı sorğulanır, ona görə köhnə qeydə GERİ QAYIDIB
bu sahələri doldurmaq mümkün deyil (əməliyyat artıq axtarış pəncərəsindən
çıxıb). Belə qeydlər (test mühitində İlkanə Mahmudova-nın #1 qeydi daxil)
YALNIZ silinib YENİDƏN, düzgün formadan təsdiqlənə bilər — GOAML modulunda
silmə/redaktə funksiyası YOXDUR (qəsdən, "sırf oxunuş" dizaynı), bu səbəbdən
korrupt test qeydləri SQL Server-dən ƏL İLƏ (`DELETE FROM GoamlEmeliyyatlar
WHERE Id=...`) təmizlənməlidir — yalnız TEST qeydləri üçün, production
datası üçün belə bir silmə HEÇ VAXT edilməməlidir.

**Qayda (təkrar, bu dəfə "görüntü üçün ayrı span" formasında):** bir dəyəri
"sırf göstərmə üçündür, formaya göndərilmir" deyə `<input>`-dən MƏHRUM
EDƏRKƏN, əvvəlcə O DƏYƏRİN hədəf entity-də DAİMİ SAXLANAN bir sütun olub
olmadığını yoxla. Yalnız HESABLANMIŞ/TƏKRAR-ÇIXARILA BİLƏN göstərişlər
(məs. "N nəfər oxudu" sayğacı) təhlükəsiz şəkildə inputsuz qala bilər —
entity-nin ÖZ sütununa yazılası bir dəyər isə, ekranda "sadəcə göstərmək
üçündür" düşüncəsi ilə YAZILMAZSA, SaveChanges-i sındırmaz (nullable olduğu
üçün) və buna görə XƏTASIZ, SƏSSİZCƏ itər — bu, ən çətin aşkarlanan növdür,
çünki heç bir log/xəbərdarlıq yaranmır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı, DTO sahə adlarının (`Debet`/`Kredit`/
`NameUr`/`ToAdi`/`Primechanie`/`KodValuti`/`SummaVNacval`) `GoamlTesdiqDto`
ilə bir-bir tutuşdurulması). İstifadəçi Visual Studio-da **Rebuild All**
ilə, sonra YENİDƏN eyni ssenarini (Tip+ReportCode seç, bir KYC bloku doldur,
Təsdiqlə) sınamalıdır — bu dəfə "Bax" ekranındakı Debet/Kredit/Məbləğ (AZN)
sütunlarının axtarış siyahısındakı dəyərlərlə DƏQIQ üst-üstə düşdüyünü
təsdiqləməli, həmçinin köhnə (bu düzəlişdən əvvəlki) test qeydini silib
yenidən sınamalıdır.

## GOAML — CON/SIG Avtomatik Doldurma Tapıldı: Hüquqi Şəxsin Öz AML KYC Bazası (09.11.2026)

İstifadəçi "sistem gətirə bilmirsə, işçi doldurur — hesab melumatları bazadan gəlməlidir"
tapşırığı üzərinə, canlı Oracle araşdırması (istifadəçinin özü PL/SQL Developer-də
işlədib nəticəni göndərdi) "Nümayəndənin" (CON) və "Hesab imza hüququ olan şəxsin"
(SIG) bloklarının ƏSL mənbəyini tapdı — bu, 06.11.2026-dakı "Update New" kaskadından
TAMAMİLƏ AYRI, bankın öz **AML/KYC** modulunun bazasıdır.

### Mənbə cədvəllər

| Hesab növü | Mənbə | Açar |
|---|---|---|
| **Fiziki** (`regnom.fizik=1`) | `odb.regnom`-un İNGİLİSCƏ sütunları — `NAME`/`SURNAME`/`PATRONYMIC`/`BIRTHDATE`/`BIRTHADDRESS`/`GENDER`/`REGISTRATIONADDRESS`/`CITIZENSHIP`/`EMPLOYER`/`PIN`/`DOCUMENTNUMBER`/`GIVENDATE`/`EXPIREDATE`/`GIVENORGANIZATOIN` — HAMISI `VARCHAR2`, tarix sahələri DAXİL | `substr(hesab,10,6) = regnom.regnom` |
| **Hüquqi** | `odb.imza_huquqi_olan_shexsler` — ADI/SOYADI/ATA_ADI/FİN/DOGULDUGU_TARİX/VEZIFESI/ISH_YERININ_ADI və s. (hamısı `VARCHAR2`) | `regnom = regnomKodu` (eyni substr) |

**Rol — `odb.aml_related_persons`(ID_AML, ID_RELATED_PERSONS) → `odb.aml_setup_related_persons`
(ID, DESCRIPT)** — sabit açar cədvəl: 0=boş, **1=Hesaba imza hüququ olan şəxslər**,
**2=Rəhbər**, 3=Təsisçi, 4=Benefisiar, 5=Digər. İstifadəçi qərarı: **SIG-ə "Rəhbər"
(id=2) yazılır**, "Hesaba imza hüququ olan şəxslər" (id=1) kateqoriyası birbaşa
İŞLƏNMİR (ekran-görüntüsündə açıq göstərilib). `IMZA_BITME IS NULL` = hazırda aktiv
imza hüququ (istifadəçi təsdiqi) — köhnə/bitmiş qeydlər avtomatik doldurmaya daxil
edilmir.

### Niyə CON avtomatik doldurulmur — operator ÖZÜ seçir

İstifadəçi qərarı: "nümayəndə"ni (əməliyyatı FİZİKİ olaraq aparan şəxs) etibarlı
müəyyən etmək mümkün deyil — şirkətin HƏR hansı əlaqəli şəxsi (rəhbər, mühasib,
əməkdaş) sayğaca gələ bilər. Ona görə CON ÜÇÜN sistem **seçim veriri**: şirkətin
BÜTÜN (hazırda aktiv) `imza_huquqi_olan_shexsler` sətirləri rol etiketi ilə
(`LISTAGG`-lə, `AML_RELATED_PERSONS`+`AML_SETUP_RELATED_PERSONS`-dən) bir
`<select>`-ə doldurulur, operator kimi seçsə o şəxsin tam KYC-si JS ilə (əlavə
server sorğusu OLMADAN, GET-də artıq gətirilmiş JSON-dan) CON-un bütün xanalarına
yazılır. Fiziki hesabda isə bu sualın özü yoxdur — hesab sahibi ÖZÜ həm CON, həm
SIG-dir, ikisi də birbaşa doldurulur.

### Struktur

`GoamlElaqaliSexsDto`/`GoamlHesabSahibiKycDto` (`FinNex.Application/DTOs/Goaml/`) —
`Fiziki` bayrağı, `TekSexs` (fizikidə CON+SIG-in hər ikisi, hüquqidə YALNIZ
"Rəhbər" → SIG), `ElaqaliSexsler` (hüquqidə CON seçimi üçün tam siyahı).
`IGoamlService.HesabSahibiKycTapAsync(filial, debet, kredit)` — Debet-ə baxır,
FAYDALI nəticə (TekSexs VAR **və ya** ElaqaliSexsler boş DEYİL) tapılmasa Kredit-ə
keçir (istifadəçinin "hansı olursa" qərarına uyğun — iki hesabdan BOŞ çıxanı
qaytarmaq əvəzinə digərini sına).

`EmeliyyatlarController.Tesdiq` (GET) indi **async**-dır — namizəd açılan kimi bu
metodu çağırır, tapılanı `GoamlTesdiqDto`-nun Con*/Sig* sahələrinə yazır,
`ViewBag.ElaqaliSexsler`-i CON seçim dropdown-u üçün ötürür. `Tesdiq.cshtml`-də
avtomatik dolduğunda yaşıl info-banner ("sistem avtomatik gətirdi, yoxlayın") —
HƏR sahə yenə də REDAKTƏ OLUNA BİLƏR, bu yalnız BAŞLANĞIC nöqtəsidir, operator
səhv/köhnə gördüyünü düzəldə bilər.

⚠️ **Tarix sahələri (`DOGULDUGU_TARIX`, `VERILME_TARIXI`, `ETIBARLILIQ_MUDDETI`,
REGNOM-un `BIRTHDATE`/`GIVENDATE`/`EXPIREDATE`-i DAXİL) Oracle-da HAMISI `VARCHAR2`
— `DATE` tipi DEYİL.** `GoamlService.TarixiParseEt` bir neçə formatı (`dd.MM.yyyy`,
`yyyy-MM-dd`, `dd-MM-yyyy`, `dd/MM/yyyy` və s.) sırayla sınayır, heç biri tutmasa
(mətn boş/naməlum formatdadırsa) **sakitcə `null` qaytarır** — parse xətası
`TesdiqleAsync`-i sındırmır, sadəcə o sahə boş qalır, operator özü yazır.

⚠️ **`IMZA_BASHLAMA`/`IMZA_BITME` — bunlar əsl `DATE` tipindədir** (yeganə real
DATE sütunları bu iki cədvəldə) — `IMZA_BITME IS NULL` şərti birbaşa SQL-də
yoxlanılır, C#-da əlavə parse lazım deyil.

⚠️ **Bu, 06-07.11.2026-dakı "Update New" mexanizmindən (AvtoDolduruAsync) TAM
MÜSTƏQİLDİR** — o, POST-da (TesdiqleAsync daxilində) işləyir və sənəd
seriya/nömrə parçalama, TO-nun SIG-dən köçürülməsi kimi İKİNCİ DƏRƏCƏLİ
sahələri doldurur; bu YENİ mexanizm isə GET-də (forma AÇILANDA) işləyir və
CON/SIG-in ƏSAS şəxsiyyət sahələrini doldurur. İkisi EYNİ sahəyə YAZMIR —
toqquşma yoxdur.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR, YALNIZ BUNDAN SONRAKI "Təsdiqlə" GET-lərinə
TƏSİR EDİR** — artıq təsdiqlənmiş qeydlər dəyişmir (bu modulda redaktə/silmə
funksiyası yoxdur, yenidən təsdiqləmək lazımdır).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı Python skriptlə, həm ümumi fayl, həm YALNIZ yeni əlavə olunan
sətirlər ayrıca yoxlanıldı — hər ikisi balanslı çıxdı; interfeys↔implementasiya↔
controller↔view dörd qatı sahə adı-adı əl ilə tutuşduruldu, JSON casing-i
`JsonNamingPolicy.CamelCase` ilə açıq təyin edildi — "Razor-da JsonSerializer.Serialize
casing tələsi" qaydasına uyğun). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra real ssenari ilə yoxlamalıdır: (1) bizim bankın fiziki müştərisinin hesabı
olan bir namizəd aç → CON+SIG-in hər ikisinin avtomatik (yaşıl banner ilə) dolduğunu
təsdiqlə; (2) hüquqi şəxsin (AML bazasında "Rəhbər"i qeydiyyatda olan) hesabı olan
bir namizəd aç → SIG-in "Rəhbər"dən avtomatik dolduğunu, CON bloku üzərindəki
"Əlaqəli şəxslərdən seçin" dropdown-unun şirkətin bütün aktiv əlaqəli şəxslərini
(rol etiketi ilə) göstərdiyini, seçimdən sonra CON xanalarının dolduğunu
təsdiqləməli; (3) xarici/başqa bankın hesabı olan namizəddə heç bir avtomatik
doldurma/banner/dropdown ÇIXMADIĞINI (əvvəlki kimi tam əl ilə doldurma) yoxlamalı.

## GOAML — CON/SIG Avtomatik Doldurma `odb.regnom`-un ÖZÜ TAPILMASA, TAM BLOKLANIRDI (09.11.2026, KRİTİK — DÜZƏLDİLDİ)

Yuxarıdakı CON/SIG avtomatik doldurma mexanizmi (eyni günün əvvəlki bölməsi) real
mühitdə sınandı — hüquqi şəxsin (Kredit sahibi "DR.ESS" MMC) hesabı olan bir
namizəddə "Nümayəndənin" VƏ "Hesab imza hüququ olan şəxsin" blokları HƏR İKİSİ
boş qaldı, baxmayaraq ki `imza_huquqi_olan_shexsler`-də bu şirkətin aktiv (1
sətir) qeydi VAR idi.

**Diaqnoz — istifadəçi PL/SQL Developer-də dörd ayrı yoxlama apardı:**

| Sorğu | Nəticə |
|---|---|
| `substr(kredit, 10, 6)` | `019843` |
| `odb.balscht_ur` üzvlüyü (Kredit) | **BİZİMDİR (1)** |
| `odb.imza_huquqi_olan_shexsler where regnom='019843'` | **1 sətir, 1 aktiv** (`imza_bitme IS NULL`) |
| `odb.balscht_fiz`/`balscht_ur` üzvlüyü (Debet) | 0 sətir — bizim deyil |

Yəni Kredit hesabı bizim bankın hüquqi şəxs hesabıdır VƏ onun aktiv imza hüququ
olan şəxsi var — amma ekranda heç nə görünmürdü.

**Kök səbəb:** `GoamlService.HesabSahibiKycAsync` FİZİKİ/HÜQUQİ ayrımını
`odb.regnom.FIZIK` sütunundan təyin edirdi — AMMA bundan ƏVVƏL **ƏVVƏLCƏ
`odb.regnom where regnom=X` sorğusunu icra edirdi**, sıfır sətir qayıdanda
isə `if (regnomSetirler.Count == 0) return null;` ilə **BÜTÖV metodu
dayandırırdı** — `imza_huquqi_olan_shexsler`-ə ÇATMADAN. `odb.regnom`
əsasən FİZİKİ şəxs profilləri üçün nəzərdə tutulmuş görünür — hüquqi
şəxslərin çoxu (DR.ESS daxil) orada HEÇ BİR sətrə malik deyil. Nəticə:
BALSCHT_UR-da tanınan, `imza_huquqi_olan_shexsler`-də real data olan BİR
hüquqi şəxs belə, `odb.regnom`-da sətri olmadığı üçün bütün axın səssizcə
`null` qayıdırdı.

**Düzəliş — FİZİKİ/HÜQUQİ ayrımı artıq `odb.regnom`-a GÜVƏNMİR, BİRBAŞA
`BALSCHT_FIZ`/`BALSCHT_UR` üzvlüyündən götürülür** (bu, onsuz da `bizimdirmi`
yoxlamasında ARTIQ hesablanırdı, sadəcə İKİ AYRI sual kimi işlənirdi — "bizim
hesabımızdırmı" VƏ "FİZİKİDİRMİ" — halbuki BİR sorğu hər ikisinə cavab
verir):

```csharp
var fizPrefiksdir = await BalschtDaxilindedirAsync("balscht_fiz", temiz, ct);
var urPrefiksdir = !fizPrefiksdir && await BalschtDaxilindedirAsync("balscht_ur", temiz, ct);
if (!fizPrefiksdir && !urPrefiksdir) return null;
```

`fizPrefiksdir=true` olanda YENƏ DƏ `odb.regnom`-dan KYC oxunur (fiziki
şəxslərin profili həqiqətən orada saxlanılır — bu qol dəyişmədi). `urPrefiksdir=
true` olanda isə artıq **`odb.regnom`-a HEÇ TOXUNMUR**, birbaşa
`imza_huquqi_olan_shexsler`-ə keçir — "sətir tapılmadı" halı artıq YALNIZ
"bu şirkətin əlaqəli şəxsi qeydə alınmayıb" (`hamisi.Count == 0` →
`new GoamlHesabSahibiKycDto { Fiziki = false }`, boş CON siyahısı, boş SIG)
mənasına gəlir — "odb.regnom-da sətir yoxdur deyə hər şeyi dayandır" artıq
MÜMKÜN DEYİL.

**Qayda:** bir "bu şəxs fiziki, ya hüquqidir?" sualını cavablamaq üçün İKİ
AYRI mənbə varsa (burada: `BALSCHT_FIZ/UR` üzvlüyü VƏ `odb.regnom.FIZIK`
sütunu) — əgər BİRİ artıq ETİBARLI VƏ TAM (BALSCHT, hesab prefiksinə görə
HƏR hesabı əhatə edir) İSƏ, İKİNCİSİNİ (regnom, YALNIZ QİSMƏN əhatəli) yalnız
ƏLAVƏ MƏLUMAT üçün (KYC-nin özünü oxumaq) işlət, YOX "bu şəxs mövcuddurmu"
sualının ÖZÜNÜ ona bağlama — əks halda ikinci mənbənin boşluğu (bir sətrinin
olmaması) birinci, etibarlı mənbənin verdiyi müsbət cavabı SƏSSİZCƏ ləğv edər.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR — yalnız BUNDAN SONRAKI "Təsdiqlə" GET-lərinə
TƏSİR EDİR.** Artıq açılmış/təsdiqlənmiş formalar dəyişmir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı — dəyişikliyin ÖZÜ balanslıdır, `git diff`-in əlavə/silinən
sətirləri ayrıca yoxlanıldı: 9/9 və 10/10 mötərizə, fayl-ümumi -8 fərqi əvvəldən
mövcud şərh mətnindəndir, dəyişməyib). İstifadəçi Visual Studio-da **Rebuild
All** ilə, sonra EYNİ namizədi (Kredit sahibi DR.ESS MMC) yenidən açıb
yoxlamalıdır: "Nümayəndənin əlaqəli şəxslərindən seçin" dropdown-unun artıq
göründüyünü, "Hesab imza hüququ olan şəxsin" blokunun "Rəhbər"dən avtomatik
dolduğunu.

### Yuxarıdakı Düzəliş REBUILD OLUNUB SINANDI — Yenə BOŞ ÇIXDI, SƏBƏB TAPILMADI (09.11.2026, EYNİ GÜN, KRİTİK — DİAQNOSTİKA DAVAM EDİR)

İstifadəçi real Visual Studio-da **Rebuild All** etdi (Output: "0 Errors, 136
Warnings — Rebuild completed", köhnə/əlaqəsiz xəbərdarlıqlar), tətbiqi yenidən
işə saldı və EYNİ namizədi (DR.ESS MMC, regnom=019843) YENİDƏN açdı —
**"Nümayəndənin"** VƏ **"Hesab imza hüququ olan şəxsin"** blokları HƏR İKİSİ
YENƏ TAM BOŞ idi, heç bir yaşıl "avtomatik dolduruldu" banner-i, heç bir
"əlaqəli şəxslərdən seçin" dropdown-u görünmürdü.

**Diaqnoz:** `HesabSahibiKycTapAsync` → Debet (2501900000000300007) bizim
bankın hesabı DEYİL (əvvəlki diaqnostik bunu təsdiqləmişdi, `fizPrefiksdir`/
`urPrefiksdir` hər ikisi `false` → erkən `null`, heç bir exception YOXDUR) →
Kredit-ə keçir (4006000000198430000, `urPrefiksdir=true`, BALSCHT_UR-da
TƏSDİQLƏNMİŞ) → **`odb.imza_huquqi_olan_shexsler` sorğusunun ÖZÜ** (LISTAGG
alt-sorğusu ilə, `odb.aml_related_persons`/`odb.aml_setup_related_persons`-ə
join edərək) icra olunur. Köhnə `catch { return null; }` bu sorğunun
NƏTİCƏSİNDƏN (uğurlu, 0 sətir, yoxsa ORA-xətası) **heç bir iz saxlamırdı** —
nəticə hər iki halda eyni görünürdü: tam boş ekran.

**Kök şübhə — SQL-dəki ÇOXLU sütun adı HEÇ VAXT canlı Oracle-a qarşı
TƏK-TƏK YOXLANILMAYIB.** Əvvəlki sessiyada `odb.imza_huquqi_olan_shexsler`-in
YALNIZ `ADI/SOYADI/ATA_ADI/FİN/DOGULDUGU_TARİX/VEZIFESI/ISH_YERININ_ADI`
sütunları canlı sorğu ilə TƏSDİQLƏNMİŞDİ (bax yuxarı, "GOAML Arxitekturası"
bölməsi). Bu düzəlişdə əlavə edilən **12 sütun** (`telefon_nomresi`,
`unvanin_teyinati`, `kuchenin_adi_ve_nomresi`, `sheher`, `olke`,
`senedin_novu`, `seriyasi_ve_nomresi`, `verilme_tarixi`, `etibarliliq_muddeti`,
`veren_orqanin_adi`, `verildiyi_olke`, `doguldugu_yer`, `cinsi`) VƏ LISTAGG
alt-sorğusundakı `aml_related_persons.id_related_persons`/
`aml_setup_related_persons.descript` sütunları — Tesdiq formunun özündəki sahə
adlarından (ad oxşarlığı ilə) **TƏXMİN EDİLMİŞDİ**, canlı Oracle-a qarşı
ayrıca YOXLANILMAMIŞDI. CLAUDE.md-nin "Oracle Verilənlər Bazası" bölməsindəki
əsas qaydanın (sütun adını canlı sorğu ilə TƏSDİQLƏMƏDƏN yazma) pozulduğu
yer məhz budur — nəticə isə `OracleService.SelectAsync`-in ORA-00904 kimi
sintaksis xətalarını (`KeciciXetalar` siyahısında olmadığı üçün) BİLƏRƏKDƏN
İSTİSNA ATMASI, sonra `GoamlService`-in öz `catch { return null; }`-inin bu
istisnani SƏSSİZCƏ UDMASI idi — "`ex.Message` TƏK BAŞINA HEÇ NƏ DEMİR" /
"AI Çağırışları — CallApiAsync ƏSL XƏTANI UDURDU" bölmələrindəki EYNİ
tələnin, bu dəfə Oracle sorğusu formasında təkrarı.

**Düzəliş (bu dalğada edilən, DİQQƏT: Oracle sorğusunun ÖZÜ DƏYİŞMƏDİ) —
`GoamlService`-ə `ILogger<GoamlService>` inject edildi** (layihənin artıq
`MarifAiService`/`SenedAiService`/`HRMeslehetciService`-də işlətdiyi EYNİ
konvensiya — konstruktor injection, `_logger.LogWarning`/`LogError`),
`BalschtDaxilindedirAsync`-in VƏ `HesabSahibiKycAsync`-in silinmiş
`catch { return null/false; }` bloklarına **`_logger.LogError(ex, ...)` /
`LogWarning(ex, ...)`** əlavə olundu — davranış DƏYİŞMİR (hələ də səssizcə
`null`/`false` qaytarır, "Təsdiqlə" GET-i sınmır), YALNIZ SƏBƏB artıq
`Logs\log-<bugünkü-tarix>.txt`-ə yazılır.

⚠️ **BU, İCAD YERİNƏ DİAQNOSTİKA ADDIMIDIR — real kök səbəb HƏLƏ
TƏSDİQLƏNMƏYİB.** İstifadəçi Visual Studio-da Rebuild All edib EYNİ namizədi
yenidən açdıqdan sonra `Logs\log-<bugünkü-tarix>.txt` faylında "GOAML
HesabSahibiKycAsync uğursuz oldu" sətrini (varsa) tapıb göndərməlidir —
`InnerException`/Oracle-un əsl mesajı (məs. `ORA-00904: "S"."TELEFON_NOMRESI":
invalid identifier`) orada görünəcək.

**PARALEL, DAHA SÜRƏTLİ YOL — birbaşa PL/SQL Developer-də test:** rebuild
gözləmədən, aşağıdakı sorğunu (C#-ın ÖZÜNÜN göndərdiyi mətnin HƏRFİ eynisi,
`regnomKodu='019843'` ilə) PL/SQL Developer-də icra etmək səbəbi DƏRHAL
göstərəcək:

```sql
select s.id_aml, s.adi, s.soyadi, s.ata_adi, s.doguldugu_tarix, s.doguldugu_yer,
       s.cinsi, s.fin, s.telefon_nomresi, s.unvanin_teyinati, s.kuchenin_adi_ve_nomresi,
       s.sheher, s.olke, s.senedin_novu, s.seriyasi_ve_nomresi, s.verilme_tarixi,
       s.etibarliliq_muddeti, s.veren_orqanin_adi, s.verildiyi_olke, s.vezifesi,
       s.ish_yerinin_adi,
       (select listagg(st.descript, ', ') within group (order by st.id)
        from odb.aml_related_persons rp
        join odb.aml_setup_related_persons st on st.id = rp.id_related_persons
        where rp.id_aml = s.id_aml) as rollar
from odb.imza_huquqi_olan_shexsler s
where s.regnom = '019843' and s.imza_bitme is null;
```

Xəta çıxarsa (ən güman: `ORA-00904`), Oracle **dəqiq hansı sütun adının
səhv olduğunu** mesajın özündə göstərəcək — növbəti addım o sütunu `select *
from odb.imza_huquqi_olan_shexsler where rownum <= 1` (bütün sütunları, əsl
adları ilə) sorğusu ilə düzəltmək olacaq. Sorğu XƏTASIZ icra olunub sıfır
sətir qaytararsa, problem sütun adında deyil — onda `regnomKodu`/`imza_bitme`
şərtinin özü araşdırılmalıdır (məs. tarix sahəsinin əslində `imza_bitme`
yox, başqa bir sütun olması).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı — `git diff` əlavə 6/6, silinən 1/1 mötərizə; əlavə 6/6,
silinən 2/2 fiqurlu mötərizə, balanslıdır; `ILogger<GoamlService>`-in DI
qeydiyyatı lazım deyil — `GoamlService` heç yerdə əl ilə `new` edilmir,
`AddApplicationServices.cs`-dəki `AddScoped` konteynerin öz framework-daxili
`ILogger<T>`-ni avtomatik inject edəcək). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra EYNİ namizədi yenidən açıb (1) ya yuxarıdakı SQL-i
PL/SQL Developer-də dərhal işlədib göndərməli, (2) ya da `Logs\log-<bugünkü-
tarix>.txt`-dəki "GOAML HesabSahibiKycAsync uğursuz oldu" sətrini tapıb
göndərməlidir.

### Əsl Kök Səbəb Tapıldı — `ORA-01031: insufficient privileges`, KOD DEYİL, ORACLE İCAZƏSİ (09.11.2026, EYNİ GÜN, SONA ÇATDI)

Yuxarıdakı loglama düzəlişi SƏBƏBİ nəhayət üzə çıxardı — amma kod BUGU DEYİL
çıxdı:

```
GOAML BalschtDaxilindedirAsync(balscht_fiz) uğursuz oldu — prefiks=25019
Oracle.ManagedDataAccess.Client.OracleException: ORA-01031: insufficient privileges
```

Tətbiqin Oracle-a qoşulduğu hesab (`appsettings.json → Oracle:ConnectionString`,
`USER ID=FOXPRO`) **`odb.balscht_fiz` VƏ `odb.balscht_ur` cədvəllərinə SELECT
icazəsinə malik deyil**. Hər İKİ cədvəl, HƏR İKİ hesab (Debet 25019, Kredit
40060) üçün eyni xəta verdi — yəni problem KONKRET bir sorğuda/sütunda deyil,
BÜTÖV İKİ CƏDVƏLƏ İCAZƏ YOXLUĞUNDADIR.

**Bu, həmin günün bütün əvvəlki "SQL-i sına, dotnet build-i yoxla, log axtar"
araşdırmasının niyə HEÇ NƏ TAPMADIĞINI izah edir:**
- İstifadəçinin PL/SQL Developer-də işlətdiyi diaqnostik sorğu (`imza_huquqi_
  olan_shexsler` üzərində) **uğurla 1 sətir qaytardı**, çünki PL/SQL
  Developer-də **ŞƏXSİ, tam-icazəli Oracle hesabı** ilə bağlanılıb — `FOXPRO`
  ilə YOX. Sorğunun ÖZÜ həmişə düzgün idi (CLAUDE.md-nin "sütun adını canlı
  sorğu ilə təsdiqləmədən yazma" qaydası əslində POZULMAMIŞDI) — sadəcə bu
  sorğu heç vaxt ÇAĞIRILA bilmirdi, çünki ONDAN ƏVVƏLKİ addım
  (`BalschtDaxilindedirAsync`) FOXPRO-nun icazəsizliyinə görə İSTİSNA ATIB
  bütün metodu erkən `null` ilə dayandırırdı (09.11.2026-ın əvvəlki "regnom
  bypass" düzəlişi BU SƏBƏBDƏN TƏSİRSİZ qaldı — o düzəliş düzgün idi, amma
  `BalschtDaxilindedirAsync`-in özü ARTIQ keçmirdi).
- Əvvəlki `catch { return false; }` / `catch { return null; }` (loglamadan
  ƏVVƏLKİ versiya) bu ORA-01031-i SƏSSİZCƏ udurdu — istifadəçinin bir neçə
  saatlıq "rebuild et, yenidən sına" dövrəsi elə BU SƏBƏBDƏN nəticəsiz qaldı.

**Bu icazə boşluğu YALNIZ bu gün yazılan CON/SIG koduna aid DEYİL — eyni
`BalschtDaxilindedirAsync("balscht_ur"/"balscht_fiz", ...)` çağırışı
07.11.2026-da qurulan "Update New" avtomatik doldurma mexanizminin DƏ İKİ
addımında (5-ci addım: "Alan tərəf bizim öz bankımızdırsa" VƏ 7-ci addım:
"Vəsaitin növü TRANSMODE_CODE") işlənir** (`GoamlService.cs:603, 643`) — yəni
bu iki addım da 07.11.2026-dan bəri EYNİ SƏBƏBDƏN sakitcə işləməyib, heç vaxt
fərq edilməyib (həmin günün "best-effort, Oracle əlçatmaz olsa belə Təsdiqlə
sınmır" dizaynı buna görə görünməz qalmışdı — bax yuxarı "AvtoDolduruAsync"
bölməsi).

**Düzəliş — KOD TƏRƏFİNDƏ YOXDUR, DBA/Oracle tərəfindədir:**
```sql
GRANT SELECT ON odb.balscht_fiz TO FOXPRO;
GRANT SELECT ON odb.balscht_ur TO FOXPRO;
```
Bu GRANT-lar veriləndən sonra **tətbiqi yenidən başlatmağa ehtiyac yoxdur**
(hər Oracle sorğusu yeni bağlantı açır, icazə DƏRHAL effektlidir) — CON/SIG
avtomatik doldurma VƏ yuxarıdakı iki "Update New" addımı eyni anda işə
düşməlidir.

**Qayda (bu sessiyanın yekun dərsi):** `BalschtDaxilindedirAsync` kimi,
LAYİHƏNİN ÖZ Oracle istifadəçi hesabının (`FOXPRO`) İLK DƏFƏ toxunduğu bir
cədvələ sorğu yazanda — hətta cədvəlin ÖZÜ, sütun adları canlı yoxlanılmış
olsa belə — HESABIN O CƏDVƏLƏ İCAZƏSİ OLUB-OLMADIĞI AYRI BİR SUALDIR, sorğunun
sintaksisindən TAM MÜSTƏQİL. `ORA-01031` ilə `ORA-00904` eyni "catch"
blokunda eyni cür tutula bilər, amma səbəbləri KÖKÜNDƏN fərqlidir (biri
icazə, biri sintaksis) — loglama (`ex.Message`/`LogError(ex, ...)`) OLMADAN
ikisini ayırd etmək MÜMKÜN DEYİL, bu da "`ex.Message` TƏK BAŞINA HEÇ NƏ DEMİR"
qaydasının niyə HƏR Oracle/HTTP çağırışında tətbiq olunmalı olduğunun REAL,
bu sessiyada yaşanmış sübutudur — loglama olmasaydı bu kök səbəb HEÇ VAXT
tapılmazdı, sadəcə "sistem işləmir" qalardı.

⚠️ **Bu GRANT-ları işlədəcək səlahiyyət bu kod sessiyasında (və FOXPRO
hesabının özündə) YOXDUR** — istifadəçi bunu DBA/Oracle admin hesabı ilə
(və ya `odb` sxeminin sahibi ilə) özü icra etməlidir. Kod tərəfində başqa heç
bir dəyişiklik lazım deyil.

✅ **TƏSDİQLƏNDİ (09.11.2026, eyni gün).** İstifadəçi GRANT-ları tətbiq etdi,
EYNİ namizədi (DR.ESS MMC) TƏZƏDƏN (yeni GET sorğusu ilə) açdı — "Nümayəndənin"
VƏ "Hesab imza hüququ olan şəxsin" blokları artıq AVTOMATİK DOLU gəldi.
Diqqət: ARTIQ AÇIQ olan köhnə (GRANT-dan əvvəl yüklənmiş) səhifədə "Saxla"ya
basmaq kifayət ETMƏDİ — ilk sınaqda hələ boş dəyərlər saxlanıldı, çünki GRANT
geriyə, artıq brauzerdə yüklü olan formaya təsir etmir; YALNIZ səhifəni
TƏZƏDƏN açmaq (yeni GET → yeni Oracle sorğusu, indi icazəli) düzgün nəticə
verdi. Bütöv zəncir bununla TAM bağlandı: `odb.regnom`-un bəzi hüquqi
şəxslərdə sətri olmaması → `BalschtDaxilindedirAsync` əsaslı yenidən dizayn →
bunun da ORA-01031 ilə səssiz uğursuzluğu → loglama ilə kök səbəbin tapılması →
GRANT SELECT → iş axını uçdan-uca işlək.

## GOAML — "Təmizlə" Düymələri + Təsdiqlənmiş Qeydin Silinməsi (09.11.2026)

CON/SIG avtomatik doldurma işə düşəndən dərhal sonra istifadəçi real ekranda
(hüquqi şəxsin "əlaqəli şəxslərdən seçin" dropdown-u) narahat oldu: *"işçi
birini seçdi, amma yadına düşdükü başqa adam idi, əl ilə yazmalıdır, onda
gərək bütün məlumatı əl ilə silə"* — ~20 xananı tək-tək təmizləmək əvəzinə
bir kliklik "Təmizlə" düyməsi istədi. Əlavə olaraq: *"təsdiqlə siyahısından
silmək olsun, bəlkə sonra bildi ki ehtiyac yoxdur"*.

**"Təmizlə" — hər bloka ÖZ düyməsi, ortaq JS funksiyası.** `Tesdiq.cshtml`-in
"Nümayəndənin" VƏ "Hesab imza hüququ olan şəxsin" bloklarının hər ikisinin
`<summary>` başlığına (sağda, chevron-dan əvvəl) bir "Təmizlə" düyməsi əlavə
olundu. `gmlBlokTemizle(btn)` — `btn.closest('.gml-block')` ilə HANSI blokda
basıldığını tapır, YALNIZ o blokun `.gml-block-body`-sindəki bütün
`<input>`-ları boşaldır, `<select>`-ləri (CON blokundakı "əlaqəli şəxsdən
seç" daxil) ilk seçimə qaytarır — QONŞU BLOKA TOXUNMUR. `onclick="event.
preventDefault(); ..."` MƏCBURİDİR — düymə `<summary>`-nin DAXİLİNDƏDİR,
`preventDefault` olmasa klik native `<details>` aç/bağla davranışını da
işə salardı (blok bağlanardı, təmizlənmədən).

⚠️ `fn-btn--danger` sinfi layihədə VAR, amma YALNIZ `hr/xerc.css`/`hr/isci.css`-
də tərif olunub — GOAML bu faylları keçirmədiyi üçün işlətsəydik sinif
stilsiz qalardı (bu sessiyada dəfələrlə rast gəlinən "paylaşılan sinif,
keçirilməyən CSS" tələsinin eyni forması). Əvəzinə `Siyahi.cshtml`-in ÖZ
`<style>` blokuna kiçik, yerli `.gml-btn-danger` əlavə edildi.

**Silmə — YALNIZ hələ ixrac edilməmiş qeydlər üçün, yumşaq.**
`IGoamlService.SilAsync(int id)` — `GoamlEmeliyyat.Status != Qaralama`
olarsa rədd edir (`"Bu qeyd artıq ixrac edilib — silinə bilməz"`) — hazırda
`GoamlEmeliyyatStatusu`-da YALNIZ `Qaralama` dəyəri olduğu üçün bu qoruyucu
İNDİ HƏR ZAMAN keçir, AMMA "Export to file" (tapşırıq #15) yeni status
(ixrac edilib) əlavə edəndə kodda HEÇ BİR DƏYİŞİKLİK LAZIM OLMADAN avtomatik
işə düşəcək. `YumshakSilAsync(id)` (`EfRepositoryAsync`-in standart
`Silinib=true` metodu) çağırılır — `MovcuddurmuAsync`/`HamisiniGetirAsync`
avtomatik `!Silinib` filtrlədiyi üçün silinmiş qeyd nə siyahıda görünür, nə
də eyni namizədin YENİDƏN təsdiqlənməsini bloklayır (`TesdiqleAsync`-in
dublikat yoxlaması).

### Sual: "İşçi ixrac faylını hazırlayıb göndərdi, bir neçə gün sonra yenə buradadırsa, təkrar göndərilməyəcək?"

**Cavab: Bu artıq dizaynın özündə nəzərdə tutulub, AMMA ÖZÜ HƏLƏ qurulmayıb
(tapşırıq #15).** `GoamlEmeliyyatStatusu` enum-unun ÖZ şərhi
(`FinNex.Domain/Entities/Goaml/GoamlEnums.cs:6`) bunu açıq deyir: *"Operator
təsdiqləyib, hələ heç bir ixraca daxil edilməyib"* — yəni bu status sahəsi
məhz BU sual üçün əvvəlcədən hazırlanıb, sadəcə hələ YALNIZ "Qaralama" dəyəri
var. "Export to file" qurulanda:
1. İxrac YALNIZ `Status == Qaralama` olan qeydləri götürəcək;
2. Fayl yaradıldıqdan SONRA həmin qeydlərin statusu yeni bir dəyərə (məs.
   `IxracEdilib`) keçiriləcək;
3. Növbəti ixrac artıq bu qeydləri GÖRMƏYƏCƏK — təkrar göndərmə riski
   STRUKTUR SƏVİYYƏSİNDƏ bağlanmış olacaq, əlavə "göndərilmiş siyahı" saxlamaq
   lazım gəlməyəcək.

Yəni HAZIRDA (Export to file hələ qurulmayıb) bütün təsdiqlənmiş qeydlər
"Qaralama" olaraq qalır — gələcəkdə bu statusu dəyişdirən məxsusi mexanizm
yazılana qədər TƏKRAR İXRAC RİSKİ VAR (əgər kimsə eyni siyahını əl ilə iki
dəfə Excel-ə köçürsə). Bu, tapşırıq #15-in QURULMASI ilə avtomatik həll
olunacaq, bu sessiyada EDİLMƏDİ.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə/tag balansı Python skriptlə — `<details>`/`<summary>`/`<span>`/
`<form>`/`<script>` hər biri cüt sayda tapıldı, `git diff`-in yalnız YENİ
sətirləri 13/13 mötərizə, 1/1 fiqurlu mötərizə ilə balanslıdır). İstifadəçi
Visual Studio-da **Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır:
(1) "Nümayəndə" blokunda dropdown-dan birini seçib sonra "Təmizlə"yə basanda
BÜTÜN CON sahələrinin (və dropdown-un özünün) boşaldığını, "Hesab imza
hüququ" blokuna TOXUNULMADIĞINI; (2) "Təsdiqlənmiş əməliyyatlar" siyahısında
"Sil"ə basıb təsdiq pəncərəsini keçəndə qeydin siyahıdan yoxa çıxdığını və
EYNİ namizədi YENİDƏN axtarıb təsdiqləyə bildiyini (dublikat bloku
olmadığını).

## GOAML — CON ("Nümayəndə") Mənbəyi Səhv İdi — Düzgünü `odb.NUMAYENDE` (10.11.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi "Risk" tabını araşdırarkən (`v$open_cursor` diaqnostikası, SID=145)
AYSEL-in Risk-in namizəd populyasiyasını qurarkən işlətdiyi UNION-da
(`COUNTRY_CODES_RED/GRAY`-ə uyğunluq yoxlaması) **`ODB.NUMAYENDE`** adlı,
bizim HƏLƏ TOXUNMADIĞIMIZ bir cədvəl aşkar olundu. İstifadəçi: *"numayende
olani men sen demisdim, hesabin ayrica numayendelerini saxlayan table var,
burda regnom var o da REGNOM-dakı regnoma bağlıdır"* — yəni bu, `imza_huquqi_
olan_shexsler` (imza HÜQUQU — fərqli konsept) ilə QARIŞDIRILMAMALI, AYRI,
"hesabın öz nümayəndələrini" saxlayan xüsusi reyestrdir.

**Kök səbəb:** 09.11.2026-da qurulan CON ("Nümayəndənin") avtomatik doldurma
(bax yuxarı "CON/SIG Avtomatik Doldurma Tapıldı" bölməsi) CON-un seçim
siyahısını **SƏHVƏN `imza_huquqi_olan_shexsler`-dən** qururdu — o sessiyada
"nümayəndəni" etibarlı tapmaq üçün ayrıca bir cədvəl axtarılmamışdı, ona görə
mövcud olan (SIG üçün artıq işlənən) cədvəl təkrar istifadə edilmişdi. Canlı
`SELECT * FROM odb.NUMAYENDE WHERE ROWNUM<=5` (istifadəçi tərəfindən işlədilib)
göstərdi ki, bu cədvəlin KYC sahələri (tam ünvan parçalanması — Küçə/Şəhər/
Rayon/Poçt indeksi/Ölkə ayrı-ayrı, sənəd detalları, telefon, **PEP** (`POLITICAL_
ANKET/PROGRAM/LOCAL/FOREIGN/POSITION`), **`SANCTION_SCREENING`**, `IMZA_BASHLAMA`/
`IMZA_BITME`) `imza_huquqi_olan_shexsler`-dən (cəmi 7 sahə) QAT-QAT ZƏNGİNDİR.

**Düzəliş — `GoamlService.HesabSahibiKycAsync`, hüquqi-şəxs qolu, İKİ MÜSTƏQİL
sorğuya ayrıldı:**
- **SIG** ("Hesab imza hüququ olan şəxs") — **TOXUNULMADI**, hələ də
  `imza_huquqi_olan_shexsler`-dən, "Rəhbər" rolu (`ID_RELATED_PERSONS=2`).
- **CON** ("Nümayəndə") seçim siyahısı — yeni `NumayendeSiyahisiAsync(regnomKodu,
  ct)` metodu, `odb.NUMAYENDE` cədvəlindən (`REGNOM` = hesabın regnom kodu,
  `IMZA_BITME IS NULL`). Dropdown etiketi (`Rollar` sahəsi) indi NUMAYENDE-nin
  öz `VEZIFESI` (vəzifə) sütunundan gəlir — əvvəlki `aml_related_persons`/
  `aml_setup_related_persons` rol-birləşməsi NUMAYENDE üçün ARAŞDIRILMAYIB
  (`imza_huquqi_olan_shexsler`-in öz ID_AML-i ilə eyni rol cədvəlinə bağlı
  olub-olmadığı bilinmir, risksiz "vəzifə" etiketi seçildi).
- Hər iki sorğu **MÜSTƏQİLDİR** — biri boş qayıdarsa o biri YENƏ DƏ doldurulur
  (əvvəlki kod `imza_huquqi_olan_shexsler` boş olanda bütün metodu erkən
  `return`lə dayandırırdı, NUMAYENDE-yə heç çatmırdı).

**`TarixiParseEt` möhkəmləndirildi** (`string?` → `object?` parametr) —
NUMAYENDE-nin tarix sütunlarının VARCHAR2 yoxsa `imza_huquqi_olan_shexsler`-in
`IMZA_BASHLAMA`/`IMZA_BITME`-si kimi əsl DATE olduğu bilinmədiyi üçün, metod
indi HƏR İKİ tipi tanıyır (`DateTime` gələrsə birbaşa, `string` gələrsə
əvvəlki parse zənciri ilə). Mövcud `regnom`/`imza_huquqi_olan_shexsler`
çağırışlarına (VARCHAR2 olduğu artıq təsdiqlənib) TOXUNULMADI.

⚠️ **Yazı səhvi tapılıb özüm düzəltdim (publish-dən əvvəl):** ilk yazılışda
sorğuda `n.vezifesi` düzgün yazılmışdı, amma oxuyarkən `DVal(r, "VEZISI")`
(hərf çatışmazlığı) yazılmışdı — bu, heç bir xəta vermədən `Rollar`-ı HƏMİŞƏ
boş qoyardı. CLAUDE.md-dəki "sütun adını əl ilə yazanda tutuşdur" qaydasının
məhz bu özüdür — kodu publish etməzdən əvvəl SQL-dəki sütun adları ilə `DVal`
çağırışlarını bir-bir tutuşdurub tapıldı.

⚠️ **NUMAYENDE-nin zəngin sahələri (PEP/sanksiya bayraqları, tam ünvan
parçalanması) HƏLƏ FORMAYA ƏLAVƏ EDİLMƏYİB** — `Tesdiq.cshtml`-in CON bloku
əvvəlki kimi yalnız əsas KYC sahələrini (ad/soyad/doğum/vətəndaşlıq/sənəd/
ünvan — bir sətirdə) doldurur. PEP/sanksiya bayraqlarını göstərmək/saxlamaq
ayrıca qərar tələb edir (yeni forma sahələri + `GoamlEmeliyyat` entity-sinə
yeni sütunlar) — bu sessiyada EDİLMƏDİ.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — düzəliş yalnız BUNDAN SONRAKI "Təsdiqlə" GET-
sorğularına təsir edir, artıq təsdiqlənmiş qeydlərin CON-u dəyişmir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə balansı — `git diff`-in əlavə/silinən sətirləri ayrıca ölçüldü: hər
ikisi eyni +1 fərqlə, yəni düzəlişin ÖZÜ net-neytraldır; SQL-dəki hər sütun
adı `DVal` çağırışı ilə bir-bir tutuşduruldu). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra NUMAYENDE-də qeydi olan bir hüquqi şəxsin hesabı
ilə namizəd açıb yoxlamalıdır: "Nümayəndənin" dropdown-unda indi NUMAYENDE-dəki
şəxslərin (vəzifə etiketi ilə) göründüyünü, seçimdən sonra CON xanalarının
düzgün dolduğunu, "Hesab imza hüququ olan şəxs"in (Rəhbər) əvvəlki kimi
avtomatik qaldığını.

## GOAML — "Risk" Tabı Quruldu, AYSEL-in Staging Cədvəlindən ASILI DEYİL (11.11.2026)

Növbəti tab (növbə: Əməliyyatlar → **Risk** → Export to file) — BMI-nin
AYSEL/GOAML-in "Risk" ekranının namizəd-seçim məntiqi `v$sql`/`v$open_cursor`
ilə canlı tutulub (10.11.2026), sonra isə AYSEL-in öz staging cədvəlindən
(`ODB.DOC_DNA_ALL_RISK_T`) **ASILI OLMAYAN** bir ekvivalent canlı Oracle-a
qarşı ADDIM-ADDIM doğrulanıb (11.11.2026).

### Kəşf — AYSEL "Risk" basanda NƏ BAŞ VERİR

Operator tarix daxil edib Filter basanda, HƏMİN tarix aralığı üçün:
1. `DELETE ODB.DOC_DNA_ALL_RISK_T` — staging TAMAMİLƏ boşaldılır.
2. `SELECT ... FROM ODB.DOC_DNA_ALL_RISK A, VIEW_LICSCH_ALL LD, VIEW_LICSCH_ALL LK
   WHERE DATE_OPER BETWEEN :B2 AND :B1 AND ... AND (LD.REGISTRAC_NOMER IN (<5
   cədvəllik UNION, COUNTRY_CODES_RED/GRAY-ə qarşı>) OR LK.REGISTRAC_NOMER IN (...))`
   — namizədlər **mənbə** cədvəldən (`DOC_DNA_ALL_RISK`) seçilir.
3. `INSERT INTO ODB.DOC_DNA_ALL_RISK_T(...) VALUES(..., NVL(NVL(:B11,:B10),:B9), ...)`
   — staginqə yazarkən `COUNTRYCODE_DE`/`COUNTRYCODE_KRE` sütunları ÜÇ-SƏVİYYƏLİ
   bir NVL zənciri ilə **zənginləşdirilir** (Oracle Forms-un öz, görünməyən
   trigger məntiqi — `v$sql`-dən yalnız bind yer tutucuları (`:B9`-`:B11`)
   görünür, ONLARI nəyin doldurduğu YOX).
4. `SELECT ... FROM odb.doc_dna_all_risk_t WHERE ... (COUNTRYCODE_KRE IN RED
   OR COUNTRYCODE_DE IN RED OR CC_DD IN RED OR emitent/benefisiar vətəndaşlığı
   OR balans-sinif cütü OR qara siyahı) AND DATE_OPER BETWEEN ...` — grid-i
   bu, staginqdən göstərir.

⚠️ **Staginq cədvəli SABİT KEŞ DEYİL** — hər Filter klikində O ANKI tarix
aralığı üçün YENİDƏN DELETE+INSERT olunur. FinNex Oracle-a YAZI apara
bilmədiyi üçün (layihənin kökü qaydası) bu cədvələ GÜVƏNMƏK TƏHLÜKƏLİDİR —
AYSEL dayandırılsa, ya sadəcə başqa bir tarix üçün işə düşsə, staginq bizim
üçün köhnə/boş qalar.

### Axtarış — addım-addım, canlı Oracle-da doğrulanıb

1. Mənə məlum WHERE-kriteriyası (#65-in özü) ilk olaraq `doc_dna_all_risk_t`-ə
   qarşı test edildi — AYSEL-in öz grid-i ilə 3-4 FƏRQLİ tarixdə **1:1 eynilik**
   verdi.
2. Eyni kriteriya birbaşa `ODB.DOC_DNA_ALL_RISK` (mənbə, staginq YOX) üzərində
   işlədildi — **6** sətir (staginqın verdiyi 9-dan AZ).
3. Fərqi tapmaq üçün staginqdəki 9 sətrin RECNUM-u ilə mənbədəki 6-nı
   `NOT IN` ilə tutuşduruldu — **3 RECNUM** (iki fərqli REGNOM: `017857`,
   `016719`) əskik çıxdı.
4. `ODB.VIEW_LICSCH_ALL.COUNTRYCODE` (hesabın ÖZ bank-tərəfi ölkəsi) ehtiyat
   mənbə kimi sınandı — say DƏYİŞMƏDİ (hələ 6). Yəni bu sütun deyil.
5. **Əsl tapıntı:** eyni iki REGNOM-u `ELAQELI_HUQUQI_SHEXSLER`/
   `imza_huquqi_olan_shexsler`/`NUMAYENDE`/`FIZIKI_SHEXS`-də axtaranda —
   `ELAQELI_HUQUQI_SHEXSLER.INCORPORATION_COUNTRY_CODE` = **"AZ"** (şirkətin
   öz qeydiyyat ölkəsi, TƏMİZ), amma `imza_huquqi_olan_shexsler`/`NUMAYENDE`-nin
   **vətəndaşlıq** (`VETENDASHLIGI`) sahəsində HƏM "AZ", HƏM "**IR**" (İran)
   qeydləri VAR idi — yəni hesabın öz qeydiyyatı təmiz olsa da, ona bağlı
   imza sahibi/nümayəndənin vətəndaşlığı risklidir. Bu, #13-ün (candidate-
   populate sorğusu) ÖZ WHERE şərtinin artıq istifadə etdiyi 4 cədvəldir.
6. **Kod format tələsi** (iki uğursuz cəhddən sonra tapıldı): `COUNTRY_CODES_RED`-in
   **İKİ** sütunu var — `VALUE_C` (3-hərfli, "AZE") və `VALUE_C_2` (2-hərfli,
   "AZ"/"IR"). KYC cədvəllərinin (ELAQELI/IMZA/NUMAYENDE/FIZIKI) dəyərləri
   2-hərflidir — `VALUE_C_2`-yə baxılmalıdır (`r_emitent_benefisiar`-ın öz
   vətəndaşlıq şərti artıq bunu belə işlədirdi, mən səhvən əvvəlcə `VALUE_C`
   yazmışdım). Düzəldikdən sonra say **9** çıxdı — staginqla DƏQIQ EYNİ.

### Həll — `docs/sql/goaml/91_Goaml_Risk_OracleSorgular.sql` (`GOAML_RISK_NAMIZED`)

`ODB.DOC_DNA_ALL_RISK` (mənbə, staginq YOX) üzərində, yuxarıdakı tam doğrulanmış
WHERE-i daşıyan yeni, MÜSTƏQİL saxlanmış Oracle sorğusu. Sütun sırası
Əməliyyatlar-ın `GOAML_EMELIYYAT_NAMIZED`-i ilə EYNİDİR (19 sütun,
`GoamlNamizedSetirDto`-nun sahə sırasına uyğun — POZİSİYA ilə oxunur) ki,
İKİ tab eyni DTO-nu, eyni "Təsdiqlə" axınını paylaşa bilsin.

**`GoamlService.NamizedAxtarAsyncCore`** — Əməliyyatlar VƏ Risk-in ORTAQ
namizəd-axtarış mexanizmi (sorğu adı + sql fayl yolu parametrləşdirilib,
əvvəlki `NamizedAxtarAsync`-in bütün gövdəsi bura köçürülüb) — iki tabın
kodu TƏKRARLANMASIN deyə. `IGoamlService.RiskNamizedAxtarAsync` yeni action.

**`Areas/Goaml/Controllers/RiskController.cs`** (yeni) — YALNIZ `Index`+`Axtar`
daşıyır. **"Təsdiqlə"/"Təsdiqlənmiş siyahı"/"Detal"/"Sil" TƏKRARLANMAYIB** —
Risk-in namizədi `GoamlNamizedSetirDto` ilə EYNİ formada olduğu üçün "Təsdiqlə"
düyməsi birbaşa `EmeliyyatlarController.Tesdiq`-ə yönləndirir (cross-controller,
`@Url.Action("Tesdiq", "Emeliyyatlar")`) — iki ayrı Tesdiq ekranı/axını
YARADILMADI, GoamlEmeliyyat cədvəlinə yazma HƏR İKİ tab üçün TƏK yoldur.

`Areas/Goaml/Views/Risk/Index.cshtml` — Əməliyyatlar/Index.cshtml-in EYNİ
naxışı (axtarış forması, AJAX, köhnəlmiş-cavab qoruyucusu, sütun sıralama,
sessionStorage filtr yaddaşı — **AYRI açar** `gmlRiskSonAxtaris`, Əməliyyatlar-ın
`gmlSonAxtaris`-i ilə QARIŞMASIN deyə), + yeni "Ölkə (D/K)" sütunu
(`COUNTRYCODE_DE`/`KRE` göstərir, varsa — vətəndaşlıq-əsaslı 3 sətirdə bu
sütun hələ də BOŞ görünəcək, çünki `COUNTRYCODE_DE/KRE` yalnız bəzi sətirlərdə
mənbədə birbaşa dolu idi, qalanlar YALNIZ vətəndaşlıq-şərti ilə tutulur —
görüntüdə "niyə risk?" sualına HƏR zaman tam cavab vermir, yalnız KÖMƏKÇİ
kontekstdir).

**Dashboard/Index.cshtml** — tab siyahısının tuple-u `Controller` sahəsi
aldı (əvvəl yalnız "Əməliyyatlar" üçün hardcode `if` idi) — hər hazır tab
İNDİ öz controller adını daşıyır, `if (t.Hazir && t.Controller != null)` ilə
ÜMUMİ şəkildə keçid verilir. "Risk" sətri `Hazir=true, Controller="Risk"`.

**Sidebar** (`_UserLayout.cshtml`, `case "goaml"`) — "Əməliyyatlar"ın
ardınca yeni "Risk" sətri (`bi-shield-exclamation`).

⚠️ **AÇIQ QALAN, KİÇİK UX BOŞLUĞU — "Geri"/"Ləğv et" Risk-dən DEYİL,
HƏMIŞƏ Əməliyyatlar-ın Index-inə qayıdır.** `Tesdiq.cshtml`-dəki bu linklər
`asp-action="Index"` (controller-siz, CARİ controller-ə — yəni həmişə
`EmeliyyatlarController`-ə) yazılıb. Risk-dən bir namizəd təsdiqlənəndə/ləğv
ediləndə operator Risk/Index-ə YOX, Əməliyyatlar/Index-ə qayıdır (öz Risk
filtri sessionStorage-da qalır, amma avtomatik bərpa olunmur — yalnız Risk
səhifəsinə ƏL İLƏ qayıdanda bərpa olunur). Qəsdən DÜZƏLDİLMƏDİ — "Tesdiq/
Siyahi/Detal/Sil TƏKRARLANMASIN" prinsipi ilə ziddiyyətə düşməmək üçün; lazım
gələrsə `Tesdiq` GET/POST-a bir `qaynaqTab` query parametri əlavə edib "Geri"
linkini dinamik (Emeliyyatlar vs Risk) etmək mümkündür — bu sessiyada edilmədi.

⚠️ **FAZA 1 (paralel dövr) qaydası BURADA DA keçərlidir** — AYSEL hələ
işlədiyi müddətcə `ODB.DOC_DNA_ALL_RISK`-i O doldurur, biz YALNIZ OXUYURUQ.
AYSEL dayandırılandan sonra bu sorğu yenidən qurulmalı ola bilər (eyni
Əməliyyatlar sorğusunun qeydi kimi).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə — `git diff`-in TƏK BU faylın əlavə/
silinən sətirləri ayrıca ölçüldü, hər ikisi eyni -2 paren-imbalance daşıyır,
yəni düzəlişin ÖZÜ net-neytraldır; interfeys↔implementasiya↔controller↔view
dörd qatı sahə adı-adı əl ilə tutuşduruldu, RZ1031-ə səbəb ola biləcək "adsız
bare `@(...)` atribut" nümunəsi hər üç yeni/dəyişən `.cshtml`-də `grep` ilə
axtarılıb tapılmadı). İstifadəçi Visual Studio-da **Rebuild All** ilə, SQL
faylını SQL Server-də işlətdikdən sonra real ssenari ilə yoxlamalıdır: "Risk"
sidebar sətrinə basıb bir tarix aralığı axtarsın → nəticənin sayının (və
sətirlərin) bu sessiyada canlı Oracle-da yoxlanmış rəqəmlə (eyni tarix
aralığında) üst-üstə düşdüyünü, "Təsdiqlə"nin Əməliyyatlar-ın öz Tesdiq
ekranını düzgün açdığını, oradan təsdiqlədikdən sonra "Təsdiqlənmiş siyahı"da
göründüyünü təsdiqləməlidir.

### `RiskController.cs` — `using FinNex.Domain;` Unudulub, CS0103 (11.11.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi real Visual Studio-da bu dəyişikliyi açar-açmaz **CS0103**
(`The name 'IcazeKodlari' does not exist in the current context`) aldı —
dəqiq CLAUDE.md-nin "Qovluq Adı ≠ Namespace" bölməsindəki tələ. `IcazeKodlari`
`FinNex.Domain` (kök namespace) daşıyır, `EmeliyyatlarController.cs`-in özü
artıq `using FinNex.Domain;` yazır (oranı yoxlamışdım), amma `RiskController.cs`-i
yazarkən həmin konkret sətri köçürməyi UNUTMUŞAM — fayl-ümumi brace/paren
balansı (yoxladığım yeganə şey, `dotnet build` olmadığı üçün) bu cür SƏHV
`using`-i HEÇ VAXT tuta bilməz, çünki o, sintaktik olaraq tam düzgün koddur.

**Düzəliş:** `using FinNex.Domain;` əlavə edildi.

**Qayda (təkrar, bu modulda İKİNCİ dəfə): eyni `[Icaze(IcazeKodlari...)]`
atributunu YENİ bir controller-ə köçürəndə, mövcud bir controller-dən
(`EmeliyyatlarController.cs`) KOPYALAMAQ əvəzinə YAZANDA onun BÜTÜN
`using`-lərini (təkcə görünən/işlənən tiplərinkini yox) sətir-sətir
tutuşdurmaq lazımdır** — mötərizə balansı YALNIZ sintaksisi yoxlayır, bir
tipin HANSI namespace-dən gəldiyini YOX.

## GOAML — Əməliyyatlar Tabı `DOC_DNA_ALL_TAB`-DAN ASILI İMİŞ — AYSEL BAŞQA TARİXİ FİLTR EDƏNDƏ KÖHNƏ GÜN YOX OLURDU (11.11.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi real mühitdə Əməliyyatlar tabında bir tarixlə axtarış edib "0 namizəd"
aldı, AYSEL-də EYNİ tarixlə filter edəndən sonra bizim tətbiqə qayıdanda nəticə
göründü — sual: *"görəsən GOAML hansısa sorğunun əsasında hər filterdə bazaya
yazır və onu gətirir?"* Haqlı çıxdı.

**Kök səbəb — `P_DOC_DNA_GOAML`-in tam mənbə kodu (`all_source`, 11.11.2026-da
canlı Oracle-dan çıxarılıb) göstərdi ki, 06.11.2026-dakı "DOC_DNA_ALL_TAB
biznes qaydaları ilə filtrlənmiş namizəddir" fərziyyəsi SƏHV İDİ:**

```sql
delete odb.doc_dna_all_tab;   -- ŞƏRTSİZ, BÜTÜN cədvəli boşaldır (öz filialına görə filtr şərhə alınıb)
commit;
insert into odb.doc_dna_all_tab (...)
select distinct ...
  from odb.doc_dna_all_risk   -- YEGANƏ mənbə, HEÇ BİR GL/netting filtri YOXDUR
 where date_oper between p_date_s and p_date_po;
commit;
```

`DOC_DNA_ALL_TAB` — AYSEL-də **İSTƏNİLƏN istifadəçi İSTƏNİLƏN tarix aralığı
üçün** bu prosedura müraciət etdikdə TAM SİLİNİR və YALNIZ o tarix aralığının
sətirləri ilə yenidən doldurulan, **qlobal, paylaşılan, sıfır dayanıqlılıqlı
"scratch" cədvəldir** — Risk tabının mənbəyi `DOC_DNA_ALL_RISK`-dən (06-11.11.2026-da
canlı doğrulanıb, sabit/müstəqildir) KƏSKIN FƏRQLİDİR. 06.11.2026-da "GL/netting
istisna qaydaları `P_DOC_DNA_GOAML`-dən gəlir" deyilmişdi — bu da YANLIŞ idi:
həmin qaydalar əslində AYSEL-in GÖSTƏRMƏ sorğusundandır (v$sql ilə ayrıca
tutulub), `P_DOC_DNA_GOAML`-in ÖZÜ heç bir belə filtr tətbiq etmir.

**Niyə əvvəlki "Risk" araşdırması bunu üzə çıxarmadı:** Risk-in sorğusu başdan
`DOC_DNA_ALL_RISK`-dən oxuyurdu (heç vaxt `DOC_DNA_ALL_TAB`-a toxunmadı), ona
görə bu problemi görə BİLMƏZDİ — iki tab fərqli mənbədən oxuduğu üçün eyni
`v$sql`/canlı-doğrulama metodu bir tabda düzgün nəticə verib o birini aşkar
etməmişdi.

**Həll — mənbə `DOC_DNA_ALL_RISK`-ə dəyişdirildi, `DOC_DNA_ALL_TAB`
TAMAMİLƏ TƏRK EDİLDİ.** `P_DOC_DNA_GOAML`-in öz kursoru göstərdi ki,
`DOC_DNA_ALL_RISK`-in ÖZÜ onsuz da bütün lazım olan sütunları (`NAME_LICSCH_DE/KRE`,
`REGISTRAC_NOMER_DE/KRE`, `DOCUMENT_SOURCE` daxil) NATİV daşıyır — `odb.arh_dd`-yə
enib P_DOC_DNA_GOAML-in (əslində mövcud olmayan) biznes qaydasını yenidən qurmaq
lazım olmadı. `docs/sql/goaml/90_Goaml_OracleSorgular.sql` yeniləndi:
`from odb.doc_dna_all_tab t left join odb.doc_dna_all_risk r on ...` →
`select distinct ... from odb.doc_dna_all_risk t` (GL/netting istisna şərtləri
DƏYİŞMƏDİ, `name_licsch_de/kre`/`registrac_nomer_de/kre` indi `r.` yox `t.`-dən
oxunur). `SELECT DISTINCT` P_DOC_DNA_GOAML-in öz kursorundakı `distinct`-lə EYNİ
məqsədə xidmət edir — `DOC_DNA_ALL_RISK`-də bir əməliyyat birdən çox risk-səbəbinə
görə təkrarlana bilər, bizim seçdiyimiz (risk-səbəb sütunu olmayan) sütunlarda
sadə DISTINCT bunu düzgün toplayır.

**Əlavə tapıntı (gələcək üçün) — `ID_VD` `DOC_DNA_ALL_RISK`-in NATİV
sütunudur.** "Update New" kaskadının 7 ATLANAN qaydasının (bax yuxarı, "'Update
New' Avtomatik Doldurma Mexanizmi Köçürüldü" bölməsi) hamısı `ID_VD`-yə bağlı
idi, mənbəyi bilinmirdi — indi aydındır: `ID_VD` elə `DOC_DNA_ALL_RISK.ID_VD`-dir,
birbaşa namizəd sorğusundan əlçatandır. Bu qaydalar toxunulanda artıq ayrıca
Oracle araşdırmasına ehtiyac yoxdur.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — bu düzəlişdən ƏVVƏL "0 namizəd" görünüb keçmiş
hər hansı axtarış (və onun nəticəsində təsdiqlənməmiş qalmış real əməliyyatlar)
avtomatik bərpa olunmur, operator sadəcə YENİDƏN axtarış etməlidir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(yeni SQL mətninin sütun sırası `GoamlNamizedSetirDto`-nun sahə sırası ilə
bir-bir tutuşduruldu — `C#` kodu HEÇ dəyişmədi, yalnız `docs/sql/goaml/
90_Goaml_OracleSorgular.sql` SQL Server-də YENİDƏN işlədilməlidir ki,
`OracleSorgular` cədvəlindəki `GOAML_EMELIYYAT_NAMIZED` sətri yenilənsin).
İstifadəçi faylı SQL Server-də işlətdikdən sonra real ssenari ilə yoxlamalıdır:
AYSEL-ə HEÇ TOXUNMADAN, birbaşa Əməliyyatlar tabında əvvəllər "0 namizəd"
göstərən bir tarixlə (məs. 03.11.2026) axtarış edib indi nəticə gəldiyini
təsdiqləməli, sonra AYSEL-də BAŞQA bir tarixlə filter edib bizim tətbiqə
qayıdaraq ƏVVƏLKİ tarixin NƏTİCƏSİNİN DƏYİŞMƏDİYİNİ (artıq AYSEL-dən asılı
olmadığını) yoxlamalıdır.

## GOAML — Export to JSON (#15) — MVP Qurulub, `JSON_MMX_IN`-ə Əsaslanır (12.11.2026)

İstifadəçi: *"baslayaq.\nisci arasdirir,nese olsa deyecek"* (Export-a başlayaq,
işçi paralel olaraq Əməliyyatlar/Risk datasını yoxlayır). AYSEL-in `JSON_MMX_IN`
prosedurunun (11.11.2026-da arxivlənmiş `docs/sql/goaml/97_Goaml_JSON_MMX_IN_
Tam_Metn.sql`) **YALNIZ "_IN" (GƏLƏN əməliyyat) variantı** FinNex-ə köçürüldü —
"_SEND" (gedən) HƏLƏ YOXDUR, o prosedurun mətni heç araşdırılmayıb.

### Yeni sahə — `DocumentSource`, Təsdiqlə axınına əlavə edildi

Namizəd axtarışı `DOC_DNA_ALL_RISK.DOCUMENT_SOURCE`-u onsuz da daşıyırdı
(`GoamlNamizedSetirDto.DocumentSource`), amma `GoamlEmeliyyat`-a (Təsdiqlədikdən
sonra qalıcı yazılan sətir) HEÇ VAXT YAZILMIRDI — bu, Export-un trn_type/
trn_payment_system/trn_chanel təyini üçün MƏCBURİDİR (`document_source` mətn-
uyğunluğu). Əlavə olundu: `GoamlEmeliyyat.DocumentSource` (migration
`20261112080000_GoamlDocumentSource.cs`, yalnız `AddColumn`), `GoamlTesdiqDto.
DocumentSource`, `EmeliyyatlarController.Tesdiq` GET-in yeni parametri,
`Tesdiq.cshtml`-də gizli `<input>`, `Index.cshtml`-in "Təsdiqlə" linkinin
query-string-inə əlavə — beş qat da "Şərtli Render Olunan Form Sahəsi" qaydasına
uyğun: operator ƏL İLƏ YAZMIR, gizli sahə kimi axır.

### Status — `GoamlEmeliyyatStatusu.IxracEdilib` (yeni, =2)

`SilAsync`-in artıq əvvəldən (09.11.2026-dan) daşıdığı qoruyucu şərh
(*"YALNIZ hələ ixrac edilməmiş (Status==Qaralama) qeydlər üçün"*) BU DƏYƏRİ
gözləyirdi — indi həqiqətən işlənir: Export uğurla bitəndə ixrac edilən
qeydlərin `Status`-u `IxracEdilib`-ə keçir, `JsonIxracAsync`-in öz sorğusu
YALNIZ `Status==Qaralama` oxuduğu üçün **növbəti ixrac eyni qeydi TƏKRAR
GÖTÜRMƜZ** — "ikiqat ixrac"ın strukturaca qarşısı alınıb.

### `JSON_MMX_IN`-dən canlı yoxlanmış mapping-lər (hamısı prosedurun ÖZÜNDƏN,
### faylın 900-1403-cü sətirlərindən, yoxlanıb təsdiqlənib)

| JSON sahə | Mənbə (bizim entity) | Qeyd |
|---|---|---|
| `sender_entity_*` | `NameUr`/`IncorporationCountryCode`/`IncorporationNumber` | Sender VƏ Receiver Entity EYNİ `v_goal.Name_Ur`-dan oxunur (Tip-ə görə mütəqabil-eksklüziv) |
| `sender_first/last/middle_name` | `ToAdi`/`ToSoyadi`/`ToAtasininAdi` | "_IN" prosedurunda "Alan fiziki şəxsin" (To) sahələri Sender kimi yazılır — **sadə görünən, amma TƏSDİQLƏNMİŞ Oracle davranışıdır** |
| `sender_id_serial/number/type` | `RecIdSerial`/`RecIdNumber`/`RecIdType` | "Rec" = Receiver DEYİL, bu konkret 3 sahədə Sender-in sənəd nömrəsi kimi işlənir (prosedur mətnindən hərfi) |
| `sender_pin_code` | `SnnTo` | |
| `sender_tax_id` | `IncorporationNumberTo` | |
| `receiver_first/last/middle_name` | `SigAdi`/`SigSoyadi`/`SigAtasininAdi` | Receiver = "Hesab imza hüququ olan şəxs" (bizim bankın müştərisi) |
| `receiver_id_serial/number/type` | `SenderIdSerial`/`SenderIdNumber`/`SenderIdType` | Tərs — bu 3 sahə Receiver üçün işlənir |
| `receiver_pin_code` | `Snn` | |
| `receiver_citizenship` | `SigVetendasligi` | Oracle-da `v_goal.nationality1` (suffikssiz) |
| Correspondence `debet`/`Credit` | `Debet`/`Kredit`-in ilk 5 simvolu | |

**`trn_type`/`trn_payment_system`/`trn_chanel`** — `document_source`-dəki
`instr(...,'TOKEN')>0` mətn-uyğunluğu birə-bir C#-a köçürüldü (`Has()`/`Pref3()`
köməkçiləri). `trn_chanel` AYRICA diqqət tələb edir — Oracle-da bunlar **ardıcıl
`if`-lərdir (elsif DEYİL)**, SON uyğun gələn qalib gəlir — C# kodu da bu
ardıcıllığı EYNİ sıra ilə təkrarlayır.

### Oracle-a YALNIZ İKİ əlavə canlı sorğu (hər ikisi yalnız-oxu, keşlənir)

- **`odb.kurval.SOKNAMEVALUT`** — hesab nömrəsinin 6-7-ci simvolundan (sabit
  mövqe) valyuta kodu — `func_get_Ekv`-in ekvivalenti. Bu sessiyanın ƏVVƏLİNDƏ
  (Proqres "Hesablar" ekranını v$sql ilə araşdırarkən) tapılan STRUCTURE_LICSCH→
  KURVAL zəncirinin İKİNCİ yarısıdır.
- **`odb.malumatin_novu`** — REPORT_CODE üzrə `reportType`/`reportReason`
  (header-in yeganə iki məzmunlu sahəsi).

### ⚠️ BİLƏRƏKDƏN PORT EDİLMƏYƏN (bu artımın qapsamı XARİCİNDƏ, `GoamlService.
### JsonIxracAsync`-in baş şərhində VƏ `Export.cshtml`-də AÇIQ qeyd olunub)

- **"_SEND" (gedən əməliyyat) proseduru HEÇ ARAŞDIRILMAYIB** — Export hazırda
  YALNIZ "_IN" məntiqini tətbiq edir. Gedən əməliyyat üçün AYSEL-in ayrıca
  `JSON_MMX_SEND`/`JSON_MMX_SEND_H_B` prosedurları var (06.11.2026-da adları
  tapılıb, mətnləri OXUNMAYIB) — bu, gələcək, ayrıca iş.
- **`dubl_number`** (hüquqi şəxsin VÖEN üzrə ikinci qeydiyyat nömrəsi,
  `huquqi_shexs.REGISTRATION_NUMBER`) — boş qalır, canlı lookup yazılmayıb.
- **QHT/PEP/BLR header sahələri** (tin, hrzCountry, customerRiskDegree,
  typeOfConnectionToPep/Hrz, sphereOfActivity və s.) — bunlar `v_fizik`/
  `v_imza_huquq`/`v_REGNUMBER`/`v_PV_HUQUQI`/`v_cif`/`v_emitent_benefisiar`
  kimi ƏLAVƏ altı-yeddi Oracle kursoru tələb edir, heç biri YOXDUR. Header
  YALNIZ `sendDate`/`reportType`/`reportReason` daşıyır.
- **`trn_payment_system`-in dəqiq (UTF/XKS/SWF/AZP/AOS) alt-təyini** — `ID_VD`/
  `OPCODE_2`/`pla_sys` cursoruna bağlıdır, bizim namizəd sorğumuzda (bax
  11.11.2026-dakı düzəliş) `ID_VD` artıq ƏLÇATANDIR (`DOC_DNA_ALL_RISK`-in
  nativ sütunu kimi tapıldı), AMMA bu artımda İSTİFADƏ EDİLMƏYİB — defolt
  `CBB` qalır.
- **Bizim öz bankımızın hesabına aid V_YURIK__NAME/ATTRIB/C_INN_BANK
  zənginləşdirilməsi** — Account bloklarının bank-adı/VÖEN sahələri bizim ÖZ
  entity-mizdəki (InstitutionName/Swift/...) dəyərlərdən qurulur, Oracle-un
  köməkçi reyestr join-ləri YOXDUR.
- **Representative-in "SIG" qolu** — Oracle-un öz mənbəyində DEAD CODE-dur
  (əvvəlki `NRP` şərtinin `OR`-u onsuz da bu halı tutur), bilərəkdən
  köçürülməyib — yalnız PAT/NRP var.

### Fayllar

`FinNex.Application/DTOs/Goaml/GoamlJsonIxracDto.cs` (yeni — tam goAML JSON
sxemi, HƏR sahə `[JsonPropertyName]` ilə açıq işarələnib, System.Text.Json-un
default naming policy-sinə GÜVƏNİLMİR, çünki sxem özü qarışıq-casing-lidir:
header=camelCase, blok açarları=PascalCase, leaf sahələr=snake_case — bu
BİZİM seçimimiz deyil, goAML-in xarici sxemidir). `IGoamlService.JsonIxracAsync` +
`GoamlService.JsonIxracAsync` (+ 10 private köməkçi: `ReportQurAsync`,
`TransactionQur`, `TrnTipTeyinEt`, `TrnOdenisSistemiTeyinEt`, `TrnKanalTeyinEt`,
`RepresentativeQur`, `SenderQurAsync`, `ReceiverQurAsync`, `ValyutaVeMeblegAsync`,
`ValyutaKoduAsync`, `MalumatinNovuTapAsync`). `EmeliyyatlarController.Export`
(GET+POST) — JSON mətnini **həm** `C:\FinNex_DMS\goaml-ixrac\`-ə yazır (best-
effort, "Fayl Yükləmə" qaydasına uyğun, `wwwroot`-a YOX), **həm** birbaşa
brauzer-yükləməsi kimi qaytarır (`File(bytes, "application/json", ad)`).
`Export.cshtml` (yeni) — tarix aralığı + istəyə bağlı Tip/ReportCode filtri,
yuxarıdaki qapsam-xaric siyahısını açıq göstərir. Dashboard/Index.cshtml-in
tab siyahısı + sidebar-a "Export to file" sətri əlavə olundu (`Emeliyyatlar/
Export`-a keçid).

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
— hər DTO sahə adı `97_Goaml_JSON_MMX_IN_Tam_Metn.sql`-in müvafiq sətirləri ilə
BİR-BİR (xüsusilə Sender/Receiver-in çarpaz Rec/Sender_Id_* qarışıqlığı) təkrar
oxunub təsdiqləndi, mötərizə balansı yoxlanıldı (yeni əlavə olunan sətirlər
261/261 açıq/bağlı parentez ilə tam balanslıdır). İstifadəçi Visual Studio-da
**Rebuild All**, `Update-Database`, sonra real ssenari ilə yoxlamalıdır: bir
neçə TƏSDİQLƏNMİŞ (Qaralama statusunda) qeyd üçün Export et → JSON-un
`reports[].transactions[0]`-da Transaction/Sender/Receiver/Correspondence
bloklarının mənalı dəyərlərlə doldurulduğunu, ixrac edilən qeydlərin
"Təsdiqlənmiş əməliyyatlar" siyahısında artıq görünmədiyini (və ya statusunun
"İxrac edilib" göstərdiyini) təsdiqləməli — sonra YENİDƏN Export edib EYNİ
qeydlərin TƏKRAR gəlmədiyini yoxlamalı. **Yaradılan JSON-u real goAML
portalına göndərmədən ƏVVƏL mütləq əl ilə, yuxarıdaki qapsam-xaric siyahısına
görə yoxlamalıdır** — bu, tam, istehsala-hazır ixrac DEYİL, ilk artımdır.

## GOAML — CON/SIG "Debet-ə Bax, Faydalı Olsa Dayan" SƏHV İDİ — İndi Tip-in İstiqaməti Seçir (12.11.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi canlı bir hadisə göstərdi: Debet=35025, Kredit=40140, hər ikisi
hüquqi şəxs hesabı. "İmza hüququ olan şəxs" (SIG) boş gəldi, baxmayaraq ki
digər tərəfdə (40140) real Rəhbər var idi. Sual: *"sen bu halda hansi
hesabin rehberini axtarirsan? ... eger burda 35 esas goturmusukse bu
sehvdir"*.

**Kök səbəb — `HesabSahibiKycTapAsync` (09-10.11.2026) "Debet-ə bax, faydalı
nəticə olsa DAYAN" qaydası idi:**
```
faydalı = TekSexs (Rəhbər) VAR  VƏ YA  ElaqaliSexsler (Nümayəndə siyahısı) BOŞ DEYİL
```
Bu, YANLIŞ idi: 35025-in `NUMAYENDE`-də (CON) bir-iki sətri ola bilər, AMMA
`imza_huquqi_olan_shexsler`-də aktiv "Rəhbər" (SIG) qeydi OLMAYA bilər — bunlar
İKİ AYRI cədvəldir. Kod "CON var, demək bu hesab KİFAYƏTDİR" deyib **Kredit-ə
(40140) HEÇ BAXMIRDI** — halbuki əsl Rəhbər elə orada idi.

### İstifadəçinin təklifi — Tip-in istiqaməti (OUT/IN) qərar versin

İlk təklifim ("hər iki tərəfi yoxla, SIG və CON-u AYRI-AYRI, hansında
tapılarsa ORADAN götür") natamam idi — istifadəçi haqlı etiraz etdi: *"huquqi
hesasbdan diger huquqi hesaba emeliyyat olsa sistem hansini goturecek? her
ikisininde rehberi var"*. Yəni iki tərəf HƏR İKİSİ faydalı nəticə versə,
"birini seç" sualı "hansı DOĞRU tərəfdir" sualından fərqlidir.

**Düzgün cavab — Tip-in özü artıq bu sualı cavablandırır.** `GoamlTipi`
enum-u İSTİQAMƏT daşıyır (`...Out` = biz göndəririk = Debet bizimdir,
`...In` = biz alırıq = Kredit bizimdir). Yeni `GoamlTipiExtensions.
BizimTerefDebetdir()`:

| Tip | Nəticə |
|---|---|
| `FizikOut, HuquqiOut, HToFOut, C2cOut, FToHOut` | `true` (Debet bizimdir) |
| `FizikIn, HuquqiIn, HToHIn, HToFIn, FToHIn, C2cIn` | `false` (Kredit bizimdir) |
| `HToH, FToF` (suffikssiz) | `null` — İSTİQAMƏTSİZ, hər iki tərəf bizim ola bilər |

### Memarlıq — Tip operator TƏRƏFDƏN seçilir, GET zamanı YOX

`Tesdiq` GET action-ı Tip-dən ƏVVƏL yüklənir (Tip formanın özündə, operator
seçir) — ona görə "Tip-ə görə hansı tərəfi götür" qərarı GET-də VERİLƏ BİLMƏZ.
Həll — server HƏR İKİ tərəfin xam KYC-sini (Oracle-dan) BİR DƏFƏYƏ tapıb
brauzerə ötürür, Tip dəyişəndə (JS, server sorğusu TƏKRARLANMADAN) doğru
tərəf tətbiq olunur:

- **`IGoamlService.HesabSahibiKycTapAsync`** imzası dəyişdi: `GoamlHesabSahibiKycDto?`
  (tək nəticə) → **`GoamlHesabSahibiKycHerIkiTerefDto`** (`Debet`+`Kredit`,
  hər ikisi müstəqil, `null` ola bilər). Köhnə "Debet-ə bax, dayan" məntiqi
  SİLİNDİ — hər iki tərəf HƏMİŞƏ oxunur.
- **`EmeliyyatlarController.Tesdiq` (GET)** — yalnız BİR tərəfdə nəticə varsa
  (digəri tam boşdursa), qeyri-müəyyənlik yoxdur, DƏRHAL doldurulur (köhnə
  davranış). HƏR İKİSİNDƏ nəticə varsa — DOLDURMA, `ViewBag.KycDebet`/
  `KycKredit` ilə hər iki tərəfin xam datası görüntüyə ötürülür,
  `ViewBag.HerIkiTerefdeKycVar` bayrağı qoyulur.
- **`Tesdiq.cshtml`** — `HerIkiTerefdeKycVar` olanda sarı xəbərdarlıq banner-i
  ("Debet VƏ Kredit hər ikisində KYC tapıldı... Tip seçin") + iki düymə
  ("Debet tərəfini götür" / "Kredit tərəfini götür", HToH/FToF üçün əl ilə
  seçim). JS: `Tip` select-inin `onchange`-i `gmlTipBizimDebetdir` lüğətinə
  (server-dən, `BizimTerefDebetdir()`-in ÖZÜNDƏN render olunub — EYNİ qayda
  iki yerdə YAZILMIR) baxıb uyğun tərəfin KYC-sini (`gmlKycDebet`/`gmlKycKredit`,
  hər ikisi server-dən JSON kimi gəlir) CON+SIG sahələrinə tətbiq edir.
  **HEÇ NƏ SİLMİR** — Tip-in göstərdiyi tərəfdə heç nə tapılmayıbsa
  (`null`), mövcud sahələrə TOXUNULMUR (operator lazım olsa "Təmizlə"
  düyməsini özü işlədir).
- CON seçim dropdown-u (`gml-con-select`) artıq HƏMİŞƏ DOM-dadır (əvvəl
  `@if (gmlElaqaliSexsler.Count > 0)` ilə şərtli idi) — `gmlConDropdownYenile()`
  onu Tip dəyişəndə/əl ilə seçimdə YENİDƏN qurur (Debet və Kredit-in öz,
  FƏRQLİ əlaqəli-şəxs siyahıları ola bilər).

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — düzəliş yalnız BUNDAN SONRAKI "Təsdiqlə"
GET-lərinə təsir edir, artıq təsdiqlənmiş qeydlər dəyişmir.

⚠️ **AÇIQ QALAN İŞ (istifadəçi ilə müzakirə edilib, BU SESSİYADA EDİLMƏDİ) —
Tip-in ÖZÜNÜN avtomatlaşdırılması.** İstifadəçi sual verdi: *"bacara
bilseydik tipide maximum avtomatlasdirardiq, biz bunu bacara bilerik?"*
Prinsipcə BƏLİ — AYSEL-in "Update New" kaskadında TIP-i hesab prefiksindən/
strukturundan hesablayan **15 qayda** var (06-07.11.2026-da tapılıb,
bax CLAUDE.md "'Update New' Avtomatik Doldurma Mexanizmi Köçürüldü" bölməsi),
AMMA həmin qaydaların **dəqiq SQL mətni YALNIZ 300 simvola KƏSİLMİŞ önizləmə
kimi əldə edilib** — tam mətn heç vaxt götürülməyib (o vaxt qərar "operator
özü seçsin" idi, kəsilmiş mətni port etməyə ehtiyac yox idi). Diaqnostik
skript artıq hazırdır (`docs/sql/goaml/93-94` faylları) — növbəti addım
istifadəçinin bunu AYSEL-də YENİDƏN işlədib tam mətni göndərməsidir, sonra
TIP-i də CON/SIG kimi "avtomatik TƏKLİF et, operator baxıb təsdiqləsin/
dəyişsin" prinsipi ilə əlavə etmək olar.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə yoxlanıldı**
(mötərizə/fiqurlu-mötərizə balansı skriptlə, `git diff`-in YALNIZ əlavə
olunan sətirləri ayrıca ölçülüb — bir yerdə stray `)` tapılıb düzəldildi,
qalan bütün "imbalans" siqnalları çoxsətrli şərhlərin diff-hunk-dan kəsilməsi
idi, tam fayl balansı YOXLANILIB TƏSDİQLƏNDİ). İstifadəçi Visual Studio-da
**Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır: Debet VƏ Kredit-in
HƏR İKİSİ hüquqi olan, hər ikisinin öz Rəhbəri/Nümayəndəsi olan bir namizəd
aç → sarı banner çıxsın → "Huquqi (IN)" Tip seçib Kredit-in Rəhbərinin
avtomatik dolduğunu, "Huquqi (OUT)" seçəndə Debet-in Rəhbərinin dolduğunu,
"H TO H" seçəndə heç nəyin avtomatik dəyişmədiyini (yalnız banner-dəki
düymələrlə seçildiyini) təsdiqləməli.

## GOAML — "Hər İki Tərəf" Banner-i 4 SANİYƏYƏ YOXA ÇIXIRDI + "İmza Boş Qalır" İzahı Yox İdi (12.11.2026, KRİTİK — DÜZƏLDİLDİ)

Yuxarıdakı Tip-əsaslı düzəlişi real mühitdə sınayan istifadəçi iki şikayət
bildirdi: *"ekrana bu cixir biranliq ve gedir"* (banner ekrana çıxır, bir an
sonra yoxa çıxır) və *"tipi deyisirem ama hecne dolmur imza terefde"* (Tip-i
dəyişirəm, amma imza (SIG) tərəfində heç nə dolmur).

**Kök səbəb (banner yoxa çıxması) — "Şərh Xülasəsini `.fn-alert` ilə YAZMA"
qaydasının (CLAUDE.md-də artıq sənədləşmiş, `avtopark.css`-də presedenti olan)
elə özü, bu dəfə GOAML-də unudulmuşdu.** `user-area.js`-in qlobal "Auto-hide
alerts" skripti HƏR `.fn-alert` elementini 4 saniyə sonra `el.remove()` ilə
DOM-dan SİLİR. "Hər iki tərəfdə KYC var" banner-i (`gmlIkiTerefBanner`)
`class="fn-alert fn-alert--warning"` daşıyırdı — İKİ interaktiv düymə
("Debet/Kredit tərəfini götür") elə BU banner-in İÇİNDƏ idi, ona görə 4
saniyə sonra DÜYMƏLƏR DƏ yox olurdu, operator onları klikləyə bilmirdi.

**İkinci, əlaqəli tapıntı:** `.fn-alert--warning` VƏ `.fn-alert--info`
siniflərinin ÖZÜ layihənin HEÇ bir CSS faylında tərif OLUNMAYIB (yalnız
`.fn-alert`-in baza padding/radius-u qalırdı, fon/rəng/sərhəd YOX idi) —
yəni banner "4 saniyəyə yoxa çıxmazdan ƏVVƏL" belə, demək olar rəngsiz,
çətin seçilən bir qutu idi. "Bir anlıq çıxıb gedir" təəssüratının bir hissəsi
elə buradan gəlirdi.

**Düzəliş — Tesdiq.cshtml-in ÖZ `@section Styles`-ında (qlobal CSS-ə
TOXUNULMADI) yeni, `fn-alert`-dən TAMAMİLƏ MÜSTƏQİL siniflər:**
`.gml-banner` + `.gml-banner--info` + `.gml-banner--warning` — `avtopark.css`-
dəki `.av-val-summary`-nin elə eyni qaydası (sinif adı QƏSDƏN `fn-alert`
daşımır ki, qlobal auto-hide onu tutmasın). Hər iki köhnə `fn-alert`
çağırışı (avtomatik-doldurma info banner-i VƏ "hər iki tərəf" warning
banner-i) bu yeni siniflərə keçirildi.

**İkinci şikayət ("imza tərəfdə heç nə dolmur") — ARAŞDIRILDI, BU, KOD BUGU
DEYİL.** `HesabSahibiKycAsync`-in hüquqi-şəxs qolu SIG-i (`TekSexs`) YALNIZ
`odb.imza_huquqi_olan_shexsler`-də **AKTİV** (`imza_bitme is null`) VƏ **"Rəhbər"**
rolu (`aml_related_persons.id_related_persons = 2`) ilə işarələnmiş sətir
tapanda doldurur; CON ("Nümayəndə") isə TAMAMİLƏ AYRI bir cədvəldən
(`odb.NUMAYENDE`) gəlir. `Faydali()` yoxlaması isə `TekSexs != null **VƏ YA**
ElaqaliSexsler.Count > 0` şərtidir (OR) — yəni bir hesabda YALNIZ Nümayəndə
qeydləri olub, aktiv "Rəhbər" olmasa belə, həmin tərəf "faydalı" sayılır
(banner-i tətikləyir), AMMA SIG üçün doldurulası HEÇ nə yoxdur. Bu, iki ayrı
hesabın (Debet VƏ Kredit) HƏR İKİSİNDƏ baş verəndə — hər ikisinin Nümayəndə
siyahısı var, amma heç birinin aktiv "Rəhbər"i yoxdursa — Tip hansı tərəfi
seçərsə-seçsin SIG HEÇ VAXT dolmayacaq, bu GÖZLƏNİLƏN davranışdır (kodda
dəyişiklik TƏLƏB ETMİR), sadəcə operator üçün aydın DEYİLDİ.

**Düzəliş — yalnız İZAH, banner-in içinə əlavə edildi:** "Tip seçdikdən/
düyməyə basdıqdan sonra 'Nümayəndə' sahələri doldu, amma 'Hesab imza hüququ
olan şəxs' boş qalırsa — bu, xəta DEYİL: o hesab üçün Oracle-da aktiv
'Rəhbər' qeydi tapılmayıb (yalnız nümayəndə var), imza sahəsini özünüz
doldurmalısınız." Əgər istifadəçi KONKRET bir hesabda real "Rəhbər"in Oracle-da
olduğuna əmindirsə (buna baxmayaraq SIG boş qalırsa), yoxlanacaq yer
`odb.imza_huquqi_olan_shexsler`-də HƏM `imza_bitme is null` ŞƏRTİNİ, HƏM
`aml_related_persons`-da `id_related_persons=2` tag-ının mövcudluğunu
birgə yoxlamaqdır — ikisindən biri əskikdirsə kod onu "tapılmadı" sayacaq.

⚠️ **Eyni günün əvvəlki sualı — "əməliyyatın nömrəsi" (RecNum) HEÇ YERDƏ
görünmürdü, əlavə edildi.** `GoamlNamizedSetirDto.RecNum` (Oracle-un öz
`RECNUM` açarı, namizəd axtarışının bir hissəsi idi) və `GoamlTesdiqDto.
CandidateRecNum` (Təsdiqlədikdən sonra `GoamlEmeliyyat`-da daimi saxlanan
sütun) artıq KODDA var idi, AMMA heç bir ekranda GÖSTƏRİLMİRDİ — yalnız
"Təsdiqlə" linkinin gizli query-string parametri kimi daşınırdı. Dörd yerə
"Əməliyyat №" sütunu/sahəsi əlavə edildi:
- `Emeliyyatlar/Index.cshtml` VƏ `Risk/Index.cshtml` — axtarış nəticəsi
  cədvəlinə yeni "№" sütunu (`s.recNum`, sıralana bilər).
- `Tesdiq.cshtml`-in `gml-info-strip`-i — "Tarix"dən ƏVVƏL, `@Model.
  CandidateRecNum`.
- `Detal.cshtml`-in `gml-info-strip`-i — "Əməliyyat tarixi"dən ƏVVƏL, EYNİ
  sahə (`GoamlEmeliyyatDetalDto : GoamlTesdiqDto`-dan miras).
- **`Siyahi.cshtml`** (təsdiqlənmiş siyahı) — bu YEGANƏ yer idi ki, sahə
  DTO-da ÜMUMİYYƏTLƏ YOX idi: `GoamlEmeliyyatListDto.CandidateRecNum` (yeni)
  əlavə edildi, `GoamlService.HamisiniGetirAsync`-in mapping-inə bir sətir
  (`CandidateRecNum = x.CandidateRecNum`) qoşuldu, cədvələ yeni ilk sütun
  əlavə olundu.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə/div/script/style balansı hər dəyişən faylda ayrıca
skriptlə yoxlanıldı — `GoamlService.cs`-in fayl-ümumi 737/745 paren fərqi
əvvəlki sessiyalardan qalan, bu düzəlişdən ASILI OLMAYAN köhnə şərh
nümunəsidir, `git diff` izolyasiyası ilə təsdiqləndi). İstifadəçi Visual
Studio-da **Rebuild All** ilə, sonra real ssenari ilə yoxlamalıdır: "hər
iki tərəf" banner-inin artıq 4 saniyədən sonra da (və ümumiyyətlə aydın
rəngli/oxunaqlı) qaldığını, Əməliyyatlar/Risk axtarış cədvəllərində,
Təsdiqlə/Bax ekranlarında və Təsdiqlənmiş siyahıda "Əməliyyat №" sütununun
göründüyünü.

## GOAML — CON/SIG HEÇ NƏ DOLMURDU — "AKTİV" ŞƏRTİ `IMZA_BITME IS NULL`-A GÜVƏNİRDİ, REAL DATA SENTİNEL TARİX YAZIRDI (12.11.2026, KRİTİK — DÜZƏLDİLDİ)

İstifadəçi başqa bir namizəddə (Debet=3502020000001600000, Kredit=4014002000167190001,
Tip="Hüquqi (IN)" artıq seçilmiş) **heç bir avtomatik doldurma, heç bir banner**
(nə "avtomatik gətirildi" yaşıl, nə "hər iki tərəfdə tapıldı" sarı) görmədiyini
bildirdi: *"gelmirde"*. Göndərdiyi İKİNCİ ekran-görüntüsü (Oracle-un öz "Əlaqəli
şəxslər" grid-i) həlledici idi: "Rəhbər" sətrinin "Səlahiyyətinin bitmə tarixi"
sütunu **boş DEYİL** — **"05-05-2125"** yazırdı (başlama "05-05-2025").

**Kök səbəb:** `HesabSahibiKycAsync`-in SIG sorğusu (`odb.imza_huquqi_olan_
shexsler`) VƏ `NumayendeSiyahisiAsync`-in CON sorğusu (`odb.NUMAYENDE`) hər
ikisi "aktiv" şərtini **`imza_bitme is null`** kimi yazmışdı — 09-10.11.2026-da
test edilən (DR.ESS MMC) hesabda bu sahə HƏQİQƏTƏN `NULL` idi, ona görə şərt
işləyirdi. Bu YENİ hesabda isə Oracle-a məlumatı daxil edən sistem/operator
"hələ bitməyib" mənasını `NULL` ƏVƏZİNƏ **uzaq-gələcək bir sentinel tarixlə**
(2125-ci il) ifadə etmişdi — real data İKİ FƏRQLİ konvensiyanı QARIŞIQ işlədir.
`IS NULL` yoxlaması bu sətri "bitib, qeyri-aktiv" sayıb TAMAMİLƏ gizlədirdi —
SIG sorğusu sıfır sətir, CON sorğusu da (eyni problem) sıfır sətir qaytarırdı.
Hər ikisi boş olduğu üçün `Faydali()` yoxlaması (`TekSexs != null VƏ YA
ElaqaliSexsler.Count > 0`) hər iki tərəf üçün `false` idi — nə tək-tərəfli
avtomatik doldurma, nə "hər iki tərəfdə tapıldı" banner-i tətiklənmədi, ekran
sükutla tam boş qaldı (heç bir xəta, heç bir log — Oracle sorğusu UĞURLA 0
sətir qaytarmışdı, bu, `catch` bloklarının tutduğu bir İSTİSNA deyildi).

**Düzəliş — hər iki sorğuda bir şərt:**
```sql
-- əvvəl:  and s.imza_bitme is null
-- indi:   and (s.imza_bitme is null or s.imza_bitme > sysdate)
```
(`GoamlService.cs`, SIG sorğusu ~sətir 942, CON/`NumayendeSiyahisiAsync`
sorğusu ~sətir 982). Məntiq: "səlahiyyətin bitmə tarixi" sahəsi bir
GÖZLƏNİLƏN BİTİŞ tarixidir — boşdursa (heç vaxt bitmir) VƏ YA hələ
KEÇMƏYİBSƏ (gələcəkdədir, nə qədər uzaq olsa belə), səlahiyyət HAZIRDA
aktivdir. `> sysdate` AZ-timezone-dan asılı deyil — sadə tarix müqayisəsidir.

**Qayda: Oracle-dan gələn "bitmə tarixi" tipli bir sahədə "aktivdir" şərtini
YALNIZ `IS NULL` ilə yazma — real data operatorların İKİ fərqli konvensiyası
(boş = həmişəlik, sentinel uzaq-tarix = həmişəlik) qarışıq işlədə bilər.**
Bu, "Qara Jeton — Menfi Miqdar" bölməsindəki "işarə NECƏ YAZILIRSA YAZILSIN,
miqdarı işlədən kod qoruncaqlı olmalıdır" prinsipinin tarix sahəsi üçün
formasıdır — `IS NULL` tək başına "aktivdir" demək deyil, "bitmə tarixi hələ
gəlməyib" (`IS NULL OR > SYSDATE`) daha dözümlü, düzgün şərtdir. Simptom
xüsusilə çaşdırıcı idi, çünki İKİ DÜZGÜN işləyən köhnə test (DR.ESS MMC) ilə
YENİ, sükutla boş qalan hal arasında KOD HEÇ DƏYİŞMƏMİŞDİ — fərq YALNIZ
Oracle-dakı data konvensiyasında idi, bu da "əvvəlki test işləyirdi, deməli
kod düzgündür" fərziyyəsini xüsusilə təhlükəli edir.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR — yalnız BUNDAN SONRAKI "Təsdiqlə" GET-lərinə
TƏSİR EDİR.** Artıq açılmış/görülmüş boş formalar dəyişmir, operator
namizədi YENİDƏN açmalıdır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı — `git diff`-in əlavə/silinən sətirləri
ayrıca ölçülüb, hər ikisi eyni net fərqlə, düzəlişin ÖZÜ balanslıdır).
İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra EYNİ namizədi
(Debet=3502020000001600000, Kredit=4014002000167190001) yenidən açıb
yoxlamalıdır: "Nümayəndənin" VƏ "Hesab imza hüququ olan şəxs"in indi
avtomatik (ya tək-tərəfli, ya da "hər iki tərəfdə tapıldı" banner-i ilə)
dolduğunu təsdiqləməli.

## GOAML — "Hər İki Tərəf" Banner-i İSTİQAMƏTLİ Tip Seçiləndə ÇAŞDIRICI QALIRDI (12.11.2026)

Yuxarıdakı sentinel-tarix düzəlişindən sonra istifadəçi datanın gəldiyini
təsdiqlədi, sonra sual verdi: *"bu hesab seçilməlidir? yoxsa tip ilə kifayət
edir?"* — cavab verildi: Tip **istiqamətli** (Out/In) olanda özü kifayətdir,
`onchange` handler artıq doğru tərəfi avtomatik tətbiq edir; YALNIZ
istiqamətsiz (`H to H`/`F to F`) Tip-lərdə operator banner-dəki "Debet/Kredit
tərəfini götür" düymələrindən birini seçməlidir. İstifadəçi bunun ÜSTÜNƏ əlavə
etdi: *"onda o hesab söhbəti artıqdır mənə, çaşdırıcıdır"* → *"onda, o zaman
çıxsa yaxşı olmaz?"* — yəni Tip artıq istiqaməti həll edəndə banner/düymələr
ekranda **görünməməlidir**, yalnız həqiqətən qərarsız (istiqamətsiz) halda
qalmalıdır.

**Düzəliş — `Tesdiq.cshtml`-in Tip `change` handler-i:** `gmlTipBizimDebetdir`-
dən oxunan istiqamət (`true`/`false`/`null`) artıq KYC-ni tətbiq etməklə
yanaşı, `#gmlIkiTerefBanner`-in görünüşünü də idarə edir — istiqamət
BİLİNİRSƏ (`true` və ya `false`) banner `display:none` olur, BİLİNMİRSƏ
(HToH/FToF, yaxud Tip hələ seçilməyib) `display` sıfırlanır (`''`, defolt
görünüş geri qayıdır). Operator Tip-i HToH-dan geri istiqamətli bir tipə
dəyişsə, banner YENİDƏN gizlənir; əksinə dəyişsə, YENİD�ən görünür — bannerin
görünüşü HƏR dəfə cari Tip-in istiqamətinə görə yenidən hesablanır, "bir dəfə
gizlədib unutma" deyil.

⚠️ Server-tərəfli ilkin render (`gmlHerIkiTerefdeVar` → banner DOM-a
yazılır-yazılmır) DƏYİŞMƏDİ — bu, hələ Tip seçilməmiş GET anında doğru
qərardır (hər iki tərəfdə KYC var, hələ hansı bizimdir bilinmir). Düzəliş
YALNIZ klient tərəfdə, Tip seçimindən SONRAKI görünüşü dəqiqləşdirir.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı skriptlə — `(`/`)` 239/239, `{`/`}` 52/52,
`[`/`]` 9/9, tam fayl). İstifadəçi Visual Studio-da **Rebuild All** ilə,
sonra hər iki tərəfdə KYC olan bir namizəddə yoxlamalıdır: istiqamətli Tip
(məs. "Hüquqi (IN)") seçəndə banner-in yoxa çıxdığını, "H to H" seçəndə
YENİDƏN göründüyünü.

## GOAML — "Hər İki Tərəf" Söhbəti "SEÇİM" Deyil İmiş — AML Məsul Şəxsin Qaydası: EYNİ ƏMƏLİYYAT İKİ DƏFƏ GÖNDƏRİLİR (12.11.2026, KRİTİK — DÜZƏLDİLDİ)

Yuxarıdakı banner-i "Tip seçiləndə yoxa çıxsın" kimi düzəltdikdən dərhal sonra
istifadəçi AML məsul şəxsdən soruşub cavabı gətirdi: *"hansının bizim tərəf
olduğu yazısı səhvdir, çünki mən soruşdum məsul şəxsdən — dedi ki belə hal
olsa hər ikisini də göndərməli olacağıq, yəni iki dəfə seçib Tip-ini dəyişib
faylı hazırlayıb göndərəcək"*.

**Bu, əvvəlki iki dalğanın (sentinel-tarix düzəlişi + banner-i gizlətmə) altında
duran fərziyyənin ÖZÜNÜ YANLIŞ çıxardı.** Mən "hər iki tərəfdə KYC var" halını
**"hansı tərəf bizimdir" sualına CAVAB TAPILMALI olan bir qeyri-müəyyənlik**
kimi modelləşdirmişdim — əslində bu sual mexaniki olaraq Tip-dən (OUT/IN)
artıq məlumdur, HEÇ BİR qeyri-müəyyənlik yoxdur. Əsl məsələ başqa idi: məsul
şəxsin qaydasına görə, hər iki tərəfdə KYC tapılan əməliyyat AML baxımından
HƏR İKİ TƏRƏFDƏN hesabat tələb edə bilər — yəni **EYNİ əməliyyat (eyni
filial+tarix+recnum) İKİ AYRI dəfə, İKİ FƏRQLİ (əks istiqamətli) Tip ilə
təsdiqlənib göndərilməlidir**, "bir dəfə seçib bitirmək" deyil.

**Kök problem — dedup şərti bunu TAMAMİLƏ BLOKLAYIRDI.** `TesdiqleAsync`-in
dublikat yoxlaması (`MovcuddurmuAsync`) yalnız `(FilialNumber, DateOper,
RecNum)` üçlüyünə baxırdı, `Tip`-i NƏZƏRƏ ALMIRDI — yəni operator bu namizədi
ARTIQ bir Tip ilə təsdiqləyib göndərmişdisə, EYNİ namizədi əks Tip ilə İKİNCİ
dəfə təsdiqləməyə çalışanda sistem **"Bu əməliyyat artıq təsdiqlənib"** deyə
RƏDD EDİRDİ — AML qaydasının tələb etdiyi ikinci hesabatı yarada bilmirdi.
Eyni şəkildə axtarış siyahısında (`Index.cshtml`/`Risk/Index.cshtml`) "artıq
təsdiqlənib" bədgisi "Təsdiqlə" düyməsini TAM GİZLƏDİRDİ — operator ikinci
dəfə heç basa BİLMİRDİ.

**Düzəliş — üç qat:**

1. **`GoamlService.TesdiqleAsync`** — dedup şərtinə `&& x.Tip == dto.Tip`
   əlavə olundu. İndi EYNİ namizəd EYNİ Tip ilə ikinci dəfə RƏDD edilir (əsl
   təkrar-klik qoruması qalır), AMMA **fərqli Tip ilə TƏKRAR təsdiqlənə
   bilər** (ikinci, əks istiqamətli hesabat üçün).
2. **`GoamlNamizedSetirDto.TesdiqlenmisTipler`** (yeni, `List<string>`) —
   `NamizedAxtarAsyncCore`-un dedup sorğusu indi `HashSet`-dən
   `Dictionary<(filial,tarix,recnum), List<string>>`-ə keçdi: hər açar üçün
   ARTIQ təsdiqlənmiş Tip(lər)in **etiketini** (`GoamlTipiExtensions.
   Etiket()`) toplayır. `ArtiqTesdiqlenib` sahəsi qalır ("ən azı bir dəfə
   təsdiqlənib" mənasında), amma TƏK BAŞINA düyməni bloklamır.
3. **`Index.cshtml` (Əməliyyatlar VƏ Risk)** — "Təsdiqlənib" bədgisi İNDİ
   "Təsdiqlə" düyməsini ƏVƏZ ETMİR, onun YANINDA göstərilir (bədgidə artıq
   göndərilmiş Tip-lərin adı), düymənin mətni "Əks tərəfi də təsdiqlə"-yə
   dəyişir ki, operator bunun İKİNCİ bir təsdiq olduğunu bilsin.

**Banner mətni də düzəldildi (`Tesdiq.cshtml`)** — "hansının bizim tərəf
olduğunu bilmirik, seçin" çərçivəsi SİLİNDİ, əvəzinə: Tip seçiləndə tərəf
MEXANİKİ olaraq özü həll olunur (banner yoxa çıxır — əvvəlki dalğa), AMMA
əsl mesaj indi **"AML qaydasına görə belə halda əməliyyat çox vaxt İKİ AYRI
hesabat kimi göndərilməlidir — bir istiqamətli Tip ilə təsdiqləyib göndərin,
SONRA bu namizədi yenidən tapıb ƏKS istiqamətli Tip ilə TƏKRAR təsdiqləyin"**.
"Debet/Kredit tərəfini götür" düymələri YALNIZ istiqamətsiz (HToH/FToF)
tiplərdə xam tərəfi əl ilə görmək üçün qalır.

**Qayda:** real biznes/compliance qaydasını (AML məsul şəxs kimi) YALNIZ ekran
davranışından/kodun strukturundan **təxmin etmə** — "hər iki tərəfdə məlumat
var" kimi bir texniki şərt, ilk baxışda "hansını seçək" sualı kimi görünsə
belə, əslində "hər ikisini də et" tələbinin işarəsi ola bilər. Bu fərq
TEXNİKİ SƏVİYYƏDƏ (dedup açarı Tip-i daşımırmı?) həll olunmalı idi — kod
"seçim" fərziyyəsinə əsaslanıb YANLIŞ YERDƏ (dedup şərtində) məhdudlaşdırma
qoymuşdu, bu da əsl tələbi STRUKTUR SƏVİYYƏSİNDƏ qeyri-mümkün edirdi.

⚠️ **GERİYƏ TƏTBİQ OLUNMUR** — düzəlişdən ƏVVƏL "artıq təsdiqlənib" bədgisi
ilə BLOKLANMIŞ namizədlər bu düzəlişdən sonra YENİDƏN görünəcək (düymə geri
qayıdır), əlavə miqrasiya/data düzəlişi lazım deyil — DB-də heç nə pozulmayıb,
yalnız UI/server qaydası dəyişdi.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə balansı — `git diff`-in əlavə/silinən sətirləri ayrıca
ölçülüb, hər ikisi eyni net fərqlə, düzəlişin ÖZÜ balanslıdır; JSON casing
`Json()` helper-inin qlobal camelCase qaydasına uyğun yoxlanıldı). İstifadəçi
Visual Studio-da **Rebuild All** ilə, sonra hər iki tərəfdə KYC olan bir
namizədi (1) bir Tip ilə təsdiqləsin → "Təsdiqlənib (...)" bədgisinin
düymənin YANINDA (onu gizlətmədən) göründüyünü, (2) EYNİ namizədi əks
istiqamətli Tip ilə YENİDƏN təsdiqləsin → bu dəfə də uğurla keçdiyini, (3)
EYNİ Tip ilə ÜÇÜNCÜ dəfə cəhd etsin → "eyni Tip ilə artıq təsdiqlənib" xətası
ilə rədd olunduğunu təsdiqləməlidir.

## GOAML — "Hər İki Tərəf" Banner-i YENƏ DƏ Lazımsız UZUN İDİ (13.11.2026)

Yuxarıdakı dalğada banner "SEÇİM yox, İKİ DƏFƏ GÖNDƏR" izahı ilə BÖYÜDÜLMÜŞDÜ
(uzun paraqraf + 2 düymə + ayrıca "SIG boş qala bilər" qeydi). İstifadəçi
(ekran-görüntüsü ilə) bunu RƏDD ETDİ: *"bu nediye bele yazib doldurmusan,
sehife acilan kimi bele yazilir,buna umumilikde ehtiyac yoxdur mence,sadece
xirda imkan ver isci ozu secsin vessalamda bu qeder yaziya ne ehtiyac"* —
mətnin ÖZÜ (AML-in iki dəfə göndərmə tələbi) yox, onun HƏR DƏFƏ, DƏRHAL,
UZUN formada göstərilməsi narahatlıq idi.

**Düzəliş — izahlı paraqraf VƏ "SIG boş qala bilər" qeydi SİLİNDİ, yalnız
BİR sətirlik, kompakt bir seçim qaldı:** ikon + qısa mətn ("Hər iki tərəfdə
KYC var:") + iki kiçik düymə (Debet/Kredit, ad-göstərmədən). Yeni
`.gml-banner--compact` sinfi (`display:flex; padding:8px 14px; font-size:
.82rem`) banner-in hündürlüyünü böyük info-qutudan (14-18px padding, geniş
sətir) vizual olaraq AYIRIR.

**Davranış DƏYİŞMƏDİ** — Tip istiqamətli (OUT/IN) seçiləndə JS (`#gmlIkiTerefBanner`-ə
`display:none`) bu sətri YENƏ DƏ avtomatik gizlədir; YALNIZ istiqamətsiz
(HToH/FToF) tiplərdə, operator Tip-i hələ seçməyib ikən görünür qalır.
`gmlTerefTetbiqEt(teref)` funksiyası TOXUNULMADI — düymələr əvvəlki kimi
`gmlKycDebet`/`gmlKycKredit`-i tətbiq edir.

**Qayda:** düzgün bir biznes qaydasını (AML-in iki dəfə göndərmə tələbi kimi)
ÇEVİRİB kodda düzgün tətbiq etmək (dedup-un Tip-i daşıması) ilə onu
İNTERFEYSDƏ NECƏ TƏQDİM ETMƏK AYRI məsələdir — əvvəlki dalğa birincini
düzəltdi, amma ikincini "nə qədər çox izah, bir o qədər yaxşı" fərziyyəsi ilə
HƏDDİNDƏN ARTIQ genişləndirdi. Operatorun HƏR DƏFƏ, HƏR namizəddə görəcəyi bir
elementdə (bura: hər iki tərəfdə KYC olan namizədlər, nadir DEYİL) uzun izah
YORUCUDUR — qaydanın ÖZÜ CLAUDE.md-də (bu fayl) sənədləşir, ekranda isə
YALNIZ əməl üçün lazım olan minimum qalır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə/tag balansı Python skriptlə — `{`/`(`/`[` və
`div`/`button`/`form`/`select`/`span`/`style`/`script` teqlərinin hamısı
cüt sayda, tam fayl). İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra
hər iki tərəfdə KYC olan bir namizəd aça bilər — banner-in indi kiçik, bir
sətirlik bir qutu olaraq göründüyünü, Tip-i istiqamətli seçəndə YOXA ÇIXDIĞINI
təsdiqləməlidir.

## GOAML — "Hər İki Tərəf" Banner ÜÇÜNCÜ DALĞA — TAMAMİLƏ ÇIXARILDI, Seçim SIG Blokunun İçinə Köçürüldü (13.11.2026)

İstifadəçi kompakt banner-i (yuxarıdakı dalğa) sınayıb YENƏ narazı qaldı:
*"tipi deyisirem hemin butonlar gedir,men helede o butonlar ne is yerayir
basa dusmedim,cox casdirici geldi mene,bunu ele tip ile idare edekde olmaz?
eger bilinmeyen tipi secse asagida imza selahiyyeri acanda orda numayende
kimi secim versin vessalamda"* — yəni problem artıq mətnin uzunluğu DEYİL
(əvvəlki dalğa onu düzəltmişdi), problem **ayrıca, üzən bir banner elementinin
ÖZÜ** idi: operator Tip-i dəyişəndə düymələrin yoxa çıxmasını "nə baş verdi?"
kimi qəbul edirdi, çünki bu, formanın əsas axınından KƏNAR bir element idi.

**Həll — banner TAMAMİLƏ SİLİNDİ, seçim artıq "Hesab imza hüququ olan
şəxsin" (SIG) blokunun DAXİLİNDƏ, Nümayəndə (CON) blokunun ÖZ "əlaqəli
şəxslərdən seç" dropdown-u İLƏ EYNİ üslubda bir dropdown-dur** (yeni
`#gmlTerefSelectWrap`, `gml-con-select`-in elə formatını təkrarlayır) —
istifadəçinin öz tələbinə görə ("numayende kimi secim versin"). Tip
istiqamətli (OUT/IN) seçiləndə dropdown eyni JS-lə (`display:none`)
gizlənir; istiqamətsiz (H to H/F to F) qalanda **SIG blokunun ÖZÜ default
AÇIQ gəlir** (`<details open="@(gmlHerIkiTerefdeVar ? "open" : null)">`) ki,
operator bloku əl ilə açmaq məcburiyyətində qalmasın — seçim dərhal görünür.

**`gmlTerefTetbiqEt(teref)` funksiyası TOXUNULMADI** (`gmlKycTetbiqEt`-i
çağırır, bu da hüquqi şəxsdə CON-un öz dropdown-unu da (`gmlConDropdownYenile`)
yeniləyir) — seçim YALNIZ SIG-ə görünür, amma tətbiq edəndə CON da (əlaqəli
şəxs siyahısı ilə) düzgün yenilənir, çünki funksiyanın özü hər iki bloku
birlikdə doldurur.

**`data-visible` atributu əlavə edildi** (`#gmlTerefSelectWrap`) — Tip
"bilinmir" halına qayıdanda (JS) dropdown-u YALNIZ ilk renderdə
`gmlHerIkiTerefdeVar=true` olduğu təqdirdə geri göstərir; əks halda (yalnız
bir tərəfdə KYC var idisə) heç vaxt görünməz qalır — köhnə banner-in "yalnız
hər iki tərəf olanda göstər" şərtini dropdown üçün də qoruyur.

⚠️ **Köhnə `.gml-banner--compact` CSS sinfi silindi** (artıq işlənmir),
`.gml-banner--warning` (yalnız bu banner işlədirdi) hazırda ölü qalıb —
zərərsiz, gələcək bir xəbərdarlıq üçün saxlanıldı.

**Qayda:** bir UI elementi "düzgün işləyir" olsa da (bu halda: Tip seçiləndə
doğru tərəfi tapıb tətbiq edirdi), onun YERİ (formanın əsas axınından kənar,
üzən bir banner) özü QARIŞIQLIQ mənbəyi ola bilər. İkinci rədd mesajı birinci
ilə EYNİ ŞİKAYƏTİ təkrarlamırdı ("uzundur") — fərqli, daha dərin bir şikayət
idi ("bu elementin NƏ olduğunu anlamıram"). Belə halda mətni daha da
qısaltmaq kömək etməz — elementin ÖZÜNÜ formanın təbii axınına (bura: artıq
mövcud olan, tanış CON-dropdown naxışının yanına, SIG blokunun daxilinə)
köçürmək lazımdır.

⚠️ **Bu sessiyada `dotnet build` mümkün olmadığı üçün yalnız əl ilə
yoxlanıldı** (mötərizə/tag balansı Python skriptlə — `{`/`(`/`[` və
`div`/`button`/`select`/`details`/`summary` teqlərinin hamısı cüt sayda, tam
fayl). İstifadəçi Visual Studio-da **Rebuild All** ilə, sonra hər iki tərəfdə
KYC olan bir namizəd aça bilər — üstdə artıq heç bir banner olmadığını,
"Hesab imza hüququ olan şəxsin" blokunun default AÇIQ gəldiyini və içində
Debet/Kredit seçim dropdown-unun göründüyünü, seçimdən sonra HƏM SIG, HƏM
CON-un dolduğunu, istiqamətli Tip seçəndə dropdown-un yoxa çıxdığını
təsdiqləməlidir.
