/* ============================================================================
   ÖS-6.1 — STATİSTİK VAHİDİN MÜŞTƏRİ HESABLARI HAQQINDA STATİSTİK HESABAT
   (AMB "Ödəniş Statistikası" Əlavə 6.1)  | Mühasibat — OracleSorgular INSERT/UPDATE
   (SQL Server-də işlət, Oracle-da YOX). Script TƏKRAR İŞLƏDİLƏ BİLƏR — artıq
   mövcud olan sətri YENİLƏYİR (IF EXISTS...UPDATE), yoxdursa yaradır.

   Mənbə: mühasibin "OVS yeni 6.1.xlsm" iş kitabının "Açılmış hesablar" /
   "Ümumi hesablar" / "Kredit hesablari" xam ixraclarının "_mod" vərəqlərində
   MID() ilə hesab nömrəsindən çıxardığı sütunlar — Oracle-da bunlar
   `odb.licsch.licsch` sütununun SUBSTR-i ilə BİRƏ-BİR eynidir (18.11.2026-da
   canlı sınanıb):

     MID(hesab,2,2)  (Klass)       = SUBSTR(licsch,1,2)
     MID(hesab,2,3)  (Qrup)        = SUBSTR(licsch,1,3)
     MID(hesab,2,5)  (Bölmə)       = SUBSTR(licsch,1,5)
     MID(hesab,4,1)  (Rezerv=9)    = SUBSTR(licsch,3,1)
     MID(hesab,6,1)  (Kredit=0/5)  = SUBSTR(licsch,5,1)
     MID(hesab,7,2)  (Valyuta)     = SUBSTR(licsch,6,2)
     MID(hesab,11,6) (Regnom kodu) = SUBSTR(licsch,10,6)

   ("Hesab" Exceldə öndə BOŞLUQLU sətirdir — ' 38939000000000400000' — ona görə
   Excel MID-i bizim SUBSTR-dən 1 mövqe irəlidədir.)

   Excel "Qalıq" sütunu — SALDO_ISH_NACVAL (AZN) və ya SALDO_ISH_INVAL (FX,
   valyuta≠AZN) ABS dəyəridir (18.11.2026-da canlı sınanıb). ÖS-6.1-in ÖZÜ
   pul məbləği YAZMIR (yalnız say), AMMA "ondan aktiv hesablar üzrə" (H/J/K
   sütunları) Qalıq>0 şərtinə görə seçilir — ona görə qaliq_nacval/qaliq_inval
   sorğuya əlavə edilib, C# tərəfdə Qalıq = (valyuta=='00') ? ABS(nacval) :
   ABS(inval) kimi hesablanacaq.

   "Hesabat ayı üzrə" = date_open_licsch dövr daxilindədir (sabit tarixi fakt).
   "Dövrün sonuna cəmi" = dövrün SON gününə account mövcud idi — yəni
     date_open_licsch <= {D2} AND (date_close_licsch IS NULL OR date_close_licsch > {D2}).
   Bu, retroaktiv (keçmiş ay üçün) hesabatda da doğru nəticə verir, çünki
   açılma/bağlanma tarixləri licsch-də dəyişməyən tarixi faktlardır.

   {D1} / {D2} — servis tərəfindən dd-MM-yyyy formatında əvəz edilir
   (dövrün 1-ci günü / son günü).

   ⚠️ YALNIZ-OXU — Oracle-a heç nə yazılmır (SELECT, CLAUDE.md-nin "Oracle
   Verilənlər Bazası" qaydasına uyğun).
   ========================================================================== */

SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

SELECT TOP 1 @DepId = Id
FROM   Departamentler
WHERE  (Ad LIKE N'%ühasib%' OR Ad LIKE N'%aliyy%')
  AND  ISNULL(Silinib,0) = 0
ORDER BY Id;

IF @DepId IS NULL
BEGIN
    INSERT INTO Departamentler (Ad, Aciqlama, YaradilmaTarixi, Silinib)
    VALUES (N'Mühasibatlıq', N'Maliyyə / mühasibat hesabatları', GETDATE(), 0);
    SET @DepId = SCOPE_IDENTITY();
END

DECLARE @Ad NVARCHAR(200), @Mahiyyet NVARCHAR(MAX), @Sql NVARCHAR(MAX);

SET @Ad       = N'Muhasibat — OS6.1 Hesablar';
SET @Mahiyyet = N'ÖS-6.1 (AMB) — müştəri hesabları haqqında statistik hesabat. licsch-dən hesab sayı/müştəri sayı hesablamaq üçün xam sətirlər ({D1}=ayın 1-i, {D2}=ayın son günü).';
SET @Sql = N'
select
/* =====================================================================
   ÖS-6.1 — xam hesab sətirləri (dövr: {D1} .. {D2}, dd-mm-yyyy)
   Hər sətir = bir licsch qeydi, Klass/Qrup/Bölmə/Valyuta/Regnom hesab
   nömrəsinin SUBSTR-i ilə əvvəlcədən hesablanıb (mühasibin MID() düsturunun
   Oracle qarşılığı).
   ===================================================================== */
    l.licsch                      hesab,
    substr(l.licsch,1,2)          klass,
    substr(l.licsch,1,3)          qrup,
    substr(l.licsch,1,5)          bolme,
    substr(l.licsch,3,1)          rezerv_flag,
    substr(l.licsch,5,1)          kredit_flag,
    substr(l.licsch,6,2)          valyuta,
    substr(l.licsch,10,6)         regnom_kod,
    l.name_licsch                 adi,
    l.inn_licsch                  voen,
    l.date_open_licsch            acilma_tarixi,
    l.date_close_licsch           baglama_tarixi,
    l.saldo_ish_nacval             qaliq_nacval,
    l.saldo_ish_inval              qaliq_inval,
    case when l.date_open_licsch between to_date(''{D1}'',''dd-mm-yyyy'') and to_date(''{D2}'',''dd-mm-yyyy'')
         then 1 else 0 end        acilib_bu_ay,
    case when l.date_open_licsch <= to_date(''{D2}'',''dd-mm-yyyy'')
          and (l.date_close_licsch is null or l.date_close_licsch > to_date(''{D2}'',''dd-mm-yyyy''))
         then 1 else 0 end        movcud_dovr_sonu,
/* 18.11.2026 — KREDİT üçün "açıqdır" licsch.date_close_licsch-dən YOX,
   arh_licschkre.date_close-dan (kredit müqaviləsinin ÖZ bağlanma tarixi,
   canlı sınanıb: licsch 643, arh_licschkre 289 — licsch ledger sətri
   bağlanmır, AMMA müqavilə artıq bağlı ola bilir). Korrelə skalyar
   alt-sorğu (JOIN YOX) ki, arh_licschkre-də eyni (licschkre,date_oper)
   üçün birdən çox subschkre sətri olsa belə əsas cədvəl SƏTIR SAYI
   ARTMASIN — subschkre=0 (və ya ən kiçik) üstün tutulur. */
    (select count(*) from odb.arh_licschkre al
      where al.licschkre = l.licsch
        and al.date_oper = to_date(''{D2}'',''dd-mm-yyyy''))  kredit_arh_tapildi,
    (select max(al.date_close) keep (dense_rank first order by nvl(al.subschkre,0))
       from odb.arh_licschkre al
      where al.licschkre = l.licsch
        and al.date_oper = to_date(''{D2}'',''dd-mm-yyyy''))  kredit_arh_baglama
from odb.licsch l
where l.date_open_licsch is not null
  and l.date_open_licsch <= to_date(''{D2}'',''dd-mm-yyyy'')
/* 18.11.2026 — SÜRƏT: D1-dən ƏVVƏL bağlanmış hesab bu hesabata heç vaxt
   daxil ola BİLMƏZ — nə "bu ay açılan" (açılma D1-dən əvvələ düşərdi), nə
   "dövrün sonuna açıq" (artıq D2-dən əvvəl bağlanıb). ⚠️ NULL-U QORUYUCUSU
   MƏCBURİDİR: `date_close_licsch >= D1` TƏK yazılsa, hazırda AÇIQ (NULL)
   hesabların HAMISI SƏSSİZCƏ rədd olunar (Oracle-da NULL>=tarix UNKNOWN-dur,
   WHERE bunu keçirmir) — "IS NULL OR" olmadan bu filtr əksər sətri silərdi. */
  and (l.date_close_licsch is null or l.date_close_licsch >= to_date(''{D1}'',''dd-mm-yyyy''))
';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1, DepartamentId = @DepId
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Muhasibat — OS6.1 Hesablar sorğusu yazıldı/yeniləndi.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH
