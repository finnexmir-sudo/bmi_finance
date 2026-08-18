/* ═══════════════════════════════════════════════════════════════════════════
   VM 98.2.1 — İŞÇİ KREDİT FAİZİ (Oracle sorğusu, YALNIZ SELECT)
   18.08.2026

   NƏ EDİR: verilmiş dövrdə işçi kreditləri (`tipzaloga = 10`) üzrə FAKTİKİ
   hesablanmış faizi müştəri kodu üzrə toplayır. Hesabi gəlir bu faizdən
   hesablanır — QALIQDAN YOX (bax: docs/kredit-fayda/VM_98_2_1_Isci_Krediti.md §3.2).

   İŞÇİ BAĞI: `regnom.pincode` = FIN → FinNex `Isciler.FIN`.
   18.08.2026-da 40 sətrin üzərində yoxlanıldı — tutuşan hər FIN simvol-simvol
   eyni çıxdı (11AN3Y1, 2FATQTQ, 1MMWSTV, 2GZ1KS3, 1E0B33, 4XJXXL7, 6JUNK3H).
   Ona görə əl ilə «müştəri kodu → işçi» cədvəli QURULMUR.

   ⚠️ LEFT JOIN — MƏCBURİ. `regnom`-da qarşılığı olmayan (və ya FIN-i «XX» kimi
   yanlış olan) kredit sətri sorğudan DÜŞMƏMƏLİDİR: ekranda «işçi tapılmadı»
   kimi görünüb mühasibin diqqətinə çatmalıdır. INNER JOIN yazsaq belə sətir
   səssizcə yox olar və hesabi gəlir əskik qalar.
   Real nümunə: `000091 NAZARİ MORTEZA AHMAD` → pincode = «XX».

   ⚠️ `date_close` şərti QƏSDƏN YOXDUR — dövrü provodkalar özü təyin edir:
   ay ortasında bağlanan kredit də, yeni götürülən də düşür; həmin ay faizi
   olmayan kredit isə `arh_dd`-də sətri olmadığı üçün onsuz da gəlmir.

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

IF NOT EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Isci Kredit Faizi' AND ISNULL(Silinib,0)=0)
INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
VALUES (N'Isci Kredit Faizi',
        N'VM 98.2.1 — işçi kreditləri üzrə {BAS}-{SON} dövründə hesablanmış faiz (adi + vaxtı keçmiş), FIN ilə birlikdə',
        N'select substr(l.licschpkre,10,6) as musteri_kodu,
       r.pincode                as fin,
       r.name_regnom            as musteri_adi,
       substr(l.licschpkre,6,2) as valyuta,
       l.subschkre              as subkod,
       l.procstavkre            as isci_faizi,
       l.procstav_19            as vk_faizi,
       sum(case when a.debet = l.licschpkre  then a.summa_v_nacval else 0 end) as faiz_adi,
       sum(case when a.debet = l.licschppkre then a.summa_v_nacval else 0 end) as faiz_vk
from   odb.licschkre l
join   odb.arh_dd a on  a.ssd = l.subschkre
                    and (   (a.debet = l.licschpkre  and a.kredit = l.trlicschkre)
                         or (a.debet = l.licschppkre and a.kredit = l.trlicsch_19) )
left join odb.regnom r on r.regnom = substr(l.licschpkre,10,6)
where  l.tipzaloga = 10
  and  a.date_oper between to_date(''{BAS}'',''dd/mm/yyyy'') and to_date(''{SON}'',''dd/mm/yyyy'')
group by substr(l.licschpkre,10,6), r.pincode, r.name_regnom,
         substr(l.licschpkre,6,2), l.subschkre, l.procstavkre, l.procstav_19
order by 1', 1, @DepId, GETDATE(), 0);

COMMIT;
GO

/* ── YOXLAMA: nə yazıldı ─────────────────────────────────────────────────── */
SELECT Id, SorguAdi, Aktiv, DepartamentId
FROM   OracleSorgular
WHERE  SorguAdi = N'Isci Kredit Faizi';
GO
