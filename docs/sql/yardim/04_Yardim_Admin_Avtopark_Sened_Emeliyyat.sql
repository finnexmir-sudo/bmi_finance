/* ============================================================================
   SƏHİFƏ TƏLİMATLARI — PAKET 4
   Admin · Avtopark · Sənəd dövriyyəsi · Əməliyyat  (27 səhifə)
   ----------------------------------------------------------------------------
   Mətnlər səhifələrin REAL etiketlərindən və sistemin qaydalarından (CLAUDE.md)
   yazılıb. Əmin olmadığım yerdə ümumi ifadə işlədilib — səhv gördüyünüzü
   `/Admin/Yardim`-də düzəldin.

   FORMAT: sadə mətn.  # Başlıq · - siyahı · 1. nömrəli · *qalın*

   SSMS-də FinNex_Maliyye_Db üzərində bir dəfə işlədin.
   TƏKRAR İŞLƏTMƏK TƏHLÜKƏSİZDİR — `IF NOT EXISTS` sizin düzəlişinizi qoruyur.
   ============================================================================ */

SET NOCOUNT ON;

/* ══ ADMIN ═══════════════════════════════════════════════════════════════ */

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

/* ══ AVTOPARK ════════════════════════════════════════════════════════════ */

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

/* ══ SƏNƏD DÖVRİYYƏSİ ════════════════════════════════════════════════════ */

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

/* ══ ƏMƏLİYYAT ═══════════════════════════════════════════════════════════ */

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

/* ══ NƏTİCƏ ══════════════════════════════════════════════════════════════ */
SELECT Modul, Basliq, Acar
FROM   SehifeYardimlari
WHERE  Silinib = 0
   AND (Acar LIKE N'admin/%' OR Acar LIKE N'avtopark/%'
     OR Acar LIKE N'seneddovriyyesi/%' OR Acar LIKE N'emeliyyat/%')
ORDER  BY Modul, Basliq;

SELECT COUNT(*) AS CemiTelimat FROM SehifeYardimlari WHERE Silinib = 0;
