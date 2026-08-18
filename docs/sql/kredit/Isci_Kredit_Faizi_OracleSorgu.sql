/* ═══════════════════════════════════════════════════════════════════════════
   VM 98.2.1 — İŞÇİ KREDİT FAİZİ (Oracle sorğuları, YALNIZ SELECT)
   18.08.2026

   İKİ AYRI SORĞU — QƏSDƏN. Birində məbləğ, birində ad/FIN.

   NİYƏ BİRLƏŞDİRİLMİR: `regnom`-a join edib `pincode`-u GROUP BY-a salsaq və
   `regnom`-da eyni koda İKİ sətir olsa, join `arh_dd` sətirlərini ikiqat edir →
   hər qrup tam məbləği toplayır → FAİZ İKİQAT görünür. Məbləği ad axtarışından
   asılı etmək olmaz: ad qatı səhv olsa da rəqəm düz qalmalıdır.

   Ona görə:
     1) «Isci Kredit Faizi» — MƏBLƏĞ. `regnom`-a TOXUNMUR. Dövr parametrli.
     2) «Isci Kredit FIN»   — AD/FIN xəritəsi. DISTINCT, parametrsiz, ~40 sətir.
        Xəritədə tapılmayan kod ekranda «işçi tapılmadı» kimi görünür, cəmə düşmür.

   İŞÇİ BAĞI: `regnom.pincode` = FIN → FinNex `Isciler.FIN`.
   18.08.2026-da ölçüldü — tutuşan hər FIN simvol-simvol eyni çıxdı
   (11AN3Y1, 2FATQTQ, 1MMWSTV, 2GZ1KS3, 1E0B33, 4XJXXL7, 6JUNK3H).
   Əl ilə «müştəri kodu → işçi» cədvəli QURULMUR.

   ⚠️ `date_close` şərti QƏSDƏN YOXDUR — dövrü provodkalar özü təyin edir:
   ay ortasında bağlanan kredit də, yeni götürülən də düşür; həmin ay faizi
   olmayan kredit isə `arh_dd`-də sətri olmadığı üçün onsuz da gəlmir.
   Köhnə/bağlı müştərilər (2-ci sorğuda 40 sətir) 1-ci sorğuya düşmür.

   PARAMETRLƏR: {BAS} / {SON} — dd/mm/yyyy. Dövr = [sonuncu maaş günü … cari gün−1].
   ═══════════════════════════════════════════════════════════════════════════ */

USE FinNex_Maliyye_Db;
GO

BEGIN TRAN;

DECLARE @DepId INT;
SELECT @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari' AND ISNULL(Silinib,0)=0;

IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler
    WHERE (Ad LIKE N'%ühasib%' OR Ad LIKE N'%aliyy%') AND ISNULL(Silinib,0)=0 ORDER BY Id;

/* ── 1. MƏBLƏĞ — dövrdə hesablanmış faiz (regnom-a TOXUNMUR) ─────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Isci Kredit Faizi' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Isci Kredit Faizi',
        N'VM 98.2.1 — işçi kreditləri üzrə {BAS}-{SON} dövründə hesablanmış faiz (adi + vaxtı keçmiş)',
        N'select substr(l.licschpkre,10,6) as musteri_kodu,
       substr(l.licschpkre,6,2)  as valyuta,
       l.subschkre               as subkod,
       l.procstavkre             as isci_faizi,
       l.procstav_19             as vk_faizi,
       sum(case when a.debet = l.licschpkre  then a.summa_v_nacval else 0 end) as faiz_adi,
       sum(case when a.debet = l.licschppkre then a.summa_v_nacval else 0 end) as faiz_vk
from   odb.licschkre l
join   odb.arh_dd a on  a.ssd = l.subschkre
                    and (   (a.debet = l.licschpkre  and a.kredit = l.trlicschkre)
                         or (a.debet = l.licschppkre and a.kredit = l.trlicsch_19) )
where  l.tipzaloga = 10
  and  a.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
group by substr(l.licschpkre,10,6), substr(l.licschpkre,6,2),
         l.subschkre, l.procstavkre, l.procstav_19
order by 1', 1, @DepId, GETDATE(), 0);

/* ── 2. AD/FIN XƏRİTƏSİ — parametrsiz, DISTINCT ──────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Isci Kredit FIN' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Isci Kredit FIN',
        N'VM 98.2.1 — işçi kredit müştəri kodu → FIN/ad xəritəsi (Isciler.FIN ilə bağlamaq üçün)',
        N'select distinct substr(l.licschpkre,10,6) as musteri_kodu,
       r.pincode     as fin,
       r.name_regnom as musteri_adi
from   odb.licschkre l
join   odb.regnom r on r.regnom = substr(l.licschpkre,10,6)
where  l.tipzaloga = 10
order by 1', 1, @DepId, GETDATE(), 0);

COMMIT;
GO

/* ── YOXLAMA ─────────────────────────────────────────────────────────────── */
SELECT Id, SorguAdi, Aktiv, DepartamentId
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Isci Kredit Faizi', N'Isci Kredit FIN')
ORDER  BY SorguAdi;
GO
