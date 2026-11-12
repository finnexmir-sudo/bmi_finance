namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>
    /// Oracle-dan (DOC_DNA_ALL_RISK — 11.11.2026-dan, əvvəl DOC_DNA_ALL_TAB
    /// idi, bax 90_Goaml_OracleSorgular.sql-in baş şərhi) oxunan BİR namizəd
    /// əməliyyat sətri — "Əməliyyatlar" VƏ "Risk" tablarının ortaq nəticə
    /// formasıdır. Oracle-a YAZILMIR, YALNIZ göstərilir/seçilir.
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

        /// <summary>Bu namizəd artıq (ən azı bir Tip ilə) bizim bazada
        /// təsdiqlənibmi — siyahıda "artıq göndərilib" işarəsi üçün. 12.11.2026-
        /// dan "Təsdiqlə" düyməsini TAMAMİLƏ gizlətmir, yalnız yanında kiçik
        /// bir bədgi göstərir — bax <see cref="TesdiqlenmisTipler"/>.</summary>
        public bool ArtiqTesdiqlenib { get; set; }

        /// <summary>Bu namizəd üçün ARTIQ təsdiqlənmiş Tip(lər)in etiketi
        /// (12.11.2026, AML məsul şəxsin tələbi: hər iki tərəfdə KYC tapılan
        /// əməliyyat tez-tez İKİ AYRI hesabat kimi — hər tərəf üçün fərqli Tip
        /// ilə — göndərilməlidir). Boşdursa heç vaxt təsdiqlənməyib. Eyni
        /// candidate açarı (filial+tarix+recnum) FƏRQLİ Tip ilə TƏKRAR
        /// təsdiqlənə bilər — server (`TesdiqleAsync`) yalnız EYNİ Tip-in
        /// təkrarını bloklayır.</summary>
        public List<string> TesdiqlenmisTipler { get; set; } = new();
    }
}
