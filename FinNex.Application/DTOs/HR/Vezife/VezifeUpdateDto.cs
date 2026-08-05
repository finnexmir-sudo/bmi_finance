namespace FinNex.Application.DTOs.HR.Vezife
{
    public class VezifeUpdateDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public int DepartamentId { get; set; }
        public string? Tesvir { get; set; }
        public bool Aktivdir { get; set; }

        // Vəzifə adının yönlük halı — məzuniyyət əmrləri üçün ("rəis" → "rəisinə").
        // Boşdursa sistem avtomatik şəkilçi qoşur.
        public string? YonlukHal { get; set; }
        public int EsasMezuniyyetGunu { get; set; } = 21;
    }

}
