// FinNex.Domain.Entities.Communication/Gorush.cs
using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class Gorush : BaseEntity
    {
        public string Bashliq { get; set; } = "";
        public string? Agenda { get; set; }

        public int TeshkilatciIsciId { get; set; }
        public Isci TeshkilatciIsci { get; set; } = null!;

        public DateTime Tarix { get; set; }
        public TimeSpan BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }

        public string? Yer { get; set; }       // Otaq adı və ya ünvan
        public string? OnlineLink { get; set; } // Zoom/Teams linki

        public GorushNovu Nov { get; set; } = GorushNovu.Offline;
        public GorushStatus Status { get; set; } = GorushStatus.Planlanib;

        public string? Qeydler { get; set; } // Görüş sonrası qeydlər

        public ICollection<GorushIshtirakci> Ishtirakcılar { get; set; } = new List<GorushIshtirakci>();
    }

    public enum GorushNovu
    {
        Offline = 1,
        Online = 2,
        Hibrid = 3
    }

    public enum GorushStatus
    {
        Planlanib = 1,
        Bashladi = 2,
        Bitdi = 3,
        LegvEdildi = 4
    }
}