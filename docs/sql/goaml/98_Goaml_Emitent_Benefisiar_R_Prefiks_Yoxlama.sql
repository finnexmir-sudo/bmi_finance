/* ============================================================================
   GOAML — `odb.emitent_benefisiar` ilə `odb.r_emitent_benefisiar` EYNİ CƏDVƏLDİRMİ?
   (13.11.2026)

   BU FAYLI ORACLE-DA (PL/SQL Developer və s.) İŞLƏT — SQL Server-də YOX.
   Yalnız SELECT — heç nə yazmır, heç nə dəyişmir.

   Niyə lazımdır: `GoamlService.EmitentKycAsync` (CON/Nümayəndə doldurması,
   #22 tapşırığı) `odb.emitent_benefisiar`-dan (prefikssiz) oxuyur — bu, artıq
   `docs/sql/aml/90_AML_OracleSorgular.sql`-dəki sınanmış "AML Hesab üzrə
   sorğu" ilə TƏSDİQLƏNİB, DOĞRUDUR.

   Amma AYSEL-in öz `JSON_MMX_IN` prosedurunun arxivlənmiş mətni
   (`docs/sql/goaml/97_Goaml_JSON_MMX_IN_Tam_Metn.sql`, sətir 274-280, 551-566)
   PEP bayraqlarını (E_SIYASI_XADIM_DEYIL, E_SIYASI_XADIMIN_AILE_UZVLERI)
   **`r_emitent_benefisiar`** (prefiksli) adından oxuyur — AYNI FILIALNUMBER+
   DOC_ID açarı ilə. Bu sorğu bunların HƏQİQƏTƏN eyni obyekt (sinonim/view)
   olub-olmadığını, yoxsa iki MÜSTƏQİL cədvəl olduğunu göstərir.

   ── A — HƏR İKİSİ MÖVCUDDURMU, HANSI NÖV OBYEKTDİR (TABLE/VIEW/SYNONYM)? ────
   ========================================================================== */

SELECT owner, object_name, object_type, status
  FROM all_objects
 WHERE object_name IN ('EMITENT_BENEFISIAR', 'R_EMITENT_BENEFISIAR')
 ORDER BY object_name, owner;

/* Nəticə TABLE/TABLE isə — iki MÜSTƏQİL cədvəldir, aşağıdaki B/C-yə keç.
   Nəticədə biri VIEW çıxırsa — aşağıdaki D-yə keç (view-un mətninə bax). */

/* ── B — SÜTUNLAR YAN-YANA (hər ikisində HANSI sütun var/yoxdur) ──────────── */

SELECT 'EMITENT_BENEFISIAR' AS cedvel, column_name, data_type, data_length
  FROM all_tab_columns
 WHERE owner = 'ODB' AND table_name = 'EMITENT_BENEFISIAR'
UNION ALL
SELECT 'R_EMITENT_BENEFISIAR' AS cedvel, column_name, data_type, data_length
  FROM all_tab_columns
 WHERE owner = 'ODB' AND table_name = 'R_EMITENT_BENEFISIAR'
 ORDER BY column_name, cedvel;

/* ── C — Sadəcə BİR tərəfdə olan sütunlar (fərq) ──────────────────────────── */

SELECT column_name, 'YALNIZ EMITENT_BENEFISIAR-da var' AS fark
  FROM all_tab_columns
 WHERE owner = 'ODB' AND table_name = 'EMITENT_BENEFISIAR'
   AND column_name NOT IN (
       SELECT column_name FROM all_tab_columns
        WHERE owner = 'ODB' AND table_name = 'R_EMITENT_BENEFISIAR')
UNION ALL
SELECT column_name, 'YALNIZ R_EMITENT_BENEFISIAR-da var' AS fark
  FROM all_tab_columns
 WHERE owner = 'ODB' AND table_name = 'R_EMITENT_BENEFISIAR'
   AND column_name NOT IN (
       SELECT column_name FROM all_tab_columns
        WHERE owner = 'ODB' AND table_name = 'EMITENT_BENEFISIAR')
 ORDER BY fark, column_name;

/* Nəticə BOŞ çıxarsa — sütunlar tam eynidir (yəni çox güman view/sinonimdir
   və ya eyni strukturda iki ayrı snapshot cədvəldir). Fərq çıxarsa — iki
   MÜSTƏQİL, FƏRQLİ strukturlu cədvəldir, "bəs edir" sualının cavabı NƏYƏ
   görə lazım olduğuna bağlıdır (CON adı/ünvanı üçün emitent_benefisiar
   kifayətdir, PEP bayraqları yalnız R_-də olsa, PEP funksiyası üçün
   R_EMITENT_BENEFISIAR LAZIM OLACAQ). */

/* ── D — R_EMITENT_BENEFISIAR VIEW-dirsə, onun ÖZ mətni (hansı cədvələ baxır) */

SELECT text
  FROM all_views
 WHERE owner = 'ODB' AND view_name = 'R_EMITENT_BENEFISIAR';

/* ── E — PEP bayraqları (E_SIYASI_XADIM_DEYIL / E_SIYASI_XADIMIN_AILE_UZVLERI)
        HƏR İKİ cədvəldə VARMI? (B-nin nəticəsi bunu da göstərir, amma
        tam əminlik üçün ayrıca) ──────────────────────────────────────────── */

SELECT 'EMITENT_BENEFISIAR' AS cedvel, column_name
  FROM all_tab_columns
 WHERE owner = 'ODB' AND table_name = 'EMITENT_BENEFISIAR'
   AND column_name IN ('E_SIYASI_XADIM_DEYIL', 'E_SIYASI_XADIMIN_AILE_UZVLERI', 'E_PINCODE', 'E_VETENDASHLIGI')
UNION ALL
SELECT 'R_EMITENT_BENEFISIAR' AS cedvel, column_name
  FROM all_tab_columns
 WHERE owner = 'ODB' AND table_name = 'R_EMITENT_BENEFISIAR'
   AND column_name IN ('E_SIYASI_XADIM_DEYIL', 'E_SIYASI_XADIMIN_AILE_UZVLERI', 'E_PINCODE', 'E_VETENDASHLIGI')
 ORDER BY cedvel, column_name;

/* ── F — Eyni DOC_ID üçün hər ikisindən sətir çıxırmı, dəyərlər eynidirmi?
        (real bir RECNUM/FILIALNUMBER ilə, mən soruşmadan əvvəl özünüz
        bir konkret əməliyyatın recnum/filialnumber-ini yazın) ──────────────── */

-- SELECT * FROM odb.emitent_benefisiar   WHERE doc_id = <RECNUM> AND filialnumber = <FILIALNUMBER>;
-- SELECT * FROM odb.r_emitent_benefisiar WHERE doc_id = <RECNUM> AND filialnumber = <FILIALNUMBER>;
