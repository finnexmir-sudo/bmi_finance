-- ═══════════════════════════════════════════════════════════════════════
--  ŞƏXSİYYƏT VƏSİQƏSİNİN BİTMƏ TARİXİ — BMI (Oracle), YALNIZ SELECT
--
--  Səhifə : HR → Müddətlər → «Şəxsiyyət vəsiqəsi» sekmesi
--  Servis : FinNex.Application/Services/HR/VesiqeService.cs
--  Sorğu adı (OracleSorgular.SorguAdi): VESIQE_BITME
--
--  ⚠️ BU MƏLUMAT HEÇ YERƏ YAZILMIR (istifadəçi qərarı 09.09.2026).
--     `Isci` cədvəlində vəsiqə tarixi sütunu YOXDUR və olmamalıdır —
--     səhifə hər açılanda rəqəmi birbaşa BMI-dən oxuyur. İki nüsxə
--     saxlansaydı biri gec-tez köhnələrdi.
--
--  ⚠️ SÜTUN ADLARI (alias) MƏCBURİDİR — servis onları adla axtarır:
--         FIN                 → regnom.pincode
--         VESIQE_BIT_TARIXI   → regnom.pasport_date_close
--     Alias dəyişsə dəyər səssizcə boşalmır — səhifə açıq xəbərdarlıq verir
--     («sorğunun sütun adları uyğun deyil»).
--
--  ⚠️ TARİX SÜTUNU MƏTNƏ ÇEVRİLMİR — `to_char(...)` YAZMA. Servis Oracle
--     DATE tipini birbaşa götürür; mətnə çevirsək mədəniyyət qarışığı
--     riski yaranır (CLAUDE.md — «Oracle Rəqəmi: ToString + Parse»).
--
--  {FINLER} — İSTƏYƏ BAĞLI yer tutucusu. Sorğuda varsa, servis onu
--     işçilərin FİN-lərinin təmizlənmiş siyahısı ilə əvəz edir
--     ('5AB2CD1','1XY9Z8Q',...) və Oracle yalnız lazım olan sətirləri
--     qaytarır. Yoxdursa bütün `regnom` gəlir və süzgəc yaddaşda işləyir —
--     nəticə eynidir, sadəcə daha çox sətir daşınır.
-- ═══════════════════════════════════════════════════════════════════════

-- ── TÖVSİYƏ OLUNAN VARİANT ({FINLER} ilə — yalnız bizim işçilər) ────────
select r.pincode              as FIN,
       r.name_regnom          as ADI,
       r.pasport_date_close   as VESIQE_BIT_TARIXI
  from regnom r
 where r.svazanniy = 1
   and upper(trim(r.pincode)) in ({FINLER})


-- ── SADƏ VARİANT (istifadəçinin verdiyi orijinal — bütün sətirlər) ─────
-- select r.pincode            as FIN,
--        r.name_regnom        as ADI,
--        r.pasport_date_close as VESIQE_BIT_TARIXI
--   from regnom r
--  where r.svazanniy = 1


-- ═══════════════════════════════════════════════════════════════════════
--  QURAŞDIRMA — Admin → Oracle Sorğuları:
--     SorguAdi   : VESIQE_BITME
--     Mahiyyet   : Şəxsiyyət vəsiqəsinin bitmə tarixi (HR → Müddətlər)
--     SorguMetni : yuxarıdakı SELECT (şərh sətirləri olmadan)
--     Aktiv      : bəli
--
--  YOXLAMA: bir işçinin FİN-i ilə tək sətir çıxarıb tarixi vəsiqənin
--  üzərindəki tarixlə tutuşdur. `regnom`-da bir FİN üçün birdən çox sətir
--  ola bilər — servis ƏN SON (ən böyük) tarixi götürür, yəni köhnə vəsiqə
--  sətri yenisini üstələmir.
-- ═══════════════════════════════════════════════════════════════════════
