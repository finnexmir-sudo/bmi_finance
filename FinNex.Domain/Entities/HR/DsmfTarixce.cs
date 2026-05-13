namespace FinNex.Domain.Entities.HR
{
    public class DsmfTarixce
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal IsciDsmf { get; set; }
        public decimal SirketDsmf { get; set; }
    }
}
