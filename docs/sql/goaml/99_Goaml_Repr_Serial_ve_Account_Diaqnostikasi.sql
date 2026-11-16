-- ============================================================================
-- GOAML — Diaqnostika (16.11.2026, YENİLƏNİB — hər ikisi artıq İXTİYARİDİR)
-- ============================================================================
-- Bu faylın İLK versiyası iki açıq sualı (A: repr_id_serial/number, B: Account
-- mənbəyi) həll etmək üçün Oracle-da YOXLAMA tələb edirdi. İKİSİ DƏ sonradan
-- KODDA həll olundu — bu fayl artıq MƏCBURİ DEYİL, yalnız İSTƏYƏNDƏ daha
-- DƏQİQ nəticə üçün saxlanılır.
--
-- A) "repr_id_serial"/"repr_id_number" — DÜZƏLDİ, GRANT TƏLƏB OLUNMUR.
--    `ParcalaSenedNomresi` indi Oracle `odb.mmx_id_serial` lüğəti əlçatmaz
--    olsa (GRANT yoxdur/sorğu uğursuzdur) belə, Oracle-dan ASILI OLMAYAN bir
--    YEDƏK qayda ilə seriya/nömrəni ayırır: sənədin başındaki hərf/işarə
--    hissəsi seriya, ardınca gələn rəqəm nömrədir ("D10019478" → "D"/
--    "10019478"). GRANT aşağıdakı A1/A2 sorğularını vermək İNDİ YALNIZ DAHA
--    DƏQIQ (Oracle-un öz rəsmi prefiks siyahısına görə) nəticə üçündür, ZƏRURİ
--    DEYİL — verməsəniz də seriya/nömrə sahələri artıq boş qalmayacaq.
--
-- B) "Account" bloku — mübadilə stolu (Tip=FToF, kassa/GL hesabı) üçün BAĞLANDI.
--    İstifadəçi real AYSEL faylını göstərdi: Account bu ssenarida ORADA DA
--    YOXDUR. Kod indi bu halda Account-u (həm Sender, həm Receiver) TAMAMİLƏ
--    yazmır (əvvəl Receiver-də səhvən yazılırdı) — aşağıdakı B sorğusu bu
--    halı bir daha YOXLAMIR.
--
--    ⚠️ AÇIQ QALAN (fərqli ssenari) — mübadilə-stolu OLMAYAN əməliyyatlarda
--    (real müştəri hesabı) Account bloku YENƏ DƏ yazılır, AMMA onun
--    `sender_account_number`/`receiver_account_number`-i (`e.Account`/
--    `e.AccountTo`) HEÇ YERDƏ doldurulmur (nə formada, nə controller-də) —
--    bu halda yenə boş çıxacaq. Bu, BAŞQA bir ssenari olduğu üçün BU sessiyada
--    TƏSDİQLƏNMƏYİB — real bir qeyri-mübadilə-stolu AYSEL nümunəsi göndərilsə,
--    aşağıdakı B sorğusu o nümunənin RecNum-u ilə işə yarayacaq.
-- ============================================================================


-- ─── A — İXTİYARİ, YALNIZ DƏQİQLİK ÜÇÜN: FOXPRO görə bilirmi? ───────────────
select count(*) as mmx_id_serial_say from odb.mmx_id_serial;
select count(*) as types_of_documents_say from odb.types_of_documents;
-- 0/xəta çıxsa belə, yedək qayda işləyir — GRANT MƏCBURİ DEYİL:
--   GRANT SELECT ON odb.mmx_id_serial TO FOXPRO;
--   GRANT SELECT ON odb.types_of_documents TO FOXPRO;


-- ─── B — YALNIZ qeyri-mübadilə-stolu bir ssenari araşdırılanda lazımdır ─────
-- (<RECNUM> yerinə real, Account-un YAZILMALI olduğu bir əməliyyatın RecNum-unu yaz)
select transactionnumber, debet, kredit, account, account_to, filialnumber, tip, report_code
from odb.goaml_clients
where transactionnumber = <RECNUM>;
