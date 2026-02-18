namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasDetailVM
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

        // Computed
        public string AyAd => Ay switch
        {
            1  => "Yanvar",  2  => "Fevral",   3  => "Mart",
            4  => "Aprel",   5  => "May",       6  => "İyun",
            7  => "İyul",    8  => "Avqust",    9  => "Sentyabr",
            10 => "Oktyabr", 11 => "Noyabr",   12 => "Dekabr",
            _  => Ay.ToString()
        };
    }
}
