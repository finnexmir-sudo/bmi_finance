/* ============================================================================
   MƏKTUB JURNALI — BMI (Oracle) idxalından ƏVVƏL təmizlik
   ----------------------------------------------------------------------------
   FinNex-də sınaq məqsədilə yaradılmış qeydlər BMI nömrələri ilə TOQQUŞUR:
   BMI 2026-cı ildə 2026-651-ə çatıb, FinNex isə test qeydinə "1/2026" verib
   (səbəb: köhnə `ParseNum` "2026-651" formatını oxuya bilmirdi — kodda bağlandı).

   İdxaldan əvvəl bu qeydlər silinməlidir, yoxsa jurnalda iki fərqli nömrələmə
   qalacaq. Silinən qeydlərin `Kod`-u yoxdur (Oracle-dan gəlməyib) — meyar budur.

   İCRA QAYDASI: əvvəl ÖNBAXIŞ-ı işlət, siyahını gör, sonra SİLMƏ blokunu aç.
   ============================================================================ */

/* ── ÖNBAXIŞ — nə silinəcək ─────────────────────────────────────────────── */
SELECT Id, QEY_NOM, TARIX, GON_YER, QISA_MEZ, ICRACI, IL,
       YaradilmaTarixi, YaradanIcraciId,
       N'Oracle-dan gəlməyib (KOD boş) — FinNex test qeydi' AS Qeyd
FROM XaricMektub
WHERE Silinib = 0 AND KOD IS NULL
ORDER BY IL, Id;

SELECT Id, NOM, NOM1, DAX_TARIX, IDARE_ADI, IL,
       YaradilmaTarixi, YaradanIcraciId,
       N'Oracle-dan gəlməyib (NOM boş) — FinNex test qeydi' AS Qeyd
FROM DaxilMektub
WHERE Silinib = 0 AND NOM IS NULL
ORDER BY IL, Id;

/* ── SİLMƏ — önbaxış təsdiqləndikdən SONRA şərhi götür ───────────────────
   Yumşaq silmə (Silinib = 1) istifadə olunur: qeyd bazada qalır, siyahılardan
   düşür. Tam silmək istəyirsənsə DELETE-ə keç, amma geri qaytarmaq mümkün olmaz.

BEGIN TRAN;

UPDATE XaricMektub
SET Silinib = 1, SilinmeTarixi = SYSDATETIME()
WHERE Silinib = 0 AND KOD IS NULL;
SELECT @@ROWCOUNT AS Xaric_Silinen;

UPDATE DaxilMektub
SET Silinib = 1, SilinmeTarixi = SYSDATETIME()
WHERE Silinib = 0 AND NOM IS NULL;
SELECT @@ROWCOUNT AS Daxil_Silinen;

-- COMMIT;
-- ROLLBACK;
*/

/* ── İDXALDAN SONRA YOXLAMA ──────────────────────────────────────────────
   Hər il üzrə say və maksimum nömrə. Oracle-dakı ilə tutuşdur:
     SELECT il, COUNT(*), MAX(TO_NUMBER(SUBSTR(qey_nom, 6))) FROM odb.xaric_mektub GROUP BY il;
*/
SELECT IL,
       COUNT(*) AS Say,
       MAX(TRY_CONVERT(int, RIGHT(QEY_NOM, CHARINDEX('-', REVERSE(QEY_NOM)) - 1))) AS MaxNomre
FROM XaricMektub
WHERE Silinib = 0
GROUP BY IL
ORDER BY IL;

SELECT IL, COUNT(*) AS Say, MAX(NOM1) AS MaxNomre
FROM DaxilMektub
WHERE Silinib = 0
GROUP BY IL
ORDER BY IL;
