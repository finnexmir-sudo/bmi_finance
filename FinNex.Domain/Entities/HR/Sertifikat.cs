namespace FinNex.Domain.Entities.HR
{
    public class Sertifikat : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public string Ad { get; set; } = null!;
        public string? VerenQurum { get; set; }
        public DateTime VerilmeTarixi { get; set; }
        public DateTime? BitirmeTarixi { get; set; } // Null = müddətsiz

        public string? FaylYolu { get; set; }
    }
}
