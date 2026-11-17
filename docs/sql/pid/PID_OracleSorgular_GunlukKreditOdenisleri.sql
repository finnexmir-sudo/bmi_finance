/* ============================================================================
   PID — Gündəlik Kredit Ödənişləri (Ödənişə Nəzarət, 3-cü tab)
   ----------------------------------------------------------------------------
   Bir sorğu əlavə edir (idempotent — varsa təkrar yaratmır):
     "PID Odenis Nezaret Gunluk" — docdna-dan GÜNDƏLİK gələn kredit ödənişləri.

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
        a) BİRBAŞA kreditin öz hesabına düşür (licschkre/licschpkre/licsch_19/licschppkre) —
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
      (eyni RECNUM-la). UI/operator bunu RECNUM-a görə tanıya bilər. Çox-kreditli müştəri
      tək sətirə yığılsın-yığılmasın hələ qərar verilməyib.

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
      görə YENİD�ən qurulmalıdır — bu sorğuda İCAD EDİLMƏYİB, AÇIQ İŞ.

   7. tipzal/tipkre LEFT JOIN-dir (BMI-nin köhnə INNER JOIN-i kodu olmayan krediti
      səssizcə atırdı — CLAUDE.md-dəki dəfələrlə sənədləşmiş dərs).

   Servis sorğunu ada görə tapır: adında "Odenis" + "Nezaret" + "Gunluk"
   (normalizasiya: Azərbaycan hərfləri ASCII-yə, boşluqlar silinir). Adi siyahı VƏ
   Gray list axtarışlarına "gunluk" İSTİSNASI əlavə edilib (OdenisNezaretiService.cs)
   ki, üç sorğu bir-birini örtmesin.

   Oracle YALNIZ SELECT (CLAUDE.md).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

SELECT TOP 1 @DepId = DepartamentId FROM OracleSorgular
WHERE  (SorguAdi LIKE N'%PID%' OR SorguAdi LIKE N'%Nezaret%' OR SorguAdi LIKE N'%Nəzarət%')
  AND  ISNULL(Silinib,0)=0
ORDER BY Id;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler WHERE ISNULL(Silinib,0)=0 ORDER BY Id;

IF NOT EXISTS (SELECT 1 FROM OracleSorgular
               WHERE SorguAdi = N'PID Odenis Nezaret Gunluk' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
    N'PID Odenis Nezaret Gunluk',
    N'Gündəlik gələn kredit ödənişləri — docdna (kassa/cari hesab/köçürmə → kredit, regnom üzrə).',
    N'SELECT d.date_oper AS tarix,
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
 ORDER BY d.date_oper DESC, r.name_regnom',
    1, 0, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'PID Gündəlik Kredit Ödənişləri sorğusu əlavə olundu.';

SELECT SorguAdi, DepartamentId, Aktiv FROM OracleSorgular
WHERE  SorguAdi = N'PID Odenis Nezaret Gunluk';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH
