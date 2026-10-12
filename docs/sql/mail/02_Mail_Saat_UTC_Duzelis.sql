/* ============================================================================
   GƏLƏN/GÖNDƏRİLƏN MAİL — SAAT DÜZƏLİŞİ (UTC → server yerli vaxtı) · 12.10.2026
   ----------------------------------------------------------------------------
   BU SKRİPT HEÇ NƏ DƏYİŞMİR — YALNIZ SELECT. Sondakı UPDATE blokları ŞƏRH
   İÇİNDƏDİR, qərar və icra sizindir.

   NİYƏ YARANDI: `GelenMailImapSyncer.cs`/`GedenMailImapSyncer.cs` mail tarixini
   `msg.Date.UtcDateTime` ilə yazırdı, qonşu `YaradilmaTarixi` sahəsi isə
   `DateTime.Now` (server yerli vaxtı) — ekranda 4 saat (Azərbaycan UTC+4)
   fərq yaranırdı. Kod düzəldildi (`.LocalDateTime`), AMMA bu, YALNIZ bundan
   sonra sinxronlaşan (yeni gələn/göndərilən) mailləri düzəldir — sinxron
   `MessageId` üzrə artıq bilinən sətirləri YENİDƏN oxumur, köhnə sətirlər
   UTC-də qalır.

   Bu skript bazada ARTIQ olan sətirləri +4 saat irəli çəkərək düzəldir.
   ⚠️ Yalnız Azərbaycan vaxt qurşağı üçün (UTC+4, yay/qış saatı FƏRQİ YOXDUR,
   2016-dan bəri sabitdir) — başqa server saat qurşağında bu ədəd fərqlidir.

   ⚠️ TƏKRAR İŞLƏTMƏ — hər icra sətirləri YENƏ +4 saat sürüşdürər. UPDATE-i
   YALNIZ BİR DƏFƏ icra et, WHERE şərtindəki tarix hüdudunu (bax aşağı) özünüzün
   DÜZƏLİŞ başlama anına uyğun saxlayın ki, düzəlişdən SONRA (artıq doğru) gələn
   sətirlər təkrar sürüşməsin.
   ============================================================================ */
SET NOCOUNT ON;

/* ── 1) Nə qədər sətir təsirlənəcək — kod düzəlişindən ƏVVƏL sinxronlaşmış
         (deməli hələ UTC-də qalan) bütün Gələn/Göndərilən mail sətirləri ── */
SELECT 'GelenMailler' AS Cedvel, COUNT(*) AS SetirSayi,
       MIN(AlinmaTarixi) AS EnKohne, MAX(AlinmaTarixi) AS EnYeni
FROM GelenMailler
WHERE Silinib = 0
UNION ALL
SELECT 'GedenMailler', COUNT(*), MIN(GonderilmeTarixi), MAX(GonderilmeTarixi)
FROM GedenMailler
WHERE Silinib = 0;

/* ── 2) Nümunə sətirlər (ilk 20) — düzəlişdən ƏVVƏL/SONRA necə görünəcəyini
         yoxlamaq üçün ── */
SELECT TOP 20 Id, Movzu, KimdenEmail,
       AlinmaTarixi            AS HazirkiDeyer,
       DATEADD(HOUR, 4, AlinmaTarixi) AS DuzelishdenSonra
FROM GelenMailler
WHERE Silinib = 0
ORDER BY Id DESC;

/* ── 3) ƏN VACİB: bu skripti HANSI ANA QƏDƏR icra etmək lazımdır? ──────────
   Kod düzəlişi commit `645bb4c3` ilə tətbiq olundu (12.10.2026). Server bu
   commit-dən sonrakı kodla YENİDƏN DEPLOY olunduğu andan etibarən YENİ
   sinxronlaşan sətirlər artıq DOĞRU (server yerli vaxtı) yazılır. Deploy
   anından ƏVVƏL yazılmış sətirlər isə UTC-dədir və düzəlməlidir.

   Aşağıdakı @DeployAni dəyərini REAL deploy/rebuild tarixinizlə (server
   saatı ilə) əvəz edin — bu tarixdən SONRA yazılmış sətirləri UPDATE-ə
   DAXİL ETMƏYİN, yoxsa onlar artıq doğru olduğu halda yenidən sürüşdürülər. */

-- DECLARE @DeployAni DATETIME2 = '2026-10-12 16:00:00';  -- ⚠️ öz deploy vaxtınızla əvəz edin

-- UPDATE GelenMailler
-- SET AlinmaTarixi = DATEADD(HOUR, 4, AlinmaTarixi)
-- WHERE Silinib = 0 AND AlinmaTarixi < @DeployAni;

-- UPDATE GedenMailler
-- SET GonderilmeTarixi = DATEADD(HOUR, 4, GonderilmeTarixi)
-- WHERE Silinib = 0 AND GonderilmeTarixi < @DeployAni;

/* ── Alternativ (daha təhlükəsiz, amma YAVAŞ) — Mail İzləmə bildirişləri və
   siyahı sıralaması tarixə görə getdiyi üçün UPDATE əvəzinə sadəcə ən köhnə
   sətirləri SİLİB yenidən IMAP-dan sinxronlaşdırmaq da mümkündür (qoşmalar
   diskdə qalır, DB sətri yenidən yaranar) — bu, UPDATE-dən daha çox vaxt
   aparır (hər mail üçün yenidən IMAP sorğusu), amma "hansı anadək" sualını
   aradan qaldırır. Seçim sizindir. ── */
