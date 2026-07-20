/* ============================================================================
   IFRS 9 ECL — TAM MODEL (Excel metodologiyasının SQL versiyası)
   ----------------------------------------------------------------------------
   Məqsəd: sənin 2 mərhələli Excel işini (keçid matrisi + cari portfel + düstur)
   BİR Oracle SQL-ə yığmaq və nəticəni Excel ilə tutuşdurmaq.

   Metodologiya (sənin faylından, dəqiq təkrarlanıb):
     • Staging DPD-dən: 0–30 = Stage 1, 31–90 = Stage 2, 90+ = Stage 3.
     • 5 illik keçid (roll-rate) → hər sətir üçün M (risk məbləği):
         MAX( floor ,  Stage3: (F-G-H-J-K)×(1-bərpa)  |  Stage1&2: toS3×survival×(1-bərpa) )
         floor: mənzil(1902/1904) 0.1% · Stage1 1% · Stage2 2% · Stage3 0
         bərpa: mənzil Q2, digər P2  (Stage3 sətirlərinin (J+K)/F ortalaması)
     • risk_faiz(sahə, stage) = AVG(M/F)
     • Cari ECL = EAD × risk_faiz(sahə, stage)

   Parametrlər (PL/SQL Developer soruşacaq):
     &tarix = hesabat tarixi (DD-MM-YYYY), məs. 30-06-2026
     &mode  = WITH_CURRENT  (cari il daxil)  |  başqa dəyər (cari il xaric)

   YOXLAMA: əvvəlcə &tarix=17-02-2026 ilə işlət — Stage cəmləri Excel-dəki
   04_Nəticə ilə tutuşmalıdır (Stage1≈61443, Stage2≈6832, Stage3≈210496,
   ümumi≈278771). Tutuşsa, model düzdür → sonra 30-06-2026 üçün işlədirik.
   ============================================================================ */

WITH iller AS (
    /* 6 snapshot → 5 keçid. WITH_CURRENT: 2021..2026 (il_start 2021-2025 = Excel ilə eyni). */
    SELECT EXTRACT(YEAR FROM ADD_MONTHS(TRUNC(TO_DATE('&tarix','DD-MM-YYYY'),'YYYY'),
             CASE WHEN '&mode'='WITH_CURRENT' THEN -12*(LEVEL-1) ELSE -12*LEVEL END)) AS il
    FROM dual CONNECT BY LEVEL <= 6
),
son_tarixler AS (
    SELECT i.il, MAX(ar.date_oper) AS son_tarix
    FROM iller i
    JOIN arh_licschkre ar ON EXTRACT(YEAR FROM ar.date_oper)=i.il
                          AND ar.date_oper <= TO_DATE('&tarix','DD-MM-YYYY')
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
    WHERE ar.date_close IS NULL   /* snapshot-state: hər ilin snapshotunda o vaxt açıq olan kreditlər */
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
trans AS (   /* Excel 01_SQL_Keçidlər: hər (il, sahə, stage) SƏTRİ — SAHƏ SƏVİYYƏSİNDƏ CƏMLƏNMİŞ.
              DİQQƏT: fərdi kredit (licschpkre/subschkre) üzrə YOX — Excel-də F/G/H/I/J/K
              sahə üzrə cəmdir, M/N floor+bərpa qeyri-xətti olduğu üçün cəmləmə səviyyəsi vacibdir. */
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
riskrow AS (   /* M və N (=M/F) hər tarixi sətir üçün */
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
riskfaiz AS (   /* risk faizi = AVG(N) sahə+stage üzrə */
    SELECT sahe_kodu, stage_start, AVG(CASE WHEN f=0 THEN 0.0001 ELSE m/f END) AS risk_faiz
    FROM riskrow GROUP BY sahe_kodu, stage_start
),
cari_snap AS (
    SELECT MAX(date_oper) AS d FROM arh_licschkre WHERE date_oper <= TO_DATE('&tarix','DD-MM-YYYY')
),
cari AS (   /* hesabat tarixindəki AÇIQ portfel — BİRBAŞA arh_licschkre-dən.
              Sənin göstərişin: hesab (licschkre), sub kod (subschkre), qalıq,
              cari ehtiyat (procstavrez), verilmə tarixi (date_open) — bu qədər bəsdir.
              Staging DPD-dən gəlir, amma view_nacpogprokre_all-a LEFT JOIN edilir ki,
              view-da qarşılığı olmayan kreditlər ATILMASIN (əvvəl INNER idi → 3.07M,
              kreditlər düşürdü). Qarşılığı olmayan (heç vaxt gecikməmiş) kredit
              təbii olaraq Stage 1 (DPD=0) sayılır. */
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
       COUNT(*)                                    AS say,
       ROUND(SUM(c.ead),2)                         AS cari_qaliq,
       ROUND(SUM(c.ead*NVL(rf.risk_faiz,0.0001)),2) AS ecl,
       ROUND(SUM(c.ead*NVL(rf.risk_faiz,0.0001))/NULLIF(SUM(c.ead),0)*100,2) AS ecl_faiz,
       ROUND(SUM(c.ead*c.cari_ehtiyat_faiz/100),2) AS bank_ehtiyat  /* FINA cari ehtiyat — müqayisə üçün */
FROM cari c
LEFT JOIN riskfaiz rf ON rf.sahe_kodu=c.sahe_kodu AND rf.stage_start=c.stage
GROUP BY c.stage
ORDER BY c.stage;
