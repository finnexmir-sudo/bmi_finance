using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kredit
{
    /// <summary>
    /// Müştəriyə göndərilən SMS-lərin loqu. İlkin mərhələdə real SMS gateway
    /// qoşulmadan, yalnız cədvələ yazılır — UI hazırdırsa, gateway əlavə
    /// edildikdə yalnız göndərmə servisi dəyişir.
    /// </summary>
    public class KreditSmsLog : BaseEntity
    {
        public int? KreditMuracietId { get; set; }
        public KreditMuraciet? KreditMuraciet { get; set; }

        public string Telefon { get; set; } = null!;
        public string Metn { get; set; } = null!;
        public string? Sablon { get; set; }   // Hansı şablon istifadə edildi (Təsdiq, Rədd, Randevu və s.)

        public KreditSmsStatus Status { get; set; } = KreditSmsStatus.Gozleyir;
        public DateTime? GonderilmeTarixi { get; set; }
        public string? GatewayCavabi { get; set; }
        public string? Xeta { get; set; }

        public int GonderenIsciId { get; set; }
        public Isci GonderenIsci { get; set; } = null!;
    }

    public enum KreditSmsStatus
    {
        Gozleyir = 0,
        Gonderilib = 1,
        Xeta = 2
    }
}
