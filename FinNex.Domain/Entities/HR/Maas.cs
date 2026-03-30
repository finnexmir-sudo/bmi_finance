namespace FinNex.Domain.Entities.HR
{
    // MASTER: Hər ay üçün bir sətir
    public class Maas : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }
        public DateTime HesablanmaTarixi { get; set; } = DateTime.UtcNow;
        public DateTime? TesdiqTarixi { get; set; }
        public DateTime? OdenisTarixi { get; set; }

        public decimal BrutMebleg { get; set; } // Tutulmalardan evvel mebleg
        public decimal NetMebleg { get; set; } // Iscinin kartina yatan son mebleg

        public MaasStatus Status { get; set; } = MaasStatus.Layihe;

        public ICollection<MaasDetay> Detallar { get; set; } = new List<MaasDetay>();
    }
}