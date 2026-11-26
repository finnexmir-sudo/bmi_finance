/* ============================================================================
   SƏHİFƏ TƏLİMATLARI — PAKET 5: AVANS + MƏZUNİYYƏT ÖDƏNİŞLƏRİ (3 səhifə)
   ----------------------------------------------------------------------------
   26.11.2026 — "Qabaqcadan Məzuniyyət → Avtomatik Əvəzləşmə Avansı"
   funksiyası ilə birlikdə yazılıb (bax CLAUDE.md, eyni tarixli bölmə).

   Mətnlər səhifələrin REAL davranışından (AvansController, MezuniyyetOdenis
   Controller, MezuniyyetAvansService) yazılıb. Uydurma yoxdur.

   FORMAT: sadə mətn (HTML yox). İşarələr:
       # Başlıq   ·   - siyahı   ·   1. nömrəli siyahı   ·   *qalın*

   NECƏ İŞLƏDİLİR: SSMS-də FinNex_Maliyye_Db üzərində bir dəfə.
   TƏKRAR İŞLƏTMƏK TƏHLÜKƏSİZDİR — `IF NOT EXISTS` ilə qorunur, yəni admin
   redaktoru ilə sonradan dəyişdirdiyiniz mətn ÜSTƏLƏNMİR.

   ⚠️ Bu skript işləməzdən əvvəl 20260827100000_SehifeYardimiCedveli
   migration-ı tətbiq olunmuş olmalıdır (tətbiq startup-da avtomatik keçir).
   ============================================================================ */

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.SehifeYardimlari', N'U') IS NULL
BEGIN
    RAISERROR(N'SehifeYardimlari cedveli tapilmadi. Once yeni build-i bir defe ise salin (migration avtomatik tetbiq olunur), sonra bu skripti yeniden isledin.', 16, 1);
    RETURN;
END;

/* ── AVANS ──────────────────────────────────────────────────────────────── */

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/avans/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/avans/index', N'avans-muracietleri', N'Avans Müraciətləri', N'Əmək haqqı',
N'İşçilərin aylıq avans müraciətləri — təsdiq, rədd və əməliyyatın yazılışı.',
N'İşçilərin aylıq avans müraciətlərinin siyahısı.

# Dövr
Yuxarıdakı İl/Ay seçimi göstərilən siyahını dəyişir. *Ay = Hamısı* seçsəniz, həmin ilin bütün ayları bir siyahıda görünür. Sayğaclar (Gözləmədə, Təsdiqlənib və s.) seçilmiş dövrə aiddir, status filtrindən asılı olmayaraq sabit qalır.

# Status
- *Gözləmədə* — işçi müraciət edib, hələ baxılmayıb.
- *Təsdiqlənib* — təsdiqlənib; məbləğ gələn ayın maaş hesablamasında avtomatik tutulma kimi çıxılır.
- *Rədd edildi* — səbəbi SƏBƏB sütununda yazılır, işçiyə bildirişdə göstərilir.

# Təsdiq / Rədd
Düymələr yalnız *Gözləmədə* statuslu sətirdə görünür. Rədd edəndə səbəb yazmaq tövsiyə olunur.

# Əməliyyatın yazılışı (.xls)
Seçilmiş ay üzrə *təsdiqlənmiş* avansları bank idxalı formatında Excelə çıxarır. Kredit sütunu işçinin bank hesabıdır. İxrac avans statusunu *dəyişmir* — istədiyiniz qədər təkrar çıxara bilərsiniz.

Debet sütunu iki fərqli mühasibat hesabından gəlir:
- Adi (işçinin öz tələb etdiyi) avanslar — *Avans Debet* hesabı.
- Qabaqcadan ödənilmiş məzuniyyətin əvəzləşmə sətirləri (aşağı bax) — *Məzuniyyət Əvəzləşməsi Debet* hesabı.

Hər ikisi Mühasibat Hesabları səhifəsindən təyin olunur — ay üçün uyğun hesab təyin edilməyibsə, ixrac həmin qrup üçün dayanıb xəbərdarlıq yazır.

# "Əvəzləşmə" sətirləri — işçi müraciət etməyib, sistem özü yaradıb
Səbəb sütununda "qabaqcadan ödənilmiş məzuniyyətin ... əvəzləşməsi" yazılan sətirlər işçinin öz müraciəti DEYİL. Bunlar, bir neçə aya düşən qabaqcadan-ödənilmiş məzuniyyətin növbəti ay(lar)ına aid payını avtomatik yaradan sistemdir — ikiqat ödəniş baş verməsin deyə. Adi avans kimi görünür, eyni qaydada təsdiqlənib maaşdan tutulur. Ətraflı izah: *Məzuniyyət Ödənişləri* → həmin işçinin müraciətinin detalı.',
0, 0, 0, GETDATE(), 0);

/* ── MƏZUNİYYƏT ÖDƏNİŞLƏRİ (Mühasib) ──────────────────────────────────── */

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/mezuniyyetodenis/index')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/mezuniyyetodenis/index', N'mezuniyyet-odenisleri', N'Məzuniyyət Ödənişləri', N'Məzuniyyət',
N'Qabaqcadan ödəniş seçilmiş məzuniyyət müraciətləri — HR təsdiqindən sonra buraya düşür.',
N'Qabaqcadan ödəniş seçilmiş məzuniyyət müraciətlərinin siyahısı.

# Yalnız qabaqcadan ödəniş
Bu siyahı *yalnız* işçinin məzuniyyət müraciətində "qabaqcadan ödəniş" seçdiyi qeydləri göstərir. Adi — maaş günündə ödənilən — məzuniyyət bura düşmür.

# Filtr
- *Gözləyir* — HR təsdiqləyib, hələ hesablama/ödəniş aparılmayıb (defolt).
- *Planlı* — məbləğ hesablanıb, ödəniş tarixi planlaşdırılıb.
- *Ödənilib* — artıq icra olunub.
- *Hamısı* — bütün dövrlər.

# Nə etməlisiniz
Sətrə klikləyib detala keçin. Orada hesablamanın addım-addım necə çıxdığını görəcəksiniz, lazım gələrsə məbləği yoxlayıb *Planla* və sonra *İcra et* edəcəksiniz.',
0, 0, 0, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM SehifeYardimlari WHERE Acar = N'hr/mezuniyyetodenis/detail')
INSERT INTO SehifeYardimlari (Acar, Slug, Basliq, Modul, Xulase, Metn, Hazirlanir, YalnizAdmin, BaxisSayi, YaradilmaTarixi, Silinib)
VALUES (N'hr/mezuniyyetodenis/detail', N'mezuniyyet-odenisi-detali', N'Məzuniyyət Ödənişi Detalı', N'Məzuniyyət',
N'Bir qabaqcadan-ödəniş məzuniyyətinin tam hesablanması — rəqəm necə çıxıb, ödəniş necə icra olunur.',
N'Bir müraciətin tam hesablama mənzərəsi.

# Hesablama necə aparılır
İki üsul paralel hesablanır, işçinin xeyrinə *böyük olan* seçilir (cədvəldə QALİB yazısı ilə işarələnir):
- *Üsul A* — son 12 ayın qazancının gündəlik tarixi ortası × iş günü.
- *Üsul B* — cari maaşın gündəlik dərəcəsi × iş günü.

# Ay-ay bölünmə
Məzuniyyət bir neçə təqvim ayına düşürsə, aşağıdakı cədvəl pulu AYLARA bölür — hər ay öz vergi bazasını daşıyır. Sonuncu sütun, *Əvəzləşmə (NET)*, mühasibin aylıq cədvəlinin Avans sütununa yazılan rəqəmdir.

# "Əvəzləşmə avansı" nə üçündür — DİQQƏT
Pulun HAMISI, ödənişin verildiyi ayda, bir dəfəyə işçiyə çatır. Amma vergi/DSMF HƏR AYA görə ayrıca hesablanır. Ona görə sonrakı ay(lar)ın maaşı hesablanarkən, həmin ayın payı qədər məbləğ işçinin netindən *bir daha çıxılmalıdır* — əks halda eyni pul iki dəfə ödənilmiş olur.

Bunu əl ilə izləməyə ehtiyac yoxdur. *Planla* basılan kimi sistem özü, sonrakı hər ay üçün Avans Müraciətləri siyahısına bir əvəzləşmə sətri yazır (işçi müraciət etmir, sistem yaradır — bax Avans Müraciətləri səhifəsinin təlimatı). Həmin sətir növbəti ayın maaş hesablamasında avtomatik tutulur.

# "Əvəzləşmə avanslarını indi yarat" düyməsi
Yalnız bu funksiyadan ƏVVƏL planlaşdırılmış köhnə müraciətlərdə, əvəzləşmə hələ yaradılmayıbsa görünür — bir dəfəlik, geriyə düzəliş üçündür. Təkrar basmaq təhlükəsizdir, artıq yaradılmış sətri ikinci dəfə yaratmır. Yeni müraciətlərdə bu artıq avtomatik baş verdiyi üçün düymə görünmür.

# Planla / İcra et
*Planla* — məbləği təsdiqləyir və ödəniş tarixini planlaşdırır (həmin andan əvəzləşmə sətirləri də yaranır). *İcra et* — planlaşdırılmış ödənişi faktiki icra olunmuş kimi işarələyir.',
0, 0, 0, GETDATE(), 0);

/* ── NƏTİCƏ ─────────────────────────────────────────────────────────────── */
SELECT Modul, Basliq, Acar, Slug
FROM   SehifeYardimlari
WHERE  Silinib = 0 AND Acar IN (N'hr/avans/index', N'hr/mezuniyyetodenis/index', N'hr/mezuniyyetodenis/detail')
ORDER  BY Acar;
