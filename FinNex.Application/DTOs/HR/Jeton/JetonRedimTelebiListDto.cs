using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Jeton
{
    public class JetonRedimTelebiListDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciTamAd { get; set; } = null!;
        public RedimNovu RedimNovu { get; set; }
        public decimal CemiSaat { get; set; }
        public RedimStatus Status { get; set; }
        public DateTime TelabTarixi { get; set; }
        public DateTime? NeticeTarixi { get; set; }
        public string? Qeyd { get; set; }
        public string? TesdiqleyenAd { get; set; }
        public List<IsciJetonuListDto> XerclenenJetonlar { get; set; } = new();
    }
}
