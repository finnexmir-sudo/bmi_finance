/* ============================================================================
   GOAML — "Risk" tabı, namizəd əməliyyat sorğusu  (10-11.11.2026)
   Mənbə: BMI AYSEL/GOAML Oracle Forms modulunun "Risk" ekranı — PL/SQL-in
   `ODB.DOC_DNA_ALL_RISK_T` staging cədvəlini doldurduğu DELETE+INSERT+SELECT
   zənciri `v$sql`/`v$open_cursor` ilə canlı tutulub (10.11.2026), sonra isə
   staging-dən ASILI OLMAYAN (yəni Oracle-a yazı tələb etməyən) bir ekvivalent
   AYNI gündə/sabahısı canlı Oracle-a qarşı DƏQIQ TEST EDİLİB (11.11.2026):
   eyni tarix aralığında `doc_dna_all_risk_t`-nin verdiyi say (9) bu sorğu ilə
   ƏN SON doğrulanan formada BİRƏ-BİR üst-üstə düşür.

   BU FAYLI SQL SERVER-DƏ (FinNex bazasında) İŞLƏT — Oracle-da YOX.
   Script TƏKRAR İŞLƏDİLƏ BİLƏR.

   ── KƏŞF TARİXÇƏSİ (qısa) ────────────────────────────────────────────────────
   1. İlk sınaq: yuxarıdakı risk-kriteriyasını birbaşa `ODB.DOC_DNA_ALL_RISK`-ə
      (staging YOX, mənbə) tətbiq edəndə 6 sətir çıxdı, staging isə 9 verirdi —
      3 sətir əskik idi.
   2. Səbəb: staging-in populyasiya INSERT-i `COUNTRYCODE_DE`/`COUNTRYCODE_KRE`
      sütunlarını `NVL(NVL(:B11,:B10),:B9)` zənciri ilə ZƏNGİNLƏŞDİRİR — mənbə
      cədvəldə bu iki sütun bəzi sətirlərdə NULL olur, staging isə onları
      hesab sahibinə bağlı KYC cədvəllərindən (aşağı) doldurur.
   3. `VIEW_LICSCH_ALL.COUNTRYCODE` (hesabın ÖZ bank-tərəfi ölkəsi) ehtiyat
      mənbə kimi SINANDI — say DƏYİŞMƏDİ (hələ 6), yəni bu sütun deyil.
   4. Əsl mənbə tapıldı: 3 RECNUM-un əskik qalmasının səbəbi — hesab SAHİBİNİN
      (DEBET/KREDIT-in REGNOM-u üzərindən) **vətəndaşlığı/qeydiyyat ölkəsi**
      `ELAQELI_HUQUQI_SHEXSLER.INCORPORATION_COUNTRY_CODE` və ya
      `IMZA_HUQUQI_OLAN_SHEXSLER`/`NUMAYENDE`/`FIZIKI_SHEXS.VETENDASHLIGI`-dən
      biri RED siyahıdadır (canlı nümunədə: "İR" — İran), halbuki hesabın öz
      bank-tərəfi ("AZ") TƏMİZDİR. Bu, elə #13-ün (candidate-populate sorğusu)
      ÖZ WHERE şərtinin istifadə etdiyi EYNİ 4 cədvəldir.
   5. Kod formatı tələsi: `COUNTRY_CODES_RED`-in İKİ ayrı sütunu var —
      `VALUE_C` (3-hərfli, "AZE") və `VALUE_C_2` (2-hərfli, "AZ"/"IR").
      KYC cədvəllərinin (ELAQELI/IMZA/NUMAYENDE/FIZIKI) dəyərləri 2-hərflidir
      — `VALUE_C_2`-yə baxılmalıdır, `VALUE_C`-yə YOX (bu, `r_emitent_
      benefisiar`-ın öz vətəndaşlıq şərtinin artıq istifadə etdiyi formatla
      eynidir).

   ── YER TUTUCULARI (Əməliyyatlar sorğusu ilə EYNİ konvensiya) ───────────────
   {DOVREVVEL} , {DOVRSON}  →  servis `dd-MM-yyyy` formatında əvəz edir.
   {MEBLEG_SERT}             →  operator minimum məbləğ yazıbsa
                                `AND t.summa_v_nacval >= <ədəd>`, yoxsa `AND 1=1`.
   {VOEN_HESAB_SERT}         →  operator VÖEN/hesab yazıbsa müvafiq LIKE şərti,
                                yoxsa `AND 1=1`. Dəyər sanitize olunur.

   ⚠️ FAZA 1 (paralel dövr, Əməliyyatlar sorğusu ilə EYNİ qeyd) — AYSEL hələ
   işlədiyi müddətcə `odb.doc_dna_all_risk`-i O doldurur, biz YALNIZ OXUYURUQ.
   ========================================================================== */

SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi LIKE N'RISK%' AND ISNULL(Silinib, 0) = 0
ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler WHERE ISNULL(Silinib, 0) = 0 ORDER BY Id;

IF @DepId IS NULL
BEGIN
    RAISERROR (N'Departament tapılmadı — sorğu əlavə edilmədi.', 16, 1);
    ROLLBACK TRAN;
    RETURN;
END

DECLARE @Ad NVARCHAR(200), @Mahiyyet NVARCHAR(MAX), @Sql NVARCHAR(MAX);

SET @Ad       = N'GOAML_RISK_NAMIZED';
SET @Mahiyyet = N'GOAML → Risk tabı. ODB.DOC_DNA_ALL_RISK — ölkə/qara siyahı/balans-sinif/hesab-sahibi-vətəndaşlığı əsaslı risk kriteriyası (AYSEL-in staging cədvəlindən ASILI DEYİL, canlı test edilib eynidir).';
SET @Sql      = N'select t.recnum, t.date_oper, t.filialnumber, t.debet, t.kredit,
       t.summa_v_inval, t.summa_v_nacval, t.kod_valuti, t.kurs_valuti,
       t.primechanie, t.inn_licsch_de, t.inn_licsch_kre,
       t.countrycode_de, t.countrycode_kre,
       substr(t.debet,10,6) as registrac_nomer_de,
       substr(t.kredit,10,6) as registrac_nomer_kre,
       t.document_source,
       t.name_licsch_de, t.name_licsch_kre
  from odb.doc_dna_all_risk t
 where t.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                        and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
   {MEBLEG_SERT}
   {VOEN_HESAB_SERT}
   and (
        t.countrycode_kre in (select value_c from odb.country_codes_red)
     or t.countrycode_de  in (select value_c from odb.country_codes_red)
     or t.cc_dd in (select value_c from odb.country_codes_red)
     or t.recnum in (
          select s.doc_id from odb.r_emitent_benefisiar s
          where s.b_vetendashligi in (select value_c_2 from odb.country_codes_red)
             or s.e_vetendashligi in (select value_c_2 from odb.country_codes_red)
        )
     or ( substr(t.debet,1,4)||substr(t.kredit,1,4) in (''4104''||''4101'', ''4105''||''4102'') )
     or t.kredit in (select licsch from odb.licsch_blaklist)
     or t.debet  in (select licsch from odb.licsch_blaklist)
     or substr(t.debet,10,6) in (
          select regnom from odb.elaqeli_huquqi_shexsler where incorporation_country_code in (select value_c_2 from odb.country_codes_red)
          union select regnom from odb.imza_huquqi_olan_shexsler where vetendashligi in (select value_c_2 from odb.country_codes_red)
          union select regnom from odb.numayende where vetendashligi in (select value_c_2 from odb.country_codes_red)
          union select regnom from odb.fiziki_shexs where vetendashligi in (select value_c_2 from odb.country_codes_red)
        )
     or substr(t.kredit,10,6) in (
          select regnom from odb.elaqeli_huquqi_shexsler where incorporation_country_code in (select value_c_2 from odb.country_codes_red)
          union select regnom from odb.imza_huquqi_olan_shexsler where vetendashligi in (select value_c_2 from odb.country_codes_red)
          union select regnom from odb.numayende where vetendashligi in (select value_c_2 from odb.country_codes_red)
          union select regnom from odb.fiziki_shexs where vetendashligi in (select value_c_2 from odb.country_codes_red)
        )
      )
 order by t.date_oper desc, t.filialnumber, t.recnum';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

COMMIT TRAN;
PRINT N'GOAML_RISK_NAMIZED sorğusu yazıldı/yeniləndi.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH
