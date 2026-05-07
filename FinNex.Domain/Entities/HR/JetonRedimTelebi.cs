namespace FinNex.Domain.Entities.HR
{
    public class JetonRedimTelebi : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public RedimNovu RedimNovu { get; set; }
        public decimal CemiSaat { get; set; }

        public RedimStatus Status { get; set; } = RedimStatus.Gozlenilir;

        public DateTime TelabTarixi { get; set; } = DateTime.Now;
        public DateTime? NeticeTarixi { get; set; }

        public int? TesdiqleyenUserId { get; set; }
        public string? Qeyd { get; set; }

        public ICollection<IsciJetonu> XerclenenJetonlar { get; set; } = new List<IsciJetonu>();
    }
}
