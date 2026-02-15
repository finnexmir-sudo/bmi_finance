namespace FinNex.Domain.Entities.HR
{
    public class Maas : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }

        public decimal EsasMaas { get; set; }
        public decimal Elave { get; set; }
        public decimal Cərimə { get; set; }

        public decimal UmumiMaas { get; set; }
    }

}
