namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>
    /// Hesab sahibinin (fiziki şəxsin özü, hüquqi şəxsin "Rəhbər"i, ya da
    /// "Nümayəndə"si) GOAML CON/SIG bloklarına uyğun KYC-si. Mənbə: fiziki
    /// hesabda `odb.regnom`-un ingiliscə sütunları; hüquqi hesabda SIG ("Rəhbər")
    /// üçün `odb.imza_huquqi_olan_shexsler`, CON ("Nümayəndə" seçim siyahısı)
    /// üçün isə **`odb.NUMAYENDE`** (10.11.2026-da düzəldildi — əvvəl səhvən
    /// `imza_huquqi_olan_shexsler`-dən gəlirdi, bax GoamlService.
    /// HesabSahibiKycAsync-in baş şərhi). Sahə adları GoamlTesdiqDto-nun
    /// Con*/Sig* sahələri ilə EYNİDİR ki, mapping sadə olsun.
    /// </summary>
    public class GoamlElaqaliSexsDto
    {
        /// <summary>Hüquqi şəxsin CON namizədləri üçün — `odb.NUMAYENDE.ID_AML`
        /// (SIG-in "Rəhbər"i üçün isə `imza_huquqi_olan_shexsler.ID_AML`).
        /// CON seçim siyahısında hər namizədi fərqləndirmək üçün.</summary>
        public int? IdAml { get; set; }

        /// <summary>Yalnız CON seçim siyahısında göstərmək üçün — NUMAYENDE-nin
        /// öz VEZIFESI (vəzifə) sahəsindən. SIG-in "Rəhbər"i üçün boş qalır
        /// (yalnız BİR namizəd olduğu üçün etiketə ehtiyac yoxdur).</summary>
        public string? Rollar { get; set; }

        public string? Adi { get; set; }
        public string? AtaAdi { get; set; }
        public string? Soyadi { get; set; }
        public DateTime? DogulduguTarix { get; set; }
        public string? DogulduguYer { get; set; }
        public string? Vetendasligi { get; set; }
        public string? Cinsi { get; set; }
        public string? Pin { get; set; }
        public string? TelefonNomresi { get; set; }
        public string? UnvaninNovu { get; set; }
        public string? Unvan { get; set; }
        public string? Seher { get; set; }
        public string? Olke { get; set; }
        public string? SenedinNovu { get; set; }
        public string? SenedinSeriyaNomresi { get; set; }
        public DateTime? SenedinVerilmeTarixi { get; set; }
        public DateTime? SenedinEtibarliliqMuddeti { get; set; }
        public string? VerenOrqan { get; set; }
        public string? VerildiyiOlke { get; set; }

        /// <summary>Yalnız SIG blokunda var (iş yeri/vəzifə) — CON-a yazılmır.</summary>
        public string? Vezife { get; set; }
        public string? IsYeriAdi { get; set; }
    }

    /// <summary>
    /// `HesabSahibiKycTapAsync`-in nəticəsi — ya fiziki şəxsin öz tək profili
    /// (CON+SIG-ə birbaşa yazılır), ya da hüquqi şəxsin "Rəhbər"i (SIG-ə yazılır)
    /// + əlaqəli şəxslərin tam siyahısı (CON-u operator seçsin deyə).
    /// </summary>
    public class GoamlHesabSahibiKycDto
    {
        /// <summary>true — hesab fiziki şəxsindir, <see cref="TekSexs"/> CON+SIG-in
        /// hər ikisinə aiddir. false — hüquqi şəxsdir, <see cref="TekSexs"/>
        /// (varsa) YALNIZ "Rəhbər" rolu, SIG-ə yazılır.</summary>
        public bool Fiziki { get; set; }

        public GoamlElaqaliSexsDto? TekSexs { get; set; }

        /// <summary>Yalnız hüquqi şəxsdə doldurulur — CON seçimi üçün şirkətin
        /// bütün (hazırda aktiv, IMZA_BITME boş) əlaqəli şəxsləri.</summary>
        public List<GoamlElaqaliSexsDto> ElaqaliSexsler { get; set; } = new();
    }

    /// <summary>
    /// `HesabSahibiKycTapAsync`-in nəticəsi (12.11.2026-dan) — Debet VƏ Kredit
    /// hesablarının HƏR İKİSİNİN KYC-si, MÜSTƏQİL. Köhnə versiya (yalnız bir
    /// tək <see cref="GoamlHesabSahibiKycDto"/> qaytarırdı, "Debet-ə bax,
    /// faydalı olsa dayan, olmasa Kredit-ə keç") real hadisədə (35025→40140)
    /// səhvə düşürdü: Debet-də YALNIZ Nümayəndə (CON) tapılıb Rəhbər (SIG)
    /// boş qalanda, kod "faydalı" sayıb Kredit-ə HEÇ BAXMIRDI — halbuki əsl
    /// Rəhbər elə orada idi. İndi hər iki tərəfin nəticəsi ayrıca saxlanır,
    /// hansının tətbiq olunacağını Tip-in istiqaməti (OUT/IN,
    /// <see cref="FinNex.Domain.Entities.Goaml.GoamlTipiExtensions.BizimTerefDebetdir"/>)
    /// həll edir — operator Tip seçən kimi (client-side, əlavə server sorğusu
    /// olmadan). Hər iki sahə `null`dursa, o tərəfdə heç bir faydalı KYC
    /// tapılmayıb deməkdir (nə Rəhbər/TekSexs, nə əlaqəli şəxs siyahısı).
    /// </summary>
    public class GoamlHesabSahibiKycHerIkiTerefDto
    {
        public GoamlHesabSahibiKycDto? Debet { get; set; }
        public GoamlHesabSahibiKycDto? Kredit { get; set; }
    }
}
