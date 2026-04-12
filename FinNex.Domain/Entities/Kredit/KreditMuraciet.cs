using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kredit
{
    public class KreditMuraciet : BaseEntity
    {
        // Müştəri məlumatları
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

        // Status + qiymətləndirmə
        public KreditMuracietStatus Status { get; set; } = KreditMuracietStatus.Yeni;
        public string? Qeyd { get; set; }
        public int? BaxanIsciId { get; set; }
        public Isci? BaxanIsci { get; set; }
        public DateTime? BaxilmaTarixi { get; set; }

        // Komitə
        public string? KomiteQerari { get; set; }
        public string? KomiteProtokolNo { get; set; }
        public DateTime? KomiteQerarTarixi { get; set; }
    }

    public enum KreditMuracietStatus
    {
        Yeni = 0,
        Yoxlanilir = 1,
        KomiteyeGonderildi = 2,
        Tesdiqlenib = 3,
        ReddEdilib = 4
    }
}
