using FinNex.Domain.Entities.Goaml;

namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>
    /// Operatorun "Təsdiqlə" formundan göndərdiyi dəyərlər — namizəd əməliyyatın
    /// açarı + bütün KYC blokları. Sahə adları <see cref="GoamlEmeliyyat"/> ilə
    /// EYNİDİR (manual mapping asan olsun deyə) — bax o faylın baş şərhi.
    /// </summary>
    public class GoamlTesdiqDto
    {
        // Namizədin Oracle açarı
        public int CandidateFilialNumber { get; set; }
        public DateTime CandidateDateOper { get; set; }
        public long CandidateRecNum { get; set; }

        public string? Debet { get; set; }
        public string? Kredit { get; set; }
        public string? VipNetDa { get; set; }
        public decimal? SummaVInval { get; set; }
        public decimal? SummaVNacval { get; set; }
        public int? KodValuti { get; set; }
        public decimal? KursValuti { get; set; }
        public GoamlTipi? Tip { get; set; }
        public GoamlHesabatNovu? ReportCode { get; set; }
        public string? TransactionNumber { get; set; }
        public string? Primechanie { get; set; }
        public DateTime? DateOper { get; set; }
        public string? TransmodeCode { get; set; }
        public string? TransmodeComment { get; set; }
        public decimal? AmountLocal { get; set; }
        public string? FromFundsCode { get; set; }
        public string? ForeignCurrencyCode { get; set; }
        public string? ForeignCurrencyCodeFrom { get; set; }
        public decimal? ForeignAmount { get; set; }
        public decimal? ForeignExchangeRate { get; set; }
        public string? ToFundsCode { get; set; }
        public string? Action { get; set; }
        public string? Reason { get; set; }
        public string? Url { get; set; }
        public DateTime? Opened { get; set; }
        public string? Business { get; set; }
        public string? RegistracNomer { get; set; }
        public string? Branch { get; set; }
        public string? Iban { get; set; }
        public string? PersonalAccountType { get; set; }
        public string? GelirMenbeyi { get; set; }
        public bool? ToFizik { get; set; }
        public bool? ToYurik { get; set; }
        public bool? ToPred { get; set; }
        public string? PanTo { get; set; }

        // Nümayəndənin (_CON)
        public string? ConAdi { get; set; }
        public string? ConAtasininAdi { get; set; }
        public string? ConSoyadi { get; set; }
        public DateTime? ConDogulduguTarix { get; set; }
        public string? ConDogulduguYer { get; set; }
        public string? ConVetendasligi { get; set; }
        public string? ConTelefonNovu { get; set; }
        public string? ConRabiteNovu { get; set; }
        public string? ConTelefonNomresi { get; set; }
        public string? ConUnvaninNovu { get; set; }
        public string? ConUnvan { get; set; }
        public string? ConSeher { get; set; }
        public string? ConOlke { get; set; }
        public string? ConSenedinNovu { get; set; }
        public string? ConSenedinSeriyaNomresi { get; set; }
        public DateTime? ConSenedinVerilmeTarixi { get; set; }
        public DateTime? ConSenedinEtibarliliqMuddeti { get; set; }
        public string? ConVerenOrqan { get; set; }
        public string? ConVerildiyiOlke { get; set; }
        public string? ConCinsi { get; set; }
        public string? ConPin { get; set; }
        public string? ReprIdType { get; set; }
        public string? ReprIdSerial { get; set; }
        public string? ReprIdNumber { get; set; }

        // Hesab imza hüququ olan həxsin (_SIG)
        public string? SigAdi { get; set; }
        public string? SigAtasininAdi { get; set; }
        public string? SigSoyadi { get; set; }
        public DateTime? SigDogulduguTarix { get; set; }
        public string? SigDogulduguYer { get; set; }
        public string? SigVetendasligi { get; set; }
        public string? SigTelefonNovu { get; set; }
        public string? SigRabiteNovu { get; set; }
        public string? SigTelefonNomresi { get; set; }
        public string? SigUnvaninNovu { get; set; }
        public string? SigUnvan { get; set; }
        public string? SigSeher { get; set; }
        public string? SigOlke { get; set; }
        public string? SigSenedinNovu { get; set; }
        public string? SigSenedinSeriyaNomresi { get; set; }
        public DateTime? SigSenedinVerilmeTarixi { get; set; }
        public DateTime? SigSenedinEtibarliliqMuddeti { get; set; }
        public string? SigVerenOrqan { get; set; }
        public string? SigVerildiyiOlke { get; set; }
        public string? SigVezife { get; set; }
        public string? SigIsYeriAdi { get; set; }

        // Hüquqi şəxsin (suffikssiz, göndərən tərəf)
        public string? InstitutionName { get; set; }
        public string? Swift { get; set; }
        public string? Account { get; set; }
        public string? IncorporationNumber { get; set; }
        public string? IncorporationCountryCode { get; set; }
        public string? NameUr { get; set; }
        public string? FromCountry { get; set; }
        public DateTime? HesabinAcilmaTarixi { get; set; }
        public string? BankinAdi { get; set; }
        public string? SeherinKodu { get; set; }
        public string? FealiyyatIstiqameti { get; set; }

        // Alan fiziki həxsin (_TO, fiziki)
        public string? ToAdi { get; set; }
        public string? ToAtasininAdi { get; set; }
        public string? ToSoyadi { get; set; }
        public DateTime? ToDogulduguTarix { get; set; }
        public string? ToDogulduguYer { get; set; }
        public string? ToVetendasligi { get; set; }
        public string? ToTelefonNovu { get; set; }
        public string? ToRabiteNovu { get; set; }
        public string? ToTelefonNomresi { get; set; }
        public string? ToUnvaninNovu { get; set; }
        public string? ToUnvan { get; set; }
        public string? ToSeher { get; set; }
        public string? ToOlke { get; set; }
        public string? ToOlkeAdi { get; set; }
        public string? ToSenedinNovu { get; set; }
        public string? ToSenedinSeriyaNomresi { get; set; }
        public DateTime? ToSenedinVerilmeTarixi { get; set; }
        public DateTime? ToSenedinEtibarliliqMuddeti { get; set; }
        public string? ToVerenOrqan { get; set; }
        public string? ToVerildiyiOlke { get; set; }

        // Banka aid olmayan müş. Hüquq şəxsin (_TO, hüquqi)
        public string? IncorporationCountryCodeTo { get; set; }
        public string? IncorporationNumberTo { get; set; }
        public string? NameUrTo { get; set; }
        public string? InstitutionNameTo { get; set; }
        public string? SwiftTo { get; set; }
        public string? AccountTo { get; set; }
        public string? AlaqeNovuTo { get; set; }
        public string? InstitutionNameBranchTo { get; set; }

        public string? Gender { get; set; }
        public string? Snn { get; set; }
        public string? GenderTo { get; set; }
        public string? SnnTo { get; set; }
        public string? Occupation { get; set; }
        public string? EmployerName { get; set; }
        public string? SenderIdType { get; set; }
        public string? SenderIdSerial { get; set; }
        public string? SenderIdNumber { get; set; }
        public string? RecIdType { get; set; }
        public string? RecIdSerial { get; set; }
        public string? RecIdNumber { get; set; }
    }
}
