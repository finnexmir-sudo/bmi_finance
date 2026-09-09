-- ═══════════════════════════════════════════════════════════════════════
--  ŞƏXSİYYƏT VƏSİQƏSİNİN BİTMƏ TARİXİ — BMI (Oracle), YALNIZ SELECT
--
--  Səhifə : HR → Müddətlər → «Şəxsiyyət vəsiqəsi» sekmesi
--  Servis : FinNex.Application/Services/HR/VesiqeService.cs
--  Sorğu adı (OracleSorgular.SorguAdi): VESIQE_BITME
--
--  ┌───────────────────────────────────────────────────────────────────┐
--  │ ⛔ BU FAYLI BÜTÖV KOPYALAMA!                                       │
--  │                                                                   │
--  │ `OracleService.YalnizSelect` sorğunun «SELECT» və ya «WITH» ilə    │
--  │ BAŞLAMASINI tələb edir. Mətn `--` şərh sətri ilə başlayırsa sorğu  │
--  │ Oracle-a HEÇ GETMİR və səhifə xəta verir.                         │
--  │                                                                   │
--  │ Admin → Oracle Sorğuları xanasına YALNIZ aşağıdakı «KOPYALANACAQ   │
--  │ MƏTN» bölməsindəki 6 sətri yapışdır — şərhsiz, ilk simvol «s».     │
--  └───────────────────────────────────────────────────────────────────┘
--
--  ⚠️ SÜTUN ADLARI (alias) MƏCBURİDİR — servis onları adla axtarır:
--         FIN                 → regnom.pincode
--         VESIQE_BIT_TARIXI   → regnom.pasport_date_close
--     Alias dəyişsə səhifə açıq xəbərdarlıq verir («sütun adları uyğun deyil»).
--
--  ⚠️ TARİX SÜTUNU MƏTNƏ ÇEVRİLMİR — `to_char(...)` YAZMA. Servis Oracle
--     DATE tipini birbaşa götürür; mətnə çevirsək mədəniyyət qarışığı
--     riski yaranır (CLAUDE.md — «Oracle Rəqəmi: ToString + Parse»).
--
--  {FINLER} — servis onu işçilərin FİN-lərinin təmizlənmiş siyahısı ilə
--     əvəz edir ('5AB2CD1','1XY9Z8Q',...). Beləcə Oracle bütün `regnom`
--     cədvəlini yox, yalnız bizim işçiləri qaytarır.
--     Yer tutucusunu çıxarsan da işləyir — süzgəc onda yaddaşda gedir.
--
--  ⚠️ BU MƏLUMAT HEÇ YERƏ YAZILMIR (istifadəçi qərarı 09.09.2026).
--     `Isci` cədvəlində vəsiqə tarixi sütunu YOXDUR — səhifə hər açılanda
--     rəqəmi birbaşa BMI-dən oxuyur.
-- ═══════════════════════════════════════════════════════════════════════


-- ─────────────────  KOPYALANACAQ MƏTN — BU SƏTİRDƏN SONRA  ─────────────────
select r.pincode            as FIN,
       r.name_regnom        as ADI,
       r.pasport_date_close as VESIQE_BIT_TARIXI
  from regnom r
 where r.svazanniy = 1
   and upper(trim(r.pincode)) in ({FINLER})
-- ─────────────────  KOPYALANACAQ MƏTN — BU SƏTİRDƏN ƏVVƏL  ─────────────────


-- ═══════════════════════════════════════════════════════════════════════
--  QURAŞDIRMA — Admin → Oracle Sorğuları:
--     SorguAdi   : VESIQE_BITME
--     Mahiyyet   : Şəxsiyyət vəsiqəsinin bitmə tarixi (HR → Müddətlər)
--     SorguMetni : yuxarıdakı 6 sətir (ŞƏRHSİZ, ilk simvol «s»)
--     Aktiv      : bəli
--
--  YOXLAMA: bir işçinin FİN-i ilə tək sətir çıxarıb tarixi vəsiqənin
--  üzərindəki tarixlə tutuşdur. `regnom`-da bir FİN üçün birdən çox sətir
--  ola bilər — servis ƏN SON (ən böyük) tarixi götürür, yəni köhnə vəsiqə
--  sətri yenisini üstələmir.
--
--  İstifadəçinin verdiyi orijinal sorğu (süzgəcsiz — bütün `regnom`):
--     select r.pincode, r.name_regnom, r.pasport_date_close
--       from regnom r where r.svazanniy = 1
--  Yuxarıdakı variant onun alias əlavə edilmiş və FİN üzrə süzülmüş halıdır.
-- ═══════════════════════════════════════════════════════════════════════
