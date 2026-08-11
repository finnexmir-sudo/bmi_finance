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
  AND ISNULL(i.Sebeb, '') NOT LIKE N'Jetonla ödənilib%'
ORDER BY i.IcazeTarixi;

/* ── ƏLLƏ YOXLANMALI — avtomatik düzəlişə DAXİL DEYİL ────────────────────
   Pəncərəsi 180+nahar həddini də aşan qeydlər: bunlar nə adi, nə də nahar
   limitindən keçə bilməzdi → başqa yolla (köhnə build / jeton / əl ilə) yaranıb.
   Bayrağı təxmin etmək düzgün olmaz, ona görə burada yalnız SİYAHILANIR.       */
SELECT  i.Id,
        isc.Ad + ' ' + isc.Soyad                        AS Isci,
        i.IcazeTarixi, i.BaslamaSaati, i.BitisSaati,
        DATEDIFF(MINUTE, i.BaslamaSaati, i.BitisSaati)  AS PencereDeq,
        i.NaharNezereAlinmasin, i.JetonOdenenSaat, i.Sebeb, i.YaradilmaTarixi,
        N'ƏLLƏ YOXLA — limitdən uzun pəncərə'           AS Qeyd
FROM Icazeler i
JOIN Isciler isc ON isc.Id = i.IsciId
WHERE i.Silinib = 0
  AND i.Status = 5
  AND DATEDIFF(MINUTE, i.BaslamaSaati, i.BitisSaati) > 180 + @NaharDeq
ORDER BY i.IcazeTarixi DESC;

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
  AND  ISNULL(i.Sebeb, '') NOT LIKE N'Jetonla ödənilib%';

-- Gözlənilən sətir sayını yoxla, düzdürsə COMMIT, deyilsə ROLLBACK.
SELECT @@ROWCOUNT AS DeyisenSetir;

-- COMMIT;
-- ROLLBACK;
*/
