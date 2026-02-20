namespace FinNex.Application.DTOs.HR.Maas
{
    public class MaasListDto
    {
        public int Id { get; set; }
        public string IsciTamAd { get; set; } = null!;
        public string Sobe { get; set; } = null!;
        public int Il { get; set; }
        public int Ay { get; set; }
        public string AyAd { get; set; } = null!;
        public decimal EsasMaas { get; set; }
        public decimal Elave { get; set; }
        public decimal Cerime { get; set; }
        public decimal UmumiMaas { get; set; }
    }

}
