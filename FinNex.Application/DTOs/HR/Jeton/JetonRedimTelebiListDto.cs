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

        // Icaze sorğusu detalları (yalnız RedimNovu == Icaze)
        public DateTime? IcazeTarixi { get; set; }
        public TimeSpan? BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }

        // Rəhbər təsdiq mərhələsi
        public bool? RehberTesdiq { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }
        public string? RehberAd { get; set; }
        public string? RehberQeyd { get; set; }

        public List<IsciJetonuListDto> XerclenenJetonlar { get; set; } = new();
    }
}
