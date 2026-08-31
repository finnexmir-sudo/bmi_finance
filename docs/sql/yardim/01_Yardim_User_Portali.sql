/* ============================================================================
   SƏHİFƏ TƏLİMATLARI — PAKET 1: İŞÇİ PORTALI (User area)
   ----------------------------------------------------------------------------
   Mətnləri Claude yazıb, açarlar koddan (marşrut cədvəlindən) götürülüb.
   Admin bunları `/Admin/Yardim` ekranında oxuyub düzəldə bilər — bazadadır,
   deploy tələb olunmur.

   NECƏ İŞLƏDİLİR: SSMS-də FinNex_Maliyye_Db üzərində bir dəfə işlət.
   TƏKRAR İŞLƏTMƏK TƏHLÜKƏSİZDİR — hər blok `IF NOT EXISTS` ilə qorunur,
   yəni artıq yazılmış (və sizin redaktə etdiyiniz) mətn ÜSTƏLƏNMİR.

   ⚠️ Mətni dəyişmək istəyirsinizsə bu faylı yox, `/Admin/Yardim` ekranını
   işlədin — fayl yalnız ilk yükləmə üçündür.

   Azərbaycan hərfləri üçün hər sətirdə N'…' prefiksi MƏCBURİDİR.
   ============================================================================ */

SET NOCOUNT ON;

/* ── 1) Əsas səhifə — işçi paneli ───────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/dashboard/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/dashboard/index', N'esas-sehife', N'Əsas səhifə (işçi paneli)', N'Ümumi',
N'Məzuniyyət qalığı, icazə saatı, müraciətlər və bildirişlər — hamısı bir ekranda.',
N'<p>Sistemə girəndə ilk açılan səhifədir. Burada heç nə yazılmır — yalnız sizin cari vəziyyətinizi göstərir.</p>
<h3>Yuxarıdakı kartlar</h3>
<ul>
<li><b>Məzuniyyət qalığı</b> — əmək məzuniyyətindən neçə gününüz qalıb.</li>
<li><b>İcazə saatı — bu il</b> — bu il istifadə etdiyiniz saatlıq icazə.</li>
<li><b>Gecikmə — bu il</b> — bu il neçə dəfə gec gəlmisiniz.</li>
</ul>
<h3>Aktiv müraciətlər</h3>
<p>Göndərdiyiniz, amma hələ cavablanmamış müraciətlər. <b>Gözləmədə</b> yazısı müraciətin təsdiq gözlədiyini bildirir; kimdə qaldığını görmək üçün üstünə klikləyin.</p>
<h3>Bildirişlər</h3>
<p>Sizə aid son hadisələr: müraciətiniz təsdiqlənəndə, imtina olunanda və ya sizdən təsdiq gözləyəndə burada görünür.</p>
<h3>Davamiyyət və Son ödənişlər</h3>
<p>Günlərinizin yığımı (İşlədi, Gecikmə, İcazəli, Qayıb, Xəstəlik, Ezamiyyət) və sizə edilən son ödənişlər. Hər ikisinin altındakı keçid tam siyahını açır.</p>
<h3>Rəqəm düz gəlmirsə</h3>
<p>Bu səhifədə heç nə düzəldilmir — göstəricilər davamiyyət və müraciət qeydlərindən hesablanır. Uyğunsuzluq görsəniz HR ilə əlaqə saxlayın.</p>',
0, 0, 0, GETDATE(), 0);

/* ── 2) Məzuniyyətlərim ─────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/mezuniyyet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/mezuniyyet/index', N'mezuniyyetlerim', N'Məzuniyyətlərim', N'Məzuniyyət',
N'Bütün məzuniyyət müraciətləriniz və onların hansı mərhələdə olduğu.',
N'<p>Göndərdiyiniz bütün məzuniyyət müraciətləri — köhnə və yeni.</p>
<h3>Status nə deməkdir</h3>
<ul>
<li><b>Gözləmədə</b> — müraciət göndərilib, hələ heç kim baxmayıb.</li>
<li><b>Şöbə rəisi / Rəhbər / HR təsdiqində</b> — məhz həmin şəxsdə gözləyir.</li>
<li><b>Təsdiqlənib</b> — bütün mərhələlər keçilib, məzuniyyət rəsmiləşib.</li>
<li><b>İmtina edildi</b> — səbəbi müraciətin içində yazılır.</li>
</ul>
<h3>Müraciətin harada qaldığını görmək</h3>
<p>Sətrin üstünə klikləyin — «Müraciət gedişatı» hansı addımların keçildiyini və indi kimdə olduğunu göstərir.</p>
<h3>Ləğv etmək</h3>
<p><b>Ləğv et</b> düyməsi ilə ləğv sorğusu göndərilir və səbəb yazılır. Təsdiqlənmiş məzuniyyəti ləğv etmək HR-ın təsdiqini tələb edir — balans avtomatik geri qayıdır.</p>
<h3>Yeni müraciət</h3>
<p><b>Müraciət göndər</b> düyməsi ilə. Eyni tarixlərə ikinci məzuniyyət yazmaq mümkün deyil — sistem üst-üstə düşməni bloklayır və hansı qeydlə toqquşduğunu yazır.</p>',
0, 0, 0, GETDATE(), 0);

/* ── 3) Yeni məzuniyyət müraciəti ───────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/mezuniyyet/create')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/mezuniyyet/create', N'yeni-mezuniyyet-muracieti', N'Yeni məzuniyyət müraciəti', N'Məzuniyyət',
N'Tarix seçirsiniz, sistem gün sayını və ödənişi hesablayır.',
N'<p>Tarixləri seçən kimi sistem gün sayını və təxmini ödənişi göstərir.</p>
<h3>Gün sayı necə sayılır — DİQQƏT</h3>
<p>Əmək məzuniyyətində <b>təqvim günü</b> sayılır: şənbə və bazar da daxildir. Yalnız məzuniyyətdə hesablanmayan bayram günləri çıxılır.</p>
<p>Yəni 20–24 avqust seçsəniz balansdan <b>5 gün</b> düşür, 3 yox. Ekrandakı «iş günü» göstəricisi ayrı məlumatdır — balansdan düşən rəqəm təqvim günüdür.</p>
<h3>Məzuniyyət pulu</h3>
<p>Aşağıdakı hesablama qutusu pulun necə çıxdığını addım-addım göstərir: son 12 ayın qazancı, artım əmsalı və cari maaş müqayisə olunur, işçinin xeyrinə <b>böyük olan</b> götürülür.</p>
<p>Rəqəm təxminidir — son məbləği mühasibat təsdiq edir.</p>
<h3>Əvəzedici</h3>
<p>Əvəzedici seçsəniz müraciət əvvəlcə ona gedir; o qəbul edəndən sonra rəhbərə keçir. Seçməsəniz birbaşa rəhbərə gedir.</p>
<h3>Nə vaxt bloklanır</h3>
<ul>
<li>Seçdiyiniz tarixlər mövcud məzuniyyətinizlə üst-üstə düşürsə (bir gün toxunsa belə).</li>
<li>Balansınız çatmırsa.</li>
</ul>
<p>Hər iki halda ekranda səbəb və toqquşan qeydin tarixləri yazılır.</p>',
0, 0, 0, GETDATE(), 0);

/* ── 4) Məzuniyyət — müraciətin detalı ──────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/mezuniyyet/detail')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/mezuniyyet/detail', N'mezuniyyet-detali', N'Məzuniyyət müraciətinin detalı', N'Məzuniyyət',
N'Müraciətin gedişatı — hansı addım keçilib, indi kimdədir.',
N'<p>Bir müraciətin tam mənzərəsi: tarixlər, gün sayı, ödəniş və təsdiq zənciri.</p>
<h3>Müraciət gedişatı</h3>
<p>Addımlar sıra ilə göstərilir. Yaşıl addım keçilib, boz addım hələ gözləyir. İndi kimdə olduğu qalın yazılır.</p>
<p>Bəzi addımlar sizin rolunuza görə <b>ümumiyyətlə olmur</b> — məsələn özünüz rəhbərsinizsə rəhbər addımı keçilmiş sayılır. Bu, səhv deyil.</p>
<h3>İmtina olunubsa</h3>
<p>Səbəb burada yazılır. Düzəliş edib yenidən göndərmək üçün yeni müraciət yaradın — imtina olunmuş müraciət redaktə edilmir.</p>
<h3>Ləğv</h3>
<p>Təsdiqlənmiş məzuniyyəti ləğv etmək üçün səbəb yazılır və HR-a gedir. HR təsdiqləyəndən sonra balans geri qayıdır və mühasibata «ödənişi icra etməyin» bildirişi düşür.</p>',
0, 0, 0, GETDATE(), 0);

/* ── 5) İcazələrim ──────────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/icaze/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/icaze/index', N'icazelerim', N'İcazələrim', N'İcazə',
N'Saatlıq icazə müraciətləriniz və illik sayğacınız.',
N'<p>Bütün saatlıq icazə müraciətləriniz və illik balansınız.</p>
<h3>İstifadə edilib</h3>
<p>Bu il sayğacınızdan düşən saat. İllik hədd <b>36 saatdır</b>.</p>
<h3>Sayğaca nə yazılır</h3>
<p>Sayğaca düşən rəqəm həmişə <b>faktiki pəncərədən az ola bilər</b>, çünki güzəştlərin qarşılığı çıxılır:</p>
<ul>
<li><b>Nahara çıxmıram</b> seçmisinizsə — nahar fasiləsi qədər çıxılır.</li>
<li><b>Jetonla ödəmisinizsə</b> — jetonla örtülən hissə çıxılır.</li>
</ul>
<p>Ona görə sayğaca yazılan icazə heç vaxt <b>3 saatı keçmir</b>.</p>
<h3>Status</h3>
<p><b>Gözləmədə</b> — təsdiq gözləyir. <b>Təsdiqlənib</b> — icazə rəsmiləşib. <b>İmtina edildi</b> — səbəb müraciətin içindədir.</p>',
0, 0, 0, GETDATE(), 0);

/* ── 6) Yeni icazə müraciəti ────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/icaze/create')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/icaze/create', N'yeni-icaze-muracieti', N'Yeni icazə müraciəti', N'İcazə',
N'Saat seçirsiniz; nahar və jeton güzəştləri pəncərəni uzadır.',
N'<p>Saatlıq icazə üçün başlama və bitmə saatını seçirsiniz.</p>
<h3>Standart hədd — 3 saat</h3>
<p>Adi icazə ən çox <b>3 saat</b> ola bilər. Onu uzadan iki güzəşt var və <b>hər ikisinin qarşılığı sayğacdan çıxılır</b>:</p>
<h3>1. Nahara çıxmıram</h3>
<p>İşarələsəniz pəncərə nahar fasiləsi qədər uzana bilər, əvəzində həmin müddət sayğacdan çıxılır. Yəni naharda işləyirsiniz, o vaxt sizə qaytarılır.</p>
<h3>2. Artıq müddəti jetonumdan ödə</h3>
<p>3 saatı aşan hissəni jeton balansınızdan ödəyir. <b>Miqdarı siz yazmırsınız</b> — sistem pəncərədən özü hesablayır; siz yalnız işarələyirsiniz.</p>
<p>Rəhbər təsdiq edərkən jetonu <b>artıra bilər</b>, amma məcburi həddin altına <b>sala bilməz</b>.</p>
<h3>Nümunə</h3>
<p>13:00–17:45 (285 dəqiqə) + nahar + 1 saat jeton → sayğaca <b>180 dəqiqə</b> yazılır.</p>
<h3>Nə vaxt keçmir</h3>
<p>Jeton və ya illik 36 saatlıq balans çatmırsa müraciət qəbul edilmir — ekranda səbəb yazılır.</p>',
0, 0, 0, GETDATE(), 0);

/* ── 7) Davamiyyətim ────────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/davamiyyet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/davamiyyet/index', N'davamiyyetim', N'Davamiyyətim', N'Davamiyyət',
N'Gün-gün giriş/çıxış qeydləriniz və statusları.',
N'<p>Gün-gün giriş və çıxış qeydləriniz. Məlumat <b>barmaq izi cihazından</b> gəlir — bu səhifədə əl ilə düzəliş edilmir.</p>
<h3>Statuslar</h3>
<ul>
<li><b>İşdə</b> — vaxtında gəlib.</li>
<li><b>Gecikmə</b> — iş saatından gec giriş.</li>
<li><b>Saat İcazəsi</b> — təsdiqlənmiş icazə ilə çıxış.</li>
<li><b>Ezamiyyət</b> — ezamiyyət günü; gecikmə yazılmır.</li>
<li><b>Məzuniyyət günü</b> — təsdiqlənmiş məzuniyyət.</li>
<li><b>Erkən çıxış</b> — iş vaxtı bitmədən çıxış.</li>
<li><b>Qayıb</b> — cihazda qeyd yoxdur və icazə/məzuniyyət də yoxdur.</li>
</ul>
<h3>Səhv görürsünüzsə</h3>
<p>Status cihazın qeydindən hesablanır. Məsələn icazəniz sonradan təsdiqlənibsə köhnə gün <b>öz-özünə dəyişməyə bilər</b>. Belə halda HR-a müraciət edin — düzəlişi yalnız HR edir.</p>
<h3>Qayıb görünürəm, amma işdə idim</h3>
<p>Çox vaxt səbəb cihaza vurulmamasıdır. Şöbə rəisiniz və HR bunu qeydlə düzəldə bilər.</p>',
0, 0, 0, GETDATE(), 0);

/* ── 8) Müraciətlərim (birləşmiş portal) ────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/muraciet/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/muraciet/index', N'muracietlerim', N'Müraciətlərim', N'Ümumi',
N'Məzuniyyət və ezamiyyət müraciətləriniz bir ekranda.',
N'<p>Məzuniyyət və ezamiyyət müraciətlərinizi bir yerdə göstərir — ayrı-ayrı səhifələrə keçməyə ehtiyac qalmır.</p>
<h3>Bölmələr</h3>
<p>Yuxarıdakı seçimlə növü dəyişirsiniz. Hər bölmənin öz sayğacı var (Cəmi, Gözləmədə).</p>
<h3>Nə edə bilərsiniz</h3>
<ul>
<li><b>Müraciət göndər</b> — yeni müraciət yaradır.</li>
<li><b>Ləğv et</b> — səbəb yazıb ləğv sorğusu göndərir.</li>
<li>Sətrə klikləməklə gedişatı açırsınız.</li>
</ul>
<h3>Qeyd</h3>
<p>Bu ekran məzuniyyət və icazə səhifələri ilə <b>eyni məlumatı</b> göstərir — sadəcə birləşdirilmiş görünüşdür. Hansından baxmağınızın fərqi yoxdur.</p>',
0, 0, 0, GETDATE(), 0);

/* ── 9) Profil ──────────────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'user/profile/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'user/profile/index', N'profil', N'Profil', N'Ümumi',
N'Şəxsi məlumatlarınız, əlaqə və mail ayarları.',
N'<p>Şəxsi məlumatlarınız və əlaqə vasitələriniz.</p>
<h3>Nəyi özünüz dəyişə bilərsiniz</h3>
<p>Telefon və əlaqə məlumatları. Ad, vəzifə, şöbə və maaş <b>dəyişdirilmir</b> — onları HR idarə edir.</p>
<h3>Mail ayarları</h3>
<p>Bildirişlərin göndərildiyi ünvanı burada təyin edirsiniz. <b>Mail sına</b> düyməsi ayarların düzgünlüyünü yoxlayır — sınaq mesajı gəlmirsə ünvanı və parolu yenidən yoxlayın.</p>
<h3>Məlumat səhvdirsə</h3>
<p>Vəzifə və ya şöbə səhv görünürsə HR ilə əlaqə saxlayın — düzəliş kadr qeydindən gəlir.</p>',
0, 0, 0, GETDATE(), 0);

/* ============================================================================
   NƏTİCƏ — nə yazıldı, nə hazırdır
   ============================================================================ */
SELECT Modul, Basliq, Acar, Slug,
       CASE WHEN Hazirlanir = 1 THEN N'hazırlanır' ELSE N'hazır' END AS Veziyyet
FROM   SehifeYardimlari
WHERE  Silinib = 0
ORDER  BY Modul, Basliq;
