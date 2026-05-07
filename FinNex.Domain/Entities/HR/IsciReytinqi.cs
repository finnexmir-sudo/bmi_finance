namespace FinNex.Domain.Entities.HR
{
    public class IsciReytinqi : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }

        public int AvtomatikXal { get; set; }
        public int ManualXal { get; set; }
        public int CemiXal { get; set; }              // AvtomatikXal + ManualXal

        public int? KateqoriyaId { get; set; }
        public ReytingKateqoriyasi? Kateqoriya { get; set; }

        public DateTime HesablamaTarixi { get; set; } = DateTime.Now;

        public ICollection<ManualReytingQeydi> ManualQeydler { get; set; } = new List<ManualReytingQeydi>();
    }
}
