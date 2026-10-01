/* ============================================================================
   RİSK → HESABAT TAB-LARINDA "Ölkə üzrə müştərilər" İKİ DƏFƏ GÖRÜNÜR
   ----------------------------------------------------------------------------
   Səbəb: koddan DEYİL, DATADAN gəlir (CLAUDE.md: "OracleSorgular DATA-dır,
   KOD DEYİL" — RiskService.HesabatlarAsync() heç bir dublikat süzgəci
   tətbiq etmir, bu QƏSDƏNDİR, çünki prod/lokal bazalar fərqli ola bilər).

   `OracleSorgular` cədvəlində "Ölkə üzrə müştərilər" adı ilə İKİ AYRI sətir
   var — eyni sorğunun kopyası DEYİL, İKİ FƏRQLİ sorğudur:

     A) AQREQAT (BAR qrafik) — ölkə üzrə SAY:
        docs/sql/risk/Risk_Panel_Widgetler.sql:112-123
        docs/sql/risk/Risk_TAM_Son_Versiya.sql:134-146  ("── 6 ──")

     B) DETALLI SİYAHI — ad-ad, hansı müştəri hansı ölkədə:
        əvvəlki versiya: docs/sql/risk/Risk_Sorgular.sql:127-138
          (adı sadəcə "Ölkə üzrə müştərilər" yazılıb — A ilə TOQQUŞUR)
        DÜZƏLDİLMİŞ versiya: docs/sql/risk/Risk_TAM_Son_Versiya.sql:307-325
          ("── 15 ──", adı "Ölkə üzrə müştərilər (siyahı)" olaraq
          DƏYİŞDİRİLİB, məhz bu toqquşmanı önləmək üçün — və filtr də
          6-cı qrafiklə UYĞUNLAŞDIRILIB, yoxsa bankın öz filialı kimi
          qeyri-müştərilər də siyahıya düşürdü).

   Deməli: `OracleSorgular`-dakı B sətri hələ KÖHNƏ adla qalıb (Admin →
   Oracle Sorğular ekranından yazılanda son (Risk_TAM_Son_Versiya.sql)
   adı işlədilməyib). Düzəliş KOD DEYİL, bu sətrin adını (və filtrini)
   son sənədlə uyğunlaşdırmaqdır.

   ⚠ Bu fayl SQL Server-ə qarşı işlədilir (OracleSorgular — SQL Server-dəki
   "saxlanmış Oracle sorğuları" cədvəlidir, Oracle-ın özü DEYİL).
   ============================================================================ */

/* ── 1) DİAQNOSTİKA — əvvəlcə NƏ dəyişəcəyini gör ────────────────────────── */
SELECT Id, SorguAdi, Mahiyyet, Aktiv,
       LEN(SorguMetni) AS SorguUzunlugu,
       CASE WHEN SorguMetni LIKE '%count(distinct%' THEN N'A — AQREQAT (BAR)'
            WHEN SorguMetni LIKE '%order by olke%' THEN N'B — DETALLI SİYAHI'
            ELSE N'naməlum' END AS Nov,
       LEFT(SorguMetni, 300) AS SorguBasi
FROM OracleSorgular
WHERE SorguAdi LIKE N'Ölkə üzrə müştərilər%'
ORDER BY Id;

/* Gözlənilən nəticə: 2 sətir.
   - "A — AQREQAT (BAR)" sətrinin adı artıq "Ölkə üzrə müştərilər" kimi QALMALIDIR
     (bu, 6-cı dashboard qrafikidir — adı dəyişsə qrafik başlığı da dəyişər).
   - "B — DETALLI SİYAHI" sətrinin adı AŞAĞIDAKI UPDATE ilə dəyişdirilməlidir. */


/* ── 2) DÜZƏLİŞ — YALNIZ yuxarıdakı SELECT-i gördükdən və B sətrinin Id-sini
        təsdiqlədikdən sonra işə salın. Id-ni YUXARIDAKI nəticəyə görə dəyişin. ── */
-- UPDATE OracleSorgular
-- SET SorguAdi = N'Ölkə üzrə müştərilər (siyahı)'
-- WHERE Id = <B sətrinin Id-si>   -- məs. WHERE Id = 24  (diaqnostikadan gələn ədəd)
--   AND SorguMetni LIKE '%order by olke%';  -- əlavə təhlükəsizlik — yalnız DETALLI sətri tutsun

/* ── 3) (İSTƏYƏ BAĞLI) B sətrinin FİLTRİ də A ilə UYĞUNLAŞDIRILSIN
        Risk_TAM_Son_Versiya.sql-in öz qeydi: filtr fərqli olduğu üçün köhnə
        siyahıya bankın öz filialı (BMİ - HAMBURG BRANCH / Almaniya) kimi
        qeyri-müştərilər də düşürdü. Yeni SQL mətni (A ilə eyni real-müştəri
        süzgəci ilə): ────────────────────────────────────────────────────── */
-- UPDATE OracleSorgular
-- SET SorguMetni = N'select distinct r.regnom qeyd_no, r.name_regnom ad,
--        case when l.countrycode=''GEO'' then ''Gürcüstan'' else c.name end olke
-- from   regnom r, licsch l, countrycode c
-- where  r.regnom = l.registrac_nomer
--   and  l.date_close_licsch is null
--   and  substr(l.licsch,1,1) in (''2'',''3'',''4'')
--   and  (r.yurik = 1 or r.fizik = 1 or r.predprinimatel = 1)
--   and  l.countrycode = c.code
-- order by olke, r.name_regnom'
-- WHERE Id = <B sətrinin Id-si>;

/* Bu addım MÜTLƏQ deyil (ad dəyişikliyi tab-ı dublikatdan çıxarmaq üçün
   kifayətdir) — amma Risk_TAM_Son_Versiya.sql-in qeydinə görə filtr fərqi
   siyahıda qeyri-müştəri sətirləri buraxır. Tətbiq etməzdən əvvəl B
   sətrinin HAZIRKI SorguMetni-ni yuxarıdakı SELECT-dən oxuyub tutuşdurun —
   başqa əl düzəlişi aparılıbsa üstələməyin. */
