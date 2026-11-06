namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>
    /// Oracle-dan (DOC_DNA_ALL_TAB + DOC_DNA_ALL_RISK) oxunan BİR namizəd
    /// əməliyyat sətri — "Əməliyyatlar" tabının axtarış nəticəsi.
    /// Oracle-a YAZILMIR, YALNIZ göstərilir/seçilir.
    /// </summary>
    public class GoamlNamizedSetirDto
    {
        public long RecNum { get; set; }
        public DateTime DateOper { get; set; }
        public int FilialNumber { get; set; }
        public string? Debet { get; set; }
        public string? Kredit { get; set; }
        public decimal? SummaVInval { get; set; }
        public decimal? SummaVNacval { get; set; }
        public int? KodValuti { get; set; }
        public decimal? KursValuti { get; set; }
        public string? Primechanie { get; set; }
        public string? InnLicschDe { get; set; }
        public string? InnLicschKre { get; set; }
        public string? CountryCodeDe { get; set; }
        public string? CountryCodeKre { get; set; }
        public string? RegistracNomerDe { get; set; }
        public string? RegistracNomerKre { get; set; }
        public string? DocumentSource { get; set; }
        /// <summary>Debet hesabın sahibi (DOC_DNA_ALL_RISK.NAME_LICSCH_DE).</summary>
        public string? NameLicschDe { get; set; }
        /// <summary>Kredit hesabın sahibi (DOC_DNA_ALL_RISK.NAME_LICSCH_KRE).</summary>
        public string? NameLicschKre { get; set; }

        /// <summary>Bu namizəd artıq bizim bazada təsdiqlənibmi (eyni açarla
        /// GoamlEmeliyyat sətri varmı) — siyahıda "artıq göndərilib" işarəsi üçün.</summary>
        public bool ArtiqTesdiqlenib { get; set; }
    }
}
