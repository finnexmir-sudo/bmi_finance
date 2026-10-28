-- ============================================================================
-- Elxan Axundov (və ya axtarılan istənilən işçi) üçün "qabaqcadan ödənilmiş
-- məzuniyyət" qeydlərinin tarix sahələrini göstərir — yalnız SELECT, heç nə
-- dəyişmir.
--
-- Məqsəd: Toplu Hesabla/mühasib Excel "Məzuniyyət haqqı" sütunu
-- BaslamaTarixi (məzuniyyətin başlanğıc tarixi) üzrə ayı göstərir, provodka
-- isə OdenilmeTarixi (avansın HƏQİQƏTƏN ödənildiyi tarix) üzrə ayı yazır.
-- Bu sorğu bu ikisini yan-yana göstərir ki, fərqin səbəbi aydın olsun:
--   - OdenilmeTarixi BaslamaTarixi-dən FƏRQLİ aydadırsa → provodka DÜZGÜNDÜR,
--     avans başqa ayın provodkasına düşüb (çox güman ƏVVƏLKİ ay).
--   - OdenilmeTarixi NULL-dursa, halbuki OdenisStatus='Ödənilib'-dir →
--     real bug var, MezQabaqcadanBrutMapAsync bu qeydi HEÇ BİR ayda tapmır.
-- ============================================================================

SELECT
    i.Id                    AS IsciId,
    i.Ad, i.Soyad,
    m.Id                    AS MezuniyyetId,
    m.BaslamaTarixi,
    m.BitmeTarixi,
    m.OdenisTipi,            -- enum int: QabaqcadanOdenis-in dəyərini yoxlayın (layihədə adətən 1 və ya 2)
    m.OdenisStatus,          -- enum int: Odenilib-in dəyərini yoxlayın
    m.OdenilmeTarixi,        -- ⚠️ BUNA baxın — provodka buna görə ay seçir
    m.OdenenMeblegBrut,      -- ⚠️ NULL olarsa provodkada 0 kimi sayılır (COALESCE 0)
    m.OdenenMeblegNet,
    m.Silinib
FROM Mezuniyyetler m
JOIN Isciler i ON i.Id = m.IsciId
WHERE i.Ad LIKE N'%Elxan%' AND i.Soyad LIKE N'%Axundov%'
ORDER BY m.BaslamaTarixi DESC;
