namespace FinNex.Domain.Entities.Kredit
{
    public class KreditMuraciet : BaseEntity
    {
        public string AdSoyadAtaAdi { get; set; } = null!;
        public string? FIN { get; set; }
        public decimal KreditMeblegi { get; set; }
        public string Valyuta { get; set; } = "AZN";
        public string? KreditMuddeti { get; set; }
        public string? IsYeri { get; set; }
        public decimal? EmekHaqqi { get; set; }
        public string? Telefon { get; set; }
        public string? Meqsed { get; set; }

        // Mail metadata
        public DateTime MuracietTarixi { get; set; }
        public string? IP { get; set; }
        public string? MailMessageId { get; set; }

        // Status
        public KreditMuracietStatus Status { get; set; } = KreditMuracietStatus.Yeni;
        public string? Qeyd { get; set; }
        public int? BaxanIsciId { get; set; }
        public DateTime? BaxilmaTarixi { get; set; }
    }

    public enum KreditMuracietStatus
    {
        Yeni = 0,
        Baxilir = 1,
        Tesdiqlenib = 2,
        ReddEdilib = 3
    }
}
