/* ═══════════════════════════════════════════════════════════════════════════
   KREDİT ARAYIŞLARI — sıralama düzəlişi · 02.09.2026

   NƏ DƏYİŞİR: «Arayış Borcalan» və «Arayış Zamin» sorğularının YALNIZ
   `ORDER BY` hissəsi.

       KÖHNƏ:  order by t.subschkre desc              (KS üzrə)
       YENİ:   order by t.date_open desc, t.subschkre desc   (verilmə tarixi üzrə)

   NİYƏ: KS (subschkre) artım sırası tarixlə həmişə uyğun gəlmir. Real nümunə
   (regnom 000087, 11 kredit): KS=0 olan kredit 04.06.2014 tarixlidir və KS-ə
   görə siyahının ƏN SONUNA düşürdü, halbuki tarixcə 2013 və 2012-dən sonra
   gəlməli idi. İstifadəçi tələbi: yenidən köhnəyə doğru, verilmə tarixi üzrə.
   KS ikinci açar kimi qalır — eyni gündə verilmiş kreditlərin sırası sabit olsun.

   ⚠️ NİYƏ AYRICA SKRİPT: `Arayis_OracleSorgular.sql` sorğuları
   `IF NOT EXISTS` ilə əlavə edir — sətir onsuz da varsa TƏKRAR İŞLƏTMƏK HEÇ NƏ
   DƏYİŞMİR. Mövcud sətri yeniləmək üçün bu UPDATE lazımdır.

   BU SKRİPT YALNIZ SORĞU MƏTNİNƏ TOXUNUR — heç bir kredit/məktub datası
   dəyişmir, Oracle-a heç nə yazılmır.
   ═══════════════════════════════════════════════════════════════════════════ */

USE FinNex_Maliyye_Db;
GO

SET NOCOUNT ON;
BEGIN TRAN;

/* ── ƏVVƏLCƏ: indi nə var? ───────────────────────────────────────────────── */
SELECT Id, SorguAdi, Aktiv,
       KohneOrderBy = RIGHT(SorguMetni, 60)
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Arayış Borcalan', N'Arayış Zamin') AND ISNULL(Silinib,0)=0;

/* ── 1. ARAYIŞ BORCALAN ──────────────────────────────────────────────────── */
UPDATE OracleSorgular
SET    SorguMetni = N'select r.name_regnom  as ADI,
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
 order by t.date_open desc, t.subschkre desc',
       YenilenmeTarixi = SYSDATETIME()
WHERE  SorguAdi = N'Arayış Borcalan' AND ISNULL(Silinib,0)=0;

/* ── 2. ARAYIŞ ZAMIN ─────────────────────────────────────────────────────── */
UPDATE OracleSorgular
SET    SorguMetni = N'select r.name_regnom     as ADI,
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
 order by t.date_open desc, t.subschkre desc',
       YenilenmeTarixi = SYSDATETIME()
WHERE  SorguAdi = N'Arayış Zamin' AND ISNULL(Silinib,0)=0;

COMMIT TRAN;

/* ── SONRA: yoxlama — hər ikisində «date_open desc» görünməlidir ─────────── */
SELECT Id, SorguAdi, Aktiv,
       YeniOrderBy = RIGHT(SorguMetni, 60),
       DuzgunmuKi  = CASE WHEN SorguMetni LIKE N'%order by t.date_open desc%'
                          THEN N'BƏLİ' ELSE N'XEYR' END
FROM   OracleSorgular
WHERE  SorguAdi IN (N'Arayış Borcalan', N'Arayış Zamin') AND ISNULL(Silinib,0)=0;

/* Yeni quraşdırmalar üçün mənbə fayl da yeniləndi:
   docs/sql/kredit/Arayis_OracleSorgular.sql
   Yəni bu UPDATE yalnız ARTIQ QURULMUŞ bazalar üçündür. */
