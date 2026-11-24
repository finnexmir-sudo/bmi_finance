-- ============================================================================
-- GOAML — ID_VD → DOC_VNESH_SWIFT BAĞLANTISI — DİAQNOSTİKA (24.11.2026)
-- ============================================================================
-- ⚠️ 24.11.2026, İKİNCİ DALĞA — TABLE ADI DÜZƏLDİ: "DOC_VNESH_SWIFT1" DEYİL,
-- "DOC_VNESH_SWIFT"-dir (suffikssiz). "DOC_VNESH_SWIFT1" Oracle Forms-un
-- İCİ, bloka verdiyi ALİAS adı idi (Master Mirror Item = DOC_VNESH_SWIFT1.
-- SENDER_BANK_NAME) — real cədvəl adı DEYİL. İstifadəçi
-- `SELECT * FROM doc_vnesh_swift a where a.date_oper='23-11-2026'` ilə canlı
-- Oracle-da sütun siyahısını göstərib düzəltdi.
--
-- MƏQSƏD: "sender_account_fin_inst_name" (Sender-in bank adı) real goAML-də
-- v_goal.Institution_Name_TO/institution_name-dən gəlir, bunlar isə "Update
-- New" kaskadının 7 ID_VD-asılı qaydasından BİRİ ilə (SWIFT mənbəli
-- əməliyyatlarda) doc_vnesh_swift.sender_bank_name-dən doldurulur.
--
-- ✅ NƏTİCƏ — TƏSDİQLƏNDİ (24.11.2026, canlı Oracle, iki dəfə sınanıb):
--   `odb.doc_vnesh_swift.id = odb.doc_dna_all_risk.id_vd` JOIN-i İŞLƏYİR.
--   Real nümunələr (aşağıdakı D bəndinin nəticəsi):
--     RECNUM 22906763, ID_VD 47218 → "Export Development Bank of Iran"
--     RECNUM 22906811, ID_VD 47222 → "BMI Ardabil br"
--   Bu açar `GoamlService.SwiftSenderBankAdiAsync`-də (24.11.2026) KODA
--   KÖÇÜRÜLDÜ — aşağıdakı bloklar artıq YALNIZ TARİXİ DİAQNOSTİKA ARTEFAKTI,
--   növbəti dəfə oxşar bir ID_VD/SWIFT sualı yarananda TƏKRAR işlədilə bilər.
-- ============================================================================

-- A) DOC_VNESH_SWIFT-in ÖZ SÜTUNLARI — ad, tip
SELECT column_name, data_type, data_length
FROM all_tab_columns
WHERE table_name = 'DOC_VNESH_SWIFT'
ORDER BY column_id;

-- B) BİRİNCİ ƏLÇATAN BİR NECƏ SƏTİR — SENDER_BANK_NAME HƏQİQƏTƏN DOLUDURMU?
SELECT * FROM doc_vnesh_swift WHERE ROWNUM <= 5;

-- C) ID_VD-nin DOC_DNA_ALL_RISK-dəki REAL dəyərləri (bir neçə son günün
--    SWIFT-mənşəli (document_source-da 'SWIFT'/'VNESH' keçən) sətirlərindən)
SELECT recnum, date_oper, document_source, id_vd
FROM odb.doc_dna_all_risk
WHERE id_vd IS NOT NULL
  AND ROWNUM <= 20
ORDER BY date_oper DESC;

-- D) JOIN TESTİ — ID_VD = DOC_VNESH_SWIFT-in ÖZ ID-sinə bərabərdirmi? (TƏSDİQLƏNDİ)
SELECT r.recnum, r.date_oper, r.id_vd,
       s.sender_bank_name, s.id
FROM odb.doc_dna_all_risk r
JOIN odb.doc_vnesh_swift s ON s.id = r.id_vd
WHERE r.id_vd IS NOT NULL
  AND ROWNUM <= 20
ORDER BY r.date_oper DESC;
