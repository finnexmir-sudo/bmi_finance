namespace FinNex.Domain.Entities.HR
{
    public class ReytingParametri : BaseEntity
    {
        public ReytingHadiseNovu HadiseNovu { get; set; }
        public string Ad { get; set; } = null!;      // "Müsbət jeton aldı", "Gecikmə"
        public int XalDeyeri { get; set; }            // müsbət və ya mənfi ola bilər
        public bool Aktivdir { get; set; } = true;
    }
}
