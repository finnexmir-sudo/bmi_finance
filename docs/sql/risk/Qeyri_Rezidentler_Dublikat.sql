/* ============================================================================
   "Qeyri-rezidentlər" HƏM KPI KARTINDA, HƏM HESABAT TAB-INDA İKİ DƏFƏ ÇIXIR
   ----------------------------------------------------------------------------
   Fərq "Ölkə üzrə müştərilər" hadisəsindən (bax Olke_Uzre_Musterler_Dublikat.sql):
   orada iki sətir FƏRQLİ sorğu idi (aqreqat vs detallı siyahı), sadəcə adı
   eyni idi. Burada isə hər iki KPI kartı EYNİ ədədi (148) göstərir — yəni bu,
   iki fərqli sorğu deyil, ƏN ÇOX EHTİMALLA sətrin TƏKRAR (copy-paste) daxil
   edilməsidir. Sənədlərdə (Risk_Panel_Widgetler.sql, Risk_TAM_Son_Versiya.sql)
   "Qeyri-rezidentlər" adı ilə YALNIZ BİR sorğu təsvir olunub — ikinci sətrin
   haradan gəldiyi sənədləşməyib.

   ⚠ Fərziyyəni TƏSDİQLƏMƏDƏN (aşağıdakı SELECT-i görmədən) heç nə silməyin —
   ikisinin SorguMetni həqiqətən FƏRQLİ ola bilər (məs. biri köhnə filtrlə,
   biri yeni) və eyni ədədi TƏSADÜFƏN verə bilər.
   ============================================================================ */

/* ── 1) DİAQNOSTİKA ───────────────────────────────────────────────────────── */
SELECT Id, SorguAdi, Mahiyyet, Aktiv, Kataloq,
       LEN(SorguMetni) AS SorguUzunlugu,
       SorguMetni
FROM OracleSorgular
WHERE SorguAdi = N'Qeyri-rezidentlər'
ORDER BY Id;

/* Gözlənilən: 2 sətir.
   - SorguMetni SÖZ-SÖZÜNƏ EYNİDİRSƏ → təkrar daxiletmədir, aşağıdakı 2-ci
     addımla KÖHNƏ (kiçik Id-li) sətri deaktiv edin, YENİSİNİ saxlayın
     (əks tərəf lazım olarsa dəyişdirə bilərsiniz).
   - SorguMetni FƏRQLİDİRSƏ → bura Ölkə üzrə müştərilər kimi "iki fərqli
     sorğu, eyni ad" halıdır; SİLMƏYİN/DEAKTİV ETMƏYİN — əvvəlcə hansının
     doğru olduğunu müəyyənləşdirin, sonra YALNIZ adlardan birini (məs.
     "Qeyri-rezidentlər (siyahı)") dəyişdirin, eynilə Ölkə üzrə faylındakı
     kimi. */


/* ── 2) DÜZƏLİŞ — YALNIZ SorguMetni EYNİ ÇIXSA işlədin.
        SİLMƏK YOX, DEAKTİV ETMƏK seçildi — DELETE geri qaytarılmır, Aktiv=0
        isə lazım olsa 1-ə qaytarıla bilər. Id-ni yuxarıdakı nəticəyə görə
        dəyişin (köhnə/duplikat olan sətr). ── */
-- UPDATE OracleSorgular
-- SET Aktiv = 0
-- WHERE Id = <duplikat sətrin Id-si>;
