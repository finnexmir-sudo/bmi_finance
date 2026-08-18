-- Test məqsədli Pul köçürməsi sətirləri «26-T-1» və «26-T-2» nömrələrini
-- REAL BMI idxalı ilə (GedenHevale) paylaşır. Nömrə artıq düzgün verilir
-- (HevaleNomreHelper max+1) — bunlar yalnız köhnə qalıqdır.

-- 1) ƏVVƏLCƏ BAX — nə silinəcək və qarşılığında real jurnalda nə var
SELECT 'Kocurme' AS Menbe, Id, HevaleNo, Tarix, GonderenAd, GonderenSoyad, Mebleg, Silinib
FROM   Kocurme      WHERE HevaleNo IN ('26-T-1','26-T-2')
UNION ALL
SELECT 'GedenHevale', Id, HEV_NOM, TARIX, SAA, NULL, MEBLEG, Silinib
FROM   GedenHevale  WHERE HEV_NOM IN ('26-T-1','26-T-2');

-- 2) Yuxarıdakı nəticə gözlədiyiniz kimidirsə — SİL (yalnız Kocurme tərəfi)
DELETE FROM Kocurme WHERE HevaleNo IN ('26-T-1','26-T-2');
