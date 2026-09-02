/* ═══════════════════════════════════════════════════════════════════════════
   KREDİT ARAYIŞLARI — Oracle sorğuları (YALNIZ SELECT) · 02.09.2026

   BMI-dəki iki formanın sorğularının qarşılığı:
     · «Arayış Borcalan» ← `frmborcalantemizlik` (axtarış: QEYDİYYAT KODU / regnom)
     · «Arayış Zamin»    ← `zaminarayis`         (axtarış: ZAMİNİN FİN kodu)

   BMI-də bu SQL-lər formanın içində, TextBox mətni birbaşa yapışdırılmaqla
   qurulurdu — inyeksiyaya açıq idi. Burada SQL Admin panelində saxlanılır,
   axtarış dəyəri isə servisdə TƏMİZLƏNİR (regnom → yalnız rəqəm, FİN → yalnız
   hərf/rəqəm), sonra `{REGNOM}` / `{PINCODE}` yer tutucusu əvəz olunur.

   ⚠️ SÜTUN ADLARI («AS ...») KOD İLƏ BAĞLIDIR — `KreditArayisService` onları
   ADINA görə oxuyur (ADI, HESABNO, KS, TARIX, KREDIT, QALIQ, MUQAVILE_NO,
   ZAMIN). Adı dəyişsəniz sahə SƏSSİZCƏ boş qalar, heç bir xəta çıxmaz.

   ⚠️ VALYUTA sorğuda YOXDUR — kod onu `HESABNO`-nun 7–8-ci simvolundan çıxarır
   (BMI ilə eyni: 00→AZN, 01→USD, 02→AVRO). `kod_valuti` sütunu İŞLƏDİLMİR:
   o, INTEGER-dir və mətn müqayisəsində ORA-00932 verir (CLAUDE.md).

   ⚠️ `summakre` = MÜQAVİLƏ məbləği, `summa` = cari QALIQ. Arayışa müqavilə
   məbləği düşür (BMI də belədir) — qarışdırmayın (CLAUDE.md).
   ═══════════════════════════════════════════════════════════════════════════ */

USE FinNex_Maliyye_Db;
GO

SET NOCOUNT ON;
BEGIN TRAN;

/* Departament: mövcud kredit sorğusunun departamentini götürürük ki, sorğular
   Admin panelində eyni yerdə görünsün. Tapılmasa Kredit departamenti axtarılır. */
DECLARE @DepId INT;
SELECT @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi = N'Kredit Müqavilə' AND ISNULL(Silinib,0)=0;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler
    WHERE Ad LIKE N'%redit%' AND ISNULL(Silinib,0)=0 ORDER BY Id;

IF @DepId IS NULL
BEGIN
    ROLLBACK TRAN;
    THROW 50001, N'Departament tapılmadı — @DepId əl ilə təyin edilməlidir.', 1;
END

/* ── 1. ARAYIŞ BORCALAN — qeydiyyat kodu (regnom) üzrə kreditlər ─────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Arayış Borcalan' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
  N'Arayış Borcalan',
  N'Borcalan təmizlik arayışı — qeydiyyat kodu (regnom) üzrə müştərinin kreditləri. Yer tutucu: {REGNOM}',
  N'select r.name_regnom  as ADI,
       t.licschkre    as HESABNO,
       t.subschkre    as KS,
       t.date_open    as TARIX,
       t.summakre     as KREDIT,
       t.summa        as QALIQ,
       k.nomer_lsk    as MUQAVILE_NO
  from odb.licschkre t, odb.regnom r, odb.srokpogprockre k, odb.licsch m
 where substr(t.licschkre, 10, 6) = ''{REGNOM}''
   and substr(t.licschkre, 10, 6) = r.regnom
   and t.licschkre = k.licschkre
   and t.subschkre = k.subschkre
   and m.licsch    = k.licsch_3(+)
 order by t.subschkre desc',
  1, 0, @DepId, SYSDATETIME(), 0);

/* ── 2. ARAYIŞ ZAMIN — zaminin FİN kodu üzrə zaminlikləri ────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Arayış Zamin' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (
  N'Arayış Zamin',
  N'Zamin təmizlik arayışı — zaminin FİN kodu (creditinfoguarantee.pincode) üzrə zaminlikləri. Yer tutucu: {PINCODE}',
  N'select r.name_regnom     as ADI,
       t.licschkre       as HESABNO,
       t.subschkre       as KS,
       g.guarantee_name  as ZAMIN,
       t.date_open       as TARIX,
       t.summakre        as KREDIT,
       t.summa           as QALIQ
  from odb.licschkre t, odb.regnom r, odb.creditinfoguarantee g,
       odb.srokpogprockre k, odb.licsch m
 where lower(g.pincode) = lower(''{PINCODE}'')
   and substr(t.licschkre, 10, 6) = r.regnom
   and t.licschkre = g.licschkre
   and t.subschkre = g.subschkre
   and t.licschkre = k.licschkre
   and t.subschkre = k.subschkre
   and m.licsch    = k.licsch_3(+)
 order by t.subschkre desc',
  1, 0, @DepId, SYSDATETIME(), 0);

COMMIT TRAN;

/* ── Yoxlama ─────────────────────────────────────────────────────────────── */
SELECT Id, SorguAdi, Aktiv, DepartamentId, LEN(SorguMetni) AS SqlUzunlugu
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Arayış Borcalan', N'Arayış Zamin') AND ISNULL(Silinib,0)=0;

/* ═══════════════════════════════════════════════════════════════════════════
   BMI-DƏN FƏRQLƏR — QƏSDƏNDİR

   1. `like` → `=` (zamin sorğusunda). BMI `lower(g.pincode) like lower('...')`
      yazırdı, amma forma heç vaxt `%` göndərmirdi — yəni faktiki olaraq bərabərlik
      idi. `like` saxlansaydı istifadəçi FİN xanasına `%` yazıb BÜTÜN zaminlikləri
      çəkə bilərdi. Açıq axtarış lazım olsa bu ayrıca qərardır.

   2. `odb.licsch m` join-u BMI-də var, amma ondan HEÇ BİR SÜTUN seçilmir —
      outer join olduğu üçün sətir sayına da təsir etmir. Faydasızdır, amma
      SİLİNMƏYİB: BMI-nin sorğusu ilə hərfi-hərfinə eyni qalsın deyə. Sabah
      nəticələr fərqlənsə «biz nəyisə dəyişmişik» sualı yaranmasın.

   3. `date_close` şərti YOXDUR — bağlanmış kredit üçün arayış verilir, elə
      arayışın mənası da odur ki, borc bağlanıb. Açıq/bağlı filtri qoymaq
      SƏHV olardı.
   ═══════════════════════════════════════════════════════════════════════════ */
