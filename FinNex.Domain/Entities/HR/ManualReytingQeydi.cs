namespace FinNex.Domain.Entities.HR
{
    public class ManualReytingQeydi : BaseEntity
    {
        public int IsciReytinqiId { get; set; }
        public IsciReytinqi IsciReytinqi { get; set; } = null!;

        public int Xal { get; set; }                  // müsbət və ya mənfi
        public string Sebeb { get; set; } = null!;
        public int ElavEdenUserId { get; set; }
        public DateTime Tarix { get; set; } = DateTime.Now;
    }
}
