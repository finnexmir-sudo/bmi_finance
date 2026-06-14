namespace FinNex.Domain.Entities.HR
{
    // Əmr nömrə sayğacı — (Nov, Il) üzrə son verilmiş nömrəni saxlayır.
    // Növbəti nömrə = SonNomre + 1. HR köhnə (kağız) əmrlərin davamı üçün
    // SonNomre-ni əl ilə təyin edə bilər → növbəti generasiya oradan başlayır.
    public class EmrSayghaci : BaseEntity
    {
        public EmrNovu Nov      { get; set; }
        public int     Il       { get; set; }
        public int     SonNomre { get; set; }
    }
}
