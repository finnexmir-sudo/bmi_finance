-- ============================================================================
-- GOAML — Diaqnostika (16.11.2026)
-- ============================================================================
-- İKİ AYRI sual üçün:
--
-- A) "repr_id_serial"/"repr_id_number" niyə boş çıxır? — FOXPRO (tətbiq hesabı)
--    `odb.mmx_id_serial` / `odb.types_of_documents`-ə SELECT icazəsinə
--    malikdirmi? (ƏVVƏLKİ `balscht_fiz`/`balscht_ur`, `malumatin_novu`
--    ORA-01031 hadisələri İLƏ EYNİ şübhə.)
--
-- B) "Account" bloku — Sender/Receiver account NÖMRƏSİ (sender_account_number/
--    receiver_account_number) haradan gəlməlidir? AYSEL-in öz mənbəyi
--    (97_...sql, sətir 1245/1407) `v_goal.account`/`v_goal.account_to`
--    yazır — bunlar `GOAML_CLIENTS`-in ÖZ sütunlarıdır (DEBET/KREDIT İLƏ
--    EYNİ İNSERT-in bir hissəsi, AMMA AYRI sütun). Bu sorğu GOAML_CLIENTS-də
--    ARTIQ MÖVCUD OLAN (AYSEL-in real işlədiyi) bir sətirdə ACCOUNT/ACCOUNT_TO-nun
--    DEBET/KREDIT ilə NECƏ əlaqəli olduğunu BİRBAŞA göstərir — TƏXMİN YOXDUR.
--
-- NECƏ İŞLƏDİLİR: hər bloku ƏVVƏLCƏ ÖZ (tam icazəli) PL/SQL Developer
-- sessiyanızda, SONRA (A üçün MƏCBURİ) tətbiqin FOXPRO hesabı ilə işlədin.
-- ============================================================================


-- ─── A1) odb.mmx_id_serial — FOXPRO görə bilirmi? ───────────────────────────
select count(*) as mmx_id_serial_say from odb.mmx_id_serial;

-- ─── A2) odb.types_of_documents — FOXPRO görə bilirmi? ──────────────────────
select count(*) as types_of_documents_say from odb.types_of_documents;

-- Hər ikisi FOXPRO ilə 0 (və ya ORA-xxxxx xətası) qaytarır, ODB/sizin öz
-- sessiyanızda isə real say qaytarırsa — icazə problemidir, DBA-dan:
--   GRANT SELECT ON odb.mmx_id_serial TO FOXPRO;
--   GRANT SELECT ON odb.types_of_documents TO FOXPRO;


-- ─── B) RecNum 22903177 üçün GOAML_CLIENTS-in ÖZ ACCOUNT/ACCOUNT_TO dəyəri ──
-- (AYSEL bu konkret əməliyyatı artıq saxlayıb — real nəticə, təxmin yoxdur)
select
    transactionnumber,
    debet,
    kredit,
    account,
    account_to,
    filialnumber,
    tip,
    report_code
from odb.goaml_clients
where transactionnumber = 22903177;

-- Gözlənilən suallara cavab:
--   account = debet  VƏ  account_to = kredit   → Debet/Kredit-dən köçürmək kifayətdir
--   account/account_to BOŞ qalıb (AYSEL-in özündə də)  → bu sahə həmişə boş ola bilər,
--        bizim kod səhv deyil, sadəcə mənbə datası yoxdur
--   account/account_to DEBET/KREDIT-dən FƏRQLİ bir dəyərdir → ayrıca Oracle
--        mənbəyi (çox güman "Update New" kaskadının hələ tapılmamış bir qaydası)
--        var, port etmək üçün YENİDƏN v$sql/v$open_cursor araşdırması lazımdır
