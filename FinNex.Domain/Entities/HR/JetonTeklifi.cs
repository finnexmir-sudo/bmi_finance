namespace FinNex.Domain.Entities.HR
{
    public class JetonTeklifi : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;
        public JetonTeklifinNovu TeklifNovu { get; set; }
        public string Metn { get; set; } = null!;
        public string? ElaveMelumat { get; set; }
        public JetonTeklifinStatusu Status { get; set; } = JetonTeklifinStatusu.Gozlenir;
        public int? IslemEdenUserId { get; set; }
        public DateTime? IslemTarixi { get; set; }
    }

    public enum JetonTeklifinNovu
    {
        IsGununuAshdi = 1,
        EvezediciOldu = 2,
        TapshiriqTamamlandi = 3,
        GecikmeCanDoldu = 4
    }

    public enum JetonTeklifinStatusu
    {
        Gozlenir = 1,
        JetonVerildi = 2,
        Reddi = 3
    }
}
