/* ============================================================================
   AMB A1.1 — ROLL-FORWARD diaqnostik (kredit qalığının mərhələlərarası dəyişməsi)
   ----------------------------------------------------------------------------
   Dövr: cari ilin əvvəli (2025 sonu snapshot) → hesabat tarixi (30-06-2026).
   İki snapshot loan-səviyyəsində tutuşdurulur; hər kredit üçün:
     • açılış mərhələsi (os) + açılış qalığı (obal)
     • bağlanış mərhələsi (cs) + bağlanış qalığı (cbal)
   os NULL  → dövr ərzində VERİLMİŞ (yeni kredit)
   cs NULL  → dövr ərzində tam ÖDƏNİLMİŞ/bağlanmış
   os≠cs    → mərhələ KÖÇÜRMƏSİ

   YOXLAMA: bağlanış qalığı (cs üzrə cəm) A1 rəqəmlərimizlə tutuşmalıdır:
     Stage1 ≈ 2 391 132 · Stage2 ≈ 49 451 · Stage3 ≈ 748 931  (AZN)
   PL/SQL Developer → SQL Window → F8. (30-06-2026 içindədir)
   ============================================================================ */
WITH acilis_snap AS (
    SELECT MAX(date_oper) d FROM arh_licschkre
    WHERE date_oper < TRUNC(TO_DATE('30-06-2026','dd/mm/yyyy'),'YYYY')   -- 2025 sonu
),
baglanis_snap AS (
    SELECT MAX(date_oper) d FROM arh_licschkre
    WHERE date_oper <= TO_DATE('30-06-2026','dd/mm/yyyy')
),
acilis AS (
    SELECT ar.licschpkre, ar.subschkre, ar.index_otrasli sahe,
           CASE WHEN odb.tar_ferq360(x.date_oper,NVL(x.lastoverduedate,x.date_oper)) BETWEEN 0 AND 30 THEN 1
                WHEN odb.tar_ferq360(x.date_oper,NVL(x.lastoverduedate,x.date_oper)) BETWEEN 31 AND 90 THEN 2
                ELSE 3 END st,
           (ar.summa+ar.summa_19)*ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6) bal
    FROM acilis_snap cs JOIN arh_licschkre ar ON ar.date_oper=cs.d
    LEFT JOIN view_nacpogprokre_all x ON x.licschpkre=ar.licschpkre AND x.subschkre=ar.subschkre AND x.date_oper=ar.date_oper
    WHERE ar.date_close IS NULL AND LENGTH(ar.licschkre)=20
),
baglanis AS (
    SELECT ar.licschpkre, ar.subschkre, ar.index_otrasli sahe,
           CASE WHEN odb.tar_ferq360(x.date_oper,NVL(x.lastoverduedate,x.date_oper)) BETWEEN 0 AND 30 THEN 1
                WHEN odb.tar_ferq360(x.date_oper,NVL(x.lastoverduedate,x.date_oper)) BETWEEN 31 AND 90 THEN 2
                ELSE 3 END st,
           (ar.summa+ar.summa_19)*ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6) bal
    FROM baglanis_snap cs JOIN arh_licschkre ar ON ar.date_oper=cs.d
    LEFT JOIN view_nacpogprokre_all x ON x.licschpkre=ar.licschpkre AND x.subschkre=ar.subschkre AND x.date_oper=ar.date_oper
    WHERE ar.date_close IS NULL AND LENGTH(ar.licschkre)=20
),
birlesme AS (
    SELECT NVL(a.sahe,b.sahe) sahe, a.st os, b.st cs, NVL(a.bal,0) obal, NVL(b.bal,0) cbal
    FROM acilis a FULL OUTER JOIN baglanis b
      ON a.licschpkre=b.licschpkre AND a.subschkre=b.subschkre
)
SELECT NVL(TO_CHAR(os),'YENİ') acilis_merhele,
       NVL(TO_CHAR(cs),'BAGLANDI') baglanis_merhele,
       COUNT(*) say,
       ROUND(SUM(obal),2) acilis_qaliq,
       ROUND(SUM(cbal),2) baglanis_qaliq
FROM birlesme
GROUP BY os, cs
ORDER BY os NULLS LAST, cs NULLS LAST;
