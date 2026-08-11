/* ============================================================================
   HR — İcazə: səhvən silinmiş "nahara çıxmıram" bayrağının bərpası
   ----------------------------------------------------------------------------
   SƏBƏB (reqressiya, 24.07.2026 — commit ec0a695e):
     Təsdiq səhifəsində nahar checkbox-u yalnız icazə pəncərəsi nahar pəncərəsi
     ilə KƏSİŞDİKDƏ render olunurdu. Kəsişməyən pəncərələrdə (məs. 14:00–17:45)
     checkbox ümumiyyətlə səhifədə yox idi → forma sahəni göndərmirdi →
     RehberTesdiqAsync-də `NaharNezereAlinmasin = status && false` işə düşür və
     işçinin müraciətdəki seçimi SƏSSİZCƏ silinirdi.
     Nəticə: pəncərə 3 saatdan uzun qalır, amma çıxılma olmur — işçi naharda
     işlədiyi halda sayğacdan tam müddət yazılır (illik 36 saatlıq balansdan da).

   MƏNTİQ: 180 dəqiqədən uzun icazə YALNIZ bayraq işarəli olduqda yaradıla bilir
     (IcazeService.YaratAsync limit yoxlaması). Deməli pəncərəsi 180 dəq-dən
     uzun, bayrağı 0 olan təsdiqlənmiş qeyddə bayraq sonradan itirilib.

   İSTİSNA: jeton icazələri (JetonService) limitə tabe deyil və bayrağı özü 1
     qoyur — onlar `Sebeb` sahəsindəki "Jetonla ödənilib" mətninə görə kənarda
     saxlanılır.

   İCRA QAYDASI: əvvəl 1-ci ADDIM-ı işlət və nəticəni yoxla, sonra 2-ci ADDIM.
   ============================================================================ */

DECLARE @ReqressiyaTarixi date = '2026-07-24';   -- ec0a695e deploy tarixi
DECLARE @NaharDeq int = (SELECT TOP 1 NaharMuddetDeqiqe FROM IsParametrleri WHERE Silinib = 0);
SET @NaharDeq = ISNULL(@NaharDeq, 45);

/* ── 1-ci ADDIM — ÖNBAXIŞ: nə dəyişəcək ─────────────────────────────────── */
SELECT  i.Id,
        isc.Ad + ' ' + isc.Soyad                                   AS Isci,
        i.IcazeTarixi, i.BaslamaSaati, i.BitisSaati,
        DATEDIFF(MINUTE, i.BaslamaSaati, i.BitisSaati)             AS PencereDeq,
        DATEDIFF(MINUTE, i.BaslamaSaati, i.BitisSaati) - @NaharDeq AS BerpadanSonraSayilanDeq,
        i.NaharNezereAlinmasin                                     AS IndikiBayraq,
        i.YaradilmaTarixi
FROM Icazeler i
JOIN Isciler isc ON isc.Id = i.IsciId
WHERE i.Silinib = 0
  AND i.Status = 5                                    -- Tesdiqlenib
  AND i.NaharNezereAlinmasin = 0
  AND i.YaradilmaTarixi >= @ReqressiyaTarixi
  AND DATEDIFF(MINUTE, i.BaslamaSaati, i.BitisSaati) > 180
  AND DATEDIFF(MINUTE, i.BaslamaSaati, i.BitisSaati) <= 180 + @NaharDeq
  AND ISNULL(i.Sebeb, '') NOT LIKE 'Jetonla %'   -- ASCII prefiks: SSMS hərf tələsi
ORDER BY i.IcazeTarixi;

/* ── 3-cü ADDIM — JETON İCAZƏLƏRİ: nahara toxunmayan qismən aralıqlar ─────
   Jeton icazəsində bu bayraq "işçi naharda işləyib" demək DEYİL — təqvim
   aralığını iş saatına çevirmək üçündür (JetonService). İcazə tərəfindəki
   çıxılma isə sabit nahar fasiləsidir; ona görə bayraq yalnız nahar pəncərəsi
   aralığın İÇİNƏ TAM düşəndə mənalıdır. Nahara toxunmayan jeton icazəsində
   (məs. 14:00–18:00) bayraq qalsa, ekran jetonla ödənilən saatdan AZ göstərir
   (4 saat əvəzinə 3,25 saat). Kod tərəfi bağlanıb — bu, keçmiş qeydlər üçündür.

   ÖNBAXIŞ: */
SELECT  i.Id, isc.Ad + ' ' + isc.Soyad AS Isci, i.IcazeTarixi,
        i.BaslamaSaati, i.BitisSaati, i.JetonOdenenSaat,
        i.NaharNezereAlinmasin AS IndikiBayraq,
        N'Bayraq 0 olmalıdır — nahar aralığa düşmür' AS Qeyd
FROM Icazeler i
JOIN Isciler isc ON isc.Id = i.IsciId
WHERE i.Silinib = 0
  AND ISNULL(i.Sebeb, '') LIKE 'Jetonla %'      -- ASCII prefiks (SSMS hərf tələsi)
  AND i.NaharNezereAlinmasin = 1
  AND NOT (i.BaslamaSaati <= '13:00:00' AND i.BitisSaati >= '13:45:00');

/* DÜZƏLİŞ (önbaxış təsdiqləndikdən sonra şərhi götür):

BEGIN TRAN;
UPDATE i SET i.NaharNezereAlinmasin = 0, i.YenilenmeTarixi = SYSDATETIME()
FROM Icazeler i
WHERE i.Silinib = 0
  AND ISNULL(i.Sebeb, '') LIKE 'Jetonla %'
  AND i.NaharNezereAlinmasin = 1
  AND NOT (i.BaslamaSaati <= '13:00:00' AND i.BitisSaati >= '13:45:00');
SELECT @@ROWCOUNT AS DeyisenSetir;   -- gözlənilən: 1 (Tural, 05.08, Id 1061)
-- COMMIT;
-- ROLLBACK;
*/

/* ── 2-ci ADDIM — DÜZƏLİŞ (1-ci addımın nəticəsi təsdiqləndikdən SONRA) ───
   Aşağıdakı blokun şərhini götürüb işlət. İdempotentdir: artıq bayrağı 1 olan
   qeydə toxunmur, təkrar işlədilsə 0 sətir dəyişir.

BEGIN TRAN;

UPDATE i
SET    i.NaharNezereAlinmasin = 1,
       i.YenilenmeTarixi      = SYSDATETIME()
FROM   Icazeler i
WHERE  i.Silinib = 0
  AND  i.Status = 5
  AND  i.NaharNezereAlinmasin = 0
  AND  i.YaradilmaTarixi >= '2026-07-24'
  AND  DATEDIFF(MINUTE, i.BaslamaSaati, i.BitisSaati) > 180
  AND  DATEDIFF(MINUTE, i.BaslamaSaati, i.BitisSaati) <= 180 + ISNULL(
         (SELECT TOP 1 NaharMuddetDeqiqe FROM IsParametrleri WHERE Silinib = 0), 45)
  AND  ISNULL(i.Sebeb, '') NOT LIKE 'Jetonla %';  -- ASCII prefiks: SSMS hərf tələsi

-- Gözlənilən sətir sayını yoxla, düzdürsə COMMIT, deyilsə ROLLBACK.
SELECT @@ROWCOUNT AS DeyisenSetir;

-- COMMIT;
-- ROLLBACK;
*/
