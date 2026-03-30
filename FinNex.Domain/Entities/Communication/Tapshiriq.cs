// FinNex.Domain.Entities.Communication/Tapshiriq.cs
using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class Tapshiriq : BaseEntity
    {
        public string Bashliq { get; set; } = "";
        public string? Tesvir { get; set; }

        public int YaradanIsciId { get; set; }
        public Isci YaradanIsci { get; set; } = null!;

        public int TeyinOlunanIsciId { get; set; }
        public Isci TeyinOlunanIsci { get; set; } = null!;

        public DateTime? SonTarix { get; set; }
        public TapshiriqPrioritet Prioritet { get; set; } = TapshiriqPrioritet.Orta;
        public TapshiriqStatus Status { get; set; } = TapshiriqStatus.Yeni;

        public int TamamlanmaFaizi { get; set; } = 0; // 0-100
        public string? Qeyd { get; set; }

        public ICollection<TapshiriqSherh> Sherhler { get; set; } = new List<TapshiriqSherh>();
    }

    public enum TapshiriqPrioritet
    {
        Ashagi = 1,
        Orta = 2,
        Yuksek = 3,
        Kritik = 4
    }

    public enum TapshiriqStatus
    {
        Yeni = 1,
        DavamEdir = 2,
        Tamamlandi = 3,
        LegvEdildi = 4,
        Gecikdi = 5
    }
}