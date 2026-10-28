-- ============================================================================
-- İşçi Id=3 və Id=25 üçün 2025-ci ilin hər ayının maaş nəticələri.
-- Yalnız SELECT — heç nə dəyişmir.
-- ============================================================================

SELECT
    i.Id            AS IsciId,
    i.Ad, i.Soyad,
    m.Il, m.Ay,
    m.BrutMebleg,
    m.NetMebleg,
    CASE m.Status
        WHEN 1 THEN N'Layihə'
        WHEN 2 THEN N'Təsdiqləndi'
        WHEN 3 THEN N'Ödənildi'
        ELSE CAST(m.Status AS NVARCHAR(10))
    END             AS Status,
    m.HesablanmaTarixi,
    m.TesdiqTarixi,
    m.OdenisTarixi
FROM Maas m
JOIN Isciler i ON i.Id = m.IsciId
WHERE m.IsciId IN (3, 25)
  AND m.Il = 2025
  AND m.Silinib = 0
ORDER BY i.Id, m.Ay;
