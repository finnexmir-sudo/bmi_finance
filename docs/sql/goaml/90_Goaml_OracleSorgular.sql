/* ============================================================================
   GOAML — "Əməliyyatlar" tabı, namizəd əməliyyat sorğusu  (06.11.2026,
   11.11.2026-da KÖKÜNDƏN DÜZƏLDİLDİ — bax aşağı)

   BU FAYLI SQL SERVER-DƏ (FinNex bazasında) İŞLƏT — Oracle-da YOX.
   Script TƏKRAR İŞLƏDİLƏ BİLƏR.

   ── 11.11.2026 DÜZƏLİŞİ — `DOC_DNA_ALL_TAB` TAM SİLİNİR, QLOBAL, SIFIR
      DAYANIQLILIQLI bir "scratch" cədvəldir (KRİTİK) ──────────────────────────
   06.11.2026-da "biznes qaydaları ilə filtrlənmiş namizəd" kimi təqdim olunan
   `ODB.DOC_DNA_ALL_TAB` SƏHV başa düşülmüşdü. `P_DOC_DNA_GOAML`-in TAM mənbə
   kodu (11.11.2026, `all_source` ilə) göstərdi ki, bu prosedur:

     delete odb.doc_dna_all_tab;             -- ⚠️ ŞƏRTSİZ — BÜTÜN cədvəli boşaldır
     commit;
     insert into odb.doc_dna_all_tab (...)
     select distinct ...
       from odb.doc_dna_all_risk             -- YEGANƏ mənbə, HEÇ bir GL/netting filtri YOXDUR
      where date_oper between p_date_s and p_date_po;
     commit;

   Yəni `DOC_DNA_ALL_TAB` — HƏR hansı istifadəçi (AYSEL-də) İSTƏNİLƏN tarix
   aralığı üçün bu prosedura müraciət etdikdə, cədvəlin ÖZÜ TAM SİLİNİR və
   YALNIZ həmin tarix aralığının sətirləri ilə YENİDƏN doldurulur — başqa heç
   bir qayda (GL-istisna, netting-istisna) TƏTBİQ OLUNMADAN. Real hadisə:
   01.11-dən sonra AYSEL-dən 02.11-ə filter edildi → cədvəldə YALNIZ 02.11
   qaldı, bizim tətbiq 03.11/04.11 üçün "0 namizəd" göstərdi — baxmayaraq ki
   DOC_DNA_ALL_RISK-in özündə (aşağı bax) o günlərin datası var idi.

   **Həll — mənbə `ODB.DOC_DNA_ALL_RISK`-ə dəyişdirildi, `DOC_DNA_ALL_TAB`
   TAMAMİLƏ BURAXILDI.** `P_DOC_DNA_GOAML`-in öz kursoru göstərir ki,
   `DOC_DNA_ALL_RISK`-in ÖZÜ artıq bütün lazım olan sütunları (o cümlədən
   `NAME_LICSCH_DE/KRE`, `REGISTRAC_NOMER_DE/KRE`, `DOCUMENT_SOURCE`) NATIV
   daşıyır — bu, elə Risk tabının (10-11.11.2026-da canlı doğrulanmış)
   mənbəyidir. Deməli Əməliyyatlar VƏ Risk artıq EYNİ, SABİT, AYSEL-dən
   TAM MÜSTƏQİL mənbədən (DOC_DNA_ALL_RISK) oxuyur — `odb.arh_dd`-yə enib
   P_DOC_DNA_GOAML-in (əslində HEÇ bir) biznes qaydasını təkrarlamağa EHTİYAC
   OLMADI. `SELECT DISTINCT` P_DOC_DNA_GOAML-in öz `distinct` kursoru ilə EYNİ
   məqsədə xidmət edir — DOC_DNA_ALL_RISK-də bir əməliyyat (eyni DATE_OPER+
   FILIALNUMBER+RECNUM) birdən çox risk-səbəbinə görə TƏKRARLANA bilər, bizim
   seçdiyimiz sütunlarda (risk-səbəb sütunu olmadan) bu, sadə DISTINCT ilə
   düzgün toplanır.

   ── BİZNES QAYDALARI (GL/netting istisnası — AYSEL-in GÖSTƏRMƏ sorğusundan,
      06.11.2026-da v$sql ilə çıxarılıb, DƏYİŞMƏDİ) ─────────────────────────
   - SUBSTR(DEBET,1,1)/KREDIT NOT IN ('6','7','8','9') — daxili/texniki GL
     hesabları xaric edilir.
   - BALCH_NET / BALCH_NET_DEB_KRED — bilinən daxili netting cütləri xaric
     edilir.
   - `filialnumber IN (... WHERE NAME=USER)` restriksiyası BU SORĞUYA
     KÖÇÜRÜLMƏYİB — FinNex Oracle-a TƏK, ortaq xidmət hesabı ilə qoşulur,
     bu hesab `Goaml_name_fil`-də qeydiyyatda olmaya bilər (olsa belə yalnız
     BİR filialı əhatə edərdi). İstifadəçi qərarı (06.11.2026): "az göstərmək"
     əvəzinə "hamısını göstərmək" seçildi — AML alətində bu, təhlükəsiz
     istiqamətdir. Lazım gəlsə gələcəkdə FinNex-in öz departament/filial
     təyinatına görə məhdudlaşdırıla bilər.
   - `( substr(kredit,1,5)||substr(debet,1,5) NOT IN (''10010||10010'') )`
     şərti KÖÇÜRÜLMƏYİB — mənbə SQL-in özündə BUG idi (sağ tərəf hərfi
     "10010||10010" mətni ilə müqayisə edir, heç vaxt reallıqda yaranmayan
     bir dəyərdir, faktiki olaraq HEÇ NƏYİ filtrləmirdi).

   ── YER TUTUCULARI ──────────────────────────────────────────────────────────
   {DOVREVVEL} , {DOVRSON}  →  servis `dd-MM-yyyy` formatında əvəz edir
                                (DATE_OPER aralığı, HƏR İKİSİ məcburidir).
   {MEBLEG_SERT}             →  operator minimum məbləğ yazıbsa
                                `AND t.summa_v_nacval >= <ədəd>`, yoxsa `AND 1=1`.
   {VOEN_HESAB_SERT}         →  operator VÖEN/hesab yazıbsa müvafiq LIKE şərti,
                                yoxsa `AND 1=1`. Dəyər servis tərəfindən
                                sanitize olunur (yalnız rəqəm/hərf, dırnaq yox).

   ⚠️ Oracle custom funksiyası (`func_utf8_to_latin`) bu sorğuda İŞLƏNMİR —
   lazım olmadı, `MATERIALIZE` hinti də lazım deyil (view-merging riski yoxdur,
   cross-join yoxdur).

   ⚠️ GERİYƏ TƏTBİQ OLUNMUR — bu düzəlişdən ƏVVƏL "Qaralama" statusunda
   qalmış/təsdiqlənmiş heç bir qeydə təsiri yoxdur, yalnız BUNDAN SONRAKI
   axtarışlara aiddir.
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

SET @Ad       = N'GOAML_EMELIYYAT_NAMIZED';
SET @Mahiyyet = N'GOAML → Əməliyyatlar tabı. Birbaşa DOC_DNA_ALL_RISK-dən (AYSEL-in hər çağırışda TAM silib-doldurduğu DOC_DNA_ALL_TAB-dan ASILI DEYİL, 11.11.2026-da düzəldildi), GL/netting istisna qaydaları ilə.';
SET @Sql      = N'select distinct t.recnum, t.date_oper, t.filialnumber, t.debet, t.kredit,
       t.summa_v_inval, t.summa_v_nacval, t.kod_valuti, t.kurs_valuti,
       t.primechanie, t.inn_licsch_de, t.inn_licsch_kre,
       t.countrycode_de, t.countrycode_kre,
       t.registrac_nomer_de, t.registrac_nomer_kre,
       t.document_source,
       t.name_licsch_de, t.name_licsch_kre
  from odb.doc_dna_all_risk t
 where t.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                        and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
   and substr(t.debet,1,1) not in (''6'',''7'',''8'',''9'')
   and substr(t.kredit,1,1) not in (''6'',''7'',''8'',''9'')
   and ( substr(t.debet,1,5)||substr(t.kredit,1,5) not in
         ( select balch_deb||balch_kre from odb.balch_net ) )
   and ( substr(t.debet,1,5) not in ( select balch_deb from odb.balch_net_deb_kred )
      or substr(t.kredit,1,5) not in ( select balch_deb from odb.balch_net_deb_kred ) )
   {MEBLEG_SERT}
   {VOEN_HESAB_SERT}
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
PRINT N'GOAML_EMELIYYAT_NAMIZED sorğusu yazıldı/yeniləndi.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH
