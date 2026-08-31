/* ============================================================================
   SƏHİFƏ TƏLİMATLARI — HAMISI BİR FAYLDA  (58 səhifə)
   ----------------------------------------------------------------------------
   Bu fayl 01, 02, 03 və 04 nömrəli skriptlərin BİRLƏŞMİŞ variantıdır.
   Onları ayrıca işlətməyə ehtiyac yoxdur — YALNIZ BUNU işlədin.

   ƏHATƏ
     İşçi portalı ................  9 səhifə
     HR .........................  22 səhifə
     Admin ......................  10 səhifə
     Avtopark ...................   6 səhifə
     Sənəd dövriyyəsi ...........   8 səhifə
     Əməliyyat ..................   3 səhifə
                                   ── 58

   ŞƏRT: `SehifeYardimlari` cədvəli mövcud olmalıdır. O, migration ilə
   yaranır (20260827100000_SehifeYardimiCedveli) və tətbiq startup-da
   avtomatik keçir — yəni yeni build bir dəfə işə salınmalıdır.
   Cədvəl yoxdursa skript 1-ci addımda dayanır və sizə bunu deyir.

   TƏKRAR İŞLƏTMƏK TƏHLÜKƏSİZDİR
     · Mövcud qeyd ÜSTƏLƏNMİR (`IF NOT EXISTS`).
     · Sonda köhnə HTML mətnləri sadə mətnə çevirən blok var, amma o da
       YALNIZ heç vaxt redaktə edilməmiş qeydlərə toxunur (`YenilenmeTarixi
       IS NULL`) — sizin əl ilə yazdığınız mətn qorunur.

   FORMAT: sadə mətn.   # Başlıq  ·  - siyahı  ·  1. nömrəli  ·  *qalın*
   Mətni dəyişmək üçün bu fayla qayıtmayın — `/Admin/Yardim` ekranını işlədin.
   ============================================================================ */

SET NOCOUNT ON;

/* ── 0) CƏDVƏL VARMI? ───────────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.SehifeYardimlari', N'U') IS NULL
BEGIN
    RAISERROR(N'SehifeYardimlari cedveli tapilmadi. Once yeni build-i bir defe ise salin (migration avtomatik tetbiq olunur), sonra bu skripti yeniden isledin.', 16, 1);
    RETURN;
END;

PRINT N'--- Telimatlar yazilir ---';


/* ══ İŞÇİ PORTALI  (9) ═════════════════════════════════════════════════ */

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/dashboard/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/dashboard/index', N'esas-sehife', N'Əsas səhifə (işçi paneli)', N'Ümumi',
N'Məzuniyyət qalığı, icazə saatı, müraciətlər və bildirişlər — hamısı bir ekranda.',
N'Sistemə girəndə ilk açılan səhifədir. Burada heç nə yazılmır — yalnız sizin cari vəziyyətinizi göstərir.

# Yuxarıdakı kartlar
- *Məzuniyyət qalığı* — əmək məzuniyyətindən neçə gününüz qalıb.
- *İcazə saatı — bu il* — bu il istifadə etdiyiniz saatlıq icazə.
- *Gecikmə — bu il* — bu il neçə dəfə gec gəlmisiniz.

# Aktiv müraciətlər
Göndərdiyiniz, amma hələ cavablanmamış müraciətlər. *Gözləmədə* yazısı müraciətin təsdiq gözlədiyini bildirir; kimdə qaldığını görmək üçün üstünə klikləyin.

# Bildirişlər
Sizə aid son hadisələr: müraciətiniz təsdiqlənəndə, imtina olunanda və ya sizdən təsdiq gözləyəndə burada görünür.

# Davamiyyət və Son ödənişlər
Günlərinizin yığımı (İşlədi, Gecikmə, İcazəli, Qayıb, Xəstəlik, Ezamiyyət) və sizə edilən son ödənişlər. Hər ikisinin altındakı keçid tam siyahını açır.

# Rəqəm düz gəlmirsə
Bu səhifədə heç nə düzəldilmir — göstəricilər davamiyyət və müraciət qeydlərindən hesablanır. Uyğunsuzluq görsəniz HR ilə əlaqə saxlayın.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/mezuniyyet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/mezuniyyet/index', N'mezuniyyetlerim', N'Məzuniyyətlərim', N'Məzuniyyət',
N'Bütün məzuniyyət müraciətləriniz və onların hansı mərhələdə olduğu.',
N'Göndərdiyiniz bütün məzuniyyət müraciətləri — köhnə və yeni.

# Status nə deməkdir
- *Gözləmədə* — müraciət göndərilib, hələ heç kim baxmayıb.
- *Şöbə rəisi / Rəhbər / HR təsdiqində* — məhz həmin şəxsdə gözləyir.
- *Təsdiqlənib* — bütün mərhələlər keçilib, məzuniyyət rəsmiləşib.
- *İmtina edildi* — səbəbi müraciətin içində yazılır.

# Müraciətin harada qaldığını görmək
Sətrin üstünə klikləyin — «Müraciət gedişatı» hansı addımların keçildiyini və indi kimdə olduğunu göstərir.

# Ləğv etmək
*Ləğv et* düyməsi ilə ləğv sorğusu göndərilir və səbəb yazılır. Təsdiqlənmiş məzuniyyəti ləğv etmək HR-ın təsdiqini tələb edir — balans avtomatik geri qayıdır.

# Yeni müraciət
*Müraciət göndər* düyməsi ilə. Eyni tarixlərə ikinci məzuniyyət yazmaq mümkün deyil — sistem üst-üstə düşməni bloklayır və hansı qeydlə toqquşduğunu yazır.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/mezuniyyet/create')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/mezuniyyet/create', N'yeni-mezuniyyet-muracieti', N'Yeni məzuniyyət müraciəti', N'Məzuniyyət',
N'Tarix seçirsiniz, sistem gün sayını və ödənişi hesablayır.',
N'Tarixləri seçən kimi sistem gün sayını və təxmini ödənişi göstərir.

# Gün sayı necə sayılır — DİQQƏT
Əmək məzuniyyətində *təqvim günü* sayılır: şənbə və bazar da daxildir. Yalnız məzuniyyətdə hesablanmayan bayram günləri çıxılır.

Yəni 20–24 avqust seçsəniz balansdan *5 gün* düşür, 3 yox. Ekrandakı «iş günü» göstəricisi ayrı məlumatdır — balansdan düşən rəqəm təqvim günüdür.

# Məzuniyyət pulu
Aşağıdakı hesablama qutusu pulun necə çıxdığını addım-addım göstərir: son 12 ayın qazancı, artım əmsalı və cari maaş müqayisə olunur, işçinin xeyrinə *böyük olan* götürülür.

Rəqəm təxminidir — son məbləği mühasibat təsdiq edir.

# Əvəzedici
Əvəzedici seçsəniz müraciət əvvəlcə ona gedir; o qəbul edəndən sonra rəhbərə keçir. Seçməsəniz birbaşa rəhbərə gedir.

# Nə vaxt bloklanır
- Seçdiyiniz tarixlər mövcud məzuniyyətinizlə üst-üstə düşürsə (bir gün toxunsa belə).
- Balansınız çatmırsa.

Hər iki halda ekranda səbəb və toqquşan qeydin tarixləri yazılır.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/mezuniyyet/detail')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/mezuniyyet/detail', N'mezuniyyet-detali', N'Məzuniyyət müraciətinin detalı', N'Məzuniyyət',
N'Müraciətin gedişatı — hansı addım keçilib, indi kimdədir.',
N'Bir müraciətin tam mənzərəsi: tarixlər, gün sayı, ödəniş və təsdiq zənciri.

# Müraciət gedişatı
Addımlar sıra ilə göstərilir. Yaşıl addım keçilib, boz addım hələ gözləyir. İndi kimdə olduğu qalın yazılır.

Bəzi addımlar sizin rolunuza görə *ümumiyyətlə olmur* — məsələn özünüz rəhbərsinizsə rəhbər addımı keçilmiş sayılır. Bu, səhv deyil.

# İmtina olunubsa
Səbəb burada yazılır. Düzəliş edib yenidən göndərmək üçün yeni müraciət yaradın — imtina olunmuş müraciət redaktə edilmir.

# Ləğv
Təsdiqlənmiş məzuniyyəti ləğv etmək üçün səbəb yazılır və HR-a gedir. HR təsdiqləyəndən sonra balans geri qayıdır və mühasibata «ödənişi icra etməyin» bildirişi düşür.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/icaze/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/icaze/index', N'icazelerim', N'İcazələrim', N'İcazə',
N'Saatlıq icazə müraciətləriniz və illik sayğacınız.',
N'Bütün saatlıq icazə müraciətləriniz və illik balansınız.

# İstifadə edilib
Bu il sayğacınızdan düşən saat. İllik hədd *36 saatdır*.

# Sayğaca nə yazılır
Sayğaca düşən rəqəm faktiki pəncərədən *az ola bilər*, çünki güzəştlərin qarşılığı çıxılır:
- *Nahara çıxmıram* seçmisinizsə — nahar fasiləsi qədər çıxılır.
- *Jetonla ödəmisinizsə* — jetonla örtülən hissə çıxılır.

Ona görə sayğaca yazılan icazə heç vaxt *3 saatı keçmir*.

# Status
*Gözləmədə* — təsdiq gözləyir. *Təsdiqlənib* — icazə rəsmiləşib. *İmtina edildi* — səbəb müraciətin içindədir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/icaze/create')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/icaze/create', N'yeni-icaze-muracieti', N'Yeni icazə müraciəti', N'İcazə',
N'Saat seçirsiniz; nahar və jeton güzəştləri pəncərəni uzadır.',
N'Saatlıq icazə üçün başlama və bitmə saatını seçirsiniz.

# Standart hədd — 3 saat
Adi icazə ən çox *3 saat* ola bilər. Onu uzadan iki güzəşt var və hər ikisinin qarşılığı sayğacdan çıxılır.

# 1. Nahara çıxmıram
İşarələsəniz pəncərə nahar fasiləsi qədər uzana bilər, əvəzində həmin müddət sayğacdan çıxılır. Yəni naharda işləyirsiniz, o vaxt sizə qaytarılır.

# 2. Artıq müddəti jetonumdan ödə
3 saatı aşan hissəni jeton balansınızdan ödəyir. *Miqdarı siz yazmırsınız* — sistem pəncərədən özü hesablayır; siz yalnız işarələyirsiniz.

Rəhbər təsdiq edərkən jetonu *artıra bilər*, amma məcburi həddin altına *sala bilməz*.

# Nümunə
13:00–17:45 (285 dəqiqə) + nahar + 1 saat jeton → sayğaca *180 dəqiqə* yazılır.

# Nə vaxt keçmir
Jeton və ya illik 36 saatlıq balans çatmırsa müraciət qəbul edilmir — ekranda səbəb yazılır.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/davamiyyet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/davamiyyet/index', N'davamiyyetim', N'Davamiyyətim', N'Davamiyyət',
N'Gün-gün giriş/çıxış qeydləriniz və statusları.',
N'Gün-gün giriş və çıxış qeydləriniz. Məlumat *barmaq izi cihazından* gəlir — bu səhifədə əl ilə düzəliş edilmir.

# Statuslar
- *İşdə* — vaxtında gəlib.
- *Gecikmə* — iş saatından gec giriş.
- *Saat İcazəsi* — təsdiqlənmiş icazə ilə çıxış.
- *Ezamiyyət* — ezamiyyət günü; gecikmə yazılmır.
- *Məzuniyyət günü* — təsdiqlənmiş məzuniyyət.
- *Erkən çıxış* — iş vaxtı bitmədən çıxış.
- *Qayıb* — cihazda qeyd yoxdur və icazə/məzuniyyət də yoxdur.

# Səhv görürsünüzsə
Status cihazın qeydindən hesablanır. Məsələn icazəniz sonradan təsdiqlənibsə köhnə gün *öz-özünə dəyişməyə bilər*. Belə halda HR-a müraciət edin — düzəlişi yalnız HR edir.

# Qayıb görünürəm, amma işdə idim
Çox vaxt səbəb cihaza vurulmamasıdır. Şöbə rəisiniz və HR bunu qeydlə düzəldə bilər.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/muraciet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/muraciet/index', N'muracietlerim', N'Müraciətlərim', N'Ümumi',
N'Məzuniyyət və ezamiyyət müraciətləriniz bir ekranda.',
N'Məzuniyyət və ezamiyyət müraciətlərinizi bir yerdə göstərir — ayrı-ayrı səhifələrə keçməyə ehtiyac qalmır.

# Bölmələr
Yuxarıdakı seçimlə növü dəyişirsiniz. Hər bölmənin öz sayğacı var (Cəmi, Gözləmədə).

# Nə edə bilərsiniz
- *Müraciət göndər* — yeni müraciət yaradır.
- *Ləğv et* — səbəb yazıb ləğv sorğusu göndərir.
- Sətrə klikləməklə gedişatı açırsınız.

# Qeyd
Bu ekran məzuniyyət və icazə səhifələri ilə *eyni məlumatı* göstərir — sadəcə birləşdirilmiş görünüşdür. Hansından baxmağınızın fərqi yoxdur.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/profile/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/profile/index', N'profil', N'Profil', N'Ümumi',
N'Şəxsi məlumatlarınız, əlaqə və mail ayarları.',
N'Şəxsi məlumatlarınız və əlaqə vasitələriniz.

# Nəyi özünüz dəyişə bilərsiniz
Telefon və əlaqə məlumatları. Ad, vəzifə, şöbə və maaş *dəyişdirilmir* — onları HR idarə edir.

# Mail ayarları
Bildirişlərin göndərildiyi ünvanı burada təyin edirsiniz. *Mail sına* düyməsi ayarların düzgünlüyünü yoxlayır — sınaq mesajı gəlmirsə ünvanı və parolu yenidən yoxlayın.

# Məlumat səhvdirsə
Vəzifə və ya şöbə səhv görünürsə HR ilə əlaqə saxlayın — düzəliş kadr qeydindən gəlir.',
0, 0, 0, GETDATE(), 0);


/* ══ HR  (22) ══════════════════════════════════════════════════════════ */

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/jeton/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/jeton/index', N'motivasiya-ve-jetonlar', N'Motivasiya və Jetonlar', N'Jeton',
N'İşçilərə verilən jetonlar, təsdiqi və balans.',
N'İşçilərin jeton hesabı: kim nə qazanıb, nə xərcləyib, nə qalıb.

# Jeton növləri
- *Müsbət* — motivasiya üçün verilir, balansı artırır.
- *Mənfi (Qara jeton)* — cəza jetonudur. Ədədi həmişə 1-dir, dəyişdirilmir.

# Sütunlar
- *Cəmi saat* — işçinin qazandığı ümumi jeton.
- *Xərcləndi* — istifadə etdiyi hissə.
- *Qalan* — hazırda istifadə edə biləcəyi qalıq.

# Status
- *Gözlənilir* — sorğu göndərilib, cavab verilməyib.
- *Təsdiqləndi* — jeton balansa yazılıb.
- *Rədd edildi* — səbəb sətirdə görünür.

# İşçi jetonu nəyə xərcləyir
Saatlıq icazə 3 saatı aşanda artıq müddəti jetondan ödəyə bilir. Miqdarı işçi yazmır — sistem pəncərədən özü hesablayır. Rəhbər təsdiq edərkən jetonu artıra bilər, məcburi həddin altına sala bilməz.

# Ləğv
*Ləğv et* jetonu geri götürür, səbəb yazılır və balans yenidən hesablanır.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/jetonteklifi/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/jetonteklifi/index', N'motivasiya-paneli', N'Motivasiya Paneli (jeton tövsiyələri)', N'Jeton',
N'Sistemin təklif etdiyi jetonlar — təsdiq edir və ya ləğv edirsiniz.',
N'Sistem işçilərin fəaliyyətinə baxıb *jeton tövsiyəsi* verir. Siz onları təsdiqləyir və ya ləğv edirsiniz — avtomatik verilmir.

# Tövsiyə haradan çıxır
Kateqoriyalar: Gecikmə, Tapşırıq, İş norması, Əvəzedici. Yəni həm müsbət davranış, həm pozuntu tövsiyəyə səbəb ola bilər.

# Nə edirsiniz
- *Jeton Ver* — tövsiyəni qəbul edir, jeton balansa yazılır.
- *Ləğv et* — tövsiyə silinir, heç nə yazılmır.
- *Qeyd* xanası istəyə bağlıdır; yazsanız qeydə düşür.

# Diqqət
*Gözlənilən tövsiyə yoxdur* yazısı problem deyil — sadəcə hazırda baxılası tövsiyə yoxdur.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/jetonteyinati/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/jetonteyinati/index', N'jeton-kataloqu', N'Jeton Kataloqu', N'Jeton',
N'Jeton növlərinin siyahısı: dəyər, kateqoriya, rəng.',
N'Sistemdə hansı jeton növlərinin olduğunu burada təyin edirsiniz. Bu, *kataloqdur* — işçiyə jeton vermək üçün deyil.

# Sahələr
- *Dəyər* — jetonun neçə saat gətirdiyi. *Gün (×8)* seçimi bir günü 8 saat kimi yazır.
- *Kateqoriya* — Bürünc, Gümüş və s.
- *Görünüş rəngi* — siyahılarda tanınması üçün.
- *Birbaşa ödənişli* — işarələnibsə jeton dərhal balansa düşür.

# Aktiv / Deaktiv
Deaktiv edilən növ *silinmir* — yeni jeton verilərkən siyahıda görünmür, amma köhnə qeydlər olduğu kimi qalır. Silmək əvəzinə deaktiv etmək daha təhlükəsizdir.

# Dəyəri dəyişsəniz
Yeni dəyər yalnız *bundan sonrakı* jetonlara tətbiq olunur — köhnə balanslar yenidən hesablanmır.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/jetonsaatlari/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/jetonsaatlari/index', N'isci-jeton-saatlari', N'İşçi Jeton Saatları', N'Jeton',
N'Kimin nə qədər jeton saatı var — qazanılmış, istifadə, qalıq.',
N'Bütün işçilərin jeton balansı bir cədvəldə.

# Sütunlar
- *Qazanılmış (saat)* — indiyədək verilən jetonların cəmi.
- *İstifadə (saat)* — icazəyə və s. xərclənən hissə.
- *Cari balans (saat)* — qalıq; işçi yalnız bunu xərcləyə bilər.

# Yuxarıdakı sayğaclar
*Ümumi işçi*, *Balansı olan işçi*, *Ümumi cari balans* — şirkət üzrə yekun mənzərə.

# Qara jeton
Ayrıca sütunda göstərilir. Cəza jetonudur, saat balansına əlavə olunmur.

# Balans səhv görünürsə
Rəqəmlər jeton qeydlərindən hesablanır — burada əl ilə düzəldilmir. Səhv varsa jeton siyahısında həmin qeydi tapıb düzəltmək lazımdır.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/mezuniyyetbalans/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/mezuniyyetbalans/index', N'mezuniyyet-balansi', N'Məzuniyyət Balansı', N'Məzuniyyət',
N'İşçilərin məzuniyyət günləri: toplam, istifadə, qalıq.',
N'Hər işçinin məzuniyyət günlərinin vəziyyəti.

# Sütunlar
- *Toplam gün* — həmin il üçün haqq qazanılmış günlər.
- *Qalıq* — hazırda istifadə edə biləcəyi günlər.
- *Qalıq məzuniyyət (bütöv)* — əvvəlki illərdən keçən günlərlə birlikdə.

# Balans Redaktəsi
Günü əl ilə düzəltmək üçün. *Ehtiyatlı olun* — bu rəqəm işçinin müraciət göndərə bilib-bilməməsini müəyyən edir.

# Qalıq gözlədiyinizdən az görünürsə
Ən çox rast gəlinən səbəb: məzuniyyət *təqvim günü* ilə sayılır. 20–24 avqust 5 gündür, 3 yox — şənbə və bazar da düşür. Yalnız məzuniyyətdə hesablanmayan bayramlar çıxılır.

# İl seçimi
Cədvəl seçilmiş ilin balansını göstərir. İşçinin cari il qeydi yoxdursa sətri boş görünə bilər — bu, əvvəlki ilin balansı demək deyil.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/mezuniyyet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/mezuniyyet/index', N'mezuniyyet-idareetmesi', N'Məzuniyyət İdarəetməsi', N'Məzuniyyət',
N'Bütün işçilərin məzuniyyət müraciətləri və HR təsdiqi.',
N'Şirkət üzrə bütün məzuniyyət müraciətləri.

# Filtrlər
Növ və status üzrə süzgəc. *Gözləyən* sayğacı sizdən cavab gözləyənlərin sayıdır.

# HR Təsdiqi
Müraciət şöbə rəisi və rəhbərdən keçəndən sonra sizə gəlir. Təsdiqləyəndə məzuniyyət rəsmiləşir və balansdan günlər düşür.

# Geriyə qeyd
Keçmiş tarixə məzuniyyət yazmaq üçün. Sistem burada da *üst-üstə düşməni* yoxlayır — həmin işçinin mövcud məzuniyyəti ilə bir gün toxunsa belə bloklanır və toqquşan qeydin tarixləri göstərilir.

# Ləğv
Təsdiqlənmiş məzuniyyəti ləğv edəndə balans avtomatik geri qayıdır və mühasibata ödənişi icra etməmək barədə bildiriş düşür.

# Gün sayı
Ödənilən gün *təqvim günüdür* — həftəsonu daxildir. Ekranlardakı «iş günü» göstəricisi ayrı məlumatdır; balansdan düşən rəqəm təqvim günüdür.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/davamiyyet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/davamiyyet/index', N'hr-davamiyyet', N'Davamiyyət (HR)', N'Davamiyyət',
N'Bütün işçilərin gündəlik giriş/çıxışı və statusları.',
N'Şirkət üzrə gündəlik davamiyyət. Məlumat *barmaq izi cihazından* gəlir.

# Yuxarıdakı kartlar
Gəlib, Gecikmə, İcazəli, Qayıb və s. Kartın üstünə klikləyəndə həmin işçilərin siyahısı açılır. *Say ilə siyahı həmişə eyni olmalıdır* — fərq görsəniz bildirin.

# Gecikmə toleransı
Neçə dəqiqə gecikmənin «gecikmə» sayılmayacağını təyin edir. Dəyişiklik *bundan sonrakı* günlərə tətbiq olunur — keçmiş qeydlər yenidən hesablanmır.

# İcazəli və Ezamiyyət
İki qrupdan ibarətdir: cihazda faktiki çıxanlar və həmin gün cihaz qeydi olmayan, amma təsdiqlənmiş icazəsi/ezamiyyəti olanlar. İkinci qrup siyahıda ayrıca sətir kimi görünür.

# Köhnə qeyd düzəlmir
Status cihaz qeydi yazılan anda hesablanır. İcazə sonradan təsdiqlənibsə köhnə gün *öz-özünə dəyişmir*. Ekranda düzəliş göstərilə bilər, amma bazadakı qeyd olduğu kimi qalır.

# Bayram Günləri
Bu ekrandan qeyri-iş günləri siyahısına keçid var — orada təyin edilən günlər davamiyyətə və məzuniyyət hesabına təsir edir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/xestelikezamiyyet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/xestelikezamiyyet/index', N'xestelik-ve-ezamiyyet', N'Xəstəlik və Ezamiyyət', N'Davamiyyət',
N'Xəstəlik vərəqələri və ezamiyyət qeydləri.',
N'İşçilərin xəstəlik və ezamiyyət qeydləri bir siyahıda.

# Növ
*Xəstəlik* və *Ezamiyyət* — filtrlə ayrılır. Hər ikisi davamiyyət statusuna təsir edir.

# Ezamiyyət
Ezamiyyət günü işçiyə *gecikmə yazılmır*. Saatlıq ezamiyyətdə işçi qayıdıb cihaza vursa status yenidən hesablanır və adətən «İşdə» olur.

# Xəstəlik
Xəstəlik günləri əsas maaşdan mütənasib çıxılır, yerinə xəstəlik ödənişi gəlir. Şirkət payı və DSMF payı ayrıca hesablanır.

# Tarix seçimi
Başlama və bitmə tarixi verirsiniz, sistem gün sayını özü hesablayır.

# Qeyd
Bu qeydlər *təsdiqləndikdən sonra* davamiyyətə düşür. Təsdiqlənməyən qeyd heç bir hesablamaya təsir etmir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/bayramgunu/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/bayramgunu/index', N'qeyri-is-gunleri', N'Qeyri-iş günləri (bayramlar)', N'Parametrlər',
N'Bayram və qeyri-iş günləri — davamiyyətə və məzuniyyətə təsir edir.',
N'Şirkətin qeyri-iş günləri. *Bu siyahı bir neçə hesablamanı idarə edir* — ehtiyatlı dəyişin.

# Nəyə təsir edir
- Davamiyyətdə həmin gün «qayıb» sayılmır.
- Ayın iş günü sayına düşmür, yəni maaş hesablamasına təsir edir.
- Məzuniyyət gününün sayılmasına təsir edə bilər.

# Tək gün əlavə etmək
Bitiş tarixini *boş buraxsanız* tək gün əlavə olunur. Bir neçə günlük bayram üçün hər iki tarixi doldurun.

# Saat sahələri
Başlanğıc və bitmə vaxtı yalnız günün bir hissəsi qeyri-iş olanda lazımdır (məsələn qısaldılmış iş günü).

# Diqqət — məzuniyyət
Əmək məzuniyyətində *təqvim günü* sayılır: şənbə-bazar çıxılmır. Yalnız məzuniyyətdə hesablanmayan bayramlar çıxılır. Yəni bura bayram əlavə etmək işçinin məzuniyyət gününü artıra bilər.

# Keçmiş günlər
Dəyişiklik keçmiş hesablamaları avtomatik yeniləmir. Keçmiş aya təsir edəcəksə maaş və davamiyyəti yenidən yoxlayın.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/isci/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/isci/index', N'iscilerin-idaresi', N'İşçilər', N'Kadr',
N'Bütün işçilərin siyahısı, axtarış və status.',
N'Şirkətin bütün işçiləri.

# Status
- *Aktiv* — işləyən işçi.
- *Məzuniyyətdə* — hazırda təsdiqlənmiş məzuniyyətdədir.
- *İşdən çıxıb* — ayrılma tarixi və səbəbi qeyd olunur.

# Sıralama
Siyahı *İşçi Sıralaması* səhifəsində təyin etdiyiniz sıra ilə gəlir; eyni sırada olanlar ad-soyad əlifbası ilə düzülür. Əlifba sırası gözləyirsinizsə orada sıraları sıfırlamaq lazımdır.

# İşçini işdən çıxarmaq
Ayrılma tarixi və səbəb yazılır. *Diqqət:* işçi çıxarılanda təyinat sətri avtomatik bağlanmır — departament və organizasiya sxemi hesabatlarında görünməsini istəmirsinizsə təyinatı da bağlamaq lazımdır.

# Axtarış
Ad, soyad üzrə. *Sıfırla* bütün filtrləri təmizləyir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/iscisiralama/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/iscisiralama/index', N'isci-siralamasi', N'İşçi Sıralaması', N'Kadr',
N'İşçilərin sistemdəki görünüş sırası — sürüşdürüb düzürsünüz.',
N'İşçiləri siyahılarda hansı sıra ilə görmək istəyirsinizsə burada düzürsünüz.

# Necə işləyir
Sətri tutub yuxarı-aşağı sürüşdürün, sonra *Yadda saxla*.

# Harada görünür
Bu sıra *bütün işçi siyahılarında* tətbiq olunur — İşçilər, maaş ekranları, hesabatlar. Yəni bir dəfə düzəndə hər yerdə düzəlir.

# Eyni sırada olanlar
Sıra nömrəsi eyni olan işçilər öz aralarında ad-soyad əlifbası ilə düzülür.

# Yalnız aktiv işçilər
Siyahıda işdən çıxmış işçilər görünmür — onları sıralamağa ehtiyac yoxdur.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/departament/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/departament/index', N'departamentler', N'Departamentlər', N'Struktur',
N'Şöbələrin yaradılması və redaktəsi.',
N'Şirkətin şöbələri.

# Nə edirsiniz
Yeni departament əlavə edir, adını və açıqlamasını dəyişir, lazımsızı silirsiniz.

# İşçi sayı boş görünürsə
Departamentin altında işçi görünmürsə səbəb adətən *təyinat sətridir*: işçinin təyinatı bağlanmış kimi qeyd olunub, halbuki işçi aktivdir. Belə halda işçinin kartından təyinatı yoxlayın.

# Silmək
Departamenti silməzdən əvvəl altındakı işçiləri və vəzifələri başqa şöbəyə keçirin — əks halda onlar şöbəsiz qalar.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/vezife/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/vezife/index', N'vezifeler', N'Vəzifələr', N'Struktur',
N'Vəzifə adları və hansı departamentə aid olduqları.',
N'Şirkətdəki vəzifələr.

# Sahələr
Vəzifə adı, aid olduğu departament və təsvir.

# Aktiv / Deaktiv
Artıq işlədilməyən vəzifəni *silmək əvəzinə deaktiv edin* — köhnə işçi qeydləri həmin vəzifəyə istinad edir, silinsə tarixçə pozula bilər. Deaktiv vəzifə yeni təyinatda seçilmir.

# Departament dəyişikliyi
Vəzifənin departamentini dəyişsəniz, həmin vəzifədə olan işçilər avtomatik köçmür — onların təyinatını ayrıca yeniləmək lazımdır.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/organizasiya/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/organizasiya/index', N'organizasiya-sxemi', N'Organizasiya Sxemi', N'Struktur',
N'Şirkətin struktur ağacı — departament, vəzifə, işçi.',
N'Şirkətin strukturu ağac şəklində. *Yığ* düyməsi bütün budaqları bağlayır.

# Kim görünür
Yalnız *aktiv təyinatı olan aktiv işçilər*. Yəni işdən çıxmış işçi və ya bağlanmış təyinat sxemdə görünmür.

# İşçi görünmürsə
İşçi «İşçilər» siyahısında var, amma sxemdə yoxdursa səbəb təyinat sətridir — təyinat aktiv işarələnməyib və ya bağlanmış görünür. İşçinin kartından təyinatı yoxlayın.

# Sxem yalnız göstərir
Burada dəyişiklik edilmir — struktur departament, vəzifə və işçi təyinatlarından qurulur.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/emr/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/emr/index', N'emr-sayghaclari', N'Əmr Sayğacları', N'Sənəd',
N'Əmr nömrələrinin sayğacı — növ üzrə son və növbəti nömrə.',
N'Hər əmr növü üçün nömrə sayğacı.

# Sütunlar
- *Son nömrə* — indiyədək verilmiş SONUNCU nömrə.
- *Növbəti* — növbəti əmrə veriləcək nömrə (son nömrə + 1).

# Ehtiyatlı olun
Sayğacı əl ilə dəyişsəniz nömrələr *təkrarlana* və ya *atlana* bilər. Verilmiş nömrə geri qaytarılmır — əmr silinsə belə həmin nömrə yenidən istifadə edilmir.

# Nə vaxt dəyişmək lazım gəlir
Sistemə keçiddə köhnə jurnalın son nömrəsini bura yazmaq lazım olur ki, yeni əmrlər 1-dən başlamasın.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/muqavilebitme/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/muqavilebitme/index', N'muqavile-bitme-tarixi', N'Müqavilə Bitmə Tarixi', N'Sənəd',
N'Müddəti yaxınlaşan və keçmiş əmək müqavilələri.',
N'Əmək müqaviləsi bitən işçilər — vaxtında yeniləmək üçün.

# Rənglər
- *Bugün!* — müqavilə bu gün bitir.
- *Diqqət (8–30 gün)* — yaxınlaşır.
- *Müddəti keçib* — artıq bitib, təcili baxılmalıdır.

# Filtr
*Növbəti N gün* ilə pəncərəni dəyişirsiniz; *Bütün il* və *Hamısı* seçimləri də var. Ad və ya FIN üzrə axtarış edə bilərsiniz.

# Bu səhifə bildiriş göndərmir
Siyahı passivdir — yalnız göstərir. Müqavilə yenilənməsi barədə bildiriş ayrı mexanizmlə gedir.

# Excel
Siyahını Excel-ə çıxarıb rəhbərliyə göndərmək olar.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/guzest/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/guzest/index', N'vergi-guzestleri', N'Vergi Güzəştləri Kataloqu', N'Parametrlər',
N'Güzəşt növləri və məbləğləri — maaş hesablamasına təsir edir.',
N'Gəlir vergisi güzəştlərinin kataloqu (Məcburi köçkün, Əlillik və s.).

# ⚠ Bu səhifə maaşa təsir edir
Buradakı məbləğ işçinin *gəlir vergisi bazasından* çıxılır. Səhv dəyər bütün maaşları pozar — dəyişməzdən əvvəl mühasibatla razılaşın.

# Sahələr
- *Maddə* — qanunun hansı maddəsinə əsaslandığı.
- *Məbləğ* — aylıq güzəşt.
- *Status* — Aktiv güzəştlər seçim siyahısında görünür.

# Kataloq ≠ təyinat
Burada yalnız *növlər* saxlanılır. Konkret işçiyə güzəşt təyin etmək ayrı səhifədədir (İşçi güzəştləri).

# Dəyişiklik nə vaxtdan işləyir
Yeni məbləğ *bundan sonrakı* hesablamalara tətbiq olunur. Keçmiş ayı düzəltmək lazımdırsa həmin ayı yenidən hesablamaq gərəkir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/iscihys/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/iscihys/index', N'hyat-yigim-sigortasi', N'Həyat Yığım Sığortası (HYS)', N'Parametrlər',
N'İşçilərin HYS müqavilələri və aylıq məbləğ.',
N'İşçilərin həyat yığım sığortası təyinatları.

# ⚠ Bu səhifə maaşa təsir edir
Aylıq HYS məbləği maaş hesablamasında həm *tutulma*, həm də *vergi bazası* kimi nəzərə alınır. Səhv məbləğ işçinin əlinə çatan pulu dəyişər.

# Sahələr
- *Aylıq HYS* — hər ay tutulacaq məbləğ.
- *Sığorta şirkəti*, *Başlama* və *Bitmə* tarixi.
- *Status* — Aktiv, Gözləyir, Bitib.

# Bitmə tarixi
Tarix keçəndən sonra tutulma dayanır. Müqavilə uzadılıbsa bitmə tarixini yeniləyin — əks halda tutulma kəsilər.

# 3 il tamam olmadan qayıdış
Müqavilə vaxtından əvvəl bağlanıb məbləğ qaytarılırsa bu, ayrıca vergi nəticəsi doğurur. Belə halda mühasibatla əlaqə saxlayın.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/tabel/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/tabel/index', N'tabel', N'Tabel', N'Davamiyyət',
N'Aylıq iş vaxtı tabeli — hər işçi üçün gün-gün.',
N'Ayın hər günü üçün işçilərin vəziyyəti bir cədvəldə.

# Kodlar
Cədvəldəki hərf və rəqəmlər gün statusunu bildirir. Kodların açılışı səhifənin yuxarısındakı *Kodlar* bölməsindədir.

# Məlumat haradan gəlir
Davamiyyət qeydlərindən, təsdiqlənmiş məzuniyyət, icazə, xəstəlik və ezamiyyətdən avtomatik qurulur — burada əl ilə yazılmır.

# Rəqəm səhv görünürsə
Mənbə qeydi düzəltmək lazımdır: davamiyyət, məzuniyyət və ya xəstəlik səhifəsindən. Tabel düzəldiləndən sonra özü yenilənir.

# Ay seçimi
Yuxarıdan ayı dəyişirsiniz. Bağlanmış ayın tabeli sonradan dəyişsə, maaşı yenidən yoxlamaq lazım gələ bilər.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/telim/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/telim/index', N'telim-ve-sertifikatlar', N'Təlim və Sertifikatlar', N'Kadr',
N'İşçilərin təlimləri, sertifikatları və müddətləri.',
N'İşçilərin keçdiyi təlimlər və aldığı sertifikatlar.

# Sahələr
Sertifikat adı, növü, məkan, başlama və bitmə tarixi.

# Müddətsiz sertifikat
*Müddətsiz* işarələnibsə bitmə tarixi tələb olunmur və sertifikat heç vaxt «Bitib» statusuna keçmir.

# Status
- *Aktiv* — qüvvədədir.
- *Bitib* — müddəti keçib, yenilənməlidir.

# Nə üçün lazımdır
Müddəti bitən sertifikatları vaxtında görüb yeniləmək üçün. Bu səhifə avtomatik bildiriş göndərmir — vaxtaşırı baxmaq lazımdır.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/performans/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/performans/index', N'performans-qiymetlendirme', N'Performans Qiymətləndirmə', N'Kadr',
N'Rüblük performans qiymətləndirmələri və orta bal.',
N'İşçilərin dövr üzrə performans qiymətləndirməsi.

# Filtrlər
Departament və rüb üzrə süzürsünüz. *Ort. Qiymət* həmin işçinin dövr üzrə orta balıdır.

# Status
- *Gözləmədə* — qiymətləndirmə hələ başlamayıb.
- *Davam edir* — rəhbər dolduma mərhələsindədir.

# Kim qiymətləndirir
İşçinin rəhbəri. Siz burada gedişatı izləyir və yekunu görürsünüz.

# Bal necə çıxır
Qiymətləndirmə parametrləri ayrıca səhifədə təyin olunur; orta bal həmin parametrlərin nəticəsidir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/elan/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/elan/index', N'elanlar', N'Elanlar', N'Ümumi',
N'İşçilərə göstərilən elanların idarəsi.',
N'İşçilərin əsas səhifəsində görünən elanlar.

# Yaratmaq
Başlıq, mətn və bitirmə tarixi verilir. Bitirmə tarixi keçəndən sonra elan avtomatik görünmür.

# Vacib
*Vacib* işarələnən elan siyahının başında və fərqlənən şəkildə göstərilir. Hər elanı vacib etməyin — onda heç biri seçilmir.

# Aktiv
Deaktiv edilən elan silinmir, sadəcə işçilərə görünmür. Sonradan yenidən aktivləşdirə bilərsiniz.

# Kim görür
Elan bütün işçilərə göstərilir.',
0, 0, 0, GETDATE(), 0);


/* ══ ADMIN · AVTOPARK · SƏNƏD DÖVRİYYƏSİ · ƏMƏLİYYAT  (27) ═════════════ */

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/strukturrolu/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/strukturrolu/index', N'struktur-rollari', N'Struktur Rolları', N'Admin',
N'Kim şöbə rəisi, rəhbər, HR və mühasibdir — təsdiq axını buradan qurulur.',
N'Kimin *şöbə rəisi*, *rəhbər*, *HR* və ya *mühasib* olduğunu burada təyin edirsiniz.

# ⚠ Bu səhifə təsdiq axınını idarə edir
Məzuniyyət və icazə müraciəti *məhz buradakı rollara* görə yönləndirilir. Rol təyin edilməyibsə müraciət həmin addıma ümumiyyətlə getmir — heç bir xəta çıxmır, sadəcə bildiriş çatmır.

# Real hadisə
HR işçisinə struktur rolu verilməmişdi. Nəticədə müqavilə yenilənməsi bildirişi rəhbərə getdi, HR-a getmədi. Səbəb kodda deyil, məhz bu səhifədəki boşluqda idi.

# Yuxarıdakı sayğaclar
*Aktiv Şöbə Rəisi / Rəhbər / HR / Mühasib* — hər rolda neçə nəfər var. Rəqəm gözlədiyinizdən azdırsa boşluq var deməkdir.

# Bir işçidə bir neçə rol
Mümkündür və normaldır. Belə halda müraciətin hansı addımdan keçəcəyini rolların sırası həll edir — məzuniyyət və icazə modullarında bu sıra *qəsdən fərqlidir*.

# Rolu ləğv etmək
Ləğv edilən rol keçmiş müraciətləri dəyişmir; yalnız bundan sonrakı marşruta təsir edir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/oraclesorgu/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/oraclesorgu/index', N'oracle-sorgular', N'Oracle Sorğular', N'Admin',
N'BMI (Oracle) bazasından oxunan sorğuların kataloqu.',
N'Hesabatların BMI (Oracle) bazasından məlumat çəkdiyi sorğular burada saxlanılır.

# ⚠ BMI bazası YALNIZ OXUNUR
Yalnız *SELECT* icazəlidir. INSERT, UPDATE, DELETE və struktur dəyişikliyi *qəti qadağandır* — nə birbaşa, nə dolayı. Bu qayda istisnasızdır.

# Sahələr
- *Sorğu adı* — hesabatın kodda istinad etdiyi ad. Dəyişsəniz hesabat sorğunu tapmaz.
- *Şöbə* və *Mahiyyət* — kataloqda tapmaq üçün.
- *Status* — Deaktiv sorğu işə düşmür.

# Sorğunu dəyişməzdən əvvəl
Onu işlədən hesabatı tapıb yoxlayın. Bir sorğu bir neçə ekranı qidalandıra bilər — sütun adını dəyişsəniz hesabat səssizcə boş gələ bilər.

# Xəta alsanız
«TNS» və ya bağlantı xətaları adətən şəbəkə problemidir, sorğunun səhvi deyil — sistem belə hallarda özü təkrar cəhd edir. Sintaksis xətası isə sorğunun özündədir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/sistemicaze/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/sistemicaze/index', N'sistem-icazeleri', N'Sistem İcazələri', N'Admin',
N'Səhifə və əməliyyat icazələri — kimin nəyə girişi var.',
N'Sistemdəki icazələrin kataloqu və hansı istifadəçiyə verildiyi.

# İki hissə
- Solda *icazələr* — sistemdəki icazə növləri.
- Sağda *istifadəçilər* — həmin icazənin kimə verildiyi.

# Kod
Hər icazənin dəyişməz açarı var (məsələn `risk_panel_bax`). *Kodu sonradan dəyişməyin* — kod koda bağlıdır, dəyişsə həmin qoruma işləməz və səhifə hamıya açıla bilər.

# Yeni icazə
Yalnız kodda ona uyğun yoxlama varsa məna daşıyır. Sırf burada yaratmaq heç nəyi məhdudlaşdırmır.

# İcazəni geri almaq
Dərhal işə düşür — istifadəçinin növbəti sorğusunda tətbiq olunur.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/usermanagement/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/usermanagement/index', N'istifadeciler', N'İstifadəçilər', N'Admin',
N'Sistem istifadəçiləri, rolları və statusu.',
N'Sistemə girə bilən hesablar.

# Sütunlar
İstifadəçi adı, tam ad, e-poçt, rollar, qeydiyyat tarixi və status.

# Status
- *Active* — hesab işləkdir.
- *Inactive* — giriş bağlıdır.
- *Locked* — bir neçə səhv paroldan sonra kilidlənib; buradan açılır.

# Rol ≠ struktur rolu
Buradakı rol *sistemə giriş səlahiyyətidir* (Admin, HR və s.). Təsdiq axınını isə *Struktur Rolları* səhifəsi idarə edir. İkisi ayrı şeydir — biri verilib, o biri unudula bilər.

# İşçi ilə əlaqə
Hesab işçi kartı ilə bağlıdır. Bağlantı yoxdursa istifadəçi öz məzuniyyət və davamiyyət məlumatını görməz.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/rolemanagement/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/rolemanagement/index', N'rollar', N'Rollar', N'Admin',
N'Sistem rollarının siyahısı.',
N'Sistemdəki rollar (Admin, HR, Rəhbər, Operator və s.).

# Nəyə təsir edir
Rol istifadəçinin hansı bölmələri görəcəyini müəyyən edir.

# Silməzdən əvvəl
Rolu silmək həmin roldakı istifadəçiləri səlahiyyətsiz qoyur. Əvvəlcə istifadəçiləri başqa rola keçirin.

# Diqqət
Rol adları kodda yoxlanılır — adı dəyişsəniz həmin rola bağlı qorumalar işləməz.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/fealiyyetjurnali/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/fealiyyetjurnali/index', N'fealiyyet-jurnali', N'Fəaliyyət Jurnalı', N'Admin',
N'Kim nə vaxt nəyi yaratdı, dəyişdi və ya sildi.',
N'Sistemdəki dəyişikliklərin izi.

# Filtrlər
İstifadəçi, cədvəl, əməliyyat növü (Yaratdı / Yenilədi / Sildi) və tarix aralığı.

# Nə üçün lazımdır
«Bu qeydi kim dəyişib?» sualına cavab verir. Mübahisəli hallarda ilk baxılası yerdir.

# Nə göstərmir
Jurnal *hansı sətrin* dəyişdiyini göstərir, amma köhnə və yeni dəyəri həmişə saxlamır. Yəni «əvvəl nə yazılmışdı» sualına həmişə cavab verə bilmir.

# Silinmiş qeyd
Sistemdə silmə adətən *yumşaq silmədir* — qeyd bazada qalır, sadəcə görünmür.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/sistemayar/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/sistemayar/index', N'sistem-ayarlari', N'Sistem Ayarları', N'Admin',
N'Mail (IMAP) və SMS ayarları.',
N'Sistemin xarici xidmətlərlə əlaqə ayarları.

# Kredit Mail (IMAP)
Kredit müraciətlərinin oxunduğu poçt qutusu: server, port (SSL üçün adətən 993), ünvan və şifrə.

# PİD — Toplu SMS
SMS göndərişi üçün ayarlar.

# Şifrə
Bir dəfə yazıldıqdan sonra ekranda göstərilmir — *Şifrə qeydə alınıb* yazısı görünür. Dəyişmək üçün yenisini yazıb saxlayın; boş buraxsanız köhnəsi qalır.

# Dəyişiklikdən sonra
Bağlantını sınayın. Səhv ayar səssiz qalır — mail oxunmur, amma xəta görünmür.',
0, 1, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/loginlogs/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/loginlogs/index', N'giris-jurnali', N'Giriş Jurnalı', N'Admin',
N'Sistemə giriş cəhdləri — uğurlu və uğursuz.',
N'Kimin nə vaxt sistemə girdiyi.

# Nəyə baxmaq
Uğursuz cəhdlərin çoxluğu ya parolun unudulduğunu, ya da kənar cəhdi göstərir.

# Hesab kilidlənibsə
Bir neçə səhif paroldan sonra hesab kilidlənir. Açmaq üçün *İstifadəçilər* səhifəsinə keçin.

# Saxlanma müddəti
Jurnal zamanla böyüyür — köhnə qeydlərin təmizlənməsi ayrıca qərardır.',
0, 1, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/kreditbaxanisci/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/kreditbaxanisci/index', N'kredit-baxan-isciler', N'Kredit Baxan İşçilər', N'Admin',
N'Kredit məlumatlarına baxa bilən işçilər.',
N'Kredit portfelinə giriş icazəsi olan işçilər.

# Sahələr
Ad, şöbə, vəzifə, icazə tarixi və qeyd.

# Status
*Aktiv* — giriş var. *Ləğv edilib* — giriş bağlanıb, amma qeyd tarixçə üçün qalır.

# Niyə silinmir
Kim nə vaxt kreditə baxma icazəsi almışdı — bu, audit sualıdır. Ona görə qeyd silinmir, yalnız ləğv edilir.',
0, 1, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'admin/komiteuzvu/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'admin/komiteuzvu/index', N'komite-uzvleri', N'Komitə Üzvləri', N'Admin',
N'Kredit komitəsinin tərkibi və təyinat tarixləri.',
N'Kredit komitəsinin üzvləri.

# Sahələr
Ad, şöbə/vəzifə, təyinat tarixi və rol.

# Status
*Aktiv* üzvlər qərar prosesində iştirak edir. *Ləğv edilib* qeydi tarixçə üçün saxlanılır.

# Nə üçün vacibdir
Komitə qərarlarının kim tərəfindən verildiyi sənədləşir. Tərkibi dəyişəndə köhnə qərarlar toxunulmaz qalır.',
0, 1, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'avtopark/masin/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'avtopark/masin/index', N'masinlar', N'Maşınlar', N'Avtopark',
N'Şirkət maşınları, sürücüləri və cari vəziyyəti.',
N'Şirkətin avtoparkı.

# Sütunlar
Maşın, dövlət nömrəsi, departament, sürücü və *cari vəziyyət* (yerindədir / çıxıb).

# Ən yaxın müddət
Sığorta, texniki baxış və s. üzrə ən yaxın bitən müddəti göstərir — vaxtında yeniləmək üçün. Detallar *Maşın müddətləri* səhifəsindədir.

# Status
Deaktiv maşın müraciət formasında seçilmir, amma köhnə qeydlərdə qalır.

# Sürücü
Təyin edilmiş sürücü müraciətlərdə və yol vərəqəsində avtomatik gəlir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'avtopark/muddet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'avtopark/muddet/index', N'masin-muddetleri', N'Maşın Müddətləri', N'Avtopark',
N'Sığorta, texniki baxış və digər müddətlərin izlənməsi.',
N'Hər maşın üçün müddəti bitən sənədlər.

# Növlər
Sığorta, texniki baxış və s. Növlərin siyahısı *Növlər* düyməsindən idarə olunur.

# Son tarix
Yaxınlaşan və keçmiş müddətlər fərqli göstərilir. Maşınlar səhifəsindəki *Ən yaxın müddət* sütunu buradan gəlir.

# Yeniləmə
Sənəd yeniləndikdə yeni son tarix yazılır. Köhnə qeydi silməyin — tarixçə itər.

# Bildiriş
Bu səhifə avtomatik xəbərdarlıq göndərmir; vaxtaşırı baxmaq lazımdır.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'avtopark/muraciet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'avtopark/muraciet/index', N'masin-muracietlerim', N'Maşın Müraciətlərim', N'Avtopark',
N'Maşın istəyi göndərmək və gedişatına baxmaq.',
N'Xidməti maşın üçün müraciətləriniz.

# Yeni müraciət
Maşın, *çıxış tarixi və saatı*, məqsəd yazılır.

# Bitmə vaxtı soruşulmur
Qayıdış vaxtı əvvəlcədən bilinmir. Maşını qaytaranda *kassa* faktiki qayıdışı qeyd edir — ona görə formada belə bir xana yoxdur.

# Status
Müraciət təsdiqə gedir. *Rəhbər addımı atlandı* yazısı görünürsə, rolunuza görə həmin addım lazım deyil — səhv deyil.

# Açar
Təsdiqdən sonra açarı *kassa* verir. Təsdiqlənmiş müraciət avtomatik olaraq maşını sizə vermir.

# Faktiki
Çıxış və qayıdış vaxtları kassa tərəfindən yazılır və burada görünür.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'avtopark/tesdiq/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'avtopark/tesdiq/index', N'masin-muracietleri-tesdiq', N'Maşın Müraciətləri — Təsdiq', N'Avtopark',
N'Maşın istəklərinin təsdiqi.',
N'Sizdən təsdiq gözləyən maşın müraciətləri.

# Qərar
Təsdiq və ya imtina. İmtinada səbəb yazmaq faydalıdır — işçi ekranda görür.

# Təsdiqdən sonra
Açarı *kassa* verir. Yəni sizin təsdiqiniz maşını avtomatik ayırmır; işçi kassaya getməlidir.

# Toqquşma
Eyni vaxta iki müraciət olarsa sistem çıxış anında qarşısını alır — maşın artıq çıxıbsa ikinci açar verilmir.

# Siyahı boşdursa
*Təsdiq gözləyən müraciət yoxdur* — hazırda işiniz yoxdur.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'avtopark/kassa/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'avtopark/kassa/index', N'acar-jurnali', N'Açar Jurnalı (Kassa)', N'Avtopark',
N'Açarın verilməsi və maşının qayıtmasının qeydiyyatı.',
N'Maşın açarlarının verilib-qaytarılması.

# İki addım
1. *Çıxış* — təsdiqlənmiş müraciətə açar verilir, çıxış vaxtı yazılır.
2. *Qayıdış* — maşın qayıdanda burada qeyd edilir.

# Qayıdış vaxtı buradan yazılır
İşçi müraciətdə qayıdış vaxtı göstərmir — faktiki qayıdışı məhz kassa yazır.

# Qayıdış yazılmasa
Maşın «çıxıb» qalır və başqasına verilə bilmir. Gün sonunda açıq çıxışları yoxlayın.

# Siyahılar
*Açar gözləyən yoxdur* — təsdiqlənmiş, amma hələ açar götürülməmiş müraciət yoxdur.
*Bütün maşınlar yerindədir* — qayıtmamış maşın yoxdur.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'avtopark/yolvereqesi/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'avtopark/yolvereqesi/index', N'yol-vereqesi', N'Yol Vərəqəsi', N'Avtopark',
N'Maşın və dövr seçib yol vərəqəsi çıxarırsınız.',
N'Seçilmiş maşın və dövr üçün yol vərəqəsi hazırlayır.

# Nə seçilir
Maşın, sürücü, dövrün başlanğıcı və sonu.

# Məlumat haradan gəlir
Kassa jurnalındakı çıxış-qayıdış qeydlərindən. Yəni kassa qeydləri natamamdırsa vərəqə də natamam olar.

# Aktiv maşın yoxdursa
Siyahı boş gəlirsə maşınların statusu deaktivdir — *Maşınlar* səhifəsindən yoxlayın.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'seneddovriyyesi/dashboard/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'seneddovriyyesi/dashboard/index', N'sened-dovriyyesi-esas', N'Sənəd Dövriyyəsi — Əsas', N'Sənəd',
N'Sənədlər, məktublar və həvalə jurnallarına giriş.',
N'Sənəd dövriyyəsi modulunun ana səhifəsi.

# Bölmələr
- *Sənədlər* — daxili sənəd arxivi.
- *Məktublar* — daxil olan və xaric olan məktub jurnalları.
- *Həvalələr* — gedən və gələn pul köçürmələrinin jurnalı.

# Sayğaclar
Ümumi, bu gün əlavə olunan və arxivdəki sənədlərin sayı.

# Silinmişlər
Silinmiş sənədlər tamamilə yox olmur — ayrıca bölmədə saxlanılır və lazım olsa bərpa edilə bilər.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'seneddovriyyesi/sened/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'seneddovriyyesi/sened/index', N'senedler', N'Sənədlər', N'Sənəd',
N'Sənəd arxivi — axtarış, filtr və fayl əlavəsi.',
N'Şirkətin sənəd arxivi.

# Filtrlər
Şöbə, növ, sənəd tarixi və ya yaradılma tarixi üzrə. Sıralamanı böyükdən kiçiyə dəyişə bilərsiniz.

# Fayl əlavəsi
Sənədə fayl qoşulur. Fayllar server qovluğunda saxlanılır, bazada yalnız yol qeyd olunur.

# Versiya
Eyni sənədə yeni fayl əlavə edəndə köhnəsi itmir — versiya kimi saxlanılır.

# İcazə
Bəzi sənədləri yalnız icazəsi olan istifadəçilər görür. Sənəd görünmürsə səbəb icazə ola bilər, silinmə deyil.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'seneddovriyyesi/daxilmektub/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'seneddovriyyesi/daxilmektub/index', N'daxil-olan-mektublar', N'Daxil Olan Məktublar', N'Sənəd',
N'Bankа gələn məktubların jurnalı.',
N'Kənardan gələn məktubların qeydiyyatı.

# Filtrlər
İl, icraçı, tarix aralığı və mətn axtarışı.

# Qeydiyyat nömrəsi
Nömrə həmin ilin jurnalından ardıcıl verilir. *Nömrə geri qaytarılmır* — qeyd silinsə də həmin nömrə yenidən istifadə olunmur.

# İlk dəfə işlədirsinizsə
Cari ilin köhnə məktubları BMI-dən köçürülməyibsə nömrə 1-dən başlayar və köhnə nömrələrlə toqquşar. Əvvəlcə *Məktub jurnalı — BMI-dən köçürmə* səhifəsindən idxal edin.

# Qoşma
Məktubun skan surətini fayl kimi əlavə edə bilərsiniz.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'seneddovriyyesi/xaricmektub/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'seneddovriyyesi/xaricmektub/index', N'xaric-olan-mektublar', N'Xaric Olan Məktublar', N'Sənəd',
N'Bankdan göndərilən məktubların jurnalı.',
N'Bankdan kənara göndərilən məktublar.

# Nömrə
Həmin ilin jurnalından ardıcıl verilir və *geri qaytarılmır* — məktub silinsə də nömrə yenidən verilmir, çünki sənəd artıq o nömrə ilə göndərilib.

# Avtomatik yaranan məktublar
Bəzi məktublar (məsələn kredit girovu ilə bağlı) müqavilə hazırlanarkən avtomatik jurnala düşür.

# İlk dəfə işlədirsinizsə
Cari ili BMI-dən idxal etmədən real nömrə verməyin — nömrələr köhnə jurnalla toqquşar.

# Filtrlər
İl, icraçı, tarix aralığı və axtarış.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'seneddovriyyesi/gedenhevale/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'seneddovriyyesi/gedenhevale/index', N'geden-hevaleler', N'Gedən Həvalələr', N'Həvalə',
N'Gedən pul köçürmələrinin ƏSAS jurnalı.',
N'Gedən pul köçürmələrinin jurnalı. *Bu, əsas jurnaldır* — həvalə nömrəsi buradan verilir.

# Nömrə
Format `{İL}-T-{N}`. Nömrə həmin ilin jurnalından ardıcıl gəlir və *geri qaytarılmır*.

# Əməliyyat modulu ilə əlaqə
Əməliyyat → *Pul köçürməsi* səhifəsində yaradılan köçürmə buraya da sətir yazır və eyni nömrəni daşıyır. Yəni iki ayrı jurnal deyil — biridir.

# Köçürmədən gələn sətri silmək
Mümkün deyil. Belə sətri silmək üçün köçürmənin özünü silmək lazımdır; sistem sizi ora yönləndirir.

# Sahə uzunluqları
Bəzi sahələr BMI ölçüsündədir (məsələn Müqavilə № 50 simvol). Limitdən uzun mətn yazsanız ekranda konkret xəbərdarlıq çıxır və qeyd yazılmır.

# Qoşma
Sənədin skan surətini əlavə edə bilərsiniz (PDF, şəkil, Word — maks. 30 MB).',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'seneddovriyyesi/gelenhevale/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'seneddovriyyesi/gelenhevale/index', N'gelen-hevaleler', N'Gələn Həvalələr', N'Həvalə',
N'Gələn pul köçürmələrinin jurnalı.',
N'Bankа gələn pul köçürmələri.

# Nömrə ƏL İLƏ yazılır
Gedən həvalədən fərqli olaraq burada nömrəni *siz* yazırsınız — jurnaldan götürülür.

# Dublikat yoxlaması
Sistem eyni nömrənin təkrarını yoxlayır. Silinmiş qeydlər bu yoxlamada *qəsdən sayılmır* — səhv nömrə yazılıb qeyd silinibsə düzgün nömrəni yenidən yaza bilirsiniz.

# Filtrlər
İl, icraçı, tarix aralığı və axtarış.

# Qoşma
Sənəd surətini fayl kimi əlavə edin.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'seneddovriyyesi/hevaleimport/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'seneddovriyyesi/hevaleimport/index', N'hevale-idxali', N'Həvalə Jurnalı — BMI-dən Köçürmə', N'Həvalə',
N'Köhnə həvalə qeydlərinin BMI-dən idxalı.',
N'BMI (Oracle) bazasındakı köhnə həvalə jurnalını FinNex-ə köçürür.

# ⚠ Niyə vacibdir
FinNex həvalə nömrəsini *həmin ilin öz qeydlərindən* hesablayır. Cari il idxal edilməyibsə nömrə *1-dən başlayar* və köhnə nömrələrlə toqquşar. Real nömrə verməzdən əvvəl ən azı cari ili idxal edin.

# Təkrar işlətmək
Təhlükəsizdir — mövcud nömrələr yenidən yazılmır, yalnız çatışmayanlar əlavə olunur.

# İl seçimi
İdxal ediləcək ili seçirsiniz. Mənbədə il sütunu yoxdursa tarixdən çıxarılır; tarixsiz qeydlər ayrıca göstərilir.

# BMI-yə heç nə yazılmır
İdxal *yalnız oxuyur*. BMI bazasında heç bir dəyişiklik edilmir.',
0, 1, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'seneddovriyyesi/mektubimport/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'seneddovriyyesi/mektubimport/index', N'mektub-idxali', N'Məktub Jurnalı — BMI-dən Köçürmə', N'Sənəd',
N'Köhnə məktub qeydlərinin BMI-dən idxalı.',
N'BMI bazasındakı köhnə məktub jurnalını FinNex-ə köçürür.

# ⚠ Niyə vacibdir
Məktub qeydiyyat nömrəsi həmin ilin FinNex qeydlərindən hesablanır. İl idxal edilməyibsə nömrə 1-dən başlayar və köhnə jurnalla toqquşar.

# Sorğu Admin-dədir
İdxalın hansı məlumatı çəkəcəyini *Admin → Oracle Sorğular* bölməsindəki sorğu müəyyən edir.

# Təkrar işlətmək
Təhlükəsizdir — mövcud qeydlər yenidən yazılmır.

# BMI-yə heç nə yazılmır
Yalnız oxuma əməliyyatıdır.',
0, 1, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'emeliyyat/dashboard/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'emeliyyat/dashboard/index', N'emeliyyat-esas', N'Əməliyyat Departamenti — Əsas', N'Əməliyyat',
N'Pul köçürməsi və tələbə köçürməsi bölmələrinə giriş.',
N'Əməliyyat departamentinin ana səhifəsi.

# Bölmələr
- *Pul köçürməsi* — adi pul köçürmə əməliyyatları.
- *Tələbə köçürməsi* — təhsil haqqı köçürmələri (ayrı nömrə fəzası).

# Qeyd
İki bölmənin nömrələri *qarışmır* — hərəsinin öz prefiksi var.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'emeliyyat/kocurme/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'emeliyyat/kocurme/index', N'pul-kocurmesi', N'Pul Köçürməsi', N'Əməliyyat',
N'Köçürmə əməliyyatları — ərizə və jurnal qeydi.',
N'Pul köçürmə əməliyyatları.

# Həvalə nömrəsi
Nömrə *Gedən həvalə* jurnalından gəlir. Köçürmə yaradılanda həmin jurnala da sətir yazılır — iki ayrı jurnal deyil, biridir.

# Word ərizəsi
Hər köçürmə üçün ərizə çıxarıla bilər.

# ⚠ Ərizə yükləndiyi andakı vəziyyəti göstərir
Fayl endirildikdən sonra *özü yenilənmir*. Qeydi sonradan dəyişsəniz köhnə fayl köhnə rəqəmlə qalır. Rəqəm uyğunsuzluğu görsəniz ərizəni yenidən çıxarın.

# Məbləğ
Rial və Rubl köçürmələrində sənədin əsas rəqəmi *köçürülən* məbləğdir (məbləğ × məzənnə), müştəridən alınan yox.

# Redaktə və silmə
Köçürməni dəyişsəniz jurnal sətri də yenilənir; silsəniz jurnaldan da silinir.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'emeliyyat/telebekocurme/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'emeliyyat/telebekocurme/index', N'telebe-kocurmesi', N'Tələbə Köçürməsi', N'Əməliyyat',
N'Təhsil haqqı köçürmələri.',
N'Tələbələrin təhsil haqqı köçürmələri.

# Sahələr
Tələbə, universitet, məbləğ və komissiya.

# Nömrə fəzası ayrıdır
Tələbə köçürməsinin nömrə prefiksi adi pul köçürməsindən *fərqlidir* — nömrələr qarışmır.

# Komissiya
Bankın tutduğu haqq ayrıca sütunda göstərilir və köçürülən məbləğə daxil deyil.

# Sənəd
Köçürmə üçün ərizə çıxarıla bilər; fayl yükləndiyi andakı məlumatı daşıyır.',
0, 0, 0, GETDATE(), 0);


/* ══ KÖHNƏ HTML MƏTNLƏRİ SADƏ MƏTNƏ ÇEVİR ══════════════════════════════════
   Yalnız əvvəllər HTML formatında yazılmış VƏ heç vaxt əl ilə redaktə
   edilməmiş qeydlərə toxunur. Sizin düzəlişiniz varsa (YenilenmeTarixi dolu)
   qeyd OLDUĞU KİMİ qalır.
   Təmiz bazada bu blok heç nəyə toxunmur — yuxarıdakı INSERT-lər onsuz da
   sadə mətn yazıb.
   ═══════════════════════════════════════════════════════════════════════ */

UPDATE SehifeYardimlari SET Metn = N'Sistemə girəndə ilk açılan səhifədir. Burada heç nə yazılmır — yalnız sizin cari vəziyyətinizi göstərir.

# Yuxarıdakı kartlar
- *Məzuniyyət qalığı* — əmək məzuniyyətindən neçə gününüz qalıb.
- *İcazə saatı — bu il* — bu il istifadə etdiyiniz saatlıq icazə.
- *Gecikmə — bu il* — bu il neçə dəfə gec gəlmisiniz.

# Aktiv müraciətlər
Göndərdiyiniz, amma hələ cavablanmamış müraciətlər. *Gözləmədə* yazısı müraciətin təsdiq gözlədiyini bildirir; kimdə qaldığını görmək üçün üstünə klikləyin.

# Bildirişlər
Sizə aid son hadisələr: müraciətiniz təsdiqlənəndə, imtina olunanda və ya sizdən təsdiq gözləyəndə burada görünür.

# Davamiyyət və Son ödənişlər
Günlərinizin yığımı (İşlədi, Gecikmə, İcazəli, Qayıb, Xəstəlik, Ezamiyyət) və sizə edilən son ödənişlər. Hər ikisinin altındakı keçid tam siyahını açır.

# Rəqəm düz gəlmirsə
Bu səhifədə heç nə düzəldilmir — göstəricilər davamiyyət və müraciət qeydlərindən hesablanır. Uyğunsuzluq görsəniz HR ilə əlaqə saxlayın.'
WHERE Acar = N'user/dashboard/index' AND YenilenmeTarixi IS NULL
  AND (Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%');

UPDATE SehifeYardimlari SET Metn = N'Göndərdiyiniz bütün məzuniyyət müraciətləri — köhnə və yeni.

# Status nə deməkdir
- *Gözləmədə* — müraciət göndərilib, hələ heç kim baxmayıb.
- *Şöbə rəisi / Rəhbər / HR təsdiqində* — məhz həmin şəxsdə gözləyir.
- *Təsdiqlənib* — bütün mərhələlər keçilib, məzuniyyət rəsmiləşib.
- *İmtina edildi* — səbəbi müraciətin içində yazılır.

# Müraciətin harada qaldığını görmək
Sətrin üstünə klikləyin — «Müraciət gedişatı» hansı addımların keçildiyini və indi kimdə olduğunu göstərir.

# Ləğv etmək
*Ləğv et* düyməsi ilə ləğv sorğusu göndərilir və səbəb yazılır. Təsdiqlənmiş məzuniyyəti ləğv etmək HR-ın təsdiqini tələb edir — balans avtomatik geri qayıdır.

# Yeni müraciət
*Müraciət göndər* düyməsi ilə. Eyni tarixlərə ikinci məzuniyyət yazmaq mümkün deyil — sistem üst-üstə düşməni bloklayır və hansı qeydlə toqquşduğunu yazır.'
WHERE Acar = N'user/mezuniyyet/index' AND YenilenmeTarixi IS NULL
  AND (Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%');

UPDATE SehifeYardimlari SET Metn = N'Tarixləri seçən kimi sistem gün sayını və təxmini ödənişi göstərir.

# Gün sayı necə sayılır — DİQQƏT
Əmək məzuniyyətində *təqvim günü* sayılır: şənbə və bazar da daxildir. Yalnız məzuniyyətdə hesablanmayan bayram günləri çıxılır.

Yəni 20–24 avqust seçsəniz balansdan *5 gün* düşür, 3 yox. Ekrandakı «iş günü» göstəricisi ayrı məlumatdır — balansdan düşən rəqəm təqvim günüdür.

# Məzuniyyət pulu
Aşağıdakı hesablama qutusu pulun necə çıxdığını addım-addım göstərir: son 12 ayın qazancı, artım əmsalı və cari maaş müqayisə olunur, işçinin xeyrinə *böyük olan* götürülür.

Rəqəm təxminidir — son məbləği mühasibat təsdiq edir.

# Əvəzedici
Əvəzedici seçsəniz müraciət əvvəlcə ona gedir; o qəbul edəndən sonra rəhbərə keçir. Seçməsəniz birbaşa rəhbərə gedir.

# Nə vaxt bloklanır
- Seçdiyiniz tarixlər mövcud məzuniyyətinizlə üst-üstə düşürsə (bir gün toxunsa belə).
- Balansınız çatmırsa.

Hər iki halda ekranda səbəb və toqquşan qeydin tarixləri yazılır.'
WHERE Acar = N'user/mezuniyyet/create' AND YenilenmeTarixi IS NULL
  AND (Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%');

UPDATE SehifeYardimlari SET Metn = N'Bir müraciətin tam mənzərəsi: tarixlər, gün sayı, ödəniş və təsdiq zənciri.

# Müraciət gedişatı
Addımlar sıra ilə göstərilir. Yaşıl addım keçilib, boz addım hələ gözləyir. İndi kimdə olduğu qalın yazılır.

Bəzi addımlar sizin rolunuza görə *ümumiyyətlə olmur* — məsələn özünüz rəhbərsinizsə rəhbər addımı keçilmiş sayılır. Bu, səhv deyil.

# İmtina olunubsa
Səbəb burada yazılır. Düzəliş edib yenidən göndərmək üçün yeni müraciət yaradın — imtina olunmuş müraciət redaktə edilmir.

# Ləğv
Təsdiqlənmiş məzuniyyəti ləğv etmək üçün səbəb yazılır və HR-a gedir. HR təsdiqləyəndən sonra balans geri qayıdır və mühasibata «ödənişi icra etməyin» bildirişi düşür.'
WHERE Acar = N'user/mezuniyyet/detail' AND YenilenmeTarixi IS NULL
  AND (Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%');

UPDATE SehifeYardimlari SET Metn = N'Bütün saatlıq icazə müraciətləriniz və illik balansınız.

# İstifadə edilib
Bu il sayğacınızdan düşən saat. İllik hədd *36 saatdır*.

# Sayğaca nə yazılır
Sayğaca düşən rəqəm faktiki pəncərədən *az ola bilər*, çünki güzəştlərin qarşılığı çıxılır:
- *Nahara çıxmıram* seçmisinizsə — nahar fasiləsi qədər çıxılır.
- *Jetonla ödəmisinizsə* — jetonla örtülən hissə çıxılır.

Ona görə sayğaca yazılan icazə heç vaxt *3 saatı keçmir*.

# Status
*Gözləmədə* — təsdiq gözləyir. *Təsdiqlənib* — icazə rəsmiləşib. *İmtina edildi* — səbəb müraciətin içindədir.'
WHERE Acar = N'user/icaze/index' AND YenilenmeTarixi IS NULL
  AND (Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%');

UPDATE SehifeYardimlari SET Metn = N'Saatlıq icazə üçün başlama və bitmə saatını seçirsiniz.

# Standart hədd — 3 saat
Adi icazə ən çox *3 saat* ola bilər. Onu uzadan iki güzəşt var və hər ikisinin qarşılığı sayğacdan çıxılır.

# 1. Nahara çıxmıram
İşarələsəniz pəncərə nahar fasiləsi qədər uzana bilər, əvəzində həmin müddət sayğacdan çıxılır. Yəni naharda işləyirsiniz, o vaxt sizə qaytarılır.

# 2. Artıq müddəti jetonumdan ödə
3 saatı aşan hissəni jeton balansınızdan ödəyir. *Miqdarı siz yazmırsınız* — sistem pəncərədən özü hesablayır; siz yalnız işarələyirsiniz.

Rəhbər təsdiq edərkən jetonu *artıra bilər*, amma məcburi həddin altına *sala bilməz*.

# Nümunə
13:00–17:45 (285 dəqiqə) + nahar + 1 saat jeton → sayğaca *180 dəqiqə* yazılır.

# Nə vaxt keçmir
Jeton və ya illik 36 saatlıq balans çatmırsa müraciət qəbul edilmir — ekranda səbəb yazılır.'
WHERE Acar = N'user/icaze/create' AND YenilenmeTarixi IS NULL
  AND (Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%');

UPDATE SehifeYardimlari SET Metn = N'Gün-gün giriş və çıxış qeydləriniz. Məlumat *barmaq izi cihazından* gəlir — bu səhifədə əl ilə düzəliş edilmir.

# Statuslar
- *İşdə* — vaxtında gəlib.
- *Gecikmə* — iş saatından gec giriş.
- *Saat İcazəsi* — təsdiqlənmiş icazə ilə çıxış.
- *Ezamiyyət* — ezamiyyət günü; gecikmə yazılmır.
- *Məzuniyyət günü* — təsdiqlənmiş məzuniyyət.
- *Erkən çıxış* — iş vaxtı bitmədən çıxış.
- *Qayıb* — cihazda qeyd yoxdur və icazə/məzuniyyət də yoxdur.

# Səhv görürsünüzsə
Status cihazın qeydindən hesablanır. Məsələn icazəniz sonradan təsdiqlənibsə köhnə gün *öz-özünə dəyişməyə bilər*. Belə halda HR-a müraciət edin — düzəlişi yalnız HR edir.

# Qayıb görünürəm, amma işdə idim
Çox vaxt səbəb cihaza vurulmamasıdır. Şöbə rəisiniz və HR bunu qeydlə düzəldə bilər.'
WHERE Acar = N'user/davamiyyet/index' AND YenilenmeTarixi IS NULL
  AND (Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%');

UPDATE SehifeYardimlari SET Metn = N'Məzuniyyət və ezamiyyət müraciətlərinizi bir yerdə göstərir — ayrı-ayrı səhifələrə keçməyə ehtiyac qalmır.

# Bölmələr
Yuxarıdakı seçimlə növü dəyişirsiniz. Hər bölmənin öz sayğacı var (Cəmi, Gözləmədə).

# Nə edə bilərsiniz
- *Müraciət göndər* — yeni müraciət yaradır.
- *Ləğv et* — səbəb yazıb ləğv sorğusu göndərir.
- Sətrə klikləməklə gedişatı açırsınız.

# Qeyd
Bu ekran məzuniyyət və icazə səhifələri ilə *eyni məlumatı* göstərir — sadəcə birləşdirilmiş görünüşdür. Hansından baxmağınızın fərqi yoxdur.'
WHERE Acar = N'user/muraciet/index' AND YenilenmeTarixi IS NULL
  AND (Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%');

UPDATE SehifeYardimlari SET Metn = N'Şəxsi məlumatlarınız və əlaqə vasitələriniz.

# Nəyi özünüz dəyişə bilərsiniz
Telefon və əlaqə məlumatları. Ad, vəzifə, şöbə və maaş *dəyişdirilmir* — onları HR idarə edir.

# Mail ayarları
Bildirişlərin göndərildiyi ünvanı burada təyin edirsiniz. *Mail sına* düyməsi ayarların düzgünlüyünü yoxlayır — sınaq mesajı gəlmirsə ünvanı və parolu yenidən yoxlayın.

# Məlumat səhvdirsə
Vəzifə və ya şöbə səhv görünürsə HR ilə əlaqə saxlayın — düzəliş kadr qeydindən gəlir.'
WHERE Acar = N'user/profile/index' AND YenilenmeTarixi IS NULL
  AND (Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%');


/* ══ NƏTİCƏ ══════════════════════════════════════════════════════════════ */
PRINT N'--- Netice ---';

SELECT Modul, COUNT(*) AS Sehife
FROM   SehifeYardimlari WHERE Silinib = 0
GROUP  BY Modul ORDER BY Modul;

SELECT COUNT(*)                                                   AS CemiTelimat,
       SUM(CASE WHEN Metn LIKE N'%<p>%' OR Metn LIKE N'%<h3>%'
                THEN 1 ELSE 0 END)                                AS HeleHtml,
       SUM(CASE WHEN YenilenmeTarixi IS NOT NULL THEN 1 ELSE 0 END) AS SizinDuzeltdiyiniz
FROM   SehifeYardimlari WHERE Silinib = 0;
/*  CemiTelimat 58 olmalidir.  HeleHtml 0 olmalidir.
    SizinDuzeltdiyiniz — el ile redakte etdiyiniz qeydlerin sayi (toxunulmayib). */
