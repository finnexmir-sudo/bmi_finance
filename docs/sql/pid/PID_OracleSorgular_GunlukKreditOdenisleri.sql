/* ============================================================================
   PID — Gündəlik Kredit Ödənişləri (Ödənişə Nəzarət, 3-cü tab)
   ----------------------------------------------------------------------------
   TƏKRAR İŞLƏDİLƏ BİLƏR — sorğu "PID Odenis Nezaret Gunluk" adı ilə ARTIQ
   varsa, mətni YENİLƏNİR (INSERT YOX, UPDATE); yoxdursa yaradılır.

   ARXA FON (2026-11-17 müzakirəsi, canlı Oracle ilə sınanıb təsdiqlənib):

   1. docdna — CANLI, YALNIZ BUGÜNKÜ gündür (arxivi arh_dd-dir), tarix filtri
      lazım deyil — cədvəlin özü artıq bugünlə məhdudlaşır.

   2. "Gələn ödəniş" ÜÇ yoldan ola bilər (debet tərəfi):
        - KASSA     : SUBSTR(debet,1,3) IN ('100','250','110')
        - CARİ HESAB: SUBSTR(debet,1,1) IN ('3','4')   — müştərinin öz/başqa cari hesabı
        - KÖÇÜRMƏ   : otvet_ispoln = 0
      LENGTH(debet)=20 qoruyucusu əlavə olunub ki, qısa/texniki GL kodu (məs. "35")
      səhvən "3/4 sinfi" sayılmasın (hesab nömrələri bu bazada həmişə 20 simvoldur —
      bax Muhasibat_OracleSorgular_Kredit_ArhTarix.sql-dəki LENGTH(licschkre)=20 eyni qoruyucu).

   3. KREDİT tərəfi İKİ HALDA ola bilər — real ödəniş HƏR İKİSİNDƏ "ödənilib" sayılır:
        a) BİRBAŞA kreditin öz hesabına düşür (licschkre/licschpkre) —
           "Hesabı" sütunu "(birbaşa)" yazır.
        b) MÜŞTƏRİNİN CARİ hesabına düşür, kreditə hələ KEÇMƏYİB (iki addımlı axının 1-ci
           addımı) — "Hesabı" sütunu "Cari hesaba daxil olub (kreditə hələ keçməyib)" yazır.
      Uyğunlaşdırma ona görə MÜŞTƏRİ REGNOM-u üzrə gedir (hesabın öz 10-15-ci simvolları),
      BİRBAŞA hesab bərabərliyi İLƏ YOX — bu, (a)-nı da, (b)-ni də ƏHATƏ EDİR (əgər d.kredit
      HƏQİQƏTƏN licschkre-yə bərabərdirsə, substr-i də trivial olaraq bərabərdir).
      Real nümunə ilə TƏSDİQLƏNİB: debet=25019000000000300006 → kredit=41040000001972000000
      (müştərinin cari hesabı, kredit hesabının ÖZÜ YOX) — bu sorğu onu düzgün tapır,
      "Hesabı"="Cari hesaba daxil olub (kreditə hələ keçməyib)" ilə.

   4. Yalnız AÇIQ kreditlər (l.date_close IS NULL) — bağlı kreditə düşən ödəniş əlavə
      yoxlama tələb edir, v1-də göstərilmir.

   5. Gecikmə — YALNIZ əsas borc üzrə (l.summa_19 > 0 → "Gecikir"). Faiz gecikməsi
      (view_nacpogprokre_all + tar_ferq360) performans problemlərinə görə v1-dən
      ÇIXARILIB (custom PL/SQL funksiyası hər sətir üçün işə düşür, MATERIALIZE/
      ROW_NUMBER() ilə düzəldilə bilər, amma bu sorğuya hələ əlavə edilməyib).

   6. "Status" (Tam bağlanıb/Problemli, BMI-nin frm_odenis_ferqlisilinme.cs-indəki
      in=out tutuşdurması) BU SORĞUDA YOXDUR — köhnə məntiq d.kredit-in ÖZ in/out
      axınına baxırdı, bu artıq SƏHVDİR (d.kredit tez-tez müştərinin CARİ hesabıdır,
      kreditin hesabı YOX). Düzgün status krediti ÖZ GL hesabına (l.licschkre və s.)
      görə YENİDƏN qurulmalıdır — bu sorğuda İCAD EDİLMƏYİB, AÇIQ İŞ.

   7. tipzal/tipkre LEFT JOIN-dir (BMI-nin köhnə INNER JOIN-i kodu olmayan krediti
      səssizcə atırdı — CLAUDE.md-dəki dəfələrlə sənədləşmiş dərs).

   8. (17.11.2026, İSTİFADƏÇİ QƏRARI) "Vaxtı keçmiş əsas (birbaşa)" / "Vaxtı keçmiş
      faiz (birbaşa)" sətirləri (d.kredit = l.licsch_19 / l.licschppkre) OUTER WHERE-də
      XARİC EDİLİR. Kök səbəb: bunlar GƏLƏN YENİ pul DEYİL — müştərinin artıq (eyni
      gün, eyni "Cari hesaba daxil olub" sətri ilə) SAYILMIŞ cari hesab vəsaiti kreditin
      ÖZ vaxtı-keçmiş alt-hesabına (overdue sub-ledger) DAXİLİ YENİDƏN TƏSNİFATIDIR —
      BMI-nin köhnə "fərqli silinmə" (frm_odenis_ferqlisilinme.cs) formasının izlədiyi
      məhz bu hərəkətdir. İstifadəçinin öz sözü: "biz yalniz odenisleri gosterek,
      silinmeleri ayrica gostermek maraqli deyil."

   9. (17.11.2026, EYNİ GÜN, İKİNCİ DALĞA — KRİTİK) Çox-kreditli REGNOM-da eyni ödəniş
      (eyni RECNUM) BİRDƏN ÇOX sətirdə göstərilirdi — SUBSTR(d.kredit,10,6)=SUBSTR(
      l.licschkre,10,6) join-i REGNOM üzrə gedir, müştərinin HƏR açıq krediti ilə
      ayrı-ayrı uyğunlaşır (cartesian-tərzi fan-out). İki fərqli zərər:
        a) Əgər d.kredit müştərinin BİR krediti üçün BİRBAŞA uyğun gəlirsə (Əsas/Faiz),
           AMMA müştərinin İKİNCİ bir açıq krediti DƏ varsa — o İKİNCİ kredit üçün
           d.kredit heç bir sütuna (licschkre/licschpkre/licsch_19/licschppkre) uyğun
           GƏLMİR, CASE ELSE-ə düşür və SƏHVƏN "Cari hesaba daxil olub" kimi FANTOM
           bir sətir yaradır — halbuki pul artıq BİRİNCİ kreditə BİRBAŞA düşüb.
        b) Əgər pul həqiqətən CARİ hesabdadır (heç bir krediti birbaşa toxunmayıb) VƏ
           müştərinin 2+ açıq krediti varsa — EYNİ "Cari hesaba daxil olub" sətri HƏR
           kredit üçün AYRI-AYRI təkrarlanır — nəticə YALANÇI İKİQAT (və ya ÇOXQAT)
           ÖDƏNİŞ kimi görünür və Cəmdə eyni pul bir neçə dəfə sayılır.
      Real nümunə (Excel ixracı, 17.11.2026): ƏLİYEV NİZAM ÇALI OĞLU (regnom 019738,
      iki açıq krediti — biri DAŞINMAZ ƏMLAK girovlu, biri F/Ş TƏR.VER.ZƏMANƏT girovlu)
      üçün EYNİ 500,00 ödənişi İKİ sətirdə (hər kreditə bir) "Cari hesaba daxil olub"
      kimi göstərilirdi. İstifadəçinin öz sözü: "eyni setiri iki defe gosterib,sebeb
      ise hemin regnomum iki aciq krediti var...yalanci iki odenis demis oluruq."

      HƏLL — bir RECNUM = bir SƏTIR (bir docdna qeydi HƏMİŞƏ bir fiziki əməliyyatdır,
      bir neçə krediti real cür BÖLÜNMÜŞ ödəniş kimi TƏMSİL ETMİR — belə bölgü ayrı-
      ayrı RECNUM-larla gələrdi). ROW_NUMBER() OVER (PARTITION BY recnum ORDER BY
      CASE WHEN hesabi=''Cari hesaba...'' THEN 1 ELSE 0 END, kat) = 1 ilə:
        - ƏGƏR o RECNUM üçün İSTƏNİLƏN kreditə BİRBAŞA uyğunluq varsa (Əsas/Faiz/Vaxtı
          keçmiş) — O sətir QALİB gəlir, fantom "Cari hesaba daxil olub" sətirləri
          (a-bəndindəki) AVTOMATİK ATILIR;
        - ƏGƏR HEÇ bir birbaşa uyğunluq yoxdursa (b-bəndi, HƏQİQƏTƏN qeyri-müəyyən) —
          yalnız BİR kredit (sıralamada "kat" üzrə ilk gələn) TƏMSİLÇİ kimi saxlanılır,
          qalanlar atılır — Cəmdə artıq İKİQAT sayılmır.
      YENİ "coxaday" bayrağı (0/1) — YALNIZ b-bəndindəki HƏQİQƏTƏN qeyri-müəyyən halda
      1 olur (count(*) over(partition by recnum) > 1 VƏ qazanan sətir "Cari hesaba
      daxil olub"dursa) — a-bəndindəki fantom-atma heç bir "qeyri-müəyyənlik" DEYİL
      (artıq dəqiq bilinir hansı kreditə düşüb), ona görə bayraq orada 0 qalır.
      ⚠️ GERİYƏ TƏTBİQ OLUNMUR — yalnız canlı docdna üzərində işləyən BU GÜNÜN sorğusu.

   Servis sorğunu ada görə tapır: adında "Odenis" + "Nezaret" + "Gunluk"
   (normalizasiya: Azərbaycan hərfləri ASCII-yə, boşluqlar silinir). Adi siyahı VƏ
   Gray list axtarışlarına "gunluk" İSTİSNASI əlavə edilib (OdenisNezaretiService.cs)
   ki, üç sorğu bir-birini örtmesin.

   Oracle YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT, @Ad NVARCHAR(200), @Mahiyyet NVARCHAR(MAX), @Sql NVARCHAR(MAX);

SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  (SorguAdi LIKE N'%PID%' OR SorguAdi LIKE N'%Nezaret%' OR SorguAdi LIKE N'%Nəzarət%')
  AND  ISNULL(Silinib,0)=0
ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler WHERE ISNULL(Silinib,0)=0 ORDER BY Id;

SET @Ad       = N'PID Odenis Nezaret Gunluk';
SET @Mahiyyet = N'Gündəlik gələn kredit ödənişləri — docdna (kassa/cari hesab/köçürmə → kredit, regnom üzrə). "Vaxtı keçmiş" sətirləri xaricdir; eyni RECNUM çox-kreditli müştəridə artıq TƏK sətrə yığılır (yalançı ikiqat ödəniş yoxdur).';
SET @Sql      = N'SELECT tarix, debet, kredit, hesabi, mebleg, recnum, teyinat, qeyd_no, adi, kat, novu, tipi, kredit_bagli_tarixi, vk_esas, gecikme, coxaday
  FROM (
    SELECT x.*,
           ROW_NUMBER() OVER (PARTITION BY x.recnum
                ORDER BY CASE WHEN x.hesabi = N''Cari hesaba daxil olub (kreditə hələ keçməyib)'' THEN 1 ELSE 0 END, x.kat) AS rn,
           CASE WHEN COUNT(*) OVER (PARTITION BY x.recnum) > 1
                     AND x.hesabi = N''Cari hesaba daxil olub (kreditə hələ keçməyib)''
                THEN 1 ELSE 0 END AS coxaday
      FROM (
        SELECT d.date_oper AS tarix,
               d.debet          AS debet,
               d.kredit         AS kredit,
               CASE WHEN d.kredit = l.licschkre   THEN N''Əsas (birbaşa)''
                    WHEN d.kredit = l.licschpkre  THEN N''Faiz (birbaşa)''
                    WHEN d.kredit = l.licsch_19   THEN N''Vaxtı keçmiş əsas (birbaşa)''
                    WHEN d.kredit = l.licschppkre THEN N''Vaxtı keçmiş faiz (birbaşa)''
                    ELSE N''Cari hesaba daxil olub (kreditə hələ keçməyib)''
               END               AS hesabi,
               d.summa_v_nacval  AS mebleg,
               d.recnum          AS recnum,
               d.primechanie     AS teyinat,
               r.regnom          AS qeyd_no,
               r.name_regnom     AS adi,
               l.licschkre       AS kat,
               g.name            AS novu,
               t.name            AS tipi,
               l.date_close      AS kredit_bagli_tarixi,
               l.summa_19        AS vk_esas,
               CASE WHEN NVL(l.summa_19,0) > 0 THEN N''Gecikir'' ELSE N''Vaxtında'' END AS gecikme
          FROM odb.docdna d, odb.licschkre l, odb.regnom r, odb.tipzal g, odb.tipkre t
         WHERE SUBSTR(d.kredit,10,6) = SUBSTR(l.licschkre,10,6)
           AND l.date_close IS NULL
           AND r.regnom(+) = SUBSTR(l.licschkre,10,6)
           AND l.tipzaloga = g.code(+)
           AND l.tipkredita = t.code(+)
           AND (
                 (LENGTH(d.debet) = 20 AND (SUBSTR(d.debet,1,3) IN (''100'',''250'',''110'') OR SUBSTR(d.debet,1,1) IN (''3'',''4'')))
                 OR d.otvet_ispoln = 0
               )
      ) x
  )
 WHERE rn = 1
   AND hesabi NOT IN (N''Vaxtı keçmiş əsas (birbaşa)'', N''Vaxtı keçmiş faiz (birbaşa)'')
 ORDER BY tarix DESC, adi';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'PID Gündəlik Kredit Ödənişləri sorğusu yazıldı/yeniləndi — "Vaxtı keçmiş" sətirləri xaricdir, eyni RECNUM çox-kreditli müştəridə tək sətrə yığılır.';

SELECT SorguAdi, DepartamentId, Aktiv FROM OracleSorgular
WHERE  SorguAdi = N'PID Odenis Nezaret Gunluk';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
