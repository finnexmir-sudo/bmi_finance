namespace FinNex.Domain.Entities.HR
{
    // Maaş dəyişikliklərini izləmək üçün arxiv
    public class IsciMaasTarixcesi : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public decimal KohneMaas { get; set; }
        public decimal YeniMaas { get; set; }
        public DateTime DeyismeTarixi { get; set; }
        public string? EmrinNomresi { get; set; } // Rəsmi əmr
    }
}