namespace FinNex.Domain.Entities.HR
{
    public class Telim : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }
        public string? Tesviqci { get; set; } // Təlimçi/təşkilatçı adı
        public string? Mekan { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }
        public int MuddетSaat { get; set; }

        public TelimStatus Status { get; set; } = TelimStatus.Planlanib;
        public bool DaxiliTelimdir { get; set; } = true;
        public decimal? Xerc { get; set; }

        public ICollection<TelimIshtiraki> Ishtirakcilar { get; set; } = new List<TelimIshtiraki>();
    }
}
