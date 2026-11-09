namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>
    /// Hesab sahibinin (fiziki şəxsin özü, yaxud hüquqi şəxsin əlaqəli şəxsi —
    /// Rəhbər/Təsisçi/İmza hüququ) GOAML CON/SIG bloklarına uyğun KYC-si.
    /// Mənbə: fiziki hesabda `odb.regnom`-un ingiliscə sütunları, hüquqi hesabda
    /// `odb.imza_huquqi_olan_shexsler` (bax GoamlService.HesabSahibiKycTapAsync,
    /// 09.11.2026 araşdırması). Sahə adları GoamlTesdiqDto-nun Con*/Sig*
    /// sahələri ilə EYNİDİR ki, mapping sadə olsun.
    /// </summary>
    public class GoamlElaqaliSexsDto
    {
        /// <summary>Yalnız hüquqi şəxsin əlaqəli şəxsləri üçün — `imza_huquqi_olan_shexsler.ID_AML`.
        /// CON seçim siyahısında hər namizədi fərqləndirmək üçün.</summary>
        public int? IdAml { get; set; }

        /// <summary>Yalnız seçim siyahısında göstərmək üçün — `aml_setup_related_persons.DESCRIPT`-dən
        /// (Rəhbər, Təsisçi, Hesaba imza hüququ olan şəxslər və s.), vergüllə ayrılmış.</summary>
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
}
