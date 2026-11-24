-- ============================================================================
-- GOAML — ID_VD → DOC_VNESH_SWIFT1 BAĞLANTISI — DİAQNOSTİKA (24.11.2026)
-- ============================================================================
-- MƏQSƏD: "sender_account_fin_inst_name" (Sender-in bank adı) real goAML-də
-- v_goal.Institution_Name_TO/institution_name-dən gəlir, bunlar isə "Update
-- New" kaskadının 7 ID_VD-asılı qaydasından BİRİ ilə (SWIFT mənbəli
-- əməliyyatlarda) doc_vnesh_swift1.sender_bank_name-dən doldurulur (AYSEL-in
-- Oracle Forms "Properties" ekranı, 24.11.2026, Master Mirror Item =
-- DOC_VNESH_SWIFT1.SENDER_BANK_NAME).
--
-- `DOC_DNA_ALL_RISK.ID_VD`-nin NATİV sütun olduğu artıq TƏSDİQLƏNİB
-- (11.11.2026, Risk tabı işində) — AMMA bunun DOC_VNESH_SWIFT1-in ÖZ açarı
-- (ID?) ilə NECƏ bağlandığı HƏLƏ YOXLANMAYIB. Kod yazmazdan ƏVVƏL bunu
-- təsdiqləmək lazımdır — əks halda səhv join SƏSSİZCƏ heç nə tapmaz (xəta
-- yox, sadəcə boş nəticə), "düzəldi" deyib, əslində işləməyən kod yazmış olarıq.
--
-- AŞAĞIDAKI BLOKLARI PL/SQL DEVELOPER-DƏ BİR-BİR, NƏTİCƏYƏ BAXARAQ işlədin.
-- ============================================================================

-- A) DOC_VNESH_SWIFT1-in ÖZ SÜTUNLARI — ad, tip
SELECT column_name, data_type, data_length
FROM all_tab_columns
WHERE table_name = 'DOC_VNESH_SWIFT1'
ORDER BY column_id;

-- B) BİRİNCİ ƏLÇATAN BİR NECƏ SƏTİR — SENDER_BANK_NAME HƏQİQƏTƏN DOLUDURMU?
SELECT * FROM doc_vnesh_swift1 WHERE ROWNUM <= 5;

-- C) ID_VD-nin DOC_DNA_ALL_RISK-dəki REAL dəyərləri (bir neçə son günün
--    SWIFT-mənşəli (document_source-da 'SWIFT'/'VNESH' keçən) sətirlərindən)
SELECT recnum, date_oper, document_source, id_vd
FROM odb.doc_dna_all_risk
WHERE id_vd IS NOT NULL
  AND ROWNUM <= 20
ORDER BY date_oper DESC;

-- D) JOIN TESTİ — ID_VD = DOC_VNESH_SWIFT1-in ÖZ ID-sinə bərabərdirmi?
--    (B-dəki sütun siyahısına görə PK adı dəyişə bilər — "ID" yoxdursa, A
--    bəndinin nəticəsindən doğru sütun adını tapıb bura yazın)
SELECT r.recnum, r.date_oper, r.id_vd,
       s.sender_bank_name, s.id
FROM odb.doc_dna_all_risk r
JOIN odb.doc_vnesh_swift1 s ON s.id = r.id_vd
WHERE r.id_vd IS NOT NULL
  AND ROWNUM <= 20
ORDER BY r.date_oper DESC;

-- Əgər (D) sıfır sətir qaytarırsa, AMMA (C)-də id_vd dəyərləri real görünürsə —
-- join açarı "s.id = r.id_vd" YANLIŞDIR, (A)-dakı sütun siyahısına baxıb başqa
-- bir sütunu (məs. DOCUMENT_ID, SWIFT_ID) sınamaq lazımdır.
