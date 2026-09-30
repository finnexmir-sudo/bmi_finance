namespace FinNex.Domain.Entities.Risk
{
    /// <summary>
    /// MB Qərar 04/1 ("Banklarda əməliyyat risklərinin idarə edilməsi Qaydası"),
    /// Əlavə 4 — "Əməliyyat riski hadisələri barədə məlumat bazası" (30.09.2026,
    /// istifadəçi tələbi: "biz bu bazanı qurmalıyıq").
    ///
    /// İSTİFADƏÇİ QƏRARLARI (bu sessiya):
    /// - Təsdiq zənciri YOXDUR — Risk/AML şöbəsindəki işçi hadisəni müşahidə
    ///   edən kimi birbaşa yaradır, bu, artıq rəsmi qeyddir (özü təsdiqçi
    ///   səviyyəsindədir).
    /// - Redaktə sərbəstdir, amma HƏR redaktənin tam tarixçəsi saxlanılır —
    ///   bax <see cref="EmeliyyatRiskiHadisesiTarixce"/>.
    /// - Əlavə 4-ün 23-26-cı sahələri (tədbirlər planı) yalnız
    ///   <see cref="UmumiZererMebleg"/> bankın öz təyin etdiyi hədddən
    ///   (<see cref="EmeliyyatRiskiParametrleri.TedbirZererHeddi"/>) yuxarı
    ///   olanda MƏCBURİDİR — Əlavənin öz qeydi: "23-26-cı hissələri bankın
    ///   daxili qaydaları ilə müəyyən edilmiş limitdən yuxarı həddə olan
    ///   zərərlər üzrə doldurulur".
    ///
    /// Əlavə 4-ün bəzi sahələri BaseEntity-də ARTIQ VAR — təkrar yazılmır:
    /// sahə 1 (Qeydiyyat tarixi) = <see cref="BaseEntity.YaradilmaTarixi"/>;
    /// sahə 3 (Məlumatı bazaya daxil edən əməkdaş) = <see cref="BaseEntity.YaradanIcraciId"/>;
    /// sahə 27-nin tarix hissəsi (son dəyişiklik tarixi) = <see cref="BaseEntity.YenilenmeTarixi"/>.
    /// </summary>
    public class EmeliyyatRiskiHadisesi : BaseEntity
    {
        // ── Sahə 2 — Qeydiyyat kodu ("BS5R5.1" formatında, avtomatik) ──────
        // BiznesSahesi + RiskKateqoriyasi1/2-dən EmeliyyatRiskiKodHelper ilə
        // hesablanır, əl ilə yazılmır (bax servis qatı).
        public string QeydiyyatKodu { get; set; } = "";

        // ── Sahə 4-5 — hadisənin haradan gəldiyi ────────────────────────────
        /// <summary>Sahə 4: "Risk hadisəsinin baş verdiyi struktur bölmə".
        /// Sərbəst mətn saxlanılır (filial/bölmə DB-də struktur cədvəli kimi
        /// olmaya bilər, məs. kənar xidmət təminatçısı da ola bilər).</summary>
        public string StrukturBolme { get; set; } = "";

        /// <summary>Sahə 5: "Məlumatı təqdim edən əməkdaş (koordinator)" —
        /// bu layihədə ayrıca koordinator addımı yoxdur (istifadəçi qərarı),
        /// sahə yalnız hadisəni Risk/AML işçisinə İLK bildirən şəxsi (adətən
        /// başqa şöbədən) qeyd üçün saxlayır. Sistem istifadəçisi olmaya bilər
        /// — ona görə FK yox, sərbəst mətn.</summary>
        public string? MelumatiVerenSexs { get; set; }

        // ── Sahə 6-8 — vaxt oxu ──────────────────────────────────────────
        public DateTime HadiseninBasVerdiyiTarix { get; set; }
        public DateTime HadiseninMueyyenlesdirilmeTarixi { get; set; }
        /// <summary>Sahə 8: hadisəni müəyyənləşdirən əməkdaş (sistemdəki işçi).</summary>
        public int? MueyyenlesdirenIsciId { get; set; }

        // ── Sahə 9-10 — nə baş verib, niyə ────────────────────────────────
        public string Tesvir { get; set; } = "";
        public string Sebeb { get; set; } = "";

        // ── Sahə 11-12 — dərəcələr (İstilik xəritəsi üçün) ────────────────
        public RiskDerecesi TezlikDerecesi { get; set; }
        public RiskDerecesi TesirDerecesi { get; set; }

        // ── Sahə 13-17 — təsnifat ─────────────────────────────────────────
        public BiznesSahesi BiznesSahesi { get; set; }
        public string? BankMehsulu { get; set; }
        public RiskKateqoriyasi1 RiskKateqoriyasi1 { get; set; }
        public RiskKateqoriyasi2 RiskKateqoriyasi2 { get; set; }
        /// <summary>Sahə 17: "Risk hadisəsi (3-cü səviyyə)" — Əlavə 3-ün öz
        /// qeydinə görə "nümunələr bank tərəfindən artırıla bilər", ona görə
        /// sabit siyahı deyil, sərbəst mətn.</summary>
        public string? RiskHadisesiNumune { get; set; }

        // ── Sahə 18-22.1 — maliyyə təsiri ─────────────────────────────────
        public ZererTesirKateqoriyasi ZererTesirKateqoriyasi { get; set; }
        /// <summary>Sahə 19 — tam AZN məbləği (Əlavənin "min manatla" ifadəsi
        /// yalnız Əlavə 5-in AQREQAT hesabatına aiddir; tək hadisədə real
        /// məbləğ saxlanılır ki, hədd müqayisələri (bax yuxarı) dəqiq olsun,
        /// ixrac zamanı /1000 tətbiq olunacaq).</summary>
        public decimal UmumiZererMebleg { get; set; }
        public decimal? PotensialZererMebleg { get; set; }
        public DateTime? BerpaTarixi { get; set; }
        public decimal? BerpaOlunanMebleg { get; set; }
        public decimal? SigortaIleBerpaOlunanHisse { get; set; }

        // ── Sahə 23-26 — tədbirlər planı (YALNIZ limitdən yuxarı zərərdə məcburi) ──
        public DateTime? TedbirlerinTarixi { get; set; }
        public string? TedbirlerinTesviri { get; set; }
        public string? TedbirlereMesulBolme { get; set; }
        public TedbirIcraStatusu? TedbirlerinIcraStatusu { get; set; }

        /// <summary>Qeyd YARADILAN andakı <see cref="EmeliyyatRiskiParametrleri.TedbirZererHeddi"/>
        /// — bir dəfə yazılır, redaktədə DƏYİŞMİR (code-review, 30.09.2026).
        /// Səbəb: hədd sonradan aşağı salınsa, köhnə (o vaxt qanuni şəkildə
        /// tədbirsiz qalmış) qeydlər "canlı" hədd ilə müqayisə olunsaydı geriyə
        /// dönük tədbir tələb edərdi və adi bir redaktəni (məs. səhv düzəlişi)
        /// belə bloklardı. Eyni "dondurma" prinsipi layihədə artıq var —
        /// Pul Köçürməsi limitindəki `UsdEkvivalent` (bax CLAUDE.md).</summary>
        public decimal TedbirZererHeddiYaradilmaAninda { get; set; }

        // ── Sahə 27 — son dəyişikliyin QISA təsviri (tarix BaseEntity-dədir) ──
        // Tam tarixçə EmeliyyatRiskiHadisesiTarixce-dədir; bu sahə cədvəldə/
        // detalda "son dəyişiklik nə idi"-ni sürətli göstərmək üçündür.
        public string? SonDeyisiklikTesviri { get; set; }
    }
}
