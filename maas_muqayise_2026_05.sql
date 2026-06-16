/* =====================================================================
   MAAŞ MÜQAYİSƏSİ — Sistem (FinNex DB) vs Mühasib Excel
   Dövr: 2026-cı il, MAY  (Il = 2026, Ay = 5)
   YALNIZ OXUMA — SELECT. Heç bir INSERT/UPDATE/DELETE yoxdur.

   Məqsəd: mühasibin "muhasib_05_2026_maas.xlsx" faylındakı hər işçi
   sətrini bazadakı hesablanmış maaşla yan-yana qoyub fərqi görmək.
   Sütun başlıqlarındakı rəqəmlər mühasib faylının sütun nömrələridir.

   QEYD (vacib): [9_Mezuniyyet_haqqi] sütunu YALNIZ ay-sonu məzuniyyəti
   göstərir. QABAQCADAN (avans) ödənilmiş məzuniyyətin BRÜTÜ bu sütuna
   düşmür (məlum problem). Onu ayrıca yoxlamaq üçün aşağıdakı 2-ci sorğu var.
   ===================================================================== */

;WITH det AS (
    SELECT
        d.MaasId,
        SUM(CASE WHEN mn.Ad = N'Əsas Əməkhaqqı'                    THEN d.Mebleg ELSE 0 END) AS EsasEmekhaqqi,
        SUM(CASE WHEN mn.Ad = N'Davamiyyət Kəsintisi'              THEN d.Mebleg ELSE 0 END) AS DavamKesinti,
        SUM(CASE WHEN mn.Ad = N'Məzuniyyət Ödənişi'                THEN d.Mebleg ELSE 0 END) AS MezuniyyetOdenisi,
        SUM(CASE WHEN mn.Ad = N'Xəstəlik Ödənişi'                  THEN d.Mebleg ELSE 0 END) AS XestelikOdenisi,
        SUM(CASE WHEN mn.Ad = N'Bonus/Mükafat'                     THEN d.Mebleg ELSE 0 END) AS Mukafat,
        SUM(CASE WHEN mn.Ad = N'Overtime'                          THEN d.Mebleg ELSE 0 END) AS Overtime,
        SUM(CASE WHEN mn.Ad = N'IH-07 Əlavə Təminat'               THEN d.Mebleg ELSE 0 END) AS IH07,
        SUM(CASE WHEN mn.Ad = N'VM 98.2.1 Gəlirləri'               THEN d.Mebleg ELSE 0 END) AS VM9821,
        SUM(CASE WHEN mn.Ad = N'Fərqli Gəlir'                      THEN d.Mebleg ELSE 0 END) AS FerqliGelir,
        SUM(CASE WHEN mn.Ad = N'Gecikdirmə Cəriməsi'               THEN d.Mebleg ELSE 0 END) AS Cerime,
        SUM(CASE WHEN mn.Ad = N'Gəlir Vergisi'                     THEN d.Mebleg ELSE 0 END) AS GelirVergisi,
        SUM(CASE WHEN mn.Ad = N'DSMF (İşçi)'                       THEN d.Mebleg ELSE 0 END) AS DSMF_Isci,
        SUM(CASE WHEN mn.Ad = N'İşsizlik Sığortası (İşçi)'         THEN d.Mebleg ELSE 0 END) AS Issizlik_Isci,
        SUM(CASE WHEN mn.Ad = N'İTSS'                              THEN d.Mebleg ELSE 0 END) AS ITSS_Isci,
        SUM(CASE WHEN mn.Ad = N'HYS (İşçi)'                        THEN d.Mebleg ELSE 0 END) AS HYS_Isci,
        SUM(CASE WHEN mn.Ad = N'Avans Kəsintisi'                   THEN d.Mebleg ELSE 0 END) AS Avans,
        SUM(CASE WHEN mn.Ad = N'Əvvəlki Ay Artıq Ödəniş Kəsintisi' THEN d.Mebleg ELSE 0 END) AS EvvelkiAyKesinti,
        SUM(CASE WHEN mn.Ad = N'Əvvəlki Ay Kompensasiyası'         THEN d.Mebleg ELSE 0 END) AS EvvelkiAyKomp,
        SUM(CASE WHEN mn.Ad = N'DSMF (İşəgötürən)'                 THEN d.Mebleg ELSE 0 END) AS DSMF_Isegoturen,
        SUM(CASE WHEN mn.Ad = N'İşsizlik Sığortası (İşəgötürən)'   THEN d.Mebleg ELSE 0 END) AS Issizlik_Isegoturen,
        SUM(CASE WHEN mn.Ad = N'İTSS (İşəgötürən)'                 THEN d.Mebleg ELSE 0 END) AS ITSS_Isegoturen,
        SUM(CASE WHEN mn.Ad = N'HYS (İşəgötürən)'                  THEN d.Mebleg ELSE 0 END) AS HYS_Isegoturen
    FROM MaasDetaylari d
    INNER JOIN MaasNovleri mn ON mn.Id = d.MaasNovuId
    WHERE d.Silinib = 0
    GROUP BY d.MaasId
)
SELECT
    ROW_NUMBER() OVER (ORDER BY i.Sira, i.Soyad, i.Ad)                       AS [No],
    (i.Soyad + N' ' + i.Ad + N' ' + ISNULL(i.AtaAdi, N''))                   AS [SAA],
    pos.Vezife                                                              AS [Vezifesi],

    -- ── GƏLİR tərəfi ──────────────────────────────────────────────
    im.CariMaas                                                             AS [4_Muqavile_uzre],        -- mühasib col 4
    det.EsasEmekhaqqi                                                       AS [h_Esas_detal],           -- (= CariMaas olmalıdır)
    det.DavamKesinti                                                        AS [h_Davamiyyet_kesinti],   -- qayıb+məz.+xəstəlik günləri
    CAST(ISNULL(det.EsasEmekhaqqi,0) - ISNULL(det.DavamKesinti,0) AS decimal(18,2)) AS [5_Hesablanmis], -- col 5 = Əsas − Davamiyyət
    det.IH07                                                                AS [6_IH07],                 -- col 6
    det.MezuniyyetOdenisi                                                   AS [9_Mezuniyyet_haqqi],     -- col 9  (DİQQƏT: avans məz. DAXİL DEYİL)
    det.Mukafat                                                             AS [10_Mukafat],             -- col 10
    det.XestelikOdenisi                                                     AS [13_Xestelik],            -- col 13
    det.VM9821                                                              AS [15_VM_98_2_1],           -- col 15
    det.Overtime                                                            AS [17_Elave_emek_haqqi],    -- col 17
    det.FerqliGelir                                                         AS [h_Ferqli_Gelir],
    det.EvvelkiAyKomp                                                       AS [h_Evvelki_Ay_Komp],
    m.BrutMebleg                                                            AS [19_Cemi_hesablanmis_SISTEM], -- col 19 (sistem brütü — işəgötürən HYS DAXİL)

    -- ── TUTULMALAR (işçidən) ──────────────────────────────────────
    det.HYS_Isci                                                            AS [20_HYS_isci],            -- col 20
    det.DSMF_Isci                                                           AS [22_MDSS_isci],           -- col 22
    det.Issizlik_Isci                                                       AS [23_Issizlik_isci],       -- col 23
    det.GelirVergisi                                                        AS [24_Gelir_vergisi],       -- col 24
    det.ITSS_Isci                                                           AS [25_Icbari_Tibbi_isci],   -- col 25
    det.Avans                                                               AS [26_Avans],               -- col 26
    det.Cerime                                                              AS [h_Cerime],
    det.EvvelkiAyKesinti                                                    AS [h_Evvelki_Ay_Kesinti],

    -- Tutulmalar cəmi (mühasib col 28 tərzində: vergi+sığorta+HYS+avans+cərimə; Davamiyyət DAXİL DEYİL)
    CAST(ISNULL(det.GelirVergisi,0)+ISNULL(det.DSMF_Isci,0)+ISNULL(det.Issizlik_Isci,0)
        +ISNULL(det.ITSS_Isci,0)+ISNULL(det.HYS_Isci,0)+ISNULL(det.Avans,0)
        +ISNULL(det.Cerime,0)+ISNULL(det.EvvelkiAyKesinti,0) AS decimal(18,2))    AS [28_Tutulmusdur_detallar],
    CAST(m.BrutMebleg - m.NetMebleg AS decimal(18,2))                       AS [h_Brut_minus_Net],
    m.NetMebleg                                                             AS [29_Odenilmelidir_NET],   -- col 29

    -- ── İŞƏGÖTÜRƏN payları ────────────────────────────────────────
    det.DSMF_Isegoturen                                                     AS [30_MDSS_isegoturen],     -- col 30
    det.Issizlik_Isegoturen                                                 AS [h_Issizlik_isegoturen],
    det.ITSS_Isegoturen                                                     AS [31_ITSS_isegoturen],     -- col 31
    det.HYS_Isegoturen                                                      AS [21_HYS_isegoturen],      -- col 21

    im.BankHesabNo                                                          AS [32_Cari_hesab],          -- col 32
    m.Status                                                                AS [Status]
FROM Maaslar m
INNER JOIN Isciler i ON i.Id = m.IsciId
LEFT JOIN det ON det.MaasId = m.Id
OUTER APPLY (
    SELECT TOP 1 im2.CariMaas, im2.BankHesabNo
    FROM IsciMaliyeleri im2
    WHERE im2.IsciId = i.Id AND im2.Silinib = 0
    ORDER BY im2.Id DESC
) im
OUTER APPLY (
    SELECT TOP 1 v.Ad AS Vezife
    FROM IsciTeyinatlari t
    INNER JOIN Vezifeler v ON v.Id = t.VezifeId
    WHERE t.IsciId = i.Id AND t.Silinib = 0 AND t.Aktivdir = 1
    ORDER BY t.Esasdir DESC, t.BaslamaTarixi DESC
) pos
WHERE m.Silinib = 0
  AND m.Il = 2026
  AND m.Ay = 5
ORDER BY i.Sira, i.Soyad, i.Ad;


/* =====================================================================
   2-ci SORĞU — QABAQCADAN (avans) ödənilmiş məzuniyyət
   Yuxarıdakı [9_Mezuniyyet_haqqi] sütununa düşməyən BRÜT məbləği
   işçi-işçi göstərir (məlum problemi kəmiyyətcə görmək üçün).

   Enum dəyərləri (DB-də int kimi saxlanır):
     OdenisTipi:   1 = AySonuOdenis,  2 = QabaqcadanOdenis (avans)
     OdenisStatus: 0 = TetbiqEdilmir, 1 = Gozleyir, 2 = Odenilib, 3 = PlanliOdenis
   ===================================================================== */
SELECT
    i.Soyad + N' ' + i.Ad AS [SAA],
    mz.BaslamaTarixi,
    mz.BitmeTarixi,
    mz.OdenisTipi          AS [OdenisTipi_1aysonu_2avans],
    mz.OdenisStatus        AS [OdenisStatus_2odenilib],
    mz.OdenilmeTarixi,
    mz.OdenenMebleg        AS [NET_odenilmis],    -- provodkada "bağlanılması" (G12) sətrinə düşən
    mz.OdenenMeblegBrut    AS [BRUT_hesablanmis]  -- məzuniyyət xərci (G8) sətrinə DÜŞMƏLİ olan, amma düşməyən
FROM Mezuniyyetler mz
INNER JOIN Isciler i ON i.Id = mz.IsciId
WHERE mz.Silinib = 0
  AND mz.OdenisTipi = 2                 -- yalnız QabaqcadanOdenis (avans)
  AND mz.OdenilmeTarixi >= '2026-05-01'
  AND mz.OdenilmeTarixi <  '2026-06-01'
ORDER BY i.Soyad, i.Ad;
