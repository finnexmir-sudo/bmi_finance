namespace FinNex.UI.Areas.Kassa.ViewModels
{
    /// <summary>Formdan gələn xam dəyərlər — valyuta kodu önşəkilçili sahə adları
    /// (BMI-dəki txtNAUSD → USD_NA s.) sadə, JS massiv binding olmadan.</summary>
    public class ExchangeSaxlaVM
    {
        public DateTime Tarix { get; set; }

        public decimal? USD_NA { get; set; }
        public decimal? USD_NS { get; set; }
        public decimal? USD_QNA { get; set; }
        public decimal? USD_QNS { get; set; }

        public decimal? AVRO_NA { get; set; }
        public decimal? AVRO_NS { get; set; }
        public decimal? AVRO_QNA { get; set; }
        public decimal? AVRO_QNS { get; set; }

        public decimal? IRR_NA { get; set; }
        public decimal? IRR_NS { get; set; }
        public decimal? IRR_QNA { get; set; }
        public decimal? IRR_QNS { get; set; }

        public decimal? AED_NA { get; set; }
        public decimal? AED_NS { get; set; }
        public decimal? AED_QNA { get; set; }
        public decimal? AED_QNS { get; set; }

        public decimal? RUB_NA { get; set; }
        public decimal? RUB_NS { get; set; }
        public decimal? RUB_QNA { get; set; }
        public decimal? RUB_QNS { get; set; }
    }
}
