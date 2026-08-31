/* ============================================================================
   SƏHİFƏ TƏLİMATLARI — PAKET 3: HR SAHƏSİ (22 səhifə)
   ----------------------------------------------------------------------------
   İstifadəçi: «HR-dan ən çox gəlir, jeton və s.»

   Mətnlər səhifələrin REAL etiketlərindən və sistemin öz qaydalarından
   yazılıb (CLAUDE.md). Uydurma yoxdur; əmin olmadığım yerdə ümumi ifadə
   işlədilib — səhv gördüyünüzü `/Admin/Yardim`-də düzəldin.

   FORMAT: sadə mətn (HTML yox). İşarələr:
       # Başlıq   ·   - siyahı   ·   1. nömrəli siyahı   ·   *qalın*

   NECƏ İŞLƏDİLİR: SSMS-də FinNex_Maliyye_Db üzərində bir dəfə.
   TƏKRAR İŞLƏTMƏK TƏHLÜKƏSİZDİR — `IF NOT EXISTS` ilə qorunur, yəni sizin
   redaktə etdiyiniz mətn ÜSTƏLƏNMİR.

   ⚠️ Bu skript işləməzdən əvvəl 20260827100000_SehifeYardimiCedveli
   migration-ı tətbiq olunmuş olmalıdır (tətbiq startup-da avtomatik keçir).
   ============================================================================ */

SET NOCOUNT ON;

/* ── JETON MODULU ───────────────────────────────────────────────────────── */

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

/* ── MƏZUNİYYƏT / DAVAMİYYƏT ────────────────────────────────────────────── */

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

/* ── KADR ───────────────────────────────────────────────────────────────── */

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

/* ── SƏNƏD / MÜQAVİLƏ ───────────────────────────────────────────────────── */

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

/* ── MAAŞA TƏSİR EDƏN PARAMETRLƏR ──────────────────────────────────────── */

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

/* ── DİGƏR ──────────────────────────────────────────────────────────────── */

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

/* ── NƏTİCƏ ─────────────────────────────────────────────────────────────── */
SELECT Modul, Basliq, Acar, Slug
FROM   SehifeYardimlari
WHERE  Silinib = 0 AND Acar LIKE N'hr/%'
ORDER  BY Modul, Basliq;

SELECT COUNT(*) AS CemiTelimat FROM SehifeYardimlari WHERE Silinib = 0;
