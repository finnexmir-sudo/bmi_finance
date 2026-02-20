namespace FinNex.Domain.Entities.HR
{
    public class BayramGunu : BaseEntity
    {
        public string? Ad { get; set; } // Məsələn: Qurban Bayramı
        public DateTime Tarix { get; set; }
        public bool HerIlTeyinOlunur { get; set; } // Sabit tarixlər üçün (20 Yanvar və s.)
    }
}
