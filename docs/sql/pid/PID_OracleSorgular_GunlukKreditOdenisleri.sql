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

      ⚠️ AÇIQ QALAN TRADEOFF: subschkre (l.subschkre=d.ssk) join-i QƏSDƏN YOXDUR — çünki
      kredit hesabına birbaşa düşməyən ödənişdə SSK mənalı deyil. Nəticə: eyni müştərinin
      BİRDƏN ÇOX aktiv krediti varsa, BİR ödəniş HƏR kredit üçün AYRI SƏTIR kimi çıxır
      (eyni RECNUM-la). UI/operator bunu RECNUM-a görə tanıya bilər (CoxKreditliFanOut
      bayrağı, 17.11.2026 — bax Gunluk.cshtml). Çox-kreditli müştəri tək sətirə
      yığılsın-yığılmasın hələ qərar verilməyib.

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
      məhz bu hərəkətdir. Göstərilsə, EYNİ məbləğ cəmdə İKİ-ÜÇ dəfə sayılır (real nümunə:
      müştəri A, 500,00 — "Cari hesaba daxil olub" (500) + "Vaxtı keçmiş faiz (birbaşa)"
      (500, iki kreditə fan-out ilə İKİ sətir) = görünən cəmdə 1500, real daxil olan 500).
      İstifadəçinin öz sözü: "biz yalniz odenisleri gosterek, silinmeleri ayrica
      gostermek maraqli deyil." Filtr "hesabi" CASE-dən sonra, ALT SORĞUda tətbiq
      olunur (NULL-müqayisə tələsinə düşməmək üçün — computed string heç vaxt NULL
      ola bilməz, əks halda "col <> l.licsch_19" birbaşa yazılsaydı licsch_19 NULL
      olan sətirlər səssizcə atıla bilərdi).
      ⚠️ GERİYƏ TƏTBİQ OLUNMUR — bu, YALNIZ canlı docdna üzərində işləyən BU GÜNÜN
      sorğusudur, arxiv datasına təsiri yoxdur. Əsas (birbaşa)/Faiz (birbaşa)/Cari
      hesaba daxil olub sətirlərinə TOXUNULMADI.

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
SET @Mahiyyet = N'Gündəlik gələn kredit ödənişləri — docdna (kassa/cari hesab/köçürmə → kredit, regnom üzrə). "Vaxtı keçmiş" (overdue daxili yenidən təsnifatı) sətirləri xaricdir — yalnız real ödənişlər.';
SET @Sql      = N'SELECT tarix, debet, kredit, hesabi, mebleg, recnum, teyinat, qeyd_no, adi, kat, novu, tipi, kredit_bagli_tarixi, vk_esas, gecikme
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
  )
 WHERE hesabi NOT IN (N''Vaxtı keçmiş əsas (birbaşa)'', N''Vaxtı keçmiş faiz (birbaşa)'')
 ORDER BY tarix DESC, adi';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'PID Gündəlik Kredit Ödənişləri sorğusu yazıldı/yeniləndi — "Vaxtı keçmiş" sətirləri artıq xaricdir.';

SELECT SorguAdi, DepartamentId, Aktiv FROM OracleSorgular
WHERE  SorguAdi = N'PID Odenis Nezaret Gunluk';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
