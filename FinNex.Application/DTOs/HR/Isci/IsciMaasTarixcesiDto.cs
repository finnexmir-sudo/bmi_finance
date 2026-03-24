namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciMaasTarixcesiDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public decimal KohneMaas { get; set; }
        public decimal YeniMaas { get; set; }
        public DateTime DeyismeTarixi { get; set; }
        public string? EmrinNomresi { get; set; }
    }
}
