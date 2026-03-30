namespace FinNex.Application.DTOs.HR.Vezife
{
    public class VezifeListDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public int DepartamentId { get; set; }
        public string DepartamentAd { get; set; } = null!;
        public int IsciSayi { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
