/* ============================================================================
   AML → «Məlumat Bazası» — 11 Oracle sorğusu
   Mənbə: BMI desktop → BMI/AML/Sorgular/MelumatBazasi.cs (excelDoldur)
   Hədəf: FinNex → Risk → Məlumat Bazası (MelumatBazasiService)

   BU FAYLI SQL SERVER-DƏ (FinNex bazasında) İŞLƏT — Oracle-da YOX.
   Sorğuların özü Oracle-a gedir, amma mətnləri `OracleSorgular` cədvəlində
   saxlanılır (layihə qaydası — CLAUDE.md).

   ── YER TUTUCULARI ─────────────────────────────────────────────────────────
   {DOVREVVEL} , {DOVRSON}  →  servis `dd-MM-yyyy` formatında əvəz edir.
   Sorğularda `TO_DATE('{DOVREVVEL}','DD-MM-YYYY')` yazılır — format
   DƏYİŞDİRİLMƏMƏLİDİR, yoxsa gün/ay yerdəyişər və səhv dövr gələr.

   ⚠️ BMI-də bu dəyərlər BIND parametri idi (:dovrevvel). FinNex-in
   `IOracleService.SelectAsync` bind qəbul etmir — yalnız tam mətn SQL.
   Ona görə token əvəzlənməsinə keçirildi. Dəyər istifadəçidən GƏLMİR:
   `DateTime` seçicisindən oxunub serverdə formatlanır, yəni mətn kimi
   içəri nəsə yazmaq mümkün deyil.

   ⚠️ 11 sorğudan 4-ü DÖVRSÜZDÜR (token yoxdur), 7-si dövrlüdür — BMI-də də belədir:
      AML_MB_OWNER, AML_MB_ELAQELI_SEXS, AML_MB_KREDIT_ZAMIN,
      AML_MB_AKTIV_HESABLAR
   Onlar həmişə CARİ vəziyyəti verir. Dövr əlavə etmək məzmunu dəyişər —
   mühasib/AML tərəfin qərarı olmadan etmə.

   ⚠️ Sorğu adları QƏSDƏN ASCII-dir — SSMS-də Azərbaycan hərfləri pozulanda
   `=` müqayisəsi sükutla sınır.

   Yoxlama (ən sonda): 11 sətir qayıtmalıdır.
   ========================================================================== */

SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

/* Departament — 90_AML_OracleSorgular.sql ilə EYNİ üsul.
   ⚠️ Departament adına görə axtarmaq ETİBARSIZDIR (ad «Risk», «Risk İdarəetməsi»,
   «Təhlükəsizlik»… ola bilər) — NULL qayıdarsa `DepartamentId` NOT NULL olduğu
   üçün 11 INSERT-in hamısı anlaşılmaz xəta ilə sınardı.
   Ona görə: mövcud RISK% sorğularının departamenti → yoxsa ilk aktiv → yoxsa dayan. */
DECLARE @DepId INT;

SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi LIKE N'RISK%' AND ISNULL(Silinib, 0) = 0
ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler WHERE ISNULL(Silinib, 0) = 0 ORDER BY Id;

IF @DepId IS NULL
BEGIN
    RAISERROR (N'Departament tapılmadı — sorğular əlavə edilmədi.', 16, 1);
    ROLLBACK TRAN;
    RETURN;
END

/* ⚠️ `OracleSorgular.Kataloq` MƏTN DEYİL, BIT-dir — «Ümumi cari Kataloq»
   comboboxunda görünsünmü bayrağı (entity: `OracleSorgu.Kataloq`, bool).
   İlk yazılışda ora kataloq ADI yazılmışdı və script 1-ci INSERT-də sınırdı:
       Msg 245 — Conversion failed when converting the nvarchar value
       'AML / Məlumat Bazası' to data type bit.
   Bu 11 sorğu modulun ÖZ ekranına aiddir, ümumi kataloqda görünməməlidir → 0.
   (90_AML_OracleSorgular.sql-də də eyni: `1, 0, @DepId, …`) */

/* ---------------------------------------------------------------- 1/11 */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_OPEN_ACCOUNTS' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_OPEN_ACCOUNTS',
 N'Məlumat Bazası → Open_Accounts vərəqi. Dövr ərzində açılmış hesablar (3x/4x) + hesabı açan istifadəçi.',
 N'SELECT
    l.date_open_licsch AS ac_tar,
    l.licsch AS hn,
    odb.func_utf8_to_latin(l.name_licsch) AS adi,
    (
        SELECT r.u_s_e_r
        FROM log_accounts r
        WHERE r.LICSCH = l.licsch
          AND r.STATUS = ''INSERT''
          AND r.date_insupddel = (
              SELECT MAX(r2.date_insupddel)
              FROM log_accounts r2
              WHERE r2.LICSCH = r.LICSCH
                AND r2.STATUS = ''INSERT''
          )
    ) AS ad
FROM ODB.licsch l
LEFT JOIN (
    SELECT
        TO_CHAR(f.tarix, ''dd/mm/yyyy'') AS tarix,
        TRIM(f.teyinat) AS teyinat,
        TRIM(f.qn) AS qn,
        SUBSTR(f.ic_kod, 1, 2) AS ic_kod,
        TRIM(f.Icraci) AS icra
    FROM odb.qey_nezaret f
    WHERE f.tarix BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'')
                      AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')
      AND f.teyinat = ''INSERT''
) n
ON SUBSTR(l.licsch, 10, 6) = n.qn
WHERE l.date_open_licsch BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'')
                              AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')
  AND SUBSTR(l.licsch, 1, 1) IN (3,4)
ORDER BY l.date_open_licsch, l.licsch',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 2/11 */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_KOCURME_DAXILI' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_KOCURME_DAXILI',
 N'Məlumat Bazası → Kochurme_Daxili_hes vərəqi. Daxili hesablar arası köçürmələr.',
 N'SELECT
    t.date_oper AS tarix,
    t.debet,
    t.kredit,
    t.summa_v_nacval AS mebleg,
    odb.func_utf8_to_latin(t.primechanie) AS qeyd
FROM odb.arh_dd t
WHERE t.date_oper BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'')
                      AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')
  AND (
        (SUBSTR(t.debet,1,5) IN (''10020'',''15025'',''35025'',''45021'',''45023'',''45029'',''45089'')
         AND SUBSTR(t.kredit,1,5) IN (''45021'',''45023'',''45029'',''45089''))
     OR (SUBSTR(t.debet,1,5) IN (''45021'',''45023'',''45029'',''45089'')
         AND SUBSTR(t.kredit,1,5) IN (''10020'',''15025'',''35025'',''45021'',''45023'',''45029'',''45089''))
      )
ORDER BY t.recnum',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 3/11 */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_KOCURME_MUSTERI' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_KOCURME_MUSTERI',
 N'Məlumat Bazası → Kochurme_Mushteri_hes vərəqi. Müştəri hesabları ilə köçürmələr (vid_operacii < 96).',
 N'select t.date_oper tarix, t.debet, t.kredit, t.summa_v_inval valuta, t.summa_v_nacval manat, odb.func_utf8_to_latin(t.primechanie) qeyd
  from odb.arh_dd t
 where t.date_oper BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'')
                       AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')
   and ((SUBSTR(t.debet,1,5) in (15025,35025,45021,45023,45029,45089)  and (SUBSTR(t.kredit,1,2) in (38,39,40,41) or SUBSTR(t.kredit,1,5) in (35090,35100)) )
     or (SUBSTR(t.kredit,1,5) in (15025,35025,45021,45023,45029,45089)  and (SUBSTR(t.debet,1,2) in (38,39,40,41) or SUBSTR(t.debet,1,5) in (35090,35100)) ))
   and (t.vid_operacii < 96 or t.vid_operacii is null)',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 4/11 */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_EXCHANGE' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_EXCHANGE',
 N'Məlumat Bazası → Exchange vərəqi. Valyuta mübadiləsi (1005 ↔ 1006).',
 N'select t.date_oper tarix, t.debet, t.kredit, t.summa_v_inval valuta, t.summa_v_nacval manat, odb.func_utf8_to_latin(t.primechanie) qeyd
  from odb.arh_dd t
 where t.date_oper BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'')
                       AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')
   and ((substr(t.debet,1,4)=1005 and substr(t.kredit,1,4)=''1006'') or (substr(t.debet,1,4)=1006 and substr(t.kredit,1,4)=''1005''))
 order by recnum',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 5/11  (DÖVRSÜZ) */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_OWNER' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_OWNER',
 N'Məlumat Bazası → Owner vərəqi. Hüquqi şəxs/sahibkar təsisçiləri və payları. DÖVRSÜZ — cari vəziyyət.',
 N'select t.registrac_nomer rn, t.inn_licsch, odb.func_utf8_to_latin(g.name_regnom) ad, t.countrycode olke, t.licsch,
        odb.func_utf8_to_latin(r.owner_name) own, r.owner_id, r.pincode, r.tesischinin_payi pay, odb.func_utf8_to_latin(r.countrycode) vatan,
        case when g.yurik=1 then ''Huquqi'' else ''Sahibkar'' end Nov
   from odb.licsch t, odb.regnomowner r, odb.regnom g, odb.balschkli b
  where t.registrac_nomer=r.regnom(+) and t.registrac_nomer=g.regnom and (g.yurik=1 or g.predprinimatel=1)
    and substr(t.licsch,1,5) in b.balsch
  order by t.registrac_nomer, r.owner_name, t.licsch, g.yurik',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 6/11 */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_EMIT_BENEF' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_EMIT_BENEF',
 N'Məlumat Bazası → Emit_benef vərəqi. Emitent və benefisiar məlumatları.',
 N'select to_char(t.date_oper,''dd/mm/yyyy'') tarix, SUBSTR(odb.func_utf8_to_latin(TRIM(k.e_soyadi)||'' ''||TRIM(k.e_adi)),1,30) emi_ad,
        to_char(k.e_tevellud,''dd/mm/yyyy'') em_dog_tar, SUBSTR(TRIM(k.e_senedin_seriya_ve_nomresi),1,11) emi_sened,
        SUBSTR(odb.func_utf8_to_latin(TRIM(k.b_soyadi)||'' ''||TRIM(k.b_adi)),1,30) ben_ad, SUBSTR(TRIM(k.b_senedin_seriya_ve_nomresi),1,11) ben_sened,
        SUBSTR(odb.func_utf8_to_latin(t.primechanie),1,60) qeyd
   from odb.arh_dd t, odb.emitent_benefisiar k
  where t.recnum=k.doc_id
    and t.date_oper BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'')
                        AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 7/11 */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_UCUNCU_SEXS' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_UCUNCU_SEXS',
 N'Məlumat Bazası → «3-cu shexs» vərəqi. Üçüncü şəxs adından aparılan əməliyyatlar (docfio).',
 N'select t.date_oper tarix, t.debet, t.kredit, t.summa_v_inval valuta, t.summa_v_nacval manat, t.kurs_valuti kurs,
        odb.func_utf8_to_latin(t.primechanie) qeyd, odb.func_utf8_to_latin(t.fio) fio22
   from odb.docfio t
  where t.date_oper BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'')
                        AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')
    and not t.recnum is null
  order by t.date_oper',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 8/11 */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_TRANSFER' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_TRANSFER',
 N'Məlumat Bazası → Transfer vərəqi. Xarici köçürmələr — 4 mənbənin UNION-u (inval, nacval, swift, postupl).',
 N'select t.* from
(select v.date_oper tarix, odb.func_utf8_to_latin(v.account_name) emit_name, odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.amount, v.currency valuta, odb.func_utf8_to_latin(v.comments) cmnt
   from odb.doc_vnesh_inval v
  where v.date_oper BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'') AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')
 union all
 select v.date_oper tarix, odb.func_utf8_to_latin(v.name_debet) emit_name, odb.func_utf8_to_latin(v.name_credit) ben_name, v.summa_v_nacval amount, ''AZN'' valuta, ''Odemeler '' cmnt
   from odb.doc_vnesh_nacval v
  where v.date_oper BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'') AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')
 union all
 select v.date_oper tarix, odb.func_utf8_to_latin(v.sender_name) emit_name, odb.func_utf8_to_latin(v.beneficiary_name) ben_name, v.amount, v.currency valuta, ''Daxilolma'' cmnt
   from odb.doc_vnesh_swift v
  where v.date_oper BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'') AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'')
 union all
 select v.date_oper tarix, odb.func_utf8_to_latin(v.name_debet) emit_name, odb.func_utf8_to_latin(v.kredit_name) ben_name, v.sum1 amount, ''AZN'' valuta, ''Daxilolma'' cmnt
   from odb.doc_vnesh_postupl v
  where v.date_oper BETWEEN TO_DATE(''{DOVREVVEL}'', ''DD-MM-YYYY'') AND TO_DATE(''{DOVRSON}'', ''DD-MM-YYYY'') ) t
 order by t.tarix, t.cmnt',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 9/11  (DÖVRSÜZ) */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_ELAQELI_SEXS' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_ELAQELI_SEXS',
 N'Məlumat Bazası → A_M_L vərəqi. Əlaqəli şəxslər: imza səlahiyyətli + nümayəndə + əlaqəli hüquqi şəxs. DÖVRSÜZ.',
 N'select distinct t.* from
(select t.regnom, odb.func_utf8_to_latin(UPPER(t.soyadi||'' ''||t.adi||'' ''||t.ata_adi)) A_S_A, odb.func_utf8_to_latin(t.fin) fin, to_date(t.doguldugu_tarix, ''DD-MM-YYYY'') dog_tar, ''ELA_SHEXS'' tip from odb.imza_huquqi_olan_shexsler t
 union all
 select t.regnom, odb.func_utf8_to_latin(UPPER(t.soyadi||'' ''||t.adi||'' ''||t.ata_adi)) A_S_A, odb.func_utf8_to_latin(t.fin) fin, to_date(t.doguldugu_tarix, ''DD-MM-YYYY'') dog_tar, ''NUMAYEN'' tip from odb.numayende t
 union all
 select t.regnom, odb.func_utf8_to_latin(UPPER(t.adi)) A_S_A, odb.func_utf8_to_latin(t.voen) fin, null dog_tar, ''ELA_HUQUQ'' tip from odb.elaqeli_huquqi_shexsler t ) t
 where length(trim(t.a_s_a))>0 order by t.regnom',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 10/11 (DÖVRSÜZ) */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_KREDIT_ZAMIN' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_KREDIT_ZAMIN',
 N'Məlumat Bazası → Kred_zamin vərəqi. Açıq kreditlərin zaminləri. DÖVRSÜZ.',
 N'select t.licschkre, t.subschkre sk, g.guarantee_id id, odb.func_utf8_to_latin(g.guarantee_name) zamin, t.summakre mabl, g.pincode
   from odb.licschkre t, odb.creditinfoguarantee g
  where t.licschkre=g.licschkre and t.subschkre=g.subschkre and t.date_close is null and t.bs_vbs is not null and g.guarantee_id is not null',
 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 11/11 (DÖVRSÜZ) */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'AML_MB_AKTIV_HESABLAR' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'AML_MB_AKTIV_HESABLAR',
 N'Məlumat Bazası → Aktiv_hesablar vərəqi. Bağlanmamış hesabı olan müştərilər və ilk açılış tarixi. DÖVRSÜZ.',
 N'select r.regnom rn, min(t.date_open_licsch) ac_tar, odb.func_utf8_to_latin(r.name_regnom) adi
   from odb.licsch t, odb.regnom r
  where t.registrac_nomer = r.regnom(+) and length(t.licsch) = 20 and t.date_close_licsch is null
  group by r.regnom, r.name_regnom
  order by r.regnom',
 1, 0, @DepId, SYSDATETIME(), 0);

COMMIT TRAN;
PRINT N'Məlumat Bazası sorğuları hazırdır (11 ədəd).';

/* ── YOXLAMA — 11 sətir qayıtmalıdır ─────────────────────────────────── */
SELECT Id, SorguAdi, Aktiv, DepartamentId,
       CASE WHEN SorguMetni LIKE N'%{DOVREVVEL}%' THEN N'dövrlü' ELSE N'dövrsüz' END AS Novu,
       LEN(SorguMetni) AS SorguUzunlugu
  FROM OracleSorgular
 WHERE SorguAdi LIKE N'AML_MB_%' AND ISNULL(Silinib,0) = 0
 ORDER BY SorguAdi;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
    THROW;
END CATCH

/* ============================================================================
   GERİ QAYTARMA (lazım olsa) — script-i işlətdikdən sonra fikrini dəyişsən.
   ⚠️ ŞƏRHDƏN ÇIXARIB İŞLƏT, yuxarıdakı ilə birlikdə YOX.

   Yumşaq silmə (tövsiyə olunur — tarixçə qalır):

       UPDATE OracleSorgular
          SET Silinib = 1
        WHERE SorguAdi LIKE N'AML_MB_%' AND ISNULL(Silinib,0) = 0;

   Tam silmə (yalnız səhv yazılıbsa və heç işlədilməyibsə):

       DELETE FROM OracleSorgular WHERE SorguAdi LIKE N'AML_MB_%';

   Sorğunun MƏTNİNİ dəyişmək üçün silib yenidən əlavə etməyə ehtiyac yoxdur —
   Admin → Oracle Sorğular ekranından redaktə et. Servis onları ADA görə
   oxuyur (`AML_MB_*`), Id-yə görə yox.
   ========================================================================== */
