namespace FinNex.Application.DTOs.HR.Vezife
{
    public class VezifeDetailDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }
        public bool Aktivdir { get; set; }
    }

}
