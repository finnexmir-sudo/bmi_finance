/* ============================================================================
   IFRS 9 ECL — DİAQNOSTİK: cari EAD-ın nə qədəri tarixi risk %-ə uyğun gəlir?
   ----------------------------------------------------------------------------
   Məqsəd: hər stage-də "risk_uygun = 2_UYGUNLUQ_YOX" sətrinin EAD-ı böyükdürsə,
   deməli o kreditlər 0.0001 (≈0) alır və ECL az çıxır. Fix: fallback = floor.
   PL/SQL Developer → yeni SQL Window → hamısını yapışdır → F8. (17-02-2026 içindədir)
   ============================================================================ */
WITH iller AS (
    SELECT EXTRACT(YEAR FROM ADD_MONTHS(TRUNC(TO_DATE('17-02-2026','DD-MM-YYYY'),'YYYY'),
             -12*(LEVEL-1))) AS il
    FROM dual CONNECT BY LEVEL <= 5
),
son_tarixler AS (
    SELECT i.il, MAX(ar.date_oper) AS son_tarix
    FROM iller i
    JOIN arh_licschkre ar ON EXTRACT(YEAR FROM ar.date_oper)=i.il
                          AND ar.date_oper <= TO_DATE('17-02-2026','DD-MM-YYYY')
    GROUP BY i.il
),
portfel AS (
    SELECT st.il, ar.licschpkre, ar.subschkre,
           i.index_otrasli AS sahe_kodu, i.name_index_otrasli AS sahe_adi,
           CASE WHEN odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)) BETWEEN 0 AND 30 THEN 'Stage 1'
                WHEN odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)) BETWEEN 31 AND 90 THEN 'Stage 2'
                ELSE 'Stage 3' END AS stage,
           (ar.summa+ar.summa_19)*ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6) AS qaliq
    FROM son_tarixler st
    JOIN arh_licschkre ar ON ar.date_oper=st.son_tarix
    JOIN view_nacpogprokre_all x ON x.licschpkre=ar.licschpkre
                                AND x.subschkre=ar.subschkre
                                AND x.date_oper=ar.date_oper
    JOIN index_otrasli i ON i.index_otrasli=ar.index_otrasli
    WHERE ar.date_close IS NULL
      AND ar.date_open >= ADD_MONTHS(TRUNC(TO_DATE('17-02-2026','DD-MM-YYYY'),'YYYY'), -12*4)
),
kechid AS (
    SELECT p1.il AS il_start, p1.licschpkre, p1.subschkre, p1.sahe_kodu,
           p1.stage AS stage_start, NVL(p2.stage,'Baglanib') AS stage_next,
           p1.qaliq AS qaliq_start, NVL(p2.qaliq,0) AS qaliq_next
    FROM portfel p1
    LEFT JOIN portfel p2 ON p1.licschpkre=p2.licschpkre
                        AND p1.subschkre=p2.subschkre
                        AND p2.il=p1.il+1
),
trans AS (
    SELECT licschpkre, subschkre, il_start, sahe_kodu, stage_start,
           SUM(qaliq_start) AS f,
           SUM(CASE WHEN stage_next='Stage 1' THEN qaliq_next ELSE 0 END) AS g,
           SUM(CASE WHEN stage_next='Stage 2' THEN qaliq_next ELSE 0 END) AS h,
           SUM(CASE WHEN stage_next='Stage 3' THEN qaliq_next ELSE 0 END) AS i_col,
           SUM(CASE WHEN stage_next='Baglanib' THEN qaliq_start ELSE 0 END) AS j,
           SUM(CASE WHEN stage_next IN ('Stage 1','Stage 2','Stage 3') THEN (qaliq_start-qaliq_next) ELSE 0 END) AS k
    FROM kechid
    GROUP BY licschpkre, subschkre, il_start, sahe_kodu, stage_start
),
recovery AS (
    SELECT
      NVL(AVG(CASE WHEN sahe_kodu NOT IN (1902,1904) AND stage_start='Stage 3' AND f>0 THEN (j+k)/f END),0)    AS p2,
      NVL(AVG(CASE WHEN sahe_kodu     IN (1902,1904) AND stage_start='Stage 3' AND f>0 THEN (j+k)/f END),0.75) AS q2
    FROM trans
),
riskrow AS (
    SELECT t.sahe_kodu, t.stage_start, t.f,
      GREATEST(
        t.f * CASE WHEN t.sahe_kodu IN (1902,1904) THEN 0.001
                   WHEN t.stage_start='Stage 1' THEN 0.01
                   WHEN t.stage_start='Stage 2' THEN 0.02 ELSE 0 END,
        CASE WHEN t.stage_start='Stage 3'
             THEN (t.f-t.g-t.h-t.j-t.k)*CASE WHEN t.sahe_kodu IN (1902,1904) THEN r.q2 ELSE r.p2 END
             ELSE t.i_col*(CASE WHEN t.f=0 THEN 1 ELSE (t.f-t.g-t.h-t.j-t.k)/t.f END)*CASE WHEN t.sahe_kodu IN (1902,1904) THEN r.q2 ELSE r.p2 END
        END
      ) AS m
    FROM trans t CROSS JOIN recovery r
),
riskfaiz AS (
    SELECT sahe_kodu, stage_start, AVG(CASE WHEN f=0 THEN 0.0001 ELSE m/f END) AS risk_faiz
    FROM riskrow GROUP BY sahe_kodu, stage_start
),
cari_snap AS (
    SELECT MAX(date_oper) AS d FROM arh_licschkre WHERE date_oper <= TO_DATE('17-02-2026','DD-MM-YYYY')
),
cari AS (
    SELECT ar.licschkre, ar.subschkre, ar.tipkredita,
           ar.index_otrasli AS sahe_kodu, io.name_index_otrasli AS sahe_adi,
           ar.date_open AS verilme_tarixi,
           NVL(ar.procstavrez,0) AS cari_ehtiyat_faiz,
           CASE WHEN NVL(odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)),0) BETWEEN 0 AND 30 THEN 'Stage 1'
                WHEN odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)) BETWEEN 31 AND 90 THEN 'Stage 2'
                ELSE 'Stage 3' END AS stage,
           (ar.summa+ar.summa_19)*ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6) AS ead
    FROM cari_snap cs
    JOIN arh_licschkre ar ON ar.date_oper=cs.d
    LEFT JOIN view_nacpogprokre_all x ON x.licschpkre=ar.licschpkre
                                     AND x.subschkre=ar.subschkre
                                     AND x.date_oper=ar.date_oper
    JOIN index_otrasli io ON io.index_otrasli=ar.index_otrasli
    WHERE ar.date_close IS NULL AND LENGTH(ar.licschkre)=20
)
SELECT c.stage,
       CASE WHEN rf.risk_faiz IS NULL THEN '2_UYGUNLUQ_YOX' ELSE '1_var' END AS risk_uygun,
       COUNT(*)              AS say,
       ROUND(SUM(c.ead),2)   AS ead,
       ROUND(SUM(c.ead*NVL(rf.risk_faiz,0.0001)),2) AS ecl
FROM cari c
LEFT JOIN riskfaiz rf ON rf.sahe_kodu=c.sahe_kodu AND rf.stage_start=c.stage
GROUP BY c.stage, CASE WHEN rf.risk_faiz IS NULL THEN '2_UYGUNLUQ_YOX' ELSE '1_var' END
ORDER BY c.stage, risk_uygun;
