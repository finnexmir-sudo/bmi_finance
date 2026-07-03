namespace FinNex.Application.DTOs.HR.Vezife
{
    public class VezifeCreateDto
    {
        public string Ad { get; set; } = null!;
        public int DepartamentId { get; set; }
        public string? Tesvir { get; set; }
        public int EsasMezuniyyetGunu { get; set; } = 21;
    }

}
