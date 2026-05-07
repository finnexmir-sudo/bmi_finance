using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Motivasya
{
    public class JetonTeklifiDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAd { get; set; } = null!;
        public string? Vezife { get; set; }
        public string? Departament { get; set; }
        public IsciStatus IsciStatus { get; set; }
        public string IsciStatusAd { get; set; } = null!;
        public JetonTeklifinNovu TeklifNovu { get; set; }
        public string TeklifNovuAd { get; set; } = null!;
        public string TeklifNovuReng { get; set; } = null!;
        public string Metn { get; set; } = null!;
        public string? ElaveMelumat { get; set; }
        public JetonTeklifinStatusu Status { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }

    public class JetonTeklifiVerDto
    {
        public int TeklifId { get; set; }
        public int JetonTeyinatiId { get; set; }
        public string? Qeyd { get; set; }
    }
}
