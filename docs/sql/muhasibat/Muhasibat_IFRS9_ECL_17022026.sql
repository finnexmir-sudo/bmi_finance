/* ============================================================================
   IFRS 9 ECL — TAM (17-02-2026) — İSTİFADƏÇİNİN ORİJİNAL KEÇİD SORĞUSU + ECL qatı
   ----------------------------------------------------------------------------
   Keçid hissəsi (iller..kechid..trans) istifadəçinin "IFRS9 ucun TAM sql Ferdi
   kr ile" sorğusunun EYNİSİDİR — mode = cari il XARİC (else budağı):
     • illər 2021..2025 (LEVEL<=5, -12*LEVEL)
     • date_open >= 2021-01-01
     • 2025->2026 keçidi yarımçıq (2026 yüklənmir) → 2025 sətirləri "Baglanib"
   Üstünə Excel-in M/N/floor/recovery/risk% + cari portfel ECL məntiqi əlavə edilib.
   Gözlənilən: Stage cəmləri ≈ Excel 04_Nəticə (S1 61443, S2 6832, S3 210496, cəm 278771).
   ============================================================================ */
WITH iller AS (
    SELECT EXTRACT(YEAR FROM ADD_MONTHS(TRUNC(TO_DATE('17-02-2026','DD-MM-YYYY'),'YYYY'),
             -12*LEVEL)) AS il          /* cari il XARİC: 2025,2024,2023,2022,2021 */
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
      AND ar.date_open >= ADD_MONTHS(TRUNC(TO_DATE('17-02-2026','DD-MM-YYYY'),'YYYY'), -12*5)  /* 2021-01-01 */
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
trans AS (   /* Excel 01_SQL_Keçidlər: (il, sahə, stage) səviyyəsində cəm — F,G,H,I,J,K */
    SELECT il_start, sahe_kodu, stage_start,
           SUM(qaliq_start) AS f,
           SUM(CASE WHEN stage_next='Stage 1' THEN qaliq_next ELSE 0 END) AS g,
           SUM(CASE WHEN stage_next='Stage 2' THEN qaliq_next ELSE 0 END) AS h,
           SUM(CASE WHEN stage_next='Stage 3' THEN qaliq_next ELSE 0 END) AS i_col,
           SUM(CASE WHEN stage_next='Baglanib' THEN qaliq_start ELSE 0 END) AS j,
           SUM(CASE WHEN stage_next IN ('Stage 1','Stage 2','Stage 3') THEN (qaliq_start-qaliq_next) ELSE 0 END) AS k
    FROM kechid
    GROUP BY il_start, sahe_kodu, stage_start
),
recovery AS (   /* P2 (digər), Q2 (mənzil) — Stage3 (J+K)/F ortalaması */
    SELECT
      NVL(AVG(CASE WHEN sahe_kodu NOT IN (1902,1904) AND stage_start='Stage 3' AND f>0 THEN (j+k)/f END),0)    AS p2,
      NVL(AVG(CASE WHEN sahe_kodu     IN (1902,1904) AND stage_start='Stage 3' AND f>0 THEN (j+k)/f END),0.75) AS q2
    FROM trans
),
riskrow AS (   /* Excel M sütunu — floor + recovery, hər (il,sahə,stage) sətri üçün */
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
riskfaiz AS (   /* Excel 03!H = AVERAGEIFS(N) — illər üzrə sadə orta, sahə+stage üzrə */
    SELECT sahe_kodu, stage_start, AVG(CASE WHEN f=0 THEN 0.0001 ELSE m/f END) AS risk_faiz
    FROM riskrow GROUP BY sahe_kodu, stage_start
),
cari_snap AS (
    SELECT MAX(date_oper) AS d FROM arh_licschkre WHERE date_oper <= TO_DATE('17-02-2026','DD-MM-YYYY')
),
cari AS (   /* cari portfel — arh_licschkre-dən (sənin göstərişin) */
    SELECT ar.licschkre, ar.subschkre, ar.index_otrasli AS sahe_kodu, io.name_index_otrasli AS sahe_adi,
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
       COUNT(*)                                                              AS say,
       ROUND(SUM(c.ead),2)                                                   AS ead,
       ROUND(SUM(c.ead*NVL(rf.risk_faiz,0.0001))/NULLIF(SUM(c.ead),0)*100,2) AS risk_faiz,
       ROUND(SUM(c.ead*NVL(rf.risk_faiz,0.0001)),2)                          AS ecl
FROM cari c
LEFT JOIN riskfaiz rf ON rf.sahe_kodu=c.sahe_kodu AND rf.stage_start=c.stage
GROUP BY c.stage
ORDER BY c.stage;
