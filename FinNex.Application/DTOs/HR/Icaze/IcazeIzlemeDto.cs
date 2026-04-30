namespace FinNex.Application.DTOs.HR.Icaze
{
    public class IcazeIsciIstatistikDto
    {
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        public string? VezifeAdi { get; set; }

        public int CemiMuraciet { get; set; }
        public int TesdiqlenibSayi { get; set; }
        public int GozlemeSayi { get; set; }
        public int ImtinaEdildiSayi { get; set; }

        public double UmumSaat { get; set; }
        public double TesdiqSaat { get; set; }

        public DateTime? SonIcazeTarixi { get; set; }

        public List<IcazeListDto> Icazeler { get; set; } = new();
    }

    public class IcazeIzlemeFiltrDto
    {
        public DateTime? TarixFrom { get; set; }
        public DateTime? TarixTo { get; set; }
        public int? DepartamentId { get; set; }
        public string? Axtaris { get; set; }
        public int? Status { get; set; }
    }
}
