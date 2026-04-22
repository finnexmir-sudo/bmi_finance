using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kredit
{
    public class KreditMuraciet : BaseEntity
    {
        // ─── Müştəri məlumatları ─────────────────────────────
        public string AdSoyadAtaAdi { get; set; } = null!;
        public string? FIN { get; set; }
        public decimal KreditMeblegi { get; set; }
        public string Valyuta { get; set; } = "AZN";
        public string? KreditMuddeti { get; set; }
        public string? IsYeri { get; set; }
        public decimal? EmekHaqqi { get; set; }
        public string? Telefon { get; set; }
        public string? Meqsed { get; set; }

        // ─── Mənbə və metadata ───────────────────────────────
        public DateTime MuracietTarixi { get; set; }
        public string? IP { get; set; }
        public string? MailMessageId { get; set; }
        public KreditMuracietMenbe Menbe { get; set; } = KreditMuracietMenbe.Online;

        // ─── Status ──────────────────────────────────────────
        public KreditMuracietStatus Status { get; set; } = KreditMuracietStatus.Yeni;
        public string? Qeyd { get; set; }

        // ─── İlkin baxış (işçi mərhələsi) ────────────────────
        public int? BaxanIsciId { get; set; }
        public Isci? BaxanIsci { get; set; }
        public DateTime? BaxilmaTarixi { get; set; }
        public string? IlkinBaxisQeyd { get; set; }

        // ─── MKR (Mərkəzi Kredit Reyestri) yoxlaması ────────
        // Hazırda manual daxil edilir — gələcəkdə API inteqrasiyası olacaq
        public string? MkrNeticesi { get; set; }
        public DateTime? MkrBaxilmaTarixi { get; set; }
        public int? MkrBaxanIsciId { get; set; }
        public Isci? MkrBaxanIsci { get; set; }

        // ─── AsanFinance yoxlaması ───────────────────────────
        public string? AsanFinanceNeticesi { get; set; }
        public DateTime? AsanFinanceBaxilmaTarixi { get; set; }
        public int? AsanFinanceBaxanIsciId { get; set; }
        public Isci? AsanFinanceBaxanIsci { get; set; }

        // ─── Əlaqəli obyektlər ───────────────────────────────
        public ICollection<KreditZamin> Zaminler { get; set; } = new List<KreditZamin>();
        public KreditQerar? Qerar { get; set; }
        public KreditRandevu? Randevu { get; set; }
        public ICollection<KreditSmsLog> SmsLoglar { get; set; } = new List<KreditSmsLog>();

        // ─── Köhnə komitə sahələri (MIGRATION period) ────────
        // Bu sahələr artıq KreditQerar entity-də saxlanır. Mövcud controller/view
        // hələ köhnə formaya baxır; controller refaktorundan sonra bu sahələr
        // migration ilə silinəcək. Yeni kod bunları istifadə ETMƏMƏLİDİR.
        public string? KomiteQerari { get; set; }
        public string? KomiteProtokolNo { get; set; }
        public DateTime? KomiteQerarTarixi { get; set; }
        public decimal? TesdiqMebleg { get; set; }
        public string? TesdiqMuddet { get; set; }
        public decimal? FaizDerecesi { get; set; }
        public string? Teminat { get; set; }
    }

    public enum KreditMuracietStatus
    {
        Yeni = 0,
        Yoxlanilir = 1,
        KomiteyeGonderildi = 2,
        Tesdiqlenib = 3,
        ReddEdilib = 4
    }

    public enum KreditMuracietMenbe
    {
        Online = 0,
        Filial = 1
    }
}
