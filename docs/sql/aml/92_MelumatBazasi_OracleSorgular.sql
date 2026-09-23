/* ============================================================================
   AML → «Məlumat Bazası» — 11 Oracle sorğusu  (SİYAHI ÜZRƏ SÜZGƏCLİ VARİANT)
   Mənbə: BMI FoxPro → melumat_bazasi_kodlari.prg
   Hədəf: FinNex → Risk → Məlumat Bazası (MelumatBazasiService)

   BU FAYLI SQL SERVER-DƏ (FinNex bazasında) İŞLƏT — Oracle-da YOX.
   Script TƏKRAR İŞLƏDİLƏ BİLƏR: mövcud AML_MB_* sətirləri YENİLƏNİR, yoxdursa
   əlavə olunur (Id və tarixçə qorunur).

   ── NƏ DƏYİŞDİ (22.09.2026) ────────────────────────────────────────────────
   Əvvəlki variant BMI-nin C# nüsxəsindən (BMI/AML/Sorgular/MelumatBazasi.cs)
   köçürülmüşdü və orada `aml_yoxlama` ÜMUMİYYƏTLƏ YOXDUR — yəni sorğular
   BÜTÜN dövrü qaytarırdı, siyahı üzrə süzmürdü. Əsl proqram FoxPro-dur və
   onun 11 sorğusunun HAMISI `odb.aml_yoxlama y` ilə birləşir.

   Oracle-a yazmaq QADAĞAN olduğu üçün (CLAUDE.md) `aml_yoxlama` cədvəli
   `{SIYAHI}` yer tutucusu ilə əvəz olunub — servis onu sətiriçi siyahıya
   çevirir və sütun adları EYNİ qalır (`y.a_s_a`, `y.fin`, `y.voen`, `y.tel`),
   ona görə BMI-nin `where` məntiqi olduğu kimi köçüb:

       ( select 'HUSEYNOV SAMIR MIRHUSEYN' a_s_a, '1EZVKMS' fin,
                '~' voen, '~' tel from dual
         union all select ... ) y

   ── YER TUTUCULARI ─────────────────────────────────────────────────────────
   {DOVREVVEL} , {DOVRSON}  →  servis `dd-MM-yyyy` formatında əvəz edir.
                               Format DƏYİŞDİRİLMƏMƏLİDİR (gün/ay yerdəyişər).
   {SIYAHI}                 →  Exceldən oxunan şəxslərin `from dual` siyahısı.
                               11 sorğunun HAMISINDA var.

   ⚠️ SİYAHIYA GEDƏN AD ASCII OLMALIDIR. `odb.func_utf8_to_latin` Azəri
   hərflərini ASCII-yə çevirir və müqayisə onun çıxışı ilə gedir:
        Ə→A   İ→I   Ü→U   Ö→O   Ş→S   Ç→C   Ğ→G
   (22.09.2026-da BMI-də ölçülüb: «ƏLİYEVA ÜLVİYYƏ ŞÖVQİ» → «ALIYEVA ULVIYYA SOVQI»)
   DİQQƏT: `Ə → A`-dır, `E` DEYİL. Layihədəki `Sadeles` metodu `ə→e` edir və
   BU İŞ ÜÇÜN YARAMIR — ayrıca `BmiLatin` metodu işlədilir. Səhv tərəf seçilsə
   heç bir xəta çıxmır, sadəcə heç nə tapılmır.

   ⚠️ Boş xanalar üçün `null` YOX, `'~'` sentineli göndərilir: Oracle-da `''`
   elə `null`-dır və `union all` qollarında tip qarışıqlığı yaradır; `'~'` isə
   heç bir real dəyərə bərabər deyil, şərt sakitcə söndürülür.
   «Yalnız FİN/VÖEN» rejimində `a_s_a` xanasına `'~~AD_YOXDUR~~'` yazılır —
   SQL mətni dəyişmir, sadəcə ad şərti heç vaxt tutmur.

   ⚠️ 11 sorğudan 4-ü DÖVRSÜZDÜR ({DOVREVVEL} yoxdur) — BMI-də də belədir:
      AML_MB_OWNER, AML_MB_ELAQELI_SEXS, AML_MB_KREDIT_ZAMIN, AML_MB_AKTIV_HESABLAR

   ── BMI-DƏKİ SƏHVLƏR — DÜZƏLDİLDİ ──────────────────────────────────────────
   #1 OWNER — mötərizə: `(A and B or C)` → `(A and (B or C))`.
      `odb.balschkli b` cədvəlinin YEGANƏ bağlantısı `substr(licsch,1,5) in b.balsch`
      idi; FİN uyğun gələndə o qol keçilir və `b` SƏRBƏST qalırdı → dekart hasili.
      `distinct` çıxışı təmizləyirdi, amma Oracle milyonlarla cütü qurub atırdı.
      NƏTİCƏ: sətir sayı BMI-dəki 4396-dan AZ olacaq — bu, düzəlişin özüdür.
   #2 OPEN_ACCOUNTS — `icra` sütunu `qey_nezaret`-dən gəlir (C# nüsxəsi onu atıb
      `log_accounts`-a hər sətir üçün iç-içə MAX qoymuşdu — yavaşlığın səbəbi).
   #3 OPEN_ACCOUNTS — `qey_nezaret` alt sorğusu `group by` ilə `qn` üzrə təkliyə
      salındı; əks halda LEFT JOIN hesab sətrini İKİLƏŞDİRİRDİ.
   #4 Rəqəm/mətn qarışığı — `substr(...)` mətn qaytarır, amma `in (10020,…)` və
      `substr(t.debet,1,4)=1005` rəqəm yazılmışdı (Exchange-də eyni sətirdə biri
      dırnaqlı, biri dırnaqsız!). Hamısı dırnağa alındı: nəticə eynidir, amma
      hesab nömrəsində hərf olan gün ORA-01722 vermir.
   #5 A_M_L — `to_date(doguldugu_tarix)` maskasız idi (NLS-dən asılı). Sütun
      VARCHAR2-dur və `DD-MM-YYYY` formatındadır (ölçülüb); üstəlik `regexp_like`
      qoruyucusu əlavə edildi ki, bir pozuq sətir bütün vərəqi sındırmasın.
   #6 Mənasız outer join-lar (`= r.regnom(+)` + `where`-də `r.*` şərti) adi
      join-a çevrildi — nəticə eynidir, plan sadələşir.
   #7 `y` ilə açar üzrə join YOXDUR (BMI-də də belə idi): Exceldə 2 adam eyni
      sətrə uyğun gəlsə sətir İKİ dəfə çıxır. QƏSDƏN SAXLANILIB — indi
      «Axtarılan şəxs» sütunu var, yəni iki sətir DÜZGÜNDÜR.

   ── 23.09.2026 DÜZƏLİŞLƏRİ (real Oracle icrasında tapılan 3 xəta) ───────────
   #8 KOCURME_DAXILI, KOCURME_MUSTERI — `ORA-00904: "T"."NAME_LICSCH": invalid
      identifier`. `t` = `odb.arh_dd` (əməliyyat sətri) — bu cədvəldə
      `name_licsch` sütunu YOXDUR, o, `odb.licsch`-dədir. FROM-a debet/kredit
      hesabları üçün `odb.licsch ld, odb.licsch lk` (outer join, `90_AML_
      OracleSorgular.sql`-dəki `p`/`s` aliası ilə EYNİ, sınanmış naxış:
      `t.debet = ld.licsch(+)`, `t.kredit = lk.licsch(+)`) əlavə edildi.
      Ad uyğunluğu indi HƏR İKİ hesabın (debet VƏ kredit) sahibinin adına
      baxır — `rd`/`rk` FİN/VÖEN cütü ilə EYNİ məntiq (bir tərəf uyğun
      gəlsə kifayətdir, müştəri gözdən qaçmasın deyə).
   #9 AKTIV_HESABLAR — `ORA-00979: not a GROUP BY expression`. `SELECT`
      siyahısındakı `uygunluq` (CASE) `y.fin`, `y.voen`, `y.tel`-ə istinad
      edirdi, `GROUP BY`-da isə yalnız `y.a_s_a` var idi. Hər `{SIYAHI}`
      sətri (a_s_a, fin, voen, tel) sabit dördlükdür, ona görə `y.fin,
      y.voen, y.tel`-i `GROUP BY`-a əlavə etmək qruplaşma dənəviliyini
      DƏYİŞMİR — sadəcə Oracle-un tələbini ödəyir.
      (A_M_L, Kred_zamin-dəki `ORA-01013` isə bug deyil — böyük siyahı ilə
      60 saniyəlik `CommandTimeout`-a çatma, `CLAUDE.md`-də sənədləşib.)

   ── FinNex ƏLAVƏLƏRİ (BMI-də yox idi) ──────────────────────────────────────
   * Hər vərəqdə 2 yeni sütun: `axtarilan` (Exceldən hansı sətir tutdu) və
     `uygunluq` (FİN / VÖEN / Telefon / Ad — hansı kriteriya işlədi).
   * KOCURME_MUSTERI — `name_licsch` (hesab sahibinin rəsmi adı) və
     `pincode_or_passport` (FİN) əlavə edildi. BMI yalnız `primechanie`
     (sərbəst mətn) üzrə axtarırdı və ata adı ora nadir hallarda yazıldığı
     üçün praktikada çox şey itirirdi.
   * TRANSFER — 4 mənbənin hamısı `odb.regnom`-a join edildi (`inn_regnom`,
     `pincode`). Bağlantı düsturları BMI-nin `frmhesabsorgu` formasından
     götürülüb. `odb.left/odb.right` ƏVƏZİNƏ standart `substr(lpad(...),18,6)`
     işlədilir — eyni nəticə, FOXPRO istifadəçisi üçün icazə asılılığı yoxdur.
     Join `(+)` (outer) olduğu üçün YALNIZ ƏLAVƏ EDİR, heç bir sətri atmır.
   * OWNER — `t.inn_licsch` üzrə VÖEN axtarışı.
   * A_M_L — 3-cü union qolunda `fin` sütunu ƏSLİNDƏ VÖEN saxlayır (BMI-də
     sütun adı yalan danışır). `kod_novu` ilə ayrıldı, yoxsa hüquqi şəxs
     heç vaxt tapılmırdı.
   * EMIT_BENEF, 3-cu shexs, A_M_L, Kred_zamin — TƏRS `LIKE` qolu əlavə edildi.
     Bazada ad 2 hissəlidir («SOYAD AD»), Exceldə 3 («SOYAD AD ATAADI») →
     `like '%3 hissə%'` heç vaxt tutmurdu. Tərs qolda `length >= 8` qoruyucusu
     MƏCBURİDİR, yoxsa qısa/zibil ad HƏR adama uyğun gələr.

   ── MƏLUM MƏHDUDİYYƏTLƏR (BMI datası ilə ölçülüb, 22.09.2026) ───────────────
   * `emitent_benefisiar.b_pincode` — nümunədə BOŞ → benefisiar FİN üzrə tapılmır.
   * `creditinfoguarantee.pincode` və `telefon` — nümunədə BOŞ → Kred_zamin
     praktikada YALNIZ ad üzrə işləyir. `guarantee_id` FİN deyil, PASPORTdur
     (AZE00277678, IIRG9301272 — ölkə prefiksi ilə).
   * `docfio` — FİN sütunu YOXDUR (`SSN`, `PASPORT` boş; `PASSPORT_ID` = 'AZE',
     yəni ölkə kodudur). `LAST_NAME/FIRST_NAME/MIDDLE_NAME` də boşdur → yalnız `FIO`.
   * `y.tel` hazırda HƏMİŞƏ '~'-dir: `AMLexcel.xlsx` şablonunda telefon sütunu
     yoxdur. Şərtlər silinmədi — şablona `tel` sütunu əlavə edilsə koda
     toxunmadan işə düşəcək.

   Yoxlama (ən sonda): 11 sətir, hamısında {SIYAHI} olmalıdır.
   ========================================================================== */

SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

/* Departament — 90_AML_OracleSorgular.sql ilə EYNİ üsul.
   Departament ADINA görə axtarmaq etibarsızdır («Risk», «Risk İdarəetməsi»…),
   NULL qayıtsa `DepartamentId` NOT NULL olduğu üçün hamısı sınardı. */
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
   comboboxunda görünsünmü bayrağı. İlk yazılışda ora kataloq ADI yazılmışdı
   və script 1-ci INSERT-də sınırdı:
       Msg 245 — Conversion failed when converting the nvarchar value
       'AML / Məlumat Bazası' to data type bit.
   Bu 11 sorğu modulun ÖZ ekranına aiddir → Kataloq = 0. */

DECLARE @Ad NVARCHAR(200), @Mahiyyet NVARCHAR(MAX), @Sql NVARCHAR(MAX);

/* ---------------------------------------------------------------- 1/11 */
SET @Ad       = N'AML_MB_OPEN_ACCOUNTS';
SET @Mahiyyet = N'Məlumat Bazası → Open_Accounts. Dövrdə açılmış hesablar (3x/4x) + hesabı açan icraçı. Uyğunluq: hesabın adı + sahibinin FİN/VÖEN-i (regnom join).';
SET @Sql      = N'select l.date_open_licsch                              ac_tar,
       nvl(n.icra, ''OLD_REGNOM'')                       icra,
       l.licsch                                        hn,
       odb.func_utf8_to_latin(l.name_licsch)           adi,
       y.a_s_a                                         axtarilan,
       case when trim(r.pincode)    = y.fin  then ''FİN''
            when trim(r.inn_regnom) = y.voen then ''VÖEN''
            else ''Ad'' end                              uygunluq
  from odb.licsch l, odb.regnom r,
       ( select trim(f.qn) qn, trim(f.icraci) icra
           from odb.qey_nezaret f
          where f.tarix between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                            and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
            and f.teyinat = ''INSERT''
            and f.qn is not null
          group by trim(f.qn), trim(f.icraci) ) n,
       ( {SIYAHI} ) y
 where l.date_open_licsch between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                              and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
   and substr(l.licsch,1,1) in (''3'',''4'')
   and substr(l.licsch,10,6) = n.qn(+)
   and l.registrac_nomer     = r.regnom(+)
   and ( odb.func_utf8_to_latin(upper(l.name_licsch)) like ''%'' || upper(y.a_s_a) || ''%''
      or trim(r.pincode)    = y.fin
      or trim(r.inn_regnom) = y.voen )
 order by l.date_open_licsch, l.licsch';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 2/11 */
SET @Ad       = N'AML_MB_KOCURME_DAXILI';
SET @Mahiyyet = N'Məlumat Bazası → Kochurme_Daxili_hes. Daxili hesablar arası köçürmələr. Uyğunluq: hesab sahibinin adı + FİN (pincode_or_passport) + hesab sahibinin FİN/VÖEN-i (regnom join).';
SET @Sql      = N'select t.date_oper                                     tarix,
       t.debet, t.kredit,
       t.summa_v_nacval                                mebleg,
       odb.func_utf8_to_latin(t.primechanie)           qeyd,
       y.a_s_a                                         axtarilan,
       case when t.pincode_or_passport = y.fin                       then ''FİN''
            when trim(rd.pincode)    = y.fin or trim(rk.pincode)    = y.fin  then ''FİN (hesab sahibi)''
            when trim(rd.inn_regnom) = y.voen or trim(rk.inn_regnom) = y.voen then ''VÖEN (hesab sahibi)''
            else ''Ad (hesab sahibi)'' end                              uygunluq
  from odb.arh_dd t, odb.regnom rd, odb.regnom rk, odb.licsch ld, odb.licsch lk, ( {SIYAHI} ) y
 where t.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                       and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
   and substr(t.debet,10,6)  = rd.regnom(+)
   and substr(t.kredit,10,6) = rk.regnom(+)
   and t.debet  = ld.licsch(+)
   and t.kredit = lk.licsch(+)
   and ( (substr(t.debet,1,5)  in (''10020'',''15025'',''35025'',''45021'',''45023'',''45029'',''45089'')
          and substr(t.kredit,1,5) in (''45021'',''45023'',''45029'',''45089''))
      or (substr(t.debet,1,5)  in (''45021'',''45023'',''45029'',''45089'')
          and substr(t.kredit,1,5) in (''10020'',''15025'',''35025'',''45021'',''45023'',''45029'',''45089'')) )
   and ( odb.func_utf8_to_latin(upper(ld.name_licsch)) like ''%'' || upper(y.a_s_a) || ''%''
      or odb.func_utf8_to_latin(upper(lk.name_licsch)) like ''%'' || upper(y.a_s_a) || ''%''
      or t.pincode_or_passport = y.fin
      or trim(rd.pincode)    = y.fin  or trim(rk.pincode)    = y.fin
      or trim(rd.inn_regnom) = y.voen or trim(rk.inn_regnom) = y.voen )
 order by t.recnum';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 3/11 */
SET @Ad       = N'AML_MB_KOCURME_MUSTERI';
SET @Mahiyyet = N'Məlumat Bazası → Kochurme_Mushteri_hes. Müştəri hesabları üzrə köçürmələr. Uyğunluq: FİN (pincode_or_passport), hesab sahibinin adı, qeyd mətni.';
SET @Sql      = N'select t.date_oper                                     tarix,
       t.debet, t.kredit,
       t.summa_v_inval                                 valuta,
       t.summa_v_nacval                                manat,
       odb.func_utf8_to_latin(t.primechanie)           qeyd,
       y.a_s_a                                         axtarilan,
       case when t.pincode_or_passport = y.fin then ''FİN''
            when trim(rd.pincode)    = y.fin  or trim(rk.pincode)    = y.fin  then ''FİN (hesab sahibi)''
            when trim(rd.inn_regnom) = y.voen or trim(rk.inn_regnom) = y.voen then ''VÖEN (hesab sahibi)''
            when odb.func_utf8_to_latin(upper(ld.name_licsch)) like ''%'' || upper(y.a_s_a) || ''%''
              or odb.func_utf8_to_latin(upper(lk.name_licsch)) like ''%'' || upper(y.a_s_a) || ''%''
                 then ''Ad (hesab sahibi)''
            else ''Ad (qeyd mətnində)'' end              uygunluq
  from odb.arh_dd t, odb.regnom rd, odb.regnom rk, odb.licsch ld, odb.licsch lk, ( {SIYAHI} ) y
 where t.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                       and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
   and substr(t.debet,10,6)  = rd.regnom(+)
   and substr(t.kredit,10,6) = rk.regnom(+)
   and t.debet  = ld.licsch(+)
   and t.kredit = lk.licsch(+)
   and ( (substr(t.debet,1,5)  in (''15025'',''35025'',''45021'',''45023'',''45029'',''45089'')
          and (substr(t.kredit,1,2) in (''38'',''39'',''40'',''41'')
            or substr(t.kredit,1,5) in (''35090'',''35100'')))
      or (substr(t.kredit,1,5) in (''15025'',''35025'',''45021'',''45023'',''45029'',''45089'')
          and (substr(t.debet,1,2)  in (''38'',''39'',''40'',''41'')
            or substr(t.debet,1,5)  in (''35090'',''35100''))) )
   and (t.vid_operacii < 96 or t.vid_operacii is null)
   and ( odb.func_utf8_to_latin(upper(t.primechanie))  like ''%'' || upper(y.a_s_a) || ''%''
      or odb.func_utf8_to_latin(upper(ld.name_licsch)) like ''%'' || upper(y.a_s_a) || ''%''
      or odb.func_utf8_to_latin(upper(lk.name_licsch)) like ''%'' || upper(y.a_s_a) || ''%''
      or t.pincode_or_passport = y.fin
      or trim(rd.pincode)    = y.fin  or trim(rk.pincode)    = y.fin
      or trim(rd.inn_regnom) = y.voen or trim(rk.inn_regnom) = y.voen )';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 4/11 */
SET @Ad       = N'AML_MB_EXCHANGE';
SET @Mahiyyet = N'Məlumat Bazası → Exchange. Valyuta alqı-satqısı (kassa 1005 <-> 1006). Uyğunluq: FİN (pincode_or_passport) və qeyd mətni.';
SET @Sql      = N'select t.date_oper                                     tarix,
       t.debet, t.kredit,
       t.summa_v_inval                                 valuta,
       t.summa_v_nacval                                manat,
       odb.func_utf8_to_latin(t.primechanie)           qeyd,
       y.a_s_a                                         axtarilan,
       case when t.pincode_or_passport = y.fin then ''FİN''
            else ''Ad (qeyd mətnində)'' end              uygunluq
  from odb.arh_dd t, ( {SIYAHI} ) y
 where t.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                       and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
   and ( (substr(t.debet,1,4) = ''1005'' and substr(t.kredit,1,4) = ''1006'')
      or (substr(t.debet,1,4) = ''1006'' and substr(t.kredit,1,4) = ''1005'') )
   and ( odb.func_utf8_to_latin(upper(t.primechanie)) like ''%'' || trim(upper(y.a_s_a)) || ''%''
      or t.pincode_or_passport = y.fin )
 order by t.recnum';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 5/11 */
SET @Ad       = N'AML_MB_OWNER';
SET @Mahiyyet = N'Məlumat Bazası → Owner. Hüquqi şəxs / sahibkar hesablarının təsisçiləri. Uyğunluq: təsisçinin FİN-i, şirkətin VÖEN-i, təsisçinin adı. DÖVRSÜZ.';
SET @Sql      = N'select distinct
       t.registrac_nomer                               rn,
       t.inn_licsch,
       odb.func_utf8_to_latin(g.name_regnom)           ad,
       t.countrycode                                   olke,
       t.licsch,
       odb.func_utf8_to_latin(r.owner_name)            own,
       r.owner_id, r.pincode,
       r.tesischinin_payi                              pay,
       odb.func_utf8_to_latin(r.countrycode)           vatan,
       case when g.yurik = 1 then ''Huquqi'' else ''Sahibkar'' end  nov,
       y.a_s_a                                         axtarilan,
       case when trim(r.pincode)     = y.fin  then ''FİN''
            when trim(t.inn_licsch)  = y.voen then ''VÖEN''
            else ''Ad (təsisçi)'' end                    uygunluq
  from odb.licsch t, odb.regnomowner r, odb.regnom g, odb.balschkli b, ( {SIYAHI} ) y
 where t.registrac_nomer = r.regnom
   and t.registrac_nomer = g.regnom
   and (g.yurik = 1 or g.predprinimatel = 1)
   and substr(t.licsch,1,5) in (b.balsch)
   and ( odb.func_utf8_to_latin(upper(r.owner_name)) like ''%'' || upper(y.a_s_a) || ''%''
      or trim(r.pincode)    = y.fin
      or trim(t.inn_licsch) = y.voen )
 order by t.registrac_nomer, t.licsch';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 6/11 */
SET @Ad       = N'AML_MB_EMIT_BENEF';
SET @Mahiyyet = N'Məlumat Bazası → Emit_benef. Əməliyyatın emitenti və benefisiarı. Uyğunluq: FİN (e_pincode/b_pincode) və ad (TƏRS LIKE — bazada 2 hissəli ad saxlanılır).';
SET @Sql      = N'select t.date_oper                                     tarix,
       odb.func_utf8_to_latin(trim(k.e_soyadi) || '' '' || trim(k.e_adi))  emi_ad,
       k.e_tevellud                                    em_dog_tar,
       trim(k.e_senedin_seriya_ve_nomresi)             emi_sened,
       odb.func_utf8_to_latin(trim(k.b_soyadi) || '' '' || trim(k.b_adi))  ben_ad,
       trim(k.b_senedin_seriya_ve_nomresi)             ben_sened,
       odb.func_utf8_to_latin(t.primechanie)           qeyd,
       y.a_s_a                                         axtarilan,
       case when y.fin in (k.e_pincode, k.b_pincode) then ''FİN''
            when upper(y.a_s_a) like ''%'' || odb.func_utf8_to_latin(
                   upper(trim(k.e_soyadi) || '' '' || trim(k.e_adi))) || ''%'' then ''Ad (emitent)''
            else ''Ad (benefisiar)'' end                 uygunluq
  from odb.arh_dd t, odb.emitent_benefisiar k, ( {SIYAHI} ) y
 where t.recnum = k.doc_id
   and t.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                       and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
   and ( upper(y.a_s_a) like ''%'' || odb.func_utf8_to_latin(
              upper(trim(k.e_soyadi) || '' '' || trim(k.e_adi))) || ''%''
      or upper(y.a_s_a) like ''%'' || odb.func_utf8_to_latin(
              upper(trim(k.b_soyadi) || '' '' || trim(k.b_adi))) || ''%''
      or y.fin in (k.e_pincode, k.b_pincode) )';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 7/11 */
SET @Ad       = N'AML_MB_UCUNCU_SEXS';
SET @Mahiyyet = N'Məlumat Bazası → 3-cu shexs. docfio üzrə 3-cü şəxs əməliyyatları. Uyğunluq: 3-cü şəxsin adı + HESAB SAHİBİNİN FİN/VÖEN-i (regnom join). docfio-nun ÖZ SSN/PASPORT sütunları boşdur.';
SET @Sql      = N'select t.date_oper                                     tarix,
       t.debet, t.kredit,
       t.summa_v_inval                                 valuta,
       t.summa_v_nacval                                manat,
       t.kurs_valuti                                   kurs,
       odb.func_utf8_to_latin(t.primechanie)           qeyd,
       odb.func_utf8_to_latin(t.fio)                   fio22,
       y.a_s_a                                         axtarilan,
       case when trim(rd.pincode)    = y.fin  or trim(rk.pincode)    = y.fin  then ''FİN (hesab sahibi)''
            when trim(rd.inn_regnom) = y.voen or trim(rk.inn_regnom) = y.voen then ''VÖEN (hesab sahibi)''
            else ''Ad (3-cü şəxs)'' end                       uygunluq
  from odb.docfio t, odb.regnom rd, odb.regnom rk, ( {SIYAHI} ) y
 where t.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                       and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
   and t.recnum is not null
   and substr(t.debet,10,6)  = rd.regnom(+)
   and substr(t.kredit,10,6) = rk.regnom(+)
   and ( odb.func_utf8_to_latin(upper(t.fio)) like ''%'' || upper(y.a_s_a) || ''%''
      or ( length(trim(t.fio)) >= 8
           and upper(y.a_s_a) like ''%'' || odb.func_utf8_to_latin(upper(trim(t.fio))) || ''%'' )
      or trim(rd.pincode)    = y.fin  or trim(rk.pincode)    = y.fin
      or trim(rd.inn_regnom) = y.voen or trim(rk.inn_regnom) = y.voen )';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 8/11 */
SET @Ad       = N'AML_MB_TRANSFER';
SET @Mahiyyet = N'Məlumat Bazası → Transfer. 4 mənbəli ödəmə/mədaxil. Uyğunluq: FİN və VÖEN (regnom join — FinNex ƏLAVƏSİ) + göndərən/alan adı.';
SET @Sql      = N'select t.tarix, t.emit_name, t.ben_name, t.amount, t.valuta, t.cmnt, t.voen, t.fin,
       y.a_s_a                                         axtarilan,
       case when trim(t.fin)  = y.fin  then ''FİN''
            when trim(t.voen) = y.voen then ''VÖEN''
            when upper(trim(t.emit_name)) like ''%'' || upper(y.a_s_a) || ''%'' then ''Ad (göndərən)''
            else ''Ad (alan)'' end                       uygunluq
  from (
    select v.date_oper                                 tarix,
           odb.func_utf8_to_latin(v.account_name)      emit_name,
           odb.func_utf8_to_latin(v.beneficiary_name)  ben_name,
           v.amount, v.currency valuta,
           odb.func_utf8_to_latin(v.comments)          cmnt,
           r.inn_regnom voen, r.pincode fin
      from odb.doc_vnesh_inval v, odb.regnom r
     where v.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                           and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
       and substr(v.account_no,10,6) = r.regnom(+)
    union all
    select v.date_oper,
           odb.func_utf8_to_latin(v.name_debet),
           odb.func_utf8_to_latin(v.name_credit),
           v.summa_v_nacval, ''AZN'', ''Odemeler '',
           r.inn_regnom, r.pincode
      from odb.doc_vnesh_nacval v, odb.regnom r
     where v.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                           and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
       and substr(v.debet,10,6) = r.regnom(+)
    union all
    select v.date_oper,
           odb.func_utf8_to_latin(v.sender_name),
           odb.func_utf8_to_latin(v.beneficiary_name),
           v.amount, v.currency, ''Daxilolma'',
           r.inn_regnom, r.pincode
      from odb.doc_vnesh_swift v, odb.regnom r
     where v.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                           and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
       and substr(lpad(v.beneficiary_account,28,''0''),18,6) = r.regnom(+)
    union all
    select v.date_oper,
           odb.func_utf8_to_latin(v.name_debet),
           odb.func_utf8_to_latin(v.kredit_name),
           v.sum1, ''AZN'', ''Daxilolma'',
           r.inn_regnom, r.pincode
      from odb.doc_vnesh_postupl v, odb.regnom r
     where v.date_oper between to_date(''{DOVREVVEL}'',''DD-MM-YYYY'')
                           and to_date(''{DOVRSON}'',''DD-MM-YYYY'')
       and substr(lpad(v.kredit,28,''0''),18,6) = r.regnom(+)
  ) t, ( {SIYAHI} ) y
 where ( upper(trim(t.emit_name)) like ''%'' || upper(y.a_s_a) || ''%''
      or upper(trim(t.ben_name))  like ''%'' || upper(y.a_s_a) || ''%''
      or trim(t.fin)  = y.fin
      or trim(t.voen) = y.voen )
 order by t.tarix, t.cmnt';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 9/11 */
SET @Ad       = N'AML_MB_ELAQELI_SEXS';
SET @Mahiyyet = N'Məlumat Bazası → A_M_L. Əlaqəli şəxslər / nümayəndələr / əlaqəli hüquqi şəxslər. 3-cü qolda fin sütunu VÖEN saxlayır — kod_novu ilə ayrılır. DÖVRSÜZ.';
SET @Sql      = N'select distinct
       t.regnom, t.a_s_a, t.fin, t.dog_tar, t.tip,
       y.a_s_a                                         axtarilan,
       case when t.kod_novu = ''FIN''  and t.fin = y.fin  then ''FİN''
            when t.kod_novu = ''VOEN'' and t.fin = y.voen then ''VÖEN''
            else ''Ad'' end                              uygunluq
  from (
    select t.regnom,
           odb.func_utf8_to_latin(upper(t.soyadi || '' '' || t.adi || '' '' || t.ata_adi)) a_s_a,
           odb.func_utf8_to_latin(t.fin) fin, ''FIN'' kod_novu,
           case when regexp_like(trim(t.doguldugu_tarix), ''^[0-9]{2}-[0-9]{2}-[0-9]{4}$'')
                then to_date(trim(t.doguldugu_tarix),''DD-MM-YYYY'') end dog_tar,
           ''ELA_SHEXS'' tip
      from odb.imza_huquqi_olan_shexsler t
    union all
    select t.regnom,
           odb.func_utf8_to_latin(upper(t.soyadi || '' '' || t.adi || '' '' || t.ata_adi)),
           odb.func_utf8_to_latin(t.fin), ''FIN'',
           case when regexp_like(trim(t.doguldugu_tarix), ''^[0-9]{2}-[0-9]{2}-[0-9]{4}$'')
                then to_date(trim(t.doguldugu_tarix),''DD-MM-YYYY'') end,
           ''NUMAYEN''
      from odb.numayende t
    union all
    select t.regnom,
           odb.func_utf8_to_latin(upper(t.adi)),
           odb.func_utf8_to_latin(t.voen), ''VOEN'',
           null,
           ''ELA_HUQUQ''
      from odb.elaqeli_huquqi_shexsler t
  ) t, ( {SIYAHI} ) y
 where length(trim(t.a_s_a)) > 0
   and ( upper(t.a_s_a) like ''%'' || upper(y.a_s_a) || ''%''
      or ( length(trim(t.a_s_a)) >= 8
           and upper(y.a_s_a) like ''%'' || upper(trim(t.a_s_a)) || ''%'' )
      or (t.kod_novu = ''FIN''  and t.fin = y.fin)
      or (t.kod_novu = ''VOEN'' and t.fin = y.voen) )
 order by t.regnom';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 10/11 */
SET @Ad       = N'AML_MB_KREDIT_ZAMIN';
SET @Mahiyyet = N'Məlumat Bazası → Kred_zamin. Açıq kreditlərin zaminləri. DİQQƏT: creditinfoguarantee.pincode və telefon əsasən BOŞDUR — praktikada yalnız ad işləyir. DÖVRSÜZ.';
SET @Sql      = N'select t.licschkre, t.subschkre                        sk,
       g.guarantee_id                                  id,
       odb.func_utf8_to_latin(g.guarantee_name)        zamin,
       t.summakre                                      mabl,
       g.pincode,
       y.a_s_a                                         axtarilan,
       case when trim(g.pincode) = y.fin then ''FİN'' else ''Ad (zamin)'' end  uygunluq
  from odb.licschkre t, odb.creditinfoguarantee g, ( {SIYAHI} ) y
 where t.licschkre = g.licschkre
   and t.subschkre = g.subschkre
   and t.date_close is null
   and t.bs_vbs is not null
   and g.guarantee_id is not null
   and ( odb.func_utf8_to_latin(upper(g.guarantee_name)) like ''%'' || trim(upper(y.a_s_a)) || ''%''
      or ( length(trim(g.guarantee_name)) >= 8
           and upper(y.a_s_a) like ''%'' || odb.func_utf8_to_latin(upper(trim(g.guarantee_name))) || ''%'' )
      or trim(g.pincode) = y.fin
      or trim(translate(nvl(g.telefon,''0''),''(-)'','' ''))
             like ''%'' || trim(translate(nvl(y.tel,''Telefon yoxdur''),''(-)'','' '')) || ''%'' )';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

/* ---------------------------------------------------------------- 11/11 */
SET @Ad       = N'AML_MB_AKTIV_HESABLAR';
SET @Mahiyyet = N'Məlumat Bazası → Aktiv_hesablar. Açıq hesabı olan müştərilər (müştəri başına 1 sətir). Dörd kriteriya: ad + FİN + VÖEN + telefon. DÖVRSÜZ.';
SET @Sql      = N'select min(t.date_open_licsch)                         ac_tar,
       r.regnom                                        rn,
       odb.func_utf8_to_latin(r.name_regnom)           adi,
       y.a_s_a                                         axtarilan,
       case when trim(r.pincode)    = y.fin  then ''FİN''
            when trim(r.inn_regnom) = y.voen then ''VÖEN''
            when trim(translate(nvl(r.mobilniy,''0''),''(-)'','' ''))
                 like ''%'' || trim(translate(nvl(y.tel,''Telefon yoxdur''),''(-)'','' '')) || ''%''
                 then ''Telefon''
            else ''Ad'' end                              uygunluq
  from odb.licsch t, odb.regnom r, ( {SIYAHI} ) y
 where t.registrac_nomer = r.regnom
   and length(t.licsch) = 20
   and t.date_close_licsch is null
   and ( odb.func_utf8_to_latin(upper(r.name_regnom)) like ''%'' || trim(upper(y.a_s_a)) || ''%''
      or trim(r.pincode)    = y.fin
      or trim(r.inn_regnom) = y.voen
      or trim(translate(nvl(r.mobilniy,''0''),''(-)'','' ''))
             like ''%'' || trim(translate(nvl(y.tel,''Telefon yoxdur''),''(-)'','' '')) || ''%'' )
 group by r.regnom, r.name_regnom, r.pincode, r.inn_regnom, r.mobilniy, y.a_s_a, y.fin, y.voen, y.tel
 order by r.regnom';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, SYSDATETIME(), 0);

COMMIT TRAN;
PRINT N'Məlumat Bazası sorğuları hazırdır (11 ədəd, siyahı üzrə süzgəcli).';

/* ── YOXLAMA — 11 sətir qayıtmalı, «Siyahı» sütununun HAMISI «var» olmalıdır ── */
SELECT SorguAdi, Aktiv, DepartamentId,
       CASE WHEN SorguMetni LIKE N'%{DOVREVVEL}%' THEN N'dövrlü' ELSE N'dövrsüz' END AS Dovr,
       CASE WHEN SorguMetni LIKE N'%{SIYAHI}%'    THEN N'var'    ELSE N'YOXDUR!' END AS Siyahi,
       LEN(SorguMetni) AS Uzunluq
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
   GERİ QAYTARMA — ŞƏRHDƏN ÇIXARIB AYRICA İŞLƏT.

       UPDATE OracleSorgular SET Silinib = 1
        WHERE SorguAdi LIKE N'AML_MB_%' AND ISNULL(Silinib,0) = 0;

   Sorğu MƏTNİNİ dəyişmək üçün silməyə ehtiyac yoxdur — Admin → Oracle Sorğular
   ekranından redaktə et. Servis onları ADA görə oxuyur, Id-yə görə yox.
   ========================================================================== */
