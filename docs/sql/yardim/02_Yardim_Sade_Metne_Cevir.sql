/* ============================================================================
   PAKET 1-in 9 MƏTNİNİ SADƏ MƏTNƏ ÇEVİRİR (31.08.2026)
   ----------------------------------------------------------------------------
   NİYƏ: 01-ci paketdə mətnlər HTML kimi yazılmışdı. İstifadəçi qərarı:
   «HTML olmasın, sadəcə mətn kimi yazım». Sistem artıq sadə mətni özü
   formatlayır (`YardimMetn.Formatla`), ona görə saxlanan mətn də sadə olmalıdır —
   yoxsa redaktəyə girəndə HTML görünəcək.

   GÖRÜNÜŞ DƏYİŞMİR — panel eyni cür göstərəcək. Dəyişən yalnız redaktorda
   nə gördüyünüzdür.

   ⚠️ BU SKRİPT MƏTNİ ÜSTƏLƏYİR (UPDATE). Əgər 9 mətndən hər hansı birini
   ARTIQ ƏL İLƏ DÜZƏLTMİSİNİZSƏ, həmin sətri bu skriptdən silin — yoxsa
   düzəlişiniz itər.

   Nə dəyişəcəyini ƏVVƏLCƏ görmək üçün aşağıdakı SELECT-i işlədin.
   ============================================================================ */

SET NOCOUNT ON;

/* ── ƏVVƏLCƏ BAX: hansı sətirlər dəyişəcək və indi nə var ───────────────── */
SELECT Acar, Basliq,
       LEN(Metn)                                   AS UzunluqIndi,
       CASE WHEN Metn LIKE '%<p>%' OR Metn LIKE '%<h3>%'
            THEN N'HTML — çevriləcək' ELSE N'onsuz da sadə mətndir' END AS Veziyyet,
       YenilenmeTarixi                             AS SonDuzelis
FROM   SehifeYardimlari
WHERE  Silinib = 0
ORDER  BY Acar;
/*  `SonDuzelis` DOLUDURSA həmin qeydi siz redaktə etmisiniz —
    onun UPDATE-ini aşağıdan silin.                                          */


/* ── 1) Əsas səhifə ─────────────────────────────────────────────────────── */
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
WHERE Acar = N'user/dashboard/index';

/* ── 2) Məzuniyyətlərim ─────────────────────────────────────────────────── */
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
WHERE Acar = N'user/mezuniyyet/index';

/* ── 3) Yeni məzuniyyət müraciəti ───────────────────────────────────────── */
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
WHERE Acar = N'user/mezuniyyet/create';

/* ── 4) Məzuniyyət detalı ───────────────────────────────────────────────── */
UPDATE SehifeYardimlari SET Metn = N'Bir müraciətin tam mənzərəsi: tarixlər, gün sayı, ödəniş və təsdiq zənciri.

# Müraciət gedişatı
Addımlar sıra ilə göstərilir. Yaşıl addım keçilib, boz addım hələ gözləyir. İndi kimdə olduğu qalın yazılır.

Bəzi addımlar sizin rolunuza görə *ümumiyyətlə olmur* — məsələn özünüz rəhbərsinizsə rəhbər addımı keçilmiş sayılır. Bu, səhv deyil.

# İmtina olunubsa
Səbəb burada yazılır. Düzəliş edib yenidən göndərmək üçün yeni müraciət yaradın — imtina olunmuş müraciət redaktə edilmir.

# Ləğv
Təsdiqlənmiş məzuniyyəti ləğv etmək üçün səbəb yazılır və HR-a gedir. HR təsdiqləyəndən sonra balans geri qayıdır və mühasibata «ödənişi icra etməyin» bildirişi düşür.'
WHERE Acar = N'user/mezuniyyet/detail';

/* ── 5) İcazələrim ──────────────────────────────────────────────────────── */
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
WHERE Acar = N'user/icaze/index';

/* ── 6) Yeni icazə müraciəti ────────────────────────────────────────────── */
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
WHERE Acar = N'user/icaze/create';

/* ── 7) Davamiyyətim ────────────────────────────────────────────────────── */
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
WHERE Acar = N'user/davamiyyet/index';

/* ── 8) Müraciətlərim ───────────────────────────────────────────────────── */
UPDATE SehifeYardimlari SET Metn = N'Məzuniyyət və ezamiyyət müraciətlərinizi bir yerdə göstərir — ayrı-ayrı səhifələrə keçməyə ehtiyac qalmır.

# Bölmələr
Yuxarıdakı seçimlə növü dəyişirsiniz. Hər bölmənin öz sayğacı var (Cəmi, Gözləmədə).

# Nə edə bilərsiniz
- *Müraciət göndər* — yeni müraciət yaradır.
- *Ləğv et* — səbəb yazıb ləğv sorğusu göndərir.
- Sətrə klikləməklə gedişatı açırsınız.

# Qeyd
Bu ekran məzuniyyət və icazə səhifələri ilə *eyni məlumatı* göstərir — sadəcə birləşdirilmiş görünüşdür. Hansından baxmağınızın fərqi yoxdur.'
WHERE Acar = N'user/muraciet/index';

/* ── 9) Profil ──────────────────────────────────────────────────────────── */
UPDATE SehifeYardimlari SET Metn = N'Şəxsi məlumatlarınız və əlaqə vasitələriniz.

# Nəyi özünüz dəyişə bilərsiniz
Telefon və əlaqə məlumatları. Ad, vəzifə, şöbə və maaş *dəyişdirilmir* — onları HR idarə edir.

# Mail ayarları
Bildirişlərin göndərildiyi ünvanı burada təyin edirsiniz. *Mail sına* düyməsi ayarların düzgünlüyünü yoxlayır — sınaq mesajı gəlmirsə ünvanı və parolu yenidən yoxlayın.

# Məlumat səhvdirsə
Vəzifə və ya şöbə səhv görünürsə HR ilə əlaqə saxlayın — düzəliş kadr qeydindən gəlir.'
WHERE Acar = N'user/profile/index';


/* ── NƏTİCƏ: hamısı sadə mətnə keçdimi? ─────────────────────────────────── */
SELECT Acar, Basliq,
       CASE WHEN Metn LIKE '%<p>%' OR Metn LIKE '%<h3>%'
            THEN N'HƏLƏ HTML' ELSE N'sadə mətn ✓' END AS Veziyyet
FROM   SehifeYardimlari
WHERE  Silinib = 0
ORDER  BY Acar;
