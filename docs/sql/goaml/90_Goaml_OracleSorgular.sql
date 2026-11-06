/* ============================================================================
   GOAML — "Əməliyyatlar" tabı, namizəd əməliyyat sorğusu  (06.11.2026)
   Mənbə: BMI AYSEL/GOAML Oracle Forms modulu — PL/SQL `P_DOC_DNA_GOAML`
   prosedurunun doldurduğu `ODB.DOC_DNA_ALL_TAB` (biznes qaydaları ilə
   filtrlənmiş namizəd) + `ODB.DOC_DNA_ALL_RISK` (ad zənginləşdirməsi),
   `DATE_OPER+FILIALNUMBER+RECNUM` açarı ilə birləşdirilib — bu JOIN 06.11.2026
   sessiyasında canlı Oracle-da test edilib, təsdiqlənib.

   BU FAYLI SQL SERVER-DƏ (FinNex bazasında) İŞLƏT — Oracle-da YOX.
   Script TƏKRAR İŞLƏDİLƏ BİLƏR.

   ── FAZA 1 (PARALEL DÖVR) ───────────────────────────────────────────────────
   AYSEL hələ işlədiyi müddətcə `DOC_DNA_ALL_TAB`/`_RISK`-i O doldurur (onun
   öz `P_DOC_DNA_GOAML` prosedurunun DELETE+INSERT-i ilə) — biz YALNIZ OXUYURUQ.
   AYSEL dayandırılandan sonra (Faza 2) bu sorğu `odb.arh_dd`-dən başlayaraq
   YENİDƏN qurulmalıdır (CLAUDE.md-də qeyd olunacaq, bu sessiyada edilmir).

   ── BİZNES QAYDALARI (P_DOC_DNA_GOAML-dən v$sql ilə çıxarılıb) ─────────────
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
SET @Mahiyyet = N'GOAML → Əməliyyatlar tabı. DOC_DNA_ALL_TAB (biznes qaydaları ilə filtrlənmiş namizəd) + DOC_DNA_ALL_RISK (ad zənginləşdirməsi), DATE_OPER+FILIALNUMBER+RECNUM açarı ilə JOIN.';
SET @Sql      = N'select t.recnum, t.date_oper, t.filialnumber, t.debet, t.kredit,
       t.summa_v_inval, t.summa_v_nacval, t.kod_valuti, t.kurs_valuti,
       t.primechanie, t.inn_licsch_de, t.inn_licsch_kre,
       t.countrycode_de, t.countrycode_kre,
       t.registrac_nomer_de, t.registrac_nomer_kre,
       t.document_source,
       r.name_licsch_de, r.name_licsch_kre
  from odb.doc_dna_all_tab t
  left join odb.doc_dna_all_risk r
    on r.date_oper = t.date_oper
   and r.filialnumber = t.filialnumber
   and r.recnum = t.recnum
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
