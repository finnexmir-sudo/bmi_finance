namespace FinNex.Application.DTOs.Kassa
{
    public class KassaKursSetriDto
    {
        public string Valyuta { get; set; } = null!;
        public decimal? NagdAlis { get; set; }
        public decimal? NagdSatis { get; set; }
        public decimal? QeyriNagdAlis { get; set; }
        public decimal? QeyriNagdSatis { get; set; }
    }
}
