namespace FinNex.Application.DTOs.HR.Maas
{
    public class MaasDetailDto
    {
        public int Id { get; set; }

        public string IsciTamAd { get; set; } = null!;
        public string Sobe { get; set; } = null!;
        public string Vezife { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }

        public decimal EsasMaas { get; set; }
        public decimal Elave { get; set; }
        public decimal Cerime { get; set; }

        public decimal UmumiMaas { get; set; }
    }

}
