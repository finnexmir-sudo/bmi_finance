/* ============================================================================
   MÜHASİBAT — Bağlı hesab (date_close_licsch) filtrinin GÖTÜRÜLMƏSİ
   ----------------------------------------------------------------------------
   PROBLEM: Balans sorğusunda "(ch.date_close_licsch IS NULL OR ar.date_oper <=
   ch.date_close_licsch)" filtri bağlı, amma arh_saldo_ls-də HƏLƏ QALIĞI OLAN
   hesabları balansdan atırdı. 15/07/2026-da bu, 49.92 AZN-lik bir kredit-faiz
   hesabını düşürürdü — nəticədə:
     - "Kredit üzrə faizlər" 188 522.52 əvəzinə 188 472.60 görünürdü
     - Balans yoxlaması Aktiv - (Öhdəlik+Kapital) = -49.92 (bağlanmırdı)
   Qalığı olan hesab REAL aktivdir (GL-də sətir varsa, pul oradadır) — atılmamalıdır.

   HƏLL: Şərti 1=1 ilə əvəz edib neytrallaşdırırıq (join qalır — ad/dep_tip üçün
   ch lazımdır). REPLACE ilə cərrahi, whitespace-safe, idempotent. Yalnız SQL —
   kod/rebuild lazım deyil, BalansAsync sorğunu dinamik oxuyur.

   YOXLAMA (balans kritikdir): işlətdikdən sonra dashboard-da balans yoxlaması
   "Fərq" göstəricisinin ~0-a düşdüyünü təsdiqlə. Əgər Fərq böyüsə, geri qaytar.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

/* 1) Əsas balans sorğusu (böyük hərflə: IS NULL OR) */
UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni,
       'ch.date_close_licsch IS NULL OR ar.date_oper <= ch.date_close_licsch',
       '1=1 /* closed-account filter removed: balances still open are kept */')
WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari';

/* 2) Dünənlə müqayisə sorğusu (kiçik hərflə: is null or) — metod eyni qalsın */
UPDATE OracleSorgular
SET    SorguMetni = REPLACE(SorguMetni,
       'ch.date_close_licsch is null or ar.date_oper <= ch.date_close_licsch',
       '1=1 /* closed-account filter removed: balances still open are kept */')
WHERE  SorguAdi = N'Muhasibat — Balans muqayise';

COMMIT TRAN;
PRINT N'Bağlı hesab filtri götürüldü (balans + müqayisə sorğuları).';

SELECT SorguAdi,
       CASE WHEN SorguMetni LIKE '%date_close_licsch%' THEN 'FILTR HƏLƏ VAR' ELSE 'GÖTÜRÜLDÜ' END AS veziyyet
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Muhasibat — Balans qaliqlari', N'Muhasibat — Balans muqayise')
ORDER  BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
