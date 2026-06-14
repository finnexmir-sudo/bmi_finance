namespace FinNex.Domain.Entities.HR
{
    // Maaşın növlərini idarə edən kitabça
    public class MaasNovu : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public MaasDetayTipi Tip { get; set; }
        public bool Aktivdir { get; set; } = true;

        // ── Konfiqurasiyalı manual gəlir növü (mühasib admin paneldən yaradır) ──
        // true → toplu hesablamada bu növ üçün textbox göstərilir və işçiyə məbləğ
        // daxil edilir. Vergi/haqq rejimi aşağıdakı bayraqlarla müəyyən olunur.
        // Mövcud (sabit) növlər üçün false-dur — onların rejimi əvvəlki kimi koddadır,
        // ona görə bu bayraqlar yalnız ManualGelir=true növlərdə oxunur.
        public bool ManualGelir { get; set; } = false;

        // Vergi/haqq rejimi (yalnız ManualGelir=true gəlir növləri üçün):
        public bool VergiyeCelb { get; set; } = true;        // gəlir vergisi tutulur
        public bool DsmfyeCelb { get; set; } = true;         // DSMF (MDSS) tutulur
        public bool IssizliyeCelb { get; set; } = true;      // işsizlik sığortası tutulur
        public bool ItsseCelb { get; set; } = true;          // İcbari Tibbi Sığorta tutulur

        // Gəlir vergisindən QİSMƏN azad olan sabit məbləğ (VergiyeCelb=true ilə işləyir):
        // 0  → tam tutulur (azadlıq yoxdur).
        // >0 → məbləğin yalnız (giriş − bu məbləğ) hissəsi gəlir vergisinə cəlb olunur;
        //      yəni bu məbləğ qədəri vergidən azaddır (giriş bu məbləğdən azdırsa, hamısı azad).
        //      Məs. əsgərlik müavinəti: azad=2000, müavinət=2500 → vergi yalnız 500-ə.
        // QEYD: yalnız GƏLİR VERGİSİ bazasından çıxılır — DSMF/İşsizlik/İTSS tam məbləğdən.
        public decimal GelirVergisiAzadMebleg { get; set; } = 0m;

        // 200 AZN gəlir vergisi güzəştinin (VM 102.1.6, brüt ≤ 2500) yoxlanmasında
        // bu məbləğ ümumi gəlirə daxil edilirmi (həddə sayılır).
        public bool GuzestHeddineDaxil { get; set; } = true;

        // Məzuniyyət üçün 12-aylıq orta qazanca daxil edilirmi (birdəfəlik ödənişlər yox).
        public bool MezuniyyetOrtalamasinaDaxil { get; set; } = true;

        // Əməliyyat yazılışında (provodka) bu növün xərc hesabının açarı
        // (MuhasibatHesabi.Acar). Boşdursa, provodkada ayrıca xərc sətri yazılmır.
        public string? ProvodkaHesabAcari { get; set; }

        public ICollection<MaasDetay> MaasDetallari { get; set; } = new List<MaasDetay>();
    }
}
