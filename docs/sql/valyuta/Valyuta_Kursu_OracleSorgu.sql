/* ============================================================================
   VALYUTA KURSU — Oracle sorğusu (20 000 USD aylıq köçürmə limiti üçün)
   07.09.2026
   ----------------------------------------------------------------------------
   Bir sorğu əlavə edir (idempotent — varsa təkrar yaratmır):

       VALYUTA_KURSU — bir valyutanın verilmiş tarixdəki MB kursu

   ── NİYƏ LAZIMDIR ────────────────────────────────────────────────────────
   Qanun: rezident və qeyri-rezident fiziki şəxsin təqvim ayı ərzində cəmi
   20 000 ABŞ dolları EKVİVALENTİNƏDƏK köçürmələri məqsədi bəyan edilməklə
   aparılır. «Ekvivalent» sözü çevirmə tələb edir: formada məbləğ USD, Avro
   və ya AZN ola bilər, limit isə USD-dədir.

       UsdEkvivalent = Mebleg × kurs(MedaxilValyuta) ÷ kurs(USD)

   AZN üçün kurs 1-dir, USD üçün özü-özünə bölünür → Mebleg-in özü çıxır.

   ── FUNKSİYA HAQQINDA ────────────────────────────────────────────────────
   `odb.func_get_kurval(<kod>, <tarix>)` — «1 vahid = N AZN» qaytarır.
   Layihədə yeni deyil: bütün IFRS9/ECL hesabatları onu işlədir, məs.
   `docs/sql/muhasibat/Muhasibat_IFRS9_ECL_Engine.sql:49`:

       (ar.summa + ar.summa_19) * ROUND(odb.func_get_kurval(
            substr(ar.licschkre,6,2), ar.date_oper), 6)

   ── YER TUTUCULARI ───────────────────────────────────────────────────────
   {KOD}   — kurval kodu: 00 AZN, 01 USD, 02 AVRO, 03 RUBL, 04 RİAL, 05 DİRHƏM
   {TARIX} — dd.MM.yyyy

   ⚠️ Hər ikisi servisdə TƏMİZLƏNİR ({KOD} yalnız rəqəm, {TARIX} formatlanmış
   `DateTime`) — `BmiValyutaService.KursAsync`. Yer tutucularının adını
   dəyişsəniz servisi də dəyişin, yoxsa əvəzləmə baş verməz və sorğu
   olduğu kimi Oracle-a gedib sınar.

   ⚠️ SÜTUN ADI «KURS» — `KursAsync` onu ADINA görə oxuyur. Adı dəyişsəniz
   kurs SƏSSİZCƏ null qayıdar və hər əməliyyat bloklanar.

   ── SÜTUN ADI VƏ TİP ─────────────────────────────────────────────────────
   Nəticə NUMBER-dir; servis onu `decimal` kimi BİRBAŞA oxuyur.
   `ToString()` + `Parse` etmir — o, dəqiq 100× səhv verir (CLAUDE.md).

   Oracle YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */

USE FinNex_Maliyye_Db;
GO

SET NOCOUNT ON;
BEGIN TRAN;

/* Departament: mövcud valyuta sorğusu ilə eyni yerdə görünsün. */
DECLARE @DepId INT;

SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi = N'VALYUTA_SIYAHISI' AND ISNULL(Silinib, 0) = 0;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
    WHERE  (SorguAdi LIKE N'HEVALE_IDXAL%' OR SorguAdi LIKE N'MEKTUB_IDXAL%')
      AND  ISNULL(Silinib, 0) = 0
    ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler WHERE ISNULL(Silinib, 0) = 0 ORDER BY Id;

IF @DepId IS NULL
BEGIN
    ROLLBACK TRAN;
    THROW 50001, N'Departament tapılmadı — @DepId əl ilə təyin edilməlidir.', 1;
END

IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'VALYUTA_KURSU' AND ISNULL(Silinib, 0) = 0)
INSERT INTO OracleSorgular
      (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES
(
  N'VALYUTA_KURSU',
  N'Bir valyutanın verilmiş tarixdəki MB kursu (1 vahid = N AZN). Pul köçürməsində 20 000 USD aylıq limitinin ekvivalent hesablaması üçün. Yer tutucular: {KOD}, {TARIX}',
  N'select round(odb.func_get_kurval(''{KOD}'', to_date(''{TARIX}'', ''dd.mm.yyyy'')), 10) as KURS
  from dual',
  1, 0, @DepId, SYSDATETIME(), 0
);

COMMIT TRAN;

/* ── Yoxlama ─────────────────────────────────────────────────────────────── */
SELECT Id, SorguAdi, Aktiv, DepartamentId, LEN(SorguMetni) AS SqlUzunlugu
FROM   OracleSorgular
WHERE  SorguAdi = N'VALYUTA_KURSU' AND ISNULL(Silinib, 0) = 0;

/* ============================================================================
   ƏL İLƏ YOXLAMA — Admin → Oracle Sorğular → «İcra et» ilə:

     select round(odb.func_get_kurval('01', to_date('07.09.2026','dd.mm.yyyy')), 10) as KURS from dual

   Gözlənilən: USD üçün ~1,70 civarında rəqəm.
     · 00 (AZN) → 1 qaytarmalıdır
     · 01 (USD) → 1,7 civarı
   Rəqəm gəlmirsə və ya 0 çıxırsa, servis kursu «alınmadı» sayır və
   köçürmə əməliyyatını BLOKLAYIR — bu, qəsdəndir (istifadəçi qərarı).
   ============================================================================ */
