-- ══════════════════════════════════════════════════════════════════════════
-- AML → Hesab üzrə sorğu — YENİ şablon (47 sütun)
-- ⚠️  BU FAYL «Sahibkar / hüquqi şəxs VÖEN» VARİANTIDIR
--     Fiziki şəxs variantı: Hesab_Uzre_Sorgu_Yeni.sql
--
-- Mənbə: BMI/BMI/AML/frmhesabsorgu.cs → axtarhuquqi()
-- Tarix: 19.08.2026
--
-- ⚠️  YALNIZ SELECT. Sorğu hələ icra edilməyib — yeni sütun adları
--     (doc_vnesh_*.ID / .PLAT_SYSTEM, postupl.KREDIT_INN, muxbir_hesab.VOEN)
--     bazada yoxlanmalıdır.
--
-- İSTİFADƏ: yalnız `prm` blokunu dəyiş — hesab № və iki tarix.
--
-- ── FİZİKİ VARİANTDAN FƏRQLƏRİ (BMI-dən olduğu kimi saxlanılıb) ──────────
--   1. Göndərənin VÖEN-i (J) doludur: f.inn_regnom  (fizikidə boş '   ')
--   2. Alanın VÖEN-i (X) doludur: f.inn_regnom / h.inn_regnom
--   3. Qarşı tərəf şərtləri DEBET tərəfə baxır: substr(t.debet,1,3) in (100,150)
--      (fizikidə KREDİT tərəfə: substr(t.kredit,1,1)<>'4'), və 350 YOXDUR
--   4. Alan bankı vd.filial_name-dən gəlir (fizikidə vd.ben_bank / 'diger')
--   5. Mədaxil qolunda göndərənin adı vd.emit_name (fizikidə vd.ben_ad)
--   6. Mədaxil qolunun `vd` alt sorğusunda:
--        · postupl → odb.right(v.kredit,20) və lpad YOXDUR
--        · swift   → regnom 5 simvolla tutuşdurulur (fizikidə 6)
--        · nacval  → odb.mfo join YOXDUR
--   Bu fərqlər QƏSDƏN saxlanılıb — «eyniləşdirmək» BMI nəticəsini dəyişər.
-- ══════════════════════════════════════════════════════════════════════════

with prm as (
    select '41045000001857600000'                   hesab,   -- ← hesab nömrəsi
           to_date('01/08/2026','dd/mm/yyyy')        d1,      -- ← başlama tarixi
           to_date('19/08/2026','dd/mm/yyyy')        d2       -- ← bitmə tarixi
      from dual
)
select x.*
  from (

  -- ══════════════════════════════════════════════════════════════════════
  -- 1) MƏXARİC — hesab DEBET tərəfdədir
  -- ══════════════════════════════════════════════════════════════════════
  select
      t.date_oper                                                    qeb_tarix,     -- A
      t.date_oper                                                    icra_tarix,    -- B
      t.recnum                                                       dax_istinad,   -- C  🆕
      vd.vd_id                                                       xar_istinad,   -- D  🆕
      case when t.debet  in ('10010000000000100000','10020010000000100000',
                             '10020020000000100000','10020030000000100000',
                             '10020040000000100000','10020050000000100000')
             or t.kredit in ('10010000000000100000','10020010000000100000',
                             '10020020000000100000','10020030000000100000',
                             '10020040000000100000','10020050000000100000')
             or substr(t.debet ,1,5) = '25019'
             or substr(t.kredit,1,5) = '25019'
           then 'Nağd' else 'Qeyri-nağd' end                         emel_novu,     -- E  🆕
      '  '                                                           cat_kan,       -- F
      vd.plat_system                                                 odeme_sistemi, -- G  🆕
      '  '                                                           alt_nov,       -- H

      -- ── GÖNDƏRƏN (bizim müştəri) ──────────────────────────────────────
      TRIM(f.name_regnom)                                            gon_ad,        -- I
      f.inn_regnom                                                   gon_voen,      -- J
      f.pincode                                                      gon_fin,       -- K
      case when substr(t.debet,16,1)='9' then 'P/k' else 'Cari' end  gon_hesnov,    -- L
      t.debet                                                        gon_hes,       -- M
      'Bank Melli Iran'                                              gon_bank,      -- N
      'Baki fil. '                                                   gon_fil,       -- O
      '1300036291'                                                   gon_bank_voen, -- P  🆕 (bizik)
      'MELIAZ22'                                                     gon_bank_bic,  -- Q  🆕 (bizik)
      vd.sender_bic                                                  gon_mux_bic,   -- R
      p.countrycode                                                  gon_olke,      -- S
      case t.kod_valuti when '00' then 'AZN' when '01' then 'USD'
                        when '02' then 'EUR' when '03' then 'RUB'
                        when '04' then 'IRR' when '05' then 'AED' end gon_valuta,   -- T
      '   '                                                          gon_pan,       -- U
      '   '                                                          gon_mcc,       -- V

      -- ── ALAN (qarşı tərəf) ────────────────────────────────────────────
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150))
                then TRIM(f.name_regnom)
           else case when t.id_vd is null and substr(t.debet,1,1) in (4,6,7,8,9) then h.name_regnom
                     else vd.ben_ad end end                          alan_ad,       -- W
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150))
                then f.inn_regnom
           else case when t.id_vd is null and substr(t.debet,1,1) in (4,6,7,8,9) then h.inn_regnom
                     else vd.inn_kred end end                        alan_voen,     -- X
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150))
                then f.pincode
           else case when t.id_vd is null and substr(t.debet,1,1) in (4,6,7,8,9) then h.pincode
                     else vd.fin_kredit end end                      alan_fin,      -- Y
      '   '                                                          alan_hesnov,   -- Z
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)<>'4')
                then t.kredit
           else case when t.id_vd is null and substr(t.kredit,1,1)='4' then t.kredit
                     else vd.kredit end end                          alan_hes,      -- AA
      case when t.id_vd is null then 'Bank Melli Iran' else vd.filial_name end alan_bank, -- AB
      case when t.id_vd is null then 'Baki fil. '      else vd.filial_name end alan_fil,  -- AC
      case when t.id_vd is null then '1300036291'
           else vd.alan_bank_voen end                                alan_bank_voen,-- AD 🆕
      case when t.id_vd is null then 'MELIAZ22'
           else nvl(vd.alan_bank_bic2, vd.receiver_bic) end          alan_bank_bic, -- AE 🆕
      vd.receiver_bic                                                alan_mux_bic,  -- AF
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)<>'4')
                then s.countrycode
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)='4')
                     then s.countrycode else '   ' end end           alan_olke,     -- AG
      case t.kod_valuti when '00' then 'AZN' when '01' then 'USD'
                        when '02' then 'EUR' when '03' then 'RUB'
                        when '04' then 'IRR' when '05' then 'AED' end alan_valuta,  -- AH
      '   '                                                          alan_pan,      -- AI
      '   '                                                          alan_mcc,      -- AJ

      0                                                              med_val,       -- AK
      t.summa_v_inval                                                max_val,       -- AL
      t.kod_valuti                                                   val_kod,       -- AM
      0                                                              med_azn,       -- AN
      t.summa_v_nacval                                               max_azn,       -- AO
      t.primechanie                                                  emel,          -- AP
      vd.xeyrine_ad                                                  xeyrine_ad,    -- AQ 🆕
      vd.xeyrine_fin                                                 xeyrine_fin,   -- AR 🆕
      '  '                                                           kommun,        -- AS
      substr(t.debet ,1,5)                                           dt,            -- AT
      substr(t.kredit,1,5)                                           kt,            -- AU
      t.debet dbtam, t.kredit krtam, t.id_vd
  from odb.arh_dd t,
       odb.emitent_benefisiar k,
       odb.regnom f,
       odb.regnom h,
       odb.licsch p,
       odb.licsch s,
       prm,
       ( select z.* from (

              select v.date_oper tarix, v.value_date, r.inn_regnom, r.pincode fin,
                     odb.func_utf8_to_latin(v.account_name) emit_name, v.account_no debet,
                     v.filial_name, v.nomer_docum, v.beneficiary_bank_name ben_bank,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.beneficiary_account kredit,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_ad, v.amount,
                     odb.func_utf8_to_latin(v.comments) cmnt,
                     v.currency valuta, '  ' inn_kred, '  ' fin_kredit,
                     v.sender_bic, v.receiver_bic,
                     v.id                                                        vd_id,
                     v.plat_system                                               plat_system,
                     '1300036291'                                                gon_bank_voen,
                     (select max(m.voen) from odb.muxbir_hesab m
                       where m.swift_kodu = v.receiver_bic)                      alan_bank_voen,
                     'MELIAZ22'                                                  gon_bank_bic2,
                     v.receiver_bic                                              alan_bank_bic2,
                     cast(null as varchar2(300))                                 xeyrine_ad,
                     cast(null as varchar2(50))                                  xeyrine_fin
                from odb.doc_vnesh_inval v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.account_no,10,6) = r.regnom(+)

              union all
              select v.date_oper tarix, v.date_oper val_tar, r.inn_regnom, r.pincode fin,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     mf.bank_large_name filial_name, v.nomer_docum, v.mfo_credit,
                     odb.func_utf8_to_latin(v.name_credit) ben_name, v.kredit, v.name_credit,
                     v.summa_v_nacval amount, 'Odemeler ' cmnt,
                     '944' valuta, v.inn_credit, '  ' fin_kredit,
                     ' ' sender_bic, ' ' receiver_bic,
                     v.id,
                     v.plat_system,
                     '1300036291',
                     (select max(m.voen)       from odb.muxbir_hesab m where m.kod = v.mfo_credit),
                     'MELIAZ22',
                     (select max(m.swift_kodu) from odb.muxbir_hesab m where m.kod = v.mfo_credit),
                     cast(null as varchar2(300)),
                     cast(null as varchar2(50))
                from odb.doc_vnesh_nacval v, odb.regnom r, odb.mfo mf, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.debet,10,6) = r.regnom
                 and v.mfo_credit = mf.mfo(+)

              union all
              select v.date_oper tarix, v.date_oper val_tar, v.inn_debet, '     ' fin_debet,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     mf.bank_large_name filial_name, v.nomer_docum, v.mfo_kredit,
                     odb.func_utf8_to_latin(v.kredit_name) ben_name, v.kredit, v.kredit_name,
                     v.sum1 amount, 'Daxilolma' cmnt,
                     '944' valuta, r.inn_regnom, r.pincode fin_kred,
                     v.bic_debet, v.bic_kredit,
                     v.id,
                     v.plat_system,
                     nvl((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.bic_debet),
                         (select max(m.voen) from odb.muxbir_hesab m where m.kod       = v.mfo_debet)),
                     '1300036291',
                     v.bic_debet,
                     'MELIAZ22',
                     odb.func_utf8_to_latin(v.kredit_name),
                     v.kredit_inn
                from odb.doc_vnesh_postupl v, odb.regnom r, odb.mfo mf, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.kredit,28,'0'),11),6) = r.regnom
                 and v.mfo_debet = mf.mfo(+)

              union all
              select v.date_oper tarix, v.date_oper val_tar, '  ' inn_deb, '  ' fin_debet,
                     odb.func_utf8_to_latin(v.sender_name) emit_name, v.sender_account debet,
                     v.sender_bank_name filial_name, v.nomer_docum, v.beneficiary_bank_name,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.beneficiary_account,
                     v.beneficiary_name, v.amount, 'Daxilolma' cmnt,
                     v.currency valuta, r.inn_regnom, r.pincode fin_kred,
                     v.sender_bank_bic, v.beneficiary_bank_bic,
                     v.id,
                     v.plat_system,
                     (select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.sender_bank_bic),
                     '1300036291',
                     v.sender_bank_bic,
                     'MELIAZ22',
                     v.beneficiary_bank_bic,
                     cast(null as varchar2(50))
                from odb.doc_vnesh_swift v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.beneficiary_account,28,'0'),11),6) = r.regnom
              ) z, prm
          where z.debet  like '%'||prm.hesab||'%'
             or z.kredit like '%'||prm.hesab||'%'
          order by z.tarix, z.cmnt
       ) vd
  where t.recnum = k.doc_id(+)
    and t.date_oper between prm.d1 and prm.d2
    and t.debet = prm.hesab
    and substr(t.debet ,10,6) = f.regnom(+)
    and substr(t.kredit,10,6) = h.regnom(+)
    and t.debet  = p.licsch(+)
    and t.kredit = s.licsch(+)
    and t.date_oper       = vd.tarix(+)
    and t.debet           = vd.debet(+)
    and t.summa_v_nacval  = vd.amount(+)
    and (t.vid_operacii <> 97 or t.vid_operacii is null)

  UNION ALL

  -- ══════════════════════════════════════════════════════════════════════
  -- 2) MƏDAXİL — hesab KREDİT tərəfdədir
  -- ══════════════════════════════════════════════════════════════════════
  select
      t.date_oper                                                    qeb_tarix,     -- A
      t.date_oper                                                    icra_tarix,    -- B
      t.recnum                                                       dax_istinad,   -- C  🆕
      vd.vd_id                                                       xar_istinad,   -- D  🆕
      case when t.debet  in ('10010000000000100000','10020010000000100000',
                             '10020020000000100000','10020030000000100000',
                             '10020040000000100000','10020050000000100000')
             or t.kredit in ('10010000000000100000','10020010000000100000',
                             '10020020000000100000','10020030000000100000',
                             '10020040000000100000','10020050000000100000')
             or substr(t.debet ,1,5) = '25019'
             or substr(t.kredit,1,5) = '25019'
           then 'Nağd' else 'Qeyri-nağd' end                         emel_novu,     -- E  🆕
      '  '                                                           cat_kan,       -- F
      vd.plat_system                                                 odeme_sistemi, -- G  🆕
      '  '                                                           alt_nov,       -- H

      -- ── GÖNDƏRƏN (qarşı tərəf) ────────────────────────────────────────
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150))
                then h.name_regnom
           else case when t.id_vd is null and substr(t.debet,1,1) in (4,6,7,8,9) then h.name_regnom
                     else vd.emit_name end end                       gon_ad,        -- I
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150))
                then h.inn_regnom
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,1) in (4,6,7,8,9))
                     then f.inn_regnom else vd.inn_regnom end end    gon_voen,      -- J
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150))
                then h.pincode
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,1) in (4,6,7,8,9))
                     then h.pincode else vd.fin end end              gon_fin,       -- K
      '   '                                                          gon_hesnov,    -- L
      case when t.id_vd is null then t.debet else trim(vd.debet) end gon_hes,       -- M
      case when t.id_vd is null then 'Bank Melli Iran' else vd.filial_name end gon_bank, -- N
      case when t.id_vd is null then 'Baki fil. '      else vd.filial_name end gon_fil,  -- O
      case when t.id_vd is null then '1300036291'
           else vd.gon_bank_voen end                                 gon_bank_voen, -- P  🆕
      case when t.id_vd is null then 'MELIAZ22'
           else nvl(vd.gon_bank_bic2, vd.sender_bic) end             gon_bank_bic,  -- Q  🆕
      vd.sender_bic                                                  gon_mux_bic,   -- R
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150))
                then p.countrycode
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,1) in (4,6,7,8,9))
                     then s.countrycode else '   ' end end           gon_olke,      -- S
      case t.kod_valuti when '00' then 'AZN' when '01' then 'USD'
                        when '02' then 'EUR' when '03' then 'RUB'
                        when '04' then 'IRR' when '05' then 'AED' end gon_valuta,   -- T
      '   '                                                          gon_pan,       -- U
      '   '                                                          gon_mcc,       -- V

      -- ── ALAN (bizim müştəri) ──────────────────────────────────────────
      TRIM(f.name_regnom)                                            alan_ad,       -- W
      f.inn_regnom                                                   alan_voen,     -- X
      f.pincode                                                      alan_fin,      -- Y
      case when substr(t.kredit,16,1)='9' then 'P/k' else 'Cari' end alan_hesnov,   -- Z
      t.kredit                                                       alan_hes,      -- AA
      'Bank Melli Iran'                                              alan_bank,     -- AB
      'Baki fil. '                                                   alan_fil,      -- AC
      '1300036291'                                                   alan_bank_voen,-- AD 🆕 (bizik)
      'MELIAZ22'                                                     alan_bank_bic, -- AE 🆕 (bizik)
      vd.receiver_bic                                                alan_mux_bic,  -- AF
      p.countrycode                                                  alan_olke,     -- AG
      case t.kod_valuti when '00' then 'AZN' when '01' then 'USD'
                        when '02' then 'EUR' when '03' then 'RUB'
                        when '04' then 'IRR' when '05' then 'AED' end alan_valuta,  -- AH
      '   '                                                          alan_pan,      -- AI
      '   '                                                          alan_mcc,      -- AJ

      t.summa_v_inval                                                med_val,       -- AK
      0                                                              max_val,       -- AL
      t.kod_valuti                                                   val_kod,       -- AM
      t.summa_v_nacval                                               med_azn,       -- AN
      0                                                              max_azn,       -- AO
      t.primechanie                                                  emel,          -- AP
      vd.xeyrine_ad                                                  xeyrine_ad,    -- AQ 🆕
      vd.xeyrine_fin                                                 xeyrine_fin,   -- AR 🆕
      '  '                                                           kommun,        -- AS
      substr(t.debet ,1,5)                                           dt,            -- AT
      substr(t.kredit,1,5)                                           kt,            -- AU
      t.debet dbtam, t.kredit krtam, t.id_vd
  from odb.arh_dd t,
       odb.emitent_benefisiar k,
       odb.regnom f,
       odb.regnom h,
       odb.licsch p,
       odb.licsch s,
       prm,
       ( select z.* from (

              select v.date_oper tarix, v.value_date, r.inn_regnom, r.pincode fin,
                     odb.func_utf8_to_latin(v.account_name) emit_name, v.account_no debet,
                     v.filial_name, v.nomer_docum, v.beneficiary_bank_name ben_bank,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.beneficiary_account kredit,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_ad, v.amount,
                     odb.func_utf8_to_latin(v.comments) cmnt,
                     v.currency valuta, '  ' inn_kred, '  ' fin_kredit,
                     v.sender_bic, v.receiver_bic,
                     v.id                                                        vd_id,
                     v.plat_system                                               plat_system,
                     '1300036291'                                                gon_bank_voen,
                     (select max(m.voen) from odb.muxbir_hesab m
                       where m.swift_kodu = v.receiver_bic)                      alan_bank_voen,
                     'MELIAZ22'                                                  gon_bank_bic2,
                     v.receiver_bic                                              alan_bank_bic2,
                     cast(null as varchar2(300))                                 xeyrine_ad,
                     cast(null as varchar2(50))                                  xeyrine_fin
                from odb.doc_vnesh_inval v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.account_no,10,6) = r.regnom

              union all
              select v.date_oper tarix, v.date_oper val_tar, r.inn_regnom, r.pincode fin,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     v.mfo_debet filial_name, v.nomer_docum, v.mfo_credit,
                     odb.func_utf8_to_latin(v.name_credit) ben_name, v.kredit, v.name_credit,
                     v.summa_v_nacval amount, 'Odemeler ' cmnt,
                     '944' valuta, v.inn_credit, '  ' fin_kredit,
                     ' ' sender_bic, ' ' receiver_bic,
                     v.id,
                     v.plat_system,
                     '1300036291',
                     (select max(m.voen)       from odb.muxbir_hesab m where m.kod = v.mfo_credit),
                     'MELIAZ22',
                     (select max(m.swift_kodu) from odb.muxbir_hesab m where m.kod = v.mfo_credit),
                     cast(null as varchar2(300)),
                     cast(null as varchar2(50))
                from odb.doc_vnesh_nacval v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.debet,10,6) = r.regnom

              union all
              -- ⚠️ hüquqi variantda lpad YOXDUR və kredit odb.right(...,20) ilə kəsilir
              select v.date_oper tarix, v.date_oper val_tar, v.inn_debet, '     ' fin_debet,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     mf.bank_large_name filial_name, v.nomer_docum, v.mfo_kredit,
                     odb.func_utf8_to_latin(v.kredit_name) ben_name, odb.right(v.kredit,20) kredit,
                     v.kredit_name, v.sum1 amount, 'Daxilolma' cmnt,
                     '944' valuta, r.inn_regnom, r.pincode fin_kred,
                     v.bic_debet, v.bic_kredit,
                     v.id,
                     v.plat_system,
                     nvl((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.bic_debet),
                         (select max(m.voen) from odb.muxbir_hesab m where m.kod       = v.mfo_debet)),
                     '1300036291',
                     v.bic_debet,
                     'MELIAZ22',
                     odb.func_utf8_to_latin(v.kredit_name),
                     v.kredit_inn
                from odb.doc_vnesh_postupl v, odb.regnom r, odb.mfo mf, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(v.kredit,11),6) = r.regnom
                 and v.mfo_debet = mf.mfo(+)

              union all
              -- ⚠️ hüquqi variantda regnom 5 simvolla tutuşdurulur (fizikidə 6)
              select v.date_oper tarix, v.date_oper val_tar, '  ' inn_deb, '  ' fin_debet,
                     odb.func_utf8_to_latin(v.sender_name) emit_name, v.sender_account debet,
                     v.sender_bank_name filial_name, v.nomer_docum, v.beneficiary_bank_name,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.beneficiary_account,
                     v.beneficiary_name, v.amount, 'Daxilolma' cmnt,
                     v.currency valuta, r.inn_regnom, r.pincode fin_kred,
                     v.sender_bank_bic, v.beneficiary_bank_bic,
                     v.id,
                     v.plat_system,
                     (select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.sender_bank_bic),
                     '1300036291',
                     v.sender_bank_bic,
                     'MELIAZ22',
                     v.beneficiary_bank_bic,
                     cast(null as varchar2(50))
                from odb.doc_vnesh_swift v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.beneficiary_account,28,'0'),11),5) = r.regnom
              ) z, prm
          where z.debet  like '%'||prm.hesab||'%'
             or z.kredit like '%'||prm.hesab||'%'
          order by z.tarix, z.cmnt
       ) vd
  where t.recnum = k.doc_id(+)
    and t.date_oper between prm.d1 and prm.d2
    and t.kredit = prm.hesab
    and substr(t.kredit,10,6) = f.regnom(+)
    and substr(t.debet ,10,6) = h.regnom(+)
    and t.kredit = p.licsch(+)
    and t.debet  = s.licsch(+)
    and t.date_oper       = vd.tarix(+)
    and t.kredit          = vd.kredit(+)
    and t.summa_v_nacval  = vd.amount(+)
    and (t.vid_operacii <> 97 or t.vid_operacii is null)

  ) x
 order by x.qeb_tarix asc
