/* ============================================================================
   AML — «Öz xeyrinə» (AQ/AR) HÖP əməliyyatında doldurulur
   ----------------------------------------------------------------------------
   NƏ EDİR: `OracleSorgular`-dakı İKİ mövcud sətrin `SorguMetni`-ni YENİLƏYİR
   (`AML_HESAB_SORGU_FIZIKI`, `AML_HESAB_SORGU_HUQUQI`).

   ⚠️ `90_AML_OracleSorgular.sql` idempotent INSERT-dir — sətir varsa HEÇ NƏ
   ETMİR. Ona görə artıq quraşdırılmış bazada YALNIZ BU skript işləyir.
   (Sıfırdan quran üçün 90-cı skript də yenilənib — mətnlər eynidir.)

   QAYDA (istifadəçi, 20.08.2026): AQ («Xeyrinə ödənilən şəxs/müəssisə») və
   AR («FİN/VÖEN») YALNIZ müştəri hesabından **45013000000000400009**
   hesabına keçən əməliyyatda dolur — HÖP. Mənbə `arh_dd.primechanie`:

       VÖEN-AVANS: 1604964601 - Dövlət Gömrük Komitəsi (MOUSAVİAN ...)
                   |__ AR __|   |________ AQ _________|

   Mətn OLDUĞU KİMİ götürülür, şərh edilmir. Mötərizədəki şəxs AQ-ya DÜŞMÜR —
   o, Təyinat (AP) sütununda onsuz da görünür.

   NƏ DƏYİŞMİR: `doc_vnesh_postupl` / `doc_vnesh_swift`-dən gələn mövcud
   dəyərlər HEÇ VAXT üstələnmir; kredit hesabı uyğun gəlmirsə və ya şablon
   tapılmırsa sütun bu günkü kimi boş qalır. Dəyişiklik yalnız ƏLAVƏ EDİR.

   İSTİNAD SƏTİR (yoxlama üçün): 06.04.2026, debet 40060000001979100000,
   kredit 45013000000000400009, üç sətir — 37024 / 210,4 / 38,2.

   Oracle YALNIZ SELECT (CLAUDE.md) — bu skript SQL Server-dədir.
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

/* -- Ne deyisecek — EVVELCE BUNA BAX ------------------------------------- */
SELECT SorguAdi,
       LEN(SorguMetni)                                        AS KohneUzunluq,
       CASE WHEN SorguMetni LIKE N'%hop_voen%'
            THEN N'artiq yenilenib' ELSE N'yenilenecek' END   AS Veziyyet
  FROM OracleSorgular
 WHERE SorguAdi IN (N'AML_HESAB_SORGU_FIZIKI', N'AML_HESAB_SORGU_HUQUQI')
   AND ISNULL(Silinib,0) = 0;

/* -- AML_HESAB_SORGU_FIZIKI -------------------------------------- */
UPDATE OracleSorgular
   SET SorguMetni = N'with prm as (
    select ''{HESAB}''                                  hesab,
           to_date(''{TARIX1}'',''dd/mm/yyyy'')           d1,
           to_date(''{TARIX2}'',''dd/mm/yyyy'')           d2
      from dual
)
select
    -- ══════════════════════════════════════════════════════════════════════
    -- ÜST QAT — BMI-də bunlar Excel-ə yazılmazdan ƏVVƏL C#-da (exceleat2)
    -- dataGridView xanalarının üzərinə yazılırdı. İndi SQL-in içindədir ki,
    -- FinNex tərəfdə heç bir «grid düzəlişi» lazım olmasın.
    -- ══════════════════════════════════════════════════════════════════════
    x.qeb_tarix,                                                        -- A
    x.icra_tarix,                                                       -- B
    x.dax_istinad,                                                      -- C
    x.xar_istinad,                                                      -- D
    x.emel_novu,                                                        -- E
    -- F  Çatdırılma kanalı (C#: Cells[3]). C#-da hər `if` bir öncəkini
    --    ÜSTƏLƏYİRDİ → burada sıra QƏSDƏN TƏRSİNƏDİR (CASE ilk uyğunu götürür).
    case when x.dbtam = ''25010000000000300000''
              or x.krtam in (''25020010000000300002'',''25020020000000300002'')
              then ''POS terminal''
         when substr(x.dbtam,1,5) = ''25019''
              then ''Ödəniş terminalı''
         when substr(x.dbtam,1,5) in (''10010'',''10020'')
              or substr(x.krtam,1,5) in (''10010'',''10020'')
              then ''CAS''
         when substr(x.dbtam,1,5) in (''35025'',''35020'',''15025'',''15020'')
              or substr(x.krtam,1,5) in (''35025'',''35020'',''15025'',''15020'')
              then ''Digər maliyyə institutu vasitəsi''
         when x.dbtam in (''11010000020000200000'',''11010000030000200000'',''11010000050000200000'')
              or x.krtam in (''11010000020000200000'',''11010000030000200000'',''11010000050000200000'')
              then ''Digər maliyyə institutu vasitəsi''
         when x.krtam = ''25010000000000300000''
              then ''ATM''
    end                                                                 cat_kan,      -- F
    -- G  Ödəniş sisteminin növü — kod → ad.
    --    Xəritənin mənbəyi: ODB.PROC_IPS_INS_VNESH_POSTUPL, 48-ci sətir:
    --      «p_PLAT_SYSTEM >>> 0 = XOHKS, 1 = SWIFT, 2 = INTERNAL,
    --                         3 = APUS, 4 = V-Shape, 5 = HOP, 6 = IPS»
    --    Təsdiq: PCG_IPS → `c_ips_plat_system_id CONSTANT INTEGER := 6`
    --            TRG_CHECK_I_DOC_VNESH_NACVAL → «0=XOHKS, 1=SWIFT, 2=INTERNAL»
    --    Bazada LÜĞƏT CƏDVƏLİ YOXDUR — xəritə yalnız PL/SQL mənbəyində var,
    --    ona görə burada saxlanılır. Yeni sistem əlavə olunsa bu CASE yenilənir.
    --
    --    ⚠️ XARİCİ SƏNƏDLƏR BURAYA 0 GÖNDƏRMİR — `vd` alt sorğusunda
    --    `doc_vnesh_inval` və `doc_vnesh_swift` qolları sabit `''1''` (SWIFT)
    --    verir, çünki o iki cədvəldə `PLAT_SYSTEM` heç vaxt doldurulmayıb
    --    (404/404 və 82/82 sətir 0). Ətraflı izah həmin qolların yanındadır.
    --    Yəni buradakı `when ''0'' then ''XÖHKS''` yalnız DAXİLİ sənədlərə
    --    (nacval / postupl) aiddir — orada 0 real XÖHKS deməkdir.
    case x.odeme_sistemi
         when ''0'' then ''XÖHKS''
         when ''1'' then ''SWIFT''
         when ''2'' then ''Bank daxili''
         when ''3'' then ''APUS''
         when ''4'' then ''V-Shape''
         when ''5'' then ''HOP''
         when ''6'' then ''IPS''
         else x.odeme_sistemi
    end                                                                 odeme_sistemi, -- G
    x.alt_nov,                                                          -- H

    -- I  Göndərənin adı (C#: Cells[4]) — hesabın `licsch` adı üstələyir.
    --    İki xüsusi hesabın adı sabitdir və lookup-dan SONRA tətbiq olunur
    --    (C#-da `j` dövrü 37 dəfə işlədiyi üçün sabit ad axırda qalırdı).
    case when x.gon_hes = ''25052000040000300000'' then ''İŞÇİLƏRƏ AVANS MÜKAFAT''
         when x.gon_hes = ''25019000000000300006'' then ''Ödəniş terminalı''
         else nvl(x.gon_lics_ad, x.gon_ad) end                          gon_ad,       -- I
    x.gon_voen,                                                         -- J
    x.gon_fin,                                                          -- K
    x.gon_hesnov,                                                       -- L
    x.gon_hes,                                                          -- M
    x.gon_bank,                                                         -- N
    x.gon_fil,                                                          -- O
    x.gon_bank_voen,                                                    -- P
    x.gon_bank_bic,                                                     -- Q
    x.gon_mux_bic,                                                      -- R
    -- S  Ödəniş terminalı hesabında ölkə sabit AZE (C#: Cells[11])
    case when x.gon_hes = ''25019000000000300006'' then ''AZE''
         else x.gon_olke end                                            gon_olke,     -- S
    x.gon_valuta,                                                       -- T
    x.gon_pan,                                                          -- U
    x.gon_mcc,                                                          -- V

    -- W/X/Y  Alan tərəf — `licsch` + `regnom` axtarışı üstələyir
    --        (C#: Cells[16]/[17]/[18]). Üçü də YALNIZ ad tapılanda yazılır.
    nvl(x.alan_lics_ad, x.alan_ad)                                      alan_ad,      -- W
    case when x.alan_lics_ad is not null then x.alan_lics_voen
         else x.alan_voen end                                           alan_voen,    -- X
    case when x.alan_lics_ad is not null then x.alan_lics_fin
         else x.alan_fin end                                            alan_fin,     -- Y
    -- Z  Hesab növü (C#: Cells[19]). DİQQƏT — BMI burada həm alanın, həm
    --    GÖNDƏRƏNİN hesabına baxır və nəticəni ALAN sütununa yazır.
    --    Səhv görünür, amma olduğu kimi saxlanılıb (BMI ilə üzləşdirmə üçün).
    case when substr(x.alan_hes,1,2) in (''40'',''41'',''38'',''39'')
              or substr(x.gon_hes ,1,2) in (''40'',''41'',''38'',''39'') then ''Cari''
         else x.alan_hesnov end                                         alan_hesnov,  -- Z
    x.alan_hes,                                                         -- AA
    x.alan_bank,                                                        -- AB
    x.alan_fil,                                                         -- AC
    x.alan_bank_voen,                                                   -- AD
    x.alan_bank_bic,                                                    -- AE
    x.alan_mux_bic,                                                     -- AF
    x.alan_olke,                                                        -- AG
    x.alan_valuta,                                                      -- AH
    x.alan_pan,                                                         -- AI
    x.alan_mcc,                                                         -- AJ
    x.med_val,                                                          -- AK
    x.max_val,                                                          -- AL
    -- AM Valyuta kodu — BMI `kod_valuti`-ni yazıb sonra ÜSTÜNDƏN alan tərəfin
    --    valyuta mətnini yazırdı (exceleat2 sonu: Cells[24] → 31-ci sütun).
    x.alan_valuta                                                       val_kod,      -- AM
    x.med_azn,                                                          -- AN
    x.max_azn,                                                          -- AO
    x.emel,                                                             -- AP
    -- AQ/AR — mövcud struktur mənbə (doc_vnesh_*) ÜSTÜNDÜR; HÖP mətnindən
    -- oxuma yalnız HƏR İKİSİ boş olanda işə düşür. «Hər ikisi» şərti
    -- QƏSDƏNDİR: SWIFT sətrində ad var (BIC), VÖEN yoxdur — adı bir
    -- mənbədən, VÖEN-i başqasından götürsək sətir uydurma cütlük olardı.
    case when x.xeyrine_ad is null and x.xeyrine_fin is null
         then x.hop_ad   else x.xeyrine_ad  end                         xeyrine_ad,   -- AQ
    case when x.xeyrine_ad is null and x.xeyrine_fin is null
         then x.hop_voen else x.xeyrine_fin end                         xeyrine_fin,  -- AR
    x.kommun,                                                           -- AS
    x.dt,                                                               -- AT
    x.kt                                                                -- AU
  from (
    select k.*,
           -- BMI-dəki `Hes_adlari` sorğusunun eynisi, sətir-sətir axtarış əvəzinə
           -- skalyar alt sorğu ilə. `substr(r.regnom, 2.5)` BMI-də yazı səhvidir —
           -- Oracle 2.5-i 2-yə kəsir, yəni `substr(regnom,2)`.
           (select max(l.name_licsch) from odb.licsch l
             where l.licsch = k.gon_hes
               and l.date_close_licsch is null)                         gon_lics_ad,
           (select max(l.name_licsch) from odb.licsch l
             where l.licsch = k.alan_hes
               and l.date_close_licsch is null)                         alan_lics_ad,
           (select max(case when r.fizik = 1 then null
                            else to_char(l.inn_licsch) end)
              from odb.licsch l, odb.regnom r
             where l.licsch = k.alan_hes
               and l.date_close_licsch is null
               and substr(l.licsch,11,5) = substr(r.regnom,2))          alan_lics_voen,
           (select max(r.pincode)
              from odb.licsch l, odb.regnom r
             where l.licsch = k.alan_hes
               and l.date_close_licsch is null
               and substr(l.licsch,11,5) = substr(r.regnom,2))          alan_lics_fin,
           -- ── «Öz xeyrinə» (AQ/AR) — YALNIZ HÖP ƏMƏLİYYATINDA ─────────────
           -- İstifadəçi qaydası (20.08.2026): sütunlar yalnız müştəri
           -- hesabından **45013000000000400009** hesabına keçən əməliyyatda
           -- dolur (debet = müştəri hesabı, kredit = həmin hesab). Başqa
           -- sətirlərdə toxunulmur.
           --
           -- Mətn `arh_dd.primechanie`-dədir və OLDUĞU KİMİ götürülür —
           -- şərh edilmir («bizim öz yanaşmamız lazım deyil»):
           --   VÖEN-AVANS: 1604964601 - Dövlət Gömrük Komitəsi (MOUSAVİAN ...)
           --               └── AR ──┘   └────────── AQ ────────┘
           -- Mötərizədəki şəxs AQ-ya DÜŞMÜR — o, Təyinat (AP) sütununda
           -- onsuz da görünür.
           --
           -- Şablon «VÖEN» sözünə bağlıdır, sərbəst «10 rəqəm» axtarışı DEYİL:
           -- təyinatda müqavilə/faktura nömrəsi də 10 rəqəmli ola bilər və
           -- etiketsiz axtarış onu səhvən VÖEN kimi yazardı.
           -- `V[^0-9]{0,3}EN` həm «VÖEN», həm «VOEN» yazılışını tutur.
           -- Xam mətndə Azərbaycan hərfləri düzgün oxunur (06.04.2026 sətri
           -- ilə yoxlanılıb) — `func_utf8_to_latin` burada LAZIM DEYİL.
           case when k.krtam = ''45013000000000400009''
                then regexp_substr(k.emel,
                       ''V[^0-9]{0,3}EN[^0-9]{0,20}([0-9]{10})'', 1, 1, null, 1)
           end                                                          hop_voen,
           case when k.krtam = ''45013000000000400009''
                then trim(regexp_substr(
                       regexp_replace(
                           regexp_substr(k.emel,
                             ''V[^0-9]{0,3}EN[^0-9]{0,20}[0-9]{10}(.*)'', 1, 1, null, 1),
                           ''^[ .,:;-]+'', ''''),
                       ''^[^(]*''))
           end                                                          hop_ad
      from (

  -- ══════════════════════════════════════════════════════════════════════
  -- 1) MƏXARİC — hesab DEBET tərəfdədir (bizim müştəri göndərəndir)
  -- ══════════════════════════════════════════════════════════════════════
  select
      t.date_oper                                                    qeb_tarix,     -- A  İcraya qəbul
      to_char(t.date_oper,''dd-mm-yyyy'')                              icra_tarix,    -- B  Faktiki icra
      t.recnum                                                       dax_istinad,   -- C  Daxili istinad   🆕 (arh_dd.RECNUM)
      vd.vd_id                                                       xar_istinad,   -- D  Xarici istinad   🆕 (doc_vnesh_*.ID)
      case when t.debet  in (''10010000000000100000'',''10020010000000100000'',
                             ''10020020000000100000'',''10020030000000100000'',
                             ''10020040000000100000'',''10020050000000100000'')
             or t.kredit in (''10010000000000100000'',''10020010000000100000'',
                             ''10020020000000100000'',''10020030000000100000'',
                             ''10020040000000100000'',''10020050000000100000'')
             or substr(t.debet ,1,5) = ''25019''
             or substr(t.kredit,1,5) = ''25019''
           then ''Nağd'' else ''Qeyri-nağd'' end                         emel_novu,     -- E  Əməliyyatın növü 🆕
      ''  ''                                                           cat_kan,       -- F  Çatdırılma kanalı (C#-da doldurulur)
      vd.plat_system                                                 odeme_sistemi, -- G  Ödəniş sisteminin növü 🆕
      ''  ''                                                           alt_nov,       -- H  Alt növü (qərar: BOŞ)

      -- ── GÖNDƏRƏN (bizim müştəri) ──────────────────────────────────────
      TRIM(f.name_regnom)                                            gon_ad,        -- I
      ''   ''                                                          gon_voen,      -- J
      f.pincode                                                      gon_fin,       -- K
      case when substr(t.debet,16,1)=''9'' then ''P/k'' else ''Cari'' end  gon_hesnov,    -- L
      t.debet                                                        gon_hes,       -- M
      ''Bank Melli Iran''                                              gon_bank,      -- N
      ''Baki fil. ''                                                   gon_fil,       -- O
      ''1300036291''                                                   gon_bank_voen, -- P  Bankın VÖEN-i 🆕 (bizik)
      ''MELIAZ22''                                                     gon_bank_bic,  -- Q  Bankın BİC-i  🆕 (bizik)
      vd.sender_bic                                                  gon_mux_bic,   -- R  Müxbir bankın BİC-i
      p.countrycode                                                  gon_olke,      -- S
      case substr(t.debet,6,2) when ''00'' then ''AZN'' when ''01'' then ''USD''
                             when ''02'' then ''EUR'' when ''03'' then ''RUB''
                             when ''04'' then ''IRR'' when ''05'' then ''AED'' end gon_valuta,   -- T  (hesabın 6-7-ci simvolu)
      ''   ''                                                          gon_pan,       -- U
      ''   ''                                                          gon_mcc,       -- V

      -- ── ALAN (qarşı tərəf) ────────────────────────────────────────────
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)<>''4'')
                then TRIM(f.name_regnom)
           else case when t.id_vd is null and substr(t.kredit,1,1)=''4'' then h.name_regnom
                     else vd.ben_ad end end                          alan_ad,       -- W
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)<>''4'')
                then ''    ''
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)=''4'')
                     then h.inn_regnom else vd.inn_kred end end      alan_voen,     -- X
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)<>''4'')
                then f.pincode
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)=''4'')
                     then h.pincode else vd.fin_kredit end end       alan_fin,      -- Y
      ''   ''                                                          alan_hesnov,   -- Z
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)<>''4'')
                then t.kredit
           else case when t.id_vd is null and substr(t.kredit,1,1)=''4'' then t.kredit
                     else vd.kredit end end                          alan_hes,      -- AA
      case when t.id_vd is null then ''Bank Melli Iran'' else vd.ben_bank end   alan_bank, -- AB
      case when t.id_vd is null then ''Baki fil. ''      else ''diger''  end      alan_fil,  -- AC
      case when t.id_vd is null then ''1300036291''
           else vd.alan_bank_voen end                                alan_bank_voen,-- AD  Bankın VÖEN-i 🆕
      case when t.id_vd is null then ''MELIAZ22''
           else nvl(vd.alan_bank_bic2, vd.receiver_bic) end          alan_bank_bic, -- AE  Bankın BİC-i  🆕
      vd.receiver_bic                                                alan_mux_bic,  -- AF  Müxbir bankın BİC-i
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)<>''4'')
                then s.countrycode
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)=''4'')
                     then s.countrycode else ''   '' end end           alan_olke,     -- AG
      case t.kod_valuti when 0 then ''AZN'' when 1 then ''USD''
                        when 2 then ''EUR'' when 3 then ''RUB''
                        when 4 then ''IRR'' when 5 then ''AED'' end alan_valuta,  -- AH (əməliyyatın valyutası)
      ''   ''                                                          alan_pan,      -- AI
      ''   ''                                                          alan_mcc,      -- AJ

      -- ── Məbləğlər və bank hissəsi ─────────────────────────────────────
      0                                                              med_val,       -- AK Mədaxil
      t.summa_v_inval                                                max_val,       -- AL Məxaric
      t.kod_valuti                                                   val_kod,       -- AM
      0                                                              med_azn,       -- AN Mədaxil (AZN)
      t.summa_v_nacval                                               max_azn,       -- AO Məxaric (AZN)
      t.primechanie                                                  emel,          -- AP Təyinat
      vd.xeyrine_ad                                                  xeyrine_ad,    -- AQ 🆕
      vd.xeyrine_fin                                                 xeyrine_fin,   -- AR 🆕
      ''  ''                                                           kommun,        -- AS
      substr(t.debet ,1,5)                                           dt,            -- AT
      substr(t.kredit,1,5)                                           kt,            -- AU
      -- köməkçi sütunlar (Excel-ə YAZILMIR — C#-da kanal təyini üçün)
      t.debet dbtam, t.kredit krtam, t.id_vd
  from odb.arh_dd t,
       odb.emitent_benefisiar k,
       odb.regnom f,
       odb.regnom h,
       odb.licsch p,
       odb.licsch s,
       prm,
       ( select z.* from (

              -- xarici valyutada ödəmə
              select v.date_oper tarix, v.value_date, r.inn_regnom, r.pincode fin,
                     odb.func_utf8_to_latin(v.account_name) emit_name, v.account_no debet,
                     v.filial_name, v.nomer_docum, v.beneficiary_bank_name ben_bank,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.beneficiary_account kredit,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_ad, v.amount,
                     odb.func_utf8_to_latin(v.comments) cmnt,
                     v.currency valuta, ''  '' inn_kred, ''  '' fin_kredit,
                     v.sender_bic, v.receiver_bic,
                     -- 🆕
                     to_char(v.id)                                               vd_id,
                     -- ⚠️ XARİCİ SƏNƏD — PLAT_SYSTEM burada DOLDURULMUR.
                     --    `doc_vnesh_inval` (404/404) və `doc_vnesh_swift` (82/82)
                     --    sətirlərinin HAMISINDA dəyər 0-dır. Lüğətdə 0 «XÖHKS»
                     --    deməkdir, XÖHKS isə MANAT sistemidir — valyuta ödənişi
                     --    oradan keçə bilməz, yəni 0 burada «XÖHKS» yox, «BOŞ» deməkdir.
                     --    (BMI-nin öz `TRG_CHECK_I_DOC_VNESH_INVAL` triggeri məhz
                     --     `plat_system = 1` halını yoxlayır — deməli 1 gözlənilib,
                     --     amma real datada bir dənə də 1 yoxdur.)
                     --    Bu iki cədvəlin ÖZÜ SWIFT axınıdır, ona görə sahəyə
                     --    baxmırıq: sabit SWIFT kodu (1) veririk — üst qatdakı CASE
                     --    onu «SWIFT» kimi yazacaq. (İstifadəçi qərarı: variant B.)
                     ''1''                                                          plat_system,
                     ''1300036291''                                                gon_bank_voen,
                     to_char((select max(m.voen) from odb.muxbir_hesab m
                               where m.swift_kodu = v.receiver_bic))            alan_bank_voen,
                     ''MELIAZ22''                                                  gon_bank_bic2,
                     v.receiver_bic                                              alan_bank_bic2,
                     cast(null as varchar2(300))                                 xeyrine_ad,
                     cast(null as varchar2(50))                                  xeyrine_fin
                from odb.doc_vnesh_inval v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.account_no,10,6) = r.regnom(+)

              union all
              -- milli valyutada ödəmə
              select v.date_oper tarix, v.date_oper val_tar, r.inn_regnom, r.pincode fin,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     mf.bank_large_name filial_name, v.nomer_docum,
                     nvl((select max(m2.bank_large_name) from odb.mfo m2
                           where m2.mfo = v.mfo_credit), to_char(v.mfo_credit)) ben_bank,
                     odb.func_utf8_to_latin(v.name_credit) ben_name, v.kredit, v.name_credit,
                     v.summa_v_nacval amount, ''Odemeler '' cmnt,
                     ''944'' valuta, v.inn_credit, ''  '' fin_kredit,
                     '' '' sender_bic, '' '' receiver_bic,
                     -- 🆕
                     to_char(v.id),
                     to_char(v.plat_system),
                     ''1300036291'',
                     to_char((select max(m.voen) from odb.muxbir_hesab m where m.kod = v.mfo_credit)),
                     ''MELIAZ22'',
                     to_char((select max(m.swift_kodu) from odb.muxbir_hesab m where m.kod = v.mfo_credit)),
                     cast(null as varchar2(300)),
                     cast(null as varchar2(50))
                from odb.doc_vnesh_nacval v, odb.regnom r, odb.mfo mf, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.debet,10,6) = r.regnom
                 and v.mfo_credit = mf.mfo(+)

              union all
              -- milli valyutada mədaxil
              select v.date_oper tarix, v.date_oper val_tar, v.inn_debet, ''     '' fin_debet,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     mf.bank_large_name filial_name, v.nomer_docum,
                     nvl((select max(m2.bank_large_name) from odb.mfo m2
                           where m2.mfo = v.mfo_kredit), to_char(v.mfo_kredit)) ben_bank,
                     odb.func_utf8_to_latin(v.kredit_name) ben_name, v.kredit, v.kredit_name,
                     v.sum1 amount, ''Daxilolma'' cmnt,
                     ''944'' valuta, r.inn_regnom, r.pincode fin_kred,
                     v.bic_debet, v.bic_kredit,
                     -- 🆕
                     to_char(v.id),
                     to_char(v.plat_system),
                     to_char(nvl((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.bic_debet),
                                 (select max(m.voen) from odb.muxbir_hesab m where m.kod       = v.mfo_debet))),
                     ''1300036291'',
                     v.bic_debet,
                     ''MELIAZ22'',
                     odb.func_utf8_to_latin(v.kredit_name),
                     to_char(v.kredit_inn)
                from odb.doc_vnesh_postupl v, odb.regnom r, odb.mfo mf, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.kredit,28,''0''),11),6) = r.regnom
                 and v.mfo_debet = mf.mfo(+)

              union all
              -- xarici valyutada mədaxil (SWIFT)
              select v.date_oper tarix, v.date_oper val_tar, ''  '' inn_deb, ''  '' fin_debet,
                     odb.func_utf8_to_latin(v.sender_name) emit_name, v.sender_account debet,
                     v.sender_bank_name filial_name, v.nomer_docum, v.beneficiary_bank_name,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.beneficiary_account,
                     v.beneficiary_name, v.amount, ''Daxilolma'' cmnt,
                     v.currency valuta, r.inn_regnom, r.pincode fin_kred,
                     v.sender_bank_bic, v.beneficiary_bank_bic,
                     -- 🆕
                     to_char(v.id),
                     -- ⚠️ XARİCİ SƏNƏD — PLAT_SYSTEM burada DOLDURULMUR.
                     --    `doc_vnesh_inval` (404/404) və `doc_vnesh_swift` (82/82)
                     --    sətirlərinin HAMISINDA dəyər 0-dır. Lüğətdə 0 «XÖHKS»
                     --    deməkdir, XÖHKS isə MANAT sistemidir — valyuta ödənişi
                     --    oradan keçə bilməz, yəni 0 burada «XÖHKS» yox, «BOŞ» deməkdir.
                     --    (BMI-nin öz `TRG_CHECK_I_DOC_VNESH_INVAL` triggeri məhz
                     --     `plat_system = 1` halını yoxlayır — deməli 1 gözlənilib,
                     --     amma real datada bir dənə də 1 yoxdur.)
                     --    Bu iki cədvəlin ÖZÜ SWIFT axınıdır, ona görə sahəyə
                     --    baxmırıq: sabit SWIFT kodu (1) veririk — üst qatdakı CASE
                     --    onu «SWIFT» kimi yazacaq. (İstifadəçi qərarı: variant B.)
                     ''1'',
                     to_char((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.sender_bank_bic)),
                     ''1300036291'',
                     v.sender_bank_bic,
                     ''MELIAZ22'',
                     v.beneficiary_bank_bic,
                     cast(null as varchar2(50))
                from odb.doc_vnesh_swift v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.beneficiary_account,28,''0''),11),6) = r.regnom
              ) z, prm
          where z.debet  like ''%''||prm.hesab||''%''
             or z.kredit like ''%''||prm.hesab||''%''
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
  -- 2) MƏDAXİL — hesab KREDİT tərəfdədir (bizim müştəri alandır)
  -- ══════════════════════════════════════════════════════════════════════
  select
      t.date_oper                                                    qeb_tarix,     -- A
      to_char(t.date_oper,''dd/mm/yyyy'')                              icra_tarix,    -- B
      t.recnum                                                       dax_istinad,   -- C  🆕
      vd.vd_id                                                       xar_istinad,   -- D  🆕
      case when t.debet  in (''10010000000000100000'',''10020010000000100000'',
                             ''10020020000000100000'',''10020030000000100000'',
                             ''10020040000000100000'',''10020050000000100000'')
             or t.kredit in (''10010000000000100000'',''10020010000000100000'',
                             ''10020020000000100000'',''10020030000000100000'',
                             ''10020040000000100000'',''10020050000000100000'')
             or substr(t.debet ,1,5) = ''25019''
             or substr(t.kredit,1,5) = ''25019''
           then ''Nağd'' else ''Qeyri-nağd'' end                         emel_novu,     -- E  🆕
      ''  ''                                                           cat_kan,       -- F
      vd.plat_system                                                 odeme_sistemi, -- G  🆕
      ''  ''                                                           alt_nov,       -- H

      -- ── GÖNDƏRƏN (qarşı tərəf) ────────────────────────────────────────
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150,350))
                then h.name_regnom
           else case when t.id_vd is null and substr(t.debet,1,1) in (4,6,7,8,9) then h.name_regnom
                     else vd.ben_ad end end                          gon_ad,        -- I
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150,350))
                then h.inn_regnom
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,1) in (4,6,7,8,9))
                     then s.inn_licsch else vd.inn_regnom end end    gon_voen,      -- J
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150,350))
                then h.pincode
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,1) in (4,6,7,8,9))
                     then h.pincode else vd.fin end end              gon_fin,       -- K
      ''   ''                                                          gon_hesnov,    -- L
      case when t.id_vd is null then t.debet else trim(vd.debet) end gon_hes,       -- M
      case when t.id_vd is null then ''Bank Melli Iran'' else vd.filial_name end gon_bank, -- N
      case when t.id_vd is null then ''Baki fil. ''      else vd.filial_name end gon_fil,  -- O
      case when t.id_vd is null then ''1300036291''
           else vd.gon_bank_voen end                                 gon_bank_voen, -- P  🆕
      case when t.id_vd is null then ''MELIAZ22''
           else nvl(vd.gon_bank_bic2, vd.sender_bic) end             gon_bank_bic,  -- Q  🆕
      vd.sender_bic                                                  gon_mux_bic,   -- R
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150,350))
                then p.countrycode
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,1) in (4,6,7,8,9))
                     then s.countrycode else ''   '' end end           gon_olke,      -- S
      case t.kod_valuti when 0 then ''AZN'' when 1 then ''USD''
                        when 2 then ''EUR'' when 3 then ''RUB''
                        when 4 then ''IRR'' when 5 then ''AED'' end gon_valuta,   -- T  (əməliyyatın valyutası)
      ''   ''                                                          gon_pan,       -- U
      ''   ''                                                          gon_mcc,       -- V

      -- ── ALAN (bizim müştəri) ──────────────────────────────────────────
      TRIM(f.name_regnom)                                            alan_ad,       -- W
      ''   ''                                                          alan_voen,     -- X
      f.pincode                                                      alan_fin,      -- Y
      case when substr(t.kredit,16,1)=''9'' then ''P/k'' else ''Cari'' end alan_hesnov,   -- Z
      t.kredit                                                       alan_hes,      -- AA
      ''Bank Melli Iran''                                              alan_bank,     -- AB
      ''Baki fil. ''                                                   alan_fil,      -- AC
      ''1300036291''                                                   alan_bank_voen,-- AD 🆕 (bizik)
      ''MELIAZ22''                                                     alan_bank_bic, -- AE 🆕 (bizik)
      vd.receiver_bic                                                alan_mux_bic,  -- AF
      p.countrycode                                                  alan_olke,     -- AG
      case substr(t.kredit,6,2) when ''00'' then ''AZN'' when ''01'' then ''USD''
                             when ''02'' then ''EUR'' when ''03'' then ''RUB''
                             when ''04'' then ''IRR'' when ''05'' then ''AED'' end alan_valuta,  -- AH (hesabın 6-7-ci simvolu)
      ''   ''                                                          alan_pan,      -- AI
      ''   ''                                                          alan_mcc,      -- AJ

      t.summa_v_inval                                                med_val,       -- AK Mədaxil
      0                                                              max_val,       -- AL Məxaric
      t.kod_valuti                                                   val_kod,       -- AM
      t.summa_v_nacval                                               med_azn,       -- AN Mədaxil (AZN)
      0                                                              max_azn,       -- AO Məxaric (AZN)
      t.primechanie                                                  emel,          -- AP
      vd.xeyrine_ad                                                  xeyrine_ad,    -- AQ 🆕
      vd.xeyrine_fin                                                 xeyrine_fin,   -- AR 🆕
      ''  ''                                                           kommun,        -- AS
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
                     v.currency valuta, ''  '' inn_kred, ''  '' fin_kredit,
                     v.sender_bic, v.receiver_bic,
                     to_char(v.id)                                               vd_id,
                     -- ⚠️ XARİCİ SƏNƏD — PLAT_SYSTEM burada DOLDURULMUR.
                     --    `doc_vnesh_inval` (404/404) və `doc_vnesh_swift` (82/82)
                     --    sətirlərinin HAMISINDA dəyər 0-dır. Lüğətdə 0 «XÖHKS»
                     --    deməkdir, XÖHKS isə MANAT sistemidir — valyuta ödənişi
                     --    oradan keçə bilməz, yəni 0 burada «XÖHKS» yox, «BOŞ» deməkdir.
                     --    (BMI-nin öz `TRG_CHECK_I_DOC_VNESH_INVAL` triggeri məhz
                     --     `plat_system = 1` halını yoxlayır — deməli 1 gözlənilib,
                     --     amma real datada bir dənə də 1 yoxdur.)
                     --    Bu iki cədvəlin ÖZÜ SWIFT axınıdır, ona görə sahəyə
                     --    baxmırıq: sabit SWIFT kodu (1) veririk — üst qatdakı CASE
                     --    onu «SWIFT» kimi yazacaq. (İstifadəçi qərarı: variant B.)
                     ''1''                                                          plat_system,
                     ''1300036291''                                                gon_bank_voen,
                     to_char((select max(m.voen) from odb.muxbir_hesab m
                               where m.swift_kodu = v.receiver_bic))            alan_bank_voen,
                     ''MELIAZ22''                                                  gon_bank_bic2,
                     v.receiver_bic                                              alan_bank_bic2,
                     cast(null as varchar2(300))                                 xeyrine_ad,
                     cast(null as varchar2(50))                                  xeyrine_fin
                from odb.doc_vnesh_inval v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.account_no,10,6) = r.regnom

              union all
              select v.date_oper tarix, v.date_oper val_tar, r.inn_regnom, r.pincode fin,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     v.mfo_debet filial_name, v.nomer_docum,
                     nvl((select max(m2.bank_large_name) from odb.mfo m2
                           where m2.mfo = v.mfo_credit), to_char(v.mfo_credit)) ben_bank,
                     odb.func_utf8_to_latin(v.name_credit) ben_name, v.kredit, v.name_credit,
                     v.summa_v_nacval amount, ''Odemeler '' cmnt,
                     ''944'' valuta, v.inn_credit, ''  '' fin_kredit,
                     '' '' sender_bic, '' '' receiver_bic,
                     to_char(v.id),
                     to_char(v.plat_system),
                     ''1300036291'',
                     to_char((select max(m.voen) from odb.muxbir_hesab m where m.kod = v.mfo_credit)),
                     ''MELIAZ22'',
                     to_char((select max(m.swift_kodu) from odb.muxbir_hesab m where m.kod = v.mfo_credit)),
                     cast(null as varchar2(300)),
                     cast(null as varchar2(50))
                from odb.doc_vnesh_nacval v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.debet,10,6) = r.regnom

              union all
              select v.date_oper tarix, v.date_oper val_tar, v.inn_debet, ''     '' fin_debet,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     mf.bank_large_name filial_name, v.nomer_docum,
                     nvl((select max(m2.bank_large_name) from odb.mfo m2
                           where m2.mfo = v.mfo_kredit), to_char(v.mfo_kredit)) ben_bank,
                     odb.func_utf8_to_latin(v.kredit_name) ben_name, substr(v.kredit,9,20) kredit,
                     v.kredit_name, v.sum1 amount, ''Daxilolma'' cmnt,
                     ''944'' valuta, r.inn_regnom, r.pincode fin_kred,
                     v.bic_debet, v.bic_kredit,
                     to_char(v.id),
                     to_char(v.plat_system),
                     to_char(nvl((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.bic_debet),
                                 (select max(m.voen) from odb.muxbir_hesab m where m.kod       = v.mfo_debet))),
                     ''1300036291'',
                     v.bic_debet,
                     ''MELIAZ22'',
                     odb.func_utf8_to_latin(v.kredit_name),
                     to_char(v.kredit_inn)
                from odb.doc_vnesh_postupl v, odb.regnom r, odb.mfo mf, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.kredit,28,''0''),11),6) = r.regnom
                 and v.mfo_debet = mf.mfo(+)

              union all
              select v.date_oper tarix, v.date_oper val_tar, ''  '' inn_deb, ''  '' fin_debet,
                     odb.func_utf8_to_latin(v.sender_name) emit_name, v.sender_account debet,
                     v.sender_bank_name filial_name, v.nomer_docum, v.beneficiary_bank_name,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.beneficiary_account,
                     v.beneficiary_name, v.amount, ''Daxilolma'' cmnt,
                     v.currency valuta, r.inn_regnom, r.pincode fin_kred,
                     v.sender_bank_bic, v.beneficiary_bank_bic,
                     to_char(v.id),
                     -- ⚠️ XARİCİ SƏNƏD — PLAT_SYSTEM burada DOLDURULMUR.
                     --    `doc_vnesh_inval` (404/404) və `doc_vnesh_swift` (82/82)
                     --    sətirlərinin HAMISINDA dəyər 0-dır. Lüğətdə 0 «XÖHKS»
                     --    deməkdir, XÖHKS isə MANAT sistemidir — valyuta ödənişi
                     --    oradan keçə bilməz, yəni 0 burada «XÖHKS» yox, «BOŞ» deməkdir.
                     --    (BMI-nin öz `TRG_CHECK_I_DOC_VNESH_INVAL` triggeri məhz
                     --     `plat_system = 1` halını yoxlayır — deməli 1 gözlənilib,
                     --     amma real datada bir dənə də 1 yoxdur.)
                     --    Bu iki cədvəlin ÖZÜ SWIFT axınıdır, ona görə sahəyə
                     --    baxmırıq: sabit SWIFT kodu (1) veririk — üst qatdakı CASE
                     --    onu «SWIFT» kimi yazacaq. (İstifadəçi qərarı: variant B.)
                     ''1'',
                     to_char((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.sender_bank_bic)),
                     ''1300036291'',
                     v.sender_bank_bic,
                     ''MELIAZ22'',
                     v.beneficiary_bank_bic,
                     cast(null as varchar2(50))
                from odb.doc_vnesh_swift v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.beneficiary_account,28,''0''),11),6) = r.regnom
              ) z, prm
          where z.debet  like ''%''||prm.hesab||''%''
             or z.kredit like ''%''||prm.hesab||''%''
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

      ) k
  ) x
 order by x.qeb_tarix asc'
 WHERE SorguAdi = N'AML_HESAB_SORGU_FIZIKI' AND ISNULL(Silinib,0) = 0;

IF @@ROWCOUNT = 0
    PRINT N'XEBERDARLIQ: AML_HESAB_SORGU_FIZIKI tapilmadi — evvelce 90_AML_OracleSorgular.sql islet.';

/* -- AML_HESAB_SORGU_HUQUQI -------------------------------------- */
UPDATE OracleSorgular
   SET SorguMetni = N'with prm as (
    select ''{HESAB}''                                  hesab,
           to_date(''{TARIX1}'',''dd/mm/yyyy'')           d1,
           to_date(''{TARIX2}'',''dd/mm/yyyy'')           d2
      from dual
)
select
    -- ══════════════════════════════════════════════════════════════════════
    -- ÜST QAT — BMI-də bunlar Excel-ə yazılmazdan ƏVVƏL C#-da (exceleat2)
    -- dataGridView xanalarının üzərinə yazılırdı. İndi SQL-in içindədir ki,
    -- FinNex tərəfdə heç bir «grid düzəlişi» lazım olmasın.
    -- ══════════════════════════════════════════════════════════════════════
    x.qeb_tarix,                                                        -- A
    x.icra_tarix,                                                       -- B
    x.dax_istinad,                                                      -- C
    x.xar_istinad,                                                      -- D
    x.emel_novu,                                                        -- E
    -- F  Çatdırılma kanalı (C#: Cells[3]). C#-da hər `if` bir öncəkini
    --    ÜSTƏLƏYİRDİ → burada sıra QƏSDƏN TƏRSİNƏDİR (CASE ilk uyğunu götürür).
    case when x.dbtam = ''25010000000000300000''
              or x.krtam in (''25020010000000300002'',''25020020000000300002'')
              then ''POS terminal''
         when substr(x.dbtam,1,5) = ''25019''
              then ''Ödəniş terminalı''
         when substr(x.dbtam,1,5) in (''10010'',''10020'')
              or substr(x.krtam,1,5) in (''10010'',''10020'')
              then ''CAS''
         when substr(x.dbtam,1,5) in (''35025'',''35020'',''15025'',''15020'')
              or substr(x.krtam,1,5) in (''35025'',''35020'',''15025'',''15020'')
              then ''Digər maliyyə institutu vasitəsi''
         when x.dbtam in (''11010000020000200000'',''11010000030000200000'',''11010000050000200000'')
              or x.krtam in (''11010000020000200000'',''11010000030000200000'',''11010000050000200000'')
              then ''Digər maliyyə institutu vasitəsi''
         when x.krtam = ''25010000000000300000''
              then ''ATM''
    end                                                                 cat_kan,      -- F
    -- G  Ödəniş sisteminin növü — kod → ad.
    --    Xəritənin mənbəyi: ODB.PROC_IPS_INS_VNESH_POSTUPL, 48-ci sətir:
    --      «p_PLAT_SYSTEM >>> 0 = XOHKS, 1 = SWIFT, 2 = INTERNAL,
    --                         3 = APUS, 4 = V-Shape, 5 = HOP, 6 = IPS»
    --    Təsdiq: PCG_IPS → `c_ips_plat_system_id CONSTANT INTEGER := 6`
    --            TRG_CHECK_I_DOC_VNESH_NACVAL → «0=XOHKS, 1=SWIFT, 2=INTERNAL»
    --    Bazada LÜĞƏT CƏDVƏLİ YOXDUR — xəritə yalnız PL/SQL mənbəyində var,
    --    ona görə burada saxlanılır. Yeni sistem əlavə olunsa bu CASE yenilənir.
    --
    --    ⚠️ XARİCİ SƏNƏDLƏR BURAYA 0 GÖNDƏRMİR — `vd` alt sorğusunda
    --    `doc_vnesh_inval` və `doc_vnesh_swift` qolları sabit `''1''` (SWIFT)
    --    verir, çünki o iki cədvəldə `PLAT_SYSTEM` heç vaxt doldurulmayıb
    --    (404/404 və 82/82 sətir 0). Ətraflı izah həmin qolların yanındadır.
    --    Yəni buradakı `when ''0'' then ''XÖHKS''` yalnız DAXİLİ sənədlərə
    --    (nacval / postupl) aiddir — orada 0 real XÖHKS deməkdir.
    case x.odeme_sistemi
         when ''0'' then ''XÖHKS''
         when ''1'' then ''SWIFT''
         when ''2'' then ''Bank daxili''
         when ''3'' then ''APUS''
         when ''4'' then ''V-Shape''
         when ''5'' then ''HOP''
         when ''6'' then ''IPS''
         else x.odeme_sistemi
    end                                                                 odeme_sistemi, -- G
    x.alt_nov,                                                          -- H

    -- I  Göndərənin adı (C#: Cells[4]) — hesabın `licsch` adı üstələyir.
    --    İki xüsusi hesabın adı sabitdir və lookup-dan SONRA tətbiq olunur
    --    (C#-da `j` dövrü 37 dəfə işlədiyi üçün sabit ad axırda qalırdı).
    case when x.gon_hes = ''25052000040000300000'' then ''İŞÇİLƏRƏ AVANS MÜKAFAT''
         when x.gon_hes = ''25019000000000300006'' then ''Ödəniş terminalı''
         else nvl(x.gon_lics_ad, x.gon_ad) end                          gon_ad,       -- I
    x.gon_voen,                                                         -- J
    x.gon_fin,                                                          -- K
    x.gon_hesnov,                                                       -- L
    x.gon_hes,                                                          -- M
    x.gon_bank,                                                         -- N
    x.gon_fil,                                                          -- O
    x.gon_bank_voen,                                                    -- P
    x.gon_bank_bic,                                                     -- Q
    x.gon_mux_bic,                                                      -- R
    -- S  Ödəniş terminalı hesabında ölkə sabit AZE (C#: Cells[11])
    case when x.gon_hes = ''25019000000000300006'' then ''AZE''
         else x.gon_olke end                                            gon_olke,     -- S
    x.gon_valuta,                                                       -- T
    x.gon_pan,                                                          -- U
    x.gon_mcc,                                                          -- V

    -- W/X/Y  Alan tərəf — `licsch` + `regnom` axtarışı üstələyir
    --        (C#: Cells[16]/[17]/[18]). Üçü də YALNIZ ad tapılanda yazılır.
    nvl(x.alan_lics_ad, x.alan_ad)                                      alan_ad,      -- W
    case when x.alan_lics_ad is not null then x.alan_lics_voen
         else x.alan_voen end                                           alan_voen,    -- X
    case when x.alan_lics_ad is not null then x.alan_lics_fin
         else x.alan_fin end                                            alan_fin,     -- Y
    -- Z  Hesab növü (C#: Cells[19]). DİQQƏT — BMI burada həm alanın, həm
    --    GÖNDƏRƏNİN hesabına baxır və nəticəni ALAN sütununa yazır.
    --    Səhv görünür, amma olduğu kimi saxlanılıb (BMI ilə üzləşdirmə üçün).
    case when substr(x.alan_hes,1,2) in (''40'',''41'',''38'',''39'')
              or substr(x.gon_hes ,1,2) in (''40'',''41'',''38'',''39'') then ''Cari''
         else x.alan_hesnov end                                         alan_hesnov,  -- Z
    x.alan_hes,                                                         -- AA
    x.alan_bank,                                                        -- AB
    x.alan_fil,                                                         -- AC
    x.alan_bank_voen,                                                   -- AD
    x.alan_bank_bic,                                                    -- AE
    x.alan_mux_bic,                                                     -- AF
    x.alan_olke,                                                        -- AG
    x.alan_valuta,                                                      -- AH
    x.alan_pan,                                                         -- AI
    x.alan_mcc,                                                         -- AJ
    x.med_val,                                                          -- AK
    x.max_val,                                                          -- AL
    -- AM Valyuta kodu — BMI `kod_valuti`-ni yazıb sonra ÜSTÜNDƏN alan tərəfin
    --    valyuta mətnini yazırdı (exceleat2 sonu: Cells[24] → 31-ci sütun).
    x.alan_valuta                                                       val_kod,      -- AM
    x.med_azn,                                                          -- AN
    x.max_azn,                                                          -- AO
    x.emel,                                                             -- AP
    -- AQ/AR — mövcud struktur mənbə (doc_vnesh_*) ÜSTÜNDÜR; HÖP mətnindən
    -- oxuma yalnız HƏR İKİSİ boş olanda işə düşür. «Hər ikisi» şərti
    -- QƏSDƏNDİR: SWIFT sətrində ad var (BIC), VÖEN yoxdur — adı bir
    -- mənbədən, VÖEN-i başqasından götürsək sətir uydurma cütlük olardı.
    case when x.xeyrine_ad is null and x.xeyrine_fin is null
         then x.hop_ad   else x.xeyrine_ad  end                         xeyrine_ad,   -- AQ
    case when x.xeyrine_ad is null and x.xeyrine_fin is null
         then x.hop_voen else x.xeyrine_fin end                         xeyrine_fin,  -- AR
    x.kommun,                                                           -- AS
    x.dt,                                                               -- AT
    x.kt                                                                -- AU
  from (
    select k.*,
           -- BMI-dəki `Hes_adlari` sorğusunun eynisi, sətir-sətir axtarış əvəzinə
           -- skalyar alt sorğu ilə. `substr(r.regnom, 2.5)` BMI-də yazı səhvidir —
           -- Oracle 2.5-i 2-yə kəsir, yəni `substr(regnom,2)`.
           (select max(l.name_licsch) from odb.licsch l
             where l.licsch = k.gon_hes
               and l.date_close_licsch is null)                         gon_lics_ad,
           (select max(l.name_licsch) from odb.licsch l
             where l.licsch = k.alan_hes
               and l.date_close_licsch is null)                         alan_lics_ad,
           (select max(case when r.fizik = 1 then null
                            else to_char(l.inn_licsch) end)
              from odb.licsch l, odb.regnom r
             where l.licsch = k.alan_hes
               and l.date_close_licsch is null
               and substr(l.licsch,11,5) = substr(r.regnom,2))          alan_lics_voen,
           (select max(r.pincode)
              from odb.licsch l, odb.regnom r
             where l.licsch = k.alan_hes
               and l.date_close_licsch is null
               and substr(l.licsch,11,5) = substr(r.regnom,2))          alan_lics_fin,
           -- ── «Öz xeyrinə» (AQ/AR) — YALNIZ HÖP ƏMƏLİYYATINDA ─────────────
           -- İstifadəçi qaydası (20.08.2026): sütunlar yalnız müştəri
           -- hesabından **45013000000000400009** hesabına keçən əməliyyatda
           -- dolur (debet = müştəri hesabı, kredit = həmin hesab). Başqa
           -- sətirlərdə toxunulmur.
           --
           -- Mətn `arh_dd.primechanie`-dədir və OLDUĞU KİMİ götürülür —
           -- şərh edilmir («bizim öz yanaşmamız lazım deyil»):
           --   VÖEN-AVANS: 1604964601 - Dövlət Gömrük Komitəsi (MOUSAVİAN ...)
           --               └── AR ──┘   └────────── AQ ────────┘
           -- Mötərizədəki şəxs AQ-ya DÜŞMÜR — o, Təyinat (AP) sütununda
           -- onsuz da görünür.
           --
           -- Şablon «VÖEN» sözünə bağlıdır, sərbəst «10 rəqəm» axtarışı DEYİL:
           -- təyinatda müqavilə/faktura nömrəsi də 10 rəqəmli ola bilər və
           -- etiketsiz axtarış onu səhvən VÖEN kimi yazardı.
           -- `V[^0-9]{0,3}EN` həm «VÖEN», həm «VOEN» yazılışını tutur.
           -- Xam mətndə Azərbaycan hərfləri düzgün oxunur (06.04.2026 sətri
           -- ilə yoxlanılıb) — `func_utf8_to_latin` burada LAZIM DEYİL.
           case when k.krtam = ''45013000000000400009''
                then regexp_substr(k.emel,
                       ''V[^0-9]{0,3}EN[^0-9]{0,20}([0-9]{10})'', 1, 1, null, 1)
           end                                                          hop_voen,
           case when k.krtam = ''45013000000000400009''
                then trim(regexp_substr(
                       regexp_replace(
                           regexp_substr(k.emel,
                             ''V[^0-9]{0,3}EN[^0-9]{0,20}[0-9]{10}(.*)'', 1, 1, null, 1),
                           ''^[ .,:;-]+'', ''''),
                       ''^[^(]*''))
           end                                                          hop_ad
      from (

  -- ══════════════════════════════════════════════════════════════════════
  -- 1) MƏXARİC — hesab DEBET tərəfdədir
  -- ══════════════════════════════════════════════════════════════════════
  select
      t.date_oper                                                    qeb_tarix,     -- A
      t.date_oper                                                    icra_tarix,    -- B
      t.recnum                                                       dax_istinad,   -- C  🆕
      vd.vd_id                                                       xar_istinad,   -- D  🆕
      case when t.debet  in (''10010000000000100000'',''10020010000000100000'',
                             ''10020020000000100000'',''10020030000000100000'',
                             ''10020040000000100000'',''10020050000000100000'')
             or t.kredit in (''10010000000000100000'',''10020010000000100000'',
                             ''10020020000000100000'',''10020030000000100000'',
                             ''10020040000000100000'',''10020050000000100000'')
             or substr(t.debet ,1,5) = ''25019''
             or substr(t.kredit,1,5) = ''25019''
           then ''Nağd'' else ''Qeyri-nağd'' end                         emel_novu,     -- E  🆕
      ''  ''                                                           cat_kan,       -- F
      vd.plat_system                                                 odeme_sistemi, -- G  🆕
      ''  ''                                                           alt_nov,       -- H

      -- ── GÖNDƏRƏN (bizim müştəri) ──────────────────────────────────────
      TRIM(f.name_regnom)                                            gon_ad,        -- I
      f.inn_regnom                                                   gon_voen,      -- J
      f.pincode                                                      gon_fin,       -- K
      case when substr(t.debet,16,1)=''9'' then ''P/k'' else ''Cari'' end  gon_hesnov,    -- L
      t.debet                                                        gon_hes,       -- M
      ''Bank Melli Iran''                                              gon_bank,      -- N
      ''Baki fil. ''                                                   gon_fil,       -- O
      ''1300036291''                                                   gon_bank_voen, -- P  🆕 (bizik)
      ''MELIAZ22''                                                     gon_bank_bic,  -- Q  🆕 (bizik)
      vd.sender_bic                                                  gon_mux_bic,   -- R
      p.countrycode                                                  gon_olke,      -- S
      case substr(t.debet,6,2) when ''00'' then ''AZN'' when ''01'' then ''USD''
                             when ''02'' then ''EUR'' when ''03'' then ''RUB''
                             when ''04'' then ''IRR'' when ''05'' then ''AED'' end gon_valuta,   -- T  (hesabın 6-7-ci simvolu)
      ''   ''                                                          gon_pan,       -- U
      ''   ''                                                          gon_mcc,       -- V

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
      ''   ''                                                          alan_hesnov,   -- Z
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)<>''4'')
                then t.kredit
           else case when t.id_vd is null and substr(t.kredit,1,1)=''4'' then t.kredit
                     else vd.kredit end end                          alan_hes,      -- AA
      case when t.id_vd is null then ''Bank Melli Iran'' else vd.filial_name end alan_bank, -- AB
      case when t.id_vd is null then ''Baki fil. ''      else vd.filial_name end alan_fil,  -- AC
      case when t.id_vd is null then ''1300036291''
           else vd.alan_bank_voen end                                alan_bank_voen,-- AD 🆕
      case when t.id_vd is null then ''MELIAZ22''
           else nvl(vd.alan_bank_bic2, vd.receiver_bic) end          alan_bank_bic, -- AE 🆕
      vd.receiver_bic                                                alan_mux_bic,  -- AF
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)<>''4'')
                then s.countrycode
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.kredit,1,1)=''4'')
                     then s.countrycode else ''   '' end end           alan_olke,     -- AG
      case t.kod_valuti when 0 then ''AZN'' when 1 then ''USD''
                        when 2 then ''EUR'' when 3 then ''RUB''
                        when 4 then ''IRR'' when 5 then ''AED'' end alan_valuta,  -- AH (əməliyyatın valyutası)
      ''   ''                                                          alan_pan,      -- AI
      ''   ''                                                          alan_mcc,      -- AJ

      0                                                              med_val,       -- AK
      t.summa_v_inval                                                max_val,       -- AL
      t.kod_valuti                                                   val_kod,       -- AM
      0                                                              med_azn,       -- AN
      t.summa_v_nacval                                               max_azn,       -- AO
      t.primechanie                                                  emel,          -- AP
      vd.xeyrine_ad                                                  xeyrine_ad,    -- AQ 🆕
      vd.xeyrine_fin                                                 xeyrine_fin,   -- AR 🆕
      ''  ''                                                           kommun,        -- AS
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
                     v.currency valuta, ''  '' inn_kred, ''  '' fin_kredit,
                     v.sender_bic, v.receiver_bic,
                     to_char(v.id)                                               vd_id,
                     -- ⚠️ XARİCİ SƏNƏD — PLAT_SYSTEM burada DOLDURULMUR.
                     --    `doc_vnesh_inval` (404/404) və `doc_vnesh_swift` (82/82)
                     --    sətirlərinin HAMISINDA dəyər 0-dır. Lüğətdə 0 «XÖHKS»
                     --    deməkdir, XÖHKS isə MANAT sistemidir — valyuta ödənişi
                     --    oradan keçə bilməz, yəni 0 burada «XÖHKS» yox, «BOŞ» deməkdir.
                     --    (BMI-nin öz `TRG_CHECK_I_DOC_VNESH_INVAL` triggeri məhz
                     --     `plat_system = 1` halını yoxlayır — deməli 1 gözlənilib,
                     --     amma real datada bir dənə də 1 yoxdur.)
                     --    Bu iki cədvəlin ÖZÜ SWIFT axınıdır, ona görə sahəyə
                     --    baxmırıq: sabit SWIFT kodu (1) veririk — üst qatdakı CASE
                     --    onu «SWIFT» kimi yazacaq. (İstifadəçi qərarı: variant B.)
                     ''1''                                                          plat_system,
                     ''1300036291''                                                gon_bank_voen,
                     to_char((select max(m.voen) from odb.muxbir_hesab m
                               where m.swift_kodu = v.receiver_bic))            alan_bank_voen,
                     ''MELIAZ22''                                                  gon_bank_bic2,
                     v.receiver_bic                                              alan_bank_bic2,
                     cast(null as varchar2(300))                                 xeyrine_ad,
                     cast(null as varchar2(50))                                  xeyrine_fin
                from odb.doc_vnesh_inval v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.account_no,10,6) = r.regnom(+)

              union all
              select v.date_oper tarix, v.date_oper val_tar, r.inn_regnom, r.pincode fin,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     mf.bank_large_name filial_name, v.nomer_docum,
                     nvl((select max(m2.bank_large_name) from odb.mfo m2
                           where m2.mfo = v.mfo_credit), to_char(v.mfo_credit)) ben_bank,
                     odb.func_utf8_to_latin(v.name_credit) ben_name, v.kredit, v.name_credit,
                     v.summa_v_nacval amount, ''Odemeler '' cmnt,
                     ''944'' valuta, v.inn_credit, ''  '' fin_kredit,
                     '' '' sender_bic, '' '' receiver_bic,
                     to_char(v.id),
                     to_char(v.plat_system),
                     ''1300036291'',
                     to_char((select max(m.voen) from odb.muxbir_hesab m where m.kod = v.mfo_credit)),
                     ''MELIAZ22'',
                     to_char((select max(m.swift_kodu) from odb.muxbir_hesab m where m.kod = v.mfo_credit)),
                     cast(null as varchar2(300)),
                     cast(null as varchar2(50))
                from odb.doc_vnesh_nacval v, odb.regnom r, odb.mfo mf, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.debet,10,6) = r.regnom
                 and v.mfo_credit = mf.mfo(+)

              union all
              select v.date_oper tarix, v.date_oper val_tar, v.inn_debet, ''     '' fin_debet,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     mf.bank_large_name filial_name, v.nomer_docum,
                     nvl((select max(m2.bank_large_name) from odb.mfo m2
                           where m2.mfo = v.mfo_kredit), to_char(v.mfo_kredit)) ben_bank,
                     odb.func_utf8_to_latin(v.kredit_name) ben_name, v.kredit, v.kredit_name,
                     v.sum1 amount, ''Daxilolma'' cmnt,
                     ''944'' valuta, r.inn_regnom, r.pincode fin_kred,
                     v.bic_debet, v.bic_kredit,
                     to_char(v.id),
                     to_char(v.plat_system),
                     to_char(nvl((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.bic_debet),
                                 (select max(m.voen) from odb.muxbir_hesab m where m.kod       = v.mfo_debet))),
                     ''1300036291'',
                     v.bic_debet,
                     ''MELIAZ22'',
                     odb.func_utf8_to_latin(v.kredit_name),
                     to_char(v.kredit_inn)
                from odb.doc_vnesh_postupl v, odb.regnom r, odb.mfo mf, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.kredit,28,''0''),11),6) = r.regnom
                 and v.mfo_debet = mf.mfo(+)

              union all
              select v.date_oper tarix, v.date_oper val_tar, ''  '' inn_deb, ''  '' fin_debet,
                     odb.func_utf8_to_latin(v.sender_name) emit_name, v.sender_account debet,
                     v.sender_bank_name filial_name, v.nomer_docum, v.beneficiary_bank_name,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.beneficiary_account,
                     v.beneficiary_name, v.amount, ''Daxilolma'' cmnt,
                     v.currency valuta, r.inn_regnom, r.pincode fin_kred,
                     v.sender_bank_bic, v.beneficiary_bank_bic,
                     to_char(v.id),
                     -- ⚠️ XARİCİ SƏNƏD — PLAT_SYSTEM burada DOLDURULMUR.
                     --    `doc_vnesh_inval` (404/404) və `doc_vnesh_swift` (82/82)
                     --    sətirlərinin HAMISINDA dəyər 0-dır. Lüğətdə 0 «XÖHKS»
                     --    deməkdir, XÖHKS isə MANAT sistemidir — valyuta ödənişi
                     --    oradan keçə bilməz, yəni 0 burada «XÖHKS» yox, «BOŞ» deməkdir.
                     --    (BMI-nin öz `TRG_CHECK_I_DOC_VNESH_INVAL` triggeri məhz
                     --     `plat_system = 1` halını yoxlayır — deməli 1 gözlənilib,
                     --     amma real datada bir dənə də 1 yoxdur.)
                     --    Bu iki cədvəlin ÖZÜ SWIFT axınıdır, ona görə sahəyə
                     --    baxmırıq: sabit SWIFT kodu (1) veririk — üst qatdakı CASE
                     --    onu «SWIFT» kimi yazacaq. (İstifadəçi qərarı: variant B.)
                     ''1'',
                     to_char((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.sender_bank_bic)),
                     ''1300036291'',
                     v.sender_bank_bic,
                     ''MELIAZ22'',
                     v.beneficiary_bank_bic,
                     cast(null as varchar2(50))
                from odb.doc_vnesh_swift v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.beneficiary_account,28,''0''),11),6) = r.regnom
              ) z, prm
          where z.debet  like ''%''||prm.hesab||''%''
             or z.kredit like ''%''||prm.hesab||''%''
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
      case when t.debet  in (''10010000000000100000'',''10020010000000100000'',
                             ''10020020000000100000'',''10020030000000100000'',
                             ''10020040000000100000'',''10020050000000100000'')
             or t.kredit in (''10010000000000100000'',''10020010000000100000'',
                             ''10020020000000100000'',''10020030000000100000'',
                             ''10020040000000100000'',''10020050000000100000'')
             or substr(t.debet ,1,5) = ''25019''
             or substr(t.kredit,1,5) = ''25019''
           then ''Nağd'' else ''Qeyri-nağd'' end                         emel_novu,     -- E  🆕
      ''  ''                                                           cat_kan,       -- F
      vd.plat_system                                                 odeme_sistemi, -- G  🆕
      ''  ''                                                           alt_nov,       -- H

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
      ''   ''                                                          gon_hesnov,    -- L
      case when t.id_vd is null then t.debet else trim(vd.debet) end gon_hes,       -- M
      case when t.id_vd is null then ''Bank Melli Iran'' else vd.filial_name end gon_bank, -- N
      case when t.id_vd is null then ''Baki fil. ''      else vd.filial_name end gon_fil,  -- O
      case when t.id_vd is null then ''1300036291''
           else vd.gon_bank_voen end                                 gon_bank_voen, -- P  🆕
      case when t.id_vd is null then ''MELIAZ22''
           else nvl(vd.gon_bank_bic2, vd.sender_bic) end             gon_bank_bic,  -- Q  🆕
      vd.sender_bic                                                  gon_mux_bic,   -- R
      case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,3) in (100,150))
                then p.countrycode
           else case when t.id_vd is null and (substr(t.debet,10,6)=substr(t.kredit,10,6) or substr(t.debet,1,1) in (4,6,7,8,9))
                     then s.countrycode else ''   '' end end           gon_olke,      -- S
      case t.kod_valuti when 0 then ''AZN'' when 1 then ''USD''
                        when 2 then ''EUR'' when 3 then ''RUB''
                        when 4 then ''IRR'' when 5 then ''AED'' end gon_valuta,   -- T  (əməliyyatın valyutası)
      ''   ''                                                          gon_pan,       -- U
      ''   ''                                                          gon_mcc,       -- V

      -- ── ALAN (bizim müştəri) ──────────────────────────────────────────
      TRIM(f.name_regnom)                                            alan_ad,       -- W
      f.inn_regnom                                                   alan_voen,     -- X
      f.pincode                                                      alan_fin,      -- Y
      case when substr(t.kredit,16,1)=''9'' then ''P/k'' else ''Cari'' end alan_hesnov,   -- Z
      t.kredit                                                       alan_hes,      -- AA
      ''Bank Melli Iran''                                              alan_bank,     -- AB
      ''Baki fil. ''                                                   alan_fil,      -- AC
      ''1300036291''                                                   alan_bank_voen,-- AD 🆕 (bizik)
      ''MELIAZ22''                                                     alan_bank_bic, -- AE 🆕 (bizik)
      vd.receiver_bic                                                alan_mux_bic,  -- AF
      p.countrycode                                                  alan_olke,     -- AG
      case substr(t.kredit,6,2) when ''00'' then ''AZN'' when ''01'' then ''USD''
                             when ''02'' then ''EUR'' when ''03'' then ''RUB''
                             when ''04'' then ''IRR'' when ''05'' then ''AED'' end alan_valuta,  -- AH (hesabın 6-7-ci simvolu)
      ''   ''                                                          alan_pan,      -- AI
      ''   ''                                                          alan_mcc,      -- AJ

      t.summa_v_inval                                                med_val,       -- AK
      0                                                              max_val,       -- AL
      t.kod_valuti                                                   val_kod,       -- AM
      t.summa_v_nacval                                               med_azn,       -- AN
      0                                                              max_azn,       -- AO
      t.primechanie                                                  emel,          -- AP
      vd.xeyrine_ad                                                  xeyrine_ad,    -- AQ 🆕
      vd.xeyrine_fin                                                 xeyrine_fin,   -- AR 🆕
      ''  ''                                                           kommun,        -- AS
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
                     v.currency valuta, ''  '' inn_kred, ''  '' fin_kredit,
                     v.sender_bic, v.receiver_bic,
                     to_char(v.id)                                               vd_id,
                     -- ⚠️ XARİCİ SƏNƏD — PLAT_SYSTEM burada DOLDURULMUR.
                     --    `doc_vnesh_inval` (404/404) və `doc_vnesh_swift` (82/82)
                     --    sətirlərinin HAMISINDA dəyər 0-dır. Lüğətdə 0 «XÖHKS»
                     --    deməkdir, XÖHKS isə MANAT sistemidir — valyuta ödənişi
                     --    oradan keçə bilməz, yəni 0 burada «XÖHKS» yox, «BOŞ» deməkdir.
                     --    (BMI-nin öz `TRG_CHECK_I_DOC_VNESH_INVAL` triggeri məhz
                     --     `plat_system = 1` halını yoxlayır — deməli 1 gözlənilib,
                     --     amma real datada bir dənə də 1 yoxdur.)
                     --    Bu iki cədvəlin ÖZÜ SWIFT axınıdır, ona görə sahəyə
                     --    baxmırıq: sabit SWIFT kodu (1) veririk — üst qatdakı CASE
                     --    onu «SWIFT» kimi yazacaq. (İstifadəçi qərarı: variant B.)
                     ''1''                                                          plat_system,
                     ''1300036291''                                                gon_bank_voen,
                     to_char((select max(m.voen) from odb.muxbir_hesab m
                               where m.swift_kodu = v.receiver_bic))            alan_bank_voen,
                     ''MELIAZ22''                                                  gon_bank_bic2,
                     v.receiver_bic                                              alan_bank_bic2,
                     cast(null as varchar2(300))                                 xeyrine_ad,
                     cast(null as varchar2(50))                                  xeyrine_fin
                from odb.doc_vnesh_inval v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.account_no,10,6) = r.regnom

              union all
              select v.date_oper tarix, v.date_oper val_tar, r.inn_regnom, r.pincode fin,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     v.mfo_debet filial_name, v.nomer_docum,
                     nvl((select max(m2.bank_large_name) from odb.mfo m2
                           where m2.mfo = v.mfo_credit), to_char(v.mfo_credit)) ben_bank,
                     odb.func_utf8_to_latin(v.name_credit) ben_name, v.kredit, v.name_credit,
                     v.summa_v_nacval amount, ''Odemeler '' cmnt,
                     ''944'' valuta, v.inn_credit, ''  '' fin_kredit,
                     '' '' sender_bic, '' '' receiver_bic,
                     to_char(v.id),
                     to_char(v.plat_system),
                     ''1300036291'',
                     to_char((select max(m.voen) from odb.muxbir_hesab m where m.kod = v.mfo_credit)),
                     ''MELIAZ22'',
                     to_char((select max(m.swift_kodu) from odb.muxbir_hesab m where m.kod = v.mfo_credit)),
                     cast(null as varchar2(300)),
                     cast(null as varchar2(50))
                from odb.doc_vnesh_nacval v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and substr(v.debet,10,6) = r.regnom

              union all
              -- ⚠️ hüquqi variantda lpad YOXDUR və kredit odb.right(...,20) ilə kəsilir
              select v.date_oper tarix, v.date_oper val_tar, v.inn_debet, ''     '' fin_debet,
                     odb.func_utf8_to_latin(v.name_debet) emit_name, v.debet,
                     mf.bank_large_name filial_name, v.nomer_docum,
                     nvl((select max(m2.bank_large_name) from odb.mfo m2
                           where m2.mfo = v.mfo_kredit), to_char(v.mfo_kredit)) ben_bank,
                     odb.func_utf8_to_latin(v.kredit_name) ben_name, odb.right(v.kredit,20) kredit,
                     v.kredit_name, v.sum1 amount, ''Daxilolma'' cmnt,
                     ''944'' valuta, r.inn_regnom, r.pincode fin_kred,
                     v.bic_debet, v.bic_kredit,
                     to_char(v.id),
                     to_char(v.plat_system),
                     to_char(nvl((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.bic_debet),
                                 (select max(m.voen) from odb.muxbir_hesab m where m.kod       = v.mfo_debet))),
                     ''1300036291'',
                     v.bic_debet,
                     ''MELIAZ22'',
                     odb.func_utf8_to_latin(v.kredit_name),
                     to_char(v.kredit_inn)
                from odb.doc_vnesh_postupl v, odb.regnom r, odb.mfo mf, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(v.kredit,11),6) = r.regnom
                 and v.mfo_debet = mf.mfo(+)

              union all
              -- ⚠️ hüquqi variantda regnom 5 simvolla tutuşdurulur (fizikidə 6)
              select v.date_oper tarix, v.date_oper val_tar, ''  '' inn_deb, ''  '' fin_debet,
                     odb.func_utf8_to_latin(v.sender_name) emit_name, v.sender_account debet,
                     v.sender_bank_name filial_name, v.nomer_docum, v.beneficiary_bank_name,
                     odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.beneficiary_account,
                     v.beneficiary_name, v.amount, ''Daxilolma'' cmnt,
                     v.currency valuta, r.inn_regnom, r.pincode fin_kred,
                     v.sender_bank_bic, v.beneficiary_bank_bic,
                     to_char(v.id),
                     -- ⚠️ XARİCİ SƏNƏD — PLAT_SYSTEM burada DOLDURULMUR.
                     --    `doc_vnesh_inval` (404/404) və `doc_vnesh_swift` (82/82)
                     --    sətirlərinin HAMISINDA dəyər 0-dır. Lüğətdə 0 «XÖHKS»
                     --    deməkdir, XÖHKS isə MANAT sistemidir — valyuta ödənişi
                     --    oradan keçə bilməz, yəni 0 burada «XÖHKS» yox, «BOŞ» deməkdir.
                     --    (BMI-nin öz `TRG_CHECK_I_DOC_VNESH_INVAL` triggeri məhz
                     --     `plat_system = 1` halını yoxlayır — deməli 1 gözlənilib,
                     --     amma real datada bir dənə də 1 yoxdur.)
                     --    Bu iki cədvəlin ÖZÜ SWIFT axınıdır, ona görə sahəyə
                     --    baxmırıq: sabit SWIFT kodu (1) veririk — üst qatdakı CASE
                     --    onu «SWIFT» kimi yazacaq. (İstifadəçi qərarı: variant B.)
                     ''1'',
                     to_char((select max(m.voen) from odb.muxbir_hesab m where m.swift_kodu = v.sender_bank_bic)),
                     ''1300036291'',
                     v.sender_bank_bic,
                     ''MELIAZ22'',
                     v.beneficiary_bank_bic,
                     cast(null as varchar2(50))
                from odb.doc_vnesh_swift v, odb.regnom r, prm
               where v.date_oper between prm.d1 and prm.d2
                 and odb.left(odb.right(lpad(v.beneficiary_account,28,''0''),11),5) = r.regnom
              ) z, prm
          where z.debet  like ''%''||prm.hesab||''%''
             or z.kredit like ''%''||prm.hesab||''%''
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

      ) k
  ) x
 order by x.qeb_tarix asc'
 WHERE SorguAdi = N'AML_HESAB_SORGU_HUQUQI' AND ISNULL(Silinib,0) = 0;

IF @@ROWCOUNT = 0
    PRINT N'XEBERDARLIQ: AML_HESAB_SORGU_HUQUQI tapilmadi — evvelce 90_AML_OracleSorgular.sql islet.';

COMMIT TRAN;

/* -- YOXLAMA — her ikisinde «hop_voen» olmalidir -------------------------- */
SELECT SorguAdi,
       LEN(SorguMetni)                                        AS YeniUzunluq,
       CASE WHEN SorguMetni LIKE N'%hop_voen%'
            THEN N'OK' ELSE N'XETA — yenilenmeyib' END        AS Veziyyet
  FROM OracleSorgular
 WHERE SorguAdi IN (N'AML_HESAB_SORGU_FIZIKI', N'AML_HESAB_SORGU_HUQUQI')
   AND ISNULL(Silinib,0) = 0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XETA: ' + ERROR_MESSAGE();
    THROW;
END CATCH
