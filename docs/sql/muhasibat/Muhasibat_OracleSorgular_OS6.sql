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
with
/* =====================================================================
   ÖS-6.1 — xam hesab sətirləri (dövr: {D1} .. {D2}, dd-mm-yyyy)
   Hər sətir = bir licsch qeydi, Klass/Qrup/Bölmə/Valyuta/Regnom hesab
   nömrəsinin SUBSTR-i ilə əvvəlcədən hesablanıb (mühasibin MID() düsturunun
   Oracle qarşılığı).
   ===================================================================== */
/* 18.11.2026, DÖRDÜNCÜ DALĞA — "lupa dönür, çox fırlanır" (sürət bugu).
   Kök səbəb: ÜÇÜNCÜ DALĞA-nın düzəlişi (IN 4 sütun + date_oper<=D2) HƏR
   BİR `odb.licsch` SƏTRİ ÜÇÜN AYRI-AYRI, 2 KORRELƏ alt-sorğu kimi icra
   olunurdu — hər çağırışda `al.date_oper<=D2` (RANGE) + `l.licsch IN
   (4 sütun)` (heç bir indeksdən keçməyən bərabərlik) arxivin TAM
   tarixçəsini TƏZƏDƏN oxuyurdu. Arxiv GÜNDƏLİK yazıldığı üçün (bax
   yuxarı, ÜÇÜNCÜ DALĞA) bu cədvəl böyükdür — nəticə O(licsch sətr sayı ×
   arxivin tam tarixçəsi) idi, "lupa" şikayətinin dəqiq səbəbi budur.
   Düzəliş — NƏTİCƏ DƏYİŞMİR, YALNIZ İCRA PLANI dəyişir: dörd sütun
   (licschkre/licschpkre/licsch_19/licschppkre) BİR DƏFƏYƏ tək "acc"
   sütununa normallaşdırılır (arh_norm), BİR DƏFƏYƏ hesab üzrə
   qruplaşdırılır (arh_agg — "tapıldımı" VƏ "D2-yə qədər HƏMİN HESABIN
   ÖZ ən son `date_oper`-inə görə `date_close`" — köhnə korrelə sorğunun
   DƏQİQ EYNİ semantikası, HƏR hesab üçün AYRICA hesablanır, cədvəlin
   qlobal son tarixinə MƏHDUDLAŞDIRILMIR ki, D2-dən əvvəl bağlanıb
   arxivdən düşmüş hesablar səhvən "tapılmadı" olmasın), SONRA `l.licsch`
   bu KİÇİK, ƏVVƏLCƏDƏN hazır nəticə ilə TƏK dəfə JOIN olunur.
   "Hər sətr üçün tam arxiv taraması" (N×M) → "bütöv sorğu üçün BİR
   aqreqasiya + BİR join" (N+M). arh_agg-dəki MATERIALIZE hinti Oracle-un
   bunu YENİDƏN korrelə formaya "unmerge" etməsinin (bax CLAUDE.md-dəki
   "Oracle View Merging" qaydası) qarşısını alır. */
/* 18.11.2026, ALTINCI DALĞA — materialize+join-dən SONRA DA "lupa dönür"
   davam etdi (istifadəçinin öz canlı testi). Kök səbəb: arh_agg-in qrup-
   laşdırdığı arh_norm CTE-si `odb.arh_licschkre`-nin TAM tarixçəsini (illər
   boyu, HƏR iş günü, 4x UNPIVOT olunmuş) materiallaşdırırdı — O(M) bir dəfə
   olsa belə, M özü ÇOX BÖYÜK idi.
   Düzəliş: arh_norm-un hər 4 qoluna `date_oper > D2-31` aşağı həddi əlavə
   edildi (bir təqvim ayı pəncərəsi, D2-dən geriyə). Bu TƏHLÜKƏSİZDİR:
   "dövrün sonuna" sualı YALNIZ D2 ANINDAKI vəziyyətə aiddir — HƏQİQƏTƏN
   açıq qalan hər kredit arxivin GÜNDƏLİK yazdığı son bir neçə iş günündə
   (31 gün bunu geniş örtür) YENİDƏN görünür; pəncərədə HEÇ TAPILMAYAN
   hesab isə ya həqiqətən yoxdur, ya da çox əvvəl bağlanıb arxiv onu artıq
   yeniləmir — HƏR İKİ HALDA `KreditHesabiAktivDovrSonu`-nun `KreditArh
   Tapildi` şərti onsuz da false qaytarıb "açıq sayılmır" nəticəsinə
   gətirir, yəni YANLIŞ-NEQATİV riski YOXDUR.
   ⚠️ Bu, arxivin HƏR açıq kredit üçün HƏR iş günü (fəaliyyətindən asılı
   olmayaraq) sətir yazdığı fərziyyəsinə əsaslanır — PİD-in gündəlik ödəniş
   modulunda artıq canlı TƏSDİQLƏNMİŞ naxışdır (bax CLAUDE.md). Əgər "açıq
   kredit" sayı bu düzəlişdən sonra GÖZLƏNİLMƏDƏN azalsa, əvvəlcə bu
   fərziyyəni yoxla — pəncərəni genişləndirmək (31→60) ilk addım olmalıdır. */
arh_norm as (
    select licschkre acc, date_oper, date_close, subschkre
      from odb.arh_licschkre
     where date_oper >  to_date(''{D2}'',''dd-mm-yyyy'') - 31
       and date_oper <= to_date(''{D2}'',''dd-mm-yyyy'')
    union all
    select licschpkre acc, date_oper, date_close, subschkre
      from odb.arh_licschkre
     where date_oper >  to_date(''{D2}'',''dd-mm-yyyy'') - 31
       and date_oper <= to_date(''{D2}'',''dd-mm-yyyy'')
    union all
    select licsch_19 acc, date_oper, date_close, subschkre
      from odb.arh_licschkre
     where date_oper >  to_date(''{D2}'',''dd-mm-yyyy'') - 31
       and date_oper <= to_date(''{D2}'',''dd-mm-yyyy'')
    union all
    select licschppkre acc, date_oper, date_close, subschkre
      from odb.arh_licschkre
     where date_oper >  to_date(''{D2}'',''dd-mm-yyyy'') - 31
       and date_oper <= to_date(''{D2}'',''dd-mm-yyyy'')
),
arh_agg as (
    select /*+ materialize */
           acc,
           max(date_close) keep (dense_rank first order by date_oper desc, nvl(subschkre,0) asc) date_close
      from arh_norm
     where acc is not null
     group by acc
)
select
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
    case when son.acc is not null then 1 else 0 end          kredit_arh_tapildi,
    son.date_close                                           kredit_arh_baglama
from odb.licsch l
left join arh_agg son on son.acc = l.licsch
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
