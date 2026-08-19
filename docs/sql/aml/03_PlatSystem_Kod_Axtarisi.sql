-- ══════════════════════════════════════════════════════════════════════════
-- PLAT_SYSTEM — addım 3: lüğət cədvəli YOXDUR, kodu yazan KOD-da axtarırıq
--
-- İndiyə qədər düşənlər:
--   ✗ FK yoxdur
--   ✗ AML_SETUP_BANK_PLATFORMS — yalnız 0,1,2,3 (Web/POS/ATM/Mobil);
--     sənəddə 4,5,6 da var. Üstəlik bu, ÇATDIRILMA KANALI məfhumudur.
--   ✗ XONKS_PLAT_KOD — kommunal ödəniş kodlarıdır (Elektro/Su/Qaz/Telefon).
--     ⚠️ FAYDALI: bu, şablonun AS sütununun («Kommunal ödəniş kodu») lüğətidir.
--   ✗ MFO.PLAT_SYSTEM — yalnız 99 və NULL; sənəddəki 0/3/4/5/6-ya uyğun deyil.
--
-- Real dəyər sahəsi:  0, 3, 4, 5, 6
--   SWIFT və INVAL (xarici) → HƏMİŞƏ 0  → 0 = «təyin edilməyib» ehtimalı
--   NACVAL / POSTUPL (daxili, manat) → 3, 4, 5, 6
-- ══════════════════════════════════════════════════════════════════════════


-- ── 1) Sütunun öz şərhi (comment) ───────────────────────────────────────
select owner, table_name, column_name, comments
  from all_col_comments
 where owner = 'ODB'
   and upper(column_name) = 'PLAT_SYSTEM'
   and comments is not null;


-- ── 2) ƏN GÜCLÜ YOL: PL/SQL mənbəyində axtarış ──────────────────────────
-- Kodu YAZAN prosedur/paket adətən dəyərləri sabit kimi saxlayır
-- (məs. `plat_system := 6;  -- XOHKS`) və ya `decode`/`case` ilə adlandırır.
select s.owner, s.name, s.type, s.line, trim(s.text) setir
  from all_source s
 where upper(s.text) like '%PLAT_SYSTEM%'
 order by s.owner, s.name, s.line;


-- ── 3) View-larda axtarış (Oracle 12.2+) ────────────────────────────────
-- `text` sütunu LONG-dur, LIKE işləmir; `text_vc` versiyası işləyir.
-- ORA-00904 verərsə bu sorğunu buraxıb 4-cü ilə davam et.
select owner, view_name
  from all_views
 where upper(text_vc) like '%PLAT_SYSTEM%';


-- ── 4) Kontekst: hər kod üçün 3 nümunə sətir ────────────────────────────
-- Lüğət heç yerdə yoxdursa, mühasib/əməliyyatçı sətirlərə baxıb kodu
-- tanıya bilər («aha, 6 = XÖHKS, 4 = AZIPS» kimi).
select plat_system, nomer_docum, date_oper, summa_v_nacval,
       name_debet, name_credit, mfo_debet, mfo_credit
  from (select v.*,
               row_number() over (partition by v.plat_system
                                      order by v.date_oper desc) rn
          from odb.doc_vnesh_nacval v)
 where rn <= 3
 order by plat_system, rn;


-- ── 5) Eyni şey mədaxil tərəfi üçün ─────────────────────────────────────
select plat_system, nomer_docum, date_oper, sum1,
       name_debet, kredit_name, mfo_debet, mfo_kredit
  from (select v.*,
               row_number() over (partition by v.plat_system
                                      order by v.date_oper desc) rn
          from odb.doc_vnesh_postupl v)
 where rn <= 3
 order by plat_system, rn;
