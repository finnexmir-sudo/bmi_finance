using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Jeton
{
    public class IsciJetonuListDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciTamAd { get; set; } = null!;
        public int JetonTeyinatiId { get; set; }
        public string JetonAd { get; set; } = null!;
        public JetonNovu JetonNovu { get; set; }
        public JetonRengi JetonRengi { get; set; }
        public string JetonIkon { get; set; } = null!;
        public string JetonRengKodu { get; set; } = null!;
        public decimal JetonSaatDeyeri { get; set; }
        public JetonVahid JetonVahid { get; set; }
        public decimal? QalanSaat { get; set; }
        public DateTime QazanmaTarixi { get; set; }
        public string Sebeb { get; set; } = null!;
        public IsciJetonuStatus Status { get; set; }
        public int? RedimTelebiId { get; set; }
    }
}
