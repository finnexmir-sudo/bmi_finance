namespace FinNex.Domain.Entities.HR
{
    public class JetonTeyinati : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public JetonNovu Nov { get; set; }
        public JetonRengi Rengi { get; set; }
        public decimal SaatDeyeri { get; set; }
        public string Ikon { get; set; } = null!;
        public string RengKodu { get; set; } = null!;
        public string? Tesvir { get; set; }
        public bool Aktivdir { get; set; } = true;

        public ICollection<IsciJetonu> IsciJetonlari { get; set; } = new List<IsciJetonu>();
    }
}
