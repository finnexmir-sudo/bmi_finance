namespace FinNex.Application.DTOs.HR.Vezife
{
    public class VezifeDetailDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }
        public bool Aktivdir { get; set; }
        public int EsasMezuniyyetGunu { get; set; } = 21;
        // Vəzifə adının yönlük halı — məzuniyyət əmrləri üçün ("rəis" → "rəisinə").
        // Boşdursa sistem avtomatik şəkilçi qoşur.
        public string? YonlukHal { get; set; }

    }

}
